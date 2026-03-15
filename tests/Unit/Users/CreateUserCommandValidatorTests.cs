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
        var result = _validator.Validate(new CreateUserCommand { Email = "", FirstName = "Test", LastName = "User", Password = "password123" });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Email");
    }

    [Fact]
    public void Validate_WithInvalidEmailFormat_FailsValidation()
    {
        var result = _validator.Validate(new CreateUserCommand { Email = "not-an-email", FirstName = "Test", LastName = "User", Password = "password123" });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Email");
    }

    [Fact]
    public void Validate_WithEmptyFirstName_FailsValidation()
    {
        var result = _validator.Validate(new CreateUserCommand { Email = "test@test.com", FirstName = "", LastName = "User", Password = "password123" });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "FirstName");
    }

    [Fact]
    public void Validate_WithFirstNameExceeding100Chars_FailsValidation()
    {
        var result = _validator.Validate(new CreateUserCommand { Email = "test@test.com", FirstName = new string('A', 101), LastName = "User", Password = "password123" });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "FirstName");
    }

    [Fact]
    public void Validate_WithPasswordUnder8Chars_FailsValidation()
    {
        var result = _validator.Validate(new CreateUserCommand { Email = "test@test.com", FirstName = "Test", LastName = "User", Password = "short" });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Password");
    }

    [Fact]
    public void Validate_WithEmptyPassword_FailsValidation()
    {
        var result = _validator.Validate(new CreateUserCommand { Email = "test@test.com", FirstName = "Test", LastName = "User", Password = "" });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Password");
    }

    private static CreateUserCommand ValidCommand() => new()
    {
        Email = "valid@example.com",
        FirstName = "Valid",
        LastName = "User",
        Password = "password123"
    };
}
