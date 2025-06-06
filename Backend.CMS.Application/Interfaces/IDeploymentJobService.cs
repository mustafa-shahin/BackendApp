using Backend.CMS.Domain.Entities;

namespace Backend.CMS.Application.Interfaces.Services
{
    public interface IDeploymentJobService
    {
        /// <summary>
        /// Create deployment proposal for admin review (does not execute)
        /// </summary>
        Task<string> CreateDeploymentProposalAsync(string version, string releaseNotes, Dictionary<string, object> migrationData, string proposedBy);

        /// <summary>
        /// Get pending deployment proposals for admin review
        /// </summary>
        Task<List<DeploymentProposal>> GetPendingDeploymentProposalsAsync();

        /// <summary>
        /// Admin approves and schedules deployment for all tenants
        /// </summary>
        Task<string> ApproveAndScheduleGlobalDeploymentAsync(Guid proposalId, string approvedBy, DateTime? scheduledTime = null);

        /// <summary>
        /// Admin schedules deployment for specific tenant only
        /// </summary>
        Task<string> ScheduleTenantDeploymentAsync(string tenantId, string version, string releaseNotes, Dictionary<string, object> migrationData, string scheduledBy, DateTime? scheduledTime = null);

        /// <summary>
        /// Execute deployment (called by background job)
        /// </summary>
        Task ExecuteTenantDeploymentAsync(string tenantId, string version, string releaseNotes, Dictionary<string, object> migrationData, string deployedBy);

        /// <summary>
        /// Admin cancels deployment proposal
        /// </summary>
        Task<bool> RejectDeploymentProposalAsync(Guid proposalId, string rejectedBy, string rejectionReason);

        /// <summary>
        /// Cancel scheduled deployment
        /// </summary>
        Task<bool> CancelScheduledDeploymentAsync(string jobId, string cancelledBy);

        /// <summary>
        /// Get deployment job status
        /// </summary>
        Task<DeploymentJobStatus> GetDeploymentJobStatusAsync(string jobId);

        /// <summary>
        /// Admin schedules rollback for specific tenant
        /// </summary>
        Task<string> ScheduleTenantRollbackAsync(string tenantId, Guid targetVersionId, string scheduledBy, DateTime? scheduledTime = null);

        /// <summary>
        /// Execute rollback for specific tenant (background job)
        /// </summary>
        Task ExecuteTenantRollbackAsync(string tenantId, Guid targetVersionId, string rolledBackBy);

        /// <summary>
        /// Get deployment history and statistics
        /// </summary>
        Task<DeploymentReport> GetDeploymentReportAsync(DateTime? fromDate = null, DateTime? toDate = null);
    }

    public interface ITemplateSyncJobService
    {
        /// <summary>
        /// Detect available template updates (does not sync automatically)
        /// </summary>
        Task<TemplateUpdateDetection> DetectTemplateUpdatesAsync();

        /// <summary>
        /// Admin reviews template changes before approval
        /// </summary>
        Task<TemplateUpdatePreview> PreviewTemplateUpdatesAsync(string masterTemplateVersion);

        /// <summary>
        /// Admin approves and schedules template sync for all tenants
        /// </summary>
        Task<string> ApproveAndScheduleGlobalTemplateSyncAsync(string masterTemplateVersion, string approvedBy, DateTime? scheduledTime = null);

        /// <summary>
        /// Admin schedules template sync for specific tenant
        /// </summary>
        Task<string> ScheduleTenantTemplateSyncAsync(string tenantId, string masterTemplateVersion, string scheduledBy, DateTime? scheduledTime = null);

        /// <summary>
        /// Execute template sync for specific tenant (background job)
        /// </summary>
        Task ExecuteTenantTemplateSyncAsync(string tenantId, string masterTemplateVersion, string syncedBy);

        /// <summary>
        /// Admin manually checks for new template versions (does not auto-sync)
        /// </summary>
        Task<List<string>> CheckAvailableTemplateVersionsAsync();

        /// <summary>
        /// Get sync job status
        /// </summary>
        Task<SyncJobStatus> GetSyncJobStatusAsync(string jobId);

        /// <summary>
        /// Cancel scheduled sync
        /// </summary>
        Task<bool> CancelScheduledSyncAsync(string jobId, string cancelledBy);

