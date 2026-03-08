using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using HCSN.Identity.Domain.Interfaces;
using HCSN.Identity.Public;
using System.Security.Claims;

namespace HCSN.Identity.Infrastructure.Services;

public class TenantService : ITenantService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ITenantRepository _tenantRepository;
    private readonly IUserRepository _userRepository;
    
    public TenantService(
        IHttpContextAccessor httpContextAccessor,
        ITenantRepository tenantRepository,
        IUserRepository userRepository)
    {
        _httpContextAccessor = httpContextAccessor;
        _tenantRepository = tenantRepository;
        _userRepository = userRepository;
    }
    
    public Guid? GetCurrentTenantId()
    {
        if (_httpContextAccessor.HttpContext?.Items["TenantId"] is Guid tenantId)
            return tenantId;
            
        return null;
    }
    
    public async Task<TenantInfo?> GetCurrentTenantAsync()
    {
        var tenantId = GetCurrentTenantId();
        if (!tenantId.HasValue)
            return null;

        var tenant = await _tenantRepository.GetByIdAsync(tenantId.Value);
        if (tenant == null)
            return null;

        var features = tenant.Features?.Keys.ToList() ?? new List<string>();

        return new TenantInfo(
            tenant.Id,
            tenant.Name,
            tenant.Subdomain,
            features
        );
    }
    
    public async Task<bool> CurrentUserHasAccessToTenantAsync(Guid tenantId)
    {
        var userIdClaim = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            return false;
            
        var user = await _userRepository.GetByIdAsync(userId);
        return user != null && user.TenantId == tenantId;
    }
}