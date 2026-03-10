using HCSN.Identity.Domain.Entities;

namespace HCSN.Identity.Application.Interfaces;

public interface ITokenGenerator
{
    string GenerateToken(User user);
    string GenerateRefreshToken();
}
