using Application.Features.Products.Commands;
using FluentAssertions;

namespace Unit.Products;

public class CreateProductCommandValidatorTests
{
    private readonly CreateProductCommandValidator _validator = new();

    [Fact]
    public void Validate_WithValidCommand_PassesValidation()
    {
        var result = _validator.Validate(ValidCommand());
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithEmptyName_FailsValidation()
    {
        var result = _validator.Validate(new CreateProductCommand { Name = "", Description = "desc", Price = 9.99m });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    [Fact]
    public void Validate_WithNameExceeding200Chars_FailsValidation()
    {
        var result = _validator.Validate(new CreateProductCommand { Name = new string('X', 201), Description = "desc", Price = 9.99m });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    [Fact]
    public void Validate_WithEmptyDescription_FailsValidation()
    {
        var result = _validator.Validate(new CreateProductCommand { Name = "Widget", Description = "", Price = 9.99m });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Description");
    }

    [Fact]
    public void Validate_WithDescriptionExceeding2000Chars_FailsValidation()
    {
        var result = _validator.Validate(new CreateProductCommand { Name = "Widget", Description = new string('X', 2001), Price = 9.99m });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Description");
    }

    [Fact]
    public void Validate_WithZeroPrice_FailsValidation()
    {
        var result = _validator.Validate(new CreateProductCommand { Name = "Widget", Description = "desc", Price = 0 });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Price");
    }

    [Fact]
    public void Validate_WithNegativePrice_FailsValidation()
    {
        var result = _validator.Validate(new CreateProductCommand { Name = "Widget", Description = "desc", Price = -1m });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Price");
    }

    private static CreateProductCommand ValidCommand() => new()
    {
        Name = "Valid Product",
        Description = "A valid description",
        Price = 9.99m
    };
}
