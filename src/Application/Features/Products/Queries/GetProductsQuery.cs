using Application.Common.Models;
using Application.Features.Products.Dtos;
using MediatR;

namespace Application.Features.Products.Queries;

/// <summary>
/// Query to retrieve a paginated list of products. Inherits pagination parameters
/// from <see cref="PagedRequest"/> (<see cref="PagedRequest.PageNumber"/>, <see cref="PagedRequest.PageSize"/>).
/// Returns <see cref="PagedResponse{T}"/> containing a page of <see cref="ProductDto"/>.
/// </summary>
public class GetProductsQuery : PagedRequest, IRequest<PagedResponse<ProductDto>>
{
}
