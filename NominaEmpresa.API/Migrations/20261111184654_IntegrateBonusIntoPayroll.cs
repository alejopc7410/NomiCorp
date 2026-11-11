using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NominaEmpresa.API.Migrations
{
    /// <inheritdoc />
    public partial class IntegrateBonusIntoPayroll : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BonusPayrollAllocations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BonusSettlementDetailId = table.Column<int>(type: "int", nullable: false),
                    PayrollPeriodId = table.Column<int>(type: "int", nullable: false),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IncludedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BonusPayrollAllocations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BonusPayrollAllocations_BonusSettlementDetails_BonusSettlementDetailId",
                        column: x => x.BonusSettlementDetailId,
                        principalTable: "BonusSettlementDetails",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BonusPayrollAllocations_PayrollPeriods_PayrollPeriodId",
                        column: x => x.PayrollPeriodId,
                        principalTable: "PayrollPeriods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PayslipIssues",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PayrollDetailId = table.Column<int>(type: "int", nullable: false),
                    IssuedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IssuedByUserId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IncludesBonus = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayslipIssues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PayslipIssues_PayrollDetails_PayrollDetailId",
                        column: x => x.PayrollDetailId,
                        principalTable: "PayrollDetails",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BonusPayrollAllocations_BonusSettlementDetailId",
                table: "BonusPayrollAllocations",
                column: "BonusSettlementDetailId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BonusPayrollAllocations_PayrollPeriodId_EmployeeId",
                table: "BonusPayrollAllocations",
                columns: new[] { "PayrollPeriodId", "EmployeeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PayslipIssues_PayrollDetailId",
                table: "PayslipIssues",
                column: "PayrollDetailId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BonusPayrollAllocations");

            migrationBuilder.DropTable(
                name: "PayslipIssues");
        }
    }
}
