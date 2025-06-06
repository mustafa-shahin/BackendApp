// File: Backend.CMS.Infrastructure/Data/ApplicationDbContext.cs
using Backend.CMS.Domain.Common;
using Backend.CMS.Domain.Common.Interfaces;
using Backend.CMS.Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System.Data;
using System.Security;
using System.Text.Json;

namespace Backend.CMS.Infrastructure.Data
{
    public class ApplicationDbContext : DbContext
    {
        private readonly string _tenantId;

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, ITenantProvider tenantProvider)
            : base(options)
        {
            _tenantId = tenantProvider.GetTenantId();
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<Permission> Permissions { get; set; }
        public DbSet<UserRole> UserRoles { get; set; }
        public DbSet<RolePermission> RolePermissions { get; set; }
        public DbSet<UserSession> UserSessions { get; set; }
        public DbSet<Page> Pages { get; set; }
        public DbSet<PageComponent> PageComponents { get; set; }
        public DbSet<PagePermission> PagePermissions { get; set; }
        public DbSet<PageVersion> PageVersions { get; set; }
        public DbSet<Company> Companies { get; set; }
        public DbSet<Location> Locations { get; set; }
        public DbSet<LocationOpeningHour> LocationOpeningHours { get; set; }
        public DbSet<ComponentTemplate> ComponentTemplates { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure value converters for Dictionary properties
            var dictionaryConverter = new ValueConverter<Dictionary<string, object>, string>(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, (JsonSerializerOptions?)null) ?? new Dictionary<string, object>());

            var listConverter = new ValueConverter<List<string>, string>(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>());

            // User configuration
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => new { e.TenantId, e.Email }).IsUnique();
                entity.HasIndex(e => new { e.TenantId, e.Username }).IsUnique();
                entity.Property(e => e.Email).HasMaxLength(256);
                entity.Property(e => e.Username).HasMaxLength(256);
                entity.Property(e => e.FirstName).HasMaxLength(100);
                entity.Property(e => e.LastName).HasMaxLength(100);
                entity.Property(e => e.Preferences).HasConversion(dictionaryConverter);
                entity.Property(e => e.RecoveryCodes).HasConversion(listConverter);

                entity.HasMany(e => e.UserRoles)
                    .WithOne(e => e.User)
                    .HasForeignKey(e => e.UserId);

