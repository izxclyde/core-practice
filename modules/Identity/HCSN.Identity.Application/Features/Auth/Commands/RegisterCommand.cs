using HCSN.Identity.Application.Interfaces;
using HCSN.Identity.Domain.Entities;
using HCSN.Identity.Domain.Interfaces;
using HCSN.Identity.Public;
using MediatR;

namespace HCSN.Identity.Application.Features.Auth.Commands;

public record RegisterCommand : IRequest<AuthResult>
{
    public string Email { get; init; } = string.Empty;
    public string PhoneNumber { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public List<string>? AccessibleSystems { get; init; }
}

public class RegisterCommandHandler : IRequestHandler<RegisterCommand, AuthResult>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenGenerator _tokenGenerator;

    public RegisterCommandHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        ITokenGenerator tokenGenerator
    )
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _tokenGenerator = tokenGenerator;
    }

    public async Task<AuthResult> Handle(
        RegisterCommand request,
        CancellationToken cancellationToken
    )
    {
        // Check if email exists
        var existingUser = await _userRepository.GetByEmailAsync(request.Email);
        if (existingUser != null)
        {
            return new AuthResult(false, null, null, null, "Email already exists");
        }

        // Check if phone exists
        var existingPhone = await _userRepository.GetByPhoneNumberAsync(request.PhoneNumber);
        if (existingPhone != null)
        {
            return new AuthResult(false, null, null, null, "Phone number already exists");
        }

        // Create user with accessible systems
        var defaultSystems = new List<string> { "SalesCRM" }; // Default system access
        var systems = request.AccessibleSystems ?? defaultSystems;

        var user = new User(
            request.Email,
            request.PhoneNumber,
            request.FirstName,
            request.LastName,
            systems
        );
        var passwordHash = _passwordHasher.Hash(request.Password);
        user.SetPassword(passwordHash);

        await _userRepository.AddAsync(user);

        // Generate tokens
        var token = _tokenGenerator.GenerateToken(user);
        var refreshToken = _tokenGenerator.GenerateRefreshToken();
        user.UpdateRefreshToken(refreshToken, DateTime.UtcNow.AddDays(7));

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
            null
        );

        return new AuthResult(true, token, refreshToken, userDto, null);
    }
}
