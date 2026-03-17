using FluentValidation;

namespace Application.Features.Users.Commands;

/// <summary>
/// Validator for <see cref="CreateUserCommand"/>. Enforces:
/// - Email: required, non-empty, valid email format
/// - FirstName: required, non-empty, max 100 characters
/// - LastName: required, non-empty, max 100 characters
/// - Password: required, non-empty, min 8 characters (Phase 2: enhance with complexity rules)
///
/// This validator is automatically discovered and registered by the Application service
/// extensions, and automatically invoked by the ValidationBehavior pipeline.
/// </summary>
public class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CreateUserCommandValidator"/> class.
    /// </summary>
    public CreateUserCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage("Email is required.")
            .EmailAddress()
            .WithMessage("Email must be a valid email address.");

        RuleFor(x => x.FirstName)
            .NotEmpty()
            .WithMessage("First name is required.")
            .MaximumLength(100)
            .WithMessage("First name must not exceed 100 characters.");

        RuleFor(x => x.LastName)
            .NotEmpty()
            .WithMessage("Last name is required.")
            .MaximumLength(100)
            .WithMessage("Last name must not exceed 100 characters.");

        RuleFor(x => x.Password)
            .NotEmpty()
            .WithMessage("Password is required.")
            .MinimumLength(8)
            .WithMessage("Password must be at least 8 characters long.");
            // TODO: Phase 2 — Add complexity rules (uppercase, lowercase, digit, special char).
    }
}
