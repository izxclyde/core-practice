using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using HCSN.Identity.Domain.Entities;
using HCSN.Identity.Domain.Interfaces;
using HCSN.Identity.Public;
using HCSN.Identity.Application.Interfaces;

namespace HCSN.Identity.Infrastructure.Services;

public class TenantRegistrationService : ITenantRegistrationService, ITenantAdminService
{
    private readonly ITenantRepository _tenantRepository;
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenGenerator _tokenGenerator;
    private readonly ILogger<TenantRegistrationService> _logger;
    private readonly IEmailService? _emailService;
    private readonly IFeatureService _featureService;
    
    public TenantRegistrationService(
        ITenantRepository tenantRepository,
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        ITokenGenerator tokenGenerator,
        ILogger<TenantRegistrationService> logger,
        IEmailService emailService,
        IFeatureService featureService)
    {
        _tenantRepository = tenantRepository;
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _tokenGenerator = tokenGenerator;
        _logger = logger;
        _emailService = emailService;
        _featureService = featureService;
    }
    
    public async Task<RegistrationResult> RegisterAsync(TenantRegisterRequest request)
    {
        // 1. Find tenant by subdomain or custom domain
        var tenant = await FindTenantByDomainAsync(request.TenantIdentifier);
        if (tenant == null)
        {
            return new RegistrationResult 
            { 
                Success = false, 
                Message = "Invalid tenant",
                ErrorCode = "TENANT_NOT_FOUND"
            };
        }
        
        // 2. Check if tenant is active
        if (tenant.Status != TenantStatus.Active)
        {
            return new RegistrationResult 
            { 
                Success = false, 
                Message = "This tenant is not active",
                ErrorCode = "TENANT_INACTIVE"
            };
        }
        
        // 3. Check if tenant allows registration
        var registrationSettings = tenant.Settings?.Registration ?? new RegistrationSettings();
        if (!registrationSettings.AllowPublicRegistration)
        {
            return new RegistrationResult 
            { 
                Success = false, 
                Message = "This tenant does not allow public registration",
                ErrorCode = "REGISTRATION_DISABLED"
            };
        }
        
        // 4. Check user quota
        var existingUsers = await _userRepository.GetUsersByTenantAsync(tenant.Id);
        if (!tenant.CanAddMoreUsers())
        {
            return new RegistrationResult 
            { 
                Success = false, 
                Message = "User quota exceeded for this tenant",
                ErrorCode = "QUOTA_EXCEEDED",
                QuotaInfo = new QuotaInfoDto
                {
                    CurrentUsers = existingUsers.Count,
                    MaxUsers = tenant.Limits?.MaxUsers ?? 0,
                    QuotaType = "users"
                }
            };
        }
        
        // 5. Validate email domain if restricted
        if (tenant.AllowedDomains?.Any() == true)
        {
            var emailDomain = request.Email.Split('@')[1];
            if (!tenant.IsDomainAllowed(emailDomain))
            {
                return new RegistrationResult 
                { 
                    Success = false, 
                    Message = $"Email domain must be one of: {string.Join(", ", tenant.AllowedDomains)}",
                    ErrorCode = "DOMAIN_NOT_ALLOWED"
                };
            }
        }
        
        // 6. Check if email already exists in tenant
        var existingUser = await _userRepository.GetByEmailAndTenantAsync(request.Email, tenant.Id);
        if (existingUser != null)
        {
            return new RegistrationResult 
            { 
                Success = false, 
                Message = "Email already registered in this tenant",
                ErrorCode = "EMAIL_EXISTS"
            };
        }
        
        // 7. Validate required fields based on tenant settings
        var validationResult = await ValidateRequiredFieldsAsync(request, tenant);
        if (!validationResult.IsValid)
        {
            return new RegistrationResult 
            { 
                Success = false, 
                Message = validationResult.ErrorMessage,
                ErrorCode = "VALIDATION_FAILED",
                ValidationErrors = validationResult.ValidationErrors
            };
        }
        
        // 8. Process custom fields
        var customFieldsData = await ProcessCustomFieldsAsync(request.CustomFields, tenant);
        
        // 9. Determine accessible systems/features for user
        var accessibleSystems = DetermineUserAccessibleSystems(tenant, request);
        
        // 10. Create user with tenant context
        var user = new User(
            request.Email, 
            request.PhoneNumber ?? string.Empty,
            request.FirstName, 
            request.LastName, 
            accessibleSystems,
            tenant.Id
        );
        
        // Set password
        var passwordHash = _passwordHasher.Hash(request.Password);
        user.SetPassword(passwordHash);
        
        // Store custom fields data
        if (customFieldsData.Any())
        {
            user.SetMetadata("custom_fields", System.Text.Json.JsonSerializer.Serialize(customFieldsData));
        }
        
        // 11. Handle different registration flows based on tenant configuration
        var registrationFlow = DetermineRegistrationFlow(tenant, registrationSettings);
        
        switch (registrationFlow)
        {
            case RegistrationFlow.RequiresApproval:
                user.SetPendingApproval();
                await _userRepository.AddAsync(user);
                
                await NotifyAdminsOfPendingUser(user, tenant, customFieldsData);
                
                return new RegistrationResult
                {
                    Success = true,
                    Message = "Registration submitted for approval",
                    User = MapToDto(user),
                    RequiresApproval = true,
                    NextStep = "pending_approval",
                    FlowType = "approval"
                };
                
            case RegistrationFlow.RequiresEmailConfirmation:
                await _userRepository.AddAsync(user);
                
                var confirmationToken = GenerateEmailConfirmationToken();
                await SendConfirmationEmail(user, confirmationToken, tenant);
                
                return new RegistrationResult
                {
                    Success = true,
                    Message = "Registration successful. Please check your email to confirm.",
                    User = MapToDto(user),
                    RequiresEmailConfirmation = true,
                    NextStep = "confirm_email",
                    FlowType = "email_confirmation"
                };
                
            case RegistrationFlow.RequiresPayment:
                // Handle paid registration flow
                var paymentSession = await CreatePaymentSession(user, tenant);
                
                return new RegistrationResult
                {
                    Success = true,
                    Message = "Please complete payment to finish registration",
                    User = MapToDto(user),
                    RequiresPayment = true,
                    NextStep = "payment",
                    PaymentSessionId = paymentSession?.SessionId,
                    PaymentUrl = paymentSession?.PaymentUrl,
                    FlowType = "payment"
                };
                
            case RegistrationFlow.Complete:
            default:
                await _userRepository.AddAsync(user);
                
                // Generate tokens for immediate login
                var token = _tokenGenerator.GenerateToken(user);
                var refreshToken = _tokenGenerator.GenerateRefreshToken();
                user.UpdateRefreshToken(refreshToken, DateTime.UtcNow.AddDays(7));
                await _userRepository.UpdateAsync(user);
                
                // Send welcome email if configured
                if (registrationSettings.SendWelcomeEmail)
                {
                    await SendWelcomeEmail(user, tenant, registrationSettings.WelcomeEmailTemplate);
                }
                
                // Auto-provision workspace if configured
                if (registrationSettings.AutoProvisionWorkspace)
                {
                    await AutoProvisionUserWorkspace(user, tenant);
                }
                
                return new RegistrationResult
                {
                    Success = true,
                    Message = "Registration successful",
                    User = MapToDto(user),
                    Token = token,
                    RefreshToken = refreshToken,
                    NextStep = "complete",
                    FlowType = "direct",
                    RedirectUrl = registrationSettings.RedirectAfterLogin
                };
        }
    }
    
