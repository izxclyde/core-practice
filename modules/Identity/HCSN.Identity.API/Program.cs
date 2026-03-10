using HCSN.Identity.API.Extensions;
using HCSN.Identity.API.Middleware;
using HCSN.Identity.Application;
using HCSN.Identity.Infrastructure;
using HCSN.Identity.Infrastructure.Persistence;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

// Add CORS policy
builder.Services.AddCors(options =>
{
    options.AddPolicy(
        "AllowFrontend",
        policy =>
        {
            policy
                .WithOrigins(
                    "http://localhost:5173", // Admin app
                    "http://localhost:5174", // Tenant-base app
                    "http://localhost:5175", // Future tenants
                    "http://localhost:5176",
                    "http://localhost:5177",
                    "http://localhost:5178",
                    "http://localhost:5179",
                    "http://localhost:5180"
                )
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials(); // Important for cookies/auth headers
        }
    );

    // For production with domains
    options.AddPolicy(
        "Production",
        policy =>
        {
            policy
                .WithOrigins(
                    "https://admin.hcnatividad.com",
                    "https://*.hcnatividad.com" // Note: Wildcards don't work with credentials
                )
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials();
        }
    );
});

// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerWithJwt(); // Use the extension method

// Add Application and Infrastructure
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

// Configure pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Use CORS - IMPORTANT: Must be called before other middleware
app.UseCors("AllowFrontend"); // Use the development CORS policy

app.UseHttpsRedirection();
app.UseTenantResolution(); // Add tenant resolution middleware
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Create/Migrate database on startup
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
    await dbContext.Database.MigrateAsync();
}

await app.RunAsync();
