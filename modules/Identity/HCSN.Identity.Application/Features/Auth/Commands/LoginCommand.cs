using MediatR;
using HCSN.Identity.Domain.Interfaces;
using HCSN.Identity.Application.Interfaces;
using HCSN.Identity.Public;
using HCSN.Identity.Domain.Entities;

namespace HCSN.Identity.Application.Features.Auth.Commands;

public record LoginCommand : IRequest<AuthResult>
{
    public string Username { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public bool RememberMe { get; init; }
}

public class LoginCommandHandler : IRequestHandler<LoginCommand, AuthResult>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenGenerator _tokenGenerator;
    
    public LoginCommandHandler(
        IUserRepository userRepository, 
        IPasswordHasher passwordHasher,
        ITokenGenerator tokenGenerator)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _tokenGenerator = tokenGenerator;
    }
    
    public async Task<AuthResult> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        // Find user by email
        var user = await _userRepository.GetByEmailAsync(request.Username);
        if (user == null)
        {
            return new AuthResult(false, null, null, null, "Invalid credentials");
        }
        
        // Check if user is soft deleted
        if (user.DeletedAt != null)
        {
            return new AuthResult(false, null, null, null, "Account has been deleted");
        }
        
        // Verify password
        if (user.PasswordHash == null || !_passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            return new AuthResult(false, null, null, null, "Invalid credentials");
        }
        
        // Check if user is active
        if (!user.IsActive || user.Status != UserStatus.Active)
        {
            return new AuthResult(false, null, null, null, "Account is not active");
        }
        user.UpdateLastLogin(); 
        // Generate tokens
        var token = _tokenGenerator.GenerateToken(user);
        var refreshToken = _tokenGenerator.GenerateRefreshToken();
        user.UpdateRefreshToken(refreshToken, DateTime.UtcNow.AddDays(request.RememberMe ? 30 : 7));
        
        await _userRepository.UpdateAsync(user);
        
        var userDto = new UserDto(
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
        
        return new AuthResult(true, token, refreshToken, userDto, null);
    }
}