    public async Task<RegistrationSettingsDto> GetRegistrationSettingsAsync(string identifier)
    {
        var tenant = await FindTenantByDomainAsync(identifier);
        if (tenant == null)
            return new RegistrationSettingsDto();
            
        var settings = tenant.Settings?.Registration ?? new RegistrationSettings();
        var branding = tenant.Branding ?? new TenantBranding();
        
        return new RegistrationSettingsDto
        {
            // Basic settings
            AllowPublicRegistration = settings.AllowPublicRegistration,
            RequireEmailConfirmation = settings.RequireEmailConfirmation,
            RequireAdminApproval = settings.RequireAdminApproval,
            AllowSocialLogin = settings.AllowSocialLogin,
            SocialLoginProviders = settings.SocialLoginProviders,
            
            // Enhanced fields based on tenant type
            CollectPhoneNumber = settings.CustomFields?.Any(f => f.FieldName == "PhoneNumber") ?? false,
            CollectAddress = settings.CustomFields?.Any(f => f.FieldName == "Address") ?? false,
            CollectCompanyName = settings.CustomFields?.Any(f => f.FieldName == "CompanyName") ?? false,
            
            // New flexible fields
            CustomFields = settings.CustomFields?.Select(f => new CustomFieldDto
            {
                FieldName = f.FieldName,
                FieldType = f.FieldType,
                IsRequired = f.IsRequired,
                IsVisible = f.IsVisible,
                Options = f.Options,
                DisplayOrder = f.DisplayOrder,
                DefaultValue = f.DefaultValue,
                ValidationRegex = f.ValidationRegex,
                ErrorMessage = f.ErrorMessage
            }).ToList() ?? new List<CustomFieldDto>(),
            
            // Tenant info
            TenantName = tenant.Name,
            TenantType = tenant.Type.ToString(),
            DeploymentModel = tenant.DeploymentModel.ToString(),
            
            // Branding
            PrimaryColor = branding.PrimaryColor,
            LogoUrl = branding.LogoUrl,
            LoginBackgroundImage = branding.LoginBackgroundImage,
            
            // Additional settings
            DefaultRole = settings.DefaultRole,
            InvitationExpiryHours = settings.InvitationExpiryHours,
            
            // Features
            AvailableFeatures = GetAvailableFeatures(tenant)
        };
    }
    
