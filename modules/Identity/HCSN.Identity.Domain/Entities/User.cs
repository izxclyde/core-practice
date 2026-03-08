using System;
using System.Collections.Generic;
using System.Linq;
using HCSN.Identity.Application.Interfaces;

namespace HCSN.Identity.Domain.Entities;

public class User
{
    private User() { } // For EF Core
    
    public User(
        string email, 
        string phoneNumber, 
        string firstName, 
        string lastName, 
        List<string>? accessibleSystems = null, 
        Guid? tenantId = null,
        string? invitedBy = null,
        UserType userType = UserType.Regular)
    {
        Id = Guid.NewGuid();
        Email = email ?? throw new ArgumentNullException(nameof(email));
        PhoneNumber = phoneNumber ?? throw new ArgumentNullException(nameof(phoneNumber));
        FirstName = firstName ?? throw new ArgumentNullException(nameof(firstName));
        LastName = lastName ?? throw new ArgumentNullException(nameof(lastName));
        Status = UserStatus.Active;
        EmailConfirmed = false;
        PhoneConfirmed = false;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
        AccessibleSystems = accessibleSystems ?? new List<string>();
        TenantId = tenantId;
        IsTenantAdmin = false;
        UserType = userType;
        InvitedBy = invitedBy;
        LoginAttempts = 0;
        LockoutEnd = null;
        _customData = new Dictionary<string, object>();
        _metadata = new Dictionary<string, string>();
        _roles = new List<string>();
        _permissions = new List<string>();
        _deviceTokens = new List<string>();
    }
    
    // Core Properties
    public Guid Id { get; private set; }
    public string Email { get; private set; }
    public string PhoneNumber { get; private set; }
    public string FirstName { get; private set; }
    public string LastName { get; private set; }
    public string FullName => $"{FirstName} {LastName}";
    public UserStatus Status { get; private set; }
    public UserType UserType { get; private set; }
    
    // Authentication
    public string? PasswordHash { get; private set; }
    public string? RefreshToken { get; private set; }
    public DateTime? RefreshTokenExpiryTime { get; private set; }
    public bool TwoFactorEnabled { get; private set; }
    public string? TwoFactorSecret { get; private set; }
    public List<string> TwoFactorRecoveryCodes { get; private set; } = new();
    
    // Security
    public int LoginAttempts { get; private set; }
    public DateTime? LockoutEnd { get; private set; }
    public DateTime? LastPasswordChangedAt { get; private set; }
    public string? LastLoginIp { get; private set; }
    public string? LastLoginUserAgent { get; private set; }
    public List<string> KnownDevices { get; private set; } = new();
    
    // Confirmation Status
    public bool EmailConfirmed { get; private set; }
    public DateTime? EmailConfirmedAt { get; private set; }
    public bool PhoneConfirmed { get; private set; }
    public DateTime? PhoneConfirmedAt { get; private set; }
    public bool TermsAccepted { get; private set; }
    public DateTime? TermsAcceptedAt { get; private set; }
    public bool PrivacyAccepted { get; private set; }
    public DateTime? PrivacyAcceptedAt { get; private set; }
    
    // Timestamps
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public DateTime? DeletedAt { get; private set; }
    public DateTime? LastLoginAt { get; private set; }
    public DateTime? LastActivityAt { get; private set; }
    
    // Status
    public bool IsActive { get; private set; }
    public bool IsLocked => LockoutEnd.HasValue && LockoutEnd > DateTime.UtcNow;
    
    // Tenant Relationship
    public Guid? TenantId { get; private set; }
    public Tenant? Tenant { get; private set; }
    public bool IsTenantAdmin { get; private set; }
    public DateTime? TenantAdminAssignedAt { get; private set; }
    
    // Access Control
    public List<string> AccessibleSystems { get; private set; }
    private List<string> _roles;
    public IReadOnlyList<string> Roles => _roles.AsReadOnly();
    
    private List<string> _permissions;
    public IReadOnlyList<string> Permissions => _permissions.AsReadOnly();
    
    // Invitation Tracking
    public string? InvitedBy { get; private set; }
    public DateTime? InvitationSentAt { get; private set; }
    public DateTime? InvitationAcceptedAt { get; private set; }
    public string? InvitationToken { get; private set; }
    
