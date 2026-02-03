using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClassroomService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddReportAICheck : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ReportAIChecks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReportId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AIPercentage = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    Provider = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    RawResponse = table.Column<string>(type: "nvarchar(max)", maxLength: 5000, nullable: true),
                    CheckedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CheckedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastModifiedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReportAIChecks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReportAIChecks_Reports_ReportId",
                        column: x => x.ReportId,
                        principalTable: "Reports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ReportAIChecks_CheckedAt",
                table: "ReportAIChecks",
                column: "CheckedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ReportAIChecks_CheckedBy",
                table: "ReportAIChecks",
                column: "CheckedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ReportAIChecks_ReportId",
                table: "ReportAIChecks",
                column: "ReportId");

            migrationBuilder.CreateIndex(
                name: "IX_ReportAIChecks_ReportId_CheckedAt",
                table: "ReportAIChecks",
                columns: new[] { "ReportId", "CheckedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReportAIChecks");
        }
    }
}
