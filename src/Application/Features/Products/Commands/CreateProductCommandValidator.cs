using FluentValidation;

namespace Application.Features.Products.Commands;

/// <summary>
/// Validator for <see cref="CreateProductCommand"/>. Enforces:
/// - Name: required, non-empty, max 200 characters
/// - Description: max 2000 characters (optional)
/// - Price: must be greater than zero
/// - StockQuantity: must be non-negative
///
/// Automatically discovered and invoked by the <c>ValidationBehavior</c> MediatR pipeline.
/// </summary>
public class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    /// <summary>Initializes a new instance of <see cref="CreateProductCommandValidator"/>.</summary>
    public CreateProductCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Product name is required.")
            .MaximumLength(200)
            .WithMessage("Product name must not exceed 200 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(2000)
            .WithMessage("Description must not exceed 2000 characters.")
            .When(x => x.Description is not null);

        RuleFor(x => x.Price)
            .GreaterThan(0)
            .WithMessage("Price must be greater than zero.");

        RuleFor(x => x.StockQuantity)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Stock quantity must be non-negative.");
    }
}
