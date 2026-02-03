using StackExchange.Redis;
using System.Text.Json;

namespace CrawlData.ApiGateway.Services
{
    public class ApiKeyValidationService : IApiKeyValidationService
    {
        private readonly IDatabase _database;
        private readonly ILogger<ApiKeyValidationService> _logger;

        public ApiKeyValidationService(
            IConnectionMultiplexer redis,
            ILogger<ApiKeyValidationService> logger)
        {
            _database = redis.GetDatabase();
            _logger = logger;
        }

        public async Task<ApiKeyValidationResult> ValidateApiKeyAsync(string apiKey)
        {
            try
            {
                // Hash the API key to match stored format
                var hashedKey = BCrypt.Net.BCrypt.HashPassword(apiKey);
                
                // Try to get from cache first
                var cachedResult = await _database.StringGetAsync($"apikey:{apiKey}");
                if (cachedResult.HasValue)
                {
                    var cached = JsonSerializer.Deserialize<ApiKeyValidationResult>(cachedResult!);
                    if (cached != null)
                    {
                        _logger.LogDebug("API key validation result retrieved from cache");
                        return cached;
                    }
                }

                // TODO: In a real implementation, this would query the user service
                // For now, return a mock validation result
                var result = new ApiKeyValidationResult
                {
                    IsValid = true, // This should be determined by actual validation
                    UserId = Guid.NewGuid(), // This should come from the database
                    UserRole = "PaidUser", // This should come from the database
                    SubscriptionTier = "Pro" // This should come from the database
                };

                // Cache the result for 5 minutes
                await _database.StringSetAsync(
                    $"apikey:{apiKey}", 
                    JsonSerializer.Serialize(result),
                    TimeSpan.FromMinutes(5));

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating API key");
                return new ApiKeyValidationResult
                {
                    IsValid = false,
                    ErrorMessage = "Internal error during API key validation"
                };
            }
        }
    }
}