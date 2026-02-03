using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClassroomService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddReportCollaborationFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "BatchId",
                table: "ReportHistories",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CharactersAdded",
                table: "ReportHistories",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CharactersDeleted",
                table: "ReportHistories",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ContributorIds",
                table: "ReportHistories",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "EditDuration",
                table: "ReportHistories",
                type: "time",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsBatchFlush",
                table: "ReportHistories",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BatchId",
                table: "ReportHistories");

            migrationBuilder.DropColumn(
                name: "CharactersAdded",
                table: "ReportHistories");

            migrationBuilder.DropColumn(
                name: "CharactersDeleted",
                table: "ReportHistories");

            migrationBuilder.DropColumn(
                name: "ContributorIds",
                table: "ReportHistories");

            migrationBuilder.DropColumn(
                name: "EditDuration",
                table: "ReportHistories");

            migrationBuilder.DropColumn(
                name: "IsBatchFlush",
                table: "ReportHistories");
        }
    }
}
