namespace WebCrawlerService.Application.Services;

/// <summary>
/// Service for solving CAPTCHAs using third-party APIs (CapSolver, 2Captcha, etc.)
/// </summary>
public interface ICaptchaSolverService
{
    /// <summary>
    /// Detect if page contains CAPTCHA
    /// </summary>
    Task<bool> DetectCaptchaAsync(
        string htmlContent,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Solve reCAPTCHA v2
    /// </summary>
    Task<string> SolveRecaptchaV2Async(
        string siteKey,
        string pageUrl,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Solve reCAPTCHA v3
    /// </summary>
    Task<string> SolveRecaptchaV3Async(
        string siteKey,
        string pageUrl,
        string action,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Solve hCaptcha
    /// </summary>
    Task<string> SolveHCaptchaAsync(
        string siteKey,
        string pageUrl,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get estimated cost for solving CAPTCHA
    /// </summary>
    decimal GetEstimatedCost(string captchaType);

    /// <summary>
    /// Get service balance
    /// </summary>
    Task<decimal> GetBalanceAsync(CancellationToken cancellationToken = default);
}
