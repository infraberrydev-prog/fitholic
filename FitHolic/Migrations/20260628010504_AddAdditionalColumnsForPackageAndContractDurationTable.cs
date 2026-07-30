using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FitHolic.Migrations
{
    /// <inheritdoc />
    public partial class AddAdditionalColumnsForPackageAndContractDurationTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "Price",
                table: "Package",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "MonthlyPayment",
                table: "ContractDuration",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Price",
                table: "Package");

            migrationBuilder.DropColumn(
                name: "MonthlyPayment",
                table: "ContractDuration");
        }
    }
}
