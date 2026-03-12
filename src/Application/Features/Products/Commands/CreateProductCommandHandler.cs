using Application.Features.Products.Dtos;
using Application.Interfaces;
using AutoMapper;
using Domain.Entities;
using MediatR;

namespace Application.Features.Products.Commands;

/// <summary>
/// Handler for <see cref="CreateProductCommand"/>. Creates a new product with:
/// - Draft status by default
/// - Audit fields auto-populated by DbContext.SaveChangesAsync override
/// </summary>
public sealed class CreateProductCommandHandler : IRequestHandler<CreateProductCommand, ProductDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    /// <summary>
    /// Initializes a new instance of the <see cref="CreateProductCommandHandler"/> class.
    /// </summary>
    /// <param name="context">The application database context.</param>
    /// <param name="mapper">The AutoMapper instance.</param>
    public CreateProductCommandHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    /// <inheritdoc />
    public async Task<ProductDto> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        // Create the new product. Status is automatically set to Draft by the Product constructor.
        var product = new Product(
            name: request.Name,
            description: request.Description,
            price: request.Price);

        // Add to DbSet and persist.
        _context.Products.Add(product);
        await _context.SaveChangesAsync(cancellationToken);

        // Return the created product as a DTO.
        return _mapper.Map<ProductDto>(product);
    }
}
