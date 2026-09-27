using System.Security.Cryptography;

namespace SmartHealthcare.API.Services;

public class PasswordService
{
    private const int SaltSize = 16;
    private const int KeySize = 32;
    private const int Iterations = 100_000;

    public string HashPassword(string password)
    {
        byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);

        byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            Iterations,
            HashAlgorithmName.SHA256,
            KeySize
        );

        return $"{Iterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }

    public bool VerifyPassword(string password, string storedHash)
    {
        try
        {
            string[] parts = storedHash.Split('.');

            if (parts.Length != 3)
            {
                return false;
            }

            int iterations = int.Parse(parts[0]);

            byte[] salt = Convert.FromBase64String(parts[1]);

            byte[] expectedHash = Convert.FromBase64String(parts[2]);

            byte[] actualHash = Rfc2898DeriveBytes.Pbkdf2(
                password,
                salt,
                iterations,
                HashAlgorithmName.SHA256,
                expectedHash.Length
            );

            return CryptographicOperations.FixedTimeEquals(
                actualHash,
                expectedHash
            );
        }
        catch
        {
            return false;
        }
    }
}