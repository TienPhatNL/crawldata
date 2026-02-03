namespace UserService.Application.Common.Exceptions;

public abstract class UserServiceException : Exception
{
    protected UserServiceException(string message) : base(message)
    {
    }

    protected UserServiceException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

public class UserNotFoundException : UserServiceException
{
    public UserNotFoundException(Guid userId) 
        : base($"User with ID '{userId}' was not found.")
    {
    }

    public UserNotFoundException(string email) 
        : base($"User with email '{email}' was not found.")
    {
    }
}

public class InvalidCredentialsException : UserServiceException
{
    public InvalidCredentialsException() 
        : base("Invalid email or password.")
    {
    }
}

public class UserAlreadyExistsException : UserServiceException
{
    public string Email { get; }

    public UserAlreadyExistsException(string email) 
        : base($"User with email '{email}' already exists.")
    {
        Email = email;
    }
}

public class EmailNotConfirmedException : UserServiceException
{
    public Guid UserId { get; }

    public EmailNotConfirmedException(Guid userId) 
        : base($"Email for user '{userId}' has not been confirmed.")
    {
        UserId = userId;
    }
}

public class UserSuspendedException : UserServiceException
{
    public Guid UserId { get; }

    public UserSuspendedException(Guid userId) 
        : base($"User '{userId}' account has been suspended.")
    {
        UserId = userId;
    }
}

public class QuotaExceededException : UserServiceException
{
    public Guid UserId { get; }
    public string QuotaType { get; }

    public QuotaExceededException(Guid userId, string quotaType) 
        : base($"User '{userId}' has exceeded their {quotaType} quota.")
    {
        UserId = userId;
        QuotaType = quotaType;
    }
}

public class InvalidApiKeyException : UserServiceException
{
    public InvalidApiKeyException() 
        : base("Invalid or expired API key.")
    {
    }

    public InvalidApiKeyException(string keyId) 
        : base($"API key '{keyId}' is invalid or expired.")
    {
    }
}

public class SubscriptionRequiredException : UserServiceException
{
    public Guid UserId { get; }
    public string RequiredTier { get; }

    public SubscriptionRequiredException(Guid userId, string requiredTier) 
        : base($"User '{userId}' requires '{requiredTier}' subscription to access this feature.")
    {
        UserId = userId;
        RequiredTier = requiredTier;
    }
}

public class InvalidTokenException : UserServiceException
{
    public InvalidTokenException(string tokenType) 
        : base($"Invalid or expired {tokenType} token.")
    {
    }
}

public class PasswordResetTokenExpiredException : UserServiceException
{
    public PasswordResetTokenExpiredException() 
        : base("Password reset token has expired.")
    {
    }
}

public class UserProfileUpdateException : UserServiceException
{
    public Guid UserId { get; }

    public UserProfileUpdateException(Guid userId, string reason) 
        : base($"Failed to update profile for user '{userId}': {reason}")
    {
        UserId = userId;
    }
}

public class DuplicateApiKeyException : UserServiceException
{
    public string KeyName { get; }

    public DuplicateApiKeyException(string keyName) 
        : base($"API key with name '{keyName}' already exists.")
    {
        KeyName = keyName;
    }
}