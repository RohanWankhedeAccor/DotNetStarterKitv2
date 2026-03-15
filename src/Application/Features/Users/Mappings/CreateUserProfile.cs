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
        // Note: FirstName, LastName, and PasswordHash are set via the User constructor in the handler,
        // not via property assignment (they have private setters).
        CreateMap<CreateUserCommand, User>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.Username, opt => opt.Ignore())
            .ForMember(dest => dest.FirstName, opt => opt.Ignore())
            .ForMember(dest => dest.LastName, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.ModifiedAt, opt => opt.Ignore())
            .ForMember(dest => dest.ModifiedBy, opt => opt.Ignore())
            .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
            .ForMember(dest => dest.PasswordHash, opt => opt.Ignore())
            .ForMember(dest => dest.Status, opt => opt.Ignore())
            .ForMember(dest => dest.UserRoles, opt => opt.Ignore());

        // User entity → UserDto (used by CreateUserCommandHandler for the 201 response).
        // Roles are projected from the UserRoles navigation; for a brand-new user this will be [].
        CreateMap<User, UserDto>()
            .ForMember(dest => dest.Roles, opt => opt.MapFrom(src =>
                src.UserRoles
                    .Where(ur => ur.Role != null)
                    .Select(ur => ur.Role!.Name)
                    .ToList()));

        // CreateUserDto → CreateUserCommand (optional, for API endpoint mapping)
        CreateMap<CreateUserDto, CreateUserCommand>();
    }
}
