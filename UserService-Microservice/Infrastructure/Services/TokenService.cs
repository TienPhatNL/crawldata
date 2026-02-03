using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace UserService.Infrastructure.Services;

public class TokenService : ITokenService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<TokenService> _logger;
    private readonly string _secretKey;

    public TokenService(IConfiguration configuration, ILogger<TokenService> logger)
    {
        _configuration = configuration;
        _logger = logger;
        _secretKey = _configuration["Jwt:SecretKey"] ?? throw new InvalidOperationException("JWT Secret Key not configured");
    }

    public string GenerateEmailVerificationToken(Guid userId)
    {
        var payload = new TokenPayload
        {
            UserId = userId,
            TokenType = "email_verification",
            ExpiresAt = DateTime.UtcNow.AddHours(24) // 24-hour expiry
        };

        return GenerateToken(payload);
    }

    public string GeneratePasswordResetToken(Guid userId)
    {
        var payload = new TokenPayload
        {
            UserId = userId,
            TokenType = "password_reset",
            ExpiresAt = DateTime.UtcNow.AddHours(1) // 1-hour expiry
        };

        var token = GenerateToken(payload);
        _logger.LogDebug("Generated password reset token for user {UserId}, expires at {ExpiresAt}", userId, payload.ExpiresAt);
        return token;
    }

    public Guid? ValidateEmailVerificationToken(string token)
    {
        var payload = ValidateToken(token);
        
        if (payload == null || payload.TokenType != "email_verification")
        {
            _logger.LogWarning("Invalid email verification token provided");
            return null;
        }

        if (payload.ExpiresAt < DateTime.UtcNow)
        {
            _logger.LogWarning("Expired email verification token provided for user {UserId}", payload.UserId);
            return null;
        }

        return payload.UserId;
    }

    public Guid? ValidatePasswordResetToken(string token)
    {
        var payload = ValidateToken(token);

        if (payload == null || payload.TokenType != "password_reset")
        {
            _logger.LogWarning("Invalid password reset token provided - token payload invalid or wrong type");
            return null;
        }

        if (payload.ExpiresAt < DateTime.UtcNow)
        {
            _logger.LogWarning("Expired password reset token provided for user {UserId}. Token expired at {ExpiresAt}, current time is {CurrentTime}",
                payload.UserId, payload.ExpiresAt, DateTime.UtcNow);
            return null;
        }

        _logger.LogDebug("Password reset token validation successful for user {UserId}", payload.UserId);
        return payload.UserId;
    }

    private string GenerateToken(TokenPayload payload)
    {
        try
        {
            var json = JsonSerializer.Serialize(payload);
            var bytes = Encoding.UTF8.GetBytes(json);
            
            // Encrypt the payload
            var encryptedBytes = EncryptBytes(bytes);
            
            // Return base64 encoded token
            return Convert.ToBase64String(encryptedBytes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate token for user {UserId}", payload.UserId);
            throw;
        }
    }

    private TokenPayload? ValidateToken(string token)
    {
        try
        {
            var encryptedBytes = Convert.FromBase64String(token);
            var decryptedBytes = DecryptBytes(encryptedBytes);
            var json = Encoding.UTF8.GetString(decryptedBytes);
            
            return JsonSerializer.Deserialize<TokenPayload>(json);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to validate token");
            return null;
        }
    }

    private byte[] EncryptBytes(byte[] data)
    {
        using var aes = Aes.Create();
        aes.Key = DeriveKey(_secretKey, 32); // 256-bit key
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;
        aes.GenerateIV();

        using var encryptor = aes.CreateEncryptor();
        using var ms = new MemoryStream();
        
        // Write IV first
        ms.Write(aes.IV, 0, aes.IV.Length);
        
        // Encrypt data
        using var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write);
        cs.Write(data, 0, data.Length);
        cs.FlushFinalBlock();
        
        return ms.ToArray();
    }

    private byte[] DecryptBytes(byte[] encryptedData)
    {
        using var aes = Aes.Create();
        aes.Key = DeriveKey(_secretKey, 32);
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        // Extract IV from the beginning of encrypted data
        var iv = new byte[16]; // AES block size is 16 bytes
        Array.Copy(encryptedData, 0, iv, 0, iv.Length);
        aes.IV = iv;

        // Get the actual encrypted data
        var cipherText = new byte[encryptedData.Length - iv.Length];
        Array.Copy(encryptedData, iv.Length, cipherText, 0, cipherText.Length);

        using var decryptor = aes.CreateDecryptor();
        using var ms = new MemoryStream(cipherText);
        using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
        using var result = new MemoryStream();
        
        cs.CopyTo(result);
        return result.ToArray();
    }

    private static byte[] DeriveKey(string secret, int keyLength)
    {
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(secret));
        
        if (keyLength <= hash.Length)
        {
            var key = new byte[keyLength];
            Array.Copy(hash, key, keyLength);
            return key;
        }
        
        // If we need more bytes, concatenate multiple hashes
        var result = new byte[keyLength];
        var iterations = (keyLength + hash.Length - 1) / hash.Length;
        
        for (int i = 0; i < iterations; i++)
        {
            var iterationHash = sha256.ComputeHash(Encoding.UTF8.GetBytes(secret + i));
            var copyLength = Math.Min(iterationHash.Length, keyLength - i * hash.Length);
            Array.Copy(iterationHash, 0, result, i * hash.Length, copyLength);
        }
        
        return result;
    }

    private class TokenPayload
    {
        public Guid UserId { get; set; }
        public string TokenType { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
    }
}