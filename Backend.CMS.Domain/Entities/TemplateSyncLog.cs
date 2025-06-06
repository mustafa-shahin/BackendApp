using Backend.CMS.Domain.Common;
using Backend.CMS.Domain.Common.Interfaces;
using Backend.CMS.Domain.Enums;

namespace Backend.CMS.Domain.Entities
{
    public class TemplateSyncLog : BaseEntity, ITenantEntity
    {
        public string TenantId { get; set; } = string.Empty;
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
}