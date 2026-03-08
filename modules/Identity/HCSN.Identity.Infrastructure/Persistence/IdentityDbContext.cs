using Microsoft.EntityFrameworkCore;
using HCSN.Identity.Domain.Entities;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HCSN.Identity.Infrastructure.Persistence;

public class IdentityDbContext : DbContext
{
    public IdentityDbContext(DbContextOptions<IdentityDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<TenantModule> TenantModules => Set<TenantModule>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        static ValueComparer<T> BuildJsonValueComparer<T>() where T : class, new()
            => new(
                (left, right) => JsonSerializer.Serialize(left, (JsonSerializerOptions?)null) == JsonSerializer.Serialize(right, (JsonSerializerOptions?)null),
                value => JsonSerializer.Serialize(value, (JsonSerializerOptions?)null).GetHashCode(),
                value => JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(value, (JsonSerializerOptions?)null), (JsonSerializerOptions?)null) ?? new T());

        static PropertyBuilder<TProperty> ConfigureJsonProperty<TProperty>(
            PropertyBuilder<TProperty> property,
            JsonSerializerOptions options,
            ValueComparer<TProperty>? comparer = null)
            where TProperty : class, new()
        {
            property
                .HasColumnType("nvarchar(max)")
                .HasConversion(
                    value => JsonSerializer.Serialize(value, options),
                    value => JsonSerializer.Deserialize<TProperty>(value, options) ?? new TProperty());

            property.Metadata.SetValueComparer(comparer ?? BuildJsonValueComparer<TProperty>());
            return property;
        }

        // =========================
        // INVOICE (RELATIONAL)
        // =========================
        modelBuilder.Entity<Invoice>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.HasOne(e => e.Tenant)
                .WithMany(t => t.Invoices)
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // =========================
        // TENANT
        // =========================
        modelBuilder.Entity<Tenant>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.HasIndex(e => e.Subdomain).IsUnique();
            entity.HasIndex(e => e.CustomDomain);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.Type);
            entity.HasIndex(e => e.DeploymentModel);

            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Subdomain).IsRequired().HasMaxLength(100);
            entity.Property(e => e.CustomDomain).HasMaxLength(200);
            entity.Property(e => e.ConnectionString).HasMaxLength(500);
            entity.Property(e => e.Notes).HasMaxLength(1000);

            entity.Property(e => e.Type).HasConversion<int>();
            entity.Property(e => e.DeploymentModel).HasConversion<int>();
            entity.Property(e => e.Status).HasConversion<int>();

            // JSON properties (SIMPLE conversion, NO OwnsOne)
            ConfigureJsonProperty(entity.Property(e => e.Features), jsonOptions);
            ConfigureJsonProperty(entity.Property(e => e.Settings), jsonOptions);
            ConfigureJsonProperty(entity.Property(e => e.Branding), jsonOptions);
            ConfigureJsonProperty(entity.Property(e => e.SecurityPolicy), jsonOptions);
            ConfigureJsonProperty(entity.Property(e => e.Limits), jsonOptions);
            ConfigureJsonProperty(entity.Property(e => e.Billing), jsonOptions);
            ConfigureJsonProperty(entity.Property(e => e.AllowedDomains), jsonOptions);
            ConfigureJsonProperty(entity.Property(e => e.Metadata), jsonOptions);

            // Relationships
            entity.HasMany(e => e.Users)
                .WithOne(u => u.Tenant)
                .HasForeignKey(u => u.TenantId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(e => e.Modules)
                .WithOne()
                .HasForeignKey("TenantId")
                .OnDelete(DeleteBehavior.Cascade);
        });

        // =========================
        // USER
        // =========================
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.HasIndex(e => e.Email).IsUnique();
            entity.HasIndex(e => e.TenantId);
            entity.HasIndex(e => e.Status);

            entity.Property(e => e.Email).IsRequired().HasMaxLength(256);
            entity.Property(e => e.FirstName).HasMaxLength(100);
            entity.Property(e => e.LastName).HasMaxLength(100);

            entity.Property(e => e.Status).HasConversion<int>();
            entity.Property(e => e.UserType).HasConversion<int>();

            ConfigureJsonProperty(entity.Property(e => e.AccessibleSystems), jsonOptions);
            ConfigureJsonProperty(entity.Property(e => e.KnownDevices), jsonOptions);
            ConfigureJsonProperty(entity.Property(e => e.TwoFactorRecoveryCodes), jsonOptions);

            ConfigureJsonProperty(entity.Property<Dictionary<string, object>>("_customData"), jsonOptions)
                .HasColumnName("CustomData");
            ConfigureJsonProperty(entity.Property<Dictionary<string, string>>("_metadata"), jsonOptions)
                .HasColumnName("Metadata");
            ConfigureJsonProperty(entity.Property<List<string>>("_roles"), jsonOptions)
                .HasColumnName("Roles");
            ConfigureJsonProperty(entity.Property<List<string>>("_permissions"), jsonOptions)
                .HasColumnName("Permissions");
            ConfigureJsonProperty(entity.Property<List<string>>("_deviceTokens"), jsonOptions)
                .HasColumnName("DeviceTokens");

            entity.HasQueryFilter(e => e.DeletedAt == null);

            entity.HasOne(u => u.Tenant)
                .WithMany(t => t.Users)
                .HasForeignKey(u => u.TenantId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // =========================
        // TENANT MODULE
        // =========================
        modelBuilder.Entity<TenantModule>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.ModuleCode).IsRequired().HasMaxLength(100);
            entity.Property(e => e.ModuleName).IsRequired().HasMaxLength(200);

            ConfigureJsonProperty(entity.Property(e => e.Configuration), jsonOptions);
        });
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateTimestamps();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void UpdateTimestamps()
    {
        var entries = ChangeTracker.Entries()
            .Where(e => e.Entity is User || e.Entity is Tenant);

        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added)
            {
                if (entry.Entity is Tenant tenant)
                    tenant.GetType().GetProperty("CreatedAt")?.SetValue(tenant, DateTime.UtcNow);

                if (entry.Entity is User user)
                    user.GetType().GetProperty("CreatedAt")?.SetValue(user, DateTime.UtcNow);
            }

            if (entry.State == EntityState.Modified)
            {
                if (entry.Entity is Tenant tenant)
                    tenant.GetType().GetProperty("UpdatedAt")?.SetValue(tenant, DateTime.UtcNow);

                if (entry.Entity is User user)
                    user.GetType().GetProperty("UpdatedAt")?.SetValue(user, DateTime.UtcNow);
            }
        }
    }
}
