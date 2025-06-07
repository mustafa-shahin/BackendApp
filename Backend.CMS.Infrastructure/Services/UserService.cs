using AutoMapper;
using Backend.CMS.Application.DTOs.Users;
using Backend.CMS.Application.Interfaces.Services;
using Backend.CMS.Domain.Entities;
using Backend.CMS.Domain.Enums;
using Backend.CMS.Infrastructure.Repositories;
using Microsoft.Extensions.Logging;

namespace Backend.CMS.Infrastructure.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<UserService> _logger;

        public UserService(
            IUserRepository userRepository,
            IMapper mapper,
            ILogger<UserService> logger)
        {
            _userRepository = userRepository;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<UserDto> GetUserByIdAsync(Guid userId)
        {
            var user = await _userRepository.GetByIdAsync(userId);
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

                // Check if this is the first user - if so, make them admin
                var existingUserCount = await _userRepository.CountAsync();
                if (existingUserCount == 0)
                {
                    createUserDto.Role = UserRole.Admin;
                    _logger.LogInformation("Creating first user as administrator: {Email}", createUserDto.Email);
                }

                // Create user entity
                var user = _mapper.Map<User>(createUserDto);
                user.Id = Guid.NewGuid();
                user.CreatedAt = DateTime.UtcNow;
                user.UpdatedAt = DateTime.UtcNow;
                user.IsActive = true;
                user.IsLocked = false;
                user.FailedLoginAttempts = 0;

                await _userRepository.AddAsync(user);
                await _userRepository.SaveChangesAsync();

                _logger.LogInformation("User created successfully: {Email} with role {Role}", createUserDto.Email, user.Role);
                return _mapper.Map<UserDto>(user);
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

            _logger.LogInformation("User updated: {Email} with role {Role}", user.Email, user.Role);
            return _mapper.Map<UserDto>(user);
        }

        public async Task<UserDto> ChangeUserRoleAsync(Guid userId, ChangeUserRoleDto changeRoleDto)
        {
            var user = await _userRepository.GetByIdAsync(userId) ?? throw new ArgumentException("User not found");

            // Prevent changing role if this is the last admin
            if (user.Role == UserRole.Admin && changeRoleDto.Role != UserRole.Admin)
            {
                var adminCount = await _userRepository.CountAsync(u => u.Role == UserRole.Admin && u.IsActive && !u.IsDeleted);
                if (adminCount <= 1)
                {
                    throw new InvalidOperationException("Cannot remove admin role from the last administrator");
                }
            }

            var oldRole = user.Role;
            user.Role = changeRoleDto.Role;
            user.UpdatedAt = DateTime.UtcNow;

            _userRepository.Update(user);
            await _userRepository.SaveChangesAsync();

            _logger.LogInformation("User role changed: {Email} from {OldRole} to {NewRole}", user.Email, oldRole, user.Role);
            return _mapper.Map<UserDto>(user);
        }

        public async Task<bool> DeleteUserAsync(Guid userId)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
                return false;

            // Prevent deleting the last admin
            if (user.Role == UserRole.Admin)
            {
                var adminCount = await _userRepository.CountAsync(u => u.Role == UserRole.Admin && u.IsActive && !u.IsDeleted);
                if (adminCount <= 1)
                {
                    throw new InvalidOperationException("Cannot delete the last administrator");
                }
            }

            // Soft delete
            user.IsDeleted = true;
            user.DeletedAt = DateTime.UtcNow;
            user.UpdatedAt = DateTime.UtcNow;

            _userRepository.Update(user);
            await _userRepository.SaveChangesAsync();

            _logger.LogInformation("User deleted: {Email}", user.Email);
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

            _logger.LogInformation("User activated: {Email}", user.Email);
            return true;
        }

        public async Task<bool> DeactivateUserAsync(Guid userId)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
                return false;

            // Prevent deactivating the last admin
            if (user.Role == UserRole.Admin)
            {
                var activeAdminCount = await _userRepository.CountAsync(u => u.Role == UserRole.Admin && u.IsActive && !u.IsDeleted);
                if (activeAdminCount <= 1)
                {
                    throw new InvalidOperationException("Cannot deactivate the last administrator");
                }
            }

            user.IsActive = false;
            user.UpdatedAt = DateTime.UtcNow;
            _userRepository.Update(user);
            await _userRepository.SaveChangesAsync();

            _logger.LogInformation("User deactivated: {Email}", user.Email);
            return true;
        }

        public async Task<bool> LockUserAsync(Guid userId)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
                return false;

            // Prevent locking the last admin
            if (user.Role == UserRole.Admin)
            {
                var unlockedAdminCount = await _userRepository.CountAsync(u => u.Role == UserRole.Admin && u.IsActive && !u.IsLocked && !u.IsDeleted);
                if (unlockedAdminCount <= 1)
                {
                    throw new InvalidOperationException("Cannot lock the last administrator");
                }
            }

            user.IsLocked = true;
            user.LockoutEnd = DateTime.UtcNow.AddDays(30);
            user.UpdatedAt = DateTime.UtcNow;
            _userRepository.Update(user);
            await _userRepository.SaveChangesAsync();

            _logger.LogInformation("User locked: {Email}", user.Email);
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

            _logger.LogInformation("User unlocked: {Email}", user.Email);
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

            _logger.LogInformation("Password changed for user: {Email}", user.Email);
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

        public async Task<bool> HasPermissionAsync(Guid userId, string resource, string action)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null || !user.IsActive || user.IsLocked)
                return false;

            // Admin has all permissions
            if (user.Role == UserRole.Admin)
                return true;

            // Customer has limited permissions
            if (user.Role == UserRole.Customer)
            {
                // Define customer permissions here
                var customerPermissions = new[]
                {
                    "Pages.View", // Can view public pages
                    "Profile.View", "Profile.Update" // Can manage their own profile
                };

                var permission = $"{resource}.{action}";
                return customerPermissions.Contains(permission);
            }

            return false;
        }

        public async Task<List<UserRoleInfo>> GetAvailableRolesAsync()
        {
            await Task.CompletedTask; // Make async
            return Enum.GetValues<UserRole>().Select(role => new UserRoleInfo
            {
                Value = (int)role,
                Name = role.ToString(),
                Description = role switch
                {
                    UserRole.Admin => "Full system access - can manage all users, pages, and settings",
                    UserRole.Customer => "Limited access - can view public pages and manage own profile",
                    _ => "Unknown role"
                }
            }).ToList();
        }

        public async Task<bool> CanAccessPageAsync(Guid userId, PageAccessLevel pageAccessLevel)
        {
            if (pageAccessLevel == PageAccessLevel.Public)
                return true;

            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null || !user.IsActive || user.IsLocked)
                return false;

            if (pageAccessLevel == PageAccessLevel.LoggedInOnly)
                return true; // Any logged-in user can access

            if (pageAccessLevel == PageAccessLevel.AdminOnly)
                return user.Role == UserRole.Admin;

            return false;
        }
    }
}