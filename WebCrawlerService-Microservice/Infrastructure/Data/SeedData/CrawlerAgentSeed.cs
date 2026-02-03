using WebCrawlerService.Domain.Entities;
using WebCrawlerService.Domain.Enums;

namespace WebCrawlerService.Infrastructure.Data.SeedData;

/// <summary>
/// Seed data for system crawler agents
/// Provides default agents for different crawling strategies
/// </summary>
public static class CrawlerAgentSeed
{
    public static List<CrawlerAgent> GetSeedAgents()
    {
        return new List<CrawlerAgent>
        {
            // Shopee Mobile API Agent
            new CrawlerAgent
            {
                Id = new Guid("10000000-0000-0000-0000-000000000001"),
                Name = "Shopee API Agent",
                Type = CrawlerType.AppSpecificApi,
                Status = AgentStatus.Available,
                MaxConcurrentJobs = 5,
                CurrentJobCount = 0,
                UserAgent = "Shopee/2.98.21 Android/11",
                MaxMemoryMB = 256,
                MaxCpuPercent = 50,
                ConfigurationJson = @"{
                    ""provider"": ""Shopee"",
                    ""apiVersion"": ""v4"",
                    ""baseUrl"": ""https://shopee.vn"",
                    ""supportedDomains"": [""shopee.vn"", ""shopee.com""],
                    ""capabilities"": [
                        ""product_details"",
                        ""reviews"",
                        ""ratings"",
                        ""shop_info"",
                        ""mobile_api"",
                        ""high_speed""
                    ],
                    ""features"": {
                        ""productDetails"": true,
                        ""reviews"": true,
                        ""search"": true,
                        ""shopInfo"": true
                    },
                    ""rateLimit"": {
                        ""requestsPerMinute"": 20,
                        ""burstSize"": 5
                    },
                    ""retry"": {
                        ""maxAttempts"": 3,
                        ""backoffMs"": 1000
                    }
                }",
                CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },

            // Universal HTTP Agent
            new CrawlerAgent
            {
                Id = new Guid("10000000-0000-0000-0000-000000000002"),
                Name = "HTTP Client Agent",
                Type = CrawlerType.Universal,
                Status = AgentStatus.Available,
                MaxConcurrentJobs = 10,
                CurrentJobCount = 0,
                UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36",
                MaxMemoryMB = 128,
                MaxCpuPercent = 40,
                ConfigurationJson = @"{
                    ""capabilities"": [
                        ""html_parsing"",
                        ""static_content"",
                        ""api_calls"",
                        ""headers_manipulation"",
                        ""cookies""
                    ],
                    ""timeout"": 30000,
                    ""followRedirects"": true,
                    ""maxRedirects"": 5,
                    ""compression"": true,
                    ""retry"": {
                        ""maxAttempts"": 3,
                        ""backoffMs"": 2000
                    }
                }",
                CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },

            // Playwright Browser Agent
            new CrawlerAgent
            {
                Id = new Guid("10000000-0000-0000-0000-000000000003"),
                Name = "Playwright Browser Agent",
                Type = CrawlerType.Playwright,
                Status = AgentStatus.Available,
                MaxConcurrentJobs = 3,
                CurrentJobCount = 0,
                UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36",
                MaxMemoryMB = 1024,
                MaxCpuPercent = 80,
                ConfigurationJson = @"{
                    ""capabilities"": [
                        ""javascript_execution"",
                        ""spa_support"",
                        ""screenshots"",
                        ""pdf_generation"",
                        ""user_interactions"",
                        ""network_interception"",
                        ""geolocation"",
                        ""device_emulation""
                    ],
                    ""browser"": ""chromium"",
                    ""headless"": true,
                    ""viewport"": {
                        ""width"": 1920,
                        ""height"": 1080
                    },
                    ""timeout"": 60000,
                    ""waitUntil"": ""networkidle"",
                    ""blockResources"": [""image"", ""font"", ""media""],
                    ""retry"": {
                        ""maxAttempts"": 2,
                        ""backoffMs"": 5000
                    }
                }",
                CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            }
        };
    }
}
