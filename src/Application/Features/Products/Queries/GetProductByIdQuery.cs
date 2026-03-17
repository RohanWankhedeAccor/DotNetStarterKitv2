using Application.Common.Results;
using Application.Features.Products.Dtos;
using MediatR;

namespace Application.Features.Products.Queries;

/// <summary>
/// Query to retrieve a single product by its GUID identifier.
/// Returns <see cref="Result{T}"/> — callers must inspect <c>IsSuccess</c> rather than catching exceptions.
/// </summary>
public class GetProductByIdQuery : IRequest<Result<ProductDto>>
{
    /// <summary>Gets or sets the unique identifier of the product to retrieve.</summary>
    public required Guid Id { get; set; }
}
