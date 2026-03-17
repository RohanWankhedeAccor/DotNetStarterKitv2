namespace Domain.Exceptions;

/// <summary>
/// Thrown by the Infrastructure HTTP client wrapper when an outbound API call returns
/// a non-2xx HTTP status code.
///
/// <para>
/// The global exception handler middleware maps this to HTTP 502 Bad Gateway so that
/// callers receive a structured <c>ProblemDetails</c> response rather than a generic 500.
/// </para>
///
/// Usage in the HTTP wrapper:
/// <code>
/// if (!response.IsSuccessStatusCode)
///     throw new ExternalApiException(response.StatusCode, uri, content);
/// </code>
/// </summary>
public sealed class ExternalApiException : Exception
{
    /// <summary>
    /// Initializes a new instance of <see cref="ExternalApiException"/>.
    /// </summary>
    /// <param name="statusCode">The HTTP status code returned by the external API.</param>
    /// <param name="requestUri">The URI that was called.</param>
    /// <param name="responseBody">The raw response body (truncated if very long).</param>
    public ExternalApiException(int statusCode, string requestUri, string responseBody)
        : base($"External API call to '{requestUri}' failed with HTTP {statusCode}. Body: {Truncate(responseBody)}")
    {
        StatusCode = statusCode;
        RequestUri = requestUri;
        ResponseBody = responseBody;
    }

    /// <summary>Gets the HTTP status code returned by the external API.</summary>
    public int StatusCode { get; }

    /// <summary>Gets the URI that was called.</summary>
    public string RequestUri { get; }

    /// <summary>Gets the raw response body from the external API.</summary>
    public string ResponseBody { get; }

    private static string Truncate(string value, int maxLength = 500)
        => value.Length <= maxLength ? value : string.Concat(value.AsSpan(0, maxLength), "…");
}
