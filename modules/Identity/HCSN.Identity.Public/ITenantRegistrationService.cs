using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HCSN.Identity.Public;

public interface ITenantRegistrationService
{
    Task<RegistrationResult> RegisterAsync(TenantRegisterRequest request);
    Task<RegistrationSettingsDto> GetRegistrationSettingsAsync(string subdomain);
    Task<bool> ValidateEmailDomainAsync(string email, string subdomain);
    Task<RegistrationResult> RegisterWithInviteAsync(string token, BaseRegisterRequest request);
    Task<List<string>> GetRequiredFieldsAsync(string subdomain);
}

public interface ITenantAdminService
{
    Task<RegistrationResult> RejectUserAsync(Guid userId, Guid tenantId, string reason);
    Task<List<PendingUserDto>> GetPendingApprovalsAsync(Guid tenantId);
    Task<RegistrationResult> ApproveUserAsync(Guid userId, Guid tenantId, string? notes = null);
}