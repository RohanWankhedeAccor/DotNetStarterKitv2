namespace Application.Interfaces;

/// <summary>
/// Represents an outbound email message.
/// </summary>
/// <param name="To">Recipient email address.</param>
/// <param name="Subject">Email subject line.</param>
/// <param name="Body">Email body content.</param>
/// <param name="IsHtml">
/// When <c>true</c>, <paramref name="Body"/> is rendered as HTML.
/// Defaults to <c>false</c> (plain text).
/// </param>
public sealed record EmailMessage(
    string To,
    string Subject,
    string Body,
    bool IsHtml = false);

/// <summary>
/// Abstraction for sending outbound emails.
///
/// <para>
/// Handlers depend on this interface rather than any specific mail library,
/// keeping the Application layer free of Infrastructure concerns.
/// The Infrastructure layer provides two implementations:
/// <list type="bullet">
///   <item><c>SmtpEmailService</c> — real SMTP delivery (production).</item>
///   <item><c>LoggingEmailService</c> — logs the email without sending (development/test).</item>
/// </list>
/// The correct implementation is selected at startup based on whether
/// <c>Smtp:Host</c> is present in configuration.
/// </para>
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Sends the specified <paramref name="message"/> asynchronously.
    /// </summary>
    /// <param name="message">The email to send. Must not be <c>null</c>.</param>
    /// <param name="cancellationToken">
    /// Token used to cancel the send operation.
    /// </param>
    /// <returns>A <see cref="Task"/> that completes when the message has been sent (or logged).</returns>
    Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default);
}
