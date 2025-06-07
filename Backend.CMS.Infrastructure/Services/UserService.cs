using AutoMapper;
using Backend.CMS.Application.DTOs.Users;
using Backend.CMS.Application.Interfaces.Services;
using Backend.CMS.Domain.Entities;
using Backend.CMS.Infrastructure.Repositories;
using Microsoft.Extensions.Logging;

namespace Backend.CMS.Infrastructure.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly IRepository<Role> _roleRepository;
        private readonly IRepository<UserRole> _userRoleRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<UserService> _logger;

        public UserService(
            IUserRepository userRepository,
            IRepository<Role> roleRepository,
            IRepository<UserRole> userRoleRepository,
            IMapper mapper,
            ILogger<UserService> logger)
        {
            _userRepository = userRepository;
            _roleRepository = roleRepository;
            _userRoleRepository = userRoleRepository;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<UserDto> GetUserByIdAsync(Guid userId)
        {
            var user = await _userRepository.GetWithRolesAsync(userId);
            return user == null ? throw new ArgumentException("User not found") : _mapper.Map<UserDto>(user);
        }

        public async Task<UserDto> GetUserByEmailAsync(string email)
        {
            var user = await _userRepository.GetByEmailAsync(email);
            return user == null ? throw new ArgumentException("User not found") : _mapper.Map<UserDto>(user);
        }

        public async Task<UserDto> GetUserByUsernameAsync(string username)
        {
            var user = await _userRepository.GetByUsernameAsync(username);
            if (user == null)
                throw new ArgumentException("User not found");

            return _mapper.Map<UserDto>(user);
        }

        public async Task<List<UserListDto>> GetUsersAsync(int page = 1, int pageSize = 10, string? search = null)
        {
            var users = string.IsNullOrEmpty(search)
                ? await _userRepository.GetPagedAsync(page, pageSize)
                : await _userRepository.SearchUsersAsync(search, page, pageSize);

            return _mapper.Map<List<UserListDto>>(users);
        }

        public async Task<UserDto> CreateUserAsync(CreateUserDto createUserDto)
        {
            try
            {
                // Validate unique constraints
                if (await _userRepository.EmailExistsAsync(createUserDto.Email))
                    throw new ArgumentException("Email already exists");

                if (await _userRepository.UsernameExistsAsync(createUserDto.Username))
                    throw new ArgumentException("Username already exists");

                // Create user entity - AutoMapper will handle most properties
                var user = _mapper.Map<User>(createUserDto);
                user.Id = Guid.NewGuid();
                user.CreatedAt = DateTime.UtcNow;
                user.UpdatedAt = DateTime.UtcNow;
                user.IsActive = true;
                user.IsLocked = false;
                user.FailedLoginAttempts = 0;
                // Don't set UserRoles here - it's a navigation property

                await _userRepository.AddAsync(user);
                await _userRepository.SaveChangesAsync();

                // Assign roles separately using the UserRole junction table
                if (createUserDto.RoleIds?.Count > 0)
                {
                    await AssignRolesToUserAsync(user.Id, createUserDto.RoleIds);
                }
                else
                {
                    // Assign default "Viewer" role if no roles specified
                    await AssignDefaultRoleAsync(user.Id);
                }

                // Reload user with roles for response
                var userWithRoles = await _userRepository.GetWithRolesAsync(user.Id);

                _logger.LogInformation("User created successfully: {Email}", createUserDto.Email);
                return _mapper.Map<UserDto>(userWithRoles);
            }
            catch (ArgumentException)
            {
                throw; // Re-throw validation exceptions
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user: {Email}", createUserDto.Email);
                throw new InvalidOperationException("Failed to create user", ex);
            }
        }

        public async Task<UserDto> UpdateUserAsync(Guid userId, UpdateUserDto updateUserDto)
        {
            var user = await _userRepository.GetByIdAsync(userId) ?? throw new ArgumentException("User not found");

            if (await _userRepository.EmailExistsAsync(updateUserDto.Email, userId))
                throw new ArgumentException("Email already exists");

            if (await _userRepository.UsernameExistsAsync(updateUserDto.Username, userId))
                throw new ArgumentException("Username already exists");

            _mapper.Map(updateUserDto, user);
            user.UpdatedAt = DateTime.UtcNow;

            _userRepository.Update(user);
            await _userRepository.SaveChangesAsync();

            // Update roles if specified
            if (updateUserDto.RoleIds?.Any() == true)
            {
                await UpdateUserRolesAsync(userId, updateUserDto.RoleIds);
            }

            var updatedUser = await _userRepository.GetWithRolesAsync(userId);
            return _mapper.Map<UserDto>(updatedUser);
        }

        public async Task<bool> DeleteUserAsync(Guid userId)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
                return false;

            // Soft delete
            user.IsDeleted = true;
            user.DeletedAt = DateTime.UtcNow;
            user.UpdatedAt = DateTime.UtcNow;

            _userRepository.Update(user);
            await _userRepository.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ActivateUserAsync(Guid userId)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
                return false;

            user.IsActive = true;
            user.UpdatedAt = DateTime.UtcNow;
            _userRepository.Update(user);
            await _userRepository.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeactivateUserAsync(Guid userId)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
                return false;

            user.IsActive = false;
            user.UpdatedAt = DateTime.UtcNow;
            _userRepository.Update(user);
            await _userRepository.SaveChangesAsync();
            return true;
        }

        public async Task<bool> LockUserAsync(Guid userId)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
                return false;

            user.IsLocked = true;
            user.LockoutEnd = DateTime.UtcNow.AddDays(30);
            user.UpdatedAt = DateTime.UtcNow;
            _userRepository.Update(user);
            await _userRepository.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UnlockUserAsync(Guid userId)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
                return false;

            user.IsLocked = false;
            user.LockoutEnd = null;
            user.FailedLoginAttempts = 0;
            user.UpdatedAt = DateTime.UtcNow;
            _userRepository.Update(user);
            await _userRepository.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ChangePasswordAsync(Guid userId, ChangePasswordDto changePasswordDto)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
                return false;

            if (!BCrypt.Net.BCrypt.Verify(changePasswordDto.CurrentPassword, user.PasswordHash))
                return false;

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(changePasswordDto.NewPassword);
            user.PasswordChangedAt = DateTime.UtcNow;
            user.UpdatedAt = DateTime.UtcNow;
            _userRepository.Update(user);
            await _userRepository.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ResetPasswordAsync(string email)
        {
            var user = await _userRepository.GetByEmailAsync(email);
            if (user == null)
                return true; // Don't reveal if email exists

            // Generate reset token and send email
            return true;
        }

        public async Task<bool> AssignRoleAsync(Guid userId, Guid roleId)
        {
            try
            {
                var user = await _userRepository.GetByIdAsync(userId);
                var role = await _roleRepository.GetByIdAsync(roleId);

                if (user == null || role == null)
                    return false;

                // Check if role is already assigned
                var existingUserRole = await _userRoleRepository.FirstOrDefaultAsync(ur =>
                    ur.UserId == userId && ur.RoleId == roleId && ur.IsActive);

                if (existingUserRole != null)
                    return true; // Already assigned

                var userRole = new UserRole
                {
                    UserId = userId,
                    RoleId = roleId,
                    AssignedAt = DateTime.UtcNow,
                    AssignedBy = "System",
                    IsActive = true
                };

                await _userRoleRepository.AddAsync(userRole);
                await _userRoleRepository.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning role {RoleId} to user {UserId}", roleId, userId);
                return false;
            }
        }

        public async Task<bool> RemoveRoleAsync(Guid userId, Guid roleId)
        {
            try
            {
                var userRole = await _userRoleRepository.FirstOrDefaultAsync(ur =>
                    ur.UserId == userId && ur.RoleId == roleId && ur.IsActive);

                if (userRole == null)
                    return false;

                userRole.IsActive = false;
                _userRoleRepository.Update(userRole);
                await _userRoleRepository.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing role {RoleId} from user {UserId}", roleId, userId);
                return false;
            }
        }

        public async Task<List<RoleDto>> GetUserRolesAsync(Guid userId)
        {
            var roles = await _userRepository.GetUserRolesAsync(userId);
            return _mapper.Map<List<RoleDto>>(roles);
        }

        public async Task<bool> HasPermissionAsync(Guid userId, string resource, string action)
        {
            return await _userRepository.HasPermissionAsync(userId, resource, action);
        }

        public async Task<bool> ValidateUserCredentialsAsync(string email, string password)
        {
            var user = await _userRepository.GetByEmailAsync(email);
            if (user == null)
                return false;

            return BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
        }

        public async Task<UserDto> UpdateUserPreferencesAsync(Guid userId, Dictionary<string, object> preferences)
        {
            var user = await _userRepository.GetByIdAsync(userId) ?? throw new ArgumentException("User not found");
            user.Preferences = preferences;
            user.UpdatedAt = DateTime.UtcNow;
            _userRepository.Update(user);
            await _userRepository.SaveChangesAsync();

            return _mapper.Map<UserDto>(user);
        }

        public async Task<bool> VerifyEmailAsync(string token)
        {
            var user = await _userRepository.GetByEmailVerificationTokenAsync(token);
            if (user == null)
                return false;

            user.EmailVerifiedAt = DateTime.UtcNow;
            user.EmailVerificationToken = null;
            user.UpdatedAt = DateTime.UtcNow;
            _userRepository.Update(user);
            await _userRepository.SaveChangesAsync();
            return true;
        }

        public async Task<bool> SendEmailVerificationAsync(Guid userId)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
                return false;

            // Generate verification token and send email
            return true;
        }

        private async Task AssignRolesToUserAsync(Guid userId, List<Guid> roleIds)
        {
            foreach (var roleId in roleIds)
            {
                await AssignRoleAsync(userId, roleId);
            }
        }

        private async Task AssignDefaultRoleAsync(Guid userId)
        {
            // Find default "Viewer" role
            var roles = await _roleRepository.FindAsync(r => r.NormalizedName == "VIEWER");
            var defaultRole = roles.FirstOrDefault();

            if (defaultRole != null)
            {
                await AssignRoleAsync(userId, defaultRole.Id);
            }
        }

        private async Task UpdateUserRolesAsync(Guid userId, List<Guid> roleIds)
        {
            // Deactivate all current roles
            var currentUserRoles = await _userRoleRepository.FindAsync(ur => ur.UserId == userId && ur.IsActive);
            foreach (var userRole in currentUserRoles)
            {
                userRole.IsActive = false;
                _userRoleRepository.Update(userRole);
            }

            // Assign new roles
            await AssignRolesToUserAsync(userId, roleIds);
            await _userRoleRepository.SaveChangesAsync();
        }
    }
}