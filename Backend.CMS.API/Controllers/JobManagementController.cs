using Backend.CMS.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Backend.CMS.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Backend.CMS.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Administrator")]
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

        #region Deployment Management

        [HttpPost("deployments/propose")]
        public async Task<ActionResult<string>> CreateDeploymentProposal([FromBody] CreateDeploymentProposalRequest request)
        {
            try
            {
                var proposedBy = $"{User.FindFirst("firstName")?.Value} {User.FindFirst("lastName")?.Value}" ?? "Unknown Admin";
                var proposalId = await _deploymentJobService.CreateDeploymentProposalAsync(
                    request.Version,
                    request.ReleaseNotes,
                    request.MigrationData,
                    proposedBy);

                return Ok(new { ProposalId = proposalId, Message = "Deployment proposal created" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating deployment proposal");
                return StatusCode(500, "Error creating deployment proposal");
            }
        }

        [HttpGet("deployments/proposals/pending")]
        public async Task<ActionResult<List<DeploymentProposal>>> GetPendingDeploymentProposals()
        {
            try
            {
                return Ok(await _deploymentJobService.GetPendingDeploymentProposalsAsync());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving proposals");
                return StatusCode(500, "Error retrieving proposals");
            }
        }

        [HttpPost("deployments/proposals/{proposalId:guid}/approve")]
        public async Task<ActionResult<string>> ApproveDeploymentProposal(
            Guid proposalId,
            [FromBody] ApproveDeploymentRequest request)
        {
            try
            {
                var approvedBy = $"{User.FindFirst("firstName")?.Value} {User.FindFirst("lastName")?.Value}" ?? "Unknown Admin";
                var jobId = await _deploymentJobService.ApproveAndScheduleGlobalDeploymentAsync(
                    proposalId,
                    approvedBy,
                    request.ScheduledTime);

                return Ok(new { JobId = jobId, Message = "Deployment scheduled successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving proposal {ProposalId}", proposalId);
                return StatusCode(500, "Error approving deployment");
            }
        }

        [HttpPost("deployments/proposals/{proposalId:guid}/reject")]
        public async Task<ActionResult> RejectDeploymentProposal(
            Guid proposalId,
            [FromBody] RejectDeploymentRequest request)
        {
            try
            {
                var rejectedBy = $"{User.FindFirst("firstName")?.Value} {User.FindFirst("lastName")?.Value}" ?? "Unknown Admin";
                await _deploymentJobService.RejectDeploymentProposalAsync(
                    proposalId,
                    rejectedBy,
                    request.RejectionReason);

                return Ok(new { Message = "Proposal rejected" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting proposal {ProposalId}", proposalId);
                return StatusCode(500, "Error rejecting proposal");
            }
        }

        [HttpPost("deployments/tenant/{tenantId}")]
        public async Task<ActionResult<string>> ScheduleTenantDeployment(
            string tenantId,
            [FromBody] ScheduleTenantDeploymentRequest request)
        {
            try
            {
                var scheduledBy = $"{User.FindFirst("firstName")?.Value} {User.FindFirst("lastName")?.Value}" ?? "Unknown Admin";
                var jobId = await _deploymentJobService.ScheduleTenantDeploymentAsync(
                    tenantId,
                    request.Version,
                    request.ReleaseNotes,
                    request.MigrationData,
                    scheduledBy,
                    request.ScheduledTime);

                return Ok(new { JobId = jobId, Message = $"Deployment scheduled for {tenantId}" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error scheduling deployment for {TenantId}", tenantId);
                return StatusCode(500, "Error scheduling deployment");
            }
        }

        [HttpPost("deployments/tenant/{tenantId}/rollback")]
        public async Task<ActionResult<string>> ScheduleTenantRollback(
            string tenantId,
            [FromBody] ScheduleRollbackRequest request)
        {
            try
            {
                var scheduledBy = $"{User.FindFirst("firstName")?.Value} {User.FindFirst("lastName")?.Value}" ?? "Unknown Admin";
                var jobId = await _deploymentJobService.ScheduleTenantRollbackAsync(
                    tenantId,
                    request.TargetVersionId,
                    scheduledBy,
                    request.ScheduledTime);

                return Ok(new { JobId = jobId, Message = $"Rollback scheduled for {tenantId}" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error scheduling rollback for {TenantId}", tenantId);
                return StatusCode(500, "Error scheduling rollback");
            }
        }

        [HttpDelete("deployments/{jobId}")]
        public async Task<ActionResult> CancelDeployment(string jobId)
        {
            try
            {
                var cancelledBy = $"{User.FindFirst("firstName")?.Value} {User.FindFirst("lastName")?.Value}" ?? "Unknown Admin";
                await _deploymentJobService.CancelScheduledDeploymentAsync(jobId, cancelledBy);
                return Ok(new { Message = "Deployment cancelled" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling deployment {JobId}", jobId);
                return StatusCode(500, "Error cancelling deployment");
            }
        }

        [HttpGet("deployments/reports")]
        public async Task<ActionResult<DeploymentReport>> GetDeploymentReport(
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null)
        {
            try
            {
                return Ok(await _deploymentJobService.GetDeploymentReportAsync(fromDate, toDate));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting deployment report");
                return StatusCode(500, "Error retrieving report");
            }
        }

        [HttpGet("deployments/{jobId}/status")]
        public async Task<ActionResult<DeploymentJobStatus>> GetDeploymentJobStatus(string jobId)
        {
            try
            {
                return Ok(await _deploymentJobService.GetDeploymentJobStatusAsync(jobId));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting status for {JobId}", jobId);
                return StatusCode(500, "Error retrieving status");
            }
        }

        #endregion

        #region Template Sync Management

        [HttpPost("template-sync/check-updates")]
        public async Task<ActionResult<TemplateUpdateDetection>> CheckTemplateUpdates()
        {
            try
            {
                return Ok(await _templateSyncJobService.DetectTemplateUpdatesAsync());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking updates");
                return StatusCode(500, "Error checking updates");
            }
        }

        [HttpGet("template-sync/preview/{version}")]
        public async Task<ActionResult<TemplateUpdatePreview>> PreviewTemplateUpdates(string version)
        {
            try
            {
                return Ok(await _templateSyncJobService.PreviewTemplateUpdatesAsync(version));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error previewing version {Version}", version);
                return StatusCode(500, "Error previewing updates");
            }
        }

        [HttpPost("template-sync/approve")]
        public async Task<ActionResult<string>> ApproveTemplateSync([FromBody] ApproveTemplateSyncRequest request)
        {
            try
            {
                var approvedBy = $"{User.FindFirst("firstName")?.Value} {User.FindFirst("lastName")?.Value}" ?? "Unknown Admin";
                var jobId = await _templateSyncJobService.ApproveAndScheduleGlobalTemplateSyncAsync(
                    request.MasterTemplateVersion,
                    approvedBy,
                    request.ScheduledTime);

                return Ok(new { JobId = jobId, Message = "Template sync scheduled" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving template sync");
                return StatusCode(500, "Error approving sync");
            }
        }

        [HttpPost("template-sync/tenant/{tenantId}")]
        public async Task<ActionResult<string>> ScheduleTenantTemplateSync(
            string tenantId,
            [FromBody] ScheduleTenantTemplateSyncRequest request)
        {
            try
            {
                var scheduledBy = $"{User.FindFirst("firstName")?.Value} {User.FindFirst("lastName")?.Value}" ?? "Unknown Admin";
                var jobId = await _templateSyncJobService.ScheduleTenantTemplateSyncAsync(
                    tenantId,
                    request.MasterTemplateVersion,
                    scheduledBy,
                    request.ScheduledTime);

                return Ok(new { JobId = jobId, Message = $"Sync scheduled for {tenantId}" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error scheduling sync for {TenantId}", tenantId);
                return StatusCode(500, "Error scheduling sync");
            }
        }

        [HttpGet("template-sync/analyze-conflicts/{tenantId}/{version}")]
        public async Task<ActionResult<ConflictAnalysisReport>> AnalyzeConflicts(string tenantId, string version)
        {
            try
            {
                return Ok(await _templateSyncJobService.AnalyzeConflictsAsync(tenantId, version));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing conflicts for {TenantId}", tenantId);
                return StatusCode(500, "Error analyzing conflicts");
            }
        }

        [HttpGet("template-sync/reports")]
        public async Task<ActionResult<TemplateSyncReport>> GetTemplateSyncReport(
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null)
        {
            try
            {
                return Ok(await _templateSyncJobService.GetTemplateSyncReportAsync(fromDate, toDate));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting sync report");
                return StatusCode(500, "Error retrieving report");
            }
        }

        [HttpDelete("template-sync/{jobId}")]
        public async Task<ActionResult> CancelTemplateSync(string jobId)
        {
            try
            {
                var cancelledBy = $"{User.FindFirst("firstName")?.Value} {User.FindFirst("lastName")?.Value}" ?? "Unknown Admin";
                await _templateSyncJobService.CancelScheduledSyncAsync(jobId, cancelledBy);
                return Ok(new { Message = "Sync cancelled" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling sync {JobId}", jobId);
                return StatusCode(500, "Error cancelling sync");
            }
        }

        [HttpGet("template-sync/available-versions")]
        public async Task<ActionResult<List<string>>> GetAvailableTemplateVersions()
        {
            try
            {
                return Ok(await _templateSyncJobService.CheckAvailableTemplateVersionsAsync());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting versions");
                return StatusCode(500, "Error retrieving versions");
            }
        }

        [HttpGet("template-sync/{jobId}/status")]
        public async Task<ActionResult<SyncJobStatus>> GetTemplateSyncJobStatus(string jobId)
        {
            try
            {
                return Ok(await _templateSyncJobService.GetSyncJobStatusAsync(jobId));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting status for {JobId}", jobId);
                return StatusCode(500, "Error retrieving status");
            }
        }

        #endregion
    }

    #region Request DTOs

    public class CreateDeploymentProposalRequest
    {
        public string Version { get; set; } = string.Empty;
        public string ReleaseNotes { get; set; } = string.Empty;
        public Dictionary<string, object> MigrationData { get; set; } = new();
    }

    public class ApproveDeploymentRequest
    {
        public DateTime? ScheduledTime { get; set; }
    }

    public class RejectDeploymentRequest
    {
        public string RejectionReason { get; set; } = string.Empty;
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

    public class ApproveTemplateSyncRequest
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