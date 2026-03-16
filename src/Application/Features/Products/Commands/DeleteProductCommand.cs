using Application.Common.Results;
using MediatR;

namespace Application.Features.Products.Commands;

/// <summary>
/// Command to soft-delete a product.
/// Returns <see cref="Result"/> — callers must inspect <c>IsSuccess</c> rather than catching exceptions.
/// </summary>
public class DeleteProductCommand : IRequest<Result>
{
    /// <summary>Gets or sets the ID of the product to delete.</summary>
    public required Guid Id { get; set; }
}
