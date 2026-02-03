namespace WebCrawlerService.Domain.Enums
{
    public enum CrawlerType
    {
        HttpClient = 0,
        Selenium = 1,
        Playwright = 2,
        Scrapy = 3,
        Universal = 4,
        AppSpecificApi = 5,      // Mobile app API calls (Shopee, Lazada, etc.)
        MobileMcp = 6,           // Mobile MCP automation
        Crawl4AI = 7             // Python crawl4ai with Gemini LLM
    }
}