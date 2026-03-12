using Application.Features.Users.Commands;
using Application.Features.Users.Dtos;
using AutoMapper;
using Domain.Entities;

namespace Application.Features.Users.Mappings;

/// <summary>
/// AutoMapper profile for User feature. Defines mappings between:
/// - <see cref="CreateUserCommand"/> → <see cref="User"/>
/// - <see cref="User"/> → <see cref="UserDto"/>
/// - <see cref="CreateUserDto"/> → <see cref="CreateUserCommand"/>
///
/// Automatically discovered and registered by the Application service extensions.
/// </summary>
public class CreateUserProfile : Profile
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CreateUserProfile"/> class.
    /// </summary>
    public CreateUserProfile()
    {
        // CreateUserCommand → User entity (for command handler)
        CreateMap<CreateUserCommand, User>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.ModifiedAt, opt => opt.Ignore())
            .ForMember(dest => dest.ModifiedBy, opt => opt.Ignore())
            .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
            .ForMember(dest => dest.PasswordHash, opt => opt.MapFrom(src => src.Password))
            .ForMember(dest => dest.Status, opt => opt.Ignore())
            .ForMember(dest => dest.UserRoles, opt => opt.Ignore());

        // User entity → UserDto (for queries)
        CreateMap<User, UserDto>();

        // CreateUserDto → CreateUserCommand (optional, for API endpoint mapping)
        CreateMap<CreateUserDto, CreateUserCommand>();
    }
}