    public async Task<bool> ValidateEmailDomainAsync(string email, string identifier)
    {
        var tenant = await FindTenantByDomainAsync(identifier);
        if (tenant == null || tenant.AllowedDomains?.Any() != true)
            return true;
            
        var emailDomain = email.Split('@')[1];
        return tenant.IsDomainAllowed(emailDomain);
    }
    
public async Task<RegistrationResult> RegisterWithInviteAsync(string token, BaseRegisterRequest request)
{
    // Validate invitation token
    var invitation = await ValidateInvitationToken(token);
    if (invitation == null)
    {
        return new RegistrationResult 
        { 
            Success = false, 
            Message = "Invalid or expired invitation",
            ErrorCode = "INVALID_INVITATION"
        };
    }
    
    var tenant = await _tenantRepository.GetByIdAsync(invitation.TenantId);
    if (tenant == null)
    {
        return new RegistrationResult 
        { 
            Success = false, 
            Message = "Tenant not found",
            ErrorCode = "TENANT_NOT_FOUND"
        };
    }
    
    // Create a new request with the email from invitation
    var updatedRequest = new BaseRegisterRequest
    {
        Email = invitation.Email,
        FirstName = request.FirstName,
        LastName = request.LastName,
        Password = request.Password,
        PhoneNumber = request.PhoneNumber,
        InvitationToken = request.InvitationToken,
        Metadata = request.Metadata,
        Preferences = request.Preferences
    };
    
    // Create a TenantRegisterRequest from the base request
    // You'll need to adapt this based on your actual TenantRegisterRequest structure
    var tenantRegisterRequest = new TenantRegisterRequest
    {
        Email = updatedRequest.Email,
        FirstName = updatedRequest.FirstName,
        LastName = updatedRequest.LastName,
        Password = updatedRequest.Password,
        PhoneNumber = updatedRequest.PhoneNumber,
        InvitationToken = updatedRequest.InvitationToken,
        Metadata = updatedRequest.Metadata,
        Preferences = updatedRequest.Preferences,
        TenantIdentifier = tenant.Subdomain, // Add tenant identifier
        // Add other properties as needed
    };
    
    // Proceed with registration but skip email validation
    return await CompleteInvitedRegistration(tenantRegisterRequest, tenant, invitation);
}

// Update the CompleteInvitedRegistration method to accept TenantRegisterRequest
private async Task<RegistrationResult> CompleteInvitedRegistration(
    TenantRegisterRequest request, 
    Tenant tenant, 
    Invitation invitation)
{
    // Similar to RegisterAsync but with pre-validated invitation
    // You can reuse much of the RegisterAsync logic here
    // For now, return a placeholder
    return new RegistrationResult { Success = false, Message = "Not fully implemented" };
}
    
    public async Task<List<string>> GetRequiredFieldsAsync(string identifier)
    {
        var tenant = await FindTenantByDomainAsync(identifier);
        if (tenant?.Settings?.Registration?.CustomFields == null)
        {
            return new List<string> { "Email", "FirstName", "LastName" };
        }
        
        return tenant.Settings.Registration.CustomFields
            .Where(f => f.IsRequired)
            .Select(f => f.FieldName)
            .ToList();
    }
    
