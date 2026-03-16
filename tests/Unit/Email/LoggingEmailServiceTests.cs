using Application.Interfaces;
using FluentAssertions;
using Infrastructure.Services;
using Microsoft.Extensions.Logging;

namespace Unit.Email;

/// <summary>
/// Unit tests for <see cref="LoggingEmailService"/>.
/// Verifies the contract: emails are accepted without throwing and the log adapter
/// records the email content rather than delivering it.
/// </summary>
public sealed class LoggingEmailServiceTests
{
    // Minimal in-test ILogger fake — avoids adding Microsoft.Extensions.Logging.Testing
    // as a package dependency just for capturing log output.
    private sealed class CapturingLogger<T> : ILogger<T>
    {
        private readonly List<string> _messages = new();

        public IReadOnlyList<string> Messages => _messages;

        public bool IsEnabled(LogLevel logLevel) => true;

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
            => _messages.Add(formatter(state, exception));
    }

    private readonly CapturingLogger<LoggingEmailService> _logger = new();
    private readonly LoggingEmailService _sut;

    public LoggingEmailServiceTests()
    {
        _sut = new LoggingEmailService(_logger);
    }

    [Fact]
    public async Task SendAsync_WithPlainTextMessage_CompletesSuccessfully()
    {
        var message = new EmailMessage("user@example.com", "Hello", "Body text");

        await _sut.SendAsync(message);

        // No exception means the contract is satisfied.
    }

    [Fact]
    public async Task SendAsync_WithHtmlMessage_CompletesSuccessfully()
    {
        var message = new EmailMessage("user@example.com", "Welcome!", "<h1>Welcome</h1>", IsHtml: true);

        await _sut.SendAsync(message);
    }

    [Fact]
    public async Task SendAsync_LogsRecipientAndSubject()
    {
        var message = new EmailMessage("alice@example.com", "Reset password", "Click here.");

        await _sut.SendAsync(message);

        _logger.Messages.Should().ContainSingle()
            .Which.Should()
            .Contain("alice@example.com")
            .And.Contain("Reset password");
    }

    [Fact]
    public async Task SendAsync_LogsBodyContent()
    {
        var message = new EmailMessage("bob@example.com", "Subject", "Important body content");

        await _sut.SendAsync(message);

        _logger.Messages.Should().ContainSingle()
            .Which.Should().Contain("Important body content");
    }

    [Fact]
    public async Task SendAsync_ReturnsCompletedTask()
    {
        var message = new EmailMessage("user@example.com", "Test", "Body");

        var task = _sut.SendAsync(message);

        task.IsCompleted.Should().BeTrue("logging adapter is synchronous");
        await task; // ensure no exception
    }

    [Fact]
    public async Task SendAsync_WithNullMessage_ThrowsArgumentNullException()
    {
        var act = async () => await _sut.SendAsync(null!);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task SendAsync_CanSendMultipleMessages_LogsEachSeparately()
    {
        var first = new EmailMessage("a@example.com", "First", "Body A");
        var second = new EmailMessage("b@example.com", "Second", "Body B");

        await _sut.SendAsync(first);
        await _sut.SendAsync(second);

        _logger.Messages.Should().HaveCount(2);
        _logger.Messages[0].Should().Contain("First");
        _logger.Messages[1].Should().Contain("Second");
    }
}
