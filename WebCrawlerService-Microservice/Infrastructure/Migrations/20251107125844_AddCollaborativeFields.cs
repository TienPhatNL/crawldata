using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebCrawlerService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCollaborativeFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AccessLevel",
                table: "CrawlJobs",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "ConversationThreadId",
                table: "CrawlJobs",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "GroupId",
                table: "CrawlJobs",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsCollaborative",
                table: "CrawlJobs",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "CrawlJobParticipants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CrawlJobId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GroupId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Role = table.Column<int>(type: "int", nullable: false),
                    JoinedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastViewedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsNotified = table.Column<bool>(type: "bit", nullable: false),
                    IsWatching = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CrawlJobParticipants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CrawlJobParticipants_CrawlJobs_CrawlJobId",
                        column: x => x.CrawlJobId,
                        principalTable: "CrawlJobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "CrawlTemplates",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000001"),
                columns: new[] { "CreatedAt", "LastTestedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 11, 7, 12, 58, 43, 574, DateTimeKind.Utc).AddTicks(9532), new DateTime(2025, 11, 7, 12, 58, 43, 574, DateTimeKind.Utc).AddTicks(9523), new DateTime(2025, 11, 7, 12, 58, 43, 574, DateTimeKind.Utc).AddTicks(9533) });

            migrationBuilder.UpdateData(
                table: "CrawlTemplates",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000002"),
                columns: new[] { "CreatedAt", "LastTestedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 11, 7, 12, 58, 43, 574, DateTimeKind.Utc).AddTicks(9796), new DateTime(2025, 11, 7, 12, 58, 43, 574, DateTimeKind.Utc).AddTicks(9793), new DateTime(2025, 11, 7, 12, 58, 43, 574, DateTimeKind.Utc).AddTicks(9796) });

            migrationBuilder.UpdateData(
                table: "CrawlTemplates",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000003"),
                columns: new[] { "CreatedAt", "LastTestedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 11, 7, 12, 58, 43, 575, DateTimeKind.Utc).AddTicks(435), new DateTime(2025, 11, 7, 12, 58, 43, 575, DateTimeKind.Utc).AddTicks(432), new DateTime(2025, 11, 7, 12, 58, 43, 575, DateTimeKind.Utc).AddTicks(436) });

            migrationBuilder.CreateIndex(
                name: "IX_CrawlJobParticipants_CrawlJobId",
                table: "CrawlJobParticipants",
                column: "CrawlJobId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CrawlJobParticipants");

            migrationBuilder.DropColumn(
                name: "AccessLevel",
                table: "CrawlJobs");

            migrationBuilder.DropColumn(
                name: "ConversationThreadId",
                table: "CrawlJobs");

            migrationBuilder.DropColumn(
                name: "GroupId",
                table: "CrawlJobs");

            migrationBuilder.DropColumn(
                name: "IsCollaborative",
                table: "CrawlJobs");

            migrationBuilder.UpdateData(
                table: "CrawlTemplates",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000001"),
                columns: new[] { "CreatedAt", "LastTestedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 10, 24, 9, 31, 40, 221, DateTimeKind.Utc).AddTicks(9068), new DateTime(2025, 10, 24, 9, 31, 40, 221, DateTimeKind.Utc).AddTicks(9059), new DateTime(2025, 10, 24, 9, 31, 40, 221, DateTimeKind.Utc).AddTicks(9069) });

            migrationBuilder.UpdateData(
                table: "CrawlTemplates",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000002"),
                columns: new[] { "CreatedAt", "LastTestedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 10, 24, 9, 31, 40, 221, DateTimeKind.Utc).AddTicks(9376), new DateTime(2025, 10, 24, 9, 31, 40, 221, DateTimeKind.Utc).AddTicks(9373), new DateTime(2025, 10, 24, 9, 31, 40, 221, DateTimeKind.Utc).AddTicks(9376) });

            migrationBuilder.UpdateData(
                table: "CrawlTemplates",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000003"),
                columns: new[] { "CreatedAt", "LastTestedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 10, 24, 9, 31, 40, 222, DateTimeKind.Utc).AddTicks(64), new DateTime(2025, 10, 24, 9, 31, 40, 222, DateTimeKind.Utc).AddTicks(62), new DateTime(2025, 10, 24, 9, 31, 40, 222, DateTimeKind.Utc).AddTicks(65) });
        }
    }
}
