using Application.Features.Users.Commands;
using Application.Features.Users.Dtos;
using Application.Interfaces;
using AutoMapper;
using Domain.Entities;
using Domain.Exceptions;
using FluentAssertions;
using NSubstitute;
using Unit.Helpers;

namespace Unit.Users;

public class CreateUserCommandHandlerTests
{
    private readonly IApplicationDbContext _context = Substitute.For<IApplicationDbContext>();
    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();
    private readonly IMapper _mapper;
    private readonly CreateUserCommandHandler _handler;

    public CreateUserCommandHandlerTests()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.CreateMap<User, UserDto>()
               .ForMember(d => d.Id, o => o.MapFrom(s => s.Id))
               .ForMember(d => d.Email, o => o.MapFrom(s => s.Email))
               .ForMember(d => d.FullName, o => o.MapFrom(s => s.FullName)));
        _mapper = config.CreateMapper();

        _passwordHasher.HashPassword(Arg.Any<string>()).Returns("hashed_password");
        _context.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);

        _handler = new CreateUserCommandHandler(_context, _mapper, _passwordHasher);
    }

    [Fact]
    public async Task Handle_WithUniqueEmail_CreatesUserAndReturnsDto()
    {
        var usersSet = DbSetMockHelper.Create<User>([]);
        _context.Users.Returns(usersSet);

        var result = await _handler.Handle(
            new CreateUserCommand { Email = "new@example.com", FullName = "New User", Password = "password123" },
            default);

        result.Email.Should().Be("new@example.com");
        result.FullName.Should().Be("New User");
        _context.Users.Received(1).Add(Arg.Any<User>());
        await _context.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithDuplicateEmail_ThrowsConflictException()
    {
        var existing = new User("taken@example.com", "Existing", "hash");
        var usersSet = DbSetMockHelper.Create([existing]);
        _context.Users.Returns(usersSet);

        var act = () => _handler.Handle(
            new CreateUserCommand { Email = "taken@example.com", FullName = "Dupe", Password = "password123" },
            default);

        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("*taken@example.com*");
    }

    [Fact]
    public async Task Handle_HashesPasswordBeforeSaving()
    {
        var usersSet = DbSetMockHelper.Create<User>([]);
        _context.Users.Returns(usersSet);

        await _handler.Handle(
            new CreateUserCommand { Email = "hash@example.com", FullName = "Hashed", Password = "plaintext" },
            default);

        _passwordHasher.Received(1).HashPassword("plaintext");
    }
}
