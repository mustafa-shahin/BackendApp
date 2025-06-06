using Backend.CMS.Domain.Entities;
using Backend.CMS.Domain.Enums;

namespace Backend.CMS.Application.Interfaces.Services
{
    public interface IVersioningService
    {
        Task<DeploymentVersion> CreateDeploymentVersionAsync(string version, string releaseNotes, Dictionary<string, object> migrationData);
        Task<bool> DeployVersionAsync(Guid versionId, string deployedBy);
        Task<bool> RollbackToVersionAsync(Guid versionId, string rolledBackBy);
        Task<DeploymentVersion?> GetCurrentVersionAsync();
        Task<List<DeploymentVersion>> GetVersionHistoryAsync(int page = 1, int pageSize = 20);
        Task<bool> CanRollbackToVersionAsync(Guid versionId);
        Task<Dictionary<string, object>> GetVersionDifferencesAsync(Guid fromVersionId, Guid toVersionId);
    }

    public interface ITemplateSyncService
    {
        Task<TemplateSyncLog> InitiateSyncFromMasterAsync(string masterTemplateVersion, string syncedBy);
        Task<bool> ApplySyncChangesAsync(Guid syncLogId, Dictionary<string, ConflictResolutionStrategy> conflictResolutions);
        Task<List<TemplateSyncLog>> GetSyncHistoryAsync(int page = 1, int pageSize = 20);
        Task<Dictionary<string, object>> AnalyzeMasterTemplateChangesAsync(string masterTemplateVersion);
        Task<bool> HasPendingConflictsAsync();
        Task<List<string>> GetConflictingFilesAsync(Guid syncLogId);
    }
}