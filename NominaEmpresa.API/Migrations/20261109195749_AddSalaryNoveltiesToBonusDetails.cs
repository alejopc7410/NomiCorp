using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NominaEmpresa.API.Migrations
{
    /// <inheritdoc />
    public partial class AddSalaryNoveltiesToBonusDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "SalaryNovelties",
                table: "BonusSettlementDetails",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SalaryNovelties",
                table: "BonusSettlementDetails");
        }
    }
}
