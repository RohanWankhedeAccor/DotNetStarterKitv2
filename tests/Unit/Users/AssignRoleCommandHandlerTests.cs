using Application.Features.Users.Commands;
using Application.Interfaces;
using Domain.Entities;
using Domain.Exceptions;
using FluentAssertions;
using MediatR;
using NSubstitute;
using Unit.Helpers;

// Disambiguate: MediatR.Unit vs the test project namespace 'Unit'
using MediatRUnit = MediatR.Unit;

namespace Unit.Users;

public class AssignRoleCommandHandlerTests
{
    private readonly IApplicationDbContext _context = Substitute.For<IApplicationDbContext>();
    private readonly AssignRoleCommandHandler _handler;

    public AssignRoleCommandHandlerTests()
    {
        _handler = new AssignRoleCommandHandler(_context);
    }

    [Fact]
    public async Task Handle_ValidInput_CreatesUserRoleAndReturnsUnit()
    {
        var user = CreateActiveUser("test@example.com");
        var role = new Role("Editor", "Can view and create users.");

        var usersSet = DbSetMockHelper.Create([user]);
        var rolesSet = DbSetMockHelper.Create([role]);
        var userRolesSet = DbSetMockHelper.Create<UserRole>([]);
        _context.Users.Returns(usersSet);
        _context.Roles.Returns(rolesSet);
        _context.UserRoles.Returns(userRolesSet);
        _context.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);

        var result = await _handler.Handle(
            new AssignRoleCommand { UserId = user.Id, RoleName = "Editor" }, default);

        result.Should().Be(MediatRUnit.Value);
        _context.UserRoles.Received(1).Add(Arg.Any<UserRole>());
        await _context.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_UserNotFound_ThrowsNotFoundException()
    {
        var usersSet = DbSetMockHelper.Create<User>([]);
        var rolesSet = DbSetMockHelper.Create<Role>([]);
        var userRolesSet = DbSetMockHelper.Create<UserRole>([]);
        _context.Users.Returns(usersSet);
        _context.Roles.Returns(rolesSet);
        _context.UserRoles.Returns(userRolesSet);

        var act = () => _handler.Handle(
            new AssignRoleCommand { UserId = Guid.NewGuid(), RoleName = "Editor" }, default);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_RoleNotFound_ThrowsNotFoundException()
    {
        var user = CreateActiveUser("test@example.com");

        var usersSet = DbSetMockHelper.Create([user]);
        var rolesSet = DbSetMockHelper.Create<Role>([]);
        var userRolesSet = DbSetMockHelper.Create<UserRole>([]);
        _context.Users.Returns(usersSet);
        _context.Roles.Returns(rolesSet);
        _context.UserRoles.Returns(userRolesSet);

        var act = () => _handler.Handle(
            new AssignRoleCommand { UserId = user.Id, RoleName = "NonExistent" }, default);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_RoleAlreadyAssigned_IsIdempotentAndDoesNotSave()
    {
        var user = CreateActiveUser("test@example.com");
        var role = new Role("Editor", "Can view and create users.");
        var existingAssignment = new UserRole(user.Id, role.Id);

        var usersSet = DbSetMockHelper.Create([user]);
        var rolesSet = DbSetMockHelper.Create([role]);
        var userRolesSet = DbSetMockHelper.Create([existingAssignment]);
        _context.Users.Returns(usersSet);
        _context.Roles.Returns(rolesSet);
        _context.UserRoles.Returns(userRolesSet);

        var result = await _handler.Handle(
            new AssignRoleCommand { UserId = user.Id, RoleName = "Editor" }, default);

        result.Should().Be(MediatRUnit.Value);
        _context.UserRoles.DidNotReceive().Add(Arg.Any<UserRole>());
        await _context.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    private static User CreateActiveUser(string email)
    {
        var user = new User(email, "Test", "User", "hash");
        user.Activate();
        return user;
    }
}
