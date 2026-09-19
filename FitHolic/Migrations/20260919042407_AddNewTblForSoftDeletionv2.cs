using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace FitHolic.Migrations
{
    /// <inheritdoc />
    public partial class AddNewTblForSoftDeletionv2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Logs_DeletedReports",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    GymReportId = table.Column<int>(type: "integer", nullable: false),
                    CustomerName = table.Column<string>(type: "text", nullable: false),
                    IsCustomerMember = table.Column<bool>(type: "boolean", nullable: false),
                    DateOfEnrollment = table.Column<DateOnly>(type: "date", nullable: true),
                    MembershipFee = table.Column<decimal>(type: "numeric", nullable: false),
                    ContractDuration = table.Column<string>(type: "text", nullable: false),
                    MonthlyPayment = table.Column<decimal>(type: "numeric", nullable: false),
                    IncludePersonalTrainer = table.Column<bool>(type: "boolean", nullable: false),
                    PersonalTrainerPackage = table.Column<string>(type: "text", nullable: true),
                    PaymentOption = table.Column<string>(type: "text", nullable: false),
                    AmountToPay = table.Column<decimal>(type: "numeric", nullable: false),
                    DailyPass = table.Column<decimal>(type: "numeric", nullable: true),
                    OnlinePaymentMethod = table.Column<string>(type: "text", nullable: true),
                    ReferenceNumber = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Logs_DeletedReports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Logs_DeletedReports_GymReports_GymReportId",
                        column: x => x.GymReportId,
                        principalTable: "GymReports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Logs_DeletedReports_GymReportId",
                table: "Logs_DeletedReports",
                column: "GymReportId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Logs_DeletedReports");
        }
    }
}
