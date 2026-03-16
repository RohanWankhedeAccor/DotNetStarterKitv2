using Application.Features.Products.Dtos;
using Application.Features.Products.Queries;
using Application.Interfaces;
using AutoMapper;
using Domain.Entities;
using MediatR;

namespace Application.Features.Products.Commands;

/// <summary>
/// Handler for <see cref="CreateProductCommand"/>.
/// Creates a new product and invalidates the products list cache.
/// </summary>
public sealed class CreateProductCommandHandler : IRequestHandler<CreateProductCommand, ProductDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ICacheService _cache;

    /// <summary>Initializes a new instance of <see cref="CreateProductCommandHandler"/>.</summary>
    public CreateProductCommandHandler(IUnitOfWork unitOfWork, IMapper mapper, ICacheService cache)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _cache = cache;
    }

    /// <inheritdoc />
    public async Task<ProductDto> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        var product = new Product(
            name: request.Name,
            description: request.Description,
            price: request.Price,
            stockQuantity: request.StockQuantity);

        _unitOfWork.Products.Add(product);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Invalidate all product-list cache entries so the next GET /products sees the new product.
        _cache.RemoveByPrefix(GetProductsQueryHandler.CacheKeyPrefix);

        return _mapper.Map<ProductDto>(product);
    }
}
