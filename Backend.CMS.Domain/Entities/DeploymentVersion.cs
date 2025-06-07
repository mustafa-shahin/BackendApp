using Backend.CMS.Domain.Common;
using Backend.CMS.Domain.Enums;

namespace Backend.CMS.Domain.Entities
{
    public class DeploymentVersion : BaseEntity
    {
        public string Version { get; set; } = string.Empty;
        public string ReleaseNotes { get; set; } = string.Empty;
        public DateTime ReleasedAt { get; set; }
        public DateTime? DeployedAt { get; set; }
        public DeploymentStatus Status { get; set; } = DeploymentStatus.Pending;
        public string? ErrorMessage { get; set; }
        public Dictionary<string, object> MigrationData { get; set; } = new();
        public bool IsRollback { get; set; }
        public Guid? RollbackFromVersionId { get; set; }
        public DeploymentVersion? RollbackFromVersion { get; set; }
        public string DeployedBy { get; set; } = string.Empty;
        public Dictionary<string, object> DeploymentMetadata { get; set; } = new();
    }

    public class TemplateSyncLog : BaseEntity
    {
        public string MasterTemplateVersion { get; set; } = string.Empty;
        public string PreviousVersion { get; set; } = string.Empty;
        public SyncStatus Status { get; set; } = SyncStatus.Pending;
        public DateTime SyncStartedAt { get; set; }
        public DateTime? SyncCompletedAt { get; set; }
        public string? ErrorMessage { get; set; }
        public List<string> FilesUpdated { get; set; } = new();
        public List<string> FilesAdded { get; set; } = new();
        public List<string> FilesDeleted { get; set; } = new();
        public Dictionary<string, object> ConflictResolutions { get; set; } = new();
        public bool RequiresManualReview { get; set; }
        public string SyncedBy { get; set; } = string.Empty;
        public Dictionary<string, object> SyncMetadata { get; set; } = new();
    }


    public class DeploymentJob : BaseEntity
    {
        public string JobId { get; set; } = string.Empty;
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
}