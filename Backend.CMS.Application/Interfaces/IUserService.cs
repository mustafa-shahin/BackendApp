using Backend.CMS.Application.DTOs.Users;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Backend.CMS.Application.Interfaces.Services
{
    public interface IUserService
    {
        Task<UserDto> GetUserByIdAsync(Guid userId);
        Task<UserDto> GetUserByEmailAsync(string email);
        Task<UserDto> GetUserByUsernameAsync(string username);
        Task<List<UserListDto>> GetUsersAsync(int page = 1, int pageSize = 10, string? search = null);
        Task<UserDto> CreateUserAsync(CreateUserDto createUserDto);
        Task<UserDto> UpdateUserAsync(Guid userId, UpdateUserDto updateUserDto);
        Task<bool> DeleteUserAsync(Guid userId);
        Task<bool> ActivateUserAsync(Guid userId);
        Task<bool> DeactivateUserAsync(Guid userId);
        Task<bool> LockUserAsync(Guid userId);
        Task<bool> UnlockUserAsync(Guid userId);
        Task<bool> ChangePasswordAsync(Guid userId, ChangePasswordDto changePasswordDto);
        Task<bool> ResetPasswordAsync(string email);
        Task<bool> AssignRoleAsync(Guid userId, Guid roleId);
        Task<bool> RemoveRoleAsync(Guid userId, Guid roleId);
        Task<List<RoleDto>> GetUserRolesAsync(Guid userId);
        Task<bool> HasPermissionAsync(Guid userId, string resource, string action);
        Task<bool> ValidateUserCredentialsAsync(string email, string password);
        Task<UserDto> UpdateUserPreferencesAsync(Guid userId, Dictionary<string, object> preferences);
        Task<bool> VerifyEmailAsync(string token);
        Task<bool> SendEmailVerificationAsync(Guid userId);
    }
}