using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NominaEmpresa.API.Migrations
{
    /// <inheritdoc />
    public partial class AddBonusPreviousPayments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BonusPreviousPayments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BonusSettlementId = table.Column<int>(type: "int", nullable: false),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PaymentDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Reference = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BonusPreviousPayments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BonusPreviousPayments_BonusSettlements_BonusSettlementId",
                        column: x => x.BonusSettlementId,
                        principalTable: "BonusSettlements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BonusPreviousPayments_BonusSettlementId_EmployeeId_Reference",
                table: "BonusPreviousPayments",
                columns: new[] { "BonusSettlementId", "EmployeeId", "Reference" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BonusPreviousPayments");
        }
    }
}
