using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Linq;

namespace HCSN.Identity.Domain.Entities;
    public enum TenantType
    {
        Standard = 0,      // Regular SaaS tenant
        Enterprise = 1,     // Enterprise with custom features
        Trial = 2,         // Trial/Free tier
        Internal = 3,       // Internal system (admin, etc.)
        WhiteLabel = 4,     // White-labeled solution
        System = 5         // System tenant (for platform services)
    }
    
    // Deployment Model (matches our discussion!)
    public enum TenantDeploymentModel
    {
        Shared = 0,        // Uses tenant-base template (Scenario 1)
        Dedicated = 1,     // Has its own custom system (Scenario 2)
        Isolated = 2       // Fully isolated (separate infrastructure)
    }
    
    public enum TenantStatus
    {
        Active = 0,
        Inactive = 1,
        Suspended = 2,
        Pending = 3,
        Expired = 4,
        Maintenance = 5
    }
public class Tenant
{
    private Tenant() { } // For EF Core
        // Tenant Type Classification
    public Tenant(
        string name, 
        string subdomain, 
        string? connectionString = null,
        TenantType type = TenantType.Standard,
        TenantDeploymentModel deploymentModel = TenantDeploymentModel.Shared)
    {
        Id = Guid.NewGuid();
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Subdomain = subdomain ?? throw new ArgumentNullException(nameof(subdomain));
        ConnectionString = connectionString;
        Type = type;
        DeploymentModel = deploymentModel;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
        Features = new Dictionary<string, object>();
        Settings = new TenantSettings();
        Branding = new TenantBranding();
        SecurityPolicy = new SecurityPolicy();
        Limits = new TenantLimits();
        Billing = new BillingInfo();
        Metadata = new Dictionary<string, string>();
        AllowedDomains = new List<string>();
        CreatedAt = DateTime.UtcNow;
    }
    
    // Core Properties
    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public bool IsActive { get; private set; }
    public string Subdomain { get; private set; }
    public string? CustomDomain { get; private set; }
    public string? ConnectionString { get; private set; }
    public TenantType Type { get; private set; }
    public TenantDeploymentModel DeploymentModel { get; private set; }
    public TenantStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public DateTime? DeletedAt { get; private set; }
    public DateTime? LastActiveAt { get; private set; }
    public string? Notes { get; private set; }
    

    
    // Enhanced Features Dictionary (supports any type of feature)
    public Dictionary<string, object> Features { get; private set; }
    public virtual ICollection<Invoice> Invoices { get; private set; } = new List<Invoice>();
    // Comprehensive Settings
    public TenantSettings Settings { get; private set; }
    public TenantBranding Branding { get; private set; }
    public SecurityPolicy SecurityPolicy { get; private set; }
    public TenantLimits Limits { get; private set; }
    public BillingInfo Billing { get; private set; }
    
    // Flexible Metadata
    public Dictionary<string, string> Metadata { get; private set; }
    
    // Domain Management
    public List<string> AllowedDomains { get; private set; }
    
    // Navigation Properties
    public virtual ICollection<User> Users { get; private set; } = new List<User>();
    public virtual ICollection<TenantModule> Modules { get; private set; } = new List<TenantModule>();
    
    // Feature Management
    public void EnableFeature(string featureKey, object? configuration = null)
    {
        if (!Features.ContainsKey(featureKey))
        {
            Features[featureKey] = configuration ?? true;
            UpdatedAt = DateTime.UtcNow;
        }
    }
    
    public void DisableFeature(string featureKey)
    {
        if (Features.ContainsKey(featureKey))
        {
            Features.Remove(featureKey);
            UpdatedAt = DateTime.UtcNow;
        }
    }
    
    public bool HasFeature(string featureKey)
    {
        return Features.ContainsKey(featureKey);
    }
    
    public T? GetFeatureConfiguration<T>(string featureKey)
    {
        if (Features.TryGetValue(featureKey, out var config))
        {
            try
            {
                return (T)Convert.ChangeType(config, typeof(T));
            }
            catch
            {
                return default;
            }
        }
        return default;
    }
    
    // Domain Management
    public void AddAllowedDomain(string domain)
    {
        if (string.IsNullOrWhiteSpace(domain))
            throw new ArgumentException("Domain cannot be empty", nameof(domain));
            
        if (!AllowedDomains.Contains(domain))
        {
            AllowedDomains.Add(domain);
            UpdatedAt = DateTime.UtcNow;
        }
    }
    
    public void RemoveAllowedDomain(string domain)
    {
        if (AllowedDomains.Contains(domain))
        {
            AllowedDomains.Remove(domain);
            UpdatedAt = DateTime.UtcNow;
        }
    }
    
    public bool IsDomainAllowed(string domain)
    {
        return AllowedDomains.Count == 0 || AllowedDomains.Any(d => 
            d.Equals(domain, StringComparison.OrdinalIgnoreCase) ||
            (d.StartsWith("*.") && domain.EndsWith(d.Substring(2))));
    }
    
    // Custom Domain
    public void SetCustomDomain(string? customDomain)
    {
        CustomDomain = customDomain;
        UpdatedAt = DateTime.UtcNow;
    }
    
