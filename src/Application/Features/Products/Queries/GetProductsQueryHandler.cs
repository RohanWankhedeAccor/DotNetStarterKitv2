using Application.Common.Models;
using Application.Features.Products.Dtos;
using Application.Interfaces;
using Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Application.Features.Products.Queries;

/// <summary>
/// Handler for <see cref="GetProductsQuery"/>. Retrieves a filtered, sorted, and paginated
/// list of products.
///
/// <para>
/// Results are cached for 2 minutes via <see cref="ICacheService"/>.
/// The cache key encodes all query parameters so different filter/sort/page combinations
/// produce separate cache entries. Entries are invalidated on create and delete.
/// </para>
/// </summary>
public sealed class GetProductsQueryHandler : IRequestHandler<GetProductsQuery, PagedResponse<ProductDto>>
{
    /// <summary>
    /// Prefix shared by all product-list cache entries.
    /// Mutation handlers call <c>RemoveByPrefix</c> with this value to invalidate the entire cache.
    /// </summary>
    internal const string CacheKeyPrefix = "products:";

    private readonly IUnitOfWork _unitOfWork;
    private readonly ICacheService _cache;

    /// <summary>Initializes a new instance of <see cref="GetProductsQueryHandler"/>.</summary>
    public GetProductsQueryHandler(IUnitOfWork unitOfWork, ICacheService cache)
    {
        _unitOfWork = unitOfWork;
        _cache = cache;
    }

    /// <inheritdoc />
    public Task<PagedResponse<ProductDto>> Handle(GetProductsQuery request, CancellationToken cancellationToken)
    {
        var cacheKey = $"{CacheKeyPrefix}" +
            $"p{request.PageNumber}:s{request.PageSize}:" +
            $"q{request.Search}:ia{request.IsActive}:" +
            $"sb{request.SortBy}:sd{request.SortDescending}";

        return _cache.GetOrSetAsync(
            cacheKey,
            ct => FetchAsync(request, ct),
            absoluteExpiration: TimeSpan.FromMinutes(2),
            cancellationToken: cancellationToken);
    }

    private async Task<PagedResponse<ProductDto>> FetchAsync(GetProductsQuery request, CancellationToken cancellationToken)
    {
        var query = _unitOfWork.Products.AsQueryable().AsNoTracking();

        // ── Filtering ────────────────────────────────────────────────────────────

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim().ToLower();
            query = query.Where(p =>
                p.Name.ToLower().Contains(term) ||
                (p.Description != null && p.Description.ToLower().Contains(term)));
        }

        if (request.IsActive.HasValue)
            query = query.Where(p => p.IsActive == request.IsActive.Value);

        // ── Sorting ──────────────────────────────────────────────────────────────

        Expression<Func<Product, object>> sortKey = request.SortBy?.ToLower() switch
        {
            "price"         => p => p.Price,
            "stockquantity" => p => p.StockQuantity,
            "createdat"     => p => p.CreatedAt,
            _               => p => p.Name,
        };

        query = request.SortDescending
            ? query.OrderByDescending(sortKey)
            : query.OrderBy(sortKey);

        // ── Pagination ───────────────────────────────────────────────────────────

        var totalCount = await query.CountAsync(cancellationToken);

        var offset = (request.PageNumber - 1) * request.PageSize;
        var products = await query
            .Skip(offset)
            .Take(request.PageSize)
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
            .ToListAsync(cancellationToken);

        return new PagedResponse<ProductDto>
        {
            Items = products,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
            TotalCount = totalCount
        };
    }
}
