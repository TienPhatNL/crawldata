using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace WebCrawlerService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CrawlerAgents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ConfigurationJson = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    UserAgent = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    MaxConcurrentJobs = table.Column<int>(type: "int", nullable: false),
                    CurrentJobCount = table.Column<int>(type: "int", nullable: false),
                    TotalJobsProcessed = table.Column<int>(type: "int", nullable: false),
                    SuccessfulJobs = table.Column<int>(type: "int", nullable: false),
                    FailedJobs = table.Column<int>(type: "int", nullable: false),
                    AverageProcessingTime = table.Column<double>(type: "float", nullable: false),
                    LastHealthCheck = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastHeartbeat = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastAssignedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    MaxMemoryMB = table.Column<int>(type: "int", nullable: false),
                    MaxCpuPercent = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifiedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CrawlerAgents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CrawlTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DomainPattern = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    RecommendedCrawler = table.Column<int>(type: "int", nullable: false),
                    ConfigurationJson = table.Column<string>(type: "text", nullable: false),
                    SampleUrls = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    PreviousVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsValidated = table.Column<bool>(type: "bit", nullable: false),
                    LastTestedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeprecatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastValidationError = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    RateLimitDelayMs = table.Column<int>(type: "int", nullable: false),
                    RequiresAuthentication = table.Column<bool>(type: "bit", nullable: false),
                    ApiEndpointPattern = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    MobileApiProvider = table.Column<int>(type: "int", nullable: true),
                    MobileApiConfigJson = table.Column<string>(type: "text", nullable: true),
                    UsageCount = table.Column<int>(type: "int", nullable: false),
                    SuccessRate = table.Column<double>(type: "float", nullable: false),
                    AverageExtractionTimeMs = table.Column<int>(type: "int", nullable: false),
                    Tags = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    IsSystemTemplate = table.Column<bool>(type: "bit", nullable: false),
                    IsPublic = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifiedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CrawlTemplates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CrawlTemplates_CrawlTemplates_PreviousVersionId",
                        column: x => x.PreviousVersionId,
                        principalTable: "CrawlTemplates",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "DomainPolicies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DomainPattern = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    PolicyType = table.Column<int>(type: "int", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    MaxRequestsPerMinute = table.Column<int>(type: "int", nullable: true),
                    MaxConcurrentRequests = table.Column<int>(type: "int", nullable: true),
                    DelayBetweenRequestsMs = table.Column<int>(type: "int", nullable: true),
                    MinimumTierRequired = table.Column<int>(type: "int", nullable: true),
                    AllowedRoles = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifiedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DomainPolicies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OutboxMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    OccurredOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ProcessedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Error = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    RetryCount = table.Column<int>(type: "int", nullable: false),
                    MaxRetries = table.Column<int>(type: "int", nullable: false),
                    NextRetryAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifiedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutboxMessages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CrawlJobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssignmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Urls = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    CrawlerType = table.Column<int>(type: "int", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FailedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    RetryCount = table.Column<int>(type: "int", nullable: false),
                    MaxRetries = table.Column<int>(type: "int", nullable: false),
                    NextRetryAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ConfigurationJson = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    TimeoutSeconds = table.Column<int>(type: "int", nullable: false),
                    FollowRedirects = table.Column<bool>(type: "bit", nullable: false),
                    ExtractImages = table.Column<bool>(type: "bit", nullable: false),
                    ExtractLinks = table.Column<bool>(type: "bit", nullable: false),
                    UserPrompt = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    TemplateId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ExtractionStrategyJson = table.Column<string>(type: "text", nullable: true),
                    CrawlerPreference = table.Column<int>(type: "int", nullable: false),
                    AutoSolveCaptcha = table.Column<bool>(type: "bit", nullable: false),
                    UseProxyRotation = table.Column<bool>(type: "bit", nullable: false),
                    UrlsProcessed = table.Column<int>(type: "int", nullable: false),
                    UrlsSuccessful = table.Column<int>(type: "int", nullable: false),
                    UrlsFailed = table.Column<int>(type: "int", nullable: false),
                    TotalContentSize = table.Column<long>(type: "bigint", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AssignedAgentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CrawlerAgentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifiedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CrawlJobs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CrawlJobs_CrawlTemplates_TemplateId",
                        column: x => x.TemplateId,
                        principalTable: "CrawlTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_CrawlJobs_CrawlerAgents_AssignedAgentId",
                        column: x => x.AssignedAgentId,
                        principalTable: "CrawlerAgents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_CrawlJobs_CrawlerAgents_CrawlerAgentId",
                        column: x => x.CrawlerAgentId,
                        principalTable: "CrawlerAgents",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "CrawlResults",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CrawlJobId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Url = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    HttpStatusCode = table.Column<int>(type: "int", nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ContentSize = table.Column<long>(type: "bigint", nullable: false),
                    Content = table.Column<string>(type: "text", nullable: true),
                    ContentHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    CrawledAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ResponseTimeMs = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Keywords = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Images = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    Links = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    ExtractedDataJson = table.Column<string>(type: "text", nullable: true),
                    PromptUsed = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    TemplateId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TemplateVersion = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ExtractionConfidence = table.Column<double>(type: "float", nullable: false),
                    LlmCost = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    CaptchasSolved = table.Column<int>(type: "int", nullable: false),
                    CaptchaCost = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    ProxyUsed = table.Column<bool>(type: "bit", nullable: false),
                    ScreenshotBase64 = table.Column<string>(type: "text", nullable: true),
                    ExtractionWarnings = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsSuccess = table.Column<bool>(type: "bit", nullable: false),
                    IsAnalyzed = table.Column<bool>(type: "bit", nullable: false),
                    AnalyzedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AnalysisResultId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifiedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CrawlResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CrawlResults_CrawlJobs_CrawlJobId",
                        column: x => x.CrawlJobId,
                        principalTable: "CrawlJobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "CrawlTemplates",
                columns: new[] { "Id", "ApiEndpointPattern", "AverageExtractionTimeMs", "ConfigurationJson", "CreatedAt", "CreatedBy", "DeprecatedAt", "Description", "DomainPattern", "IsActive", "IsPublic", "IsSystemTemplate", "IsValidated", "LastModifiedAt", "LastModifiedBy", "LastTestedAt", "LastValidationError", "MobileApiConfigJson", "MobileApiProvider", "Name", "PreviousVersionId", "RateLimitDelayMs", "RecommendedCrawler", "RequiresAuthentication", "SampleUrls", "SuccessRate", "Tags", "Type", "UpdatedAt", "UsageCount", "Version" },
                values: new object[,]
                {
                    { new Guid("10000000-0000-0000-0000-000000000001"), "/api/v4/item/get?itemid={itemId}&shopid={shopId}", 0, "{\"selectors\":{\"productName\":[\"h1.product-title\",\".product-name\",\"#product-name\"],\"price\":[\".product-price\",\"[data-price]\",\".price-text\"],\"originalPrice\":[\".original-price\",\".old-price\"],\"rating\":[\".product-rating\",\"[data-rating]\"],\"stock\":[\".stock-quantity\",\"[data-stock]\"],\"sold\":[\".sold-count\",\"[data-sold]\"],\"description\":[\".product-description\",\".description-content\"],\"images\":[\".product-image img\",\".gallery-image\"]},\"dynamicSelectors\":{},\"requiresJavaScript\":false,\"scrollToBottom\":false,\"waitForSelectors\":[\".product-title\",\".product-price\"],\"confidence\":0.95,\"estimatedTimeSeconds\":2}", new DateTime(2025, 10, 17, 14, 38, 11, 655, DateTimeKind.Utc).AddTicks(7758), null, null, "Extract product information from Shopee product pages including price, rating, specifications, and reviews summary", "*.shopee.vn/product/*,*.shopee.vn/*-i.*.*", true, true, true, true, null, null, new DateTime(2025, 10, 17, 14, 38, 11, 655, DateTimeKind.Utc).AddTicks(7748), null, "{\"Provider\":0,\"BaseUrl\":\"https://shopee.vn\",\"ApiVersion\":\"v4\",\"RequiredHeaders\":{\"User-Agent\":\"Shopee/2.98.21 Android/11\",\"X-API-Source\":\"pc\",\"X-Shopee-Language\":\"vi\",\"Accept\":\"application/json\"},\"DefaultParams\":{},\"SignatureAlgorithm\":null,\"RequiresSignature\":false,\"RequiresCookies\":false,\"Region\":\"VN\",\"RateLimitPerMinute\":20,\"RequiresProxy\":false}", 0, "Shopee Product Page", null, 500, 5, false, "https://shopee.vn/Ch%C4%83n-cotton-%C4%91%C5%A9i-3-l%E1%BB%9Bp-1m8x2m-m%E1%BB%81m-m%E1%BB%8Bn-ch%C4%83n-%C4%91%C5%A9i-x%C6%A1-%C4%91%E1%BA%ADu-l%C3%A0nh-i.29708084.23984073012;https://shopee.vn/product-example-i.123456.789012", 0.0, "shopee,product,ecommerce,vietnam,mobile-api", 1, new DateTime(2025, 10, 17, 14, 38, 11, 655, DateTimeKind.Utc).AddTicks(7758), 0, 1 },
                    { new Guid("10000000-0000-0000-0000-000000000002"), "/api/v4/item/get_ratings?itemid={itemId}&shopid={shopId}&limit={limit}&offset={offset}", 0, "{\"pagination\":{\"enabled\":true,\"limitPerPage\":20,\"maxPages\":50,\"offsetParameter\":\"offset\"},\"selectors\":{\"reviewText\":[\".review-comment\",\".comment-text\"],\"rating\":[\".review-rating\",\"[data-rating]\"],\"reviewerName\":[\".reviewer-name\",\".user-name\"],\"reviewDate\":[\".review-date\",\"[data-date]\"],\"helpful\":[\".helpful-count\",\"[data-helpful]\"],\"images\":[\".review-image img\"]},\"filters\":{\"all\":0,\"withComment\":1,\"withImage\":2,\"fiveStar\":5,\"fourStar\":4,\"threeStar\":3,\"twoStar\":2,\"oneStar\":1},\"confidence\":0.9,\"estimatedTimeSeconds\":3}", new DateTime(2025, 10, 17, 14, 38, 11, 655, DateTimeKind.Utc).AddTicks(8332), null, null, "Extract user reviews and ratings from Shopee products with pagination support", "*.shopee.vn/product/*,*.shopee.vn/*-i.*.*", true, true, true, true, null, null, new DateTime(2025, 10, 17, 14, 38, 11, 655, DateTimeKind.Utc).AddTicks(8329), null, "{\"Provider\":0,\"BaseUrl\":\"https://shopee.vn\",\"ApiVersion\":\"v4\",\"RequiredHeaders\":{\"User-Agent\":\"Shopee/2.98.21 Android/11\",\"X-API-Source\":\"pc\",\"X-Shopee-Language\":\"vi\",\"Accept\":\"application/json\"},\"DefaultParams\":{},\"SignatureAlgorithm\":null,\"RequiresSignature\":false,\"RequiresCookies\":false,\"Region\":\"VN\",\"RateLimitPerMinute\":20,\"RequiresProxy\":false}", 0, "Shopee Product Reviews", null, 500, 5, false, "https://shopee.vn/Ch%C4%83n-cotton-%C4%91%C5%A9i-3-l%E1%BB%9Bp-1m8x2m-m%E1%BB%81m-m%E1%BB%8Bn-ch%C4%83n-%C4%91%C5%A9i-x%C6%A1-%C4%91%E1%BA%ADu-l%C3%A0nh-i.29708084.23984073012", 0.0, "shopee,reviews,ratings,feedback,mobile-api", 1, new DateTime(2025, 10, 17, 14, 38, 11, 655, DateTimeKind.Utc).AddTicks(8332), 0, 1 },
                    { new Guid("10000000-0000-0000-0000-000000000003"), "/api/v4/search/search_items?keyword={keyword}&limit={limit}&offset={offset}", 0, "{\"pagination\":{\"enabled\":true,\"limitPerPage\":60,\"maxPages\":20,\"offsetParameter\":\"offset\"},\"selectors\":{\"items\":[\".search-result-item\",\".product-item\"],\"productName\":[\".product-title\",\".item-name\"],\"price\":[\".product-price\",\"[data-price]\"],\"sold\":[\".sold-count\",\"[data-sold]\"],\"rating\":[\".product-rating\"],\"shopName\":[\".shop-name\"],\"productUrl\":[\"a[href*=\\u0027-i.\\u0027]\"]},\"sortOptions\":{\"relevancy\":\"relevancy\",\"sales\":\"sales\",\"price_asc\":\"price\",\"price_desc\":\"price_desc\",\"latest\":\"ctime\"},\"confidence\":0.85,\"estimatedTimeSeconds\":2}", new DateTime(2025, 10, 17, 14, 38, 11, 655, DateTimeKind.Utc).AddTicks(9035), null, null, "Search for products on Shopee by keyword with filtering and sorting options", "*.shopee.vn/search*,*.shopee.vn/api/v4/search/*", true, true, true, true, null, null, new DateTime(2025, 10, 17, 14, 38, 11, 655, DateTimeKind.Utc).AddTicks(9031), null, "{\"Provider\":0,\"BaseUrl\":\"https://shopee.vn\",\"ApiVersion\":\"v4\",\"RequiredHeaders\":{\"User-Agent\":\"Shopee/2.98.21 Android/11\",\"X-API-Source\":\"pc\",\"X-Shopee-Language\":\"vi\",\"Accept\":\"application/json\"},\"DefaultParams\":{},\"SignatureAlgorithm\":null,\"RequiresSignature\":false,\"RequiresCookies\":false,\"Region\":\"VN\",\"RateLimitPerMinute\":20,\"RequiresProxy\":false}", 0, "Shopee Product Search", null, 500, 5, false, "https://shopee.vn/search?keyword=laptop;https://shopee.vn/search?keyword=dien%20thoai", 0.0, "shopee,search,discovery,listing,mobile-api", 1, new DateTime(2025, 10, 17, 14, 38, 11, 655, DateTimeKind.Utc).AddTicks(9036), 0, 1 }
                });

            migrationBuilder.InsertData(
                table: "CrawlerAgents",
                columns: new[] { "Id", "AverageProcessingTime", "ConfigurationJson", "CreatedAt", "CreatedBy", "CurrentJobCount", "FailedJobs", "LastAssignedAt", "LastHealthCheck", "LastHeartbeat", "LastModifiedAt", "LastModifiedBy", "MaxConcurrentJobs", "MaxCpuPercent", "MaxMemoryMB", "Name", "Status", "SuccessfulJobs", "TotalJobsProcessed", "Type", "UpdatedAt", "UserAgent" },
                values: new object[,]
                {
                    { new Guid("10000000-0000-0000-0000-000000000001"), 0.0, "{\n                    \"provider\": \"Shopee\",\n                    \"apiVersion\": \"v4\",\n                    \"baseUrl\": \"https://shopee.vn\",\n                    \"supportedDomains\": [\"shopee.vn\", \"shopee.com\"],\n                    \"capabilities\": [\n                        \"product_details\",\n                        \"reviews\",\n                        \"ratings\",\n                        \"shop_info\",\n                        \"mobile_api\",\n                        \"high_speed\"\n                    ],\n                    \"features\": {\n                        \"productDetails\": true,\n                        \"reviews\": true,\n                        \"search\": true,\n                        \"shopInfo\": true\n                    },\n                    \"rateLimit\": {\n                        \"requestsPerMinute\": 20,\n                        \"burstSize\": 5\n                    },\n                    \"retry\": {\n                        \"maxAttempts\": 3,\n                        \"backoffMs\": 1000\n                    }\n                }", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 0, 0, null, null, null, null, null, 5, 50, 256, "Shopee API Agent", 0, 0, 0, 5, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Shopee/2.98.21 Android/11" },
                    { new Guid("10000000-0000-0000-0000-000000000002"), 0.0, "{\n                    \"capabilities\": [\n                        \"html_parsing\",\n                        \"static_content\",\n                        \"api_calls\",\n                        \"headers_manipulation\",\n                        \"cookies\"\n                    ],\n                    \"timeout\": 30000,\n                    \"followRedirects\": true,\n                    \"maxRedirects\": 5,\n                    \"compression\": true,\n                    \"retry\": {\n                        \"maxAttempts\": 3,\n                        \"backoffMs\": 2000\n                    }\n                }", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 0, 0, null, null, null, null, null, 10, 40, 128, "HTTP Client Agent", 0, 0, 0, 4, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36" },
                    { new Guid("10000000-0000-0000-0000-000000000003"), 0.0, "{\n                    \"capabilities\": [\n                        \"javascript_execution\",\n                        \"spa_support\",\n                        \"screenshots\",\n                        \"pdf_generation\",\n                        \"user_interactions\",\n                        \"network_interception\",\n                        \"geolocation\",\n                        \"device_emulation\"\n                    ],\n                    \"browser\": \"chromium\",\n                    \"headless\": true,\n                    \"viewport\": {\n                        \"width\": 1920,\n                        \"height\": 1080\n                    },\n                    \"timeout\": 60000,\n                    \"waitUntil\": \"networkidle\",\n                    \"blockResources\": [\"image\", \"font\", \"media\"],\n                    \"retry\": {\n                        \"maxAttempts\": 2,\n                        \"backoffMs\": 5000\n                    }\n                }", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 0, 0, null, null, null, null, null, 3, 80, 1024, "Playwright Browser Agent", 0, 0, 0, 2, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_CrawlerAgents_Status",
                table: "CrawlerAgents",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_CrawlerAgents_Type",
                table: "CrawlerAgents",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_CrawlJobs_AssignedAgentId",
                table: "CrawlJobs",
                column: "AssignedAgentId");

            migrationBuilder.CreateIndex(
                name: "IX_CrawlJobs_CrawlerAgentId",
                table: "CrawlJobs",
                column: "CrawlerAgentId");

            migrationBuilder.CreateIndex(
                name: "IX_CrawlJobs_CreatedAt",
                table: "CrawlJobs",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_CrawlJobs_Status",
                table: "CrawlJobs",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_CrawlJobs_TemplateId",
                table: "CrawlJobs",
                column: "TemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_CrawlJobs_UserId",
                table: "CrawlJobs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_CrawlJobs_UserId_Status",
                table: "CrawlJobs",
                columns: new[] { "UserId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_CrawlResults_ContentHash",
                table: "CrawlResults",
                column: "ContentHash");

            migrationBuilder.CreateIndex(
                name: "IX_CrawlResults_CrawledAt",
                table: "CrawlResults",
                column: "CrawledAt");

            migrationBuilder.CreateIndex(
                name: "IX_CrawlResults_CrawlJobId",
                table: "CrawlResults",
                column: "CrawlJobId");

            migrationBuilder.CreateIndex(
                name: "IX_CrawlResults_Url",
                table: "CrawlResults",
                column: "Url");

            migrationBuilder.CreateIndex(
                name: "IX_CrawlTemplates_CreatedBy",
                table: "CrawlTemplates",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_CrawlTemplates_DomainPattern",
                table: "CrawlTemplates",
                column: "DomainPattern");

            migrationBuilder.CreateIndex(
                name: "IX_CrawlTemplates_IsActive",
                table: "CrawlTemplates",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_CrawlTemplates_MobileApiProvider",
                table: "CrawlTemplates",
                column: "MobileApiProvider");

            migrationBuilder.CreateIndex(
                name: "IX_CrawlTemplates_PreviousVersionId",
                table: "CrawlTemplates",
                column: "PreviousVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_CrawlTemplates_Type",
                table: "CrawlTemplates",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_DomainPolicies_IsActive",
                table: "DomainPolicies",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_DomainPolicies_PolicyType",
                table: "DomainPolicies",
                column: "PolicyType");

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_OccurredOnUtc",
                table: "OutboxMessages",
                column: "OccurredOnUtc");

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_ProcessedOnUtc",
                table: "OutboxMessages",
                column: "ProcessedOnUtc");

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_ProcessedOnUtc_NextRetryAtUtc_RetryCount",
                table: "OutboxMessages",
                columns: new[] { "ProcessedOnUtc", "NextRetryAtUtc", "RetryCount" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CrawlResults");

            migrationBuilder.DropTable(
                name: "DomainPolicies");

            migrationBuilder.DropTable(
                name: "OutboxMessages");

            migrationBuilder.DropTable(
                name: "CrawlJobs");

            migrationBuilder.DropTable(
                name: "CrawlTemplates");

            migrationBuilder.DropTable(
                name: "CrawlerAgents");
        }
    }
}
