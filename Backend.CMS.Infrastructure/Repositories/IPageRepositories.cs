// File: Backend.CMS.Infrastructure/Repositories/PageRepository.cs
using Backend.CMS.Domain.Entities;
using Backend.CMS.Domain.Enums;
using Backend.CMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Backend.CMS.Infrastructure.Repositories
{
    public interface IPageRepository : IRepository<Page>
    {
        Task<Page?> GetBySlugAsync(string slug);
        Task<IEnumerable<Page>> GetPublishedPagesAsync();
        Task<IEnumerable<Page>> GetPageHierarchyAsync();
        Task<IEnumerable<Page>> GetChildPagesAsync(Guid parentPageId);
        Task<Page?> GetWithComponentsAsync(Guid pageId);
        Task<bool> SlugExistsAsync(string slug, Guid? excludePageId = null);
        Task<IEnumerable<Page>> SearchPagesAsync(string searchTerm, int page, int pageSize);
        Task<Page?> GetWithVersionsAsync(Guid pageId);
    }

    public class PageRepository : Repository<Page>, IPageRepository
    {
        public PageRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<Page?> GetBySlugAsync(string slug)
        {
            return await _dbSet
                .Include(p => p.Components.Where(c => !c.IsDeleted))
                    .ThenInclude(c => c.ChildComponents.Where(cc => !cc.IsDeleted))
                .FirstOrDefaultAsync(p => p.Slug == slug && !p.IsDeleted);
        }

        public async Task<IEnumerable<Page>> GetPublishedPagesAsync()
        {
            return await _dbSet
                .Where(p => p.Status == PageStatus.Published && !p.IsDeleted)
                .OrderBy(p => p.Priority)
                .ThenBy(p => p.Name)
                .ToListAsync();
        }

        public async Task<IEnumerable<Page>> GetPageHierarchyAsync()
        {
            return await _dbSet
                .Include(p => p.ChildPages.Where(cp => !cp.IsDeleted))
                .Where(p => p.ParentPageId == null && !p.IsDeleted)
                .OrderBy(p => p.Priority)
                .ThenBy(p => p.Name)
                .ToListAsync();
        }

        public async Task<IEnumerable<Page>> GetChildPagesAsync(Guid parentPageId)
        {
            return await _dbSet
                .Where(p => p.ParentPageId == parentPageId && !p.IsDeleted)
                .OrderBy(p => p.Priority)
                .ThenBy(p => p.Name)
                .ToListAsync();
        }

        public async Task<Page?> GetWithComponentsAsync(Guid pageId)
        {
            return await _dbSet
                .Include(p => p.Components.Where(c => !c.IsDeleted))
                    .ThenInclude(c => c.ChildComponents.Where(cc => !cc.IsDeleted))
                .FirstOrDefaultAsync(p => p.Id == pageId && !p.IsDeleted);
        }

        public async Task<bool> SlugExistsAsync(string slug, Guid? excludePageId = null)
        {
            var query = _dbSet.Where(p => p.Slug == slug && !p.IsDeleted);

            if (excludePageId.HasValue)
                query = query.Where(p => p.Id != excludePageId.Value);

            return await query.AnyAsync();
        }

        public async Task<IEnumerable<Page>> SearchPagesAsync(string searchTerm, int page, int pageSize)
        {
            return await _dbSet
                .Where(p => !p.IsDeleted &&
                           (p.Name.Contains(searchTerm) ||
                            p.Title.Contains(searchTerm) ||
                            p.Description!.Contains(searchTerm)))
                .OrderBy(p => p.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<Page?> GetWithVersionsAsync(Guid pageId)
        {
            return await _context.Pages
                .Include(p => p.Components.Where(c => !c.IsDeleted))
                .FirstOrDefaultAsync(p => p.Id == pageId && !p.IsDeleted);
        }
    }
}