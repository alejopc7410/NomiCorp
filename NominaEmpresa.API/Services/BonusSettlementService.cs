
using Microsoft.EntityFrameworkCore;
using NominaEmpresa.API.Data;
using NominaEmpresa.API.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NominaEmpresa.API.Services;

public class BonusSettlementService
{
    private readonly PayrollDbContext _context;
    private readonly BonusCalculationService _calculator;

    public BonusSettlementService(
        PayrollDbContext context,
        BonusCalculationService calculator)
    {
        _context = context;
        _calculator = calculator;
    }

    public async Task<List<object>> GetAllAsync()
    {
        var settlements = await _context.BonusSettlements
            .AsNoTracking()
            .Include(s => s.Details)
            .OrderByDescending(s => s.Year)
            .ThenByDescending(s => s.Semester)
            .ToListAsync();

        return settlements.Select(s => (object)new
        {
            s.Id,
            s.Year,
            s.Semester,
            s.Status,
            s.TotalAmount,
            s.CreatedAtUtc,
            EmployeeCount = s.Details.Count
        }).ToList();
    }


    public async Task<object?> GetByIdAsync(int id)
    {
        return await _context.BonusSettlements
            .AsNoTracking()
            .Where(s => s.Id == id)
            .Select(s => new
            {
                s.Id,
                s.Year,
                s.Semester,
                s.StartDate,
                s.EndDate,
                s.Status,
                s.TotalAmount,
                s.CreatedAtUtc,

                Details = s.Details
                    .OrderBy(d => d.EmployeeName)
                    .Select(d => new
                    {
                        d.Id,
                        d.EmployeeId,
                        d.ContractId,
                        d.Identification,
                        d.EmployeeName,
                        d.WorkedDays,
                        d.SalaryAccrued,
                        d.TransportAccrued,
                        d.SalaryNovelties,
                        d.BonusAmount
                    })
                    .ToList()
            })
            .FirstOrDefaultAsync();
    }


    public async Task<BonusSettlement> CreateDraftAsync(int year, int semester, DateOnly? cutoffDate = null)
    {
        // Evitar generar un segundo registro
        // para el mismo semestre.
        bool exists = await _context.BonusSettlements
            .AnyAsync(s =>
                s.Year == year &&
                s.Semester == semester);    

        if (exists)
        {
            throw new InvalidOperationException(
                "Ya existe una liquidación de prima " +
                "para este año y semestre.");
        }

        var preview = await _calculator.PreviewAsync(year, semester, cutoffDate);

        if (preview.Details.Count == 0)
        {
            throw new InvalidOperationException(
                "No hay empleados con prima calculable " +
                "para este semestre.");
        }

        // Impedir que un empleado aparezca más
        // de una vez mientras no exista la lógica
        // de consolidación de contratos.
        var duplicates = preview.Details
            .GroupBy(d => d.EmployeeId)
            .Where(g => g.Count() > 1)
            .ToList();

        if (duplicates.Any())
        {
            throw new InvalidOperationException(
                "Hay empleados con varios contratos " +
                "en el semestre. Deben consolidarse " +
                "antes de guardar la liquidación.");
        }

        var settlement = new BonusSettlement
        {
            Year = year,
            Semester = semester,
            StartDate = preview.StartDate,
            EndDate = preview.EndDate,
            Status = "Borrador",
            TotalAmount = preview.TotalBonus
        };

        foreach (var item in preview.Details)
        {
            settlement.Details.Add(
                new BonusSettlementDetail
                {
                    EmployeeId = item.EmployeeId,
                    ContractId = item.ContractId,
                    SalaryNovelties = item.SalaryNovelties,

                    Identification =
                        item.Identification,

                    EmployeeName =
                        item.EmployeeName,

                    WorkedDays = item.WorkedDays,

                    SalaryAccrued =
                        item.SalaryAccrued,

                    TransportAccrued =
                        item.TransportAccrued,

                    BonusAmount =
                        item.EstimatedBonus
                });
        }

        _context.BonusSettlements.Add(settlement);

        await _context.SaveChangesAsync();

        return settlement;
    }



    public async Task<BonusSettlement> RecalculateDraftAsync(int id, DateOnly? newCutoffDate = null)
    {
        await using var transaction =
            await _context.Database.BeginTransactionAsync();

        var settlement = await _context.BonusSettlements
            .Include(s => s.Details)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (settlement == null)
        {
            throw new KeyNotFoundException(
                "La liquidación de prima no existe.");
        }

        if (settlement.Status != "Borrador")
        {
            throw new InvalidOperationException(
                "Solo se pueden modificar liquidaciones en estado Borrador.");
        }

        // Utilizar la nueva fecha o conservar la anterior.
        DateOnly cutoff =
            newCutoffDate ?? settlement.EndDate;

        // Calcular nuevamente con la fecha seleccionada.
        // BonusCalculationService valida que la fecha
        // pertenezca al semestre y no sea futura.
        var preview = await _calculator.PreviewAsync(
            settlement.Year,
            settlement.Semester,
            cutoff
        );

        if (!preview.Details.Any())
        {
            throw new InvalidOperationException(
                "No hay empleados con prima calculable " +
                "hasta la fecha seleccionada.");
        }

        var duplicateEmployees = preview.Details
            .GroupBy(d => d.EmployeeId)
            .Where(g => g.Count() > 1)
            .ToList();

        if (duplicateEmployees.Any())
        {
            throw new InvalidOperationException(
                "Existen empleados con múltiples contratos " +
                "en el semestre. Deben consolidarse antes " +
                "de guardar la liquidación.");
        }

        // Eliminar los detalles anteriores.
        _context.BonusSettlementDetails.RemoveRange(
            settlement.Details
        );

        settlement.Details.Clear();

        // Crear los nuevos detalles.
        foreach (var item in preview.Details)
        {
            settlement.Details.Add(
                new BonusSettlementDetail
                {
                    EmployeeId = item.EmployeeId,
                    ContractId = item.ContractId,

                    Identification = item.Identification,
                    EmployeeName = item.EmployeeName,

                    WorkedDays = item.WorkedDays,

                    SalaryAccrued = item.SalaryAccrued,
                    TransportAccrued = item.TransportAccrued,
                    SalaryNovelties = item.SalaryNovelties,

                    BonusAmount = item.EstimatedBonus
                });
        }

        // Guardar la nueva fecha de corte.
        settlement.EndDate = preview.EndDate;

        settlement.TotalAmount = settlement.Details
            .Sum(d => d.BonusAmount);

        await _context.SaveChangesAsync();

        await transaction.CommitAsync();

        return settlement;
    }


}
