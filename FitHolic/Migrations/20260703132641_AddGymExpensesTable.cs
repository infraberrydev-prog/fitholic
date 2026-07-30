using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace FitHolic.Migrations
{
    /// <inheritdoc />
    public partial class AddGymExpensesTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GymExpenses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ReportName = table.Column<string>(type: "text", nullable: false),
                    Payroll = table.Column<decimal>(type: "numeric", nullable: false),
                    OnlinePayment = table.Column<decimal>(type: "numeric", nullable: true),
                    Electricity = table.Column<decimal>(type: "numeric", nullable: false),
                    Water = table.Column<decimal>(type: "numeric", nullable: false),
                    OtherUtilities = table.Column<decimal>(type: "numeric", nullable: false),
                    TotalExpenses = table.Column<decimal>(type: "numeric", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GymExpenses", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GymExpenses");
        }
    }
}
