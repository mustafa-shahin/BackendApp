using Backend.CMS.Domain.Common;
using Backend.CMS.Domain.Common.Interfaces;
using Backend.CMS.Domain.Enums;

namespace Backend.CMS.Domain.Entities
{
    public class DeploymentVersion : BaseEntity, ITenantEntity
    {
        public string TenantId { get; set; } = string.Empty;
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
}