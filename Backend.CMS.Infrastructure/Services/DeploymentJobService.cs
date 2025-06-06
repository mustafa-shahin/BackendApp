using Backend.CMS.Application.Interfaces.Services;
using Backend.CMS.Domain.Entities;
using Backend.CMS.Domain.Enums;
using Backend.CMS.Infrastructure.Data;
using Backend.CMS.Infrastructure.Repositories;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Backend.CMS.Infrastructure.Services
{
    public class DeploymentJobService : IDeploymentJobService
    {
        private readonly IRepository<DeploymentJob> _deploymentJobRepository;
        private readonly IRepository<DeploymentProposal> _deploymentProposalRepository;
        private readonly IRepository<TenantRegistry> _tenantRegistryRepository;
        private readonly IVersioningService _versioningService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<DeploymentJobService> _logger;
        private readonly IBackgroundJobClient _backgroundJobClient;
        private readonly IServiceProvider _serviceProvider;

        public DeploymentJobService(
            IRepository<DeploymentJob> deploymentJobRepository,
            IRepository<DeploymentProposal> deploymentProposalRepository,
            IRepository<TenantRegistry> tenantRegistryRepository,
            IVersioningService versioningService,
            IConfiguration configuration,
            ILogger<DeploymentJobService> logger,
            IBackgroundJobClient backgroundJobClient,
            IServiceProvider serviceProvider)
        {
            _deploymentJobRepository = deploymentJobRepository;
            _deploymentProposalRepository = deploymentProposalRepository;
            _tenantRegistryRepository = tenantRegistryRepository;
            _versioningService = versioningService;
            _configuration = configuration;
            _logger = logger;
            _backgroundJobClient = backgroundJobClient;
            _serviceProvider = serviceProvider;
        }

        public async Task<string> CreateDeploymentProposalAsync(string version, string releaseNotes, Dictionary<string, object> migrationData, string proposedBy)
        {
            var proposal = new DeploymentProposal
            {
                Version = version,
                ReleaseNotes = releaseNotes,
                MigrationData = migrationData,
                ProposedBy = proposedBy,
                ProposedAt = DateTime.UtcNow,
                Status = ProposalStatus.Pending
            };

            await _deploymentProposalRepository.AddAsync(proposal);
            await _deploymentProposalRepository.SaveChangesAsync();

            _logger.LogInformation("Created deployment proposal {ProposalId} for version {Version}", proposal.Id, version);
            return proposal.Id.ToString();
        }

        public async Task<List<DeploymentProposal>> GetPendingDeploymentProposalsAsync()
        {
            var proposals = await _deploymentProposalRepository.FindAsync(p => p.Status == ProposalStatus.Pending);
            return [.. proposals.OrderBy(p => p.ProposedAt)];
        }

        public async Task<string> ApproveAndScheduleGlobalDeploymentAsync(Guid proposalId, string approvedBy, DateTime? scheduledTime = null)
        {
            var proposal = await _deploymentProposalRepository.GetByIdAsync(proposalId);
            if (proposal == null || proposal.Status != ProposalStatus.Pending)
            {
                throw new ArgumentException("Proposal not found or already processed");
            }

            proposal.Status = ProposalStatus.Approved;
            proposal.ReviewedBy = approvedBy;
            proposal.ReviewedAt = DateTime.UtcNow;

            var jobId = Guid.NewGuid().ToString();
            proposal.ApprovedJobId = jobId;

            var activeTenants = await _tenantRegistryRepository.FindAsync(t => t.IsActive && t.AutoDeployEnabled);

            var deploymentJob = new DeploymentJob
            {
                JobId = jobId,
                Version = proposal.Version,
                ReleaseNotes = proposal.ReleaseNotes,
                MigrationData = proposal.MigrationData,
                Status = JobStatus.Scheduled,
                ScheduledAt = scheduledTime ?? DateTime.UtcNow,
                ScheduledBy = approvedBy,
                IsGlobalJob = true,
                TotalTenants = activeTenants.Count()
            };

            await _deploymentJobRepository.AddAsync(deploymentJob);
            _deploymentProposalRepository.Update(proposal);
            await _deploymentProposalRepository.SaveChangesAsync();

            // Schedule the job
            var hangfireJobId = scheduledTime.HasValue
                ? _backgroundJobClient.Schedule(() => ExecuteGlobalDeploymentAsync(jobId), scheduledTime.Value)
                : _backgroundJobClient.Enqueue(() => ExecuteGlobalDeploymentAsync(jobId));

            _logger.LogInformation("Scheduled global deployment job {JobId} for version {Version}", jobId, proposal.Version);
            return jobId;
        }

        public async Task<bool> RejectDeploymentProposalAsync(Guid proposalId, string rejectedBy, string rejectionReason)
        {
            var proposal = await _deploymentProposalRepository.GetByIdAsync(proposalId);
            if (proposal == null || proposal.Status != ProposalStatus.Pending)
            {
                return false;
            }

            proposal.Status = ProposalStatus.Rejected;
            proposal.ReviewedBy = rejectedBy;
            proposal.ReviewedAt = DateTime.UtcNow;
            proposal.RejectionReason = rejectionReason;

            _deploymentProposalRepository.Update(proposal);
            await _deploymentProposalRepository.SaveChangesAsync();

            _logger.LogInformation("Rejected deployment proposal {ProposalId} by {RejectedBy}", proposalId, rejectedBy);
            return true;
        }

        public async Task<string> ScheduleTenantDeploymentAsync(string tenantId, string version, string releaseNotes, Dictionary<string, object> migrationData, string scheduledBy, DateTime? scheduledTime = null)
        {
            var jobId = Guid.NewGuid().ToString();

            var deploymentJob = new DeploymentJob
            {
                JobId = jobId,
                TenantId = tenantId,
                Version = version,
                ReleaseNotes = releaseNotes,
                MigrationData = migrationData,
                Status = JobStatus.Scheduled,
                ScheduledAt = scheduledTime ?? DateTime.UtcNow,
                ScheduledBy = scheduledBy,
                IsGlobalJob = false,
                TotalTenants = 1
            };

            await _deploymentJobRepository.AddAsync(deploymentJob);
            await _deploymentJobRepository.SaveChangesAsync();

            // Schedule the job
            var hangfireJobId = scheduledTime.HasValue
                ? _backgroundJobClient.Schedule(() => ExecuteTenantDeploymentAsync(tenantId, version, releaseNotes, migrationData, scheduledBy), scheduledTime.Value)
                : _backgroundJobClient.Enqueue(() => ExecuteTenantDeploymentAsync(tenantId, version, releaseNotes, migrationData, scheduledBy));

            _logger.LogInformation("Scheduled tenant deployment job {JobId} for tenant {TenantId} version {Version}", jobId, tenantId, version);
            return jobId;
        }

        public async Task ExecuteTenantDeploymentAsync(string tenantId, string version, string releaseNotes, Dictionary<string, object> migrationData, string deployedBy)
        {
            _logger.LogInformation("Starting deployment for tenant {TenantId} version {Version}", tenantId, version);

            try
            {
                // Create tenant-specific DbContext
                var connectionStringTemplate = _configuration.GetConnectionString("DefaultConnection");
                var connectionString = connectionStringTemplate?.Replace("{TENANT_ID}", tenantId);

                var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
                optionsBuilder.UseNpgsql(connectionString);

                using var tenantContext = new ApplicationDbContext(optionsBuilder.Options, new DebugTenantProvider(tenantId));

                // Create VersioningService with proper dependencies
                var tenantVersioningService = new VersioningService(
                    new Repository<DeploymentVersion>(tenantContext),
                    tenantContext,
                    _serviceProvider.GetRequiredService<ILogger<VersioningService>>());

                // Create and deploy version
                var deploymentVersion = await tenantVersioningService.CreateDeploymentVersionAsync(version, releaseNotes, migrationData);
                var success = await tenantVersioningService.DeployVersionAsync(deploymentVersion.Id, deployedBy);

                if (success)
                {
                    // Update tenant registry
                    var tenant = await _tenantRegistryRepository.FirstOrDefaultAsync(t => t.TenantId == tenantId);
                    if (tenant != null)
                    {
                        tenant.CurrentVersion = version;
                        tenant.LastDeployment = DateTime.UtcNow;
                        _tenantRegistryRepository.Update(tenant);
                        await _tenantRegistryRepository.SaveChangesAsync();
                    }

                    _logger.LogInformation("Successfully deployed version {Version} to tenant {TenantId}", version, tenantId);
                }
                else
                {
                    throw new Exception($"Deployment failed for tenant {tenantId}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to deploy version {Version} to tenant {TenantId}", version, tenantId);
                throw;
            }
        }

        public async Task<string> ScheduleTenantRollbackAsync(string tenantId, Guid targetVersionId, string scheduledBy, DateTime? scheduledTime = null)
        {
            var jobId = Guid.NewGuid().ToString();

            var rollbackJob = new DeploymentJob
            {
                JobId = jobId,
                TenantId = tenantId,
                Version = $"rollback-{targetVersionId}",
                ReleaseNotes = $"Rollback to version {targetVersionId}",
                Status = JobStatus.Scheduled,
                ScheduledAt = scheduledTime ?? DateTime.UtcNow,
                ScheduledBy = scheduledBy,
                IsGlobalJob = false,
                TotalTenants = 1,
                JobMetadata = new Dictionary<string, object> { { "targetVersionId", targetVersionId.ToString() } }
            };

            await _deploymentJobRepository.AddAsync(rollbackJob);
            await _deploymentJobRepository.SaveChangesAsync();

            var hangfireJobId = scheduledTime.HasValue
                ? _backgroundJobClient.Schedule(() => ExecuteTenantRollbackAsync(tenantId, targetVersionId, scheduledBy), scheduledTime.Value)
                : _backgroundJobClient.Enqueue(() => ExecuteTenantRollbackAsync(tenantId, targetVersionId, scheduledBy));

            return jobId;
        }

        public async Task ExecuteTenantRollbackAsync(string tenantId, Guid targetVersionId, string rolledBackBy)
        {
            _logger.LogInformation("Starting rollback for tenant {TenantId} to version {TargetVersionId}", tenantId, targetVersionId);

            try
            {
                var connectionStringTemplate = _configuration.GetConnectionString("DefaultConnection");
                var connectionString = connectionStringTemplate?.Replace("{TENANT_ID}", tenantId);

                var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
                optionsBuilder.UseNpgsql(connectionString);

                using var tenantContext = new ApplicationDbContext(optionsBuilder.Options, new DebugTenantProvider(tenantId));

                var tenantVersioningService = new VersioningService(
                    new Repository<DeploymentVersion>(tenantContext),
                    tenantContext,
                    _serviceProvider.GetRequiredService<ILogger<VersioningService>>());

                var success = await tenantVersioningService.RollbackToVersionAsync(targetVersionId, rolledBackBy);

                if (!success)
                {
                    throw new Exception($"Rollback failed for tenant {tenantId}");
                }

                _logger.LogInformation("Successfully rolled back tenant {TenantId} to version {TargetVersionId}", tenantId, targetVersionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to rollback tenant {TenantId} to version {TargetVersionId}", tenantId, targetVersionId);
                throw;
            }
        }

        public async Task<bool> CancelScheduledDeploymentAsync(string jobId, string cancelledBy)
        {
            try
            {
                var job = await _deploymentJobRepository.FirstOrDefaultAsync(j => j.JobId == jobId);
                if (job != null && job.Status == JobStatus.Scheduled)
                {
                    job.Status = JobStatus.Cancelled;
                    _deploymentJobRepository.Update(job);
                    await _deploymentJobRepository.SaveChangesAsync();

                    // Cancel in Hangfire
                    _backgroundJobClient.Delete(jobId);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to cancel deployment job {JobId}", jobId);
                return false;
            }
        }

        public async Task<DeploymentJobStatus> GetDeploymentJobStatusAsync(string jobId)
        {
            var job = await _deploymentJobRepository.FirstOrDefaultAsync(j => j.JobId == jobId);
            if (job == null)
            {
                return new DeploymentJobStatus { JobId = jobId, Status = "NotFound" };
            }

            return new DeploymentJobStatus
            {
                JobId = job.JobId,
                Status = job.Status.ToString(),
                ErrorMessage = job.ErrorMessage,
                CreatedAt = job.CreatedAt,
                CompletedAt = job.CompletedAt,
                Metadata = new Dictionary<string, object>
                {
                    { "version", job.Version },
                    { "tenantId", job.TenantId ?? "All" },
                    { "totalTenants", job.TotalTenants },
                    { "completedTenants", job.CompletedTenants },
                    { "failedTenants", job.FailedTenants }
                }
            };
        }

        public async Task<DeploymentReport> GetDeploymentReportAsync(DateTime? fromDate = null, DateTime? toDate = null)
        {
            var deployments = await _deploymentJobRepository.GetAllAsync();

            if (fromDate.HasValue)
                deployments = deployments.Where(d => d.CreatedAt >= fromDate.Value);

            if (toDate.HasValue)
                deployments = deployments.Where(d => d.CreatedAt <= toDate.Value);

            var deploymentsList = deployments.ToList();

            return new DeploymentReport
            {
                TotalDeployments = deploymentsList.Count,
                SuccessfulDeployments = deploymentsList.Count(d => d.Status == JobStatus.Completed),
                FailedDeployments = deploymentsList.Count(d => d.Status == JobStatus.Failed),
                PendingApprovals = (await _deploymentProposalRepository.FindAsync(p => p.Status == ProposalStatus.Pending)).Count(),
                RecentDeployments = deploymentsList
                    .Where(d => d.CompletedAt.HasValue)
                    .OrderByDescending(d => d.CompletedAt)
                    .Take(10)
                    .Select(d => new DeploymentSummary
                    {
                        JobId = d.JobId,
                        Version = d.Version,
                        Status = d.Status.ToString(),
                        CompletedAt = d.CompletedAt!.Value,
                        AffectedTenants = d.TotalTenants,
                        DeployedBy = d.ScheduledBy
                    }).ToList(),
                TenantDeploymentCounts = deploymentsList
                    .Where(d => !string.IsNullOrEmpty(d.TenantId))
                    .GroupBy(d => d.TenantId!)
                    .ToDictionary(g => g.Key, g => g.Count())
            };
        }

        [Queue("deployment")]
        public async Task ExecuteGlobalDeploymentAsync(string jobId)
        {
            var job = await _deploymentJobRepository.FirstOrDefaultAsync(j => j.JobId == jobId);
            if (job == null) return;

            job.Status = JobStatus.InProgress;
            job.StartedAt = DateTime.UtcNow;
            _deploymentJobRepository.Update(job);
            await _deploymentJobRepository.SaveChangesAsync();

            try
            {
                var activeTenants = await _tenantRegistryRepository.FindAsync(t => t.IsActive && t.AutoDeployEnabled);
                var completedCount = 0;
                var failedTenants = new List<string>();

                foreach (var tenant in activeTenants)
                {
                    try
                    {
                        await ExecuteTenantDeploymentAsync(tenant.TenantId, job.Version, job.ReleaseNotes, job.MigrationData, job.ScheduledBy);
                        completedCount++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to deploy to tenant {TenantId}", tenant.TenantId);
                        failedTenants.Add(tenant.TenantId);
                    }

                    // Update progress
                    job.CompletedTenants = completedCount;
                    job.FailedTenants = failedTenants;
                    _deploymentJobRepository.Update(job);
                    await _deploymentJobRepository.SaveChangesAsync();
                }

                job.Status = failedTenants.Any() ? JobStatus.PartiallyCompleted : JobStatus.Completed;
                job.CompletedAt = DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Global deployment job {JobId} failed", jobId);
                job.Status = JobStatus.Failed;
                job.ErrorMessage = ex.Message;
                job.CompletedAt = DateTime.UtcNow;
            }

            _deploymentJobRepository.Update(job);
            await _deploymentJobRepository.SaveChangesAsync();
        }
    }

    public class DebugTenantProvider : ITenantProvider
    {
        private readonly string _tenantId;

        public DebugTenantProvider(string tenantId)
        {
            _tenantId = tenantId;
        }

        public string GetTenantId() => _tenantId;
    }
}