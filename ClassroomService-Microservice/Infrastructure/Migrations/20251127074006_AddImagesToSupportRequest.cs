using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClassroomService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddImagesToSupportRequest : Migration
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

            migrationBuilder.AddColumn<string>(
                name: "Images",
                table: "SupportRequests",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SupportRequests_AssignedStaffId",
                table: "SupportRequests",
                column: "AssignedStaffId");

            migrationBuilder.CreateIndex(
                name: "IX_SupportRequests_ConversationId",
                table: "SupportRequests",
                column: "ConversationId",
                unique: true,
                filter: "[ConversationId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SupportRequests_Priority",
                table: "SupportRequests",
                column: "Priority");

            migrationBuilder.CreateIndex(
                name: "IX_SupportRequests_RequestedAt",
                table: "SupportRequests",
                column: "RequestedAt");

            migrationBuilder.CreateIndex(
                name: "IX_SupportRequests_RequesterId",
                table: "SupportRequests",
                column: "RequesterId");

            migrationBuilder.CreateIndex(
                name: "IX_SupportRequests_Status",
                table: "SupportRequests",
                column: "Status");

            migrationBuilder.AddForeignKey(
                name: "FK_SupportRequests_Conversations_ConversationId",
                table: "SupportRequests",
                column: "ConversationId",
                principalTable: "Conversations",
                principalColumn: "Id",
                onDelete: ReferentialAction.NoAction);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SupportRequests_Conversations_ConversationId",
                table: "SupportRequests");

            migrationBuilder.DropIndex(
                name: "IX_SupportRequests_AssignedStaffId",
                table: "SupportRequests");

            migrationBuilder.DropIndex(
                name: "IX_SupportRequests_ConversationId",
                table: "SupportRequests");

            migrationBuilder.DropIndex(
                name: "IX_SupportRequests_Priority",
                table: "SupportRequests");

            migrationBuilder.DropIndex(
                name: "IX_SupportRequests_RequestedAt",
                table: "SupportRequests");

            migrationBuilder.DropIndex(
                name: "IX_SupportRequests_RequesterId",
                table: "SupportRequests");

            migrationBuilder.DropIndex(
                name: "IX_SupportRequests_Status",
                table: "SupportRequests");

            migrationBuilder.DropColumn(
                name: "Images",
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
    }
}