    // Status Management
    public void Activate() 
    { 
        Status = TenantStatus.Active;
        UpdatedAt = DateTime.UtcNow;
    }
    
    public void Deactivate() 
    { 
        Status = TenantStatus.Inactive;
        UpdatedAt = DateTime.UtcNow;
    }
    
    public void Suspend(string? reason = null)
    {
        Status = TenantStatus.Suspended;
        Notes = reason;
        UpdatedAt = DateTime.UtcNow;
    }
    
    public void MarkAsActive()
    {
        LastActiveAt = DateTime.UtcNow;
    }
    
    // Configuration Methods
    public void UpdateSettings(TenantSettings settings)
    {
        Settings = settings ?? throw new ArgumentNullException(nameof(settings));
        UpdatedAt = DateTime.UtcNow;
    }
    
    public void UpdateBranding(TenantBranding branding)
    {
        Branding = branding ?? throw new ArgumentNullException(nameof(branding));
        UpdatedAt = DateTime.UtcNow;
    }
    
    public void UpdateSecurityPolicy(SecurityPolicy policy)
    {
        SecurityPolicy = policy ?? throw new ArgumentNullException(nameof(policy));
        UpdatedAt = DateTime.UtcNow;
    }
    
    public void UpdateLimits(TenantLimits limits)
    {
        Limits = limits ?? throw new ArgumentNullException(nameof(limits));
        UpdatedAt = DateTime.UtcNow;
    }
    
    public void UpdateBilling(BillingInfo billing)
    {
        Billing = billing ?? throw new ArgumentNullException(nameof(billing));
        UpdatedAt = DateTime.UtcNow;
    }
    
    public void AddMetadata(string key, string value)
    {
        Metadata[key] = value;
        UpdatedAt = DateTime.UtcNow;
    }
    
    // Module Management
    public void AddModule(TenantModule module)
    {
        if (!Modules.Any(m => m.ModuleCode == module.ModuleCode))
        {
            Modules.Add(module);
            UpdatedAt = DateTime.UtcNow;
        }
    }
    
    public void RemoveModule(string moduleCode)
    {
        var module = Modules.FirstOrDefault(m => m.ModuleCode == moduleCode);
        if (module != null)
        {
            Modules.Remove(module);
            UpdatedAt = DateTime.UtcNow;
        }
    }
    
    // Validation
    public bool CanAddMoreUsers()
    {
        return Limits.MaxUsers == null || Users.Count < Limits.MaxUsers;
    }
    
    public bool CanUseFeature(string featureKey)
    {
        return HasFeature(featureKey) && Status == TenantStatus.Active;
    }
    
    // Helper Methods
    public string GetDeploymentPath()
    {
        return DeploymentModel switch
        {
            TenantDeploymentModel.Shared => "tenant-base",
            TenantDeploymentModel.Dedicated => $"tenant-{Subdomain}",
            TenantDeploymentModel.Isolated => $"isolated-{Subdomain}",
            _ => "tenant-base"
        };
    }
    
    public string GetFullDomain()
    {
        return !string.IsNullOrEmpty(CustomDomain) 
            ? CustomDomain 
            : $"{Subdomain}.hcsn.com";
    }
}

// Enhanced RegistrationSettings
public class RegistrationSettings
{
    public RegistrationSettings()
    {
        SocialLoginProviders = new List<string>();
        CustomFields = new List<CustomField>();
    }
    
    public bool AllowPublicRegistration { get; set; } = true;
    public bool RequireEmailConfirmation { get; set; } = true;
    public bool RequireAdminApproval { get; set; } = false;
    public bool AllowSocialLogin { get; set; } = false;
    public List<string> SocialLoginProviders { get; set; }
    public string? WelcomeEmailTemplate { get; set; }
    public string? RegistrationSuccessUrl { get; set; }
    public string? InvitationExpiryHours { get; set; } = "48";
    public List<CustomField> CustomFields { get; set; }
    public string? RedirectAfterLogin { get; set; }
    public bool AutoProvisionWorkspace { get; set; } = true;
    public string? DefaultRole { get; set; } = "User";
    public bool SendWelcomeEmail { get; set; } = true;
}

public class CustomField
{
    public string FieldName { get; set; } = string.Empty;
    public string FieldType { get; set; } = "text"; // text, email, phone, date, select, etc.
    public bool IsRequired { get; set; }
    public bool IsVisible { get; set; } = true;
    public string? DefaultValue { get; set; }
    public List<string>? Options { get; set; } // For select fields
    public int DisplayOrder { get; set; }
    public string? ValidationRegex { get; set; }
    public string? ErrorMessage { get; set; }
    
    public string? Placeholder { get; set; }  // ADD THIS LINE

    public Dictionary<string, object>? Metadata { get; init; } // Additional field metadata
}

