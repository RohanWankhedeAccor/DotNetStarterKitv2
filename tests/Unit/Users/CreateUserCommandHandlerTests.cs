using Application.Features.Users.Commands;
using Application.Features.Users.Dtos;
using Application.Interfaces;
using AutoMapper;
using Domain.Entities;
using FluentAssertions;
using NSubstitute;
using Unit.Helpers;

namespace Unit.Users;

public class CreateUserCommandHandlerTests
{
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IRepository<User> _usersRepo = Substitute.For<IRepository<User>>();
    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();
    private readonly ICacheService _cache = Substitute.For<ICacheService>();
    private readonly IMapper _mapper;
    private readonly CreateUserCommandHandler _handler;

    public CreateUserCommandHandlerTests()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.CreateMap<User, UserDto>()
               .ForMember(d => d.Id, o => o.MapFrom(s => s.Id))
               .ForMember(d => d.Email, o => o.MapFrom(s => s.Email))
               .ForMember(d => d.FirstName, o => o.MapFrom(s => s.FirstName))
               .ForMember(d => d.LastName, o => o.MapFrom(s => s.LastName)));
        _mapper = config.CreateMapper();

        _unitOfWork.Users.Returns(_usersRepo);
        _passwordHasher.HashPassword(Arg.Any<string>()).Returns("hashed_password");
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);

        _handler = new CreateUserCommandHandler(_unitOfWork, _mapper, _passwordHasher, _cache);
    }

    [Fact]
    public async Task Handle_WithUniqueEmail_CreatesUserAndReturnsDto()
    {
        _usersRepo.AsQueryable().Returns(new TestAsyncEnumerable<User>([]));

        var result = await _handler.Handle(
            new CreateUserCommand { Email = "new@example.com", FirstName = "New", LastName = "User", Password = "password123" },
            default);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Email.Should().Be("new@example.com");
        result.Value.FirstName.Should().Be("New");
        result.Value.LastName.Should().Be("User");
        _usersRepo.Received(1).Add(Arg.Any<User>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithDuplicateEmail_ReturnsConflictError()
    {
        var existing = new User("taken@example.com", "Existing", "User", "hash");
        _usersRepo.AsQueryable().Returns(new TestAsyncEnumerable<User>([existing]));

        var result = await _handler.Handle(
            new CreateUserCommand { Email = "taken@example.com", FirstName = "Dupe", LastName = "User", Password = "password123" },
            default);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Conflict");
        result.Error.Description.Should().Contain("taken@example.com");
    }

    [Fact]
    public async Task Handle_HashesPasswordBeforeSaving()
    {
        _usersRepo.AsQueryable().Returns(new TestAsyncEnumerable<User>([]));

        await _handler.Handle(
            new CreateUserCommand { Email = "hash@example.com", FirstName = "Hashed", LastName = "User", Password = "plaintext" },
            default);

        _passwordHasher.Received(1).HashPassword("plaintext");
    }
}
