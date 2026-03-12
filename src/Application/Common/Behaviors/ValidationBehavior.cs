using FluentValidation;
using MediatR;

namespace Application.Common.Behaviors;

/// <summary>
/// MediatR pipeline behavior that automatically runs all FluentValidation validators
/// for a request before the handler executes. If validation fails, throws
/// <see cref="ValidationException"/> with all error details.
///
/// This behavior eliminates the need to manually call validators in every handler.
/// </summary>
/// <typeparam name="TRequest">The MediatR request type.</typeparam>
/// <typeparam name="TResponse">The MediatR response type.</typeparam>
public sealed class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationBehavior{TRequest, TResponse}"/> class.
    /// </summary>
    /// <param name="validators">All registered FluentValidation validators for <typeparamref name="TRequest"/>.</param>
    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    /// <inheritdoc />
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // Collect all validation results in parallel.
        var context = new ValidationContext<TRequest>(request);
        var validationTasks = _validators
            .Select(v => v.ValidateAsync(context, cancellationToken));

        var results = await Task.WhenAll(validationTasks);

        // Aggregate all failures — one validator may produce multiple errors per property.
        var failures = results
            .Where(r => !r.IsValid)
            .SelectMany(r => r.Errors)
            .ToList();

        // Throw if any validator reported failures.
        if (failures.Count > 0)
        {
            throw new ValidationException(failures);
        }

        // All validators passed — proceed to handler.
        return await next();
    }
}
