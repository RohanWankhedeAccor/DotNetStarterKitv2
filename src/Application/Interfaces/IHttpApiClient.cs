namespace Application.Interfaces;

/// <summary>
/// Abstraction for making outbound HTTP API calls.
///
/// <para>
/// Application handlers that need to call external REST services should depend
/// on this interface (or a more specific domain interface backed by it),
/// keeping them free of <c>HttpClient</c> and serialization concerns.
/// The Infrastructure layer provides the concrete implementation via a typed
/// <see cref="System.Net.Http.HttpClient"/> managed by <c>IHttpClientFactory</c>.
/// </para>
///
/// <para><b>Error handling</b></para>
/// Non-2xx HTTP responses throw <see cref="Domain.Exceptions.ExternalApiException"/>
/// so callers do not need to inspect status codes directly.
///
/// <para>
/// <b>Correlation ID propagation:</b>
/// The implementation automatically forwards the current request's
/// <c>X-Correlation-Id</c> header on every outbound call so distributed traces
/// span service boundaries.
/// </para>
/// </summary>
public interface IHttpApiClient
{
    /// <summary>
    /// Sends a GET request to <paramref name="requestUri"/> and deserialises the response body as
    /// <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The expected response type.</typeparam>
    /// <param name="requestUri">Absolute URI or path relative to the base address.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The deserialised response body.</returns>
    Task<T> GetAsync<T>(string requestUri, CancellationToken cancellationToken = default);

    /// <summary>
    /// Serialises <paramref name="body"/> as JSON and sends it in a POST request.
    /// Returns the deserialised response body as <typeparamref name="TResponse"/>.
    /// </summary>
    Task<TResponse> PostAsync<TRequest, TResponse>(
        string requestUri,
        TRequest body,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Serialises <paramref name="body"/> as JSON and sends it in a PUT request.
    /// Returns the deserialised response body as <typeparamref name="TResponse"/>.
    /// </summary>
    Task<TResponse> PutAsync<TRequest, TResponse>(
        string requestUri,
        TRequest body,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a DELETE request to <paramref name="requestUri"/>.
    /// Throws <see cref="Domain.Exceptions.ExternalApiException"/> on non-2xx response.
    /// </summary>
    Task DeleteAsync(string requestUri, CancellationToken cancellationToken = default);
}
