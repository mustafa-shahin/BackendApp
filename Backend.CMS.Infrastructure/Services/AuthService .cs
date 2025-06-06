using AutoMapper;
using Backend.CMS.Application.DTOs.Users;
using Backend.CMS.Application.Interfaces;
using Backend.CMS.Application.Interfaces.Services;
using Backend.CMS.Domain.Entities;
using Backend.CMS.Infrastructure.Repositories;
using BCrypt.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using OtpNet;
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
        private readonly IRepository<PasswordResetToken> _passwordResetRepository;
        private readonly IConfiguration _configuration;
        private readonly IMapper _mapper;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IEmailService _emailService;

        public AuthService(
            IUserRepository userRepository,
            IRepository<UserSession> sessionRepository,
            IRepository<PasswordResetToken> passwordResetRepository,
            IConfiguration configuration,
            IMapper mapper,
            IHttpContextAccessor httpContextAccessor,
            IEmailService emailService)
        {
            _userRepository = userRepository;
            _sessionRepository = sessionRepository;
            _passwordResetRepository = passwordResetRepository;
            _configuration = configuration;
            _mapper = mapper;
            _httpContextAccessor = httpContextAccessor;
            _emailService = emailService;
        }

        public async Task<LoginResponseDto> LoginAsync(LoginDto loginDto)
        {
            var user = await _userRepository.GetByEmailAsync(loginDto.Email);

            if (user == null || !BCrypt.Net.BCrypt.Verify(loginDto.Password, user.PasswordHash))
            {
                // Increment failed attempts for existing user
                if (user != null)
                {
                    user.FailedLoginAttempts++;
                    if (user.FailedLoginAttempts >= 5)
                    {
                        user.IsLocked = true;
                        user.LockoutEnd = DateTime.UtcNow.AddMinutes(30);
                        await _emailService.SendAccountLockedEmailAsync(user.Email, user.FirstName);
                    }
                    _userRepository.Update(user);
                    await _userRepository.SaveChangesAsync();
                }

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
                    var remainingTime = user.LockoutEnd.Value - DateTime.UtcNow;
                    throw new UnauthorizedAccessException($"Account is locked for {remainingTime.Minutes} more minutes");
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
                    // Check if it's a recovery code
                    var isRecoveryCode = await UseRecoveryCodeAsync(user.Id, loginDto.TwoFactorCode);
                    if (!isRecoveryCode)
                    {
                        throw new UnauthorizedAccessException("Invalid two-factor authentication code");
                    }
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
                s.ExpiresAt > DateTime.UtcNow) ?? throw new UnauthorizedAccessException("Invalid refresh token");
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

            // Invalidate any existing password reset tokens for this user
            var existingTokens = await _passwordResetRepository.FindAsync(t => t.UserId == user.Id && !t.IsUsed);
            foreach (var existingToken in existingTokens)
            {
                existingToken.IsUsed = true;
                existingToken.UsedAt = DateTime.UtcNow;
                _passwordResetRepository.Update(existingToken);
            }

            // Generate new reset token
            var resetToken = GenerateSecureToken();
            var passwordResetToken = new PasswordResetToken
            {
                UserId = user.Id,
                Token = resetToken,
                ExpiresAt = DateTime.UtcNow.AddHours(24),
                IsUsed = false,
                IpAddress = GetClientIpAddress(),
                UserAgent = GetUserAgent()
            };

            await _passwordResetRepository.AddAsync(passwordResetToken);
            await _passwordResetRepository.SaveChangesAsync();

            // Send password reset email
            var resetUrl = _configuration["AppSettings:FrontendUrl"] + "/reset-password";
            await _emailService.SendPasswordResetEmailAsync(user.Email, resetToken, resetUrl);

            return true;
        }

        public async Task<bool> ResetPasswordAsync(string token, string newPassword)
        {
            var passwordResetToken = await _passwordResetRepository.FirstOrDefaultAsync(t =>
                t.Token == token &&
                !t.IsUsed &&
                t.ExpiresAt > DateTime.UtcNow);

            if (passwordResetToken == null)
            {
                return false;
            }

            var user = await _userRepository.GetByIdAsync(passwordResetToken.UserId);
            if (user == null)
            {
                return false;
            }

            // Update password
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            user.PasswordChangedAt = DateTime.UtcNow;
            user.FailedLoginAttempts = 0;
            user.IsLocked = false;
            user.LockoutEnd = null;

            // Mark token as used
            passwordResetToken.IsUsed = true;
            passwordResetToken.UsedAt = DateTime.UtcNow;

            _userRepository.Update(user);
            _passwordResetRepository.Update(passwordResetToken);
            await _passwordResetRepository.SaveChangesAsync();

            // Revoke all existing sessions
            await RevokeAllSessionsAsync(user.Id);

            // Send confirmation email
            await _emailService.SendPasswordChangedEmailAsync(user.Email, user.FirstName);

            return true;
        }

        public async Task<bool> Enable2FAAsync(Guid userId)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null) return false;

            user.TwoFactorEnabled = true;
            _userRepository.Update(user);
            await _userRepository.SaveChangesAsync();

            // Send confirmation email
            await _emailService.Send2FAEnabledEmailAsync(user.Email, user.FirstName);

            return true;
        }

        public async Task<bool> Disable2FAAsync(Guid userId)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null) return false;

            user.TwoFactorEnabled = false;
            user.TwoFactorSecret = null;
            user.RecoveryCodes.Clear();
            _userRepository.Update(user);
            await _userRepository.SaveChangesAsync();
            return true;
        }

        public async Task<string> Generate2FASecretAsync(Guid userId)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null) throw new ArgumentException("User not found");

            var secret = Base32Encoding.ToString(KeyGeneration.GenerateRandomKey(20));
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

            try
            {
                var secretBytes = Base32Encoding.ToBytes(user.TwoFactorSecret);
                var totp = new Totp(secretBytes);

                // Verify current window and one window before/after for clock drift
                var currentTime = DateTime.UtcNow;
                var verificationWindow = TimeSpan.FromSeconds(30);

                for (int i = -1; i <= 1; i++)
                {
                    var timeToCheck = currentTime.AddSeconds(i * 30);
                    var expectedCode = totp.ComputeTotp(timeToCheck);
                    if (expectedCode == code)
                    {
                        return true;
                    }
                }

                return false;
            }
            catch
            {
                return false;
            }
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
                new("lastName", user.LastName),
                new("tenantId", user.TenantId)
            };

            // Add role claims
            if (user.UserRoles?.Any() == true)
            {
                foreach (var userRole in user.UserRoles.Where(ur => ur.IsActive))
                {
                    claims.Add(new Claim(ClaimTypes.Role, userRole.Role.Name));
                }
            }

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
            return $"{random.Next(1000, 9999)}-{random.Next(1000, 9999)}";
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
            var ipAddress = _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();

            // Check for forwarded IP (load balancer, proxy)
            var forwardedFor = _httpContextAccessor.HttpContext?.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrEmpty(forwardedFor))
            {
                ipAddress = forwardedFor.Split(',').FirstOrDefault()?.Trim();
            }

            return ipAddress ?? "Unknown";
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