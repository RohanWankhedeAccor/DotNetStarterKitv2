using System.Security.Claims;
using Application.Features.Auth.Commands;
using Application.Interfaces;
using Domain.Entities;
using Domain.Exceptions;
using FluentAssertions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Unit.Helpers;

namespace Unit.Auth;

public class AzureLoginCommandHandlerTests
{
    private readonly IApplicationDbContext _context = Substitute.For<IApplicationDbContext>();
    private readonly IAzureAdTokenValidator _validator = Substitute.For<IAzureAdTokenValidator>();
    private readonly ITokenService _tokenService = Substitute.For<ITokenService>();
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly AzureLoginCommandHandler _handler;

    public AzureLoginCommandHandlerTests()
    {
        _handler = new AzureLoginCommandHandler(_context, _validator, _tokenService, _currentUser);
        _tokenService.ExpirationMinutes.Returns(60);
        _tokenService.GenerateToken(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
                Arg.Any<IEnumerable<string>>(), Arg.Any<IEnumerable<string>>())
            .Returns("test.azure.jwt");
    }

    [Fact]
    public async Task Handle_WithValidToken_CreatesNewUserAndReturnsLoginResponse()
    {
        SetupValidatorReturns("oid-123", "azure@example.com", "Azure User");
        var usersSet = DbSetMockHelper.Create<User>([]);
        _context.Users.Returns(usersSet);
        _context.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);

        var result = await _handler.Handle(new AzureLoginCommand("valid.token"), default);

        result.Token.Should().Be("test.azure.jwt");
        result.Email.Should().Be("azure@example.com");
        await _context.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithExistingUser_UpdatesDisplayNameAndReturnsToken()
    {
        SetupValidatorReturns("oid-123", "azure@example.com", "Updated Name");
        var existing = new User("azure@example.com", "Old", "Name", null);
        existing.ProvisionAzureAd("oid-123");
        existing.Activate();
        var usersSet = DbSetMockHelper.Create([existing]);
        _context.Users.Returns(usersSet);
        _context.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);

        var result = await _handler.Handle(new AzureLoginCommand("valid.token"), default);

        result.Token.Should().Be("test.azure.jwt");
        existing.FirstName.Should().Be("Updated");
        existing.LastName.Should().Be("Name");
    }

    [Fact]
    public async Task Handle_WithInvalidToken_ThrowsAzureAdTokenValidationException()
    {
        _validator.ValidateTokenAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new Exception("bad signature"));

        var act = () => _handler.Handle(new AzureLoginCommand("bad.token"), default);

        await act.Should().ThrowAsync<AzureAdTokenValidationException>()
            .WithMessage("*Failed to validate Azure AD token*");
    }

    [Fact]
    public async Task Handle_WithMissingOidClaim_ThrowsAzureAdTokenValidationException()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim("email", "user@example.com"),
        ]));
        _validator.ValidateTokenAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(principal);
        _validator.GetClaimValue(principal, "oid").Returns((string?)null);
        _validator.GetClaimValue(principal, "email").Returns("user@example.com");

        var act = () => _handler.Handle(new AzureLoginCommand("no.oid.token"), default);

        await act.Should().ThrowAsync<AzureAdTokenValidationException>()
            .WithMessage("*oid*");
    }

    private void SetupValidatorReturns(string oid, string email, string name)
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim("oid", oid),
            new Claim("email", email),
            new Claim("name", name),
        ]));
        _validator.ValidateTokenAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(principal);
        _validator.GetClaimValue(principal, "oid").Returns(oid);
        _validator.GetClaimValue(principal, "email").Returns(email);
        _validator.GetClaimValue(principal, "name").Returns(name);
        _validator.GetClaimValue(principal, "preferred_username").Returns((string?)null);
        _validator.GetClaimValue(principal, "upn").Returns((string?)null);
    }
}
