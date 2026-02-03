using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClassroomService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSupportRequestRejectionAndConversationClosure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "RejectedAt",
                table: "SupportRequests",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "RejectedBy",
                table: "SupportRequests",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RejectionComments",
                table: "SupportRequests",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RejectionReason",
                table: "SupportRequests",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ClosedAt",
                table: "Conversations",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ClosedBy",
                table: "Conversations",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsClosed",
                table: "Conversations",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RejectedAt",
                table: "SupportRequests");

            migrationBuilder.DropColumn(
                name: "RejectedBy",
                table: "SupportRequests");

            migrationBuilder.DropColumn(
                name: "RejectionComments",
                table: "SupportRequests");

            migrationBuilder.DropColumn(
                name: "RejectionReason",
                table: "SupportRequests");

            migrationBuilder.DropColumn(
                name: "ClosedAt",
                table: "Conversations");

            migrationBuilder.DropColumn(
                name: "ClosedBy",
                table: "Conversations");

            migrationBuilder.DropColumn(
                name: "IsClosed",
                table: "Conversations");
        }
    }
}
