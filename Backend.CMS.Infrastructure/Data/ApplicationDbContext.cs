using Backend.CMS.Domain.Common;
using Backend.CMS.Domain.Entities;
using Backend.CMS.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System.Text.Json;

namespace Backend.CMS.Infrastructure.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<UserSession> UserSessions { get; set; }
        public DbSet<PasswordResetToken> PasswordResetTokens { get; set; }
        public DbSet<Page> Pages { get; set; }
        public DbSet<PageComponent> PageComponents { get; set; }
        public DbSet<PageVersion> PageVersions { get; set; }
        public DbSet<Company> Companies { get; set; }
        public DbSet<Location> Locations { get; set; }
        public DbSet<LocationOpeningHour> LocationOpeningHours { get; set; }
        public DbSet<ComponentTemplate> ComponentTemplates { get; set; }
        public DbSet<DeploymentVersion> DeploymentVersions { get; set; }
        public DbSet<TemplateSyncLog> TemplateSyncLogs { get; set; }
        public DbSet<DeploymentJob> DeploymentJobs { get; set; }
        public DbSet<TemplateSyncJob> TemplateSyncJobs { get; set; }
        public DbSet<DeploymentProposal> DeploymentProposals { get; set; }
        public DbSet<TemplateUpdateProposal> TemplateUpdateProposals { get; set; }

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
                entity.HasIndex(e => e.Email).IsUnique();
                entity.HasIndex(e => e.Username).IsUnique();
                entity.Property(e => e.Email).HasMaxLength(256);
                entity.Property(e => e.Username).HasMaxLength(256);
                entity.Property(e => e.FirstName).HasMaxLength(100);
                entity.Property(e => e.LastName).HasMaxLength(100);
                entity.Property(e => e.Phone).HasMaxLength(50);
                entity.Property(e => e.BillingAddress).HasMaxLength(500);
                entity.Property(e => e.BillingCity).HasMaxLength(100);
                entity.Property(e => e.BillingState).HasMaxLength(100);
                entity.Property(e => e.BillingCountry).HasMaxLength(100);
                entity.Property(e => e.BillingPostalCode).HasMaxLength(20);
                entity.Property(e => e.ShippingAddress).HasMaxLength(500);
                entity.Property(e => e.ShippingCity).HasMaxLength(100);
                entity.Property(e => e.ShippingState).HasMaxLength(100);
                entity.Property(e => e.ShippingCountry).HasMaxLength(100);
                entity.Property(e => e.ShippingPostalCode).HasMaxLength(20);
                entity.Property(e => e.Gender).HasMaxLength(20);
                entity.Property(e => e.Preferences).HasConversion(dictionaryConverter);
                entity.Property(e => e.RecoveryCodes).HasConversion(listConverter);
                entity.Property(e => e.Role).HasConversion<string>();

                entity.HasMany(e => e.Sessions)
                    .WithOne(e => e.User)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(e => e.PasswordResetTokens)
                    .WithOne(e => e.User)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // PasswordResetToken configuration
            modelBuilder.Entity<PasswordResetToken>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Token).IsUnique();
                entity.Property(e => e.Token).HasMaxLength(500);
                entity.Property(e => e.IpAddress).HasMaxLength(45);
                entity.Property(e => e.UserAgent).HasMaxLength(500);
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
                entity.HasIndex(e => e.Slug).IsUnique();
                entity.Property(e => e.Name).HasMaxLength(200);
                entity.Property(e => e.Title).HasMaxLength(200);
                entity.Property(e => e.Slug).HasMaxLength(200);
                entity.Property(e => e.MetaTitle).HasMaxLength(200);
                entity.Property(e => e.MetaDescription).HasMaxLength(500);
                entity.Property(e => e.Status).HasConversion<string>();
                entity.Property(e => e.AccessLevel).HasConversion<string>();

                entity.HasOne(e => e.ParentPage)
                    .WithMany(e => e.ChildPages)
                    .HasForeignKey(e => e.ParentPageId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasMany(e => e.Components)
                    .WithOne(e => e.Page)
                    .HasForeignKey(e => e.PageId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // PageComponent configuration
            modelBuilder.Entity<PageComponent>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).HasMaxLength(200);
                entity.Property(e => e.Type).HasConversion<string>();
                entity.Property(e => e.Properties).HasConversion(dictionaryConverter);
                entity.Property(e => e.Styles).HasConversion(dictionaryConverter);
                entity.Property(e => e.Content).HasConversion(dictionaryConverter);
                entity.Property(e => e.ResponsiveSettings).HasConversion(dictionaryConverter);
                entity.Property(e => e.AnimationSettings).HasConversion(dictionaryConverter);
                entity.Property(e => e.InteractionSettings).HasConversion(dictionaryConverter);

                entity.HasOne(e => e.ParentComponent)
                    .WithMany(e => e.ChildComponents)
                    .HasForeignKey(e => e.ParentComponentId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // PageVersion configuration
            modelBuilder.Entity<PageVersion>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => new { e.PageId, e.VersionNumber }).IsUnique();

                entity.HasOne(e => e.Page)
                    .WithMany()
                    .HasForeignKey(e => e.PageId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.CreatedByUser)
                    .WithMany()
                    .HasForeignKey(e => e.CreatedByUserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Company configuration
            modelBuilder.Entity<Company>(entity =>
            {
                entity.HasKey(e => e.Id);
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
                    .HasForeignKey(e => e.CompanyId)
                    .OnDelete(DeleteBehavior.Cascade);
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
                    .HasForeignKey(e => e.LocationId)
                    .OnDelete(DeleteBehavior.Cascade);
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
                entity.HasIndex(e => e.Name).IsUnique();
                entity.Property(e => e.Name).HasMaxLength(200);
                entity.Property(e => e.DisplayName).HasMaxLength(200);
                entity.Property(e => e.Category).HasMaxLength(100);
                entity.Property(e => e.Icon).HasMaxLength(100);
                entity.Property(e => e.Type).HasConversion<string>();
                entity.Property(e => e.DefaultProperties).HasConversion(dictionaryConverter);
                entity.Property(e => e.DefaultStyles).HasConversion(dictionaryConverter);
                entity.Property(e => e.Schema).HasConversion(dictionaryConverter);
                entity.Property(e => e.ConfigSchema).HasConversion(dictionaryConverter);
            });

            // DeploymentVersion configuration
            modelBuilder.Entity<DeploymentVersion>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Version);
                entity.Property(e => e.Version).HasMaxLength(50);
                entity.Property(e => e.DeployedBy).HasMaxLength(256);
                entity.Property(e => e.Status).HasConversion<string>();
                entity.Property(e => e.MigrationData).HasConversion(dictionaryConverter);
                entity.Property(e => e.DeploymentMetadata).HasConversion(dictionaryConverter);

                entity.HasOne(e => e.RollbackFromVersion)
                    .WithMany()
                    .HasForeignKey(e => e.RollbackFromVersionId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // TemplateSyncLog configuration
            modelBuilder.Entity<TemplateSyncLog>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.MasterTemplateVersion).HasMaxLength(50);
                entity.Property(e => e.PreviousVersion).HasMaxLength(50);
                entity.Property(e => e.SyncedBy).HasMaxLength(256);
                entity.Property(e => e.Status).HasConversion<string>();
                entity.Property(e => e.FilesUpdated).HasConversion(listConverter);
                entity.Property(e => e.FilesAdded).HasConversion(listConverter);
                entity.Property(e => e.FilesDeleted).HasConversion(listConverter);
                entity.Property(e => e.ConflictResolutions).HasConversion(dictionaryConverter);
                entity.Property(e => e.SyncMetadata).HasConversion(dictionaryConverter);
            });

            // DeploymentJob configuration
            modelBuilder.Entity<DeploymentJob>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.JobId).IsUnique();
                entity.Property(e => e.JobId).HasMaxLength(100);
                entity.Property(e => e.Version).HasMaxLength(50);
                entity.Property(e => e.ScheduledBy).HasMaxLength(256);
                entity.Property(e => e.ExecutedBy).HasMaxLength(256);
                entity.Property(e => e.Status).HasConversion<string>();
                entity.Property(e => e.MigrationData).HasConversion(dictionaryConverter);
                entity.Property(e => e.JobMetadata).HasConversion(dictionaryConverter);
                entity.Property(e => e.FailedTenants).HasConversion(listConverter);
            });

            // TemplateSyncJob configuration
            modelBuilder.Entity<TemplateSyncJob>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.JobId).IsUnique();
                entity.Property(e => e.JobId).HasMaxLength(100);
                entity.Property(e => e.MasterTemplateVersion).HasMaxLength(50);
                entity.Property(e => e.PreviousVersion).HasMaxLength(50);
                entity.Property(e => e.ScheduledBy).HasMaxLength(256);
                entity.Property(e => e.ExecutedBy).HasMaxLength(256);
                entity.Property(e => e.Status).HasConversion<string>();
                entity.Property(e => e.JobMetadata).HasConversion(dictionaryConverter);
                entity.Property(e => e.FailedTenants).HasConversion(listConverter);
                entity.Property(e => e.ConflictResolutions).HasConversion(dictionaryConverter);
            });

            // DeploymentProposal configuration
            modelBuilder.Entity<DeploymentProposal>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Version).HasMaxLength(50);
                entity.Property(e => e.ProposedBy).HasMaxLength(256);
                entity.Property(e => e.ReviewedBy).HasMaxLength(256);
                entity.Property(e => e.Status).HasConversion<string>();
                entity.Property(e => e.MigrationData).HasConversion(dictionaryConverter);
                entity.Property(e => e.ImpactAnalysis).HasConversion(dictionaryConverter);
                entity.Property(e => e.RollbackPlan).HasConversion(dictionaryConverter);
                entity.Property(e => e.AffectedTenants).HasConversion(listConverter);
                entity.Property(e => e.PrerequisiteChecks).HasConversion(listConverter);
            });

            // TemplateUpdateProposal configuration
            modelBuilder.Entity<TemplateUpdateProposal>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.MasterTemplateVersion).HasMaxLength(50);
                entity.Property(e => e.PreviousVersion).HasMaxLength(50);
                entity.Property(e => e.DetectedBy).HasMaxLength(256);
                entity.Property(e => e.ReviewedBy).HasMaxLength(256);
                entity.Property(e => e.Status).HasConversion<string>();
                entity.Property(e => e.ChangedFiles).HasConversion(listConverter);
                entity.Property(e => e.AddedFiles).HasConversion(listConverter);
                entity.Property(e => e.DeletedFiles).HasConversion(listConverter);
                entity.Property(e => e.BreakingChanges).HasConversion(listConverter);
                entity.Property(e => e.ConflictAnalysis).HasConversion(dictionaryConverter);
                entity.Property(e => e.AffectedTenants).HasConversion(listConverter);
                entity.Property(e => e.ImpactAnalysis).HasConversion(dictionaryConverter);
            });

            // Set value comparers for collections to avoid EF warnings
            SetValueComparers(modelBuilder);
        }

        private void SetValueComparers(ModelBuilder modelBuilder)
        {
            var dictionaryComparer = new ValueComparer<Dictionary<string, object>>(
                (c1, c2) => c1!.SequenceEqual(c2!),
                c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                c => c.ToDictionary(k => k.Key, k => k.Value));

            var listComparer = new ValueComparer<List<string>>(
                (c1, c2) => c1!.SequenceEqual(c2!),
                c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                c => c.ToList());

            // Apply value comparers to dictionary properties
            var entities = new[]
            {
                typeof(Company), typeof(ComponentTemplate), typeof(DeploymentJob),
                typeof(DeploymentProposal), typeof(DeploymentVersion), typeof(Location),
                typeof(PageComponent), typeof(TemplateSyncJob), typeof(TemplateSyncLog),
                typeof(TemplateUpdateProposal), typeof(User)
            };

            foreach (var entityType in entities)
            {
                var entity = modelBuilder.Entity(entityType);
                var properties = entityType.GetProperties()
                    .Where(p => p.PropertyType == typeof(Dictionary<string, object>));

                foreach (var property in properties)
                {
                    entity.Property(property.Name).Metadata.SetValueComparer(dictionaryComparer);
                }

                var listProperties = entityType.GetProperties()
                    .Where(p => p.PropertyType == typeof(List<string>));

                foreach (var property in listProperties)
                {
                    entity.Property(property.Name).Metadata.SetValueComparer(listComparer);
                }
            }
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
                }
                else if (entry.State == EntityState.Modified)
                {
                    entity.UpdatedAt = now;
                }
            }
        }
    }
}