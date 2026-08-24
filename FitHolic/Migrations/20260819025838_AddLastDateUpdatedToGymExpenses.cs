using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FitHolic.Migrations
{
    /// <inheritdoc />
    public partial class AddLastDateUpdatedToGymExpenses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastDateUpdated",
                table: "GymExpenses",
                type: "timestamp without time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastDateUpdated",
                table: "GymExpenses");
        }
    }
}
