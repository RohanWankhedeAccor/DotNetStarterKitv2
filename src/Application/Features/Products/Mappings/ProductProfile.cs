using Application.Features.Products.Commands;
using Application.Features.Products.Dtos;
using AutoMapper;
using Domain.Entities;

namespace Application.Features.Products.Mappings;

/// <summary>
/// AutoMapper profile for the Products feature. Defines mappings between:
/// - <see cref="Product"/> → <see cref="ProductDto"/> (used by create/update handlers)
/// - <see cref="CreateProductDto"/> → <see cref="CreateProductCommand"/> (optional, for API endpoint mapping)
///
/// Automatically discovered and registered by the Application service extensions via
/// <c>AddAutoMapper(typeof(IApplicationAssemblyMarker).Assembly)</c>.
/// </summary>
public class ProductProfile : Profile
{
    /// <summary>Initializes a new instance of <see cref="ProductProfile"/>.</summary>
    public ProductProfile()
    {
        // Product entity → ProductDto (returned by create handler and query handlers).
        CreateMap<Product, ProductDto>();

        // CreateProductDto → CreateProductCommand (API endpoint to MediatR command).
        CreateMap<CreateProductDto, CreateProductCommand>();
    }
}
