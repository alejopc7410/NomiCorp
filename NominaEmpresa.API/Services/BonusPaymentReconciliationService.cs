
using Microsoft.EntityFrameworkCore;
using NominaEmpresa.API.Data;
using NominaEmpresa.API.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NominaEmpresa.API.Services;

public class BonusPaymentReconciliationService
{
    private readonly PayrollDbContext _context;

    public BonusPaymentReconciliationService(
        PayrollDbContext context)
    {
        _context = context;
    }

    private static DateOnly ColombiaToday()
    {
        var zone = TimeZoneInfo.FindSystemTimeZoneById(
            "America/Bogota");

        return DateOnly.FromDateTime(
            TimeZoneInfo.ConvertTimeFromUtc(
                DateTime.UtcNow, zone));
    }

    public async Task<object> GetSummaryAsync(
        int settlementId)
    {
        var settlement = await _context.BonusSettlements
            .AsNoTracking()
            .Include(s => s.Details)
            .FirstOrDefaultAsync(s => s.Id == settlementId);

        if (settlement == null)
        {
            throw new KeyNotFoundException(
                "La liquidación no existe.");
        }

        var payments = await _context.BonusPreviousPayments
            .AsNoTracking()
            .Where(p =>
                p.BonusSettlementId == settlementId)
            .OrderByDescending(p => p.PaymentDate)
            .ThenByDescending(p => p.Id)
            .ToListAsync();

        var employees = settlement.Details
            .GroupBy(d => new
            {
                d.EmployeeId,
                d.EmployeeName
            })
            .Select(group =>
            {
                decimal calculated = group
                    .Sum(d => d.BonusAmount);

                decimal previouslyPaid = payments
                    .Where(p =>
                        p.EmployeeId == group.Key.EmployeeId)
                    .Sum(p => p.Amount);

                return new
                {
                    group.Key.EmployeeId,
                    group.Key.EmployeeName,
                    CalculatedBonus = calculated,
                    PreviouslyPaid = previouslyPaid,
                    PendingBalance =
                        calculated - previouslyPaid
                };
            })
            .OrderBy(e => e.EmployeeName)
            .ToList();

        return new
        {
            settlement.Id,
            settlement.Year,
            settlement.Semester,
            settlement.Status,
            TotalCalculated = employees.Sum(
                e => e.CalculatedBonus),
            TotalPreviouslyPaid = employees.Sum(
                e => e.PreviouslyPaid),
            TotalPending = employees.Sum(
                e => e.PendingBalance),
            Employees = employees,
            Payments = payments.Select(p => new
            {
                p.Id,
                p.EmployeeId,
                p.Amount,
                p.PaymentDate,
                p.Reference,
                p.Notes
            })
        };
    }

    public async Task<BonusPreviousPayment> RegisterAsync(
        int settlementId,
        int employeeId,
        decimal amount,
        DateOnly paymentDate,
        string reference,
        string? notes)
    {
        reference = reference?.Trim() ?? "";

        if (amount <= 0)
            throw new InvalidOperationException(
                "El valor debe ser mayor que cero.");

        if (string.IsNullOrWhiteSpace(reference) ||
            reference.Length > 100)
            throw new InvalidOperationException(
                "La referencia es obligatoria " +
                "y no puede superar 100 caracteres.");

        if (notes?.Length > 500)
            throw new InvalidOperationException(
                "Las observaciones superan 500 caracteres.");

        if (paymentDate == default ||
            paymentDate > ColombiaToday())
            throw new InvalidOperationException(
                "La fecha debe corresponder " +
                "a un pago que ya ocurrió.");

        var settlement = await _context.BonusSettlements
            .Include(s => s.Details)
            .FirstOrDefaultAsync(
                s => s.Id == settlementId);

        if (settlement == null)
            throw new KeyNotFoundException(
                "La liquidación no existe.");

        if (settlement.Status != "Borrador")
            throw new InvalidOperationException(
                "Solo se permiten registros de " +
                "conciliación en borradores.");

        if (paymentDate < settlement.StartDate ||
            paymentDate > ColombiaToday())
            throw new InvalidOperationException(
                "La fecha del pago no corresponde " +
                "al período permitido.");

        var employeeDetails = settlement.Details
            .Where(d => d.EmployeeId == employeeId)
            .ToList();

        if (employeeDetails.Count == 0)
            throw new InvalidOperationException(
                "El empleado no pertenece " +
                "a esta liquidación.");

        bool duplicate = await _context.BonusPreviousPayments
            .AnyAsync(p =>
                p.BonusSettlementId == settlementId &&
                p.EmployeeId == employeeId &&
                p.Reference == reference);

        if (duplicate)
            throw new InvalidOperationException(
                "Ya existe un pago con esa referencia " +
                "para este empleado.");

        decimal calculatedBonus = employeeDetails
            .Sum(d => d.BonusAmount);

        decimal previouslyPaid =
            await _context.BonusPreviousPayments
                .Where(p =>
                    p.BonusSettlementId == settlementId &&
                    p.EmployeeId == employeeId)
                .SumAsync(p => (decimal?)p.Amount) ?? 0m;

        if (previouslyPaid + amount > calculatedBonus)
            throw new InvalidOperationException(
                "Los pagos registrados superarían " +
                "la prima estimada. Revisa el cálculo " +
                "y los comprobantes antes de continuar.");

        var payment = new BonusPreviousPayment
        {
            BonusSettlementId = settlementId,
            EmployeeId = employeeId,
            Amount = amount,
            PaymentDate = paymentDate,
            Reference = reference,
            Notes = notes?.Trim()
        };

        _context.BonusPreviousPayments.Add(payment);

        await _context.SaveChangesAsync();

        return payment;
    }
}
