using ClassroomService.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ClassroomService.Infrastructure.Services;

/// <summary>
/// Service for generating unique course codes
/// </summary>
public class CourseUniqueCodeService : ICourseUniqueCodeService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly Random _random = new Random();
    private const string Characters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
    private const int CodeLength = 6;

    public CourseUniqueCodeService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    /// <summary>
    /// Generates a unique 6-character alphanumeric code for a course
    /// </summary>
    public async Task<string> GenerateUniqueCodeAsync(CancellationToken cancellationToken = default)
    {
        const int maxAttempts = 10;
        
        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            var code = GenerateRandomCode();
            
            if (await IsCodeAvailableAsync(code, cancellationToken))
            {
                return code;
            }
        }
        
        // If we couldn't generate a unique code after max attempts, throw exception
        throw new InvalidOperationException($"Failed to generate unique course code after {maxAttempts} attempts");
    }

    /// <summary>
    /// Validates if a unique code is already in use
    /// </summary>
    public async Task<bool> IsCodeAvailableAsync(string uniqueCode, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(uniqueCode))
        {
            return false;
        }

        var existingCourse = await _unitOfWork.Courses.GetAsync(
            predicate: c => c.UniqueCode == uniqueCode,
            cancellationToken: cancellationToken);

        return existingCourse == null;
    }

    /// <summary>
    /// Generates a random 6-character alphanumeric code
    /// </summary>
    private string GenerateRandomCode()
    {
        var code = new char[CodeLength];
        
        for (int i = 0; i < CodeLength; i++)
        {
            code[i] = Characters[_random.Next(Characters.Length)];
        }
        
        return new string(code);
    }
}
