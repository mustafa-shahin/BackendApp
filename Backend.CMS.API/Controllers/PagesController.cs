using Backend.CMS.Application.DTOs.Pages;
using Backend.CMS.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.CMS.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PagesController : ControllerBase
    {
        private readonly IPageService _pageService;
        private readonly ILogger<PagesController> _logger;

        public PagesController(IPageService pageService, ILogger<PagesController> logger)
        {
            _pageService = pageService;
            _logger = logger;
        }

        /// <summary>
        /// Get page by ID with full structure
        /// </summary>
        [HttpGet("{id:guid}")]
        public async Task<ActionResult<PageDto>> GetPage(Guid id)
        {
            try
            {
                var page = await _pageService.GetPageByIdAsync(id);
                return Ok(page);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning("Page not found: {PageId}", id);
                return NotFound(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving page {PageId}", id);
                return StatusCode(500, new { Message = "An error occurred while retrieving the page" });
            }
        }

        /// <summary>
        /// Get page by slug (public endpoint for frontend)
        /// </summary>
        [HttpGet("by-slug/{slug}")]
        [AllowAnonymous]
        public async Task<ActionResult<PageDto>> GetPageBySlug(string slug)
        {
            try
            {
                var page = await _pageService.GetPageBySlugAsync(slug);
                return Ok(page);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning("Page not found by slug: {Slug}", slug);
                return NotFound(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving page by slug {Slug}", slug);
                return StatusCode(500, new { Message = "An error occurred while retrieving the page" });
            }
        }

        /// <summary>
        /// Get paginated list of pages for admin
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<PagedResult<PageListDto>>> GetPages(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? search = null)
        {
            try
            {
                var pages = await _pageService.GetPagesAsync(page, pageSize, search);
                // You would typically return a paged result with total count
                return Ok(new PagedResult<PageListDto>
                {
                    Items = pages,
                    Page = page,
                    PageSize = pageSize,
                    TotalCount = pages.Count // This should come from the service
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving pages");
                return StatusCode(500, new { Message = "An error occurred while retrieving pages" });
            }
        }

        /// <summary>
        /// Get page hierarchy for navigation
        /// </summary>
        [HttpGet("hierarchy")]
        public async Task<ActionResult<List<PageDto>>> GetPageHierarchy()
        {
            try
            {
                var hierarchy = await _pageService.GetPageHierarchyAsync();
                return Ok(hierarchy);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving page hierarchy");
                return StatusCode(500, new { Message = "An error occurred while retrieving page hierarchy" });
            }
        }

        /// <summary>
        /// Get published pages (public endpoint)
        /// </summary>
        [HttpGet("published")]
        [AllowAnonymous]
        public async Task<ActionResult<List<PageDto>>> GetPublishedPages()
        {
            try
            {
                var pages = await _pageService.GetPublishedPagesAsync();
                return Ok(pages);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving published pages");
                return StatusCode(500, new { Message = "An error occurred while retrieving published pages" });
            }
        }

        /// <summary>
        /// Create a new page
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<PageDto>> CreatePage([FromBody] CreatePageDto createPageDto)
        {
            try
            {
                var page = await _pageService.CreatePageAsync(createPageDto);
                return CreatedAtAction(nameof(GetPage), new { id = page.Id }, page);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning("Page creation failed: {Message}", ex.Message);
                return BadRequest(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating page");
                return StatusCode(500, new { Message = "An error occurred while creating the page" });
            }
        }

        /// <summary>
        /// Update an existing page
        /// </summary>
        [HttpPut("{id:guid}")]
        public async Task<ActionResult<PageDto>> UpdatePage(Guid id, [FromBody] UpdatePageDto updatePageDto)
        {
            try
            {
                var page = await _pageService.UpdatePageAsync(id, updatePageDto);
                return Ok(page);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning("Page update failed for {PageId}: {Message}", id, ex.Message);
                return BadRequest(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating page {PageId}", id);
                return StatusCode(500, new { Message = "An error occurred while updating the page" });
            }
        }

        /// <summary>
        /// Delete a page
        /// </summary>
        [HttpDelete("{id:guid}")]
        public async Task<ActionResult> DeletePage(Guid id)
        {
            try
            {
                var success = await _pageService.DeletePageAsync(id);
                if (!success)
                {
                    return NotFound(new { Message = "Page not found" });
                }
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting page {PageId}", id);
                return StatusCode(500, new { Message = "An error occurred while deleting the page" });
            }
        }

        /// <summary>
        /// Save page structure (components and layout)
        /// </summary>
        [HttpPost("{id:guid}/structure")]
        public async Task<ActionResult<PageDto>> SavePageStructure(Guid id, [FromBody] SavePageStructureDto savePageStructureDto)
        {
            try
            {
                savePageStructureDto.PageId = id; // Ensure consistency
                var page = await _pageService.SavePageStructureAsync(savePageStructureDto);
                return Ok(page);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning("Page structure save failed for {PageId}: {Message}", id, ex.Message);
                return BadRequest(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving page structure for {PageId}", id);
                return StatusCode(500, new { Message = "An error occurred while saving page structure" });
            }
        }

        /// <summary>
        /// Publish a page
        /// </summary>
        [HttpPost("{id:guid}/publish")]
        public async Task<ActionResult<PageDto>> PublishPage(Guid id)
        {
            try
            {
                var page = await _pageService.PublishPageAsync(id);
                return Ok(page);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning("Page publish failed for {PageId}: {Message}", id, ex.Message);
                return BadRequest(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error publishing page {PageId}", id);
                return StatusCode(500, new { Message = "An error occurred while publishing the page" });
            }
        }

        /// <summary>
        /// Unpublish a page
        /// </summary>
        [HttpPost("{id:guid}/unpublish")]
        public async Task<ActionResult<PageDto>> UnpublishPage(Guid id)
        {
            try
            {
                var page = await _pageService.UnpublishPageAsync(id);
                return Ok(page);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning("Page unpublish failed for {PageId}: {Message}", id, ex.Message);
                return BadRequest(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unpublishing page {PageId}", id);
                return StatusCode(500, new { Message = "An error occurred while unpublishing the page" });
            }
        }

        /// <summary>
        /// Duplicate a page
        /// </summary>
        [HttpPost("{id:guid}/duplicate")]
        public async Task<ActionResult<PageDto>> DuplicatePage(Guid id, [FromBody] DuplicatePageDto duplicatePageDto)
        {
            try
            {
                var page = await _pageService.DuplicatePageAsync(id, duplicatePageDto.NewName);
                return CreatedAtAction(nameof(GetPage), new { id = page.Id }, page);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning("Page duplication failed for {PageId}: {Message}", id, ex.Message);
                return BadRequest(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error duplicating page {PageId}", id);
                return StatusCode(500, new { Message = "An error occurred while duplicating the page" });
            }
        }

        /// <summary>
        /// Validate page slug availability
        /// </summary>
        [HttpGet("validate-slug")]
        public async Task<ActionResult<bool>> ValidateSlug([FromQuery] string slug, [FromQuery] Guid? excludePageId = null)
        {
            try
            {
                var isValid = await _pageService.ValidateSlugAsync(slug, excludePageId);
                return Ok(new { IsValid = isValid });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating slug {Slug}", slug);
                return StatusCode(500, new { Message = "An error occurred while validating the slug" });
            }
        }

        /// <summary>
        /// Create page version
        /// </summary>
        [HttpPost("{id:guid}/versions")]
        public async Task<ActionResult<PageDto>> CreatePageVersion(Guid id, [FromBody] CreatePageVersionDto createVersionDto)
        {
            try
            {
                var page = await _pageService.CreatePageVersionAsync(id, createVersionDto.ChangeNotes);
                return Ok(page);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning("Page version creation failed for {PageId}: {Message}", id, ex.Message);
                return BadRequest(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating page version for {PageId}", id);
                return StatusCode(500, new { Message = "An error occurred while creating page version" });
            }
        }

        /// <summary>
        /// Get page versions
        /// </summary>
        [HttpGet("{id:guid}/versions")]
        public async Task<ActionResult<List<PageVersionDto>>> GetPageVersions(Guid id)
        {
            try
            {
                var versions = await _pageService.GetPageVersionsAsync(id);
                return Ok(versions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving page versions for {PageId}", id);
                return StatusCode(500, new { Message = "An error occurred while retrieving page versions" });
            }
        }

        /// <summary>
        /// Restore page from version
        /// </summary>
        [HttpPost("{id:guid}/versions/{versionId:guid}/restore")]
        public async Task<ActionResult<PageDto>> RestorePageVersion(Guid id, Guid versionId)
        {
            try
            {
                var page = await _pageService.RestorePageVersionAsync(id, versionId);
                return Ok(page);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning("Page version restore failed for {PageId}, version {VersionId}: {Message}", id, versionId, ex.Message);
                return BadRequest(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error restoring page version for {PageId}, version {VersionId}", id, versionId);
                return StatusCode(500, new { Message = "An error occurred while restoring page version" });
            }
        }
    }

    public class PagedResult<T>
    {
        public List<T> Items { get; set; } = new();
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    }

    public class DuplicatePageDto
    {
        public string NewName { get; set; } = string.Empty;
    }

    public class CreatePageVersionDto
    {
        public string? ChangeNotes { get; set; }
    }
}