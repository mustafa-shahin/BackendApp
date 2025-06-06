using Backend.CMS.Application.DTOs.Components;
using Backend.CMS.Application.DTOs.ComponentTemplates;
using Backend.CMS.Application.Interfaces.Services;
using Backend.CMS.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.CMS.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ComponentsController : ControllerBase
    {
        private readonly IComponentService _componentService;
        private readonly ILogger<ComponentsController> _logger;

        public ComponentsController(IComponentService componentService, ILogger<ComponentsController> logger)
        {
            _componentService = componentService;
            _logger = logger;
        }

        /// <summary>
        /// Get component library (all templates organized by category)
        /// </summary>
        [HttpGet("library")]
        public async Task<ActionResult<ComponentLibraryDto>> GetComponentLibrary()
        {
            try
            {
                var library = await _componentService.GetComponentLibraryAsync();
                return Ok(library);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving component library");
                return StatusCode(500, new { Message = "An error occurred while retrieving the component library" });
            }
        }

        /// <summary>
        /// Get all component templates
        /// </summary>
        [HttpGet("templates")]
        public async Task<ActionResult<List<ComponentTemplateDto>>> GetComponentTemplates()
        {
            try
            {
                var templates = await _componentService.GetComponentTemplatesAsync();
                return Ok(templates);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving component templates");
                return StatusCode(500, new { Message = "An error occurred while retrieving component templates" });
            }
        }

        /// <summary>
        /// Get component template by ID
        /// </summary>
        [HttpGet("templates/{id:guid}")]
        public async Task<ActionResult<ComponentTemplateDto>> GetComponentTemplate(Guid id)
        {
            try
            {
                var template = await _componentService.GetComponentTemplateByIdAsync(id);
                return Ok(template);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning("Component template not found: {TemplateId}", id);
                return NotFound(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving component template {TemplateId}", id);
                return StatusCode(500, new { Message = "An error occurred while retrieving the component template" });
            }
        }

        /// <summary>
        /// Get component templates by type
        /// </summary>
        [HttpGet("templates/by-type/{type}")]
        public async Task<ActionResult<List<ComponentTemplateDto>>> GetComponentTemplatesByType(ComponentType type)
        {
            try
            {
                var templates = await _componentService.GetComponentTemplatesByTypeAsync(type);
                return Ok(templates);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving component templates by type {Type}", type);
                return StatusCode(500, new { Message = "An error occurred while retrieving component templates" });
            }
        }

        /// <summary>
        /// Get component templates by category
        /// </summary>
        [HttpGet("templates/by-category/{category}")]
        public async Task<ActionResult<List<ComponentTemplateDto>>> GetComponentTemplatesByCategory(string category)
        {
            try
            {
                var templates = await _componentService.GetComponentTemplatesByCategoryAsync(category);
                return Ok(templates);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving component templates by category {Category}", category);
                return StatusCode(500, new { Message = "An error occurred while retrieving component templates" });
            }
        }

        /// <summary>
        /// Get system component templates
        /// </summary>
        [HttpGet("templates/system")]
        public async Task<ActionResult<List<ComponentTemplateDto>>> GetSystemTemplates()
        {
            try
            {
                var templates = await _componentService.GetSystemTemplatesAsync();
                return Ok(templates);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving system component templates");
                return StatusCode(500, new { Message = "An error occurred while retrieving system templates" });
            }
        }

        /// <summary>
        /// Get custom component templates
        /// </summary>
        [HttpGet("templates/custom")]
        public async Task<ActionResult<List<ComponentTemplateDto>>> GetCustomTemplates()
        {
            try
            {
                var templates = await _componentService.GetCustomTemplatesAsync();
                return Ok(templates);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving custom component templates");
                return StatusCode(500, new { Message = "An error occurred while retrieving custom templates" });
            }
        }

        /// <summary>
        /// Create a new component template
        /// </summary>
        [HttpPost("templates")]
        public async Task<ActionResult<ComponentTemplateDto>> CreateComponentTemplate([FromBody] CreateComponentTemplateDto createComponentTemplateDto)
        {
            try
            {
                var template = await _componentService.CreateComponentTemplateAsync(createComponentTemplateDto);
                return CreatedAtAction(nameof(GetComponentTemplate), new { id = template.Id }, template);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning("Component template creation failed: {Message}", ex.Message);
                return BadRequest(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating component template");
                return StatusCode(500, new { Message = "An error occurred while creating the component template" });
            }
        }

        /// <summary>
        /// Update an existing component template
        /// </summary>
        [HttpPut("templates/{id:guid}")]
        public async Task<ActionResult<ComponentTemplateDto>> UpdateComponentTemplate(Guid id, [FromBody] UpdateComponentTemplateDto updateComponentTemplateDto)
        {
            try
            {
                var template = await _componentService.UpdateComponentTemplateAsync(id, updateComponentTemplateDto);
                return Ok(template);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning("Component template update failed for {TemplateId}: {Message}", id, ex.Message);
                return BadRequest(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating component template {TemplateId}", id);
                return StatusCode(500, new { Message = "An error occurred while updating the component template" });
            }
        }

        /// <summary>
        /// Delete a component template
        /// </summary>
        [HttpDelete("templates/{id:guid}")]
        public async Task<ActionResult> DeleteComponentTemplate(Guid id)
        {
            try
            {
                var success = await _componentService.DeleteComponentTemplateAsync(id);
                if (!success)
                {
                    return NotFound(new { Message = "Component template not found" });
                }
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting component template {TemplateId}", id);
                return StatusCode(500, new { Message = "An error occurred while deleting the component template" });
            }
        }

        /// <summary>
        /// Validate component data against schema
        /// </summary>
        [HttpPost("validate")]
        public async Task<ActionResult<bool>> ValidateComponentData([FromBody] ValidateComponentDto validateComponentDto)
        {
            try
            {
                var isValid = await _componentService.ValidateComponentDataAsync(validateComponentDto.Type, validateComponentDto.Data);
                return Ok(new { IsValid = isValid });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating component data");
                return StatusCode(500, new { Message = "An error occurred while validating component data" });
            }
        }

        /// <summary>
        /// Get default properties for a component type
        /// </summary>
        [HttpGet("defaults/{type}/properties")]
        public async Task<ActionResult<Dictionary<string, object>>> GetDefaultProperties(ComponentType type)
        {
            try
            {
                var properties = await _componentService.GetDefaultPropertiesAsync(type);
                return Ok(properties);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving default properties for {Type}", type);
                return StatusCode(500, new { Message = "An error occurred while retrieving default properties" });
            }
        }

        /// <summary>
        /// Get default styles for a component type
        /// </summary>
        [HttpGet("defaults/{type}/styles")]
        public async Task<ActionResult<Dictionary<string, object>>> GetDefaultStyles(ComponentType type)
        {
            try
            {
                var styles = await _componentService.GetDefaultStylesAsync(type);
                return Ok(styles);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving default styles for {Type}", type);
                return StatusCode(500, new { Message = "An error occurred while retrieving default styles" });
            }
        }
    }

    public class ValidateComponentDto
    {
        public ComponentType Type { get; set; }
        public Dictionary<string, object> Data { get; set; } = new();
    }
}