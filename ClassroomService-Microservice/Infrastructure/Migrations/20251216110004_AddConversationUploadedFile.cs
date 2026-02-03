using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClassroomService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddConversationUploadedFile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "ConversationCrawlData");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "ConversationCrawlData");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "ConversationCrawlData");

            migrationBuilder.CreateTable(
                name: "ConversationUploadedFiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ConversationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    FileUrl = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    FileSize = table.Column<long>(type: "bigint", nullable: false),
                    DataJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RowCount = table.Column<int>(type: "int", nullable: false),
                    ColumnNamesJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UploadedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastModifiedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConversationUploadedFiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConversationUploadedFiles_Conversations_ConversationId",
                        column: x => x.ConversationId,
                        principalTable: "Conversations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ConversationUploadedFiles_ConversationId",
                table: "ConversationUploadedFiles",
                column: "ConversationId");

            migrationBuilder.CreateIndex(
                name: "IX_ConversationUploadedFiles_ConversationId_UploadedAt",
                table: "ConversationUploadedFiles",
                columns: new[] { "ConversationId", "UploadedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ConversationUploadedFiles_ConversationId_UploadedAt",
                table: "ConversationUploadedFiles");

            migrationBuilder.DropIndex(
                name: "IX_ConversationUploadedFiles_ConversationId",
                table: "ConversationUploadedFiles");

            migrationBuilder.DropTable(
                name: "ConversationUploadedFiles");

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "ConversationCrawlData",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedBy",
                table: "ConversationCrawlData",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedBy",
                table: "ConversationCrawlData",
                type: "uniqueidentifier",
                nullable: true);
        }
    }
}
