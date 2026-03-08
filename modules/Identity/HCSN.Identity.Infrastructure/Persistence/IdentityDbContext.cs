using Microsoft.EntityFrameworkCore;
using HCSN.Identity.Domain.Entities;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.ChangeTracking;

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
            var stringListComparer = new ValueComparer<List<string>>(
                                    (c1, c2) => c1!.SequenceEqual(c2!),
                                    c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                                    c => c.ToList());

            entity.Property(e => e.Features)
                .HasColumnType("nvarchar(max)")
                .HasConversion(
                    v => JsonSerializer.Serialize(v, jsonOptions),
                    v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, jsonOptions)
                         ?? new Dictionary<string, object>());

            entity.Property(e => e.Settings)
                .HasColumnType("nvarchar(max)")
                .HasConversion(
                    v => JsonSerializer.Serialize(v, jsonOptions),
                    v => JsonSerializer.Deserialize<TenantSettings>(v, jsonOptions)!);

            entity.Property(e => e.Branding)
                .HasColumnType("nvarchar(max)")
                .HasConversion(
                    v => JsonSerializer.Serialize(v, jsonOptions),
                    v => JsonSerializer.Deserialize<TenantBranding>(v, jsonOptions)!);

            entity.Property(e => e.SecurityPolicy)
                .HasColumnType("nvarchar(max)")
                .HasConversion(
                    v => JsonSerializer.Serialize(v, jsonOptions),
                    v => JsonSerializer.Deserialize<SecurityPolicy>(v, jsonOptions)!);

            entity.Property(e => e.Limits)
                .HasColumnType("nvarchar(max)")
                .HasConversion(
                    v => JsonSerializer.Serialize(v, jsonOptions),
                    v => JsonSerializer.Deserialize<TenantLimits>(v, jsonOptions)!);

            entity.Property(e => e.Billing)
                .HasColumnType("nvarchar(max)")
                .HasConversion(
                    v => JsonSerializer.Serialize(v, jsonOptions),
                    v => JsonSerializer.Deserialize<BillingInfo>(v, jsonOptions)!);

            entity.Property(e => e.AllowedDomains)
                .HasColumnType("nvarchar(max)")
                .HasConversion(
                    v => JsonSerializer.Serialize(v, jsonOptions),
                    v => JsonSerializer.Deserialize<List<string>>(v, jsonOptions) ?? new List<string>())
                .Metadata.SetValueComparer(stringListComparer);

            entity.Property(e => e.Metadata)
                .HasColumnType("nvarchar(max)")
                .HasConversion(
                    v => JsonSerializer.Serialize(v, jsonOptions),
                    v => JsonSerializer.Deserialize<Dictionary<string, string>>(v, jsonOptions)
                         ?? new Dictionary<string, string>());

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

            entity.Property(e => e.Configuration)
                .HasColumnType("nvarchar(max)")
                .HasConversion(
                    v => JsonSerializer.Serialize(v, jsonOptions),
                    v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, jsonOptions)
                         ?? new Dictionary<string, object>());
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