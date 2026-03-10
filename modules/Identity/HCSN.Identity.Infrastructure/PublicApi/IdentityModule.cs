using HCSN.Identity.Application.Features.Auth.Commands;
using HCSN.Identity.Domain.Entities;
using HCSN.Identity.Domain.Interfaces;
using HCSN.Identity.Public;
using MediatR;

namespace HCSN.Identity.Infrastructure.PublicApi;

public class IdentityModule : IIdentityModule
{
    private readonly IMediator _mediator;
    private readonly IUserRepository _userRepository;

    public IdentityModule(IMediator mediator, IUserRepository userRepository)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
    }

    public async Task<AuthResult> LoginAsync(LoginRequest request)
    {
        var command = new LoginCommand
        {
            Username = request.Username,
            Password = request.Password,
            RememberMe = request.RememberMe,
        };

        return await _mediator.Send(command);
    }

    public async Task<AuthResult> RegisterAsync(RegisterRequest request)
    {
        var command = new RegisterCommand
        {
            Email = request.Email,
            PhoneNumber = request.PhoneNumber,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Password = request.Password,
            AccessibleSystems = request.AccessibleSystems,
        };

        return await _mediator.Send(command);
    }

    public async Task<UserDto?> GetUserByIdAsync(Guid userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
            return null;

        return MapToDto(user);
    }

    public async Task<UserDto?> GetUserByEmailAsync(string email)
    {
        var user = await _userRepository.GetByEmailAsync(email);
        if (user == null)
            return null;

        return MapToDto(user);
    }

    public async Task<List<string>> GetUserAccessibleSystemsAsync(Guid userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        return user?.AccessibleSystems ?? new List<string>();
    }

    public async Task<AuthResult> RefreshTokenAsync(string refreshToken)
    {
        return new AuthResult(false, null, null, null, "Not implemented");
    }

    public Task LogoutAsync(Guid userId)
    {
        return Task.CompletedTask;
    }

    private static UserDto MapToDto(HCSN.Identity.Domain.Entities.User user) // Explicitly qualify
    {
        return new UserDto(
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
        );
    }
}