    public async Task<RegistrationResult> ApproveUserAsync(Guid userId, Guid tenantId, string? notes = null)
    {
        var tenant = await _tenantRepository.GetByIdAsync(tenantId);
        if (tenant == null)
        {
            return new RegistrationResult 
            { 
                Success = false, 
                Message = "Tenant not found",
                ErrorCode = "TENANT_NOT_FOUND"
            };
        }
        
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null || user.TenantId != tenantId)
        {
            return new RegistrationResult 
            { 
                Success = false, 
                Message = "User not found",
                ErrorCode = "USER_NOT_FOUND"
            };
        }
        
        user.Approve();
        user.SetMetadata("approval_notes", notes);
        user.SetMetadata("approved_by", "admin"); // In real app, get current admin
        user.SetMetadata("approved_at", DateTime.UtcNow.ToString("o"));
        
        await _userRepository.UpdateAsync(user);
        
        // Generate welcome token if needed
        var token = _tokenGenerator.GenerateToken(user);
        
        await SendApprovalEmail(user, tenant, token);
        
        return new RegistrationResult
        {
            Success = true,
            Message = "User approved successfully",
            User = MapToDto(user),
            Token = token
        };
    }
    
    public async Task<RegistrationResult> RejectUserAsync(Guid userId, Guid tenantId, string reason)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null || user.TenantId != tenantId)
        {
            return new RegistrationResult 
            { 
                Success = false, 
                Message = "User not found",
                ErrorCode = "USER_NOT_FOUND"
            };
        }
        
        user.Reject(reason);
        user.SetMetadata("rejection_reason", reason);
        user.SetMetadata("rejected_at", DateTime.UtcNow.ToString("o"));
        
        await _userRepository.UpdateAsync(user);
        
        await SendRejectionEmail(user, reason);
        
        return new RegistrationResult
        {
            Success = true,
            Message = "User rejected",
            User = MapToDto(user)
        };
    }
    
    public async Task<List<PendingUserDto>> GetPendingApprovalsAsync(Guid tenantId)
    {
        var pendingUsers = await _userRepository.GetPendingUsersByTenantAsync(tenantId);
        return pendingUsers.Select(u => new PendingUserDto(
            u.Id,
            u.Email,
            u.FullName,
            u.CreatedAt,
            u.GetMetadata<Dictionary<string, object>>("custom_fields") ?? new Dictionary<string, object>()
        )).ToList();
    }
    
