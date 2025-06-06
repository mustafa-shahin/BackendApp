using Backend.CMS.Application.Interfaces.Services;
using Backend.CMS.Domain.Entities;
using Backend.CMS.Domain.Enums;
using Backend.CMS.Infrastructure.Data;
using Backend.CMS.Infrastructure.Repositories;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Backend.CMS.Infrastructure.Services
{
    public class TemplateSyncJobService : ITemplateSyncJobService
    {
        private readonly IRepository<TemplateSyncJob> _templateSyncJobRepository;
        private readonly IRepository<TemplateUpdateProposal> _templateUpdateProposalRepository;
        private readonly IRepository<TenantRegistry> _tenantRegistryRepository;
        private readonly IConfiguration _configuration;
        private readonly ILogger<TemplateSyncJobService> _logger;
        private readonly IBackgroundJobClient _backgroundJobClient;

        public TemplateSyncJobService(
            IRepository<TemplateSyncJob> templateSyncJobRepository,
            IRepository<TemplateUpdateProposal> templateUpdateProposalRepository,
            IRepository<TenantRegistry> tenantRegistryRepository,
            IConfiguration configuration,
            ILogger<TemplateSyncJobService> logger,
            IBackgroundJobClient backgroundJobClient)
        {
            _templateSyncJobRepository = templateSyncJobRepository;
            _templateUpdateProposalRepository = templateUpdateProposalRepository;
            _tenantRegistryRepository = tenantRegistryRepository;
            _configuration = configuration;
            _logger = logger;
            _backgroundJobClient = backgroundJobClient;
        }

        public async Task<TemplateUpdateDetection> DetectTemplateUpdatesAsync()
        {
            try
            {
                // Simulate template update detection logic
                var currentVersion = "1.0.0";
                var latestVersion = "1.1.0";
                var availableVersions = new List<string> { "1.0.0", "1.0.1", "1.1.0" };

                return new TemplateUpdateDetection
                {
                    CurrentVersion = currentVersion,
                    LatestVersion = latestVersion,
                    UpdatesAvailable = currentVersion != latestVersion,
                    AvailableVersions = availableVersions,
                    LastChecked = DateTime.UtcNow,
                    VersionComparison = new Dictionary<string, object>
                    {
                        { "hasUpdates", currentVersion != latestVersion },
                        { "versionCount", availableVersions.Count }
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error detecting template updates");
                throw;
            }
        }

        public async Task<TemplateUpdatePreview> PreviewTemplateUpdatesAsync(string masterTemplateVersion)
        {
            try
            {
                // Simulate template preview logic
                return new TemplateUpdatePreview
                {
                    Version = masterTemplateVersion,
                    ChangedFiles = new List<string> { "template.html", "styles.css" },
                    AddedFiles = new List<string> { "new-component.html" },
                    DeletedFiles = new List<string> { "old-component.html" },
                    BreakingChanges = new List<string>(),
                    PotentialConflicts = new List<ConflictWarning>(),
                    ImpactAnalysis = new Dictionary<string, object>
                    {
                        { "affectedTenants", 10 },
                        { "riskLevel", "Low" }
                    },
                    RequiresManualReview = false
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error previewing template updates for version {Version}", masterTemplateVersion);
                throw;
            }
        }

        public async Task<string> ApproveAndScheduleGlobalTemplateSyncAsync(string masterTemplateVersion, string approvedBy, DateTime? scheduledTime = null)
        {
            var jobId = Guid.NewGuid().ToString();
            var activeTenants = await _tenantRegistryRepository.FindAsync(t => t.IsActive && t.AutoSyncEnabled);

            var templateSyncJob = new TemplateSyncJob
            {
                JobId = jobId,
                MasterTemplateVersion = masterTemplateVersion,
                PreviousVersion = "1.0.0", // This should be determined dynamically
                Status = JobStatus.Scheduled,
                ScheduledAt = scheduledTime ?? DateTime.UtcNow,
                ScheduledBy = approvedBy,
                IsGlobalJob = true,
                TotalTenants = activeTenants.Count()
            };

            await _templateSyncJobRepository.AddAsync(templateSyncJob);
            await _templateSyncJobRepository.SaveChangesAsync();

            // Schedule the job
            var hangfireJobId = scheduledTime.HasValue
                ? _backgroundJobClient.Schedule(() => ExecuteGlobalTemplateSyncAsync(jobId), scheduledTime.Value)
                : _backgroundJobClient.Enqueue(() => ExecuteGlobalTemplateSyncAsync(jobId));

            _logger.LogInformation("Scheduled global template sync job {JobId} for version {Version}", jobId, masterTemplateVersion);
            return jobId;
        }

        public async Task<string> ScheduleTenantTemplateSyncAsync(string tenantId, string masterTemplateVersion, string scheduledBy, DateTime? scheduledTime = null)
        {
            var jobId = Guid.NewGuid().ToString();

            var templateSyncJob = new TemplateSyncJob
            {
                JobId = jobId,
                TenantId = tenantId,
                MasterTemplateVersion = masterTemplateVersion,
                PreviousVersion = "1.0.0", // This should be determined dynamically
                Status = JobStatus.Scheduled,
                ScheduledAt = scheduledTime ?? DateTime.UtcNow,
                ScheduledBy = scheduledBy,
                IsGlobalJob = false,
                TotalTenants = 1
            };

            await _templateSyncJobRepository.AddAsync(templateSyncJob);
            await _templateSyncJobRepository.SaveChangesAsync();

            // Schedule the job
            var hangfireJobId = scheduledTime.HasValue
                ? _backgroundJobClient.Schedule(() => ExecuteTenantTemplateSyncAsync(tenantId, masterTemplateVersion, scheduledBy), scheduledTime.Value)
                : _backgroundJobClient.Enqueue(() => ExecuteTenantTemplateSyncAsync(tenantId, masterTemplateVersion, scheduledBy));

            _logger.LogInformation("Scheduled tenant template sync job {JobId} for tenant {TenantId} version {Version}", jobId, tenantId, masterTemplateVersion);
            return jobId;
        }

        public async Task ExecuteTenantTemplateSyncAsync(string tenantId, string masterTemplateVersion, string syncedBy)
        {
            _logger.LogInformation("Starting template sync for tenant {TenantId} version {Version}", tenantId, masterTemplateVersion);

            try
            {
                // Create tenant-specific DbContext
                var connectionStringTemplate = _configuration.GetConnectionString("DefaultConnection");
                var connectionString = connectionStringTemplate?.Replace("{TENANT_ID}", tenantId);

                var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
                optionsBuilder.UseNpgsql(connectionString);

                using var tenantContext = new ApplicationDbContext(optionsBuilder.Options, new DebugTenantProvider(tenantId));

                // Create template sync log
                var syncLog = new TemplateSyncLog
                {
                    TenantId = tenantId,
                    MasterTemplateVersion = masterTemplateVersion,
                    PreviousVersion = "1.0.0", // Should be determined dynamically
                    Status = SyncStatus.InProgress,
                    SyncStartedAt = DateTime.UtcNow,
                    SyncedBy = syncedBy,
                    FilesUpdated = new List<string>(),
                    FilesAdded = new List<string>(),
                    FilesDeleted = new List<string>(),
                    ConflictResolutions = new Dictionary<string, object>(),
                    SyncMetadata = new Dictionary<string, object>()
                };

                tenantContext.TemplateSyncLogs.Add(syncLog);
                await tenantContext.SaveChangesAsync();

                // Simulate template sync process
                await Task.Delay(1000); // Simulate work

                // Update sync completion
                syncLog.Status = SyncStatus.Completed;
                syncLog.SyncCompletedAt = DateTime.UtcNow;
                syncLog.FilesUpdated = new List<string> { "template.html", "styles.css" };

                tenantContext.TemplateSyncLogs.Update(syncLog);
                await tenantContext.SaveChangesAsync();

                // Update tenant registry
                var tenant = await _tenantRegistryRepository.FirstOrDefaultAsync(t => t.TenantId == tenantId);
                if (tenant != null)
                {
                    tenant.CurrentTemplateVersion = masterTemplateVersion;
                    tenant.LastTemplateSync = DateTime.UtcNow;
                    _tenantRegistryRepository.Update(tenant);
                    await _tenantRegistryRepository.SaveChangesAsync();
                }

                _logger.LogInformation("Successfully synced template version {Version} to tenant {TenantId}", masterTemplateVersion, tenantId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to sync template version {Version} to tenant {TenantId}", masterTemplateVersion, tenantId);
                throw;
            }
        }

        public async Task<List<string>> CheckAvailableTemplateVersionsAsync()
        {
            try
            {
                // Simulate checking available template versions
                return new List<string> { "1.0.0", "1.0.1", "1.1.0", "1.2.0" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking available template versions");
                throw;
            }
        }

        public async Task<SyncJobStatus> GetSyncJobStatusAsync(string jobId)
        {
            var job = await _templateSyncJobRepository.FirstOrDefaultAsync(j => j.JobId == jobId);
            if (job == null)
            {
                return new SyncJobStatus { JobId = jobId, Status = "NotFound" };
            }

            return new SyncJobStatus
            {
                JobId = job.JobId,
                Status = job.Status.ToString(),
                ErrorMessage = job.ErrorMessage,
                TotalTenants = job.TotalTenants,
                CompletedTenants = job.CompletedTenants,
                FailedTenants = job.FailedTenants,
                CreatedAt = job.CreatedAt,
                CompletedAt = job.CompletedAt
            };
        }

        public async Task<bool> CancelScheduledSyncAsync(string jobId, string cancelledBy)
        {
            try
            {
                var job = await _templateSyncJobRepository.FirstOrDefaultAsync(j => j.JobId == jobId);
                if (job != null && job.Status == JobStatus.Scheduled)
                {
                    job.Status = JobStatus.Cancelled;
                    _templateSyncJobRepository.Update(job);
                    await _templateSyncJobRepository.SaveChangesAsync();

                    // Cancel in Hangfire
                    _backgroundJobClient.Delete(jobId);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to cancel template sync job {JobId}", jobId);
                return false;
            }
        }

        public async Task<TemplateSyncReport> GetTemplateSyncReportAsync(DateTime? fromDate = null, DateTime? toDate = null)
        {
            var syncJobs = await _templateSyncJobRepository.GetAllAsync();

            if (fromDate.HasValue)
                syncJobs = syncJobs.Where(s => s.CreatedAt >= fromDate.Value);

            if (toDate.HasValue)
                syncJobs = syncJobs.Where(s => s.CreatedAt <= toDate.Value);

            var syncJobsList = syncJobs.ToList();

            return new TemplateSyncReport
            {
                TotalSyncs = syncJobsList.Count,
                SuccessfulSyncs = syncJobsList.Count(s => s.Status == JobStatus.Completed),
                FailedSyncs = syncJobsList.Count(s => s.Status == JobStatus.Failed),
                ConflictsDetected = syncJobsList.Count(s => s.RequiresManualReview),
                RecentSyncs = syncJobsList
                    .Where(s => s.CompletedAt.HasValue)
                    .OrderByDescending(s => s.CompletedAt)
                    .Take(10)
                    .Select(s => new TemplateSyncSummary
                    {
                        JobId = s.JobId,
                        Version = s.MasterTemplateVersion,
                        Status = s.Status.ToString(),
                        CompletedAt = s.CompletedAt!.Value,
                        AffectedTenants = s.TotalTenants,
                        SyncedBy = s.ScheduledBy
                    }).ToList(),
                TenantSyncCounts = syncJobsList
                    .Where(s => !string.IsNullOrEmpty(s.TenantId))
                    .GroupBy(s => s.TenantId!)
                    .ToDictionary(g => g.Key, g => g.Count())
            };
        }

        public async Task<ConflictAnalysisReport> AnalyzeConflictsAsync(string tenantId, string masterTemplateVersion)
        {
            try
            {
                // Simulate conflict analysis
                var conflicts = new List<ConflictWarning>();

                // Add some sample conflicts if needed
                if (masterTemplateVersion == "2.0.0")
                {
                    conflicts.Add(new ConflictWarning
                    {
                        TenantId = tenantId,
                        TenantName = $"Tenant {tenantId}",
                        ConflictType = "Template Override",
                        Description = "Custom template modifications may be overwritten",
                        Severity = "Medium",
                        AffectedFiles = new List<string> { "custom-template.html" }
                    });
                }

                return new ConflictAnalysisReport
                {
                    TenantId = tenantId,
                    MasterTemplateVersion = masterTemplateVersion,
                    Conflicts = conflicts,
                    OverallRiskLevel = conflicts.Any() ? "Medium" : "Low",
                    RequiresManualIntervention = conflicts.Any(c => c.Severity == "High" || c.Severity == "Critical"),
                    RecommendedActions = conflicts.Any()
                        ? new List<string> { "Review custom template modifications", "Backup current templates before sync" }
                        : new List<string> { "Safe to proceed with automatic sync" }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing conflicts for tenant {TenantId} version {Version}", tenantId, masterTemplateVersion);
                throw;
            }
        }

        [Queue("template-sync")]
        public async Task ExecuteGlobalTemplateSyncAsync(string jobId)
        {
            var job = await _templateSyncJobRepository.FirstOrDefaultAsync(j => j.JobId == jobId);
            if (job == null) return;

            job.Status = JobStatus.InProgress;
            job.StartedAt = DateTime.UtcNow;
            _templateSyncJobRepository.Update(job);
            await _templateSyncJobRepository.SaveChangesAsync();

            try
            {
                var activeTenants = await _tenantRegistryRepository.FindAsync(t => t.IsActive && t.AutoSyncEnabled);
                var completedCount = 0;
                var failedTenants = new List<string>();

                foreach (var tenant in activeTenants)
                {
                    try
                    {
                        await ExecuteTenantTemplateSyncAsync(tenant.TenantId, job.MasterTemplateVersion, job.ScheduledBy);
                        completedCount++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to sync template to tenant {TenantId}", tenant.TenantId);
                        failedTenants.Add(tenant.TenantId);
                    }

                    // Update progress
                    job.CompletedTenants = completedCount;
                    job.FailedTenants = failedTenants;
                    _templateSyncJobRepository.Update(job);
                    await _templateSyncJobRepository.SaveChangesAsync();
                }

                job.Status = failedTenants.Any() ? JobStatus.PartiallyCompleted : JobStatus.Completed;
                job.CompletedAt = DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Global template sync job {JobId} failed", jobId);
                job.Status = JobStatus.Failed;
                job.ErrorMessage = ex.Message;
                job.CompletedAt = DateTime.UtcNow;
            }

            _templateSyncJobRepository.Update(job);
            await _templateSyncJobRepository.SaveChangesAsync();
        }
    }

    //public class DebugTenantProvider : ITenantProvider
    //{
    //    private readonly string _tenantId;

    //    public DebugTenantProvider(string tenantId)
    //    {
    //        _tenantId = tenantId;
    //    }

    //    public string GetTenantId() => _tenantId;
    //}
}