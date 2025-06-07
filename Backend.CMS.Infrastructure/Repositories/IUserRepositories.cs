using Backend.CMS.Domain.Entities;
using Backend.CMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Backend.CMS.Infrastructure.Repositories
{
    public interface IUserRepository : IRepository<User>
    {
        Task<User?> GetByEmailAsync(string email);
        Task<User?> GetByUsernameAsync(string username);
        Task<IEnumerable<User>> SearchUsersAsync(string searchTerm, int page, int pageSize);
        Task<bool> EmailExistsAsync(string email, Guid? excludeUserId = null);
        Task<bool> UsernameExistsAsync(string username, Guid? excludeUserId = null);
        Task<User?> GetByEmailVerificationTokenAsync(string token);
    }

    public class UserRepository : Repository<User>, IUserRepository
    {
        public UserRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            return await _dbSet
                .FirstOrDefaultAsync(u => u.Email == email && !u.IsDeleted);
        }

        public async Task<User?> GetByUsernameAsync(string username)
        {
            return await _dbSet
                .FirstOrDefaultAsync(u => u.Username == username && !u.IsDeleted);
        }

        public async Task<IEnumerable<User>> SearchUsersAsync(string searchTerm, int page, int pageSize)
        {
            return await _dbSet
                .Where(u => !u.IsDeleted &&
                           (u.FirstName.Contains(searchTerm) ||
                            u.LastName.Contains(searchTerm) ||
                            u.Email.Contains(searchTerm) ||
                            u.Username.Contains(searchTerm)))
                .OrderBy(u => u.FirstName)
                .ThenBy(u => u.LastName)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<bool> EmailExistsAsync(string email, Guid? excludeUserId = null)
        {
            var query = _dbSet.Where(u => u.Email == email && !u.IsDeleted);

            if (excludeUserId.HasValue)
                query = query.Where(u => u.Id != excludeUserId.Value);

            return await query.AnyAsync();
        }

        public async Task<bool> UsernameExistsAsync(string username, Guid? excludeUserId = null)
        {
            var query = _dbSet.Where(u => u.Username == username && !u.IsDeleted);

            if (excludeUserId.HasValue)
                query = query.Where(u => u.Id != excludeUserId.Value);

            return await query.AnyAsync();
        }

        public async Task<User?> GetByEmailVerificationTokenAsync(string token)
        {
            return await _dbSet
                .FirstOrDefaultAsync(u => u.EmailVerificationToken == token && !u.IsDeleted);
        }
    }
}