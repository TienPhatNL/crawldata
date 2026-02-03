namespace WebCrawlerService.Domain.Enums;

/// <summary>
/// Mobile app API provider types
/// </summary>
public enum MobileApiProvider
{
    /// <summary>
    /// Shopee mobile app API
    /// </summary>
    Shopee = 0,

    /// <summary>
    /// Lazada mobile app API
    /// </summary>
    Lazada = 1,

    /// <summary>
    /// Tiki mobile app API
    /// </summary>
    Tiki = 2,

    /// <summary>
    /// Sendo mobile app API
    /// </summary>
    Sendo = 3,

    /// <summary>
    /// Generic/unknown mobile app API
    /// </summary>
    Generic = 99
}
