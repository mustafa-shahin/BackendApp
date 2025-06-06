using AutoMapper;
using Backend.CMS.Application.DTOs.Pages;
using Backend.CMS.Application.Interfaces.Services;
using Backend.CMS.Domain.Entities;
using Backend.CMS.Domain.Enums;
using Backend.CMS.Infrastructure.Repositories;

namespace Backend.CMS.Infrastructure.Services
{
    public class PageService : IPageService
    {
        private readonly IPageRepository _pageRepository;
        private readonly IMapper _mapper;

        public PageService(IPageRepository pageRepository, IMapper mapper)
        {
            _pageRepository = pageRepository;
            _mapper = mapper;
        }

        public async Task<PageDto> GetPageByIdAsync(Guid pageId)
        {
            var page = await _pageRepository.GetWithComponentsAsync(pageId);
            if (page == null)
                throw new ArgumentException("Page not found");

            return _mapper.Map<PageDto>(page);
        }

        public async Task<PageDto> GetPageBySlugAsync(string slug)
        {
            var page = await _pageRepository.GetBySlugAsync(slug);
            if (page == null)
                throw new ArgumentException("Page not found");

            return _mapper.Map<PageDto>(page);
        }

        public async Task<List<PageListDto>> GetPagesAsync(int page = 1, int pageSize = 10, string? search = null)
        {
            var pages = string.IsNullOrEmpty(search)
                ? await _pageRepository.GetPagedAsync(page, pageSize)
                : await _pageRepository.SearchPagesAsync(search, page, pageSize);

            return _mapper.Map<List<PageListDto>>(pages);
        }

        public async Task<List<PageDto>> GetPageHierarchyAsync()
        {
            var pages = await _pageRepository.GetPageHierarchyAsync();
            return _mapper.Map<List<PageDto>>(pages);
        }

        public async Task<PageDto> CreatePageAsync(CreatePageDto createPageDto)
        {
            var page = _mapper.Map<Page>(createPageDto);
            await _pageRepository.AddAsync(page);
            await _pageRepository.SaveChangesAsync();
            return _mapper.Map<PageDto>(page);
        }

        public async Task<PageDto> UpdatePageAsync(Guid pageId, UpdatePageDto updatePageDto)
        {
            var page = await _pageRepository.GetByIdAsync(pageId);
            if (page == null)
                throw new ArgumentException("Page not found");

            _mapper.Map(updatePageDto, page);
            _pageRepository.Update(page);
            await _pageRepository.SaveChangesAsync();
            return _mapper.Map<PageDto>(page);
        }

        public async Task<bool> DeletePageAsync(Guid pageId)
        {
            var page = await _pageRepository.GetByIdAsync(pageId);
            if (page == null)
                return false;

            _pageRepository.Remove(page);
            await _pageRepository.SaveChangesAsync();
            return true;
        }

        public async Task<PageDto> SavePageStructureAsync(SavePageStructureDto savePageStructureDto)
        {
            var page = await _pageRepository.GetWithComponentsAsync(savePageStructureDto.PageId);
            if (page == null)
                throw new ArgumentException("Page not found");

            // Clear existing components
            page.Components.Clear();

            // Add new components
            var components = _mapper.Map<List<PageComponent>>(savePageStructureDto.Components);
            foreach (var component in components)
            {
                component.PageId = page.Id;
                page.Components.Add(component);
            }

            _pageRepository.Update(page);
            await _pageRepository.SaveChangesAsync();
            return _mapper.Map<PageDto>(page);
        }

        public async Task<PageDto> PublishPageAsync(Guid pageId)
        {
            var page = await _pageRepository.GetByIdAsync(pageId);
            if (page == null)
                throw new ArgumentException("Page not found");

            page.Status = PageStatus.Published;
            page.PublishedOn = DateTime.UtcNow;
            _pageRepository.Update(page);
            await _pageRepository.SaveChangesAsync();
            return _mapper.Map<PageDto>(page);
        }

        public async Task<PageDto> UnpublishPageAsync(Guid pageId)
        {
            var page = await _pageRepository.GetByIdAsync(pageId);
            if (page == null)
                throw new ArgumentException("Page not found");

            page.Status = PageStatus.Draft;
            _pageRepository.Update(page);
            await _pageRepository.SaveChangesAsync();
            return _mapper.Map<PageDto>(page);
        }

        public async Task<PageDto> DuplicatePageAsync(Guid pageId, string newName)
        {
            var originalPage = await _pageRepository.GetWithComponentsAsync(pageId);
            if (originalPage == null)
                throw new ArgumentException("Page not found");

            var duplicatedPage = new Page
            {
                Name = newName,
                Title = originalPage.Title + " (Copy)",
                Slug = originalPage.Slug + "-copy",
                Description = originalPage.Description,
                MetaTitle = originalPage.MetaTitle,
                MetaDescription = originalPage.MetaDescription,
                MetaKeywords = originalPage.MetaKeywords,
                Status = PageStatus.Draft,
                Template = originalPage.Template,
                Priority = originalPage.Priority,
                ParentPageId = originalPage.ParentPageId
            };

            await _pageRepository.AddAsync(duplicatedPage);
            await _pageRepository.SaveChangesAsync();
            return _mapper.Map<PageDto>(duplicatedPage);
        }

        public async Task<List<PageDto>> GetPublishedPagesAsync()
        {
            var pages = await _pageRepository.GetPublishedPagesAsync();
            return _mapper.Map<List<PageDto>>(pages);
        }

        public async Task<List<PageDto>> GetChildPagesAsync(Guid parentPageId)
        {
            var pages = await _pageRepository.GetChildPagesAsync(parentPageId);
            return _mapper.Map<List<PageDto>>(pages);
        }

        public async Task<bool> ValidateSlugAsync(string slug, Guid? excludePageId = null)
        {
            return !await _pageRepository.SlugExistsAsync(slug, excludePageId);
        }

        public async Task<PageDto> CreatePageVersionAsync(Guid pageId, string? changeNotes = null)
        {
            var page = await _pageRepository.GetByIdAsync(pageId);
            if (page == null)
                throw new ArgumentException("Page not found");

            return _mapper.Map<PageDto>(page);
        }

        public async Task<List<PageVersionDto>> GetPageVersionsAsync(Guid pageId)
        {
            return new List<PageVersionDto>();
        }

        public async Task<PageDto> RestorePageVersionAsync(Guid pageId, Guid versionId)
        {
            var page = await _pageRepository.GetByIdAsync(pageId);
            if (page == null)
                throw new ArgumentException("Page not found");

            return _mapper.Map<PageDto>(page);
        }
    }
}