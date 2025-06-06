using Backend.CMS.Application.Interfaces.Services;
using Backend.CMS.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.CMS.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class VersioningController : ControllerBase
    {
        private readonly IVersioningService _versioningService;
        private readonly ILogger<VersioningController> _logger;

        public VersioningController(IVersioningService versioningService, ILogger<VersioningController> logger)
        {
            _versioningService = versioningService;
            _logger = logger;
        }

        /// <summary>
        /// Get current deployed version
        /// </summary>
        [HttpGet("current")]
        public async Task<ActionResult<DeploymentVersion>> GetCurrentVersion()
        {
            try
            {
                var currentVersion = await _versioningService.GetCurrentVersionAsync();
                if (currentVersion == null)
                {
                    return NotFound(new { Message = "No deployed version found" });
                }

                return Ok(currentVersion);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving current version");
                return StatusCode(500, new { Message = "An error occurred while retrieving current version" });
            }
        }

        /// <summary>
        /// Get version history
        /// </summary>
        [HttpGet("history")]
        public async Task<ActionResult<List<DeploymentVersion>>> GetVersionHistory(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                var versions = await _versioningService.GetVersionHistoryAsync(page, pageSize);
                return Ok(versions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving version history");
                return StatusCode(500, new { Message = "An error occurred while retrieving version history" });
            }
        }

        /// <summary>
        /// Create a new deployment version
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<DeploymentVersion>> CreateVersion([FromBody] CreateVersionDto createVersionDto)
        {
            try
            {
                var version = await _versioningService.CreateDeploymentVersionAsync(
                    createVersionDto.Version,
                    createVersionDto.ReleaseNotes,
                    createVersionDto.MigrationData);

                return CreatedAtAction(nameof(GetVersionById), new { id = version.Id }, version);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating deployment version");
                return StatusCode(500, new { Message = "An error occurred while creating the deployment version" });
            }
        }

        /// <summary>
        /// Get version by ID
        /// </summary>
        [HttpGet("{id:guid}")]
        public async Task<ActionResult<DeploymentVersion>> GetVersionById(Guid id)
        {
            try
            {
                var versions = await _versioningService.GetVersionHistoryAsync(1, 1000);
                var version = versions.FirstOrDefault(v => v.Id == id);

                if (version == null)
                {
                    return NotFound(new { Message = "Version not found" });
                }

                return Ok(version);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving version {VersionId}", id);
                return StatusCode(500, new { Message = "An error occurred while retrieving the version" });
            }
        }

        /// <summary>
        /// Deploy a version
        /// </summary>
        [HttpPost("{id:guid}/deploy")]
        public async Task<ActionResult> DeployVersion(Guid id)
        {
            try
            {
                var deployedBy = User.FindFirst("firstName")?.Value + " " + User.FindFirst("lastName")?.Value;
                var success = await _versioningService.DeployVersionAsync(id, deployedBy ?? "Unknown");

                if (!success)
                {
                    return BadRequest(new { Message = "Failed to deploy version" });
                }

                return Ok(new { Message = "Version deployed successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deploying version {VersionId}", id);
                return StatusCode(500, new { Message = "An error occurred while deploying the version" });
            }
        }

        /// <summary>
        /// Rollback to a previous version
        /// </summary>
        [HttpPost("{id:guid}/rollback")]
        public async Task<ActionResult> RollbackToVersion(Guid id)
        {
            try
            {
                var canRollback = await _versioningService.CanRollbackToVersionAsync(id);
                if (!canRollback)
                {
                    return BadRequest(new { Message = "Cannot rollback to this version" });
                }

                var rolledBackBy = User.FindFirst("firstName")?.Value + " " + User.FindFirst("lastName")?.Value;
                var success = await _versioningService.RollbackToVersionAsync(id, rolledBackBy ?? "Unknown");

                if (!success)
                {
                    return BadRequest(new { Message = "Failed to rollback to version" });
                }

                return Ok(new { Message = "Rollback completed successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rolling back to version {VersionId}", id);
                return StatusCode(500, new { Message = "An error occurred during rollback" });
            }
        }

        /// <summary>
        /// Check if rollback is possible for a version
        /// </summary>
        [HttpGet("{id:guid}/can-rollback")]
        public async Task<ActionResult<bool>> CanRollbackToVersion(Guid id)
        {
            try
            {
                var canRollback = await _versioningService.CanRollbackToVersionAsync(id);
                return Ok(new { CanRollback = canRollback });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking rollback capability for version {VersionId}", id);
                return StatusCode(500, new { Message = "An error occurred while checking rollback capability" });
            }
        }

        /// <summary>
        /// Get differences between two versions
        /// </summary>
        [HttpGet("compare")]
        public async Task<ActionResult<Dictionary<string, object>>> CompareVersions(
            [FromQuery] Guid fromVersionId,
            [FromQuery] Guid toVersionId)
        {
            try
            {
                var differences = await _versioningService.GetVersionDifferencesAsync(fromVersionId, toVersionId);
                return Ok(differences);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error comparing versions {FromVersionId} to {ToVersionId}", fromVersionId, toVersionId);
                return StatusCode(500, new { Message = "An error occurred while comparing versions" });
            }
        }
    }

    public class CreateVersionDto
    {
        public string Version { get; set; } = string.Empty;
        public string ReleaseNotes { get; set; } = string.Empty;
        public Dictionary<string, object> MigrationData { get; set; } = new();
    }
}