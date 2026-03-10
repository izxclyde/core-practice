using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HCSN.Identity.Domain.Entities;

namespace HCSN.Identity.Domain.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id);
    Task<User?> GetByEmailAsync(string email);
    Task<User?> GetByPhoneNumberAsync(string phoneNumber);
    Task<List<User>> GetUsersBySystemAccessAsync(string systemName);
    Task<bool> IsEmailUniqueAsync(string email);
    Task<bool> IsPhoneUniqueAsync(string phoneNumber);
    Task AddAsync(User user);
    Task UpdateAsync(User user);
    Task SoftDeleteAsync(Guid id);

    // New multi-tenant methods
    Task<List<User>> GetUsersByTenantAsync(Guid tenantId);
    Task<User?> GetByEmailAndTenantAsync(string email, Guid tenantId);
    Task<List<User>> GetTenantAdminsAsync(Guid tenantId);
    Task<List<User>> GetPendingUsersByTenantAsync(Guid tenantId);
}
