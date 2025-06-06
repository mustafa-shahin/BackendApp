using Backend.CMS.Application.DTOs.Pages;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Backend.CMS.Application.Interfaces.Services
{
    public interface IPageService
    {
        Task<PageDto> GetPageByIdAsync(Guid pageId);
        Task<PageDto> GetPageBySlugAsync(string slug);
        Task<List<PageListDto>> GetPagesAsync(int page = 1, int pageSize = 10, string? search = null);
        Task<List<PageDto>> GetPageHierarchyAsync();
        Task<PageDto> CreatePageAsync(CreatePageDto createPageDto);
        Task<PageDto> UpdatePageAsync(Guid pageId, UpdatePageDto updatePageDto);
        Task<bool> DeletePageAsync(Guid pageId);
        Task<PageDto> SavePageStructureAsync(SavePageStructureDto savePageStructureDto);
        Task<PageDto> PublishPageAsync(Guid pageId);
        Task<PageDto> UnpublishPageAsync(Guid pageId);
        Task<PageDto> DuplicatePageAsync(Guid pageId, string newName);
        Task<List<PageDto>> GetPublishedPagesAsync();
        Task<List<PageDto>> GetChildPagesAsync(Guid parentPageId);
        Task<bool> ValidateSlugAsync(string slug, Guid? excludePageId = null);
        Task<PageDto> CreatePageVersionAsync(Guid pageId, string? changeNotes = null);
        Task<List<PageVersionDto>> GetPageVersionsAsync(Guid pageId);
        Task<PageDto> RestorePageVersionAsync(Guid pageId, Guid versionId);
    }

    public class PageVersionDto
    {
        public Guid Id { get; set; }
        public int VersionNumber { get; set; }
        public string? ChangeNotes { get; set; }
        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
    }
}