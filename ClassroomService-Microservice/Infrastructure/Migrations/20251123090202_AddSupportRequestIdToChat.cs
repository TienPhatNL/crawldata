using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClassroomService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSupportRequestIdToChat : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "SupportRequestId",
                table: "Chats",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Chats_SupportRequestId",
                table: "Chats",
                column: "SupportRequestId");

            migrationBuilder.AddForeignKey(
                name: "FK_Chats_SupportRequests_SupportRequestId",
                table: "Chats",
                column: "SupportRequestId",
                principalTable: "SupportRequests",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Chats_SupportRequests_SupportRequestId",
                table: "Chats");

            migrationBuilder.DropIndex(
                name: "IX_Chats_SupportRequestId",
                table: "Chats");

            migrationBuilder.DropColumn(
                name: "SupportRequestId",
                table: "Chats");
        }
    }
}
