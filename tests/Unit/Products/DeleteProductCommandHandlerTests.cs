using Application.Features.Products.Commands;
using Application.Interfaces;
using Domain.Entities;
using FluentAssertions;
using NSubstitute;
using Unit.Helpers;

namespace Unit.Products;

public class DeleteProductCommandHandlerTests
{
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IRepository<Product> _productsRepo = Substitute.For<IRepository<Product>>();
    private readonly ICacheService _cache = Substitute.For<ICacheService>();
    private readonly DeleteProductCommandHandler _handler;

    public DeleteProductCommandHandlerTests()
    {
        _unitOfWork.Products.Returns(_productsRepo);
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);
        _handler = new DeleteProductCommandHandler(_unitOfWork, _cache);
    }

    [Fact]
    public async Task Handle_WithExistingProduct_SoftDeletesAndSaves()
    {
        var product = new Product("Widget", null, 9.99m, 10);
        _productsRepo.GetByIdAsync(product.Id, Arg.Any<CancellationToken>()).Returns(product);

        var result = await _handler.Handle(new DeleteProductCommand { Id = product.Id }, default);

        result.IsSuccess.Should().BeTrue();
        product.IsDeleted.Should().BeTrue();
        _productsRepo.Received(1).Update(product);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithNonExistentId_ReturnsNotFoundError()
    {
        var missingId = Guid.NewGuid();
        _productsRepo.GetByIdAsync(missingId, Arg.Any<CancellationToken>()).Returns((Product?)null);

        var result = await _handler.Handle(new DeleteProductCommand { Id = missingId }, default);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("NotFound");
    }

    [Fact]
    public async Task Handle_AfterDelete_InvalidatesProductListCache()
    {
        var product = new Product("Widget", null, 9.99m, 10);
        _productsRepo.GetByIdAsync(product.Id, Arg.Any<CancellationToken>()).Returns(product);

        await _handler.Handle(new DeleteProductCommand { Id = product.Id }, default);

        _cache.Received(1).RemoveByPrefix(Arg.Is<string>(k => k.StartsWith("products:")));
    }
}
