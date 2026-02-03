using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClassroomService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddIsCrawlerConversation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Conversations_CourseId_User1Id_User2Id",
                table: "Conversations");

            migrationBuilder.AddColumn<bool>(
                name: "IsCrawler",
                table: "Conversations",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_Conversations_CourseId_User1Id_User2Id",
                table: "Conversations",
                columns: new[] { "CourseId", "User1Id", "User2Id" },
                unique: true,
                filter: "[IsCrawler] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Conversations_CourseId_User1Id_User2Id",
                table: "Conversations");

            migrationBuilder.DropColumn(
                name: "IsCrawler",
                table: "Conversations");

            migrationBuilder.CreateIndex(
                name: "IX_Conversations_CourseId_User1Id_User2Id",
                table: "Conversations",
                columns: new[] { "CourseId", "User1Id", "User2Id" },
                unique: true);
        }
    }
}
