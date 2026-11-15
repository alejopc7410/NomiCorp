using Microsoft.EntityFrameworkCore;
using NominaEmpresa.API.Data;
using NominaEmpresa.API.DTOs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NominaEmpresa.API.Services;

public class BonusValidationService
{
    private readonly PayrollDbContext _context;
    private readonly BonusCalculationService _calculator;
    private readonly BonusLaborIncidentService _laborService;

    public BonusValidationService(
        PayrollDbContext context,
        BonusCalculationService calculator,
        BonusLaborIncidentService laborService)
    {
        _context = context;
        _calculator = calculator;
        _laborService = laborService;
    }


    private static DateOnly ColombiaToday()
    {
        var timeZone =
            TimeZoneInfo.FindSystemTimeZoneById(
                "America/Bogota");

        var localDate = TimeZoneInfo.ConvertTimeFromUtc(
            DateTime.UtcNow,
            timeZone);

        return DateOnly.FromDateTime(localDate);
    }

    private static bool DifferentMoney(
        decimal a,
        decimal b)
    {
        return Math.Abs(a - b) > 0.01m;
    }

    public async Task<BonusValidationDto> ValidateAsync(
        int settlementId)
    {
        var settlement = await _context.BonusSettlements
            .AsNoTracking()
            .Include(s => s.Details)
            .FirstOrDefaultAsync(
                s => s.Id == settlementId);

        if (settlement == null)
        {
            throw new KeyNotFoundException(
                "La liquidación de prima no existe.");
        }

        var today = ColombiaToday();

        var result = new BonusValidationDto
        {
            SettlementId = settlement.Id,
            Year = settlement.Year,
            Semester = settlement.Semester,
            ValidationDate = today,
            PeriodEndDate = settlement.EndDate,
            ReadyForApproval = false
        };

        // 1. Estado del borrador.
        if (settlement.Status != "Borrador")
        {
            result.Errors.Add(
                "La liquidación no está en estado Borrador.");
        }

        // 2. Verificar las fechas del semestre.
        DateOnly expectedStart = new(
            settlement.Year,
            settlement.Semester == 1 ? 1 : 7,
            1);

        DateOnly expectedEnd = settlement.Semester == 1
            ? new DateOnly(settlement.Year, 6, 30)
            : new DateOnly(settlement.Year, 12, 31);

        if (settlement.Semester != 1 &&
            settlement.Semester != 2)
        {
            result.Errors.Add(
                "El semestre no es válido.");
        }

        if (settlement.StartDate != expectedStart ||
            settlement.EndDate < expectedStart ||
            settlement.EndDate > expectedEnd)
        {
            result.Errors.Add(
                "Las fechas guardadas no corresponden " +
                "al semestre seleccionado.");
        }

        if (settlement.EndDate > today)
        {
            result.Errors.Add(
                "La fecha de corte incluye días futuros.");
        }

        if (settlement.EndDate < expectedEnd)
        {
            result.Warnings.Add(
                "Esta liquidación tiene una fecha de " +
                "corte anterior al cierre del semestre. " +
                "El valor representa una causación " +
                "parcial y no el semestre completo.");
        }

        // 3. Verificar que existan detalles.
        if (!settlement.Details.Any())
        {
            result.Errors.Add(
                "La liquidación no tiene empleados.");
        }

        // 4. Empleados duplicados.
        var duplicateEmployees = settlement.Details
            .GroupBy(d => d.EmployeeId)
            .Where(g => g.Count() > 1)
            .ToList();

        if (duplicateEmployees.Any())
        {
            result.Errors.Add(
                "Hay empleados con varios detalles " +
                "en el mismo semestre.");
        }

        // 5. Verificar valores individuales.
        foreach (var detail in settlement.Details)
        {
            if (detail.WorkedDays < 1 ||
                detail.WorkedDays > 180)
            {
                result.Errors.Add(
                    $"Días inválidos: {detail.EmployeeName}.");
            }

            if (detail.BonusAmount < 0 ||
                detail.SalaryAccrued < 0 ||
                detail.TransportAccrued < 0 ||
                detail.SalaryNovelties < 0)
            {
                result.Errors.Add(
                    $"Valores negativos: {detail.EmployeeName}.");
            }

            decimal expectedBonus = Math.Round(
                (
                    detail.SalaryAccrued +
                    detail.TransportAccrued +
                    detail.SalaryNovelties
                ) / 12m,
                2,
                MidpointRounding.AwayFromZero);

            // La comparación permite una pequeña
            // diferencia por redondeo de componentes.
            if (DifferentMoney(
                detail.BonusAmount,
                expectedBonus))
            {
                result.Errors.Add(
                    $"El importe de prima del empleado " +
                    $"{detail.EmployeeName} no coincide " +
                    "con la base almacenada.");
            }
        }

        // 6. Total de la liquidación.
        decimal savedTotal = settlement.Details
            .Sum(d => d.BonusAmount);

        if (DifferentMoney(
            settlement.TotalAmount,
            savedTotal))
        {
            result.Errors.Add(
                "El total guardado no coincide " +
                "con los detalles.");
        }

        // 7. Consultar todas las novedades relevantes.
        var contractIds = settlement.Details
            .Select(d => d.ContractId)
            .Distinct()
            .ToList();

        var novelties = await _context.PayrollNovelties
            .AsNoTracking()
            .Where(n =>
                contractIds.Contains(n.ContractId) &&
                n.NoveltyDate >= settlement.StartDate &&
                n.NoveltyDate <= settlement.EndDate &&
                n.Status != "Anulada")
            .ToListAsync();

        var pending = novelties
            .Where(n => n.Status != "Aprobada")
            .ToList();

        if (pending.Any())
        {
            result.Errors.Add(
                $"Existen {pending.Count} novedades " +
                "pendientes de aprobación o con " +
                "un estado no reconocido.");
        }

        var unclassified = novelties
            .Where(n =>
                n.Category == "Devengado" &&
                n.IsSalaryConstitutive == null)
            .ToList();

        if (unclassified.Any())
        {
            result.Errors.Add(
                $"Existen {unclassified.Count} devengados " +
                "sin clasificación salarial.");
        }

        // Las informativas todavía requieren
        // tratamiento especializado.

        var laborIncidents =
            await _laborService.GetIncidentsAsync(
                settlement.StartDate,
                settlement.EndDate,
                contractIds
            );

        result.LaborIncidents = laborIncidents;

        if (laborIncidents.Any(x => x.RequiresReview))
        {
            result.Errors.Add(
                $"Existen {laborIncidents.Count} novedades " +
                "laborales que requieren revisión. " +
                "No se han descontado días automáticamente."
            );
        }


        /*if (informative.Any())
        {
            result.Errors.Add(
                $"Existen {informative.Count} novedades " +
                "informativas que requieren revisión " +
                "de ausencias, licencias o incapacidades.");
        }*/

        // 8. Comparar con el cálculo actual.
        // Solo si las validaciones previas permiten
        // ejecutar la vista previa de forma coherente.
        if (!result.Errors.Any())
        {
            try
            {
                var preview = await _calculator.PreviewAsync(
                    settlement.Year,
                    settlement.Semester,
                    settlement.EndDate);

                if (preview.Details.Count !=
                    settlement.Details.Count)
                {
                    result.Errors.Add(
                        "La cantidad de detalles cambió. " +
                        "Recalcula el borrador.");
                }

                var previewByContract = preview.Details
                    .ToDictionary(d => d.ContractId);

                foreach (var saved in settlement.Details)
                {
                    if (!previewByContract.TryGetValue(
                        saved.ContractId,
                        out var current))
                    {
                        result.Errors.Add(
                            $"El contrato {saved.ContractId} " +
                            "ya no coincide con el cálculo.");
                        continue;
                    }

                    if (saved.EmployeeId != current.EmployeeId ||
                        saved.WorkedDays != current.WorkedDays ||
                        saved.Identification != current.Identification ||
                        saved.EmployeeName != current.EmployeeName ||
                        DifferentMoney(
                            saved.SalaryAccrued,
                            current.SalaryAccrued) ||
                        DifferentMoney(
                            saved.TransportAccrued,
                            current.TransportAccrued) ||
                        DifferentMoney(
                            saved.SalaryNovelties,
                            current.SalaryNovelties) ||
                        DifferentMoney(
                            saved.BonusAmount,
                            current.EstimatedBonus))
                    {
                        result.Errors.Add(
                            $"Los datos del empleado " +
                            $"{saved.EmployeeName} cambiaron. " +
                            "Recalcula el borrador.");
                    }
                }

                if (DifferentMoney(
                    settlement.TotalAmount,
                    preview.TotalBonus))
                {
                    result.Errors.Add(
                        "El total de prima está desactualizado.");
                }
            }
            catch (InvalidOperationException ex)
            {
                result.Errors.Add(
                    "No fue posible validar el nuevo cálculo: " +
                    ex.Message);
            }
        }

        var previousPayments = await _context.BonusPreviousPayments
            .AsNoTracking()
            .Where(p =>
                p.BonusSettlementId == settlement.Id)
            .ToListAsync();

        foreach (var detail in settlement.Details)
        {
            decimal paid = previousPayments
                .Where(p => p.EmployeeId == detail.EmployeeId)
                .Sum(p => p.Amount);

            if (paid > detail.BonusAmount)
            {
                result.Errors.Add(
                    $"Los pagos anteriores de " +
                    $"{detail.EmployeeName} superan " +
                    "la prima calculada.");
            }
        }

        result.Warnings.Add(
            "Los pagos históricos registrados deben " +
            "conciliarse con sus comprobantes originales " +
            "antes de aprobar la liquidación.");

        // 9. Reglas que aún debemos implementar.
        result.Warnings.Add(
            "Falta integrar el tratamiento específico " +
            "de suspensiones, licencias e incapacidades.");

        result.Warnings.Add(
            "Falta conciliar anticipos y primas " +
            "pagadas previamente.");

        result.Warnings.Add(
            "El cálculo no constituye todavía " +
            "una liquidación definitiva autorizada.");

        // La aprobación permanecerá bloqueada hasta
        // completar los controles legales pendientes.
        result.ReadyForApproval = false;

        return result;
    }
}
