using System;
using System.Collections.Generic;

namespace HCSN.Identity.Public;

// Base registration request with enhanced fields
public record BaseRegisterRequest
{
    public string Email { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public string? PhoneNumber { get; init; }
    public string? InvitationToken { get; init; }
    
    // New: Support for additional metadata
    public Dictionary<string, object>? Metadata { get; init; }
    
    // New: User preferences
    public UserPreferencesDto? Preferences { get; init; }
}

// Enhanced tenant registration request
public record TenantRegisterRequest : BaseRegisterRequest
{
    // Can be subdomain or custom domain
    public string TenantIdentifier { get; init; } = string.Empty;
    
    // Company information
    public string? CompanyName { get; init; }
    public string? Address { get; init; }
    public string? City { get; init; }
    public string? State { get; init; }
    public string? Country { get; init; }
    public string? ZipCode { get; init; }
    public string? VatNumber { get; init; }
    
    // New: Custom fields based on tenant configuration
    public Dictionary<string, object>? CustomFields { get; init; }
    
    // New: User role request (if applicable)
    public string? RequestedRole { get; init; }
    
    // New: Terms acceptance
    public bool AcceptedTerms { get; init; }
    public bool AcceptedPrivacy { get; init; }
    public DateTime? TermsAcceptedAt { get; init; }
    
    // New: Marketing preferences
    public bool SubscribeToNewsletter { get; init; }
    public bool AllowMarketingEmails { get; init; }
}

// Enhanced tenant creation request
public record CreateTenantWithSettingsRequest
{
    // Core tenant info
    public string Name { get; init; } = string.Empty;
    public string Subdomain { get; init; } = string.Empty;
    public string? CustomDomain { get; init; }
    public TenantTypeDto Type { get; init; } = TenantTypeDto.Standard;
    public TenantDeploymentModelDto DeploymentModel { get; init; } = TenantDeploymentModelDto.Shared;
    
    // Settings
    public TenantSettingsDto Settings { get; init; } = new();
    public TenantBrandingDto Branding { get; init; } = new();
    public SecurityPolicyDto SecurityPolicy { get; init; } = new();
    public TenantLimitsDto Limits { get; init; } = new();
    public BillingInfoDto Billing { get; init; } = new();
    
    // Features
    public Dictionary<string, object> Features { get; init; } = new();
    
    // Domain management
    public List<string> AllowedDomains { get; init; } = new();
    
    // Metadata
    public Dictionary<string, string> Metadata { get; init; } = new();
}

// FIXED: Enhanced registration settings DTO with all properties the service expects
public record RegistrationSettingsDto
{
    // Basic settings
    public bool AllowPublicRegistration { get; set; } = true;
    public bool RequireEmailConfirmation { get; set; } = true;
    public bool RequireAdminApproval { get; set; } = false;
    public bool AllowSocialLogin { get; set; } = false;
    public List<string> SocialLoginProviders { get; set; } = new();
    
    // New: Custom fields configuration
    public List<CustomFieldDto> CustomFields { get; set; } = new();
    
    // New: Email templates
    public string? WelcomeEmailTemplate { get; set; }
    public string? ConfirmationEmailTemplate { get; set; }
    public string? ApprovalEmailTemplate { get; set; }
    public string? RejectionEmailTemplate { get; set; }
    
    // New: Redirection settings
    public string? RegistrationSuccessUrl { get; set; }
    public string? RedirectAfterLogin { get; set; }
    public string? TermsUrl { get; set; }
    public string? PrivacyUrl { get; set; }
    
    // New: Invitation settings
    public string? InvitationExpiryHours { get; set; } = "48";
    public bool AllowInvitations { get; set; } = true;
    
    // New: Default settings
    public string? DefaultRole { get; set; } = "User";
    public bool AutoProvisionWorkspace { get; set; } = true;
    public bool SendWelcomeEmail { get; set; } = true;
    
    // FIXED: Added missing properties that the service is trying to set
    public List<string> AvailableFeatures { get; set; } = new();
    public string? TenantName { get; set; }
    public string? TenantType { get; set; }
    public string? DeploymentModel { get; set; }
    public string? PrimaryColor { get; set; }
    public string? LogoUrl { get; set; }
    public string? LoginBackgroundImage { get; set; }
    
    // Legacy fields mapped to custom fields
    public bool CollectPhoneNumber 
    { 
        get => CustomFields?.Any(f => f.FieldName == "PhoneNumber") ?? false;
        set 
        {
            if (value && CustomFields != null && !CustomFields.Any(f => f.FieldName == "PhoneNumber"))
            {
                CustomFields.Add(new CustomFieldDto
                {
                    FieldName = "PhoneNumber",
                    FieldType = "tel",
                    IsRequired = false,
                    DisplayOrder = CustomFields.Count + 1
                });
            }
        }
    }
    
    public bool CollectAddress 
    { 
        get => CustomFields?.Any(f => f.FieldName == "Address") ?? false;
        set
        {
            if (value && CustomFields != null && !CustomFields.Any(f => f.FieldName == "Address"))
            {
                CustomFields.Add(new CustomFieldDto
                {
                    FieldName = "Address",
                    FieldType = "text",
                    IsRequired = false,
                    DisplayOrder = CustomFields.Count + 1
                });
            }
        }
    }
    
