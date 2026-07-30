using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FitHolic.Migrations
{
    /// <inheritdoc />
    public partial class AddDailyPassColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "DailyPass",
                table: "GymReportRows",
                type: "numeric",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DailyPass",
                table: "GymReportRows");
        }
    }
}
