using Application.Interfaces;
using BCrypt.Net;

namespace Infrastructure.Identity;

/// <summary>
/// Implementation of <see cref="IPasswordHasher"/> using bcrypt for secure, salted password hashing.
/// </summary>
public class PasswordHasher : IPasswordHasher
{
    private const int WorkFactor = 12; // bcrypt work factor (higher = slower, more secure)

    /// <inheritdoc />
    public string HashPassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
            throw new ArgumentException("Password cannot be null or empty.", nameof(password));

        return BCrypt.Net.BCrypt.HashPassword(password, workFactor: WorkFactor);
    }

    /// <inheritdoc />
    public bool VerifyPassword(string password, string hash)
    {
        if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(hash))
            return false;

        try
        {
            return BCrypt.Net.BCrypt.Verify(password, hash);
        }
        catch
        {
            return false;
        }
    }
}