// New Tenant Settings Class
public class TenantSettings
{
    public string? TimeZone { get; set; } = "UTC";
    public string? DateFormat { get; set; } = "MM/dd/yyyy";
    public string? TimeFormat { get; set; } = "HH:mm";
    public string? Currency { get; set; } = "USD";
    public string? Language { get; set; } = "en-US";
    public RegistrationSettings Registration { get; set; } = new();
    public bool AllowUserInvitations { get; set; } = true;
    public bool AllowUserDeletion { get; set; } = false;
    public int SessionTimeoutMinutes { get; set; } = 60;
    public bool RequireMfa { get; set; } = false;
    public string? DefaultTheme { get; set; } = "light";
    public Dictionary<string, object> CustomSettings { get; set; } = new();
}

// Tenant Branding
public class TenantBranding
{
    public string? LogoUrl { get; set; }
    public string? FaviconUrl { get; set; }
    public string? PrimaryColor { get; set; } = "#2563EB";
    public string? SecondaryColor { get; set; } = "#7C3AED";
    public string? AccentColor { get; set; } = "#059669";
    public string? BackgroundColor { get; set; } = "#FFFFFF";
    public string? TextColor { get; set; } = "#1F2937";
    public string? FontFamily { get; set; } = "Inter";
    public string? LoginBackgroundImage { get; set; }
    public string? DashboardLogo { get; set; }
    public string? EmailHeaderImage { get; set; }
    public Dictionary<string, string> CustomCss { get; set; } = new();
}

// Security Policy
public class SecurityPolicy
{
    public int PasswordMinLength { get; set; } = 8;
    public bool RequireUppercase { get; set; } = true;
    public bool RequireLowercase { get; set; } = true;
    public bool RequireNumbers { get; set; } = true;
    public bool RequireSpecialCharacters { get; set; } = true;
    public int PasswordExpiryDays { get; set; } = 90;
    public int MaxLoginAttempts { get; set; } = 5;
    public int LockoutDurationMinutes { get; set; } = 15;
    public bool EnforceMfa { get; set; } = false;
    public List<string> AllowedMfaMethods { get; set; } = new() { "authenticator", "sms", "email" };
    public int SessionIdleTimeoutMinutes { get; set; } = 30;
    public bool EnableIpWhitelisting { get; set; } = false;
    public List<string> IpWhitelist { get; set; } = new();
    public bool RequireHttps { get; set; } = true;
    public string? ContentSecurityPolicy { get; set; }
}

// Tenant Limits
public class TenantLimits
{
    public int? MaxUsers { get; set; } = 100;
    public int? MaxStorageGb { get; set; } = 10;
    public int? MaxApiCallsPerDay { get; set; } = 10000;
    public int? MaxConcurrentSessions { get; set; } = 5;
    public List<string> AllowedFeatures { get; set; } = new();
    public Dictionary<string, int> FeatureLimits { get; set; } = new();
    public bool AllowApiAccess { get; set; } = true;
    public int? RateLimitPerMinute { get; set; } = 60;
    public int? MaxFileSizeMb { get; set; } = 25;
    public List<string> AllowedFileTypes { get; set; } = new() { ".jpg", ".png", ".pdf", ".docx" };
}

// Billing Info
public class BillingInfo
{
    public string? PlanId { get; set; }
    public string? PlanName { get; set; } = "Free";
    public string? BillingCycle { get; set; } = "monthly";
    public decimal? MonthlyPrice { get; set; }
    public decimal? AnnualPrice { get; set; }
    public DateTime? TrialEndsAt { get; set; }
    public DateTime? SubscriptionStartAt { get; set; }
    public DateTime? SubscriptionEndAt { get; set; }
    public bool AutoRenew { get; set; } = true;
    public string? StripeCustomerId { get; set; }
    public string? StripeSubscriptionId { get; set; }
    public string? PaymentMethod { get; set; }
    public string? BillingEmail { get; set; }
    public BillingAddress? Address { get; set; }
    //public List<Invoice> Invoices { get; set; } = new();
}

public class BillingAddress
{
    public string? Street { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? Country { get; set; }
    public string? ZipCode { get; set; }
    public string? VatNumber { get; set; }
}

public class Invoice
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }

    public string? InvoiceNumber { get; set; }
    public DateTime Date { get; set; }
    public decimal Amount { get; set; }
    public string? Status { get; set; }
    public string? PdfUrl { get; set; }

    public Tenant Tenant { get; set; } = null!;
}

// Tenant Modules
public class TenantModule
{
    public Guid Id { get; set; }
    public string ModuleCode { get; set; } = string.Empty;
    public string ModuleName { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
    public DateTime? EnabledAt { get; set; }
    public Dictionary<string, object> Configuration { get; set; } = new();
    public string? Version { get; set; }
}

// Extension methods for tenant queries
public static class TenantExtensions
{
    public static IQueryable<Tenant> Active(this IQueryable<Tenant> query)
    {
        return query.Where(t => t.Status == TenantStatus.Active);
    }
    
    public static IQueryable<Tenant> OfType(this IQueryable<Tenant> query, TenantType type)
    {
        return query.Where(t => t.Type == type);
    }
    
    public static IQueryable<Tenant> WithDeploymentModel(this IQueryable<Tenant> query, TenantDeploymentModel model)
    {
        return query.Where(t => t.DeploymentModel == model);
    }
}