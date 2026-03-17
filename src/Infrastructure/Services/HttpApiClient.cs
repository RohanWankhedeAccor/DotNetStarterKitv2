using System.Net.Http.Json;
using System.Text.Json;
using Application.Interfaces;
using Domain.Exceptions;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

/// <summary>
/// Typed HTTP client wrapper that implements <see cref="IHttpApiClient"/>.
///
/// <para>
/// Registered via <c>AddHttpClient&lt;HttpApiClient&gt;()</c> so its lifetime and
/// underlying <see cref="System.Net.Http.HttpClient"/> socket pool are managed by
/// <c>IHttpClientFactory</c>. Do not store this service in a field of a singleton.
/// </para>
///
/// <para><b>Features</b></para>
/// <list type="bullet">
///   <item>JSON serialisation / deserialisation via <c>System.Text.Json</c>.</item>
///   <item>Non-2xx responses → <see cref="ExternalApiException"/> with status code and body.</item>
///   <item>Correlation ID forwarded on every outbound call via <see cref="CorrelationIdDelegatingHandler"/>.</item>
///   <item>Structured log entries at <c>Information</c> level for each request and response.</item>
/// </list>
/// </summary>
public sealed class HttpApiClient : IHttpApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly HttpClient _httpClient;
    private readonly ILogger<HttpApiClient> _logger;

    /// <summary>
    /// Initializes the client. <paramref name="httpClient"/> is injected by
    /// <c>IHttpClientFactory</c> with the named handler pipeline configured in DI.
    /// </summary>
    public HttpApiClient(HttpClient httpClient, ILogger<HttpApiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<T> GetAsync<T>(string requestUri, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("GET {Uri}", requestUri);

        var response = await _httpClient.GetAsync(requestUri, cancellationToken);
        await EnsureSuccessAsync(response, requestUri, cancellationToken);

        var result = await response.Content.ReadFromJsonAsync<T>(JsonOptions, cancellationToken);

        _logger.LogInformation("GET {Uri} → {StatusCode}", requestUri, (int)response.StatusCode);

        return result!;
    }

    /// <inheritdoc />
    public async Task<TResponse> PostAsync<TRequest, TResponse>(
        string requestUri,
        TRequest body,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("POST {Uri}", requestUri);

        var response = await _httpClient.PostAsJsonAsync(requestUri, body, JsonOptions, cancellationToken);
        await EnsureSuccessAsync(response, requestUri, cancellationToken);

        var result = await response.Content.ReadFromJsonAsync<TResponse>(JsonOptions, cancellationToken);

        _logger.LogInformation("POST {Uri} → {StatusCode}", requestUri, (int)response.StatusCode);

        return result!;
    }

    /// <inheritdoc />
    public async Task<TResponse> PutAsync<TRequest, TResponse>(
        string requestUri,
        TRequest body,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("PUT {Uri}", requestUri);

        var response = await _httpClient.PutAsJsonAsync(requestUri, body, JsonOptions, cancellationToken);
        await EnsureSuccessAsync(response, requestUri, cancellationToken);

        var result = await response.Content.ReadFromJsonAsync<TResponse>(JsonOptions, cancellationToken);

        _logger.LogInformation("PUT {Uri} → {StatusCode}", requestUri, (int)response.StatusCode);

        return result!;
    }

    /// <inheritdoc />
    public async Task DeleteAsync(string requestUri, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("DELETE {Uri}", requestUri);

        var response = await _httpClient.DeleteAsync(requestUri, cancellationToken);
        await EnsureSuccessAsync(response, requestUri, cancellationToken);

        _logger.LogInformation("DELETE {Uri} → {StatusCode}", requestUri, (int)response.StatusCode);
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    /// <summary>
    /// Reads the response body and throws <see cref="ExternalApiException"/> for non-2xx responses.
    /// </summary>
    private static async Task EnsureSuccessAsync(
        HttpResponseMessage response,
        string requestUri,
        CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
            return;

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        throw new ExternalApiException((int)response.StatusCode, requestUri, body);
    }
}
