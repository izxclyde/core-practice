using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HCSN.Identity.Public;

public interface IIdentityModule
{
    Task<AuthResult> LoginAsync(LoginRequest request);
    Task<AuthResult> RegisterAsync(RegisterRequest request);
    Task<UserDto?> GetUserByIdAsync(Guid userId);
    Task<UserDto?> GetUserByEmailAsync(string email);
    Task<List<string>> GetUserAccessibleSystemsAsync(Guid userId);
    Task<AuthResult> RefreshTokenAsync(string refreshToken);
    Task LogoutAsync(Guid userId);
}

public record LoginRequest(string Username, string Password, bool RememberMe = false);

public record RegisterRequest(
    string Email,
    string PhoneNumber,
    string FirstName,
    string LastName,
    string Password,
    List<string>? AccessibleSystems = null
);

public record AuthResult(
    bool Success,
    string? Token,
    string? RefreshToken,
    UserDto? User,
    string? Error
);

public record UserDto(
    Guid Id,
    string Email,
    string FullName,
    bool EmailConfirmed,
    bool TwoFactorEnabled,
    bool IsActive,
    List<string> AccessibleSystems,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    DateTime? DeletedAt,
    DateTime? LastLoginAt
);
