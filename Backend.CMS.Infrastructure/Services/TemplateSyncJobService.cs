using Backend.CMS.Application.Interfaces.Services;
using Backend.CMS.Domain.Entities;
using Backend.CMS.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Runtime.InteropServices;

namespace Backend.CMS.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class JobManagementController : ControllerBase
    {
        private readonly IDeploymentJobService _deploymentJobService;
        private readonly ITemplateSyncJobService _templateSyncJobService;
        private readonly ILogger<JobManagementController> _logger;

        public JobManagementController(
            IDeploymentJobService deploymentJobService,
            ITemplateSyncJobService templateSyncJobService,
            ILogger<JobManagementController> logger)
        {
            _deploymentJobService = deploymentJobService;
            _templateSyncJobService = templateSyncJobService;
            _logger = logger;
        }

        #region Deployment Jobs

        /// <summary>
        /// Schedule global deployment to all tenants
        /// </summary>
        [HttpPost("deployments/global")]
        public async Task<ActionResult<string>> ScheduleGlobalDeployment([FromBody] ScheduleGlobalDeploymentRequest request)
        {
            try
            {
                var jobId = await _deploymentJobService.ScheduleGlobalDeploymentAsync(
                    request.Version,
                    request.ReleaseNotes,
                    request.MigrationData,
                    request.ScheduledTime);

                return Ok(new { JobId = jobId, Message = "Global deployment scheduled successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error scheduling global deployment");
                return StatusCode(500, new { Message = "An error occurred while scheduling deployment" });
            }
        }

        /// <summary>
        /// Schedule deployment for specific tenant
        /// </summary>
        [HttpPost("deployments/tenant/{tenantId}")]
        public async Task<ActionResult<string>> ScheduleTenantDeployment(string tenantId, [FromBody] ScheduleTenantDeploymentRequest request)
        {
            try
            {
                var jobId = await _deploymentJobService.ScheduleTenantDeploymentAsync(
                    tenantId,
                    request.Version,
                    request.ReleaseNotes,
                    request.MigrationData,
                    request.ScheduledTime);

                return Ok(new { JobId = jobId, Message = $"Deployment scheduled for tenant {tenantId}" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error scheduling tenant deployment for {TenantId}", tenantId);
                return StatusCode(500, new { Message = "An error occurred while scheduling deployment" });
            }
        }

        /// <summary>
        /// Schedule rollback for specific tenant
        /// </summary>
        [HttpPost("deployments/tenant/{tenantId}/rollback")]
        public async Task<ActionResult<string>> ScheduleTenantRollback(string tenantId, [FromBody] ScheduleRollbackRequest request)
        {
            try
            {
                var rolledBackBy = User.FindFirst("firstName")?.Value + " " + User.FindFirst("lastName")?.Value;
                var jobId = await _deploymentJobService.ScheduleTenantRollbackAsync(
                    tenantId,
                    request.TargetVersionId,
                    rolledBackBy ?? "Unknown",
                    request.ScheduledTime);

                return Ok(new { JobId = jobId, Message = $"Rollback scheduled for tenant {tenantId}" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error scheduling tenant rollback for {TenantId}", tenantId);
                return StatusCode(500, new { Message = "An error occurred while scheduling rollback" });
            }
        }

        /// <summary>
        /// Get deployment job status
        /// </summary>
        [HttpGet("deployments/{jobId}/status")]
        public async Task<ActionResult<DeploymentJobStatus>> GetDeploymentJobStatus(string jobId)
        {
            try
            {
                var status = await _deploymentJobService.GetDeploymentJobStatusAsync(jobId);
                return Ok(status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting deployment job status for {JobId}", jobId);
                return StatusCode(500, new { Message = "An error occurred while retrieving job status" });
            }
        }

        /// <summary>
        /// Cancel scheduled deployment
        /// </summary>
        [HttpDelete("deployments/{jobId}")]
        public async Task<ActionResult> CancelDeployment(string jobId)
        {
            try
            {
                var success = await _deploymentJobService.CancelScheduledDeploymentAsync(jobId);
                if (!success)
                {
                    return BadRequest(new { Message = "Failed to cancel deployment job" });
                }

                return Ok(new { Message = "Deployment job cancelled successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling deployment job {JobId}", jobId);
                return StatusCode(500, new { Message = "An error occurred while cancelling deployment" });
            }
        }

        #endregion

        #region Template Sync Jobs

        /// <summary>
        /// Schedule global template sync to all tenants
        /// </summary>
        [HttpPost("template-sync/global")]
        public async Task<ActionResult<string>> ScheduleGlobalTemplateSync([FromBody] ScheduleGlobalTemplateSyncRequest request)
        {
            try
            {
                var jobId = await _templateSyncJobService.ScheduleGlobalTemplateSyncAsync(
                    request.MasterTemplateVersion,
                    request.ScheduledTime);

                return Ok(new { JobId = jobId, Message = "Global template sync scheduled successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error scheduling global template sync");
                return StatusCode(500, new { Message = "An error occurred while scheduling template sync" });
            }
        }

        /// <summary>
        /// Schedule template sync for specific tenant
        /// </summary>
        [HttpPost("template-sync/tenant/{tenantId}")]
        public async Task<ActionResult<string>> ScheduleTenantTemplateSync(string tenantId, [FromBody] ScheduleTenantTemplateSyncRequest request)
        {
            try
            {
                var jobId = await _templateSyncJobService.ScheduleTenantTemplateSyncAsync(
                    tenantId,
                    request.MasterTemplateVersion,
                    request.ScheduledTime);

                return Ok(new { JobId = jobId, Message = $"Template sync scheduled for tenant {tenantId}" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error scheduling tenant template sync for {TenantId}", tenantId);
                return StatusCode(500, new { Message = "An error occurred while scheduling template sync" });
            }
        }

        /// <summary>
        /// Get template sync job status
        /// </summary>
        [HttpGet("template-sync/{jobId}/status")]
        public async Task<ActionResult<SyncJobStatus>> GetTemplateSyncJobStatus(string jobId)
        {
            try
            {
                var status = await _templateSyncJobService.GetSyncJobStatusAsync(jobId);
                return Ok(status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting template sync job status for {JobId}", jobId);
                return StatusCode(500, new { Message = "An error occurred while retrieving job status" });
            }
        }

        /// <summary>
        /// Cancel scheduled template sync
        /// </summary>
        [HttpDelete("template-sync/{jobId}")]
        public async Task<ActionResult> CancelTemplateSync(string jobId)
        {
            try
            {
                var success = await _templateSyncJobService.CancelScheduledSyncAsync(jobId);
                if (!success)
                {
                    return BadRequest(new { Message = "Failed to cancel template sync job" });
                }

                return Ok(new { Message = "Template sync job cancelled successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling template sync job {JobId}", jobId);
                return StatusCode(500, new { Message = "An error occurred while cancelling template sync" });
            }
        }

        /// <summary>
        /// Start template sync monitoring
        /// </summary>
        [HttpPost("template-sync/monitoring/start")]
        public async Task<ActionResult> StartTemplateSyncMonitoring()
        {
            try
            {
                await _templateSyncJobService.StartTemplateSyncMonitoringAsync();
                return Ok(new { Message = "Template sync monitoring started" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting template sync monitoring");
                return StatusCode(500, new { Message = "An error occurred while starting monitoring" });
            }
        }

        /// <summary>
        /// Stop template sync monitoring
        /// </summary>
        [HttpPost("template-sync/monitoring/stop")]
        public async Task<ActionResult> StopTemplateSyncMonitoring()
        {
            try
            {
                await _templateSyncJobService.StopTemplateSyncMonitoringAsync();
                return Ok(new { Message = "Template sync monitoring stopped" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping template sync monitoring");
                return StatusCode(500, new { Message = "An error occurred while stopping monitoring" });
            }
        }

        /// <summary>
        /// Check for template updates and sync if needed
        /// </summary>
        [HttpPost("template-sync/check-updates")]
        public async Task<ActionResult> CheckTemplateUpdates()
        {
            try
            {
                await _templateSyncJobService.CheckAndSyncTemplateUpdatesAsync();
                return Ok(new { Message = "Template update check completed" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking template updates");
                return StatusCode(500, new { Message = "An error occurred while checking updates" });
            }
        }

        /// <summary>
        /// Analyze master template changes
        /// </summary>
        [HttpGet("template-sync/analyze/{version}")]
        public async Task<ActionResult<Dictionary<string, object>>> AnalyzeMasterTemplateChanges(string version)
        {
            try
            {
                var changes = await _templateSyncJobService.AnalyzeMasterTemplateChangesAsync(version);
                return Ok(changes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing master template changes for version {Version}", version);
                return StatusCode(500, new { Message = "An error occurred while analyzing changes" });
            }
        }

        #endregion
    }

    #region Request DTOs

    public class ScheduleGlobalDeploymentRequest
    {
        public string Version { get; set; } = string.Empty;
        public string ReleaseNotes { get; set; } = string.Empty;
        public Dictionary<string, object> MigrationData { get; set; } = new();
        public DateTime? ScheduledTime { get; set; }
    }

    public class ScheduleTenantDeploymentRequest
    {
        public string Version { get; set; } = string.Empty;
        public string ReleaseNotes { get; set; } = string.Empty;
        public Dictionary<string, object> MigrationData { get; set; } = new();
        public DateTime? ScheduledTime { get; set; }
    }

    public class ScheduleRollbackRequest
    {
        public Guid TargetVersionId { get; set; }
        public DateTime? ScheduledTime { get; set; }
    }

    public class ScheduleGlobalTemplateSyncRequest
    {
        public string MasterTemplateVersion { get; set; } = string.Empty;
        public DateTime? ScheduledTime { get; set; }
    }

    public class ScheduleTenantTemplateSyncRequest
    {
        public string MasterTemplateVersion { get; set; } = string.Empty;
        public DateTime? ScheduledTime { get; set; }
    }

    #endregion
}