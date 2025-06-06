using Backend.CMS.Application.DTOs.Users;
using System;
using System.Threading.Tasks;

namespace Backend.CMS.Application.Interfaces.Services
{
    public interface IAuthService
    {
        Task<LoginResponseDto> LoginAsync(LoginDto loginDto);
        Task<LoginResponseDto> RefreshTokenAsync(RefreshTokenDto refreshTokenDto);
        Task<bool> LogoutAsync(string refreshToken);
        Task<bool> RevokeAllSessionsAsync(Guid userId);
        Task<bool> ValidateTokenAsync(string token);
        Task<UserDto> GetCurrentUserAsync();
        Task<bool> ForgotPasswordAsync(string email);
        Task<bool> ResetPasswordAsync(string token, string newPassword);
        Task<bool> Enable2FAAsync(Guid userId);
        Task<bool> Disable2FAAsync(Guid userId);
        Task<string> Generate2FASecretAsync(Guid userId);
        Task<bool> Verify2FACodeAsync(Guid userId, string code);
        Task<List<string>> GenerateRecoveryCodesAsync(Guid userId);
        Task<bool> UseRecoveryCodeAsync(Guid userId, string code);
    }
}