namespace WebCrawlerService.Domain.Enums
{
    public enum NavigationSessionType
    {
        Template = 0,    // Using existing template
        Continuous = 1,  // Learning/refining existing strategy
        NewSession = 2,  // Fresh start, no prior knowledge
        Guided = 3       // User provides step-by-step guidance
    }
}
