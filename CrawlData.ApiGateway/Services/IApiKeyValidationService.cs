namespace CrawlData.ApiGateway.Services
{
    public interface IApiKeyValidationService
    {
        Task<ApiKeyValidationResult> ValidateApiKeyAsync(string apiKey);
    }

    public class ApiKeyValidationResult
    {
        public bool IsValid { get; set; }
        public Guid? UserId { get; set; }
        public string? UserRole { get; set; }
        public string? SubscriptionTier { get; set; }
        public string? ErrorMessage { get; set; }
    }
}