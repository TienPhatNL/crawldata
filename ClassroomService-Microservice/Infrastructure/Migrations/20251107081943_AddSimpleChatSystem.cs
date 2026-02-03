using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClassroomService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSimpleChatSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Chats_Courses_CourseId",
                table: "Chats");

            migrationBuilder.AddColumn<Guid>(
                name: "ConversationId",
                table: "Chats",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Chats",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "Conversations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CourseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    User1Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    User2Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LastMessageAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastMessagePreview = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastModifiedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Conversations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Conversations_Courses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Courses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Chats_ConversationId_SentAt",
                table: "Chats",
                columns: new[] { "ConversationId", "SentAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Chats_IsDeleted",
                table: "Chats",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_Conversations_CourseId_User1Id_User2Id",
                table: "Conversations",
                columns: new[] { "CourseId", "User1Id", "User2Id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Conversations_User1Id_LastMessageAt",
                table: "Conversations",
                columns: new[] { "User1Id", "LastMessageAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Conversations_User2Id_LastMessageAt",
                table: "Conversations",
                columns: new[] { "User2Id", "LastMessageAt" });

            migrationBuilder.AddForeignKey(
                name: "FK_Chats_Conversations_ConversationId",
                table: "Chats",
                column: "ConversationId",
                principalTable: "Conversations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Chats_Courses_CourseId",
                table: "Chats",
                column: "CourseId",
                principalTable: "Courses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Chats_Conversations_ConversationId",
                table: "Chats");

            migrationBuilder.DropForeignKey(
                name: "FK_Chats_Courses_CourseId",
                table: "Chats");

            migrationBuilder.DropTable(
                name: "Conversations");

            migrationBuilder.DropIndex(
                name: "IX_Chats_ConversationId_SentAt",
                table: "Chats");

            migrationBuilder.DropIndex(
                name: "IX_Chats_IsDeleted",
                table: "Chats");

            migrationBuilder.DropColumn(
                name: "ConversationId",
                table: "Chats");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Chats");

            migrationBuilder.AddForeignKey(
                name: "FK_Chats_Courses_CourseId",
                table: "Chats",
                column: "CourseId",
                principalTable: "Courses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
