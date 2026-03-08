using System;
using System.Linq;  // For LINQ methods like ToList(), Count(), GroupBy(), Sum()
using System.Collections.Generic;  // For List<T>
using System.Threading.Tasks;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HCSN.Identity.Domain.Entities;
using HCSN.Identity.Domain.Interfaces;
using HCSN.Identity.Public;
using Microsoft.Extensions.Logging;

namespace HCSN.Identity.API.Controllers;

[Authorize]
[ApiController]
[Route("api/system/tenants")]
public class SystemAdminController : ControllerBase
{
    private readonly ITenantRepository _tenantRepository;
    private readonly IUserRepository _userRepository;
    private readonly ILogger<SystemAdminController> _logger;
    
    public SystemAdminController(
        ITenantRepository tenantRepository,
        IUserRepository userRepository,
        ILogger<SystemAdminController> logger)
    {
        _tenantRepository = tenantRepository;
        _userRepository = userRepository;
        _logger = logger;
    }
    
    private async Task<bool> IsSystemAdmin()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            return false;
            
        var user = await _userRepository.GetByIdAsync(userId);
        
        return user != null && 
               user.IsActive &&
               (user.UserType == UserType.SuperAdmin || 
                (user.AccessibleSystems?.Contains("SystemAdmin") == true));
    }
    
    private async Task<User?> GetCurrentUser()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            return null;
            
        return await _userRepository.GetByIdAsync(userId);
    }
    
    // Conversion methods
    private TenantType ConvertToDomainType(TenantTypeDto dtoType)
    {
        return dtoType switch
        {
            TenantTypeDto.Standard => TenantType.Standard,
            TenantTypeDto.Enterprise => TenantType.Enterprise,
            TenantTypeDto.Trial => TenantType.Trial,
            TenantTypeDto.Internal => TenantType.Internal,
            TenantTypeDto.WhiteLabel => TenantType.WhiteLabel,
            TenantTypeDto.System => TenantType.System,
            _ => TenantType.Standard
        };
    }
    
    private TenantDeploymentModel ConvertToDomainModel(TenantDeploymentModelDto dtoModel)
    {
        return dtoModel switch
        {
            TenantDeploymentModelDto.Shared => TenantDeploymentModel.Shared,
            TenantDeploymentModelDto.Dedicated => TenantDeploymentModel.Dedicated,
            TenantDeploymentModelDto.Isolated => TenantDeploymentModel.Isolated,
            _ => TenantDeploymentModel.Shared
        };
    }
    
    [HttpPost]
