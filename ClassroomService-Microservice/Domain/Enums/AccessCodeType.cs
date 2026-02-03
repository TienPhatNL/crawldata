namespace ClassroomService.Domain.Enums;

public enum AccessCodeType
{
    Numeric = 1,        // 123456
    AlphaNumeric = 2,   // ABC123
    Words = 3,          // happy-cat-123
    Custom = 4          // lecturer-defined
}