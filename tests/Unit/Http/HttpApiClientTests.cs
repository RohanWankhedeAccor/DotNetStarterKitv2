using System.Net;
using System.Text.Json;
using Domain.Exceptions;
using FluentAssertions;
using Infrastructure.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace Unit.Http;

/// <summary>
/// Unit tests for <see cref="HttpApiClient"/>.
/// Uses a <see cref="StubHttpMessageHandler"/> to simulate HTTP responses
/// without making real network calls.
/// </summary>
public sealed class HttpApiClientTests
{
    // ── Stub handler ──────────────────────────────────────────────────────────

    /// <summary>
    /// A minimal <see cref="HttpMessageHandler"/> replacement that returns a
    /// pre-configured response, allowing <see cref="HttpClient"/> to be tested
    /// in isolation without a real HTTP server.
    /// </summary>
    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _statusCode;
        private readonly string _responseBody;

        public StubHttpMessageHandler(HttpStatusCode statusCode, string responseBody)
        {
            _statusCode = statusCode;
            _responseBody = responseBody;
        }

        /// <summary>The last request sent through this handler.</summary>
        public HttpRequestMessage? LastRequest { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            LastRequest = request;
            return Task.FromResult(new HttpResponseMessage(_statusCode)
            {
                Content = new StringContent(_responseBody, System.Text.Encoding.UTF8, "application/json"),
            });
        }
    }

    private record SampleDto(int Id, string Name);

    private static HttpApiClient BuildClient(HttpStatusCode status, string body)
    {
        var handler = new StubHttpMessageHandler(status, body);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.example.com") };
        return new HttpApiClient(httpClient, NullLogger<HttpApiClient>.Instance);
    }

    // ── GetAsync ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAsync_WithSuccessResponse_DeserialisesBody()
    {
        var json = JsonSerializer.Serialize(new SampleDto(1, "Alice"));
        var sut = BuildClient(HttpStatusCode.OK, json);

        var result = await sut.GetAsync<SampleDto>("/api/sample");

        result.Id.Should().Be(1);
        result.Name.Should().Be("Alice");
    }

    [Fact]
    public async Task GetAsync_WithNonSuccessResponse_ThrowsExternalApiException()
    {
        var sut = BuildClient(HttpStatusCode.NotFound, """{"error":"not found"}""");

        var act = async () => await sut.GetAsync<SampleDto>("/api/missing");

        await act.Should().ThrowAsync<ExternalApiException>()
            .Where(ex => ex.StatusCode == 404);
    }

    [Fact]
    public async Task GetAsync_ExceptionCarriesRequestUri()
    {
        var sut = BuildClient(HttpStatusCode.ServiceUnavailable, "down");

        var act = async () => await sut.GetAsync<SampleDto>("/api/broken");

        var ex = await act.Should().ThrowAsync<ExternalApiException>();
        ex.Which.RequestUri.Should().Contain("/api/broken");
    }

    // ── PostAsync ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task PostAsync_WithSuccessResponse_DeserialisesBody()
    {
        var json = JsonSerializer.Serialize(new SampleDto(42, "Bob"));
        var sut = BuildClient(HttpStatusCode.Created, json);

        var result = await sut.PostAsync<object, SampleDto>("/api/sample", new { });

        result.Id.Should().Be(42);
        result.Name.Should().Be("Bob");
    }

    [Fact]
    public async Task PostAsync_WithNonSuccessResponse_ThrowsExternalApiException()
    {
        var sut = BuildClient(HttpStatusCode.BadRequest, """{"error":"bad"}""");

        var act = async () => await sut.PostAsync<object, SampleDto>("/api/sample", new { });

        await act.Should().ThrowAsync<ExternalApiException>()
            .Where(ex => ex.StatusCode == 400);
    }

    // ── PutAsync ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task PutAsync_WithSuccessResponse_DeserialisesBody()
    {
        var json = JsonSerializer.Serialize(new SampleDto(7, "Carol"));
        var sut = BuildClient(HttpStatusCode.OK, json);

        var result = await sut.PutAsync<object, SampleDto>("/api/sample/7", new { });

        result.Id.Should().Be(7);
    }

    [Fact]
    public async Task PutAsync_WithNonSuccessResponse_ThrowsExternalApiException()
    {
        var sut = BuildClient(HttpStatusCode.InternalServerError, "error");

        var act = async () => await sut.PutAsync<object, SampleDto>("/api/sample/7", new { });

        await act.Should().ThrowAsync<ExternalApiException>()
            .Where(ex => ex.StatusCode == 500);
    }

    // ── DeleteAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_WithSuccessResponse_DoesNotThrow()
    {
        var sut = BuildClient(HttpStatusCode.NoContent, string.Empty);

        var act = async () => await sut.DeleteAsync("/api/sample/1");

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task DeleteAsync_WithNonSuccessResponse_ThrowsExternalApiException()
    {
        var sut = BuildClient(HttpStatusCode.Forbidden, "forbidden");

        var act = async () => await sut.DeleteAsync("/api/sample/1");

        await act.Should().ThrowAsync<ExternalApiException>()
            .Where(ex => ex.StatusCode == 403);
    }

    // ── ExternalApiException content ─────────────────────────────────────────

    [Fact]
    public async Task GetAsync_ExceptionCarriesResponseBody()
    {
        var sut = BuildClient(HttpStatusCode.UnprocessableEntity, """{"field":"value"}""");

        var act = async () => await sut.GetAsync<SampleDto>("/api/validate");

        var ex = await act.Should().ThrowAsync<ExternalApiException>();
        ex.Which.ResponseBody.Should().Contain("field");
    }
}
