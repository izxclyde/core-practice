using System;
using System.Threading.Tasks;
using HCSN.Identity.Domain.Entities;
using HCSN.Identity.Domain.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace HCSN.Identity.API.Middleware;

public class TenantResolutionMiddleware
{
    private readonly RequestDelegate _next;

    public TenantResolutionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(
        HttpContext context,
        ITenantRepository tenantRepository,
        IHostEnvironment environment
    )
    {
        var isLocalhost = IsLocalhost(context) || environment.IsDevelopment();
        Tenant? tenant = null;

        // Method 1: For production - from subdomain
        if (!isLocalhost)
        {
            var host = context.Request.Host.Host;
            var subdomain = ExtractSubdomain(host);

            if (!string.IsNullOrEmpty(subdomain) && subdomain != "www" && subdomain != "hcsn")
            {
                tenant = await tenantRepository.GetBySubdomainAsync(subdomain);
                if (tenant != null)
                {
                    SetTenantContext(context, tenant);
                }
            }
        }

        // Method 2: For localhost - from path
        if (isLocalhost && tenant == null)
        {
            tenant = await GetTenantFromPathAsync(context, tenantRepository);
            if (tenant != null)
            {
                SetTenantContext(context, tenant);
            }
        }

        // Method 3: From header (for API clients) - works everywhere
        if (tenant == null)
        {
            var tenantHeader = context.Request.Headers["X-Tenant-ID"].FirstOrDefault();
            if (
                !string.IsNullOrEmpty(tenantHeader) && Guid.TryParse(tenantHeader, out var tenantId)
            )
            {
                var headerTenant = await tenantRepository.GetByIdAsync(tenantId);
                if (headerTenant != null)
                {
                    SetTenantContext(context, headerTenant);
                    tenant = headerTenant;
                }
            }
        }

        // Method 4: From query string (works everywhere)
        if (tenant == null && context.Request.Query.TryGetValue("tenant", out var tenantQuery))
        {
            var queryTenant = await tenantRepository.GetBySubdomainAsync(tenantQuery.ToString());
            if (queryTenant != null)
            {
                SetTenantContext(context, queryTenant);
            }
        }

        await _next(context);
    }

    private bool IsLocalhost(HttpContext context)
    {
        var host = context.Request.Host.Host;
        return host == "localhost" || host == "127.0.0.1" || host == "::1";
    }

    private string? ExtractSubdomain(string host)
    {
        // Remove port if present
        if (host.Contains(':'))
        {
            host = host.Split(':')[0];
        }

        // Handle localhost test domains like acme.localhost
        if (host.EndsWith(".localhost"))
        {
            return host.Replace(".localhost", "");
        }

        // Handle production domains like acme.hcsn.com
        var parts = host.Split('.');
        if (parts.Length >= 3) // subdomain.domain.tld
        {
            return parts[0];
        }

        return null;
    }

    private async Task<Tenant?> GetTenantFromPathAsync(
        HttpContext context,
        ITenantRepository tenantRepository
    )
    {
        var path = context.Request.Path.Value ?? "";
        var pathSegments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);

        // Pattern 1: /register/{tenant}
        if (pathSegments.Length >= 2 && pathSegments[0].ToLower() == "register")
        {
            return await tenantRepository.GetBySubdomainAsync(pathSegments[1]);
        }

        // Pattern 2: /api/tenant/{tenant}/...
        if (pathSegments.Length >= 3 && pathSegments[0].ToLower() == "api")
        {
            for (int i = 1; i < pathSegments.Length - 1; i++)
            {
                if (pathSegments[i].ToLower() == "tenant" || pathSegments[i].ToLower() == "tenants")
                {
                    return await tenantRepository.GetBySubdomainAsync(pathSegments[i + 1]);
                }
            }
        }

        // Pattern 3: /{tenant}/... (first segment is tenant)
        if (
            pathSegments.Length >= 1
            && pathSegments[0] != "api"
            && pathSegments[0] != "register"
            && pathSegments[0] != "swagger"
            && pathSegments[0] != "health"
        )
        {
            // Try to treat first segment as tenant if it's not a reserved path
            return await tenantRepository.GetBySubdomainAsync(pathSegments[0]);
        }

        return null;
    }

    private void SetTenantContext(HttpContext context, Tenant tenant)
    {
        context.Items["Tenant"] = tenant;
        context.Items["TenantId"] = tenant.Id;
        context.Items["TenantSubdomain"] = tenant.Subdomain;
    }
}

public static class TenantResolutionMiddlewareExtensions
{
    public static IApplicationBuilder UseTenantResolution(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<TenantResolutionMiddleware>();
    }
}
