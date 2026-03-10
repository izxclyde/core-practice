using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HCSN.Identity.Domain.Entities;

namespace HCSN.Identity.Domain.Interfaces;

public interface ITenantRepository
{
    Task<IEnumerable<Tenant>> GetAllAsync();
    Task<Tenant?> GetByIdAsync(Guid id);
    Task<Tenant?> GetBySubdomainAsync(string subdomain);
    Task<Tenant?> GetByCustomDomainAsync(string customDomain);
    Task<IEnumerable<Tenant>> GetAllActiveAsync();
    Task AddAsync(Tenant tenant); // ← fix here
    Task UpdateAsync(Tenant tenant);
    Task<bool> IsSubdomainUniqueAsync(string subdomain);
}
