using System.Net;
using System.Net.Mail;
using Application.Interfaces;
using Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Services;

/// <summary>
/// Production email service that delivers messages via SMTP using
/// <see cref="System.Net.Mail.SmtpClient"/> (built-in, no extra packages).
///
/// <para>
/// Registered when <c>Smtp:Host</c> and <c>Smtp:FromAddress</c> are both present
/// in configuration (see <c>InfrastructureServiceExtensions</c>).
/// In environments where SMTP is not configured, <see cref="LoggingEmailService"/>
/// is registered instead so the application still starts cleanly.
/// </para>
///
/// <para><b>Security note</b>: Never commit SMTP credentials to source control.
/// Use <c>dotnet user-secrets</c> in development and Azure Key Vault in production.</para>
/// </summary>
public sealed class SmtpEmailService : IEmailService
{
    private readonly SmtpOptions _options;
    private readonly ILogger<SmtpEmailService> _logger;

    /// <summary>
    /// Initializes the service with resolved SMTP configuration.
    /// </summary>
    public SmtpEmailService(IOptions<SmtpOptions> options, ILogger<SmtpEmailService> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        using var client = BuildClient();
        using var mailMessage = BuildMailMessage(message);

        _logger.LogInformation(
            "Sending email to {To} via {Host}:{Port} — Subject: {Subject}",
            message.To, _options.Host, _options.Port, message.Subject);

        await client.SendMailAsync(mailMessage, cancellationToken);

        _logger.LogInformation("Email sent successfully to {To}", message.To);
    }

    private SmtpClient BuildClient()
    {
        var client = new SmtpClient(_options.Host, _options.Port)
        {
            EnableSsl = _options.UseSsl,
            DeliveryMethod = SmtpDeliveryMethod.Network,
        };

        if (!string.IsNullOrWhiteSpace(_options.Username))
        {
            client.UseDefaultCredentials = false;
            client.Credentials = new NetworkCredential(_options.Username, _options.Password);
        }

        return client;
    }

    private MailMessage BuildMailMessage(EmailMessage message)
    {
        return new MailMessage
        {
            From = new MailAddress(_options.FromAddress, _options.FromName),
            Subject = message.Subject,
            Body = message.Body,
            IsBodyHtml = message.IsHtml,
            To = { message.To },
        };
    }
}
