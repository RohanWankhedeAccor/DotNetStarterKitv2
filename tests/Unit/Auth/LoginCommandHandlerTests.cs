using Application.Features.Auth.Commands;
using Application.Interfaces;
using Domain.Entities;
using FluentAssertions;
using NSubstitute;
using Unit.Helpers;

namespace Unit.Auth;

public class LoginCommandHandlerTests
{
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IRepository<User> _usersRepo = Substitute.For<IRepository<User>>();
    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();
    private readonly ITokenService _tokenService = Substitute.For<ITokenService>();
    private readonly LoginCommandHandler _handler;

    public LoginCommandHandlerTests()
    {
        _unitOfWork.Users.Returns(_usersRepo);

        _handler = new LoginCommandHandler(_unitOfWork, _passwordHasher, _tokenService);
        _tokenService.ExpirationMinutes.Returns(60);
        _tokenService.GenerateToken(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
                Arg.Any<IEnumerable<string>>(), Arg.Any<IEnumerable<string>>())
            .Returns("test.jwt.token");
    }

    [Fact]
    public async Task Handle_WithValidCredentials_ReturnsLoginResponse()
    {
        var user = CreateActiveUser("user@example.com", "hashed_pw");
        _usersRepo.AsQueryable().Returns(new TestAsyncEnumerable<User>([user]));
        _passwordHasher.VerifyPassword("password123", "hashed_pw").Returns(true);

        var result = await _handler.Handle(new LoginCommand("user@example.com", "password123"), default);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Token.Should().Be("test.jwt.token");
        result.Value.Email.Should().Be("user@example.com");
        result.Value.ExpiresIn.Should().Be(3600);
    }

    [Fact]
    public async Task Handle_WithNonExistentEmail_ReturnsNotFoundError()
    {
        _usersRepo.AsQueryable().Returns(new TestAsyncEnumerable<User>([]));

        var result = await _handler.Handle(new LoginCommand("ghost@example.com", "pw"), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("NotFound");
    }

    [Fact]
    public async Task Handle_WithWrongPassword_ReturnsUnauthorizedError()
    {
        var user = CreateActiveUser("user@example.com", "hashed_pw");
        _usersRepo.AsQueryable().Returns(new TestAsyncEnumerable<User>([user]));
        _passwordHasher.VerifyPassword("wrongpass", "hashed_pw").Returns(false);

        var result = await _handler.Handle(new LoginCommand("user@example.com", "wrongpass"), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Unauthorized");
        result.Error.Description.Should().Be("Invalid email or password.");
    }

    [Fact]
    public async Task Handle_WithInactiveUser_ReturnsUnauthorizedError()
    {
        var user = CreateActiveUser("user@example.com", "hashed_pw");
        user.Deactivate();
        _usersRepo.AsQueryable().Returns(new TestAsyncEnumerable<User>([user]));
        _passwordHasher.VerifyPassword("password123", "hashed_pw").Returns(true);

        var result = await _handler.Handle(new LoginCommand("user@example.com", "password123"), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Unauthorized");
    }

    private static User CreateActiveUser(string email, string passwordHash)
    {
        var user = new User(email, "Test", "User", passwordHash);
        user.Activate();
        return user;
    }
}