        /// <summary>
        /// Get template sync history and conflict reports
        /// </summary>
        Task<TemplateSyncReport> GetTemplateSyncReportAsync(DateTime? fromDate = null, DateTime? toDate = null);

        /// <summary>
        /// Analyze potential conflicts before sync
        /// </summary>
        Task<ConflictAnalysisReport> AnalyzeConflictsAsync(string tenantId, string masterTemplateVersion);
    }

    public class DeploymentJobStatus
    {
        public string JobId { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? ErrorMessage { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    public class SyncJobStatus
    {
        public string JobId { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? ErrorMessage { get; set; }
        public int TotalTenants { get; set; }
        public int CompletedTenants { get; set; }
        public List<string> FailedTenants { get; set; } = new();
        public DateTime CreatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
    }

    public class DeploymentProposal
    {
        public Guid Id { get; set; }
        public string Version { get; set; } = string.Empty;
        public string ReleaseNotes { get; set; } = string.Empty;
        public Dictionary<string, object> MigrationData { get; set; } = new();
        public string ProposedBy { get; set; } = string.Empty;
        public DateTime ProposedAt { get; set; }
        public string Status { get; set; } = string.Empty; // Pending, Approved, Rejected
        public string? ReviewedBy { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public string? ReviewNotes { get; set; }
        public List<string> AffectedTenants { get; set; } = new();
        public Dictionary<string, object> ImpactAnalysis { get; set; } = new();
    }

    public class TemplateUpdateDetection
    {
        public string LatestVersion { get; set; } = string.Empty;
        public string CurrentVersion { get; set; } = string.Empty;
        public bool UpdatesAvailable { get; set; }
        public List<string> AvailableVersions { get; set; } = new();
        public DateTime LastChecked { get; set; }
        public Dictionary<string, object> VersionComparison { get; set; } = new();
    }

    public class TemplateUpdatePreview
    {
        public string Version { get; set; } = string.Empty;
        public List<string> ChangedFiles { get; set; } = new();
        public List<string> AddedFiles { get; set; } = new();
        public List<string> DeletedFiles { get; set; } = new();
        public List<string> BreakingChanges { get; set; } = new();
        public List<ConflictWarning> PotentialConflicts { get; set; } = new();
        public Dictionary<string, object> ImpactAnalysis { get; set; } = new();
        public bool RequiresManualReview { get; set; }
    }

    public class ConflictWarning
    {
        public string TenantId { get; set; } = string.Empty;
        public string TenantName { get; set; } = string.Empty;
        public string ConflictType { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty; // Low, Medium, High, Critical
        public List<string> AffectedFiles { get; set; } = new();
    }

    public class DeploymentReport
    {
        public int TotalDeployments { get; set; }
        public int SuccessfulDeployments { get; set; }
        public int FailedDeployments { get; set; }
        public int PendingApprovals { get; set; }
        public List<DeploymentSummary> RecentDeployments { get; set; } = new();
        public Dictionary<string, int> TenantDeploymentCounts { get; set; } = new();
    }

    public class TemplateSyncReport
    {
        public int TotalSyncs { get; set; }
        public int SuccessfulSyncs { get; set; }
        public int FailedSyncs { get; set; }
        public int ConflictsDetected { get; set; }
        public List<TemplateSyncSummary> RecentSyncs { get; set; } = new();
        public Dictionary<string, int> TenantSyncCounts { get; set; } = new();
    }

    public class ConflictAnalysisReport
    {
        public string TenantId { get; set; } = string.Empty;
        public string MasterTemplateVersion { get; set; } = string.Empty;
        public List<ConflictWarning> Conflicts { get; set; } = new();
        public string OverallRiskLevel { get; set; } = string.Empty;
        public bool RequiresManualIntervention { get; set; }
        public List<string> RecommendedActions { get; set; } = new();
    }

    public class DeploymentSummary
    {
        public string JobId { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime CompletedAt { get; set; }
        public int AffectedTenants { get; set; }
        public string DeployedBy { get; set; } = string.Empty;
    }

    public class TemplateSyncSummary
    {
        public string JobId { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime CompletedAt { get; set; }
        public int AffectedTenants { get; set; }
        public string SyncedBy { get; set; } = string.Empty;
    }
}