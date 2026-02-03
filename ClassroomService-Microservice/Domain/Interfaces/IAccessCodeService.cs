using ClassroomService.Domain.Entities;
using ClassroomService.Domain.Enums;

namespace ClassroomService.Domain.Interfaces;

public interface IAccessCodeService
{
    string GenerateAccessCode(AccessCodeType type = AccessCodeType.AlphaNumeric);
    bool ValidateAccessCode(string code, Course course);
    bool IsAccessCodeExpired(Course course);
    void RecordFailedAttempt(Course course);
    bool IsRateLimited(Course course);
    bool IsValidAccessCodeFormat(string code, AccessCodeType type);
}