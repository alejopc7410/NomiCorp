
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using NominaEmpresa.API.Models;

namespace NominaEmpresa.API.Data;

public class PayrollDbContext : IdentityDbContext<ApplicationUser>
{
    public PayrollDbContext(
        DbContextOptions<PayrollDbContext> options)
        : base(options)
    {
    }

    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<Contract> Contracts => Set<Contract>();
    public DbSet<SalaryHistory> SalaryHistories => Set<SalaryHistory>();
    public DbSet<PayrollNovelty> PayrollNovelties => Set<PayrollNovelty>();
    public DbSet<PayrollPeriod> PayrollPeriods => Set<PayrollPeriod>();
    public DbSet<PayrollDetail> PayrollDetails => Set<PayrollDetail>();
    public DbSet<PayrollConcept> PayrollConcepts => Set<PayrollConcept>();
    public DbSet<BonusSettlement> BonusSettlements => Set<BonusSettlement>();
    public DbSet<BonusSettlementDetail> BonusSettlementDetails => Set<BonusSettlementDetail>();
    public DbSet<BonusPreviousPayment> BonusPreviousPayments => Set<BonusPreviousPayment>();
    public DbSet<BonusPayrollAllocation> BonusPayrollAllocations => Set<BonusPayrollAllocation>();
    public DbSet<PayslipIssue> PayslipIssues => Set<PayslipIssue>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Importante: configura primero las tablas de Identity.
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<PayslipIssue>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.HasIndex(x => x.PayrollDetailId);

