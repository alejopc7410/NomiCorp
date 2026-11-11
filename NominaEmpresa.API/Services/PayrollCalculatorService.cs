
using Microsoft.EntityFrameworkCore;
using NominaEmpresa.API.Data;
using NominaEmpresa.API.Models;
using System;
using System.Threading.Tasks;

namespace NominaEmpresa.API.Services;

public class PayrollCalculationService
{
    private readonly PayrollDbContext _context;

    private const decimal MinimumSalary2026 = 1750905m;

    private const decimal EmployeeHealthRate = 0.04m;
    private const decimal EmployeePensionRate = 0.04m;

    // Tope general del IBC: 25 salarios mínimos.
    private const decimal MaximumIbc2026 =
        MinimumSalary2026 * 25m;

    // A partir de 4 SMMLV debemos contemplar
    // el Fondo de Solidaridad Pensional.
    private const decimal SolidarityThreshold2026 =
        MinimumSalary2026 * 4m;

    private const decimal TransportAllowance2026 = 249095m;

    private const decimal MaximumSalaryForTransport2026 =
        MinimumSalary2026 * 2m;

    private static decimal RoundMoney(decimal amount)
    {
        return Math.Round(
            amount,
            2,
            MidpointRounding.AwayFromZero
        );
    }

    private static int GetPayrollDay(DateOnly date)
    {
        int lastCalendarDay = DateTime.DaysInMonth(
            date.Year,
            date.Month
        );

        if (date.Day == lastCalendarDay)
            return 30;

        return Math.Min(date.Day, 30);
    }

    public PayrollCalculationService(PayrollDbContext context)
    {
        _context = context;
    }


    private static decimal CalculateIbc2026(
        decimal salaryBase,
        int workedDays)
    {
        if (workedDays < 1 || workedDays > 30)
        {
            throw new InvalidOperationException(
                "Los días cotizados deben estar entre 1 y 30.");
        }

        if (salaryBase < 0)
        {
            throw new InvalidOperationException(
                "La base salarial no puede ser negativa.");
        }

        // Mínimo proporcional para un trabajador
        // dependiente ordinario con ingreso o retiro
        // durante el mes.
        decimal minimumIbc =
            (MinimumSalary2026 / 30m) * workedDays;

        decimal maximumIbc = MaximumIbc2026;

        decimal ibc = Math.Max(
            salaryBase,
            minimumIbc
        );

        ibc = Math.Min(ibc, maximumIbc);

        // Redondeo del IBC al peso superior.
        return Math.Ceiling(ibc);
    }


