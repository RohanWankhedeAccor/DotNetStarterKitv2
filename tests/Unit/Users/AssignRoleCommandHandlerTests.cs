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
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IRepository<User> _usersRepo = Substitute.For<IRepository<User>>();
    private readonly IRepository<Role> _rolesRepo = Substitute.For<IRepository<Role>>();
    private readonly IRepository<UserRole> _userRolesRepo = Substitute.For<IRepository<UserRole>>();
    private readonly AssignRoleCommandHandler _handler;

    public AssignRoleCommandHandlerTests()
    {
        _unitOfWork.Users.Returns(_usersRepo);
        _unitOfWork.Roles.Returns(_rolesRepo);
        _unitOfWork.UserRoles.Returns(_userRolesRepo);

        _handler = new AssignRoleCommandHandler(_unitOfWork);
    }

    [Fact]
    public async Task Handle_ValidInput_CreatesUserRoleAndReturnsUnit()
    {
        var user = CreateActiveUser("test@example.com");
        var role = new Role("Editor", "Can view and create users.");

        // Use TestAsyncEnumerable directly (not a NSubstitute substitute) to avoid
        // thread-local state issues when configuring IRepository<T>.Query.
        _usersRepo.AsQueryable().Returns(new TestAsyncEnumerable<User>([user]));
        _rolesRepo.AsQueryable().Returns(new TestAsyncEnumerable<Role>([role]));
        _userRolesRepo.AsQueryable().Returns(new TestAsyncEnumerable<UserRole>([]));
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);

        var result = await _handler.Handle(
            new AssignRoleCommand { UserId = user.Id, RoleName = "Editor" }, default);

        result.Should().Be(MediatRUnit.Value);
        _userRolesRepo.Received(1).Add(Arg.Any<UserRole>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_UserNotFound_ThrowsNotFoundException()
    {
        _usersRepo.AsQueryable().Returns(new TestAsyncEnumerable<User>([]));
        _rolesRepo.AsQueryable().Returns(new TestAsyncEnumerable<Role>([]));
        _userRolesRepo.AsQueryable().Returns(new TestAsyncEnumerable<UserRole>([]));

        var act = () => _handler.Handle(
            new AssignRoleCommand { UserId = Guid.NewGuid(), RoleName = "Editor" }, default);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_RoleNotFound_ThrowsNotFoundException()
    {
        var user = CreateActiveUser("test@example.com");

        _usersRepo.AsQueryable().Returns(new TestAsyncEnumerable<User>([user]));
        _rolesRepo.AsQueryable().Returns(new TestAsyncEnumerable<Role>([]));
        _userRolesRepo.AsQueryable().Returns(new TestAsyncEnumerable<UserRole>([]));

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

        _usersRepo.AsQueryable().Returns(new TestAsyncEnumerable<User>([user]));
        _rolesRepo.AsQueryable().Returns(new TestAsyncEnumerable<Role>([role]));
        _userRolesRepo.AsQueryable().Returns(new TestAsyncEnumerable<UserRole>([existingAssignment]));

        var result = await _handler.Handle(
            new AssignRoleCommand { UserId = user.Id, RoleName = "Editor" }, default);

        result.Should().Be(MediatRUnit.Value);
        _userRolesRepo.DidNotReceive().Add(Arg.Any<UserRole>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    private static User CreateActiveUser(string email)
    {
        var user = new User(email, "Test", "User", "hash");
        user.Activate();
        return user;
    }
}
