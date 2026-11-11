
using Microsoft.EntityFrameworkCore;
using NominaEmpresa.API.Data;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NominaEmpresa.API.DTOs;
using NominaEmpresa.API.Models;

namespace NominaEmpresa.API.Services;

public class BonusCalculationService
{
    private readonly PayrollDbContext _context;

    public BonusCalculationService(PayrollDbContext context)
    {
        _context = context;
    }

    private static int PayrollDay(DateOnly date)
    {
        int lastDay = DateTime.DaysInMonth(
            date.Year, date.Month);

        return date.Day == lastDay
            ? 30
            : Math.Min(date.Day, 30);
    }

    private static decimal RoundMoney(decimal amount)
    {
        return Math.Round(
            amount, 2, MidpointRounding.AwayFromZero);
    }

    public async Task<BonusPreviewDto> PreviewAsync(int year, int semester, 
        DateOnly? cutoffDate = null, bool allowFutureProjection = false)
    {
        if (year != 2026)
            throw new InvalidOperationException(
                "La vista previa está configurada para 2026.");

        if (semester != 1 && semester != 2)
            throw new InvalidOperationException(
                "El semestre debe ser 1 o 2.");

        var today = DateOnly.FromDateTime(
            TimeZoneInfo.ConvertTimeBySystemTimeZoneId(
                DateTime.UtcNow,
                "America/Bogota"
            )
        );

        var start = new DateOnly(
            year, semester == 1 ? 1 : 7, 1);

        var end = new DateOnly(
            year, semester == 1 ? 6 : 12,
            semester == 1 ? 30 : 31);

        var cutoff = cutoffDate ?? (today < end ? today : end);

        if (cutoff < start || cutoff > end)
        {
            throw new InvalidOperationException(
                "La fecha de corte debe estar dentro " +
                "del semestre seleccionado.");
        }

        if (cutoff > today && !allowFutureProjection)
        {
            throw new InvalidOperationException(
                "No se pueden incluir días futuros " +
                "en una liquidación causada.");
        }

        const decimal transportValue = 249095m;
        const decimal transportLimit = 3501810m;

        var contracts = await _context.Contracts
            .Include(c => c.Employee)
            .Include(c => c.SalaryHistory)
            .Where(c =>
                c.StartDate <= cutoff &&
                (c.EndDate == null || c.EndDate >= start))
            .ToListAsync();


        var contractIds = contracts
            .Select(c => c.Id)
            .ToList();

        var semesterNovelties = await _context.PayrollNovelties
            .AsNoTracking()
            .Where(n =>
                contractIds.Contains(n.ContractId) &&
                n.NoveltyDate >= start &&
                n.NoveltyDate <= cutoff &&
                n.Status != "Anulada")
            .ToListAsync();


        var results = new List<BonusPreviewDetailDto>();
        decimal totalBonus = 0m;

        foreach (var contract in contracts)
        {
            decimal semesterSalary = 0m;
            decimal semesterTransport = 0m;
            int workedDays = 0;

            var employeeNovelties = semesterNovelties
                .Where(n => n.ContractId == contract.Id)
                .ToList();

            // No calcular una prima definitiva con novedades pendientes de aprobación.
            if (employeeNovelties.Any(n =>
                n.Status != "Aprobada"))
            {
                throw new InvalidOperationException(
                    $"El contrato {contract.Id} tiene novedades " +
                    "pendientes de aprobación durante el semestre.");
            }

            // Los conceptos informativos pueden representar
            // ausencias, incapacidades u otras situaciones
            // que requieren reglas específicas.
            /*if (employeeNovelties.Any(n =>
                n.Category == "Informativa"))
            {
                throw new InvalidOperationException(
                    $"El contrato {contract.Id} tiene novedades " +
                    "informativas que deben revisarse antes " +
                    "de calcular la prima.");
            }*/

            var earningsNovelties = employeeNovelties
                .Where(n => n.Category == "Devengado")
                .ToList();

            if (earningsNovelties.Any(n =>
                n.IsSalaryConstitutive == null))
            {
                throw new InvalidOperationException(
                    $"El contrato {contract.Id} contiene " +
                    "devengados sin clasificación salarial.");
            }

            // Evitar categorías desconocidas.
            if (employeeNovelties.Any(n =>
                n.Category != "Devengado" &&
                n.Category != "Deduccion" &&
                n.Category != "Informativa"))
            {
                throw new InvalidOperationException(
                    $"El contrato {contract.Id} contiene " +
                    "una categoría de novedad no válida.");
            }

            if (employeeNovelties.Any(n =>
                n.NoveltyDate < contract.StartDate ||
                (contract.EndDate.HasValue &&
                 n.NoveltyDate > contract.EndDate.Value)))
            {
                throw new InvalidOperationException(
                    $"El contrato {contract.Id} tiene novedades " +
                    "fuera de su vigencia.");
            }

            if (employeeNovelties.Any(n =>
                n.PeriodYear != n.NoveltyDate.Year ||
                n.PeriodMonth != n.NoveltyDate.Month))
            {
                throw new InvalidOperationException(
                    $"El contrato {contract.Id} tiene novedades " +
                    "con un período diferente al de su fecha.");
            }

            if (employeeNovelties.Any(n => n.Amount < 0))
            {
                throw new InvalidOperationException(
                    $"El contrato {contract.Id} tiene novedades " +
                    "con valores negativos.");
            }


            for (int month = start.Month;
                 month <= cutoff.Month;
                 month++)
            {
                var monthStart = new DateOnly(year, month, 1);

                var monthEnd = new DateOnly(
                    year, month,
                    DateTime.DaysInMonth(year, month));

                if (monthEnd > cutoff) { monthEnd = cutoff; }

                var activeStart =
                    contract.StartDate > monthStart
                        ? contract.StartDate
                        : monthStart;

                var activeEnd =
                    contract.EndDate.HasValue &&
                    contract.EndDate.Value < monthEnd
                        ? contract.EndDate.Value
                        : monthEnd;

                if (activeStart > activeEnd)
                    continue;

                int firstDay = PayrollDay(activeStart);
                int lastDay = PayrollDay(activeEnd);

                var monthlyHistory = contract.SalaryHistory
                    .Where(s =>
                        s.EffectiveFrom <= activeEnd &&
                        (s.EffectiveTo == null ||
                         s.EffectiveTo >= activeStart))
                    .ToList();

                // Evita calcular con días sin salario
                // o con dos salarios superpuestos.
                for (int day = firstDay;
                     day <= lastDay;
                     day++)
                {
                    var matching = monthlyHistory
                        .Where(s =>
                            PayrollDay(
                                s.EffectiveFrom < monthStart
                                    ? monthStart
                                    : s.EffectiveFrom) <= day &&
                            PayrollDay(
                                s.EffectiveTo.HasValue &&
                                s.EffectiveTo.Value < monthEnd
                                    ? s.EffectiveTo.Value
                                    : monthEnd) >= day)
                        .ToList();

                    if (matching.Count != 1)
                    {
                        throw new InvalidOperationException(
                            $"El contrato {contract.Id} " +
                            $"no tiene un único salario " +
                            $"para el mes {month}, día {day}.");
                    }

                    decimal salary = matching[0].MonthlySalary;

                    if (salary <= 0)
                        throw new InvalidOperationException(
                            $"Salario inválido en contrato {contract.Id}.");

                    semesterSalary += salary / 30m;

                    if (salary <= transportLimit)
                    {
                        semesterTransport +=
                            transportValue / 30m;
                    }

                    workedDays++;
                }
            }

            if (workedDays == 0)
                continue;


            decimal salaryNovelties = earningsNovelties
                .Where(n => n.IsSalaryConstitutive == true)
                .Sum(n => n.Amount);


            // Equivale a:
            // base mensual promedio * días / 360.

            decimal totalBenefitBase =
                semesterSalary +
                semesterTransport +
                salaryNovelties;

            decimal bonus = RoundMoney(
                totalBenefitBase / 12m
            );


            totalBonus += bonus;


            results.Add(new BonusPreviewDetailDto
            {
                SalaryNovelties = RoundMoney(salaryNovelties),
                ContractId = contract.Id,
                EmployeeId = contract.EmployeeId,

                EmployeeName =
                    $"{contract.Employee.FirstName} " +
                    $"{contract.Employee.LastName}",

                Identification =
                    contract.Employee.Identification,

                WorkedDays = workedDays,

                SalaryAccrued = RoundMoney(semesterSalary),

                TransportAccrued = RoundMoney(semesterTransport),

                EstimatedBonus = bonus
            });
        }


        return new BonusPreviewDto
        {
            Year = year,
            Semester = semester,
            StartDate = start,
            EndDate = cutoff,
            Status = cutoff > today
                ? "ProyeccionFutura"
                : "Proyeccion",

            Message = cutoff > today
                ? "Proyección informativa. Incluye salarios y " +
                  "novedades registradas hasta la fecha proyectada. " +
                  "Los importes futuros pueden cambiar y no " +
                  "representan un pago aprobado."
                : "Prima causada estimada hasta la fecha de corte. " +
                  "No constituye una liquidación aprobada ni un pago.",
            EmployeeCount = results.Count,
            TotalBonus = RoundMoney(totalBonus),
            Details = results
        };

    }
}
