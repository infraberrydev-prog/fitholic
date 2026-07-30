using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace FitHolic.Migrations
{
    /// <inheritdoc />
    public partial class AddGymReportTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GymReports",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ReportName = table.Column<string>(type: "text", nullable: false),
                    PaymentType = table.Column<string>(type: "text", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GymReports", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GymReportRows",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    GymReportId = table.Column<int>(type: "integer", nullable: false),
                    CustomerName = table.Column<string>(type: "text", nullable: false),
                    IsCustomerMember = table.Column<bool>(type: "boolean", nullable: false),
                    DateOfEnrollment = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    MembershipFee = table.Column<decimal>(type: "numeric", nullable: false),
                    ContractDuration = table.Column<string>(type: "text", nullable: false),
                    MonthlyPayment = table.Column<decimal>(type: "numeric", nullable: false),
                    IncludePersonalTrainer = table.Column<bool>(type: "boolean", nullable: false),
                    PersonalTrainerPackage = table.Column<string>(type: "text", nullable: true),
                    PaymentOption = table.Column<string>(type: "text", nullable: false),
                    AmountToPay = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GymReportRows", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GymReportRows_GymReports_GymReportId",
                        column: x => x.GymReportId,
                        principalTable: "GymReports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GymReportRows_GymReportId",
                table: "GymReportRows",
                column: "GymReportId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GymReportRows");

            migrationBuilder.DropTable(
                name: "GymReports");
        }
    }
}
