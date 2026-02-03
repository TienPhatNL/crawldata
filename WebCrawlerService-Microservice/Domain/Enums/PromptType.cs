namespace WebCrawlerService.Domain.Enums
{
    public enum PromptType
    {
        Crawl = 0,          // "Crawl all iPhone prices"
        Analysis = 1,       // "What's the average price?"
        Visualization = 2,  // "Create a bar chart"
        Navigation = 3      // "Go to category A and collect all pages"
    }
}
