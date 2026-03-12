namespace Application.Interfaces;

/// <summary>
/// Service for hashing and verifying passwords.
/// Abstraction allows multiple implementations (bcrypt, PBKDF2, etc.).
/// </summary>
public interface IPasswordHasher
{
    /// <summary>
    /// Hashes a plaintext password using a secure algorithm.
    /// </summary>
    /// <param name="password">The plaintext password to hash</param>
    /// <returns>A hashed password string</returns>
    string HashPassword(string password);

    /// <summary>
    /// Verifies a plaintext password against a hash.
    /// </summary>
    /// <param name="password">The plaintext password to verify</param>
    /// <param name="hash">The hash to compare against</param>
    /// <returns>True if the password matches; otherwise false</returns>
    bool VerifyPassword(string password, string hash);
}
