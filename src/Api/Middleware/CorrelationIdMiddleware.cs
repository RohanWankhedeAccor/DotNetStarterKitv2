using Serilog.Context;

namespace Api.Middleware;

/// <summary>
/// Middleware that assigns a correlation ID to every HTTP request for end-to-end tracing.
///
/// Behaviour:
/// - Reads the <c>X-Correlation-Id</c> request header if provided by the caller or an upstream
///   gateway (preserves the caller's own trace ID across service boundaries).
/// - Generates a new <see cref="Guid"/> when the header is absent.
/// - Stores the ID in <c>HttpContext.Items</c> so downstream middleware (e.g. exception handler)
///   can read it without re-parsing the header.
/// - Pushes the ID into Serilog's <see cref="LogContext"/> so every log line emitted within
///   this request automatically carries a <c>CorrelationId</c> property.
/// - Echoes the ID back in the <c>X-Correlation-Id</c> response header so clients and
///   API gateways can correlate requests to responses.
///
/// Registration order: after <see cref="ExceptionHandlerMiddleware"/> so that the LogContext
/// is still active when the exception handler logs the error, but before
/// <c>UseSerilogRequestLogging</c> so the request completion log also carries the ID.
/// </summary>
public sealed class CorrelationIdMiddleware
{
    /// <summary>The HTTP header name used to propagate the correlation ID.</summary>
    public const string HeaderName = "X-Correlation-Id";

    /// <summary>The <see cref="HttpContext.Items"/> key used to store the correlation ID.</summary>
    public const string ItemsKey = "CorrelationId";

    private readonly RequestDelegate _next;

    /// <summary>Initializes a new instance of <see cref="CorrelationIdMiddleware"/>.</summary>
    public CorrelationIdMiddleware(RequestDelegate next) => _next = next;

    /// <summary>Executes the middleware.</summary>
    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers[HeaderName].FirstOrDefault()
            ?? Guid.NewGuid().ToString();

        // Make available to downstream middleware (e.g. ExceptionHandlerMiddleware reads this
        // to include the ID in ProblemDetails without re-reading the header).
        context.Items[ItemsKey] = correlationId;

        // Echo the ID back on the response — triggered just before headers are flushed,
        // ensuring it appears even when ExceptionHandlerMiddleware writes an error response.
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[HeaderName] = correlationId;
            return Task.CompletedTask;
        });

        // Push to Serilog LogContext for the lifetime of this request.
        // Every log line emitted within _next() will carry CorrelationId automatically.
        using (LogContext.PushProperty(ItemsKey, correlationId))
        {
            await _next(context);
        }
    }
}
