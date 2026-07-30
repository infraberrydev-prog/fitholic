using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FitHolic.Migrations
{
    /// <inheritdoc />
    public partial class AddOnlinePaymentFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "OnlinePaymentMethod",
                table: "GymReportRows",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReferenceNumber",
                table: "GymReportRows",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OnlinePaymentMethod",
                table: "GymReportRows");

            migrationBuilder.DropColumn(
                name: "ReferenceNumber",
                table: "GymReportRows");
        }
    }
}
