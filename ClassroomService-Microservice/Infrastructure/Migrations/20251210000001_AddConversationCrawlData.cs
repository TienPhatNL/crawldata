using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClassroomService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddConversationCrawlData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ConversationCrawlData",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ConversationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CrawlJobId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceUrl = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    CrawledAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ResultCount = table.Column<int>(type: "int", nullable: false),
                    NormalizedDataJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EmbeddingText = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    VectorEmbeddingJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ValidRecordCount = table.Column<int>(type: "int", nullable: false),
                    InvalidRecordCount = table.Column<int>(type: "int", nullable: false),
                    DataQualityScore = table.Column<double>(type: "float", nullable: false),
                    ValidationWarningsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DetectedSchemaJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConversationCrawlData", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConversationCrawlData_Conversations_ConversationId",
                        column: x => x.ConversationId,
                        principalTable: "Conversations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ConversationCrawlData_ConversationId",
                table: "ConversationCrawlData",
                column: "ConversationId");

            migrationBuilder.CreateIndex(
                name: "IX_ConversationCrawlData_CrawlJobId",
                table: "ConversationCrawlData",
                column: "CrawlJobId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ConversationCrawlData_ConversationId_CrawledAt",
                table: "ConversationCrawlData",
                columns: new[] { "ConversationId", "CrawledAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ConversationCrawlData_DataQualityScore",
                table: "ConversationCrawlData",
                column: "DataQualityScore");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConversationCrawlData");
        }
    }
}
