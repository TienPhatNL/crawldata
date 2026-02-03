using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClassroomService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddChangeTrackingToReportHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ChangeDetails",
                table: "ReportHistories",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ChangeSummary",
                table: "ReportHistories",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UnifiedDiff",
                table: "ReportHistories",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ChangeDetails",
                table: "ReportHistories");

            migrationBuilder.DropColumn(
                name: "ChangeSummary",
                table: "ReportHistories");

            migrationBuilder.DropColumn(
                name: "UnifiedDiff",
                table: "ReportHistories");
        }
    }
}
