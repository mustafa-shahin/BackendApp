using Backend.CMS.Application.Interfaces.Services;
using Backend.CMS.Domain.Entities;
using Backend.CMS.Domain.Enums;
using Backend.CMS.Infrastructure.Data;
using Backend.CMS.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Backend.CMS.Infrastructure.Services
{
    public class VersioningService : IVersioningService
    {
        private readonly IRepository<DeploymentVersion> _deploymentRepository;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<VersioningService> _logger;

        public VersioningService(
            IRepository<DeploymentVersion> deploymentRepository,
            ApplicationDbContext context,
            ILogger<VersioningService> logger)
        {
            _deploymentRepository = deploymentRepository;
            _context = context;
            _logger = logger;
        }

        public async Task<DeploymentVersion> CreateDeploymentVersionAsync(string version, string releaseNotes, Dictionary<string, object> migrationData)
        {
            var deploymentVersion = new DeploymentVersion
            {
                Version = version,
                ReleaseNotes = releaseNotes,
                ReleasedAt = DateTime.UtcNow,
                Status = DeploymentStatus.Pending,
                MigrationData = migrationData,
                IsRollback = false
            };

            await _deploymentRepository.AddAsync(deploymentVersion);
            await _deploymentRepository.SaveChangesAsync();

            _logger.LogInformation("Created deployment version {Version}", version);
            return deploymentVersion;
        }

        public async Task<bool> DeployVersionAsync(Guid versionId, string deployedBy)
        {
            try
            {
                var version = await _deploymentRepository.GetByIdAsync(versionId);
                if (version == null)
                {
                    _logger.LogError("Deployment version {VersionId} not found", versionId);
                    return false;
                }

                if (version.Status != DeploymentStatus.Pending)
                {
                    _logger.LogError("Deployment version {VersionId} is not in pending status", versionId);
                    return false;
                }

                // Start deployment
                version.Status = DeploymentStatus.InProgress;
                version.DeployedBy = deployedBy;
                _deploymentRepository.Update(version);
                await _deploymentRepository.SaveChangesAsync();

                // Execute migration scripts based on migration data
                var success = await ExecuteMigrationAsync(version);

                if (success)
                {
                    version.Status = DeploymentStatus.Completed;
                    version.DeployedAt = DateTime.UtcNow;
                    _logger.LogInformation("Successfully deployed version {Version}", version.Version);
                }
                else
                {
                    version.Status = DeploymentStatus.Failed;
                    version.ErrorMessage = "Migration execution failed";
                    _logger.LogError("Failed to deploy version {Version}", version.Version);
                }

                _deploymentRepository.Update(version);
                await _deploymentRepository.SaveChangesAsync();

                return success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deploying version {VersionId}", versionId);

                var version = await _deploymentRepository.GetByIdAsync(versionId);
                if (version != null)
                {
                    version.Status = DeploymentStatus.Failed;
                    version.ErrorMessage = ex.Message;
                    _deploymentRepository.Update(version);
                    await _deploymentRepository.SaveChangesAsync();
                }

                return false;
            }
        }

        public async Task<bool> RollbackToVersionAsync(Guid versionId, string rolledBackBy)
        {
            try
            {
                var targetVersion = await _deploymentRepository.GetByIdAsync(versionId);
                if (targetVersion == null || targetVersion.Status != DeploymentStatus.Completed)
                {
                    return false;
                }

                var currentVersion = await GetCurrentVersionAsync();
                if (currentVersion == null)
                {
                    return false;
                }

                // Create rollback version entry
                var rollbackVersion = new DeploymentVersion
                {
                    Version = $"{targetVersion.Version}-rollback-{DateTime.UtcNow:yyyyMMddHHmmss}",
                    ReleaseNotes = $"Rollback to version {targetVersion.Version}",
                    ReleasedAt = DateTime.UtcNow,
                    Status = DeploymentStatus.InProgress,
                    IsRollback = true,
                    RollbackFromVersionId = currentVersion.Id,
                    DeployedBy = rolledBackBy,
                    MigrationData = targetVersion.MigrationData
                };

                await _deploymentRepository.AddAsync(rollbackVersion);
                await _deploymentRepository.SaveChangesAsync();

                // Execute rollback migration
                var success = await ExecuteRollbackMigrationAsync(rollbackVersion, targetVersion);

                if (success)
                {
                    rollbackVersion.Status = DeploymentStatus.Completed;
                    rollbackVersion.DeployedAt = DateTime.UtcNow;
                    currentVersion.Status = DeploymentStatus.RolledBack;

                    _deploymentRepository.Update(rollbackVersion);
                    _deploymentRepository.Update(currentVersion);

                    _logger.LogInformation("Successfully rolled back to version {Version}", targetVersion.Version);
                }
                else
                {
                    rollbackVersion.Status = DeploymentStatus.Failed;
                    rollbackVersion.ErrorMessage = "Rollback execution failed";
                    _deploymentRepository.Update(rollbackVersion);
                    _logger.LogError("Failed to rollback to version {Version}", targetVersion.Version);
                }

                await _deploymentRepository.SaveChangesAsync();
                return success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rolling back to version {VersionId}", versionId);
                return false;
            }
        }

        public async Task<DeploymentVersion?> GetCurrentVersionAsync()
        {
            var versions = await _deploymentRepository.FindAsync(v =>
                v.Status == DeploymentStatus.Completed && !v.IsRollback);

            return versions.OrderByDescending(v => v.DeployedAt).FirstOrDefault();
        }

        public async Task<List<DeploymentVersion>> GetVersionHistoryAsync(int page = 1, int pageSize = 20)
        {
            return (await _deploymentRepository.GetAllAsync())
                .OrderByDescending(v => v.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();
        }

        public async Task<bool> CanRollbackToVersionAsync(Guid versionId)
        {
            var version = await _deploymentRepository.GetByIdAsync(versionId);
            if (version == null || version.Status != DeploymentStatus.Completed)
            {
                return false;
            }

            var currentVersion = await GetCurrentVersionAsync();
            if (currentVersion == null || currentVersion.Id == versionId)
            {
                return false;
            }

            // Check if rollback data is available
            return version.MigrationData.ContainsKey("rollbackSupported") &&
                   (bool)version.MigrationData["rollbackSupported"];
        }

        public async Task<Dictionary<string, object>> GetVersionDifferencesAsync(Guid fromVersionId, Guid toVersionId)
        {
            var fromVersion = await _deploymentRepository.GetByIdAsync(fromVersionId);
            var toVersion = await _deploymentRepository.GetByIdAsync(toVersionId);

            if (fromVersion == null || toVersion == null)
            {
                return new Dictionary<string, object>();
            }

            return new Dictionary<string, object>
            {
                ["fromVersion"] = fromVersion.Version,
                ["toVersion"] = toVersion.Version,
                ["migrationChanges"] = CompareMigrationData(fromVersion.MigrationData, toVersion.MigrationData),
                ["releaseNotes"] = toVersion.ReleaseNotes
            };
        }

        private async Task<bool> ExecuteMigrationAsync(DeploymentVersion version)
        {
            try
            {
                // Execute database migrations if any
                if (version.MigrationData.ContainsKey("databaseMigrations"))
                {
                    var migrations = JsonSerializer.Deserialize<List<string>>(
                        version.MigrationData["databaseMigrations"].ToString() ?? "[]");

                    foreach (var migrationScript in migrations ?? new List<string>())
                    {
                        await _context.Database.ExecuteSqlRawAsync(migrationScript);
                    }
                }

                // Execute file system changes if any
                if (version.MigrationData.ContainsKey("fileSystemChanges"))
                {
                    await ExecuteFileSystemChangesAsync(version.MigrationData["fileSystemChanges"]);
                }

                // Execute configuration updates if any
                if (version.MigrationData.ContainsKey("configurationUpdates"))
                {
                    await ExecuteConfigurationUpdatesAsync(version.MigrationData["configurationUpdates"]);
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing migration for version {Version}", version.Version);
                return false;
            }
        }

        private async Task<bool> ExecuteRollbackMigrationAsync(DeploymentVersion rollbackVersion, DeploymentVersion targetVersion)
        {
            try
            {
                // Execute rollback scripts in reverse order
                if (rollbackVersion.MigrationData.ContainsKey("rollbackScripts"))
                {
                    var rollbackScripts = JsonSerializer.Deserialize<List<string>>(
                        rollbackVersion.MigrationData["rollbackScripts"].ToString() ?? "[]");

                    var scriptsToExecute = rollbackScripts?.AsEnumerable().Reverse().ToList() ?? new List<string>();

                    foreach (var script in scriptsToExecute)
                    {
                        await _context.Database.ExecuteSqlRawAsync(script);
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing rollback migration");
                return false;
            }
        }

        private async Task ExecuteFileSystemChangesAsync(object fileSystemChanges)
        {
            // Implementation for file system changes (copy files, update templates, etc.)
            await Task.CompletedTask;
        }

        private async Task ExecuteConfigurationUpdatesAsync(object configurationUpdates)
        {
            // Implementation for configuration updates
            await Task.CompletedTask;
        }

        private Dictionary<string, object> CompareMigrationData(
            Dictionary<string, object> fromData,
            Dictionary<string, object> toData)
        {
            var differences = new Dictionary<string, object>();

            foreach (var key in toData.Keys.Union(fromData.Keys))
            {
                if (!fromData.ContainsKey(key))
                {
                    differences[$"added_{key}"] = toData[key];
                }
                else if (!toData.ContainsKey(key))
                {
                    differences[$"removed_{key}"] = fromData[key];
                }
                else if (!fromData[key].Equals(toData[key]))
                {
                    differences[$"changed_{key}"] = new { from = fromData[key], to = toData[key] };
                }
            }

            return differences;
        }
    }
}