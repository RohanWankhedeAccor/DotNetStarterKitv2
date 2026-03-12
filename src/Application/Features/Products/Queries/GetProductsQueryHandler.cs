using Application.Common.Models;
using Application.Features.Products.Dtos;
using Application.Interfaces;
using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Products.Queries;

/// <summary>
/// Handler for <see cref="GetProductsQuery"/>. Retrieves a paginated list of products,
/// applying offset/limit based on <see cref="PagedRequest.PageNumber"/> and
/// <see cref="PagedRequest.PageSize"/>. Uses <c>AsNoTracking()</c> for performance.
/// </summary>
public sealed class GetProductsQueryHandler : IRequestHandler<GetProductsQuery, PagedResponse<ProductDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetProductsQueryHandler"/> class.
    /// </summary>
    /// <param name="context">The application database context.</param>
    /// <param name="mapper">The AutoMapper instance.</param>
    public GetProductsQueryHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    /// <inheritdoc />
    public async Task<PagedResponse<ProductDto>> Handle(GetProductsQuery request, CancellationToken cancellationToken)
    {
        // Calculate offset from page number and page size.
        var offset = (request.PageNumber - 1) * request.PageSize;

        // Fetch total count for pagination metadata.
        var totalCount = await _context.Products
            .AsNoTracking()
            .CountAsync(cancellationToken);

        // Fetch the requested page of products.
        var products = await _context.Products
            .AsNoTracking()
            .OrderBy(p => p.Name)
            .Skip(offset)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var productDtos = _mapper.Map<List<ProductDto>>(products);

        return new PagedResponse<ProductDto>
        {
            Items = productDtos,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
            TotalCount = totalCount
        };
    }
}