    // Flexible Data Storage
    private Dictionary<string, object>? _customData;
    public IReadOnlyDictionary<string, object>? CustomData => _customData;
    
    private Dictionary<string, string>? _metadata;
    public IReadOnlyDictionary<string, string>? Metadata => _metadata;
    
    // Device Management
    private List<string> _deviceTokens;
    public IReadOnlyList<string> DeviceTokens => _deviceTokens.AsReadOnly();
    
    // Preferences
    public string? PreferredLanguage { get; private set; }
    public string? PreferredTheme { get; private set; }
    public bool EmailNotifications { get; private set; } = true;
    public bool PushNotifications { get; private set; } = true;
    public string? Timezone { get; private set; }
    
    // Rejection tracking
    private string? _rejectionReason;
    public string? RejectionReason => _rejectionReason;
    public DateTime? RejectedAt { get; private set; }
    
    // Approval tracking
    public string? ApprovedBy { get; private set; }
    public DateTime? ApprovedAt { get; private set; }
    
    // Password Management
    public void SetPassword(string passwordHash)
    {
        PasswordHash = passwordHash ?? throw new ArgumentNullException(nameof(passwordHash));
        LastPasswordChangedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }
    
    public bool ValidatePassword(string password, IPasswordHasher hasher)
    {
        return hasher.Verify(PasswordHash, password);
    }
    
    // Email Confirmation
    public void ConfirmEmail()
    {
        EmailConfirmed = true;
        EmailConfirmedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }
    
    // Phone Confirmation
    public void ConfirmPhone()
    {
        PhoneConfirmed = true;
        PhoneConfirmedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }
    
    // Token Management
    public void UpdateRefreshToken(string refreshToken, DateTime expiryTime)
    {
        RefreshToken = refreshToken ?? throw new ArgumentNullException(nameof(refreshToken));
        RefreshTokenExpiryTime = expiryTime;
        UpdatedAt = DateTime.UtcNow;
    }
    
    public void ClearRefreshToken()
    {
        RefreshToken = null;
        RefreshTokenExpiryTime = null;
        UpdatedAt = DateTime.UtcNow;
    }
    
    // Two Factor Authentication
    public void EnableTwoFactor(string secret)
    {
        TwoFactorEnabled = true;
        TwoFactorSecret = secret;
        UpdatedAt = DateTime.UtcNow;
    }
    
    public void DisableTwoFactor()
    {
        TwoFactorEnabled = false;
        TwoFactorSecret = null;
        UpdatedAt = DateTime.UtcNow;
    }
    
    public void GenerateTwoFactorRecoveryCodes(int count = 10)
    {
        TwoFactorRecoveryCodes = Enumerable.Range(0, count)
            .Select(_ => GenerateRecoveryCode())
            .ToList();
        UpdatedAt = DateTime.UtcNow;
    }
    
    // Access Control
    public void AddAccessibleSystem(string systemName)
    {
        if (!string.IsNullOrWhiteSpace(systemName) && !AccessibleSystems.Contains(systemName))
        {
            AccessibleSystems.Add(systemName);
            UpdatedAt = DateTime.UtcNow;
        }
    }
    
    public void RemoveAccessibleSystem(string systemName)
    {
        if (AccessibleSystems.Contains(systemName))
        {
            AccessibleSystems.Remove(systemName);
            UpdatedAt = DateTime.UtcNow;
        }
    }
    
    public bool HasAccessToSystem(string systemName)
    {
        return IsActive && AccessibleSystems.Contains(systemName);
    }
    
    // Role Management
    public void AddRole(string role)
    {
        if (!string.IsNullOrWhiteSpace(role) && !_roles.Contains(role))
        {
            _roles.Add(role);
            UpdatedAt = DateTime.UtcNow;
        }
    }
    
    public void RemoveRole(string role)
    {
        if (_roles.Contains(role))
        {
            _roles.Remove(role);
            UpdatedAt = DateTime.UtcNow;
        }
    }
    
    public bool HasRole(string role)
    {
        return _roles.Contains(role);
    }
    
