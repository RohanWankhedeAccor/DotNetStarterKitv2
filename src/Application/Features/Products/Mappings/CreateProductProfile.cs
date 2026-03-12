using Application.Features.Products.Commands;
using Application.Features.Products.Dtos;
using AutoMapper;
using Domain.Entities;

namespace Application.Features.Products.Mappings;

/// <summary>
/// AutoMapper profile for Product feature. Defines mappings between:
/// - <see cref="CreateProductCommand"/> → <see cref="Product"/>
/// - <see cref="Product"/> → <see cref="ProductDto"/>
/// - <see cref="CreateProductDto"/> → <see cref="CreateProductCommand"/>
///
/// Automatically discovered and registered by the Application service extensions.
/// </summary>
public class CreateProductProfile : Profile
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CreateProductProfile"/> class.
    /// </summary>
    public CreateProductProfile()
    {
        // CreateProductCommand → Product entity (for command handler)
        CreateMap<CreateProductCommand, Product>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.ModifiedAt, opt => opt.Ignore())
            .ForMember(dest => dest.ModifiedBy, opt => opt.Ignore())
            .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
            .ForMember(dest => dest.Status, opt => opt.Ignore());

        // Product entity → ProductDto (for queries)
        CreateMap<Product, ProductDto>();

        // CreateProductDto → CreateProductCommand (optional, for API endpoint mapping)
        CreateMap<CreateProductDto, CreateProductCommand>();
    }
}
