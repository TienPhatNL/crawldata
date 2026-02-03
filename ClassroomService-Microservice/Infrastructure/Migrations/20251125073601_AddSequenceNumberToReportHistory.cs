using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClassroomService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSequenceNumberToReportHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add SequenceNumber column with default value 1
            migrationBuilder.AddColumn<int>(
                name: "SequenceNumber",
                table: "ReportHistories",
                type: "int",
                nullable: false,
                defaultValue: 1);
            
            // Update existing records to have proper sequence numbers
            // Calculate sequence based on ChangedAt timestamp within each Version
            migrationBuilder.Sql(@"
                WITH RankedHistory AS (
                    SELECT 
                        Id,
                        ROW_NUMBER() OVER (PARTITION BY ReportId, Version ORDER BY ChangedAt) AS SeqNum
                    FROM ReportHistories
                )
                UPDATE rh
                SET rh.SequenceNumber = rh2.SeqNum
                FROM ReportHistories rh
                INNER JOIN RankedHistory rh2 ON rh.Id = rh2.Id
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SequenceNumber",
                table: "ReportHistories");
        }
    }
}
