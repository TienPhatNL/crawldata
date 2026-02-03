using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClassroomService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ChangeSupportRequestConversationToOneToMany : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SupportRequests_Conversations_ConversationId",
                table: "SupportRequests");

            migrationBuilder.DropIndex(
                name: "IX_SupportRequests_ConversationId",
                table: "SupportRequests");

            migrationBuilder.CreateIndex(
                name: "IX_SupportRequests_ConversationId",
                table: "SupportRequests",
                column: "ConversationId");

            migrationBuilder.AddForeignKey(
                name: "FK_SupportRequests_Conversations_ConversationId",
                table: "SupportRequests",
                column: "ConversationId",
                principalTable: "Conversations",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SupportRequests_Conversations_ConversationId",
                table: "SupportRequests");

            migrationBuilder.DropIndex(
                name: "IX_SupportRequests_ConversationId",
                table: "SupportRequests");

            migrationBuilder.CreateIndex(
                name: "IX_SupportRequests_ConversationId",
                table: "SupportRequests",
                column: "ConversationId",
                unique: true,
                filter: "[ConversationId] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_SupportRequests_Conversations_ConversationId",
                table: "SupportRequests",
                column: "ConversationId",
                principalTable: "Conversations",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
