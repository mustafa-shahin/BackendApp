using AutoMapper;
using Backend.CMS.Application.DTOs.Users;
using Backend.CMS.Application.Interfaces.Services;
using Backend.CMS.Domain.Entities;
using Backend.CMS.Infrastructure.Repositories;
using BCrypt.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Backend.CMS.Infrastructure.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly IRepository<UserSession> _sessionRepository;
        private readonly IConfiguration _configuration;
        private readonly IMapper _mapper;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AuthService(
            IUserRepository userRepository,
            IRepository<UserSession> sessionRepository,
            IConfiguration configuration,
            IMapper mapper,
            IHttpContextAccessor httpContextAccessor)
        {
            _userRepository = userRepository;
            _sessionRepository = sessionRepository;
            _configuration = configuration;
            _mapper = mapper;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<LoginResponseDto> LoginAsync(LoginDto loginDto)
        {
            var user = await _userRepository.GetByEmailAsync(loginDto.Email);

            if (user == null || !BCrypt.Net.BCrypt.Verify(loginDto.Password, user.PasswordHash))
            {
                throw new UnauthorizedAccessException("Invalid email or password");
            }

            if (!user.IsActive)
            {
                throw new UnauthorizedAccessException("Account is deactivated");
            }

            if (user.IsLocked)
            {
                if (user.LockoutEnd.HasValue && user.LockoutEnd > DateTime.UtcNow)
                {
                    throw new UnauthorizedAccessException("Account is locked");
                }

                // Unlock if lockout period has expired
                user.IsLocked = false;
                user.LockoutEnd = null;
                user.FailedLoginAttempts = 0;
                _userRepository.Update(user);
                await _userRepository.SaveChangesAsync();
            }

            // Check for 2FA
            if (user.TwoFactorEnabled && string.IsNullOrEmpty(loginDto.TwoFactorCode))
            {
                return new LoginResponseDto
                {
                    RequiresTwoFactor = true
                };
            }

            if (user.TwoFactorEnabled && !string.IsNullOrEmpty(loginDto.TwoFactorCode))
            {
                var isValidCode = await Verify2FACodeAsync(user.Id, loginDto.TwoFactorCode);
                if (!isValidCode)
                {
                    throw new UnauthorizedAccessException("Invalid two-factor authentication code");
                }
            }

            // Reset failed login attempts
            user.FailedLoginAttempts = 0;
            user.LastLoginAt = DateTime.UtcNow;
            _userRepository.Update(user);

            // Generate tokens
            var accessToken = GenerateAccessToken(user);
            var refreshToken = GenerateRefreshToken();

            // Save session
            var session = new UserSession
            {
                UserId = user.Id,
                RefreshToken = refreshToken,
                IpAddress = GetClientIpAddress(),
                UserAgent = GetUserAgent(),
                ExpiresAt = DateTime.UtcNow.AddDays(GetRefreshTokenExpiryDays())
            };

            await _sessionRepository.AddAsync(session);
            await _sessionRepository.SaveChangesAsync();

            var userDto = _mapper.Map<UserDto>(user);

            return new LoginResponseDto
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresAt = DateTime.UtcNow.AddMinutes(GetAccessTokenExpiryMinutes()),
                User = userDto,
                RequiresTwoFactor = false
            };
        }

        public async Task<LoginResponseDto> RefreshTokenAsync(RefreshTokenDto refreshTokenDto)
        {
            var session = await _sessionRepository.FirstOrDefaultAsync(s =>
                s.RefreshToken == refreshTokenDto.RefreshToken &&
                !s.IsRevoked &&
                s.ExpiresAt > DateTime.UtcNow);

            if (session == null)
            {
                throw new UnauthorizedAccessException("Invalid refresh token");
            }

            var user = await _userRepository.GetByIdAsync(session.UserId);
            if (user == null || !user.IsActive)
            {
                throw new UnauthorizedAccessException("User not found or inactive");
            }

            // Generate new tokens
            var accessToken = GenerateAccessToken(user);
            var newRefreshToken = GenerateRefreshToken();

            // Update session
            session.RefreshToken = newRefreshToken;
            session.ExpiresAt = DateTime.UtcNow.AddDays(GetRefreshTokenExpiryDays());
            _sessionRepository.Update(session);
            await _sessionRepository.SaveChangesAsync();

            var userDto = _mapper.Map<UserDto>(user);

            return new LoginResponseDto
            {
                AccessToken = accessToken,
                RefreshToken = newRefreshToken,
                ExpiresAt = DateTime.UtcNow.AddMinutes(GetAccessTokenExpiryMinutes()),
                User = userDto
            };
        }

        public async Task<bool> LogoutAsync(string refreshToken)
        {
            var session = await _sessionRepository.FirstOrDefaultAsync(s => s.RefreshToken == refreshToken);
            if (session != null)
            {
                session.IsRevoked = true;
                _sessionRepository.Update(session);
                await _sessionRepository.SaveChangesAsync();
                return true;
            }
            return false;
        }

        public async Task<bool> RevokeAllSessionsAsync(Guid userId)
        {
            var sessions = await _sessionRepository.FindAsync(s => s.UserId == userId && !s.IsRevoked);
            foreach (var session in sessions)
            {
                session.IsRevoked = true;
                _sessionRepository.Update(session);
            }
            await _sessionRepository.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ValidateTokenAsync(string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.UTF8.GetBytes(GetJwtSecretKey());

                tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = GetJwtIssuer(),
                    ValidateAudience = true,
                    ValidAudience = GetJwtAudience(),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                }, out SecurityToken validatedToken);

                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<UserDto> GetCurrentUserAsync()
        {
            var userId = GetCurrentUserId();
            var user = await _userRepository.GetWithRolesAsync(userId);

            if (user == null)
            {
                throw new UnauthorizedAccessException("User not found");
            }

            return _mapper.Map<UserDto>(user);
        }

        public async Task<bool> ForgotPasswordAsync(string email)
        {
            var user = await _userRepository.GetByEmailAsync(email);
            if (user == null)
            {
                return true; // Don't reveal if email exists
            }

            // Generate reset token (implement email sending logic)
            var resetToken = GenerateSecureToken();
            // Store reset token and send email
            // Implementation depends on your email service

            return true;
        }

        public async Task<bool> ResetPasswordAsync(string token, string newPassword)
        {
            // Validate reset token and update password
            // Implementation depends on how you store reset tokens
            return true;
        }

        public async Task<bool> Enable2FAAsync(Guid userId)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null) return false;

            user.TwoFactorEnabled = true;
            _userRepository.Update(user);
            await _userRepository.SaveChangesAsync();
            return true;
        }

        public async Task<bool> Disable2FAAsync(Guid userId)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null) return false;

            user.TwoFactorEnabled = false;
            user.TwoFactorSecret = null;
            _userRepository.Update(user);
            await _userRepository.SaveChangesAsync();
            return true;
        }

        public async Task<string> Generate2FASecretAsync(Guid userId)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null) throw new ArgumentException("User not found");

            var secret = GenerateSecureToken();
            user.TwoFactorSecret = secret;
            _userRepository.Update(user);
            await _userRepository.SaveChangesAsync();

            return secret;
        }

        public async Task<bool> Verify2FACodeAsync(Guid userId, string code)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null || string.IsNullOrEmpty(user.TwoFactorSecret))
                return false;

            // Implement TOTP verification logic
            // This is a simplified version - you'd use a proper TOTP library
            return true;
        }

        public async Task<List<string>> GenerateRecoveryCodesAsync(Guid userId)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null) throw new ArgumentException("User not found");

            var recoveryCodes = new List<string>();
            for (int i = 0; i < 10; i++)
            {
                recoveryCodes.Add(GenerateRecoveryCode());
            }

            user.RecoveryCodes = recoveryCodes;
            _userRepository.Update(user);
            await _userRepository.SaveChangesAsync();

            return recoveryCodes;
        }

        public async Task<bool> UseRecoveryCodeAsync(Guid userId, string code)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null || !user.RecoveryCodes.Contains(code))
                return false;

            user.RecoveryCodes.Remove(code);
            _userRepository.Update(user);
            await _userRepository.SaveChangesAsync();

            return true;
        }

        private string GenerateAccessToken(User user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(GetJwtSecretKey());

            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new(JwtRegisteredClaimNames.Email, user.Email),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new("userId", user.Id.ToString()),
                new("firstName", user.FirstName),
                new("lastName", user.LastName)
            };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(GetAccessTokenExpiryMinutes()),
                Issuer = GetJwtIssuer(),
                Audience = GetJwtAudience(),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        private string GenerateRefreshToken()
        {
            var randomNumber = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }

        private string GenerateSecureToken()
        {
            var randomNumber = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }

        private string GenerateRecoveryCode()
        {
            var random = new Random();
            return random.Next(100000, 999999).ToString();
        }

        private Guid GetCurrentUserId()
        {
            var userIdClaim = _httpContextAccessor.HttpContext?.User.FindFirst("sub") ??
                            _httpContextAccessor.HttpContext?.User.FindFirst("userId");

            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
            {
                throw new UnauthorizedAccessException("Invalid user token");
            }

            return userId;
        }

        private string GetClientIpAddress()
        {
            return _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
        }

        private string GetUserAgent()
        {
            return _httpContextAccessor.HttpContext?.Request.Headers["User-Agent"].ToString() ?? "Unknown";
        }

        private string GetJwtSecretKey() => _configuration["JwtSettings:SecretKey"]!;
        private string GetJwtIssuer() => _configuration["JwtSettings:Issuer"]!;
        private string GetJwtAudience() => _configuration["JwtSettings:Audience"]!;
        private int GetAccessTokenExpiryMinutes() => int.Parse(_configuration["JwtSettings:ExpiryInMinutes"]!);
        private int GetRefreshTokenExpiryDays() => int.Parse(_configuration["JwtSettings:RefreshTokenExpiryInDays"]!);
    }
}