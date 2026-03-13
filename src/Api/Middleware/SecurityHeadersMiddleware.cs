namespace Api.Middleware;

/// <summary>
/// Middleware that adds security headers to all HTTP responses.
///
/// Headers added:
/// - Strict-Transport-Security (HSTS): Forces HTTPS for 1 year (including subdomains)
/// - X-Content-Type-Options: Prevents MIME-type sniffing
/// - X-Frame-Options: Prevents clickjacking attacks
/// - X-XSS-Protection: Legacy XSS protection (modern browsers use CSP)
/// - Referrer-Policy: Controls referrer information
/// </summary>
public sealed class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    /// <summary>Initializes a new instance of <see cref="SecurityHeadersMiddleware"/>.</summary>
    /// <param name="next">The next middleware delegate in the pipeline.</param>
    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    /// <summary>Adds security headers to the response and invokes the next middleware.</summary>
    /// <param name="context">The current HTTP context.</param>
    public async Task InvokeAsync(HttpContext context)
    {
        // Strict-Transport-Security: enforce HTTPS for 1 year (31536000 seconds)
        context.Response.Headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains";

        // Prevent MIME-type sniffing
        context.Response.Headers["X-Content-Type-Options"] = "nosniff";

        // Prevent clickjacking by disallowing framing
        context.Response.Headers["X-Frame-Options"] = "DENY";

        // Legacy XSS protection (modern browsers use Content-Security-Policy)
        context.Response.Headers["X-XSS-Protection"] = "1; mode=block";

        // Control referrer information
        context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";

        // Optional: Content-Security-Policy (CSP) header
        // Adjust directives based on your application's needs
        context.Response.Headers["Content-Security-Policy"] = "default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'";

        await _next(context);
    }
}