    public bool CollectCompanyName 
    { 
        get => CustomFields?.Any(f => f.FieldName == "CompanyName") ?? false;
        set
        {
            if (value && CustomFields != null && !CustomFields.Any(f => f.FieldName == "CompanyName"))
            {
                CustomFields.Add(new CustomFieldDto
                {
                    FieldName = "CompanyName",
                    FieldType = "text",
                    IsRequired = false,
                    DisplayOrder = CustomFields.Count + 1
                });
            }
        }
    }
}

// New: Custom field DTO
public record CustomFieldDto
{
    public string FieldName { get; init; } = string.Empty;
    public string FieldType { get; init; } = "text"; // text, email, tel, select, checkbox, date, etc.
    public bool IsRequired { get; init; }
    public bool IsVisible { get; init; } = true;
    public string? DefaultValue { get; init; }
    public List<string>? Options { get; init; } // For select/multi-select
    public int DisplayOrder { get; init; }
    public string? Placeholder { get; init; }
    public string? ValidationRegex { get; init; }
    public string? ErrorMessage { get; init; }
    public Dictionary<string, object>? Metadata { get; init; } // Additional field metadata
}

// New: Tenant settings DTO
public record TenantSettingsDto
{
    public string? TimeZone { get; set; } = "UTC";
    public string? DateFormat { get; set; } = "MM/dd/yyyy";
    public string? TimeFormat { get; set; } = "HH:mm";
    public string? Currency { get; set; } = "USD";
    public string? Language { get; set; } = "en-US";
    public RegistrationSettingsDto Registration { get; set; } = new();
    public bool AllowUserInvitations { get; set; } = true;
    public bool AllowUserDeletion { get; set; } = false;
    public int SessionTimeoutMinutes { get; set; } = 60;
    public bool RequireMfa { get; set; } = false;
    public string? DefaultTheme { get; set; } = "light";
    public Dictionary<string, object> CustomSettings { get; set; } = new();
}

// New: Tenant branding DTO
public record TenantBrandingDto
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

// New: Security policy DTO
public record SecurityPolicyDto
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

// New: Tenant limits DTO
public record TenantLimitsDto
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

// New: Billing info DTO
public record BillingInfoDto
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
    public string? PaymentMethod { get; set; }
    public string? BillingEmail { get; set; }
    public BillingAddressDto? Address { get; set; }
}

// New: Billing address DTO
public record BillingAddressDto
{
    public string? Street { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? Country { get; set; }
    public string? ZipCode { get; set; }
    public string? VatNumber { get; set; }
}

// New: User preferences DTO
public record UserPreferencesDto
{
    public string? Language { get; set; }
    public string? Theme { get; set; }
    public bool EmailNotifications { get; set; } = true;
    public bool PushNotifications { get; set; } = true;
    public string? Timezone { get; set; }
    public Dictionary<string, object> CustomPreferences { get; set; } = new();
}

// Enhanced registration result
public record RegistrationResult
{
    public bool Success { get; init; }
    public string? Message { get; init; }
    public string? ErrorCode { get; init; }
    public UserDto? User { get; init; }
    public string? Token { get; init; }
    public string? RefreshToken { get; init; }
    public bool RequiresApproval { get; init; }
    public bool RequiresEmailConfirmation { get; init; }
    public bool RequiresPayment { get; init; }
    public string? NextStep { get; init; }
    public string? FlowType { get; init; } // "direct", "approval", "email_confirmation", "payment"
    public string? RedirectUrl { get; init; }
    
    // Payment related
    public string? PaymentSessionId { get; init; }
    public string? PaymentUrl { get; init; }
    
    // Quota information
    public QuotaInfoDto? QuotaInfo { get; init; }
    
    // Validation errors
    public Dictionary<string, string>? ValidationErrors { get; init; }
    
    // Tenant info
    public Guid? TenantId { get; init; }
    public string? TenantName { get; init; }
}

// New: Quota info DTO
public record QuotaInfoDto
{
    public int CurrentUsers { get; init; }
    public int MaxUsers { get; init; }
    public string QuotaType { get; init; } = string.Empty;
}

// Enhanced pending user DTO
public record PendingUserDto(
    Guid Id,
    string Email,
    string FullName,
    DateTime RegisteredAt,
    Dictionary<string, object> CustomData,
    string? RequestedRole = null,
    Dictionary<string, object>? Metadata = null
);

// New: Tenant info DTO for public consumption
public record TenantInfoDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Subdomain { get; init; } = string.Empty;
    public string? CustomDomain { get; init; }
    public TenantTypeDto Type { get; init; }
    public TenantDeploymentModelDto DeploymentModel { get; init; }
    public TenantStatusDto Status { get; init; }
    public TenantBrandingDto Branding { get; init; } = new();
    public List<string> Features { get; init; } = new();
    public RegistrationSettingsDto RegistrationSettings { get; init; } = new();
}

// New: Tenant type enum for public consumption
public enum TenantTypeDto
{
    Standard = 0,
    Enterprise = 1,
    Trial = 2,
    Internal = 3,
    WhiteLabel = 4,
    System = 5
}

// New: Tenant deployment model enum for public consumption
public enum TenantDeploymentModelDto
{
    Shared = 0,
    Dedicated = 1,
    Isolated = 2
}

// New: Tenant status enum for public consumption
public enum TenantStatusDto
{
    Active = 0,
    Inactive = 1,
    Suspended = 2,
    Pending = 3,
    Expired = 4,
    Maintenance = 5
}