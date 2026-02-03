using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClassroomService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddReportHistoryTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ReportHistories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReportId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Action = table.Column<int>(type: "int", nullable: false),
                    ChangedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    ChangedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    FieldsChanged = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OldValues = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NewValues = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Comment = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReportHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReportHistories_Reports_ReportId",
                        column: x => x.ReportId,
                        principalTable: "Reports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ReportHistories_ChangedAt",
                table: "ReportHistories",
                column: "ChangedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ReportHistories_ChangedBy",
                table: "ReportHistories",
                column: "ChangedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ReportHistories_ReportId",
                table: "ReportHistories",
                column: "ReportId");

            migrationBuilder.CreateIndex(
                name: "IX_ReportHistories_ReportId_ChangedAt",
                table: "ReportHistories",
                columns: new[] { "ReportId", "ChangedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ReportHistories_ReportId_Version",
                table: "ReportHistories",
                columns: new[] { "ReportId", "Version" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReportHistories");
        }
    }
}
