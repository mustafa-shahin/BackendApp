using Backend.CMS.Domain.Common;
using Backend.CMS.Domain.Enums;

namespace Backend.CMS.Domain.Entities
{
    public class DeploymentJob : BaseEntity
    {
        public string JobId { get; set; } = string.Empty;
        public string? TenantId { get; set; } // null for global jobs
        public string Version { get; set; } = string.Empty;
        public string ReleaseNotes { get; set; } = string.Empty;
        public Dictionary<string, object> MigrationData { get; set; } = new();
        public JobStatus Status { get; set; } = JobStatus.Scheduled;
        public DateTime ScheduledAt { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string? ErrorMessage { get; set; }
        public string ScheduledBy { get; set; } = string.Empty;
        public string? ExecutedBy { get; set; }
        public Dictionary<string, object> JobMetadata { get; set; } = new();
        public bool IsGlobalJob { get; set; }
        public int TotalTenants { get; set; }
        public int CompletedTenants { get; set; }
        public List<string> FailedTenants { get; set; } = new();
    }

    public class TemplateSyncJob : BaseEntity
    {
        public string JobId { get; set; } = string.Empty;
        public string? TenantId { get; set; } // null for global jobs
        public string MasterTemplateVersion { get; set; } = string.Empty;
        public string PreviousVersion { get; set; } = string.Empty;
        public JobStatus Status { get; set; } = JobStatus.Scheduled;
        public DateTime ScheduledAt { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string? ErrorMessage { get; set; }
        public string ScheduledBy { get; set; } = string.Empty;
        public string? ExecutedBy { get; set; }
        public Dictionary<string, object> JobMetadata { get; set; } = new();
        public bool IsGlobalJob { get; set; }
        public int TotalTenants { get; set; }
        public int CompletedTenants { get; set; }
        public List<string> FailedTenants { get; set; } = new();
        public bool RequiresManualReview { get; set; }
        public Dictionary<string, object> ConflictResolutions { get; set; } = new();
    }

    public class TenantRegistry : BaseEntity
    {
        public string TenantId { get; set; } = string.Empty;
        public string TenantName { get; set; } = string.Empty;
        public string DatabaseConnectionString { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public string CurrentVersion { get; set; } = string.Empty;
        public string CurrentTemplateVersion { get; set; } = string.Empty;
        public Dictionary<string, object> TenantMetadata { get; set; } = new();
        public DateTime LastDeployment { get; set; }
        public DateTime LastTemplateSync { get; set; }
        public bool AutoDeployEnabled { get; set; } = true;
        public bool AutoSyncEnabled { get; set; } = true;
        public string MaintenanceWindow { get; set; } = string.Empty; // JSON cron expression
    }
}