[HttpPost]
public async Task<ActionResult<TenantDto>> CreateTenant(CreateTenantWithSettingsRequest request)
{
    if (!await IsSystemAdmin())
        return Forbid("SystemAdmin access required");
        
    var currentUser = await GetCurrentUser();
        
    // Check if subdomain already exists
    var existing = await _tenantRepository.GetBySubdomainAsync(request.Subdomain);
    if (existing != null)
    {
        return BadRequest(new { error = "Subdomain already exists", code = "SUBDOMAIN_EXISTS" });
    }
    
    // Check if custom domain already exists (if provided)
    if (!string.IsNullOrEmpty(request.CustomDomain))
    {
        var existingCustom = await _tenantRepository.GetByCustomDomainAsync(request.CustomDomain);
        if (existingCustom != null)
        {
            return BadRequest(new { error = "Custom domain already exists", code = "CUSTOM_DOMAIN_EXISTS" });
        }
    }
    
    try
    {
        // Convert DTO enums to domain enums
        var domainType = ConvertToDomainType(request.Type);
        var domainModel = ConvertToDomainModel(request.DeploymentModel);
        
        // Create new tenant with enhanced properties
        var tenant = new Tenant(
            request.Name, 
            request.Subdomain,
            connectionString: null,
            type: domainType,
            deploymentModel: domainModel);
        
        // Set custom domain using the method (not property)
        if (!string.IsNullOrEmpty(request.CustomDomain))
        {
            tenant.SetCustomDomain(request.CustomDomain);
        }
        
        // Set notes using the Suspend method or add a SetNotes method to Tenant entity
        // For now, we'll skip notes or you can add a SetNotes method to Tenant.cs
        
        // Apply settings if provided - CONVERT FROM DTO TO DOMAIN
        if (request.Settings != null)
        {
            var settings = ConvertToDomainSettings(request.Settings);
            tenant.UpdateSettings(settings);
        }
        
        // Apply branding if provided - CONVERT FROM DTO TO DOMAIN
        if (request.Branding != null)
        {
            var branding = ConvertToDomainBranding(request.Branding);
            tenant.UpdateBranding(branding);
        }
        
        // Apply security policy if provided - CONVERT FROM DTO TO DOMAIN
        if (request.SecurityPolicy != null)
        {
            var securityPolicy = ConvertToDomainSecurityPolicy(request.SecurityPolicy);
            tenant.UpdateSecurityPolicy(securityPolicy);
        }
        
        // Apply limits if provided - CONVERT FROM DTO TO DOMAIN
        if (request.Limits != null)
        {
            var limits = ConvertToDomainLimits(request.Limits);
            tenant.UpdateLimits(limits);
        }
        
        // Apply billing info if provided - CONVERT FROM DTO TO DOMAIN
        if (request.Billing != null)
        {
            var billing = ConvertToDomainBilling(request.Billing);
            tenant.UpdateBilling(billing);
        }
        
        // Add features
        if (request.Features != null)
        {
            foreach (var feature in request.Features)
            {
                tenant.EnableFeature(feature.Key, feature.Value);
            }
        }
        
        // Add allowed domains
        if (request.AllowedDomains != null)
        {
            foreach (var domain in request.AllowedDomains)
            {
                tenant.AddAllowedDomain(domain);
            }
        }
        
        // Add metadata
        if (request.Metadata != null)
        {
            foreach (var item in request.Metadata)
            {
                tenant.AddMetadata(item.Key, item.Value);
            }
        }
        
        // Activate tenant
        tenant.Activate();
        
        await _tenantRepository.AddAsync(tenant);
        
        _logger.LogInformation("Tenant created: {TenantName} ({TenantId}) by {User}", 
            tenant.Name, tenant.Id, currentUser?.Email ?? "Unknown");
        
        return Ok(MapToTenantDto(tenant));
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error creating tenant {TenantName}", request.Name);
        return StatusCode(500, new { error = "Error creating tenant", code = "CREATION_FAILED" });
    }
}

// Add these conversion methods to your controller

private TenantSettings ConvertToDomainSettings(TenantSettingsDto dto)
{
    if (dto == null) return new TenantSettings();
    
    return new TenantSettings
    {
        TimeZone = dto.TimeZone,
        DateFormat = dto.DateFormat,
        TimeFormat = dto.TimeFormat,
        Currency = dto.Currency,
        Language = dto.Language,
        Registration = ConvertToDomainRegistration(dto.Registration),
        AllowUserInvitations = dto.AllowUserInvitations,
        AllowUserDeletion = dto.AllowUserDeletion,
        SessionTimeoutMinutes = dto.SessionTimeoutMinutes,
        RequireMfa = dto.RequireMfa,
        DefaultTheme = dto.DefaultTheme,
        CustomSettings = dto.CustomSettings ?? new Dictionary<string, object>()
    };
}

