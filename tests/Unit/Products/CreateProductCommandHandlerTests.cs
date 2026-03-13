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
    private readonly IApplicationDbContext _context = Substitute.For<IApplicationDbContext>();
    private readonly IMapper _mapper;
    private readonly CreateProductCommandHandler _handler;

    public CreateProductCommandHandlerTests()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.CreateMap<Product, ProductDto>()
               .ForMember(d => d.Id, o => o.MapFrom(s => s.Id))
               .ForMember(d => d.Name, o => o.MapFrom(s => s.Name))
               .ForMember(d => d.Description, o => o.MapFrom(s => s.Description))
               .ForMember(d => d.Price, o => o.MapFrom(s => s.Price)));
        _mapper = config.CreateMapper();

        _context.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);
        _handler = new CreateProductCommandHandler(_context, _mapper);
    }

    [Fact]
    public async Task Handle_WithValidCommand_CreatesProductAndReturnsDto()
    {
        var productsSet = DbSetMockHelper.Create<Product>([]);
        _context.Products.Returns(productsSet);

        var result = await _handler.Handle(
            new CreateProductCommand { Name = "Widget", Description = "A widget", Price = 9.99m },
            default);

        result.Name.Should().Be("Widget");
        result.Description.Should().Be("A widget");
        result.Price.Should().Be(9.99m);
    }

    [Fact]
    public async Task Handle_CallsSaveChangesAsync()
    {
        var productsSet = DbSetMockHelper.Create<Product>([]);
        _context.Products.Returns(productsSet);

        await _handler.Handle(
            new CreateProductCommand { Name = "Widget", Description = "A widget", Price = 9.99m },
            default);

        await _context.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_AddsProductToDbSet()
    {
        var productsSet = DbSetMockHelper.Create<Product>([]);
        _context.Products.Returns(productsSet);

        await _handler.Handle(
            new CreateProductCommand { Name = "Widget", Description = "A widget", Price = 9.99m },
            default);

        _context.Products.Received(1).Add(Arg.Any<Product>());
    }
}
