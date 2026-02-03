using System;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;

namespace UserService.Application.Features.Payments.Helpers;

internal static class PaymentConfirmationTokenHelper
{
    private const int TokenByteLength = 32;

    internal static (string rawToken, string hashedToken) CreateToken()
    {
        Span<byte> buffer = stackalloc byte[TokenByteLength];
        RandomNumberGenerator.Fill(buffer);
        var rawToken = WebEncoders.Base64UrlEncode(buffer);
        var hashedToken = ComputeHash(rawToken);
        return (rawToken, hashedToken);
    }

    internal static string ComputeHash(string token)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(token);
        var hashBytes = sha256.ComputeHash(bytes);
        return Convert.ToHexString(hashBytes);
    }

    internal static bool VerifyToken(string token, string? expectedHash)
    {
        if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(expectedHash))
        {
            return false;
        }

        var computedHashBytes = Convert.FromHexString(ComputeHash(token));
        var expectedHashBytes = Convert.FromHexString(expectedHash);
        return CryptographicOperations.FixedTimeEquals(computedHashBytes, expectedHashBytes);
    }
}
