using System.ComponentModel.DataAnnotations;

namespace Infrastructure.Options;

/// <summary>
/// Strongly-typed configuration for the outbound SMTP mail service.
/// Bound from the <c>Smtp</c> section in appsettings.json.
///
/// <para>
/// None of these settings are required to run the application locally.
/// When <see cref="Host"/> is absent or empty, the DI container registers
/// <c>LoggingEmailService</c> instead, which logs emails rather than sending them.
/// </para>
///
/// <para>Example appsettings entry (add to user-secrets in development):</para>
/// <code>
/// "Smtp": {
///   "Host": "smtp.sendgrid.net",
///   "Port": 587,
///   "UseSsl": true,
///   "Username": "apikey",
///   "Password": "&lt;sendgrid-api-key&gt;",
///   "FromAddress": "noreply@yourdomain.com",
///   "FromName": "YourApp"
/// }
/// </code>
/// </summary>
public sealed class SmtpOptions
{
    /// <summary>SMTP server hostname. Empty string means SMTP is not configured.</summary>
    public string Host { get; init; } = string.Empty;

    /// <summary>SMTP server port. Defaults to 587 (STARTTLS).</summary>
    [Range(1, 65535, ErrorMessage = "Smtp:Port must be between 1 and 65535.")]
    public int Port { get; init; } = 587;

    /// <summary>
    /// When <c>true</c>, the connection is wrapped in TLS/SSL from the start (SMTPS on port 465).
    /// When <c>false</c>, STARTTLS upgrade is used (common with port 587).
    /// Defaults to <c>false</c>.
    /// </summary>
    public bool UseSsl { get; init; }

    /// <summary>SMTP authentication username. Leave blank for unauthenticated relay.</summary>
    public string? Username { get; init; }

    /// <summary>SMTP authentication password. Store in user-secrets or Key Vault — never commit.</summary>
    public string? Password { get; init; }

    /// <summary>
    /// The email address shown in the <c>From</c> header.
    /// Required only when <see cref="Host"/> is set.
    /// </summary>
    public string FromAddress { get; init; } = string.Empty;

    /// <summary>The display name shown alongside <see cref="FromAddress"/>. Defaults to "DotNetStarterKitv2".</summary>
    public string FromName { get; init; } = "DotNetStarterKitv2";

    /// <summary>
    /// Returns <c>true</c> when <see cref="Host"/> and <see cref="FromAddress"/> are both non-empty,
    /// indicating that real SMTP delivery is configured.
    /// </summary>
    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(Host) &&
        !string.IsNullOrWhiteSpace(FromAddress);
}
