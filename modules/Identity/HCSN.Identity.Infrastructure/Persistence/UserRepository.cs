using Microsoft.EntityFrameworkCore;
using HCSN.Identity.Domain.Entities;
using HCSN.Identity.Domain.Interfaces;

namespace HCSN.Identity.Infrastructure.Persistence;

public class UserRepository : IUserRepository
{
    private readonly IdentityDbContext _context;
    
    public UserRepository(IdentityDbContext context)
    {
        _context = context;
    }
    
    public async Task<User?> GetByIdAsync(Guid id)
    {
        return await _context.Users
            .IgnoreQueryFilters()
            .Include(u => u.Tenant)
            .FirstOrDefaultAsync(u => u.Id == id);
    }
    
    public async Task<User?> GetByEmailAsync(string email)
    {
        return await _context.Users
            .Include(u => u.Tenant)
            .FirstOrDefaultAsync(u => u.Email == email);
    }
    
    public async Task<User?> GetByPhoneNumberAsync(string phoneNumber)
    {
        return await _context.Users
            .Include(u => u.Tenant)
            .FirstOrDefaultAsync(u => u.PhoneNumber == phoneNumber);
    }
    
    public async Task<List<User>> GetUsersBySystemAccessAsync(string systemName)
    {
        return await _context.Users
            .Where(u => u.AccessibleSystems.Contains(systemName) && u.IsActive)
            .Include(u => u.Tenant)
            .ToListAsync();
    }
    
    public async Task<bool> IsEmailUniqueAsync(string email)
    {
        return !await _context.Users.AnyAsync(u => u.Email == email);
    }
    
    public async Task<bool> IsPhoneUniqueAsync(string phoneNumber)
    {
        return !await _context.Users.AnyAsync(u => u.PhoneNumber == phoneNumber);
    }
    
    public async Task AddAsync(User user)
    {
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();
    }
    
    public async Task UpdateAsync(User user)
    {
        _context.Users.Update(user);
        await _context.SaveChangesAsync();
    }
    
    public async Task SoftDeleteAsync(Guid id)
    {
        var user = await GetByIdAsync(id);
        if (user != null)
        {
            user.SoftDelete();
            await _context.SaveChangesAsync();
        }
    }
    
    // New multi-tenant methods
    public async Task<List<User>> GetUsersByTenantAsync(Guid tenantId)
    {
        return await _context.Users
            .Where(u => u.TenantId == tenantId)
            .Include(u => u.Tenant)
            .ToListAsync();
    }
    
    public async Task<User?> GetByEmailAndTenantAsync(string email, Guid tenantId)
    {
        return await _context.Users
            .Where(u => u.TenantId == tenantId && u.Email == email)
            .Include(u => u.Tenant)
            .FirstOrDefaultAsync();
    }
    
    public async Task<List<User>> GetTenantAdminsAsync(Guid tenantId)
    {
        return await _context.Users
            .Where(u => u.TenantId == tenantId && u.IsTenantAdmin && u.IsActive)
            .Include(u => u.Tenant)
            .ToListAsync();
    }
    
    public async Task<List<User>> GetPendingUsersByTenantAsync(Guid tenantId)
    {
        return await _context.Users
            .Where(u => u.TenantId == tenantId && u.Status == UserStatus.PendingApproval)
            .Include(u => u.Tenant)
            .ToListAsync();
    }
}