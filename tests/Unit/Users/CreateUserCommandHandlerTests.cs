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
        // Use TestAsyncEnumerable directly (not a NSubstitute substitute) to avoid
        // thread-local state issues when configuring IRepository<T>.Query.
        _usersRepo.AsQueryable().Returns(new TestAsyncEnumerable<User>([]));

        var result = await _handler.Handle(
            new CreateUserCommand { Email = "new@example.com", FirstName = "New", LastName = "User", Password = "password123" },
            default);

        result.Email.Should().Be("new@example.com");
        result.FirstName.Should().Be("New");
        result.LastName.Should().Be("User");
        _usersRepo.Received(1).Add(Arg.Any<User>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithDuplicateEmail_ThrowsConflictException()
    {
        var existing = new User("taken@example.com", "Existing", "User", "hash");
        _usersRepo.AsQueryable().Returns(new TestAsyncEnumerable<User>([existing]));

        var act = () => _handler.Handle(
            new CreateUserCommand { Email = "taken@example.com", FirstName = "Dupe", LastName = "User", Password = "password123" },
            default);

        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("*taken@example.com*");
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
