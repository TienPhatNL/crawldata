using ClassroomService.Domain.Entities;
using ClassroomService.Domain.Enums;
using ClassroomService.Domain.Interfaces;
using ClassroomService.Domain.Constants;
using Microsoft.Extensions.Logging;

namespace ClassroomService.Infrastructure.Services;

public class AccessCodeService : IAccessCodeService
{
    private readonly ILogger<AccessCodeService> _logger;
    
    public AccessCodeService(ILogger<AccessCodeService> logger)
    {
        _logger = logger;
    }

    public string GenerateAccessCode(AccessCodeType type = AccessCodeType.AlphaNumeric)
    {
        return type switch
        {
            AccessCodeType.Numeric => GenerateNumericCode(),
            AccessCodeType.AlphaNumeric => GenerateAlphaNumericCode(),
            AccessCodeType.Words => GenerateWordBasedCode(),
            AccessCodeType.Custom => throw new InvalidOperationException("Custom codes must be provided by the user"),
            _ => GenerateAlphaNumericCode()
        };
    }

    public bool ValidateAccessCode(string code, Course course)
    {
        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(course.AccessCode))
        {
            return false;
        }

        // Check if code is expired
        if (IsAccessCodeExpired(course))
        {
            _logger.LogWarning(Messages.Logging.AccessCodeExpiredLog, course.Id);
            return false;
        }

        // Case-insensitive comparison
        var isValid = string.Equals(code.Trim(), course.AccessCode.Trim(), StringComparison.OrdinalIgnoreCase);
        
        if (isValid)
        {
            _logger.LogInformation(Messages.Logging.AccessCodeValidated, course.Id);
        }
        else
        {
            _logger.LogWarning(Messages.Logging.AccessCodeInvalid, course.Id);
        }

        return isValid;
    }

    public bool IsAccessCodeExpired(Course course)
    {
        if (course.AccessCodeExpiresAt == null)
        {
            return false; // No expiration set
        }

        return DateTime.UtcNow > course.AccessCodeExpiresAt;
    }

    public void RecordFailedAttempt(Course course)
    {
        course.AccessCodeAttempts++;
        course.LastAccessCodeAttempt = DateTime.UtcNow;
        
        _logger.LogWarning(Messages.Logging.AccessCodeAttemptFailed, 
            course.AccessCodeAttempts, course.Id);
    }

    public bool IsRateLimited(Course course)
    {
        if (course.LastAccessCodeAttempt == null)
        {
            return false;
        }

        var timeSinceLastAttempt = DateTime.UtcNow - course.LastAccessCodeAttempt.Value;
        
        // Reset attempts if outside rate limit window
        if (timeSinceLastAttempt.TotalMinutes > ValidationConstants.RateLimitWindowMinutes)
        {
            return false;
        }

        var isRateLimited = course.AccessCodeAttempts >= ValidationConstants.MaxAccessCodeAttemptsPerHour;
        
        if (isRateLimited)
        {
            _logger.LogWarning("Rate limit exceeded for course {CourseId}: {Attempts} attempts in last hour", 
                course.Id, course.AccessCodeAttempts);
        }

        return isRateLimited;
    }

    public bool IsValidAccessCodeFormat(string code, AccessCodeType type)
    {
        if (string.IsNullOrWhiteSpace(code))
            return false;

        return type switch
        {
            AccessCodeType.Numeric => IsValidNumericCode(code),
            AccessCodeType.AlphaNumeric => IsValidAlphaNumericCode(code),
            AccessCodeType.Words => IsValidWordBasedCode(code),
            AccessCodeType.Custom => IsValidCustomCode(code),
            _ => false
        };
    }

    private string GenerateNumericCode()
    {
        return Random.Shared.Next(100000, 999999).ToString();
    }

    private string GenerateAlphaNumericCode()
    {
        return new string(Enumerable.Repeat(AccessCodeConstants.AlphaNumericChars, AccessCodeConstants.DefaultAlphaNumericLength)
            .Select(s => s[Random.Shared.Next(s.Length)]).ToArray());
    }

    private string GenerateWordBasedCode()
    {
        var word1 = AccessCodeConstants.WordList[Random.Shared.Next(AccessCodeConstants.WordList.Length)];
        var word2 = AccessCodeConstants.WordList[Random.Shared.Next(AccessCodeConstants.WordList.Length)];
        var number = Random.Shared.Next(AccessCodeConstants.DefaultWordBasedMinNumber, AccessCodeConstants.DefaultWordBasedMaxNumber);
        return $"{word1}-{word2}-{number}";
    }

    private bool IsValidNumericCode(string code)
    {
        return code.Length >= ValidationConstants.MinNumericCodeLength 
            && code.Length <= ValidationConstants.MaxNumericCodeLength 
            && code.All(char.IsDigit);
    }

    private bool IsValidAlphaNumericCode(string code)
    {
        return code.Length >= ValidationConstants.MinAlphaNumericCodeLength 
            && code.Length <= ValidationConstants.MaxAlphaNumericCodeLength 
            && code.All(char.IsLetterOrDigit);
    }

    private bool IsValidWordBasedCode(string code)
    {
        // Format: word-word-number or similar patterns
        return code.Length >= ValidationConstants.MinWordBasedCodeLength 
            && code.Length <= ValidationConstants.MaxWordBasedCodeLength;
    }

    private bool IsValidCustomCode(string code)
    {
        return code.Length >= ValidationConstants.MinCustomCodeLength 
            && code.Length <= ValidationConstants.MaxCustomCodeLength
            && code.All(c => AccessCodeConstants.CustomAllowedChars.Contains(c));
    }
}