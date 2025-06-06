using Backend.CMS.Application.DTOs.Companies;
using Backend.CMS.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.CMS.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class CompanyController : ControllerBase
    {
        private readonly ICompanyService _companyService;
        private readonly ILogger<CompanyController> _logger;

        public CompanyController(ICompanyService companyService, ILogger<CompanyController> logger)
        {
            _companyService = companyService;
            _logger = logger;
        }

        /// <summary>
        /// Get company information
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<CompanyDto>> GetCompany()
        {
            try
            {
                var company = await _companyService.GetCompanyAsync();
                return Ok(company);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning("Company not found: {Message}", ex.Message);
                return NotFound(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving company information");
                return StatusCode(500, new { Message = "An error occurred while retrieving company information" });
            }
        }

        /// <summary>
        /// Update company information
        /// </summary>
        [HttpPut]
        public async Task<ActionResult<CompanyDto>> UpdateCompany([FromBody] UpdateCompanyDto updateCompanyDto)
        {
            try
            {
                var company = await _companyService.UpdateCompanyAsync(updateCompanyDto);
                return Ok(company);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning("Company update failed: {Message}", ex.Message);
                return BadRequest(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating company information");
                return StatusCode(500, new { Message = "An error occurred while updating company information" });
            }
        }

        /// <summary>
        /// Get all locations
        /// </summary>
        [HttpGet("locations")]
        public async Task<ActionResult<List<LocationDto>>> GetLocations()
        {
            try
            {
                var locations = await _companyService.GetLocationsAsync();
                return Ok(locations);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving locations");
                return StatusCode(500, new { Message = "An error occurred while retrieving locations" });
            }
        }

        /// <summary>
        /// Get location by ID
        /// </summary>
        [HttpGet("locations/{id:guid}")]
        public async Task<ActionResult<LocationDto>> GetLocation(Guid id)
        {
            try
            {
                var location = await _companyService.GetLocationByIdAsync(id);
                return Ok(location);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning("Location not found: {LocationId}", id);
                return NotFound(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving location {LocationId}", id);
                return StatusCode(500, new { Message = "An error occurred while retrieving the location" });
            }
        }

        /// <summary>
        /// Get main location
        /// </summary>
        [HttpGet("locations/main")]
        public async Task<ActionResult<LocationDto>> GetMainLocation()
        {
            try
            {
                var location = await _companyService.GetMainLocationAsync();
                return Ok(location);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning("Main location not found: {Message}", ex.Message);
                return NotFound(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving main location");
                return StatusCode(500, new { Message = "An error occurred while retrieving the main location" });
            }
        }

        /// <summary>
        /// Create a new location
        /// </summary>
        [HttpPost("locations")]
        public async Task<ActionResult<LocationDto>> CreateLocation([FromBody] CreateLocationDto createLocationDto)
        {
            try
            {
                var location = await _companyService.CreateLocationAsync(createLocationDto);
                return CreatedAtAction(nameof(GetLocation), new { id = location.Id }, location);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning("Location creation failed: {Message}", ex.Message);
                return BadRequest(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating location");
                return StatusCode(500, new { Message = "An error occurred while creating the location" });
            }
        }

        /// <summary>
        /// Update an existing location
        /// </summary>
        [HttpPut("locations/{id:guid}")]
        public async Task<ActionResult<LocationDto>> UpdateLocation(Guid id, [FromBody] UpdateLocationDto updateLocationDto)
        {
            try
            {
                var location = await _companyService.UpdateLocationAsync(id, updateLocationDto);
                return Ok(location);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning("Location update failed for {LocationId}: {Message}", id, ex.Message);
                return BadRequest(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating location {LocationId}", id);
                return StatusCode(500, new { Message = "An error occurred while updating the location" });
            }
        }

        /// <summary>
        /// Delete a location
        /// </summary>
        [HttpDelete("locations/{id:guid}")]
        public async Task<ActionResult> DeleteLocation(Guid id)
        {
            try
            {
                var success = await _companyService.DeleteLocationAsync(id);
                if (!success)
                {
                    return NotFound(new { Message = "Location not found" });
                }
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting location {LocationId}", id);
                return StatusCode(500, new { Message = "An error occurred while deleting the location" });
            }
        }

        /// <summary>
        /// Set location as main location
        /// </summary>
        [HttpPost("locations/{id:guid}/set-main")]
        public async Task<ActionResult> SetMainLocation(Guid id)
        {
            try
            {
                var success = await _companyService.SetMainLocationAsync(id);
                if (!success)
                {
                    return NotFound(new { Message = "Location not found" });
                }
                return Ok(new { Message = "Main location set successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting main location {LocationId}", id);
                return StatusCode(500, new { Message = "An error occurred while setting the main location" });
            }
        }
    }
}