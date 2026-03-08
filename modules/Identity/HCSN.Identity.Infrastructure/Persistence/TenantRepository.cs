using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using HCSN.Identity.Domain.Entities;
using HCSN.Identity.Domain.Interfaces;
using HCSN.Identity.Infrastructure.Persistence;

namespace HCSN.Identity.Infrastructure.Persistence;

public class TenantRepository : ITenantRepository
{
    private readonly IdentityDbContext _context;
    
    public TenantRepository(IdentityDbContext context)
    {
        _context = context;
    }
    
    public async Task<Tenant?> GetByIdAsync(Guid id)
    {
        return await _context.Tenants
            .Include(t => t.Users)
            .Include(t => t.Modules)
            .FirstOrDefaultAsync(t => t.Id == id);
    }
    
    public async Task<Tenant?> GetBySubdomainAsync(string subdomain)
    {
        return await _context.Tenants
            .Include(t => t.Users)
            .Include(t => t.Modules)
            .FirstOrDefaultAsync(t => t.Subdomain == subdomain);
    }
    
    // ADD THIS - Implement GetByCustomDomainAsync
    public async Task<Tenant?> GetByCustomDomainAsync(string customDomain)
    {
        return await _context.Tenants
            .Include(t => t.Users)
            .Include(t => t.Modules)
            .FirstOrDefaultAsync(t => t.CustomDomain == customDomain);
    }
    
    // ADD THIS - Implement GetAllAsync
    public async Task<IEnumerable<Tenant>> GetAllAsync()
    {
        return await _context.Tenants
            .Include(t => t.Users)
            .Include(t => t.Modules)
            .ToListAsync();
    }
    
    public async Task AddAsync(Tenant tenant)
    {
        await _context.Tenants.AddAsync(tenant);
        await _context.SaveChangesAsync();
    }
    
    public async Task UpdateAsync(Tenant tenant)
    {
        _context.Tenants.Update(tenant);
        await _context.SaveChangesAsync();
    }
    
    public async Task DeleteAsync(Guid id)
    {
        var tenant = await GetByIdAsync(id);
        if (tenant != null)
        {
            _context.Tenants.Remove(tenant);
            await _context.SaveChangesAsync();
        }
    }
    
    // Optional: Add a method to get by identifier (subdomain or custom domain)
    public async Task<Tenant?> GetByIdentifierAsync(string identifier)
    {
        return await _context.Tenants
            .Include(t => t.Users)
            .Include(t => t.Modules)
            .FirstOrDefaultAsync(t => t.Subdomain == identifier || t.CustomDomain == identifier);
    }


    public async Task<IEnumerable<Tenant>> GetAllActiveAsync()
    {
        return await _context.Tenants
            .Where(t => t.IsActive)
            .ToListAsync();
    }


    public async Task<bool> IsSubdomainUniqueAsync(string subdomain)
    {
        return !await _context.Tenants
            .AnyAsync(t => t.Subdomain == subdomain);
    }    
}