            entity.HasOne<PayrollDetail>()
                .WithMany()
                .HasForeignKey(x => x.PayrollDetailId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<BonusPayrollAllocation>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.HasIndex(x => x.BonusSettlementDetailId)
                .IsUnique();

            entity.HasIndex(x => new
            {
                x.PayrollPeriodId,
                x.EmployeeId
            }).IsUnique();

            entity.Property(x => x.Amount)
                .HasPrecision(18, 2);

            entity.Property(x => x.Status)
                .HasMaxLength(30)
                .IsRequired();

            entity.HasOne<BonusSettlementDetail>()
                .WithMany()
                .HasForeignKey(x => x.BonusSettlementDetailId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne<PayrollPeriod>()
                .WithMany()
                .HasForeignKey(x => x.PayrollPeriodId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<BonusPreviousPayment>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Amount)
                .HasPrecision(18, 2);

            entity.Property(x => x.Reference)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(x => x.Notes)
                .HasMaxLength(500);

            entity.HasOne<BonusSettlement>()
                .WithMany()
                .HasForeignKey(x => x.BonusSettlementId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(x => new
            {
                x.BonusSettlementId,
                x.EmployeeId,
                x.Reference
            }).IsUnique();
        });


        modelBuilder.Entity<BonusSettlement>(entity =>
        {
            entity.HasIndex(x => new
            {
                x.Year,
                x.Semester
            }).IsUnique();

            entity.Property(x => x.TotalAmount)
                .HasPrecision(18, 2);

            entity.HasMany(x => x.Details)
                .WithOne(x => x.BonusSettlement)
                .HasForeignKey(x => x.BonusSettlementId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<BonusSettlementDetail>(entity =>
        {
            entity.HasIndex(x => new
            {
                x.BonusSettlementId,
                x.ContractId
            }).IsUnique();

            entity.Property(x => x.SalaryNovelties)
                .HasPrecision(18, 2);

            entity.Property(x => x.SalaryAccrued)
                .HasPrecision(18, 2);

            entity.Property(x => x.TransportAccrued)
                .HasPrecision(18, 2);

            entity.Property(x => x.BonusAmount)
                .HasPrecision(18, 2);
        });


        modelBuilder.Entity<PayrollPeriod>(entity =>
        {
            entity.HasIndex(p => new { p.Year, p.Month })
                .IsUnique();

            entity.Property(p => p.Status)
                .HasMaxLength(20);
        });

        modelBuilder.Entity<PayrollDetail>(entity =>
        {
            entity.HasIndex(p => new
            {
                p.PayrollPeriodId,
                p.EmployeeId
            }).IsUnique();

            entity.Property(p => p.MonthlySalary)
                .HasPrecision(18, 2);

            entity.Property(p => p.TotalEarnings)
                .HasPrecision(18, 2);

            entity.Property(p => p.TotalDeductions)
                .HasPrecision(18, 2);

            entity.Property(p => p.NetPay)
                .HasPrecision(18, 2);

            entity.HasOne(p => p.PayrollPeriod)
                .WithMany(p => p.Details)
                .HasForeignKey(p => p.PayrollPeriodId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(p => p.Employee)
                .WithMany()
                .HasForeignKey(p => p.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(p => p.Contract)
                .WithMany()
                .HasForeignKey(p => p.ContractId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PayrollConcept>(entity =>
        {
            entity.Property(p => p.Amount)
                .HasPrecision(18, 2);

            entity.Property(p => p.Quantity)
                .HasPrecision(18, 4);

            entity.Property(p => p.UnitValue)
                .HasPrecision(18, 2);

            entity.HasOne(p => p.PayrollDetail)
                .WithMany(p => p.Concepts)
                .HasForeignKey(p => p.PayrollDetailId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Employee>(entity =>
        {
            entity.HasIndex(e => e.Identification)
                .IsUnique();

            entity.Property(e => e.Identification)
                .IsRequired()
                .HasMaxLength(30);

            entity.Property(e => e.FirstName)
                .IsRequired()
                .HasMaxLength(80);

            entity.Property(e => e.LastName)
                .IsRequired()
                .HasMaxLength(80);
        });

        modelBuilder.Entity<Contract>(entity =>
        {
            entity.Property(c => c.ContractType)
                .HasMaxLength(30)
                .IsRequired();

            entity.Property(c => c.Position)
                .HasMaxLength(100)
                .IsRequired();

            entity.HasOne(c => c.Employee)
                .WithMany()
                .HasForeignKey(c => c.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(c => new
            {
                c.EmployeeId,
                c.StartDate
            });
        });

        modelBuilder.Entity<SalaryHistory>(entity =>
        {
            entity.Property(s => s.MonthlySalary)
                .HasPrecision(18, 2);

            entity.Property(s => s.Reason)
                .HasMaxLength(250);

            entity.HasOne(s => s.Contract)
                .WithMany(c => c.SalaryHistory)
                .HasForeignKey(s => s.ContractId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(s => new
            {
                s.ContractId,
                s.EffectiveFrom
            }).IsUnique();
        });

        modelBuilder.Entity<PayrollNovelty>(entity =>
        {
            entity.Property(n => n.NoveltyType)
                .HasMaxLength(40)
                .IsRequired();

            entity.Property(n => n.Category)
                .HasMaxLength(20)
                .IsRequired();

            entity.Property(n => n.Description)
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(n => n.Amount)
                .HasPrecision(18, 2);

            entity.Property(n => n.Quantity)
                .HasPrecision(18, 4);

            entity.Property(n => n.UnitValue)
                .HasPrecision(18, 2);

            entity.Property(n => n.Status)
                .HasMaxLength(20)
                .IsRequired();

            entity.HasOne(n => n.Contract)
                .WithMany()
                .HasForeignKey(n => n.ContractId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(n => new
            {
                n.ContractId,
                n.PeriodYear,
                n.PeriodMonth
            });
        });

        // Relación entre una cuenta de usuario y un empleado.
        // Un administrador puede tener EmployeeId = null.
        modelBuilder.Entity<ApplicationUser>(entity =>
        {
            entity.HasOne(u => u.Employee)
                .WithOne()
                .HasForeignKey<ApplicationUser>(u => u.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            // Evita que dos cuentas estén asociadas al mismo empleado.
            // Permite múltiples usuarios administradores sin empleado asociado.
            entity.HasIndex(u => u.EmployeeId)
                .IsUnique()
                .HasFilter("[EmployeeId] IS NOT NULL");
        });
    }
}


/*

Before implementing authentication and authorization
 
using Microsoft.EntityFrameworkCore;
using NominaEmpresa.API.Models;

namespace NominaEmpresa.API.Data;

public class PayrollDbContext : DbContext
{
    public PayrollDbContext(
        DbContextOptions<PayrollDbContext> options)
        : base(options)
    {
    }

    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<Contract> Contracts => Set<Contract>();
    public DbSet<SalaryHistory> SalaryHistories => Set<SalaryHistory>();
    public DbSet<PayrollNovelty> PayrollNovelties => Set<PayrollNovelty>();

    protected override void OnModelCreating(
        ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Employee>(entity =>
        {
            entity.HasIndex(e => e.Identification)
                .IsUnique();

            entity.Property(e => e.Identification)
                .IsRequired()
                .HasMaxLength(30);

            entity.Property(e => e.FirstName)
                .IsRequired()
                .HasMaxLength(80);

            entity.Property(e => e.LastName)
                .IsRequired()
                .HasMaxLength(80);
        });

        modelBuilder.Entity<Contract>(entity =>
        {
            entity.Property(c => c.ContractType)
                .HasMaxLength(30)
                .IsRequired();

            entity.Property(c => c.Position)
                .HasMaxLength(100)
                .IsRequired();

            entity.HasOne(c => c.Employee)
                .WithMany()
                .HasForeignKey(c => c.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(c => new
            {
                c.EmployeeId,
                c.StartDate
            });
        });

        modelBuilder.Entity<SalaryHistory>(entity =>
        {
            entity.Property(s => s.MonthlySalary)
                .HasPrecision(18, 2);

            entity.Property(s => s.Reason)
                .HasMaxLength(250);

            entity.HasOne(s => s.Contract)
                .WithMany(c => c.SalaryHistory)
                .HasForeignKey(s => s.ContractId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(s => new
            {
                s.ContractId,
                s.EffectiveFrom
            }).IsUnique();
        });

        modelBuilder.Entity<PayrollNovelty>(entity =>
        {
            entity.Property(n => n.NoveltyType)
                .HasMaxLength(40)
                .IsRequired();

            entity.Property(n => n.Category)
                .HasMaxLength(20)
                .IsRequired();

            entity.Property(n => n.Description)
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(n => n.Amount)
                .HasPrecision(18, 2);

            entity.Property(n => n.Quantity)
                .HasPrecision(18, 4);

            entity.Property(n => n.UnitValue)
                .HasPrecision(18, 2);

            entity.Property(n => n.Status)
                .HasMaxLength(20)
                .IsRequired();

            entity.HasOne(n => n.Contract)
                .WithMany()
                .HasForeignKey(n => n.ContractId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(n => new
            {
                n.ContractId,
                n.PeriodYear,
                n.PeriodMonth
            });
        });
    }
}*/