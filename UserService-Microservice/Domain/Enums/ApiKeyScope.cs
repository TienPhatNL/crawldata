namespace UserService.Domain.Enums;

public enum ApiKeyScope
{
    ReadUser = 0,
    WriteUser = 1,
    ReadCrawl = 2,
    WriteCrawl = 3,
    ReadReports = 4,
    WriteReports = 5,
    AdminAccess = 6,
    FullAccess = 7
}