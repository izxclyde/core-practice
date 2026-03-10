using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HCSN.Identity.Public;

public interface ITenantService
{
    Guid? GetCurrentTenantId();
    Task<TenantInfo?> GetCurrentTenantAsync();
    Task<bool> CurrentUserHasAccessToTenantAsync(Guid tenantId);
}

public record TenantInfo(Guid Id, string Name, string Subdomain, List<string> Features);
