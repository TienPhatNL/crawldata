namespace UserService.Infrastructure.Configuration;

public class QuotaSyncSettings
{
    public const string SectionName = "QuotaSync";

    public int IntervalSeconds { get; set; } = 300;
    public int BatchSize { get; set; } = 200;
    public bool EnableAutomaticReset { get; set; } = true;
    public int ResetWindowDays { get; set; } = 30;
}
