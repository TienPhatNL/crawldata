namespace UserService.Domain.Enums;

public enum UsageType
{
    CrawlRequest = 0,
    DataExtraction = 1,
    ReportGeneration = 2,
    ApiCall = 3,
    FileUpload = 4,
    DataExport = 5
}