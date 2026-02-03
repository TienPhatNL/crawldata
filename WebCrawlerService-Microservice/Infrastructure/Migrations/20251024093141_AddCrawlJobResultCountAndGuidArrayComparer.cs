using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebCrawlerService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCrawlJobResultCountAndGuidArrayComparer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "AgentPoolId",
                table: "CrawlJobs",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "AssignedAgentPoolId",
                table: "CrawlJobs",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ConversationId",
                table: "CrawlJobs",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CurrentNavigationStep",
                table: "CrawlJobs",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "NavigationProgressJson",
                table: "CrawlJobs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "NavigationStrategyId",
                table: "CrawlJobs",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "NavigationStrategyId1",
                table: "CrawlJobs",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ParentPromptId",
                table: "CrawlJobs",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ResultCount",
                table: "CrawlJobs",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SessionType",
                table: "CrawlJobs",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "AgentPools",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InstanceId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    AgentType = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    MaxConcurrentJobs = table.Column<int>(type: "int", nullable: false),
                    CurrentJobCount = table.Column<int>(type: "int", nullable: false),
                    LastHeartbeat = table.Column<DateTime>(type: "datetime2", nullable: true),
                    HealthStatus = table.Column<int>(type: "int", nullable: false),
                    HealthMessage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsAutoScaled = table.Column<bool>(type: "bit", nullable: false),
                    AutoScaleCreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ScheduledForRemovalAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TotalJobsProcessed = table.Column<int>(type: "int", nullable: false),
                    SuccessfulJobs = table.Column<int>(type: "int", nullable: false),
                    FailedJobs = table.Column<int>(type: "int", nullable: false),
                    AverageJobDurationMs = table.Column<double>(type: "float", nullable: false),
                    ContainerId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IpAddress = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Port = table.Column<int>(type: "int", nullable: true),
                    ConfigurationJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifiedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgentPools", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AgentScalingConfigs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AgentType = table.Column<int>(type: "int", nullable: false),
                    MinAgents = table.Column<int>(type: "int", nullable: false),
                    MaxAgents = table.Column<int>(type: "int", nullable: false),
                    TargetAgents = table.Column<int>(type: "int", nullable: false),
                    AutoScalingEnabled = table.Column<bool>(type: "bit", nullable: false),
                    ScaleUpThreshold = table.Column<double>(type: "float", nullable: false),
                    ScaleDownThreshold = table.Column<double>(type: "float", nullable: false),
                    ScaleUpCooldownMinutes = table.Column<int>(type: "int", nullable: false),
                    ScaleDownCooldownMinutes = table.Column<int>(type: "int", nullable: false),
                    MaxHourlyCost = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    PauseWhenLimitReached = table.Column<bool>(type: "bit", nullable: false),
                    LastScaleUpAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastScaleDownAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifiedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgentScalingConfigs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AnalyticsCaches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    QueryHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    OriginalQuery = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    ResultJson = table.Column<string>(type: "text", nullable: false),
                    ResultType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SourceJobIds = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    ComputedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    HitCount = table.Column<int>(type: "int", nullable: false),
                    ComputationCost = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    ComputationTimeMs = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifiedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnalyticsCaches", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LlmCostComparisons",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Provider = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Model = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    InputCostPer1M = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    OutputCostPer1M = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    AvgResponseTimeMs = table.Column<int>(type: "int", nullable: false),
                    AvgQualityScore = table.Column<double>(type: "float", nullable: false),
                    TotalUsageCount = table.Column<int>(type: "int", nullable: false),
                    TotalCostUsd = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    TotalInputTokens = table.Column<long>(type: "bigint", nullable: false),
                    TotalOutputTokens = table.Column<long>(type: "bigint", nullable: false),
                    LastUsedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastPriceUpdate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifiedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LlmCostComparisons", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NavigationStrategies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Domain = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    UrlPattern = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    NavigationStepsJson = table.Column<string>(type: "text", nullable: false),
                    TimesUsed = table.Column<int>(type: "int", nullable: false),
                    SuccessCount = table.Column<int>(type: "int", nullable: false),
                    FailureCount = table.Column<int>(type: "int", nullable: false),
                    AverageExecutionTimeMs = table.Column<double>(type: "float", nullable: false),
                    CreatedByJobId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastUsedByJobId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastUsedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastUpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsTemplate = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Tags = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedByJobId1 = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifiedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NavigationStrategies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NavigationStrategies_CrawlJobs_CreatedByJobId1",
                        column: x => x.CreatedByJobId1,
                        principalTable: "CrawlJobs",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "PromptHistories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CrawlJobId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ConversationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Type = table.Column<int>(type: "int", nullable: false),
                    PromptText = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    ResponseText = table.Column<string>(type: "text", nullable: true),
                    ResponseDataJson = table.Column<string>(type: "text", nullable: true),
                    ProcessedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ProcessingTimeMs = table.Column<int>(type: "int", nullable: false),
                    LlmCost = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifiedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PromptHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PromptHistories_CrawlJobs_CrawlJobId",
                        column: x => x.CrawlJobId,
                        principalTable: "CrawlJobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

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

            migrationBuilder.CreateIndex(
                name: "IX_CrawlJobs_AgentPoolId",
                table: "CrawlJobs",
                column: "AgentPoolId");

            migrationBuilder.CreateIndex(
                name: "IX_CrawlJobs_AssignedAgentPoolId",
                table: "CrawlJobs",
                column: "AssignedAgentPoolId");

            migrationBuilder.CreateIndex(
                name: "IX_CrawlJobs_NavigationStrategyId",
                table: "CrawlJobs",
                column: "NavigationStrategyId");

            migrationBuilder.CreateIndex(
                name: "IX_CrawlJobs_NavigationStrategyId1",
                table: "CrawlJobs",
                column: "NavigationStrategyId1");

            migrationBuilder.CreateIndex(
                name: "IX_CrawlJobs_ParentPromptId",
                table: "CrawlJobs",
                column: "ParentPromptId");

            migrationBuilder.CreateIndex(
                name: "IX_AgentPools_AgentType",
                table: "AgentPools",
                column: "AgentType");

            migrationBuilder.CreateIndex(
                name: "IX_AgentPools_HealthStatus",
                table: "AgentPools",
                column: "HealthStatus");

            migrationBuilder.CreateIndex(
                name: "IX_AgentPools_Status",
                table: "AgentPools",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_AgentScalingConfigs_AgentType",
                table: "AgentScalingConfigs",
                column: "AgentType");

            migrationBuilder.CreateIndex(
                name: "IX_AgentScalingConfigs_AutoScalingEnabled",
                table: "AgentScalingConfigs",
                column: "AutoScalingEnabled");

            migrationBuilder.CreateIndex(
                name: "IX_AgentScalingConfigs_UserId",
                table: "AgentScalingConfigs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AnalyticsCaches_CreatedAt",
                table: "AnalyticsCaches",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_AnalyticsCaches_ExpiresAt",
                table: "AnalyticsCaches",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_AnalyticsCaches_QueryHash",
                table: "AnalyticsCaches",
                column: "QueryHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AnalyticsCaches_UserId",
                table: "AnalyticsCaches",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_LlmCostComparisons_IsActive",
                table: "LlmCostComparisons",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_LlmCostComparisons_LastUsedAt",
                table: "LlmCostComparisons",
                column: "LastUsedAt");

            migrationBuilder.CreateIndex(
                name: "IX_LlmCostComparisons_Provider_Model",
                table: "LlmCostComparisons",
                columns: new[] { "Provider", "Model" });

            migrationBuilder.CreateIndex(
                name: "IX_NavigationStrategies_CreatedByJobId1",
                table: "NavigationStrategies",
                column: "CreatedByJobId1");

            migrationBuilder.CreateIndex(
                name: "IX_NavigationStrategies_Domain",
                table: "NavigationStrategies",
                column: "Domain");

            migrationBuilder.CreateIndex(
                name: "IX_NavigationStrategies_Domain_IsActive",
                table: "NavigationStrategies",
                columns: new[] { "Domain", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_NavigationStrategies_IsActive",
                table: "NavigationStrategies",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_NavigationStrategies_IsTemplate",
                table: "NavigationStrategies",
                column: "IsTemplate");

            migrationBuilder.CreateIndex(
                name: "IX_PromptHistories_ConversationId",
                table: "PromptHistories",
                column: "ConversationId");

            migrationBuilder.CreateIndex(
                name: "IX_PromptHistories_CrawlJobId",
                table: "PromptHistories",
                column: "CrawlJobId");

            migrationBuilder.CreateIndex(
                name: "IX_PromptHistories_ProcessedAt",
                table: "PromptHistories",
                column: "ProcessedAt");

            migrationBuilder.CreateIndex(
                name: "IX_PromptHistories_UserId",
                table: "PromptHistories",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_PromptHistories_UserId_Type",
                table: "PromptHistories",
                columns: new[] { "UserId", "Type" });

            migrationBuilder.AddForeignKey(
                name: "FK_CrawlJobs_AgentPools_AgentPoolId",
                table: "CrawlJobs",
                column: "AgentPoolId",
                principalTable: "AgentPools",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_CrawlJobs_AgentPools_AssignedAgentPoolId",
                table: "CrawlJobs",
                column: "AssignedAgentPoolId",
                principalTable: "AgentPools",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_CrawlJobs_NavigationStrategies_NavigationStrategyId",
                table: "CrawlJobs",
                column: "NavigationStrategyId",
                principalTable: "NavigationStrategies",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_CrawlJobs_NavigationStrategies_NavigationStrategyId1",
                table: "CrawlJobs",
                column: "NavigationStrategyId1",
                principalTable: "NavigationStrategies",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_CrawlJobs_PromptHistories_ParentPromptId",
                table: "CrawlJobs",
                column: "ParentPromptId",
                principalTable: "PromptHistories",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CrawlJobs_AgentPools_AgentPoolId",
                table: "CrawlJobs");

            migrationBuilder.DropForeignKey(
                name: "FK_CrawlJobs_AgentPools_AssignedAgentPoolId",
                table: "CrawlJobs");

            migrationBuilder.DropForeignKey(
                name: "FK_CrawlJobs_NavigationStrategies_NavigationStrategyId",
                table: "CrawlJobs");

            migrationBuilder.DropForeignKey(
                name: "FK_CrawlJobs_NavigationStrategies_NavigationStrategyId1",
                table: "CrawlJobs");

            migrationBuilder.DropForeignKey(
                name: "FK_CrawlJobs_PromptHistories_ParentPromptId",
                table: "CrawlJobs");

            migrationBuilder.DropTable(
                name: "AgentPools");

            migrationBuilder.DropTable(
                name: "AgentScalingConfigs");

            migrationBuilder.DropTable(
                name: "AnalyticsCaches");

            migrationBuilder.DropTable(
                name: "LlmCostComparisons");

            migrationBuilder.DropTable(
                name: "NavigationStrategies");

            migrationBuilder.DropTable(
                name: "PromptHistories");

            migrationBuilder.DropIndex(
                name: "IX_CrawlJobs_AgentPoolId",
                table: "CrawlJobs");

            migrationBuilder.DropIndex(
                name: "IX_CrawlJobs_AssignedAgentPoolId",
                table: "CrawlJobs");

            migrationBuilder.DropIndex(
                name: "IX_CrawlJobs_NavigationStrategyId",
                table: "CrawlJobs");

            migrationBuilder.DropIndex(
                name: "IX_CrawlJobs_NavigationStrategyId1",
                table: "CrawlJobs");

            migrationBuilder.DropIndex(
                name: "IX_CrawlJobs_ParentPromptId",
                table: "CrawlJobs");

            migrationBuilder.DropColumn(
                name: "AgentPoolId",
                table: "CrawlJobs");

            migrationBuilder.DropColumn(
                name: "AssignedAgentPoolId",
                table: "CrawlJobs");

            migrationBuilder.DropColumn(
                name: "ConversationId",
                table: "CrawlJobs");

            migrationBuilder.DropColumn(
                name: "CurrentNavigationStep",
                table: "CrawlJobs");

            migrationBuilder.DropColumn(
                name: "NavigationProgressJson",
                table: "CrawlJobs");

            migrationBuilder.DropColumn(
                name: "NavigationStrategyId",
                table: "CrawlJobs");

            migrationBuilder.DropColumn(
                name: "NavigationStrategyId1",
                table: "CrawlJobs");

            migrationBuilder.DropColumn(
                name: "ParentPromptId",
                table: "CrawlJobs");

            migrationBuilder.DropColumn(
                name: "ResultCount",
                table: "CrawlJobs");

            migrationBuilder.DropColumn(
                name: "SessionType",
                table: "CrawlJobs");

            migrationBuilder.UpdateData(
                table: "CrawlTemplates",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000001"),
                columns: new[] { "CreatedAt", "LastTestedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 10, 17, 14, 38, 11, 655, DateTimeKind.Utc).AddTicks(7758), new DateTime(2025, 10, 17, 14, 38, 11, 655, DateTimeKind.Utc).AddTicks(7748), new DateTime(2025, 10, 17, 14, 38, 11, 655, DateTimeKind.Utc).AddTicks(7758) });

            migrationBuilder.UpdateData(
                table: "CrawlTemplates",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000002"),
                columns: new[] { "CreatedAt", "LastTestedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 10, 17, 14, 38, 11, 655, DateTimeKind.Utc).AddTicks(8332), new DateTime(2025, 10, 17, 14, 38, 11, 655, DateTimeKind.Utc).AddTicks(8329), new DateTime(2025, 10, 17, 14, 38, 11, 655, DateTimeKind.Utc).AddTicks(8332) });

            migrationBuilder.UpdateData(
                table: "CrawlTemplates",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000003"),
                columns: new[] { "CreatedAt", "LastTestedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 10, 17, 14, 38, 11, 655, DateTimeKind.Utc).AddTicks(9035), new DateTime(2025, 10, 17, 14, 38, 11, 655, DateTimeKind.Utc).AddTicks(9031), new DateTime(2025, 10, 17, 14, 38, 11, 655, DateTimeKind.Utc).AddTicks(9036) });
        }
    }
}
