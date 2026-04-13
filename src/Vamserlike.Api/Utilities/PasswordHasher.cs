using System.Security.Cryptography;

namespace Vamserlike.Api.Utilities;

public static class PasswordHasher
{
    public static string Hash(string password)
    {
        byte[] salt = RandomNumberGenerator.GetBytes(16);

        using var pbkdf2 = new Rfc2898DeriveBytes(
            password,
            salt,
            100_000,
            HashAlgorithmName.SHA256);

        byte[] hash = pbkdf2.GetBytes(32);

        return $"{Convert.ToBase64String(salt)}:{Convert.ToBase64String(hash)}";
    }

    public static bool Verify(string password, string storedHash)
    {
        var parts = storedHash.Split(':', 2);

        if (parts.Length != 2)
        {
            return false;
        }

        byte[] salt = Convert.FromBase64String(parts[0]);
        byte[] expectedHash = Convert.FromBase64String(parts[1]);

        using var pbkdf2 = new Rfc2898DeriveBytes(
            password,
            salt,
            100_000,
            HashAlgorithmName.SHA256);

        byte[] actualHash = pbkdf2.GetBytes(32);

        return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
    }
}