    // Permission Management
    public void AddPermission(string permission)
    {
        if (!string.IsNullOrWhiteSpace(permission) && !_permissions.Contains(permission))
        {
            _permissions.Add(permission);
            UpdatedAt = DateTime.UtcNow;
        }
    }
    
    public void RemovePermission(string permission)
    {
        if (_permissions.Contains(permission))
        {
            _permissions.Remove(permission);
            UpdatedAt = DateTime.UtcNow;
        }
    }
    
    public bool HasPermission(string permission)
    {
        return _permissions.Contains(permission);
    }
    
    // Status Management
    public void Activate()
    {
        IsActive = true;
        Status = UserStatus.Active;
        UpdatedAt = DateTime.UtcNow;
    }
    
    public void Deactivate()
    {
        IsActive = false;
        Status = UserStatus.Inactive;
        UpdatedAt = DateTime.UtcNow;
    }
    
    public void SoftDelete()
    {
        IsActive = false;
        Status = UserStatus.Deleted;
        DeletedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }
    
    public void Suspend()
    {
        Status = UserStatus.Suspended;
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }
    
    // Login Tracking
    public void UpdateLastLogin(string? ipAddress = null, string? userAgent = null)
    {
        LastLoginAt = DateTime.UtcNow;
        LastActivityAt = DateTime.UtcNow;
        LastLoginIp = ipAddress;
        LastLoginUserAgent = userAgent;
        LoginAttempts = 0; // Reset on successful login
        UpdatedAt = DateTime.UtcNow;
    }
    
    public void RecordActivity()
    {
        LastActivityAt = DateTime.UtcNow;
    }
    
    // Lockout Management
    public void IncrementLoginAttempts()
    {
        LoginAttempts++;
        UpdatedAt = DateTime.UtcNow;
    }
    
    public void ResetLoginAttempts()
    {
        LoginAttempts = 0;
        LockoutEnd = null;
        UpdatedAt = DateTime.UtcNow;
    }
    
    public void LockUntil(DateTime lockoutEnd)
    {
        LockoutEnd = lockoutEnd;
        UpdatedAt = DateTime.UtcNow;
    }
    
    // Tenant Admin Management
    public void MakeTenantAdmin(string? assignedBy = null) 
    { 
        IsTenantAdmin = true;
        TenantAdminAssignedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }
    
    public void RemoveTenantAdmin() 
    { 
        IsTenantAdmin = false;
        UpdatedAt = DateTime.UtcNow;
    }
    
    // Approval Flow
    public void SetPendingApproval() 
    { 
        Status = UserStatus.PendingApproval;
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }
    
    public void Approve(string? approvedBy = null) 
    { 
        Status = UserStatus.Active;
        IsActive = true;
        ApprovedBy = approvedBy;
        ApprovedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }
    
    public void Reject(string reason) 
    { 
        Status = UserStatus.Rejected;
        IsActive = false;
        _rejectionReason = reason;
        RejectedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }
    
    // Invitation Flow
    public void SendInvitation(string invitedBy, string token)
    {
        InvitedBy = invitedBy;
        InvitationToken = token;
        InvitationSentAt = DateTime.UtcNow;
        Status = UserStatus.Invited;
        UpdatedAt = DateTime.UtcNow;
    }
    
    public void AcceptInvitation()
    {
        InvitationAcceptedAt = DateTime.UtcNow;
        InvitationToken = null;
        Status = UserStatus.Active;
        UpdatedAt = DateTime.UtcNow;
    }
    
    // Terms and Privacy
    public void AcceptTerms()
    {
        TermsAccepted = true;
        TermsAcceptedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }
    
    public void AcceptPrivacy()
    {
        PrivacyAccepted = true;
        PrivacyAcceptedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }
    
    // Custom Data Management
    public void SetCustomData(Dictionary<string, object> data)
    {
        _customData = data ?? new Dictionary<string, object>();
        UpdatedAt = DateTime.UtcNow;
    }
    
    public void UpdateCustomData(string key, object value)
    {
        _customData ??= new Dictionary<string, object>();
        _customData[key] = value;
        UpdatedAt = DateTime.UtcNow;
    }
    
