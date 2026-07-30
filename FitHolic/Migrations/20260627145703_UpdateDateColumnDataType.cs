using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FitHolic.Migrations
{
    /// <inheritdoc />
    public partial class UpdateDateColumnDataType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateOnly>(
                name: "DateOfEnrollment",
                table: "GymReportRows",
                type: "date",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "DateOfEnrollment",
                table: "GymReportRows",
                type: "timestamp without time zone",
                nullable: false,
                oldClrType: typeof(DateOnly),
                oldType: "date");
        }
    }
}
