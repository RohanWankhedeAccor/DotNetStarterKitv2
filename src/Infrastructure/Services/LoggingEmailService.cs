using Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

/// <summary>
/// Development / test email service that logs the email rather than delivering it.
///
/// <para>
/// Registered when <c>Smtp:Host</c> is absent from configuration, allowing the
/// application to run locally and in CI without any SMTP server. Emails are
/// written to the application log at <c>Information</c> level so developers can
/// verify that the correct messages are triggered.
/// </para>
///
/// <para>
/// To swap in real delivery: set <c>Smtp:Host</c> and <c>Smtp:FromAddress</c>
/// in configuration. The DI container will then register <see cref="SmtpEmailService"/>
/// automatically — no other code changes are required.
/// </para>
/// </summary>
public sealed class LoggingEmailService : IEmailService
{
    private readonly ILogger<LoggingEmailService> _logger;

    /// <summary>
    /// Initializes the service with the application logger.
    /// </summary>
    public LoggingEmailService(ILogger<LoggingEmailService> logger) => _logger = logger;

    /// <inheritdoc />
    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        _logger.LogInformation(
            "[EMAIL - NOT SENT] To: {To} | Subject: {Subject} | Html: {IsHtml} | Body: {Body}",
            message.To,
            message.Subject,
            message.IsHtml,
            message.Body);

        return Task.CompletedTask;
    }
}
