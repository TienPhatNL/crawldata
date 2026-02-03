using UserService.Domain.Common;

namespace UserService.Domain.Entities;

/// <summary>
/// Represents an allowed email domain for auto-creating student accounts during import
/// </summary>
public class AllowedEmailDomain : BaseEntity
{
    /// <summary>
    /// Email domain pattern (e.g., ".edu", ".ac.uk", "@university.edu")
    /// </summary>
    public string Domain { get; set; } = null!;

    /// <summary>
    /// Description of the domain purpose
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Whether this domain is currently active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Whether to allow subdomains (e.g., .edu matches @cs.university.edu)
    /// </summary>
    public bool AllowSubdomains { get; set; } = true;
}