private RegistrationSettings ConvertToDomainRegistration(RegistrationSettingsDto dto)
{
    if (dto == null) return new RegistrationSettings();
    
    var settings = new RegistrationSettings
    {
        AllowPublicRegistration = dto.AllowPublicRegistration,
        RequireEmailConfirmation = dto.RequireEmailConfirmation,
        RequireAdminApproval = dto.RequireAdminApproval,
        AllowSocialLogin = dto.AllowSocialLogin,
        SocialLoginProviders = dto.SocialLoginProviders ?? new List<string>(),
        WelcomeEmailTemplate = dto.WelcomeEmailTemplate,
        RegistrationSuccessUrl = dto.RegistrationSuccessUrl,
        InvitationExpiryHours = dto.InvitationExpiryHours,
        RedirectAfterLogin = dto.RedirectAfterLogin,
        AutoProvisionWorkspace = dto.AutoProvisionWorkspace,
        DefaultRole = dto.DefaultRole,
        SendWelcomeEmail = dto.SendWelcomeEmail,
        CustomFields = new List<CustomField>()
    };
    
    // Convert custom fields
    if (dto.CustomFields != null)
    {
        foreach (var field in dto.CustomFields)
        {
            settings.CustomFields.Add(new CustomField
            {
                FieldName = field.FieldName,
                FieldType = field.FieldType,
                IsRequired = field.IsRequired,
                IsVisible = field.IsVisible,
                DefaultValue = field.DefaultValue,
                Options = field.Options,
                DisplayOrder = field.DisplayOrder,
                Placeholder = field.Placeholder,
                ValidationRegex = field.ValidationRegex,
                ErrorMessage = field.ErrorMessage,
                Metadata = field.Metadata
            });
        }
    }
    
    return settings;
}

private TenantBranding ConvertToDomainBranding(TenantBrandingDto dto)
{
    if (dto == null) return new TenantBranding();
    
    return new TenantBranding
    {
        LogoUrl = dto.LogoUrl,
        FaviconUrl = dto.FaviconUrl,
        PrimaryColor = dto.PrimaryColor,
        SecondaryColor = dto.SecondaryColor,
        AccentColor = dto.AccentColor,
        BackgroundColor = dto.BackgroundColor,
        TextColor = dto.TextColor,
        FontFamily = dto.FontFamily,
        LoginBackgroundImage = dto.LoginBackgroundImage,
        DashboardLogo = dto.DashboardLogo,
        EmailHeaderImage = dto.EmailHeaderImage,
        CustomCss = dto.CustomCss ?? new Dictionary<string, string>()
    };
}

private SecurityPolicy ConvertToDomainSecurityPolicy(SecurityPolicyDto dto)
{
    if (dto == null) return new SecurityPolicy();
    
    return new SecurityPolicy
    {
        PasswordMinLength = dto.PasswordMinLength,
        RequireUppercase = dto.RequireUppercase,
        RequireLowercase = dto.RequireLowercase,
        RequireNumbers = dto.RequireNumbers,
        RequireSpecialCharacters = dto.RequireSpecialCharacters,
        PasswordExpiryDays = dto.PasswordExpiryDays,
        MaxLoginAttempts = dto.MaxLoginAttempts,
        LockoutDurationMinutes = dto.LockoutDurationMinutes,
        EnforceMfa = dto.EnforceMfa,
        AllowedMfaMethods = dto.AllowedMfaMethods ?? new List<string>(),
        SessionIdleTimeoutMinutes = dto.SessionIdleTimeoutMinutes,
        EnableIpWhitelisting = dto.EnableIpWhitelisting,
        IpWhitelist = dto.IpWhitelist ?? new List<string>(),
        RequireHttps = dto.RequireHttps,
        ContentSecurityPolicy = dto.ContentSecurityPolicy
    };
}

private TenantLimits ConvertToDomainLimits(TenantLimitsDto dto)
{
    if (dto == null) return new TenantLimits();
    
    return new TenantLimits
    {
        MaxUsers = dto.MaxUsers,
        MaxStorageGb = dto.MaxStorageGb,
        MaxApiCallsPerDay = dto.MaxApiCallsPerDay,
        MaxConcurrentSessions = dto.MaxConcurrentSessions,
        AllowedFeatures = dto.AllowedFeatures ?? new List<string>(),
        FeatureLimits = dto.FeatureLimits ?? new Dictionary<string, int>(),
        AllowApiAccess = dto.AllowApiAccess,
        RateLimitPerMinute = dto.RateLimitPerMinute,
        MaxFileSizeMb = dto.MaxFileSizeMb,
        AllowedFileTypes = dto.AllowedFileTypes ?? new List<string>()
    };
}

