using Application.Common.Results;

namespace Api.Extensions;

/// <summary>
/// Extension methods that convert <see cref="Result{T}"/> / <see cref="Result"/> discriminated
/// unions into the appropriate <see cref="IResult"/> HTTP responses for Minimal API endpoints.
/// </summary>
public static class ResultExtensions
{
    /// <summary>
    /// Maps a <see cref="Result{T}"/> to an HTTP response: 200 OK with the value on success,
    /// or a Problem Details error response on failure.
    /// </summary>
    public static IResult ToApiResult<T>(this Result<T> result) =>
        result.IsSuccess ? Results.Ok(result.Value) : ToErrorResult(result.Error);

    /// <summary>
    /// Maps a <see cref="Result{T}"/> using a custom <paramref name="onSuccess"/> delegate,
    /// allowing the caller to build a non-standard success response (e.g. set a cookie then return 200).
    /// </summary>
    public static IResult ToApiResult<T>(this Result<T> result, Func<T, IResult> onSuccess) =>
        result.IsSuccess ? onSuccess(result.Value!) : ToErrorResult(result.Error);

    /// <summary>
    /// Maps a <see cref="Result{T}"/> to a 201 Created response, using <paramref name="location"/>
    /// to derive the <c>Location</c> header URI from the created value.
    /// </summary>
    public static IResult ToCreatedResult<T>(this Result<T> result, Func<T, string> location) =>
        result.IsSuccess
            ? Results.Created(location(result.Value!), result.Value)
            : ToErrorResult(result.Error);

    /// <summary>
    /// Maps a void <see cref="Result"/> to an HTTP response: 204 No Content on success,
    /// or a Problem Details error response on failure.
    /// </summary>
    public static IResult ToApiResult(this Result result) =>
        result.IsSuccess ? Results.NoContent() : ToErrorResult(result.Error);

    private static IResult ToErrorResult(Error error) => error.Code switch
    {
        "NotFound"     => Results.Problem(detail: error.Description, statusCode: StatusCodes.Status404NotFound),
        "Conflict"     => Results.Problem(detail: error.Description, statusCode: StatusCodes.Status409Conflict),
        "Unauthorized" => Results.Problem(detail: error.Description, statusCode: StatusCodes.Status401Unauthorized),
        "Forbidden"    => Results.Problem(detail: error.Description, statusCode: StatusCodes.Status403Forbidden),
        _              => Results.Problem(detail: error.Description, statusCode: StatusCodes.Status500InternalServerError),
    };
}
