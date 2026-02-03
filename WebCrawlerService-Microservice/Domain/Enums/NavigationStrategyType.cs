namespace WebCrawlerService.Domain.Enums
{
    public enum NavigationStrategyType
    {
        Template = 0,   // Pre-defined by user/admin
        Learned = 1,    // AI-learned from successful crawl
        Hybrid = 2,     // Template refined by AI
        OneTime = 3     // Used once, not saved for reuse
    }
}
