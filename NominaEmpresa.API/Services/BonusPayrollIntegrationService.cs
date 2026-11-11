
using Microsoft.EntityFrameworkCore;
using NominaEmpresa.API.Data;
using NominaEmpresa.API.Models;
using System;
using System.Threading.Tasks;

namespace NominaEmpresa.API.Services;

public class BonusPayrollIntegrationService
{
    private readonly PayrollDbContext _context;

    public BonusPayrollIntegrationService(
        PayrollDbContext context)
    {
        _context = context;
    }

    public async Task<int> ScheduleAsync(
        int settlementId,
        int payrollPeriodId)
    {
        await using var transaction =
            await _context.Database.BeginTransactionAsync();

        var settlement = await _context.BonusSettlements
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.Id == settlementId);

        if (settlement == null)
            throw new InvalidOperationException(
                "La liquidación de prima no existe.");

        if (settlement.Status != "Aprobada")
            throw new InvalidOperationException(
                "La prima debe estar aprobada antes " +
                "de incorporarla a la nómina.");

        var period = await _context.PayrollPeriods
            .FirstOrDefaultAsync(p => p.Id == payrollPeriodId);

        if (period == null)
            throw new InvalidOperationException(
                "El período de nómina no existe.");

        if (period.Status != "Abierto")
            throw new InvalidOperationException(
                "Solo se puede programar la prima " +
                "en una nómina abierta.");

        if (period.Year != settlement.Year ||
            period.Month !=
                (settlement.Semester == 1 ? 6 : 12))
        {
            throw new InvalidOperationException(
                "Selecciona la nómina de junio o diciembre " +
                "correspondiente al semestre.");
        }

        var employeeIds = settlement.Details
            .Select(x => x.EmployeeId)
            .ToList();

        if (employeeIds.Count != employeeIds.Distinct().Count())
        {
            throw new InvalidOperationException(
                "Existen empleados duplicados en la prima.");
        }

        var previousPayments =
            await _context.BonusPreviousPayments
                .Where(x =>
                    x.BonusSettlementId == settlementId)
                .ToListAsync();

        var existing = await _context.BonusPayrollAllocations
            .Where(x =>
                employeeIds.Contains(x.EmployeeId) &&
                (x.PayrollPeriodId == payrollPeriodId ||
                 settlement.Details.Select(d => d.Id)
                     .Contains(x.BonusSettlementDetailId)))
            .ToListAsync();

        if (existing.Any())
        {
            throw new InvalidOperationException(
                "Una o más primas ya están programadas " +
                "en nómina.");
        }

        int count = 0;

        foreach (var detail in settlement.Details)
        {
            decimal paid = previousPayments
                .Where(x => x.EmployeeId == detail.EmployeeId)
                .Sum(x => x.Amount);

            decimal balance = detail.BonusAmount - paid;

            if (balance < 0)
            {
                throw new InvalidOperationException(
                    $"Los pagos anteriores de " +
                    $"{detail.EmployeeName} superan su prima.");
            }

            if (balance == 0)
                continue;

            // En esta primera integración, el empleado
            // debe tener un contrato vigente en el
            // período de nómina seleccionado.
            bool hasContract = await _context.Contracts
                .AnyAsync(c =>
                    c.EmployeeId == detail.EmployeeId &&
                    c.StartDate <= period.EndDate &&
                    (c.EndDate == null ||
                     c.EndDate >= period.StartDate));

            if (!hasContract)
            {
                throw new InvalidOperationException(
                    $"El empleado {detail.EmployeeName} " +
                    "no tiene contrato vigente en la nómina. " +
                    "Su prima requiere pago separado.");
            }

            _context.BonusPayrollAllocations.Add(
                new BonusPayrollAllocation
                {
                    BonusSettlementDetailId = detail.Id,
                    PayrollPeriodId = period.Id,
                    EmployeeId = detail.EmployeeId,
                    Amount = balance,
                    Status = "Programada"
                });

            count++;
        }

        if (count == 0)
        {
            throw new InvalidOperationException(
                "No hay saldos pendientes de prima " +
                "para incorporar a nómina.");
        }

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        return count;
    }
}
