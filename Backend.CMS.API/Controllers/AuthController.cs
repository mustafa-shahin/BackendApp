using Backend.CMS.Application.DTOs.Users;
using Backend.CMS.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.CMS.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IAuthService authService, ILogger<AuthController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        /// <summary>
        /// Authenticate user and return JWT tokens
        /// </summary>
        [HttpPost("login")]
        public async Task<ActionResult<LoginResponseDto>> Login([FromBody] LoginDto loginDto)
        {
            try
            {
                var result = await _authService.LoginAsync(loginDto);

                if (result.RequiresTwoFactor)
                {
                    return Ok(new { RequiresTwoFactor = true, Message = "Two-factor authentication required" });
                }

                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning("Login failed for {Email}: {Message}", loginDto.Email, ex.Message);
                return Unauthorized(new { Message = "Invalid credentials" });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning("Login validation failed for {Email}: {Message}", loginDto.Email, ex.Message);
                return BadRequest(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login for {Email}", loginDto.Email);
                return StatusCode(500, new { Message = "An error occurred during login" });
            }
        }

        /// <summary>
        /// Refresh access token using refresh token
        /// </summary>
        [HttpPost("refresh")]
        public async Task<ActionResult<LoginResponseDto>> RefreshToken([FromBody] RefreshTokenDto refreshTokenDto)
        {
            try
            {
                var result = await _authService.RefreshTokenAsync(refreshTokenDto);
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning("Token refresh failed: {Message}", ex.Message);
                return Unauthorized(new { Message = "Invalid refresh token" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during token refresh");
                return StatusCode(500, new { Message = "An error occurred during token refresh" });
            }
        }

        /// <summary>
        /// Logout user and revoke refresh token
        /// </summary>
        [HttpPost("logout")]
        [Authorize]
        public async Task<ActionResult> Logout([FromBody] RefreshTokenDto refreshTokenDto)
        {
            try
            {
                await _authService.LogoutAsync(refreshTokenDto.RefreshToken);
                return Ok(new { Message = "Logged out successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during logout");
                return StatusCode(500, new { Message = "An error occurred during logout" });
            }
        }

        /// <summary>
        /// Get current authenticated user information
        /// </summary>
        [HttpGet("me")]
        [Authorize]
        public async Task<ActionResult<UserDto>> GetCurrentUser()
        {
            try
            {
                var user = await _authService.GetCurrentUserAsync();
                return Ok(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current user");
                return StatusCode(500, new { Message = "An error occurred while retrieving user information" });
            }
        }

        /// <summary>
        /// Initiate password reset process
        /// </summary>
        [HttpPost("forgot-password")]
        public async Task<ActionResult> ForgotPassword([FromBody] ForgotPasswordDto forgotPasswordDto)
        {
            try
            {
                await _authService.ForgotPasswordAsync(forgotPasswordDto.Email);
                return Ok(new { Message = "If the email exists, a password reset link has been sent" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during forgot password for {Email}", forgotPasswordDto.Email);
                return StatusCode(500, new { Message = "An error occurred while processing the request" });
            }
        }

        /// <summary>
        /// Reset password using reset token
        /// </summary>
        [HttpPost("reset-password")]
        public async Task<ActionResult> ResetPassword([FromBody] ResetPasswordDto resetPasswordDto)
        {
            try
            {
                var success = await _authService.ResetPasswordAsync(resetPasswordDto.Token, resetPasswordDto.NewPassword);

                if (!success)
                {
                    return BadRequest(new { Message = "Invalid or expired reset token" });
                }

                return Ok(new { Message = "Password reset successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during password reset");
                return StatusCode(500, new { Message = "An error occurred while resetting the password" });
            }
        }

        /// <summary>
        /// Enable two-factor authentication
        /// </summary>
        [HttpPost("enable-2fa")]
        [Authorize]
        public async Task<ActionResult<Enable2FAResponseDto>> Enable2FA()
        {
            try
            {
                var userId = GetCurrentUserId();
                var secret = await _authService.Generate2FASecretAsync(userId);

                return Ok(new Enable2FAResponseDto
                {
                    Secret = secret,
                    QrCodeUrl = GenerateQrCodeUrl(secret)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error enabling 2FA");
                return StatusCode(500, new { Message = "An error occurred while enabling 2FA" });
            }
        }

        /// <summary>
        /// Verify and complete 2FA setup
        /// </summary>
        [HttpPost("verify-2fa")]
        [Authorize]
        public async Task<ActionResult<List<string>>> Verify2FA([FromBody] Verify2FADto verify2FADto)
        {
            try
            {
                var userId = GetCurrentUserId();
                var isValid = await _authService.Verify2FACodeAsync(userId, verify2FADto.Code);

                if (!isValid)
                {
                    return BadRequest(new { Message = "Invalid verification code" });
                }

                await _authService.Enable2FAAsync(userId);
                var recoveryCodes = await _authService.GenerateRecoveryCodesAsync(userId);

                return Ok(recoveryCodes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying 2FA");
                return StatusCode(500, new { Message = "An error occurred while verifying 2FA" });
            }
        }

        /// <summary>
        /// Disable two-factor authentication
        /// </summary>
        [HttpPost("disable-2fa")]
        [Authorize]
        public async Task<ActionResult> Disable2FA()
        {
            try
            {
                var userId = GetCurrentUserId();
                await _authService.Disable2FAAsync(userId);

                return Ok(new { Message = "Two-factor authentication disabled successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disabling 2FA");
                return StatusCode(500, new { Message = "An error occurred while disabling 2FA" });
            }
        }

        /// <summary>
        /// Revoke all user sessions
        /// </summary>
        [HttpPost("revoke-all-sessions")]
        [Authorize]
        public async Task<ActionResult> RevokeAllSessions()
        {
            try
            {
                var userId = GetCurrentUserId();
                await _authService.RevokeAllSessionsAsync(userId);

                return Ok(new { Message = "All sessions revoked successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error revoking all sessions");
                return StatusCode(500, new { Message = "An error occurred while revoking sessions" });
            }
        }

        private Guid GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst("sub") ?? User.FindFirst("userId");
            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
            {
                throw new UnauthorizedAccessException("Invalid user token");
            }
            return userId;
        }

        private string GenerateQrCodeUrl(string secret)
        {
            // Generate QR code URL for 2FA apps like Google Authenticator
            var issuer = "Backend CMS";
            var user = User.FindFirst("email")?.Value ?? "user";
            return $"otpauth://totp/{issuer}:{user}?secret={secret}&issuer={issuer}";
        }
    }

    public class ForgotPasswordDto
    {
        public string Email { get; set; } = string.Empty;
    }

    public class ResetPasswordDto
    {
        public string Token { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
    }

    public class Enable2FAResponseDto
    {
        public string Secret { get; set; } = string.Empty;
        public string QrCodeUrl { get; set; } = string.Empty;
    }

    public class Verify2FADto
    {
        public string Code { get; set; } = string.Empty;
    }
}