    public async Task<int> CalculatePeriodAsync(int payrollPeriodId)
    {
        var period = await _context.PayrollPeriods
            .FirstOrDefaultAsync(p => p.Id == payrollPeriodId);

        if (period == null)
            throw new InvalidOperationException("El periodo de nómina no existe.");

        if (period.Status != "Abierto")
            throw new InvalidOperationException(
                "Solo se pueden calcular periodos que estén Abiertos.");

        var scheduledBonuses =
            await _context.BonusPayrollAllocations
                .Where(b =>
                    b.PayrollPeriodId == period.Id &&
                    b.Status == "Programada")
                .ToListAsync();

        if (period.Month < 1 || period.Month > 12)
            throw new InvalidOperationException("El mes del periodo no es válido.");

        if (period.StartDate > period.EndDate)
            throw new InvalidOperationException("Las fechas del periodo no son válidas.");

        if (period.Year != 2026)
        {
            throw new InvalidOperationException(
                "Los parámetros de auxilio de transporte " +
                "todavía están configurados únicamente para 2026.");
        }

        await using var transaction =
            await _context.Database.BeginTransactionAsync();

        var existingDetails = await _context.PayrollDetails
        .AnyAsync(d => d.PayrollPeriodId == period.Id);

        if (existingDetails)
        {
            throw new InvalidOperationException(
                "Este periodo ya tiene detalles de nómina. " +
                "No se puede calcular nuevamente sin revisar " +
                "la liquidación existente.");
        }

        var contracts = await _context.Contracts
            .Include(c => c.Employee)
            .Include(c => c.SalaryHistory)
            .Where(c =>
                c.StartDate <= period.EndDate &&
                (c.EndDate == null || c.EndDate >= period.StartDate))
            .ToListAsync();

        // Un empleado no puede tener dos contratos simultáneos
        // dentro de esta primera versión del motor.
        var duplicateEmployees = contracts
            .GroupBy(c => c.EmployeeId)
            .Where(g => g.Count() > 1)
            .ToList();

        if (duplicateEmployees.Any())
        {
            throw new InvalidOperationException(
                "Un empleado tiene varios contratos que se cruzan con el periodo. " +
                "Debemos revisar esos contratos antes de liquidar.");
        }

        var contractIds = contracts.Select(c => c.Id).ToList();
        if (!contracts.Any())
        {
            throw new InvalidOperationException(
                "No existen contratos vigentes durante este " +
                "periodo de nómina.");
        }

        var novelties = await _context.PayrollNovelties
            .Where(n =>
                contractIds.Contains(n.ContractId) &&
                n.PeriodYear == period.Year &&
                n.PeriodMonth == period.Month &&
                n.Status == "Aprobada")
            .ToListAsync();

        foreach (var contract in contracts)
        {
            var employeeStartDay = contract.StartDate <= period.StartDate
                ? 1
                : GetPayrollDay(contract.StartDate);

            var employeeEndDay =
                contract.EndDate == null ||
                contract.EndDate >= period.EndDate
                    ? 30
                    : GetPayrollDay(contract.EndDate.Value);

            var workedDays = employeeEndDay - employeeStartDay + 1;

            if (workedDays <= 0)
                continue;

            var salaryHistories = contract.SalaryHistory
                .Where(s =>
                    s.EffectiveFrom <= period.EndDate &&
                    (s.EffectiveTo == null ||
                     s.EffectiveTo >= period.StartDate))
                .OrderBy(s => s.EffectiveFrom)
                .ToList();

            if (!salaryHistories.Any())
            {
                throw new InvalidOperationException(
                    $"El contrato {contract.Id} no tiene salario vigente " +
                    $"para el periodo {period.Year}-{period.Month}.");
            }

            decimal basicSalary = 0;
            int coveredDays = 0;
            int eligibleTransportDays = 0;
            bool[] coveredPayrollDays = new bool[31];

            foreach (var salary in salaryHistories)
            {
                var salaryStartDay = salary.EffectiveFrom <= period.StartDate
                    ? 1
                    : GetPayrollDay(salary.EffectiveFrom);

                var salaryEndDay =
                    salary.EffectiveTo == null ||
                    salary.EffectiveTo >= period.EndDate
                        ? 30
                        : GetPayrollDay(salary.EffectiveTo.Value);

                var startDay = Math.Max(employeeStartDay, salaryStartDay);
                var endDay = Math.Min(employeeEndDay, salaryEndDay);

                var days = endDay - startDay + 1;

                if (days <= 0)
                    continue;

                if (salary.MonthlySalary <= 0)
                {
                    throw new InvalidOperationException(
                        $"El contrato {contract.Id} tiene un salario inválido.");
                }

                for (int day = startDay; day <= endDay; day++)
                {
                    if (coveredPayrollDays[day])
                    {
                        throw new InvalidOperationException(
                            $"El contrato {contract.Id} tiene " +
                            $"historiales salariales superpuestos " +
                            $"en el día laboral {day}.");
                    }

                    coveredPayrollDays[day] = true;
                }

                basicSalary += (salary.MonthlySalary / 30m) * days;
                coveredDays += days;

                if (salary.MonthlySalary <= MaximumSalaryForTransport2026)
                {
                    eligibleTransportDays += days;
                }
            }

            if (coveredDays != workedDays)
            {
                throw new InvalidOperationException(
                    $"El historial salarial del contrato {contract.Id} " +
                    "no cubre todos los días trabajados o tiene periodos superpuestos.");
            }

            basicSalary = Math.Round(
                basicSalary, 2, MidpointRounding.AwayFromZero);

            var employeeNovelties = novelties
                .Where(n => n.ContractId == contract.Id)
                .ToList();


            var employeeBonus = scheduledBonuses
                .SingleOrDefault(b =>
                    b.EmployeeId == contract.EmployeeId);


            var detail = new PayrollDetail
            {
                PayrollPeriodId = period.Id,
                EmployeeId = contract.EmployeeId,
                ContractId = contract.Id,

                Identification = contract.Employee.Identification,

                EmployeeName =
                    $"{contract.Employee.FirstName} {contract.Employee.LastName}",

                Position = contract.Position,

                MonthlySalary = salaryHistories
                    .Where(s => s.EffectiveFrom <=
                        (contract.EndDate != null &&
                        contract.EndDate < period.EndDate
                        ? contract.EndDate.Value
                        :period.EndDate))
                    .OrderByDescending(s => s.EffectiveFrom)
                    .Select(s => s.MonthlySalary)
                    .FirstOrDefault(),

                TotalEarnings = 0,
                TotalDeductions = 0,
                NetPay = 0
            };

            if (employeeBonus != null)
            {
                if (employeeBonus.Amount <= 0)
                {
                    throw new InvalidOperationException(
                        "La prima programada tiene un valor inválido.");
                }

                detail.Concepts.Add(new PayrollConcept
                {
                    Code = "PRI-SERV",
                    Description = "Prima de servicios",
                    Category = "Devengado",
                    Quantity = 1,
                    UnitValue = employeeBonus.Amount,
                    Amount = employeeBonus.Amount
                });
            }

            detail.Concepts.Add(new PayrollConcept
            {
                Code = "SAL001",
                Description = $"Salario basico ({workedDays} días)",
                Category = "Devengado",
                Quantity = workedDays,
                UnitValue = Math.Round(
                    detail.MonthlySalary / 30m, 2),
                Amount = basicSalary
            });

            decimal transportAllowance = 0m;

            if (eligibleTransportDays > 0)
            {
                transportAllowance = RoundMoney(
                    (TransportAllowance2026 / 30m) *
                    eligibleTransportDays
                );

                detail.Concepts.Add(new PayrollConcept
                {
                    Code = "AUX-TRANS",
                    Description = "Auxilio de transporte",
                    Category = "Devengado",
                    Quantity = eligibleTransportDays,
                    UnitValue = RoundMoney(
                        TransportAllowance2026 / 30m
                    ),
                    Amount = transportAllowance
                });
            }

            foreach (var novelty in employeeNovelties)
            {
                if (novelty.NoveltyDate < period.StartDate ||
                    novelty.NoveltyDate > period.EndDate)
                {
                    throw new InvalidOperationException(
                        $"La novedad {novelty.Id} tiene una fecha " +
                        "fuera del periodo de nómina.");
                }

                if (novelty.NoveltyDate < contract.StartDate ||
                    (contract.EndDate.HasValue &&
                     novelty.NoveltyDate > contract.EndDate.Value))
                {
                    throw new InvalidOperationException(
                        $"La novedad {novelty.Id} está fuera de " +
                        $"la vigencia del contrato {contract.Id}.");
                }

                if (novelty.Amount < 0)
                {
                    throw new InvalidOperationException(
                        $"La novedad {novelty.Id} tiene un importe negativo.");
                }

                if (novelty.Category != "Devengado" &&
                    novelty.Category != "Deduccion" &&
                    novelty.Category != "Informativa")
                {
                    throw new InvalidOperationException(
                        $"La novedad {novelty.Id} tiene una categoría no válida.");
                }

                detail.Concepts.Add(new PayrollConcept
                {
                    Code = $"NOV-{novelty.Id}",
                    Description = novelty.Description,
                    Category = novelty.Category,
                    Quantity = novelty.Quantity,
                    UnitValue = novelty.UnitValue,
                    Amount = Math.Round(
                        novelty.Amount, 2,
                        MidpointRounding.AwayFromZero)
                });
            }

            // Base preliminar de prestaciones:
            // salario básico + auxilio de transporte.
            //
            // Se ampliará para incluir novedades constitutivas
            // de salario, como comisiones y recargos.

            decimal benefitsBase = basicSalary + transportAllowance;

            decimal monthlySeveranceProvision =
                RoundMoney(benefitsBase / 12m);

            decimal monthlyBonusProvision =
                RoundMoney(benefitsBase / 12m);



            // Cesantías causadas durante el periodo.
            detail.Concepts.Add(new PayrollConcept
            {
                Code = "CES-PROV",
                Description = "Provisión estimada de cesantías",
                Category = "Informativa",
                Quantity = workedDays,
                UnitValue = null,
                Amount = monthlySeveranceProvision
            });

            // Prima causada durante el periodo.
            detail.Concepts.Add(new PayrollConcept
            {
                Code = "PRI-PROV",
                Description = "Provisión estimada de prima de servicios",
                Category = "Informativa",
                Quantity = workedDays,
                UnitValue = null,
                Amount = monthlyBonusProvision
            });


            //
            // SEGURIDAD SOCIAL DEL EMPLEADO - 2026
            //

            // En esta versión asumimos que las novedades
            // de categoría Devengado son salariales.
            // El auxilio de transporte no se incluye.

            decimal salaryBasedNovelties = employeeNovelties
                .Where(n => n.Category == "Devengado")
                .Sum(n => n.Amount);

            // Salario básico + novedades salariales.
            // Nunca agregamos transportAllowance al IBC.
            decimal salaryBaseForIbc =
                basicSalary + salaryBasedNovelties;

            decimal ibc = CalculateIbc2026(
                salaryBaseForIbc,
                workedDays
            );

            // Evitamos omitir el aporte adicional al
            // Fondo de Solidaridad Pensional.
            if (ibc >= SolidarityThreshold2026)
            {
                throw new InvalidOperationException(
                    $"El empleado {detail.EmployeeName} " +
                    "requiere cálculo del Fondo de " +
                    "Solidaridad Pensional. " +
                    "Este caso todavía no está implementado.");
            }

            // Aportes a cargo del trabajador.
            decimal employeeHealth = RoundMoney(
                ibc * EmployeeHealthRate
            );

            decimal employeePension = RoundMoney(
                ibc * EmployeePensionRate
            );

            // Concepto de salud.
            detail.Concepts.Add(new PayrollConcept
            {
                Code = "SS-SALUD",
                Description = "Aporte a salud - empleado (4%)",
                Category = "Deduccion",
                Quantity = 1,
                UnitValue = employeeHealth,
                Amount = employeeHealth
            });

            // Concepto de pensión.
            detail.Concepts.Add(new PayrollConcept
            {
                Code = "SS-PENSION",
                Description = "Aporte a pensión - empleado (4%)",
                Category = "Deduccion",
                Quantity = 1,
                UnitValue = employeePension,
                Amount = employeePension
            });


            detail.TotalEarnings = detail.Concepts
                .Where(c => c.Category == "Devengado")
                .Sum(c => c.Amount);

            detail.TotalDeductions = detail.Concepts
                .Where(c => c.Category == "Deduccion")
                .Sum(c => c.Amount);

            detail.NetPay = detail.TotalEarnings - detail.TotalDeductions;
            if (detail.NetPay < 0)
            {
                throw new InvalidOperationException(
                    $"Las deducciones del empleado " +
                    $"{detail.EmployeeName} superan sus devengados. " +
                    "Revise las novedades antes de calcular la nómina.");
            }

            _context.PayrollDetails.Add(detail);
        }

        var calculatedEmployeeIds = _context.ChangeTracker
            .Entries<PayrollDetail>()
            .Where(e =>
                e.State == EntityState.Added &&
                e.Entity.PayrollPeriodId == period.Id)
            .Select(e => e.Entity.EmployeeId)
            .ToHashSet();

        if (scheduledBonuses.Any(b =>
            !calculatedEmployeeIds.Contains(b.EmployeeId)))
        {
            throw new InvalidOperationException(
                "Una prima programada no pudo incorporarse " +
                "al detalle de nómina correspondiente.");
        }

        period.Status = "Calculado";

        foreach (var bonus in scheduledBonuses)
        {
            bonus.Status = "Incorporada";
            bonus.IncludedAtUtc = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        return contracts.Count;
    }
}
