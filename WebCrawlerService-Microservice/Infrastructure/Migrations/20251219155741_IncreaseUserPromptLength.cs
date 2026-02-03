using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebCrawlerService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class IncreaseUserPromptLength : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "UserPrompt",
                table: "CrawlJobs",
                type: "nvarchar(max)",
                maxLength: 8000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(2000)",
                oldMaxLength: 2000,
                oldNullable: true);

            migrationBuilder.UpdateData(
                table: "CrawlTemplates",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000001"),
                columns: new[] { "CreatedAt", "LastTestedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 12, 19, 15, 57, 40, 965, DateTimeKind.Utc).AddTicks(4626), new DateTime(2025, 12, 19, 15, 57, 40, 965, DateTimeKind.Utc).AddTicks(4618), new DateTime(2025, 12, 19, 15, 57, 40, 965, DateTimeKind.Utc).AddTicks(4626) });

            migrationBuilder.UpdateData(
                table: "CrawlTemplates",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000002"),
                columns: new[] { "CreatedAt", "LastTestedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 12, 19, 15, 57, 40, 965, DateTimeKind.Utc).AddTicks(4845), new DateTime(2025, 12, 19, 15, 57, 40, 965, DateTimeKind.Utc).AddTicks(4842), new DateTime(2025, 12, 19, 15, 57, 40, 965, DateTimeKind.Utc).AddTicks(4846) });

            migrationBuilder.UpdateData(
                table: "CrawlTemplates",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000003"),
                columns: new[] { "CreatedAt", "LastTestedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 12, 19, 15, 57, 40, 965, DateTimeKind.Utc).AddTicks(5215), new DateTime(2025, 12, 19, 15, 57, 40, 965, DateTimeKind.Utc).AddTicks(5212), new DateTime(2025, 12, 19, 15, 57, 40, 965, DateTimeKind.Utc).AddTicks(5215) });

            migrationBuilder.UpdateData(
                table: "CrawlerAgents",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000001"),
                column: "ConfigurationJson",
                value: "{\n                    \"provider\": \"Shopee\",\n                    \"apiVersion\": \"v4\",\n                    \"baseUrl\": \"https://shopee.vn\",\n                    \"supportedDomains\": [\"shopee.vn\", \"shopee.com\"],\n                    \"capabilities\": [\n                        \"product_details\",\n                        \"reviews\",\n                        \"ratings\",\n                        \"shop_info\",\n                        \"mobile_api\",\n                        \"high_speed\"\n                    ],\n                    \"features\": {\n                        \"productDetails\": true,\n                        \"reviews\": true,\n                        \"search\": true,\n                        \"shopInfo\": true\n                    },\n                    \"rateLimit\": {\n                        \"requestsPerMinute\": 20,\n                        \"burstSize\": 5\n                    },\n                    \"retry\": {\n                        \"maxAttempts\": 3,\n                        \"backoffMs\": 1000\n                    }\n                }");

            migrationBuilder.UpdateData(
                table: "CrawlerAgents",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000002"),
                column: "ConfigurationJson",
                value: "{\n                    \"capabilities\": [\n                        \"html_parsing\",\n                        \"static_content\",\n                        \"api_calls\",\n                        \"headers_manipulation\",\n                        \"cookies\"\n                    ],\n                    \"timeout\": 30000,\n                    \"followRedirects\": true,\n                    \"maxRedirects\": 5,\n                    \"compression\": true,\n                    \"retry\": {\n                        \"maxAttempts\": 3,\n                        \"backoffMs\": 2000\n                    }\n                }");

            migrationBuilder.UpdateData(
                table: "CrawlerAgents",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000003"),
                column: "ConfigurationJson",
                value: "{\n                    \"capabilities\": [\n                        \"javascript_execution\",\n                        \"spa_support\",\n                        \"screenshots\",\n                        \"pdf_generation\",\n                        \"user_interactions\",\n                        \"network_interception\",\n                        \"geolocation\",\n                        \"device_emulation\"\n                    ],\n                    \"browser\": \"chromium\",\n                    \"headless\": true,\n                    \"viewport\": {\n                        \"width\": 1920,\n                        \"height\": 1080\n                    },\n                    \"timeout\": 60000,\n                    \"waitUntil\": \"networkidle\",\n                    \"blockResources\": [\"image\", \"font\", \"media\"],\n                    \"retry\": {\n                        \"maxAttempts\": 2,\n                        \"backoffMs\": 5000\n                    }\n                }");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "UserPrompt",
                table: "CrawlJobs",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldMaxLength: 8000,
                oldNullable: true);

            migrationBuilder.UpdateData(
                table: "CrawlTemplates",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000001"),
                columns: new[] { "CreatedAt", "LastTestedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 12, 11, 8, 42, 53, 452, DateTimeKind.Utc).AddTicks(7348), new DateTime(2025, 12, 11, 8, 42, 53, 452, DateTimeKind.Utc).AddTicks(7344), new DateTime(2025, 12, 11, 8, 42, 53, 452, DateTimeKind.Utc).AddTicks(7349) });

            migrationBuilder.UpdateData(
                table: "CrawlTemplates",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000002"),
                columns: new[] { "CreatedAt", "LastTestedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 12, 11, 8, 42, 53, 452, DateTimeKind.Utc).AddTicks(7555), new DateTime(2025, 12, 11, 8, 42, 53, 452, DateTimeKind.Utc).AddTicks(7553), new DateTime(2025, 12, 11, 8, 42, 53, 452, DateTimeKind.Utc).AddTicks(7556) });

            migrationBuilder.UpdateData(
                table: "CrawlTemplates",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000003"),
                columns: new[] { "CreatedAt", "LastTestedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 12, 11, 8, 42, 53, 452, DateTimeKind.Utc).AddTicks(7847), new DateTime(2025, 12, 11, 8, 42, 53, 452, DateTimeKind.Utc).AddTicks(7845), new DateTime(2025, 12, 11, 8, 42, 53, 452, DateTimeKind.Utc).AddTicks(7848) });

            migrationBuilder.UpdateData(
                table: "CrawlerAgents",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000001"),
                column: "ConfigurationJson",
                value: "{\r\n                    \"provider\": \"Shopee\",\r\n                    \"apiVersion\": \"v4\",\r\n                    \"baseUrl\": \"https://shopee.vn\",\r\n                    \"supportedDomains\": [\"shopee.vn\", \"shopee.com\"],\r\n                    \"capabilities\": [\r\n                        \"product_details\",\r\n                        \"reviews\",\r\n                        \"ratings\",\r\n                        \"shop_info\",\r\n                        \"mobile_api\",\r\n                        \"high_speed\"\r\n                    ],\r\n                    \"features\": {\r\n                        \"productDetails\": true,\r\n                        \"reviews\": true,\r\n                        \"search\": true,\r\n                        \"shopInfo\": true\r\n                    },\r\n                    \"rateLimit\": {\r\n                        \"requestsPerMinute\": 20,\r\n                        \"burstSize\": 5\r\n                    },\r\n                    \"retry\": {\r\n                        \"maxAttempts\": 3,\r\n                        \"backoffMs\": 1000\r\n                    }\r\n                }");

            migrationBuilder.UpdateData(
                table: "CrawlerAgents",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000002"),
                column: "ConfigurationJson",
                value: "{\r\n                    \"capabilities\": [\r\n                        \"html_parsing\",\r\n                        \"static_content\",\r\n                        \"api_calls\",\r\n                        \"headers_manipulation\",\r\n                        \"cookies\"\r\n                    ],\r\n                    \"timeout\": 30000,\r\n                    \"followRedirects\": true,\r\n                    \"maxRedirects\": 5,\r\n                    \"compression\": true,\r\n                    \"retry\": {\r\n                        \"maxAttempts\": 3,\r\n                        \"backoffMs\": 2000\r\n                    }\r\n                }");

            migrationBuilder.UpdateData(
                table: "CrawlerAgents",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000003"),
                column: "ConfigurationJson",
                value: "{\r\n                    \"capabilities\": [\r\n                        \"javascript_execution\",\r\n                        \"spa_support\",\r\n                        \"screenshots\",\r\n                        \"pdf_generation\",\r\n                        \"user_interactions\",\r\n                        \"network_interception\",\r\n                        \"geolocation\",\r\n                        \"device_emulation\"\r\n                    ],\r\n                    \"browser\": \"chromium\",\r\n                    \"headless\": true,\r\n                    \"viewport\": {\r\n                        \"width\": 1920,\r\n                        \"height\": 1080\r\n                    },\r\n                    \"timeout\": 60000,\r\n                    \"waitUntil\": \"networkidle\",\r\n                    \"blockResources\": [\"image\", \"font\", \"media\"],\r\n                    \"retry\": {\r\n                        \"maxAttempts\": 2,\r\n                        \"backoffMs\": 5000\r\n                    }\r\n                }");
        }
    }
}
