using Microsoft.AspNetCore.Http;

namespace Infrastructure.Services;

/// <summary>
/// A <see cref="DelegatingHandler"/> that forwards the current inbound request's
/// <c>X-Correlation-Id</c> header on every outbound HTTP call made through
/// <see cref="HttpApiClient"/>.
///
/// <para>
/// Correlation ID propagation enables end-to-end distributed tracing across
/// service boundaries without any caller code changes. The ID is read from
/// <see cref="IHttpContextAccessor"/>; when there is no active HTTP context
/// (e.g., in a background job) a new GUID is generated so outbound calls
/// are still traceable.
/// </para>
///
/// <para>
/// Registered as a transient <see cref="DelegatingHandler"/> and attached to
/// the typed <c>HttpClient</c> via <c>AddHttpMessageHandler</c> in
/// <c>InfrastructureServiceExtensions</c>.
/// </para>
/// </summary>
internal sealed class CorrelationIdDelegatingHandler : DelegatingHandler
{
    // Mirror of CorrelationIdMiddleware constants — kept here to avoid a
    // circular dependency (Infrastructure must not reference the Api project).
    private const string HeaderName = "X-Correlation-Id";
    private const string ItemsKey = "CorrelationId";

    private readonly IHttpContextAccessor _httpContextAccessor;

    public CorrelationIdDelegatingHandler(IHttpContextAccessor httpContextAccessor)
        => _httpContextAccessor = httpContextAccessor;

    /// <inheritdoc />
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        // Prefer the inbound request's correlation ID; fall back to a new GUID
        // so background jobs and test contexts are still traceable.
        var correlationId =
            _httpContextAccessor.HttpContext?.Items[ItemsKey] as string
            ?? Guid.NewGuid().ToString();

        request.Headers.TryAddWithoutValidation(HeaderName, correlationId);

        return base.SendAsync(request, cancellationToken);
    }
}
