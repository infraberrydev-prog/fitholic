using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FitHolic.Migrations
{
    /// <inheritdoc />
    public partial class UpdateGymExpensesReportTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Water",
                table: "GymExpenses",
                newName: "Expenses");

            migrationBuilder.RenameColumn(
                name: "OtherUtilities",
                table: "GymExpenses",
                newName: "CashPayment");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Expenses",
                table: "GymExpenses",
                newName: "Water");

            migrationBuilder.RenameColumn(
                name: "CashPayment",
                table: "GymExpenses",
                newName: "OtherUtilities");
        }
    }
}
