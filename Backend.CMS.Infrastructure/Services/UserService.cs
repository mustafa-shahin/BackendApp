using AutoMapper;
using Backend.CMS.Application.DTOs.Users;
using Backend.CMS.Application.Interfaces.Services;
using Backend.CMS.Domain.Entities;
using Backend.CMS.Infrastructure.Repositories;

namespace Backend.CMS.Infrastructure.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly IMapper _mapper;

        public UserService(IUserRepository userRepository, IMapper mapper)
        {
            _userRepository = userRepository;
            _mapper = mapper;
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
            if (await _userRepository.EmailExistsAsync(createUserDto.Email))
                throw new ArgumentException("Email already exists");

            if (await _userRepository.UsernameExistsAsync(createUserDto.Username))
                throw new ArgumentException("Username already exists");

            var user = _mapper.Map<User>(createUserDto);
            await _userRepository.AddAsync(user);
            await _userRepository.SaveChangesAsync();

            return _mapper.Map<UserDto>(user);
        }

        public async Task<UserDto> UpdateUserAsync(Guid userId, UpdateUserDto updateUserDto)
        {
            var user = await _userRepository.GetByIdAsync(userId) ?? throw new ArgumentException("User not found");
            if (await _userRepository.EmailExistsAsync(updateUserDto.Email, userId))
                throw new ArgumentException("Email already exists");

            if (await _userRepository.UsernameExistsAsync(updateUserDto.Username, userId))
                throw new ArgumentException("Username already exists");

            _mapper.Map(updateUserDto, user);
            _userRepository.Update(user);
            await _userRepository.SaveChangesAsync();

            return _mapper.Map<UserDto>(user);
        }

        public async Task<bool> DeleteUserAsync(Guid userId)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
                return false;

            _userRepository.Remove(user);
            await _userRepository.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ActivateUserAsync(Guid userId)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
                return false;

            user.IsActive = true;
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
            // Implementation for assigning role
            return true;
        }

        public async Task<bool> RemoveRoleAsync(Guid userId, Guid roleId)
        {
            // Implementation for removing role
            return true;
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
    }
}