                entity.HasMany(e => e.Sessions)
                    .WithOne(e => e.User)
                    .HasForeignKey(e => e.UserId);
            });

            // Role configuration
            modelBuilder.Entity<Role>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => new { e.TenantId, e.NormalizedName }).IsUnique();
                entity.Property(e => e.Name).HasMaxLength(256);
                entity.Property(e => e.NormalizedName).HasMaxLength(256);

                entity.HasMany(e => e.UserRoles)
                    .WithOne(e => e.Role)
                    .HasForeignKey(e => e.RoleId);

                entity.HasMany(e => e.RolePermissions)
                    .WithOne(e => e.Role)
                    .HasForeignKey(e => e.RoleId);
            });

            // Permission configuration
            modelBuilder.Entity<Permission>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => new { e.Resource, e.Action }).IsUnique();
                entity.Property(e => e.Name).HasMaxLength(256);
                entity.Property(e => e.Resource).HasMaxLength(100);
                entity.Property(e => e.Action).HasMaxLength(100);

                entity.HasMany(e => e.RolePermissions)
                    .WithOne(e => e.Permission)
                    .HasForeignKey(e => e.PermissionId);
            });

            // UserRole configuration
            modelBuilder.Entity<UserRole>(entity =>
            {
                entity.HasKey(e => new { e.UserId, e.RoleId });
            });

            // RolePermission configuration
            modelBuilder.Entity<RolePermission>(entity =>
            {
                entity.HasKey(e => new { e.RoleId, e.PermissionId });
            });

            // UserSession configuration
            modelBuilder.Entity<UserSession>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.RefreshToken).IsUnique();
                entity.Property(e => e.RefreshToken).HasMaxLength(500);
                entity.Property(e => e.IpAddress).HasMaxLength(45);
                entity.Property(e => e.UserAgent).HasMaxLength(500);
            });

            // Page configuration
            modelBuilder.Entity<Page>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => new { e.TenantId, e.Slug }).IsUnique();
                entity.Property(e => e.Name).HasMaxLength(200);
                entity.Property(e => e.Title).HasMaxLength(200);
                entity.Property(e => e.Slug).HasMaxLength(200);
                entity.Property(e => e.MetaTitle).HasMaxLength(200);
                entity.Property(e => e.MetaDescription).HasMaxLength(500);

                entity.HasOne(e => e.ParentPage)
                    .WithMany(e => e.ChildPages)
                    .HasForeignKey(e => e.ParentPageId);

                entity.HasMany(e => e.Components)
                    .WithOne(e => e.Page)
                    .HasForeignKey(e => e.PageId);

                entity.HasMany(e => e.Permissions)
                    .WithOne(e => e.Page)
                    .HasForeignKey(e => e.PageId);
            });

            // PageComponent configuration
            modelBuilder.Entity<PageComponent>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).HasMaxLength(200);
                entity.Property(e => e.Properties).HasConversion(dictionaryConverter);
                entity.Property(e => e.Styles).HasConversion(dictionaryConverter);
                entity.Property(e => e.Content).HasConversion(dictionaryConverter);
                entity.Property(e => e.ResponsiveSettings).HasConversion(dictionaryConverter);
                entity.Property(e => e.AnimationSettings).HasConversion(dictionaryConverter);
                entity.Property(e => e.InteractionSettings).HasConversion(dictionaryConverter);

                entity.HasOne(e => e.ParentComponent)
                    .WithMany(e => e.ChildComponents)
                    .HasForeignKey(e => e.ParentComponentId);
            });

            // PagePermission configuration
            modelBuilder.Entity<PagePermission>(entity =>
            {
                entity.HasKey(e => new { e.PageId, e.RoleId });
            });

            // PageVersion configuration - FIXED
            modelBuilder.Entity<PageVersion>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => new { e.PageId, e.VersionNumber }).IsUnique();

                entity.HasOne(e => e.Page)
                    .WithMany()
                    .HasForeignKey(e => e.PageId);

                // Fix: Use CreatedByUserId instead of CreatedBy for the foreign key
                entity.HasOne(e => e.CreatedByUser)
                    .WithMany()
                    .HasForeignKey(e => e.CreatedByUserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Company configuration
            modelBuilder.Entity<Company>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.TenantId).IsUnique();
                entity.Property(e => e.Name).HasMaxLength(200);
                entity.Property(e => e.Email).HasMaxLength(256);
                entity.Property(e => e.Phone).HasMaxLength(50);
                entity.Property(e => e.Website).HasMaxLength(500);
                entity.Property(e => e.BrandingSettings).HasConversion(dictionaryConverter);
                entity.Property(e => e.SocialMediaLinks).HasConversion(dictionaryConverter);
                entity.Property(e => e.ContactInfo).HasConversion(dictionaryConverter);
                entity.Property(e => e.BusinessSettings).HasConversion(dictionaryConverter);

                entity.HasMany(e => e.Locations)
                    .WithOne(e => e.Company)
                    .HasForeignKey(e => e.CompanyId);
            });

            // Location configuration
            modelBuilder.Entity<Location>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).HasMaxLength(200);
                entity.Property(e => e.Address).HasMaxLength(500);
                entity.Property(e => e.City).HasMaxLength(100);
                entity.Property(e => e.State).HasMaxLength(100);
                entity.Property(e => e.Country).HasMaxLength(100);
                entity.Property(e => e.PostalCode).HasMaxLength(20);
                entity.Property(e => e.Phone).HasMaxLength(50);
                entity.Property(e => e.Email).HasMaxLength(256);
                entity.Property(e => e.Website).HasMaxLength(500);
                entity.Property(e => e.Latitude).HasPrecision(10, 8);
                entity.Property(e => e.Longitude).HasPrecision(11, 8);
                entity.Property(e => e.AdditionalInfo).HasConversion(dictionaryConverter);

                entity.HasMany(e => e.OpeningHours)
                    .WithOne(e => e.Location)
                    .HasForeignKey(e => e.LocationId);
            });

            // LocationOpeningHour configuration
            modelBuilder.Entity<LocationOpeningHour>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => new { e.LocationId, e.DayOfWeek }).IsUnique();
            });

            // ComponentTemplate configuration
            modelBuilder.Entity<ComponentTemplate>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => new { e.TenantId, e.Name }).IsUnique();
                entity.Property(e => e.Name).HasMaxLength(200);
                entity.Property(e => e.DisplayName).HasMaxLength(200);
                entity.Property(e => e.Category).HasMaxLength(100);
                entity.Property(e => e.Icon).HasMaxLength(100);
                entity.Property(e => e.DefaultProperties).HasConversion(dictionaryConverter);
                entity.Property(e => e.DefaultStyles).HasConversion(dictionaryConverter);
                entity.Property(e => e.Schema).HasConversion(dictionaryConverter);
                entity.Property(e => e.ConfigSchema).HasConversion(dictionaryConverter);
            });

            // Global query filters for tenant isolation
            modelBuilder.Entity<User>().HasQueryFilter(e => e.TenantId == _tenantId);
            modelBuilder.Entity<Role>().HasQueryFilter(e => e.TenantId == _tenantId);
            modelBuilder.Entity<Page>().HasQueryFilter(e => e.TenantId == _tenantId);
            modelBuilder.Entity<Company>().HasQueryFilter(e => e.TenantId == _tenantId);
            modelBuilder.Entity<Location>().HasQueryFilter(e => e.TenantId == _tenantId);
            modelBuilder.Entity<ComponentTemplate>().HasQueryFilter(e => e.TenantId == _tenantId);
        }

        public override int SaveChanges()
        {
            UpdateAuditFields();
            return base.SaveChanges();
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            UpdateAuditFields();
            return base.SaveChangesAsync(cancellationToken);
        }

        private void UpdateAuditFields()
        {
            var entries = ChangeTracker.Entries()
                .Where(e => e.Entity is BaseEntity && (e.State == EntityState.Added || e.State == EntityState.Modified));

            foreach (var entry in entries)
            {
                var entity = (BaseEntity)entry.Entity;
                var now = DateTime.UtcNow;

                if (entry.State == EntityState.Added)
                {
                    entity.CreatedAt = now;
                    entity.UpdatedAt = now;

                    if (entity is ITenantEntity tenantEntity && string.IsNullOrEmpty(tenantEntity.TenantId))
                    {
                        tenantEntity.TenantId = _tenantId;
                    }
                }
                else if (entry.State == EntityState.Modified)
                {
                    entity.UpdatedAt = now;
                }
            }
        }
    }

    public interface ITenantProvider
    {
        string GetTenantId();
    }

    public class TenantProvider : ITenantProvider
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public TenantProvider(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public string GetTenantId()
        {
            // Get tenant from HTTP context, configuration, or other source
            var tenantId = _httpContextAccessor.HttpContext?.Request.Headers["X-Tenant-Id"].FirstOrDefault();
            return tenantId ?? "default";
        }
    }
}