using Backend.CMS.Domain.Common;
using Backend.CMS.Domain.Enums;

namespace Backend.CMS.Domain.Entities
{
    public class DeploymentProposal : BaseEntity
    {
        public string Version { get; set; } = string.Empty;
        public string ReleaseNotes { get; set; } = string.Empty;
        public Dictionary<string, object> MigrationData { get; set; } = new();
        public string ProposedBy { get; set; } = string.Empty;
        public DateTime ProposedAt { get; set; }
        public ProposalStatus Status { get; set; } = ProposalStatus.Pending;
        public string? ReviewedBy { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public string? ReviewNotes { get; set; }
        public string? RejectionReason { get; set; }
        public List<string> AffectedTenants { get; set; } = new();
        public Dictionary<string, object> ImpactAnalysis { get; set; } = new();
        public bool RequiresMaintenanceWindow { get; set; }
        public int EstimatedDurationMinutes { get; set; }
        public string RiskLevel { get; set; } = "Medium";
        public List<string> PrerequisiteChecks { get; set; } = new();
        public Dictionary<string, object> RollbackPlan { get; set; } = new();
        public bool HasBreakingChanges { get; set; }
        public DateTime? ScheduledDeploymentTime { get; set; }
        public string? ApprovedJobId { get; set; }
    }

    public class TemplateUpdateProposal : BaseEntity
    {
        public string MasterTemplateVersion { get; set; } = string.Empty;
        public string PreviousVersion { get; set; } = string.Empty;
        public string DetectedBy { get; set; } = string.Empty;
        public DateTime DetectedAt { get; set; }
        public ProposalStatus Status { get; set; } = ProposalStatus.Pending;
        public string? ReviewedBy { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public string? ReviewNotes { get; set; }
        public List<string> ChangedFiles { get; set; } = new();
        public List<string> AddedFiles { get; set; } = new();
        public List<string> DeletedFiles { get; set; } = new();
        public List<string> BreakingChanges { get; set; } = new();
        public Dictionary<string, object> ConflictAnalysis { get; set; } = new();
        public bool RequiresManualReview { get; set; }
        public string RiskLevel { get; set; } = "Medium";
        public List<string> AffectedTenants { get; set; } = new();
        public Dictionary<string, object> ImpactAnalysis { get; set; } = new();
        public DateTime? ScheduledSyncTime { get; set; }
        public string? ApprovedJobId { get; set; }
    }
}