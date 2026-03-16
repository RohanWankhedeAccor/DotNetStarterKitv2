using Application.Common.Results;
using Application.Features.Products.Queries;
using Application.Interfaces;
using MediatR;

namespace Application.Features.Products.Commands;

/// <summary>
/// Handler for <see cref="DeleteProductCommand"/>.
/// Soft-deletes the product via <see cref="Domain.Common.BaseEntity.Delete"/> and
/// invalidates the products list cache.
/// </summary>
public sealed class DeleteProductCommandHandler : IRequestHandler<DeleteProductCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICacheService _cache;

    /// <summary>Initializes a new instance of <see cref="DeleteProductCommandHandler"/>.</summary>
    public DeleteProductCommandHandler(IUnitOfWork unitOfWork, ICacheService cache)
    {
        _unitOfWork = unitOfWork;
        _cache = cache;
    }

    /// <inheritdoc />
    public async Task<Result> Handle(DeleteProductCommand request, CancellationToken cancellationToken)
    {
        var product = await _unitOfWork.Products.GetByIdAsync(request.Id, cancellationToken);

        if (product is null)
            return Error.NotFound("Product", request.Id);

        // Soft-delete: sets IsDeleted = true, picked up by the global EF query filter.
        product.Delete();
        _unitOfWork.Products.Update(product);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Invalidate product-list cache so deleted product no longer appears in list results.
        _cache.RemoveByPrefix(GetProductsQueryHandler.CacheKeyPrefix);

        return Result.Success();
    }
}
