namespace ClassroomService.Application.Common.Interfaces;

/// <summary>
/// Interface for accessing current user information from JWT token
/// </summary>
public interface ICurrentUserService
{
    /// <summary>
    /// Gets the current user ID from the JWT token
    /// </summary>
    Guid? UserId { get; }

    /// <summary>
    /// Gets the current user's email from the JWT token
    /// </summary>
    string? Email { get; }

    /// <summary>
    /// Gets the current user's role from the JWT token
    /// </summary>
    string? Role { get; }

    /// <summary>
    /// Gets the current user's full name from the JWT token
    /// </summary>
    string? FullName { get; }

    /// <summary>
    /// Checks if the current user is authenticated
    /// </summary>
    bool IsAuthenticated { get; }

    /// <summary>
    /// Checks if the current user has a specific role
    /// </summary>
    /// <param name="role">The role to check</param>
    /// <returns>True if user has the role</returns>
    bool IsInRole(string role);
}