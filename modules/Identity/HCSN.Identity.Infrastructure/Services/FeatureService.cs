using System;
using System.Collections.Generic;
using System.Linq;
using HCSN.Identity.Domain.Entities;
using HCSN.Identity.Application.Interfaces;  // Change this to Application.Interfaces
// Remove: using HCSN.Identity.Domain.Interfaces;

namespace HCSN.Identity.Infrastructure.Services;

public class FeatureService : IFeatureService  // Now implements from Application.Interfaces
{
    public bool IsFeatureAvailableForNewUsers(string feature, Tenant tenant)
    {
        // Check if the feature exists and is enabled for this tenant
        if (tenant.Features != null && tenant.Features.TryGetValue(feature, out var value))
        {
            // Handle different value types
            return value switch
            {
                bool boolValue => boolValue,
                string strValue => !string.IsNullOrEmpty(strValue) && 
                                   strValue != "false" && 
                                   strValue != "0",
                int intValue => intValue > 0,
                long longValue => longValue > 0,
                _ => false
            };
        }
        
        // Check limits if applicable
        if (tenant.Limits?.AllowedFeatures?.Contains(feature) == true)
        {
            return true;
        }
        
        // Check if it's a built-in feature
        if (IsBuiltInFeature(feature))
        {
            return true;
        }
        
        return false;
    }
    
    public List<string> GetEnabledFeatures(Tenant tenant)
    {
        var features = new List<string>();
        
        if (tenant.Features != null)
        {
            foreach (var feature in tenant.Features)
            {
                if (IsFeatureAvailableForNewUsers(feature.Key, tenant))
                {
                    features.Add(feature.Key);
                }
            }
        }
        
        if (tenant.Limits?.AllowedFeatures != null)
        {
            features.AddRange(tenant.Limits.AllowedFeatures);
        }
        
        return features.Distinct().ToList();
    }
    
    // Add this if your IFeatureService interface requires it
    public bool IsBuiltInFeature(string feature)
    {
        // Check for common built-in features
        return feature switch
        {
            "dashboard" => true,
            "profile" => true,
            "settings" => true,
            "notifications" => true,
            "reports" => true,
            "api-access" => true,
            "sso" => true,
            "audit-logs" => true,
            "user-management" => true,
            "role-management" => true,
            _ => false
        };
    }
}