using FluentValidation;

namespace Application.Features.Products.Commands;

/// <summary>
/// Validator for <see cref="CreateProductCommand"/>. Enforces:
/// - Name: required, non-empty, max 200 characters
/// - Description: required, non-empty, max 2000 characters
/// - Price: required, greater than 0
///
/// This validator is automatically discovered and registered by the Application service
/// extensions, and automatically invoked by the ValidationBehavior pipeline.
/// </summary>
public class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CreateProductCommandValidator"/> class.
    /// </summary>
    public CreateProductCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Product name is required.")
            .MaximumLength(200)
            .WithMessage("Product name must not exceed 200 characters.");

        RuleFor(x => x.Description)
            .NotEmpty()
            .WithMessage("Product description is required.")
            .MaximumLength(2000)
            .WithMessage("Product description must not exceed 2000 characters.");

        RuleFor(x => x.Price)
            .GreaterThan(0)
            .WithMessage("Product price must be greater than zero.");
    }
}
