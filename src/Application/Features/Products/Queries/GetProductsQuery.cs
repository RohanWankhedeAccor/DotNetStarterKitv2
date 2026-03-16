using Application.Common.Models;
using Application.Features.Products.Dtos;
using MediatR;

namespace Application.Features.Products.Queries;

/// <summary>
/// Query to retrieve a paginated, filtered, and sorted list of products.
/// Inherits pagination and sorting parameters from <see cref="PagedRequest"/>.
///
/// <para><b>Filtering</b></para>
/// <list type="bullet">
///   <item><see cref="Search"/> — case-insensitive substring match on <c>Name</c> or <c>Description</c>.</item>
///   <item><see cref="IsActive"/> — filters by active/inactive status; <c>null</c> returns both.</item>
/// </list>
///
/// <para><b>Sorting</b> (via <see cref="PagedRequest.SortBy"/>)</para>
/// Valid column names: <c>name</c>, <c>price</c>, <c>stockquantity</c>, <c>createdat</c>.
/// Unknown values fall back to <c>name</c> ascending.
/// </summary>
public class GetProductsQuery : PagedRequest, IRequest<PagedResponse<ProductDto>>
{
    /// <summary>
    /// Gets or sets an optional free-text search term.
    /// Matched case-insensitively against product name and description.
    /// </summary>
    public string? Search { get; set; }

    /// <summary>
    /// Gets or sets an optional active-status filter.
    /// When <c>null</c>, both active and inactive products are returned.
    /// </summary>
    public bool? IsActive { get; set; }
}