private BillingInfo ConvertToDomainBilling(BillingInfoDto dto)
{
    if (dto == null) return new BillingInfo();
    
    var billing = new BillingInfo
    {
        PlanId = dto.PlanId,
        PlanName = dto.PlanName,
        BillingCycle = dto.BillingCycle,
        MonthlyPrice = dto.MonthlyPrice,
        AnnualPrice = dto.AnnualPrice,
        TrialEndsAt = dto.TrialEndsAt,
        SubscriptionStartAt = dto.SubscriptionStartAt,
        SubscriptionEndAt = dto.SubscriptionEndAt,
        AutoRenew = dto.AutoRenew,
        PaymentMethod = dto.PaymentMethod,
        BillingEmail = dto.BillingEmail
    };
    
    if (dto.Address != null)
    {
        billing.Address = new BillingAddress
        {
            Street = dto.Address.Street,
            City = dto.Address.City,
            State = dto.Address.State,
            Country = dto.Address.Country,
            ZipCode = dto.Address.ZipCode,
            VatNumber = dto.Address.VatNumber
        };
    }
    
    return billing;
}

    
    [HttpGet]
    public async Task<ActionResult<List<TenantDto>>> GetAllTenants(
        [FromQuery] TenantType? type = null,
        [FromQuery] TenantStatus? status = null,
        [FromQuery] TenantDeploymentModel? deploymentModel = null,
        [FromQuery] string? search = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        if (!await IsSystemAdmin())
            return Forbid("SystemAdmin access required");
        
        var tenants = await _tenantRepository.GetAllAsync();
        
        // Apply filters
        var query = tenants.AsQueryable();
        
        if (type.HasValue)
            query = query.Where(t => t.Type == type.Value);
            
        if (status.HasValue)
            query = query.Where(t => t.Status == status.Value);
            
        if (deploymentModel.HasValue)
            query = query.Where(t => t.DeploymentModel == deploymentModel.Value);
            
        if (!string.IsNullOrEmpty(search))
        {
            search = search.ToLower();
            query = query.Where(t => 
                t.Name.ToLower().Contains(search) || 
                t.Subdomain.ToLower().Contains(search) ||
                (t.CustomDomain != null && t.CustomDomain.ToLower().Contains(search)));
        }
        
        // Apply pagination
        var totalCount = query.Count();
        var items = query
            .OrderBy(t => t.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => MapToTenantDto(t))
            .ToList();
        
            Response.Headers["X-Total-Count"] = totalCount.ToString();
            Response.Headers["X-Page"] = page.ToString();
            Response.Headers["X-Page-Size"] = pageSize.ToString();
        
        return Ok(items);
    }
    
    [HttpGet("{id}")]
    public async Task<ActionResult<TenantDto>> GetTenant(Guid id)
    {
        if (!await IsSystemAdmin())
            return Forbid("SystemAdmin access required");
            
        var tenant = await _tenantRepository.GetByIdAsync(id);
        if (tenant == null)
            return NotFound();
            
        return Ok(MapToTenantDto(tenant));
    }
    
    [HttpGet("{id}/settings")]
    public async Task<ActionResult<TenantSettings>> GetTenantSettings(Guid id)
    {
        if (!await IsSystemAdmin())
            return Forbid("SystemAdmin access required");
            
        var tenant = await _tenantRepository.GetByIdAsync(id);
        if (tenant == null)
            return NotFound();
            
        return Ok(tenant.Settings);
    }
    
    [HttpPut("{id}/settings")]
    public async Task<IActionResult> UpdateTenantSettings(Guid id, TenantSettings settings)
    {
        if (!await IsSystemAdmin())
            return Forbid("SystemAdmin access required");
            
        var tenant = await _tenantRepository.GetByIdAsync(id);
        if (tenant == null)
            return NotFound();
            
        tenant.UpdateSettings(settings);
        await _tenantRepository.UpdateAsync(tenant);
        
        _logger.LogInformation("Settings updated for tenant {TenantId}", id);
        
        return NoContent();
    }
    
    [HttpGet("{id}/branding")]
    public async Task<ActionResult<TenantBranding>> GetTenantBranding(Guid id)
    {
        if (!await IsSystemAdmin())
            return Forbid("SystemAdmin access required");
            
        var tenant = await _tenantRepository.GetByIdAsync(id);
        if (tenant == null)
            return NotFound();
            
        return Ok(tenant.Branding);
    }
    
    [HttpPut("{id}/branding")]
    public async Task<IActionResult> UpdateTenantBranding(Guid id, TenantBranding branding)
    {
        if (!await IsSystemAdmin())
            return Forbid("SystemAdmin access required");
            
        var tenant = await _tenantRepository.GetByIdAsync(id);
        if (tenant == null)
            return NotFound();
            
        tenant.UpdateBranding(branding);
        await _tenantRepository.UpdateAsync(tenant);
        
        return NoContent();
    }
    
    [HttpGet("{id}/security")]
    public async Task<ActionResult<SecurityPolicy>> GetSecurityPolicy(Guid id)
    {
        if (!await IsSystemAdmin())
            return Forbid("SystemAdmin access required");
            
        var tenant = await _tenantRepository.GetByIdAsync(id);
        if (tenant == null)
            return NotFound();
            
        return Ok(tenant.SecurityPolicy);
    }
    
    [HttpPut("{id}/security")]
    public async Task<IActionResult> UpdateSecurityPolicy(Guid id, SecurityPolicy policy)
    {
        if (!await IsSystemAdmin())
            return Forbid("SystemAdmin access required");
            
        var tenant = await _tenantRepository.GetByIdAsync(id);
        if (tenant == null)
            return NotFound();
            
        tenant.UpdateSecurityPolicy(policy);
        await _tenantRepository.UpdateAsync(tenant);
        
        return NoContent();
    }
    
    [HttpGet("{id}/limits")]
    public async Task<ActionResult<TenantLimits>> GetTenantLimits(Guid id)
    {
        if (!await IsSystemAdmin())
            return Forbid("SystemAdmin access required");
            
        var tenant = await _tenantRepository.GetByIdAsync(id);
        if (tenant == null)
            return NotFound();
            
        return Ok(tenant.Limits);
    }
    
    [HttpPut("{id}/limits")]
    public async Task<IActionResult> UpdateTenantLimits(Guid id, TenantLimits limits)
    {
        if (!await IsSystemAdmin())
            return Forbid("SystemAdmin access required");
            
        var tenant = await _tenantRepository.GetByIdAsync(id);
        if (tenant == null)
            return NotFound();
            
        tenant.UpdateLimits(limits);
        await _tenantRepository.UpdateAsync(tenant);
        
        return NoContent();
    }
    
    [HttpGet("{id}/billing")]
    public async Task<ActionResult<BillingInfo>> GetBillingInfo(Guid id)
    {
        if (!await IsSystemAdmin())
            return Forbid("SystemAdmin access required");
            
        var tenant = await _tenantRepository.GetByIdAsync(id);
        if (tenant == null)
            return NotFound();
            
        return Ok(tenant.Billing);
    }
    
    [HttpPut("{id}/billing")]
    public async Task<IActionResult> UpdateBillingInfo(Guid id, BillingInfo billing)
    {
        if (!await IsSystemAdmin())
            return Forbid("SystemAdmin access required");
            
        var tenant = await _tenantRepository.GetByIdAsync(id);
        if (tenant == null)
            return NotFound();
            
        tenant.UpdateBilling(billing);
        await _tenantRepository.UpdateAsync(tenant);
        
        return NoContent();
    }
    
    [HttpGet("{id}/features")]
    public async Task<ActionResult<Dictionary<string, object>>> GetFeatures(Guid id)
    {
        if (!await IsSystemAdmin())
            return Forbid("SystemAdmin access required");
            
        var tenant = await _tenantRepository.GetByIdAsync(id);
        if (tenant == null)
            return NotFound();
            
        return Ok(tenant.Features);
    }
    
    [HttpPost("{id}/features/{feature}")]
    public async Task<IActionResult> EnableFeature(Guid id, string feature, [FromBody] object? configuration = null)
    {
        if (!await IsSystemAdmin())
            return Forbid("SystemAdmin access required");
            
        var tenant = await _tenantRepository.GetByIdAsync(id);
        if (tenant == null)
            return NotFound();
            
        tenant.EnableFeature(feature, configuration ?? true);
        await _tenantRepository.UpdateAsync(tenant);
        
        return Ok(new { message = $"Feature '{feature}' enabled" });
    }
    
    [HttpDelete("{id}/features/{feature}")]
    public async Task<IActionResult> DisableFeature(Guid id, string feature)
    {
        if (!await IsSystemAdmin())
            return Forbid("SystemAdmin access required");
            
        var tenant = await _tenantRepository.GetByIdAsync(id);
        if (tenant == null)
            return NotFound();
            
        tenant.DisableFeature(feature);
        await _tenantRepository.UpdateAsync(tenant);
        
        return Ok(new { message = $"Feature '{feature}' disabled" });
    }
    
    [HttpGet("{id}/domains")]
    public async Task<ActionResult<List<string>>> GetAllowedDomains(Guid id)
    {
        if (!await IsSystemAdmin())
            return Forbid("SystemAdmin access required");
            
        var tenant = await _tenantRepository.GetByIdAsync(id);
        if (tenant == null)
            return NotFound();
            
        return Ok(tenant.AllowedDomains);
    }
    
    [HttpPost("{id}/domains")]
    public async Task<IActionResult> AddAllowedDomain(Guid id, [FromBody] string domain)
    {
        if (!await IsSystemAdmin())
            return Forbid("SystemAdmin access required");
            
        var tenant = await _tenantRepository.GetByIdAsync(id);
        if (tenant == null)
            return NotFound();
            
        tenant.AddAllowedDomain(domain);
        await _tenantRepository.UpdateAsync(tenant);
        
        return Ok(new { message = $"Domain '{domain}' added" });
    }
    
    [HttpDelete("{id}/domains/{domain}")]
    public async Task<IActionResult> RemoveAllowedDomain(Guid id, string domain)
    {
        if (!await IsSystemAdmin())
            return Forbid("SystemAdmin access required");
            
        var tenant = await _tenantRepository.GetByIdAsync(id);
        if (tenant == null)
            return NotFound();
            
        tenant.RemoveAllowedDomain(domain);
        await _tenantRepository.UpdateAsync(tenant);
        
        return Ok(new { message = $"Domain '{domain}' removed" });
    }
    
    [HttpPost("{id}/activate")]
    public async Task<IActionResult> ActivateTenant(Guid id)
    {
        if (!await IsSystemAdmin())
            return Forbid("SystemAdmin access required");
            
        var tenant = await _tenantRepository.GetByIdAsync(id);
        if (tenant == null)
            return NotFound();
            
        tenant.Activate();
        await _tenantRepository.UpdateAsync(tenant);
        
        return Ok(new { message = "Tenant activated" });
    }
    
    [HttpPost("{id}/deactivate")]
    public async Task<IActionResult> DeactivateTenant(Guid id)
    {
        if (!await IsSystemAdmin())
            return Forbid("SystemAdmin access required");
            
        var tenant = await _tenantRepository.GetByIdAsync(id);
        if (tenant == null)
            return NotFound();
            
        tenant.Deactivate();
        await _tenantRepository.UpdateAsync(tenant);
        
        return Ok(new { message = "Tenant deactivated" });
    }
    
    [HttpPost("{id}/suspend")]
    public async Task<IActionResult> SuspendTenant(Guid id, [FromBody] string? reason = null)
    {
        if (!await IsSystemAdmin())
            return Forbid("SystemAdmin access required");
            
        var tenant = await _tenantRepository.GetByIdAsync(id);
        if (tenant == null)
            return NotFound();
            
        tenant.Suspend(reason);
        await _tenantRepository.UpdateAsync(tenant);
        
        return Ok(new { message = "Tenant suspended", reason });
    }
    
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteTenant(Guid id)
    {
        if (!await IsSystemAdmin())
            return Forbid("SystemAdmin access required");
            
        var tenant = await _tenantRepository.GetByIdAsync(id);
        if (tenant == null)
            return NotFound();
        
        // Soft delete - deactivate and suspend
        tenant.Deactivate();
        tenant.Suspend("Deleted by system admin");
        await _tenantRepository.UpdateAsync(tenant);
        
        _logger.LogWarning("Tenant {TenantId} deactivated by admin", id);
        
        return Ok(new { message = "Tenant deactivated successfully" });
    }
    
