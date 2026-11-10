using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NominaEmpresa.API.Migrations
{
    /// <inheritdoc />
    public partial class AddPayrollProcesing2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PayrollDetails_Contracts_ContractId",
                table: "PayrollDetails");

            migrationBuilder.DropForeignKey(
                name: "FK_PayrollDetails_Employees_EmployeeId",
                table: "PayrollDetails");

            migrationBuilder.DropForeignKey(
                name: "FK_PayrollDetails_PayrollPeriods_PayrollPeriodId",
                table: "PayrollDetails");

            migrationBuilder.DropIndex(
                name: "IX_PayrollDetails_PayrollPeriodId",
                table: "PayrollDetails");

            migrationBuilder.AlterColumn<decimal>(
                name: "Quantity",
                table: "PayrollConcepts",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PayrollPeriods_Year_Month",
                table: "PayrollPeriods",
                columns: new[] { "Year", "Month" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PayrollDetails_PayrollPeriodId_EmployeeId",
                table: "PayrollDetails",
                columns: new[] { "PayrollPeriodId", "EmployeeId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_PayrollDetails_Contracts_ContractId",
                table: "PayrollDetails",
                column: "ContractId",
                principalTable: "Contracts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PayrollDetails_Employees_EmployeeId",
                table: "PayrollDetails",
                column: "EmployeeId",
                principalTable: "Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PayrollDetails_PayrollPeriods_PayrollPeriodId",
                table: "PayrollDetails",
                column: "PayrollPeriodId",
                principalTable: "PayrollPeriods",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PayrollDetails_Contracts_ContractId",
                table: "PayrollDetails");

            migrationBuilder.DropForeignKey(
                name: "FK_PayrollDetails_Employees_EmployeeId",
                table: "PayrollDetails");

            migrationBuilder.DropForeignKey(
                name: "FK_PayrollDetails_PayrollPeriods_PayrollPeriodId",
                table: "PayrollDetails");

            migrationBuilder.DropIndex(
                name: "IX_PayrollPeriods_Year_Month",
                table: "PayrollPeriods");

            migrationBuilder.DropIndex(
                name: "IX_PayrollDetails_PayrollPeriodId_EmployeeId",
                table: "PayrollDetails");

            migrationBuilder.AlterColumn<decimal>(
                name: "Quantity",
                table: "PayrollConcepts",
                type: "decimal(18,2)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,4)",
                oldPrecision: 18,
                oldScale: 4,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PayrollDetails_PayrollPeriodId",
                table: "PayrollDetails",
                column: "PayrollPeriodId");

            migrationBuilder.AddForeignKey(
                name: "FK_PayrollDetails_Contracts_ContractId",
                table: "PayrollDetails",
                column: "ContractId",
                principalTable: "Contracts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PayrollDetails_Employees_EmployeeId",
                table: "PayrollDetails",
                column: "EmployeeId",
                principalTable: "Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PayrollDetails_PayrollPeriods_PayrollPeriodId",
                table: "PayrollDetails",
                column: "PayrollPeriodId",
                principalTable: "PayrollPeriods",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
