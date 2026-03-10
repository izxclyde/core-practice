using HCSN.Identity.Domain.Interfaces;
using HCSN.Identity.Public;
using MediatR;

namespace HCSN.Identity.Application.Features.Users.Queries;

public record GetUsersBySystemQuery(string SystemName) : IRequest<List<UserDto>>;

public class GetUsersBySystemQueryHandler : IRequestHandler<GetUsersBySystemQuery, List<UserDto>>
{
    private readonly IUserRepository _userRepository;

    public GetUsersBySystemQueryHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<List<UserDto>> Handle(
        GetUsersBySystemQuery request,
        CancellationToken cancellationToken
    )
    {
        var users = await _userRepository.GetUsersBySystemAccessAsync(request.SystemName);

        return users
            .Select(user => new UserDto(
                user.Id,
                user.Email,
                user.FullName,
                user.EmailConfirmed,
                user.TwoFactorEnabled,
                user.IsActive,
                user.AccessibleSystems,
                user.CreatedAt,
                user.UpdatedAt,
                user.DeletedAt,
                user.LastLoginAt
            ))
            .ToList();
    }
}