    public T? GetCustomData<T>(string key)
    {
        if (_customData != null && _customData.TryGetValue(key, out var value))
        {
            try
            {
                return (T)Convert.ChangeType(value, typeof(T));
            }
            catch
            {
                return default;
            }
        }
        return default;
    }
    
        // Metadata Management
    public void SetMetadata(string key, string? value)
    {
        if (string.IsNullOrEmpty(value))
            return; // ignore null or empty values

        _metadata ??= new Dictionary<string, string>();
        _metadata[key] = value;
        UpdatedAt = DateTime.UtcNow;
    }
        
    public string? GetMetadata(string key)
    {
        return _metadata?.GetValueOrDefault(key);
    }

    // Generic method to get typed metadata
    public T? GetMetadata<T>(string key)
    {
        var value = GetMetadata(key);
        if (value == null) return default;
        
        try
        {
            // Handle JSON serialized objects
            if (typeof(T) == typeof(Dictionary<string, object>) || 
                typeof(T) == typeof(Dictionary<string, string>) ||
                typeof(T).IsClass && typeof(T) != typeof(string))
            {
                return System.Text.Json.JsonSerializer.Deserialize<T>(value);
            }
            
            // Handle primitive types
            return (T)Convert.ChangeType(value, typeof(T));
        }
        catch
        {
            return default;
        }
    }

    // Helper method to set typed metadata as JSON
    public void SetMetadata<T>(string key, T value)
    {
        if (value == null)
        {
            _metadata?.Remove(key);
            return;
        }
        
        _metadata ??= new Dictionary<string, string>();
        
        if (value is string strValue)
        {
            _metadata[key] = strValue;
        }
        else
        {
            _metadata[key] = System.Text.Json.JsonSerializer.Serialize(value);
        }
        
        UpdatedAt = DateTime.UtcNow;
    }
    
    // Device Management
    public void AddDeviceToken(string deviceToken)
    {
        if (!string.IsNullOrWhiteSpace(deviceToken) && !_deviceTokens.Contains(deviceToken))
        {
            _deviceTokens.Add(deviceToken);
            UpdatedAt = DateTime.UtcNow;
        }
    }
    
    public void RemoveDeviceToken(string deviceToken)
    {
        if (_deviceTokens.Contains(deviceToken))
        {
            _deviceTokens.Remove(deviceToken);
            UpdatedAt = DateTime.UtcNow;
        }
    }
    
    // Known Devices
    public void AddKnownDevice(string deviceIdentifier)
    {
        if (!string.IsNullOrWhiteSpace(deviceIdentifier) && !KnownDevices.Contains(deviceIdentifier))
        {
            KnownDevices.Add(deviceIdentifier);
            UpdatedAt = DateTime.UtcNow;
        }
    }
    
    // Preferences
    public void UpdatePreferences(
        string? language = null, 
        string? theme = null, 
        bool? emailNotifications = null,
        bool? pushNotifications = null,
        string? timezone = null)
    {
        PreferredLanguage = language ?? PreferredLanguage;
        PreferredTheme = theme ?? PreferredTheme;
        EmailNotifications = emailNotifications ?? EmailNotifications;
        PushNotifications = pushNotifications ?? PushNotifications;
        Timezone = timezone ?? Timezone;
        UpdatedAt = DateTime.UtcNow;
    }
    
    // Helper Methods
    public bool IsPendingApproval() => Status == UserStatus.PendingApproval;
    public bool IsRejected() => Status == UserStatus.Rejected;
    public bool IsInvited() => Status == UserStatus.Invited;
    public bool IsSuspended() => Status == UserStatus.Suspended;
    public bool IsDeleted() => Status == UserStatus.Deleted;
    
    private string GenerateRecoveryCode()
    {
        return Convert.ToBase64String(Guid.NewGuid().ToByteArray())
            .Replace("/", "")
            .Replace("+", "")
            .Substring(0, 8)
            .ToUpper();
    }
}

public enum UserStatus
{
    Active,
    Inactive,
    Suspended,
    Deleted,
    PendingApproval,
    Rejected,
    Invited,
    Locked
}

public enum UserType
{
    Regular,
    Admin,
    SuperAdmin,
    Service,
    Api,
    Guest
}