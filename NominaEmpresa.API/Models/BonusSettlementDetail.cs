using System.ComponentModel.DataAnnotations;

namespace NominaEmpresa.API.Models;

public class BonusSettlementDetail
{
    public int Id { get; set; }

    public int BonusSettlementId { get; set; }

    public BonusSettlement BonusSettlement { get; set; } = null!;

    public int EmployeeId { get; set; }

    public int ContractId { get; set; }

    public decimal SalaryNovelties { get; set; }

    [Required, MaxLength(30)]
    public string Identification { get; set; } = string.Empty;

    [Required, MaxLength(180)]
    public string EmployeeName { get; set; } = string.Empty;

    public int WorkedDays { get; set; }

    public decimal SalaryAccrued { get; set; }

    public decimal TransportAccrued { get; set; }

    public decimal BonusAmount { get; set; }
}