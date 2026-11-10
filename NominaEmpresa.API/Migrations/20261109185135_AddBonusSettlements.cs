using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NominaEmpresa.API.Migrations
{
    /// <inheritdoc />
    public partial class AddBonusSettlements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BonusSettlements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Year = table.Column<int>(type: "int", nullable: false),
                    Semester = table.Column<int>(type: "int", nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BonusSettlements", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BonusSettlementDetails",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BonusSettlementId = table.Column<int>(type: "int", nullable: false),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    ContractId = table.Column<int>(type: "int", nullable: false),
                    Identification = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    EmployeeName = table.Column<string>(type: "nvarchar(180)", maxLength: 180, nullable: false),
                    WorkedDays = table.Column<int>(type: "int", nullable: false),
                    SalaryAccrued = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TransportAccrued = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    BonusAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BonusSettlementDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BonusSettlementDetails_BonusSettlements_BonusSettlementId",
                        column: x => x.BonusSettlementId,
                        principalTable: "BonusSettlements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BonusSettlementDetails_BonusSettlementId_ContractId",
                table: "BonusSettlementDetails",
                columns: new[] { "BonusSettlementId", "ContractId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BonusSettlements_Year_Semester",
                table: "BonusSettlements",
                columns: new[] { "Year", "Semester" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BonusSettlementDetails");

            migrationBuilder.DropTable(
                name: "BonusSettlements");
        }
    }
}
