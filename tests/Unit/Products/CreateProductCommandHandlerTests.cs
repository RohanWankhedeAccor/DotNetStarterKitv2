using Application.Features.Products.Commands;
using Application.Features.Products.Dtos;
using Application.Interfaces;
using AutoMapper;
using Domain.Entities;
using FluentAssertions;
using NSubstitute;
using Unit.Helpers;

namespace Unit.Products;

public class CreateProductCommandHandlerTests
{
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IRepository<Product> _productsRepo = Substitute.For<IRepository<Product>>();
    private readonly ICacheService _cache = Substitute.For<ICacheService>();
    private readonly IMapper _mapper;
    private readonly CreateProductCommandHandler _handler;

    public CreateProductCommandHandlerTests()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.CreateMap<Product, ProductDto>());
        _mapper = config.CreateMapper();

        _unitOfWork.Products.Returns(_productsRepo);
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);

        _handler = new CreateProductCommandHandler(_unitOfWork, _mapper, _cache);
    }

    [Fact]
    public async Task Handle_WithValidCommand_AddsProductAndReturnsDto()
    {
        var command = new CreateProductCommand
        {
            Name = "Widget",
            Description = "A test widget",
            Price = 9.99m,
            StockQuantity = 100
        };

        var result = await _handler.Handle(command, default);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("Widget");
        result.Value.Price.Should().Be(9.99m);
        result.Value.StockQuantity.Should().Be(100);
        result.Value.IsActive.Should().BeTrue();

        _productsRepo.Received(1).Add(Arg.Is<Product>(p =>
            p.Name == "Widget" &&
            p.Price == 9.99m &&
            p.StockQuantity == 100));
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_AfterCreate_InvalidatesProductListCache()
    {
        var command = new CreateProductCommand
        {
            Name = "Gadget",
            Price = 19.99m,
            StockQuantity = 50
        };

        await _handler.Handle(command, default);

        _cache.Received(1).RemoveByPrefix(Arg.Is<string>(k => k.StartsWith("products:")));
    }
}