[HttpGet("stats")]
public async Task<ActionResult<object>> GetTenantStats()
{
    if (!await IsSystemAdmin())
        return Forbid("SystemAdmin access required");
        
    var tenants = await _tenantRepository.GetAllAsync();
    var tenantsList = tenants.ToList(); // Materialize the collection
    
    var stats = new
    {
        total = tenantsList.Count,
        byType = tenantsList.GroupBy(t => t.Type).ToDictionary(g => g.Key.ToString(), g => g.Count()),
        byStatus = tenantsList.GroupBy(t => t.Status).ToDictionary(g => g.Key.ToString(), g => g.Count()),
        byDeploymentModel = tenantsList.GroupBy(t => t.DeploymentModel).ToDictionary(g => g.Key.ToString(), g => g.Count()),
        active = tenantsList.Count(t => t.Status == TenantStatus.Active),
        inactive = tenantsList.Count(t => t.Status == TenantStatus.Inactive),
        suspended = tenantsList.Count(t => t.Status == TenantStatus.Suspended),
        pending = tenantsList.Count(t => t.Status == TenantStatus.Pending),
        expired = tenantsList.Count(t => t.Status == TenantStatus.Expired),
        trial = tenantsList.Count(t => t.Type == TenantType.Trial),
        enterprise = tenantsList.Count(t => t.Type == TenantType.Enterprise),
        standard = tenantsList.Count(t => t.Type == TenantType.Standard),
        shared = tenantsList.Count(t => t.DeploymentModel == TenantDeploymentModel.Shared),
        dedicated = tenantsList.Count(t => t.DeploymentModel == TenantDeploymentModel.Dedicated),
        isolated = tenantsList.Count(t => t.DeploymentModel == TenantDeploymentModel.Isolated),
        totalUsers = tenantsList.Sum(t => t.Users?.Count ?? 0) // This should work now
    };
    
    return Ok(stats);
}
    
    private TenantDto MapToTenantDto(Tenant tenant)
    {
        return new TenantDto
        {
            Id = tenant.Id,
            Name = tenant.Name,
            Subdomain = tenant.Subdomain,
            CustomDomain = tenant.CustomDomain,
            Type = tenant.Type.ToString(),
            DeploymentModel = tenant.DeploymentModel.ToString(),
            Status = tenant.Status.ToString(),
            IsActive = tenant.IsActive,
            CreatedAt = tenant.CreatedAt,
            UpdatedAt = tenant.UpdatedAt,
            LastActiveAt = tenant.LastActiveAt,
            UserCount = tenant.Users?.Count ?? 0,
            Features = tenant.Features?.Keys.ToList() ?? new List<string>(),
            Settings = tenant.Settings,
            Branding = tenant.Branding,
            SecurityPolicy = tenant.SecurityPolicy,
            Limits = tenant.Limits,
            Billing = tenant.Billing,
            AllowedDomains = tenant.AllowedDomains ?? new List<string>(),
            Notes = tenant.Notes
        };
    }
}

// DTO for returning tenant information
public class TenantDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Subdomain { get; set; } = string.Empty;
    public string? CustomDomain { get; set; }
    public string Type { get; set; } = string.Empty;
    public string DeploymentModel { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? LastActiveAt { get; set; }
    public int UserCount { get; set; }
    public List<string> Features { get; set; } = new();
    public TenantSettings? Settings { get; set; }
    public TenantBranding? Branding { get; set; }
    public SecurityPolicy? SecurityPolicy { get; set; }
    public TenantLimits? Limits { get; set; }
    public BillingInfo? Billing { get; set; }
    public List<string> AllowedDomains { get; set; } = new();
    public string? Notes { get; set; }
}