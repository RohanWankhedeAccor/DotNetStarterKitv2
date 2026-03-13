using Application.Features.Users.Commands;
using FluentAssertions;

namespace Unit.Users;

public class CreateUserCommandValidatorTests
{
    private readonly CreateUserCommandValidator _validator = new();

    [Fact]
    public void Validate_WithValidCommand_PassesValidation()
    {
        var result = _validator.Validate(ValidCommand());
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithEmptyEmail_FailsValidation()
    {
        var result = _validator.Validate(new CreateUserCommand { Email = "", FullName = "Test", Password = "password123" });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Email");
    }

    [Fact]
    public void Validate_WithInvalidEmailFormat_FailsValidation()
    {
        var result = _validator.Validate(new CreateUserCommand { Email = "not-an-email", FullName = "Test", Password = "password123" });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Email");
    }

    [Fact]
    public void Validate_WithEmptyFullName_FailsValidation()
    {
        var result = _validator.Validate(new CreateUserCommand { Email = "test@test.com", FullName = "", Password = "password123" });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "FullName");
    }

    [Fact]
    public void Validate_WithFullNameExceeding200Chars_FailsValidation()
    {
        var result = _validator.Validate(new CreateUserCommand { Email = "test@test.com", FullName = new string('A', 201), Password = "password123" });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "FullName");
    }

    [Fact]
    public void Validate_WithPasswordUnder8Chars_FailsValidation()
    {
        var result = _validator.Validate(new CreateUserCommand { Email = "test@test.com", FullName = "Test", Password = "short" });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Password");
    }

    [Fact]
    public void Validate_WithEmptyPassword_FailsValidation()
    {
        var result = _validator.Validate(new CreateUserCommand { Email = "test@test.com", FullName = "Test", Password = "" });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Password");
    }

    private static CreateUserCommand ValidCommand() => new()
    {
        Email = "valid@example.com",
        FullName = "Valid User",
        Password = "password123"
    };
}
