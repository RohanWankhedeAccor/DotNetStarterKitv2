using Application.Features.Products.Commands;
using FluentAssertions;
using FluentValidation.TestHelper;

namespace Unit.Products;

public class CreateProductCommandValidatorTests
{
    private readonly CreateProductCommandValidator _validator = new();

    [Fact]
    public void Validate_WithValidCommand_PassesValidation()
    {
        var command = new CreateProductCommand
        {
            Name = "Widget",
            Description = "A fine widget",
            Price = 9.99m,
            StockQuantity = 10
        };

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithEmptyName_FailsValidation()
    {
        var command = new CreateProductCommand { Name = "", Price = 1m, StockQuantity = 0 };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Validate_WithNameExceeding200Chars_FailsValidation()
    {
        var command = new CreateProductCommand
        {
            Name = new string('A', 201),
            Price = 1m,
            StockQuantity = 0
        };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Validate_WithZeroPrice_FailsValidation()
    {
        var command = new CreateProductCommand { Name = "Widget", Price = 0m, StockQuantity = 0 };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Price);
    }

    [Fact]
    public void Validate_WithNegativePrice_FailsValidation()
    {
        var command = new CreateProductCommand { Name = "Widget", Price = -1m, StockQuantity = 0 };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Price);
    }

    [Fact]
    public void Validate_WithNegativeStockQuantity_FailsValidation()
    {
        var command = new CreateProductCommand { Name = "Widget", Price = 1m, StockQuantity = -1 };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.StockQuantity);
    }

    [Fact]
    public void Validate_WithZeroStockQuantity_PassesValidation()
    {
        var command = new CreateProductCommand { Name = "Widget", Price = 1m, StockQuantity = 0 };
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(x => x.StockQuantity);
    }

    [Fact]
    public void Validate_WithNullDescription_PassesValidation()
    {
        var command = new CreateProductCommand { Name = "Widget", Price = 1m, StockQuantity = 0, Description = null };
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(x => x.Description);
    }

    [Fact]
    public void Validate_WithDescriptionExceeding2000Chars_FailsValidation()
    {
        var command = new CreateProductCommand
        {
            Name = "Widget",
            Price = 1m,
            StockQuantity = 0,
            Description = new string('X', 2001)
        };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Description);
    }
}
