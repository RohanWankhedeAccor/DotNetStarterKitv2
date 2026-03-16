using Application.Common.Results;
using Application.Features.Products.Dtos;
using Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Products.Queries;

/// <summary>
/// Handler for <see cref="GetProductByIdQuery"/>.
/// Projects directly to <see cref="ProductDto"/> via <c>Select()</c> to avoid
/// loading the full entity when only the DTO fields are needed.
/// Returns <see cref="Error.NotFound"/> when the product does not exist or has been soft-deleted.
/// </summary>
public sealed class GetProductByIdQueryHandler : IRequestHandler<GetProductByIdQuery, Result<ProductDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>Initializes a new instance of <see cref="GetProductByIdQueryHandler"/>.</summary>
    public GetProductByIdQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    /// <inheritdoc />
    public async Task<Result<ProductDto>> Handle(GetProductByIdQuery request, CancellationToken cancellationToken)
    {
        var product = await _unitOfWork.Products
            .AsQueryable()
            .AsNoTracking()
            .Where(p => p.Id == request.Id)
            .Select(p => new ProductDto
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                Price = p.Price,
                StockQuantity = p.StockQuantity,
                IsActive = p.IsActive,
                CreatedAt = p.CreatedAt,
                CreatedBy = p.CreatedBy,
                ModifiedAt = p.ModifiedAt,
                ModifiedBy = p.ModifiedBy
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (product is null)
            return Error.NotFound("Product", request.Id);

        return product;
    }
}