public async Task<RegistrationResult> ConfirmEmailAsync(string token)
{
    // Validate token and confirm user email
    var userId = ValidateEmailConfirmationToken(token);
    if (userId == null)
    {
        return new RegistrationResult 
        { 
            Success = false, 
            Message = "Invalid confirmation token",
            ErrorCode = "INVALID_TOKEN"
        };
    }
    
    var user = await _userRepository.GetByIdAsync(userId.Value);
    if (user == null)
    {
        return new RegistrationResult 
        { 
            Success = false, 
            Message = "User not found",
            ErrorCode = "USER_NOT_FOUND"
        };
    }
    
    user.ConfirmEmail();
    await _userRepository.UpdateAsync(user);
    
    // FIX: Rename this to avoid conflict with parameter
    var authToken = _tokenGenerator.GenerateToken(user);
    
    return new RegistrationResult
    {
        Success = true,
        Message = "Email confirmed successfully",
        User = MapToDto(user),
        Token = authToken  // Use the renamed variable
    };
}
    
    // Private helper methods
    
    private async Task<Tenant?> FindTenantByDomainAsync(string identifier)
    {
        // Try by subdomain first
        var tenant = await _tenantRepository.GetBySubdomainAsync(identifier);
        
        // If not found, try by custom domain
        if (tenant == null)
        {
            tenant = await _tenantRepository.GetByCustomDomainAsync(identifier);
        }
        
        return tenant;
    }
    
    private async Task<(bool IsValid, string ErrorMessage, Dictionary<string, string> ValidationErrors)> 
        ValidateRequiredFieldsAsync(TenantRegisterRequest request, Tenant tenant)
    {
        var errors = new Dictionary<string, string>();
        var customFields = tenant.Settings?.Registration?.CustomFields ?? new List<CustomField>();
        
        foreach (var field in customFields.Where(f => f.IsRequired))
        {
            switch (field.FieldName.ToLower())
            {
                case "email":
                    if (string.IsNullOrEmpty(request.Email))
                        errors[field.FieldName] = field.ErrorMessage ?? "Email is required";
                    else if (!string.IsNullOrEmpty(field.ValidationRegex) && 
                             !Regex.IsMatch(request.Email, field.ValidationRegex))
                        errors[field.FieldName] = field.ErrorMessage ?? "Invalid email format";
                    break;
                    
                case "firstname":
                    if (string.IsNullOrEmpty(request.FirstName))
                        errors[field.FieldName] = field.ErrorMessage ?? "First name is required";
                    break;
                    
                case "lastname":
                    if (string.IsNullOrEmpty(request.LastName))
                        errors[field.FieldName] = field.ErrorMessage ?? "Last name is required";
                    break;
                    
                case "phonenumber":
                    if (string.IsNullOrEmpty(request.PhoneNumber))
                        errors[field.FieldName] = field.ErrorMessage ?? "Phone number is required";
                    else if (!string.IsNullOrEmpty(field.ValidationRegex) && 
                             !Regex.IsMatch(request.PhoneNumber, field.ValidationRegex))
                        errors[field.FieldName] = field.ErrorMessage ?? "Invalid phone format";
                    break;
                    
                case "companyname":
                    if (string.IsNullOrEmpty(request.CompanyName))
                        errors[field.FieldName] = field.ErrorMessage ?? "Company name is required";
                    break;
                    
                case "address":
                    if (string.IsNullOrEmpty(request.Address))
                        errors[field.FieldName] = field.ErrorMessage ?? "Address is required";
                    break;
                    
                default:
                    // Handle custom fields from request.CustomFields dictionary
                    if (request.CustomFields == null || !request.CustomFields.ContainsKey(field.FieldName))
                    {
                        errors[field.FieldName] = field.ErrorMessage ?? $"{field.FieldName} is required";
                    }
                    else if (!string.IsNullOrEmpty(field.ValidationRegex) && 
                             !Regex.IsMatch(request.CustomFields[field.FieldName]?.ToString() ?? "", field.ValidationRegex))
                    {
                        errors[field.FieldName] = field.ErrorMessage ?? $"Invalid {field.FieldName} format";
                    }
                    break;
            }
        }
        
        return (
            IsValid: errors.Count == 0,
            ErrorMessage: errors.Count > 0 ? "Validation failed" : string.Empty,
            ValidationErrors: errors
        );
    }
    
    private async Task<Dictionary<string, object>> ProcessCustomFieldsAsync(
        Dictionary<string, object>? requestCustomFields, 
        Tenant tenant)
    {
        var result = new Dictionary<string, object>();
        var customFields = tenant.Settings?.Registration?.CustomFields ?? new List<CustomField>();
        
        foreach (var field in customFields)
        {
            if (requestCustomFields?.TryGetValue(field.FieldName, out var value) == true)
            {
                // Validate based on field type
                if (field.FieldType == "select" && field.Options?.Contains(value?.ToString() ?? "") == false)
                {
                    _logger.LogWarning("Invalid option for field {FieldName}: {Value}", field.FieldName, value);
                    continue;
                }
                
                result[field.FieldName] = value ?? field.DefaultValue ?? string.Empty;
            }
            else if (!string.IsNullOrEmpty(field.DefaultValue))
            {
                result[field.FieldName] = field.DefaultValue;
            }
        }
        
        return result;
    }
    
    private List<string> DetermineUserAccessibleSystems(Tenant tenant, TenantRegisterRequest request)
    {
        var systems = new List<string>();
        
        // Base system based on deployment model
        systems.Add(tenant.DeploymentModel switch
        {
            TenantDeploymentModel.Shared => "tenant-base",
            TenantDeploymentModel.Dedicated => $"tenant-{tenant.Subdomain}",
            TenantDeploymentModel.Isolated => $"isolated-{tenant.Subdomain}",
            _ => "tenant-base"
        });
        
        // Add features from tenant that user should have access to
        if (tenant.Features != null)
        {
            foreach (var feature in tenant.Features.Keys)
            {
                if (_featureService.IsFeatureAvailableForNewUsers(feature, tenant))
                {
                    systems.Add(feature);
                }
            }
        }
        
        // Add default role from registration settings
        var defaultRole = tenant.Settings?.Registration?.DefaultRole;
        if (!string.IsNullOrEmpty(defaultRole) && !systems.Contains(defaultRole))
        {
            systems.Add(defaultRole);
        }
        
        return systems.Distinct().ToList();
    }
    
    private RegistrationFlow DetermineRegistrationFlow(Tenant tenant, RegistrationSettings settings)
    {
        if (settings.RequireAdminApproval)
            return RegistrationFlow.RequiresApproval;
            
        if (settings.RequireEmailConfirmation)
            return RegistrationFlow.RequiresEmailConfirmation;
            
        // Check if tenant has payment requirements
        if (tenant.HasFeature("paid_plan") && tenant.Billing?.PlanId != "free")
            return RegistrationFlow.RequiresPayment;
            
        return RegistrationFlow.Complete;
    }
    
    private List<string> GetAvailableFeatures(Tenant tenant)
    {
        var features = new List<string>();
        
        if (tenant.Features != null)
        {
            foreach (var feature in tenant.Features)
            {
                if (feature.Value is bool boolValue && boolValue)
                    features.Add(feature.Key);
                else if (feature.Value is string strValue && !string.IsNullOrEmpty(strValue))
                    features.Add(feature.Key);
            }
        }
        
        return features;
    }
    
    // Email notification methods
    private async Task NotifyAdminsOfPendingUser(User user, Tenant tenant, Dictionary<string, object> customFields)
    {
        var admins = await _userRepository.GetTenantAdminsAsync(tenant.Id);
        var customFieldsHtml = string.Join("<br/>", customFields.Select(f => $"{f.Key}: {f.Value}"));
        
        foreach (var admin in admins)
        {
            if (_emailService != null)
            {
                await _emailService.SendEmailAsync(
                    admin.Email,
                    $"New User Pending Approval - {tenant.Name}",
                    $@"
                    <h2>New User Registration</h2>
                    <p>A new user has registered and needs approval:</p>
                    <ul>
                        <li><strong>Name:</strong> {user.FullName}</li>
                        <li><strong>Email:</strong> {user.Email}</li>
                        <li><strong>Registered:</strong> {user.CreatedAt:yyyy-MM-dd HH:mm}</li>
                    </ul>
                    <h3>Additional Information:</h3>
                    <p>{customFieldsHtml}</p>
                    <p>
                        <a href='https://admin.{tenant.Subdomain}.hcsn.com/users/pending/{user.Id}'>
                            Review Application
                        </a>
                    </p>
                    "
                );
            }
            else
            {
                _logger.LogWarning("Email service not configured. Would have sent approval request to {Email} for user {UserId}", 
                    admin.Email, user.Id);
            }
        }
    }
    
    private async Task SendConfirmationEmail(User user, string token, Tenant tenant)
    {
        if (_emailService != null)
        {
            var branding = tenant.Branding ?? new TenantBranding();
            var confirmationUrl = $"https://{tenant.GetFullDomain()}/confirm-email?token={token}";
            
            await _emailService.SendEmailWithTemplateAsync(
                user.Email,
                "confirm-email",
                new Dictionary<string, object>
                {
                    ["user_name"] = user.FullName,
                    ["confirmation_url"] = confirmationUrl,
                    ["tenant_name"] = tenant.Name,
                    ["logo_url"] = branding.LogoUrl ?? "",
                    ["primary_color"] = branding.PrimaryColor ?? "",
                    ["expiry_hours"] = tenant.Settings?.Registration?.InvitationExpiryHours ?? "48"
                }
            );
        }
        else
        {
            _logger.LogWarning("Email service not configured. Would have sent confirmation email to {Email} with token {Token}", 
                user.Email, token);
        }
    }
    
    private async Task SendWelcomeEmail(User user, Tenant tenant, string? templateId)
    {
        if (_emailService != null && !string.IsNullOrEmpty(templateId))
        {
            var branding = tenant.Branding ?? new TenantBranding();
            var loginUrl = $"https://{tenant.GetFullDomain()}/login";
            
            await _emailService.SendEmailWithTemplateAsync(
                user.Email,
                templateId,
                new Dictionary<string, object>
                {
                    ["user_name"] = user.FullName,
                    ["login_url"] = loginUrl,
                    ["tenant_name"] = tenant.Name,
                    ["logo_url"] = branding.LogoUrl ?? "",
                    ["support_email"] = $"support@{tenant.Subdomain}.hcsn.com"
                }
            );
        }
    }
    
    private async Task SendApprovalEmail(User user, Tenant tenant, string token)
    {
        if (_emailService != null)
        {
            var loginUrl = $"https://{tenant.GetFullDomain()}/login?token={token}";
            
            await _emailService.SendEmailAsync(
                user.Email,
                $"Your {tenant.Name} Account Has Been Approved",
                $@"
                <h2>Welcome to {tenant.Name}!</h2>
                <p>Your account has been approved. You can now log in:</p>
                <p><a href='{loginUrl}'>Log in to your account</a></p>
                <p>If the button doesn't work, copy this link: {loginUrl}</p>
                "
            );
        }
        else
        {
            _logger.LogWarning("Email service not configured. Would have sent approval email to {Email}", user.Email);
        }
    }
    
    private async Task SendRejectionEmail(User user, string reason)
    {
        if (_emailService != null)
        {
            await _emailService.SendEmailAsync(
                user.Email,
                "Account Registration Update",
                $@"
                <h2>Registration Update</h2>
                <p>We regret to inform you that your account registration has been rejected.</p>
                <p><strong>Reason:</strong> {reason}</p>
                <p>If you believe this is an error, please contact support.</p>
                "
            );
        }
        else
        {
            _logger.LogWarning("Email service not configured. Would have sent rejection email to {Email} with reason: {Reason}", 
                user.Email, reason);
        }
    }
    
    // Helper methods for new flows
    private async Task<Invitation?> ValidateInvitationToken(string token)
    {
        // Implementation for validating invitation tokens
        // This would check against stored invitations in database
        return null; // Placeholder
    }
    
    private async Task<RegistrationResult?> CompleteInvitedRegistration(
        BaseRegisterRequest request, 
        Tenant tenant, 
        Invitation invitation)
    {
        // Similar to RegisterAsync but with pre-validated invitation
        // Implementation details...
        return new RegistrationResult { Success = false };
    }
    
    private async Task<PaymentSession?> CreatePaymentSession(User user, Tenant tenant)
    {
        // Create payment session with Stripe/PayPal etc.
        return new PaymentSession
        {
            SessionId = Guid.NewGuid().ToString(),
            PaymentUrl = $"https://pay.hcsn.com/session/{Guid.NewGuid()}"
        };
    }
    
    private async Task AutoProvisionUserWorkspace(User user, Tenant tenant)
    {
        // Auto-create default resources for new user
        _logger.LogInformation("Auto-provisioning workspace for user {UserId} in tenant {TenantId}", 
            user.Id, tenant.Id);
        
        // Implementation would create:
        // - Default folders
        // - Initial data
        // - Welcome content
        // - etc.
        
        await Task.CompletedTask;
    }
    
    private string GenerateEmailConfirmationToken()
    {
        return Convert.ToBase64String(Guid.NewGuid().ToByteArray())
            .Replace("/", "_")
            .Replace("+", "-");
    }
    
    private Guid? ValidateEmailConfirmationToken(string token)
    {
        try
        {
            // In real implementation, this would check against stored tokens
            // For now, just return a dummy ID
            return Guid.NewGuid();
        }
        catch
        {
            return null;
        }
    }
    
    private UserDto MapToDto(User user)
    {
        return new UserDto(
            user.Id,
            user.Email,
            user.FullName,
            user.EmailConfirmed,
            user.TwoFactorEnabled,
            user.IsActive,
            user.AccessibleSystems,
            user.CreatedAt,
            user.UpdatedAt,
            user.DeletedAt,
            user.LastLoginAt
        );
    }
}

// Supporting enums and classes
public enum RegistrationFlow
{
    Complete,
    RequiresEmailConfirmation,
    RequiresApproval,
    RequiresPayment
}

public class Invitation
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public bool IsUsed { get; set; }
}

public class PaymentSession
{
    public string SessionId { get; set; } = string.Empty;
    public string PaymentUrl { get; set; } = string.Empty;
}

public class QuotaInfo
{
    public int CurrentUsers { get; set; }
    public int MaxUsers { get; set; }
    public string QuotaType { get; set; } = string.Empty;
}