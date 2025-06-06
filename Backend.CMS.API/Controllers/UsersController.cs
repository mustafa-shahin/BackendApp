using Backend.CMS.Application.DTOs.Users;
using Backend.CMS.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.CMS.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly ILogger<UsersController> _logger;

        public UsersController(IUserService userService, ILogger<UsersController> logger)
        {
            _userService = userService;
            _logger = logger;
        }

        /// <summary>
        /// Get user by ID
        /// </summary>
        [HttpGet("{id:guid}")]
        public async Task<ActionResult<UserDto>> GetUser(Guid id)
        {
            try
            {
                var user = await _userService.GetUserByIdAsync(id);
                return Ok(user);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning("User not found: {UserId}", id);
                return NotFound(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user {UserId}", id);
                return StatusCode(500, new { Message = "An error occurred while retrieving the user" });
            }
        }

        /// <summary>
        /// Get paginated list of users
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<PagedResult<UserListDto>>> GetUsers(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? search = null)
        {
            try
            {
                var users = await _userService.GetUsersAsync(page, pageSize, search);
                return Ok(new PagedResult<UserListDto>
                {
                    Items = users,
                    Page = page,
                    PageSize = pageSize,
                    TotalCount = users.Count
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving users");
                return StatusCode(500, new { Message = "An error occurred while retrieving users" });
            }
        }

        /// <summary>
        /// Create a new user
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<UserDto>> CreateUser([FromBody] CreateUserDto createUserDto)
        {
            try
            {
                var user = await _userService.CreateUserAsync(createUserDto);
                return CreatedAtAction(nameof(GetUser), new { id = user.Id }, user);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning("User creation failed: {Message}", ex.Message);
                return BadRequest(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user");
                return StatusCode(500, new { Message = "An error occurred while creating the user" });
            }
        }

        /// <summary>
        /// Update an existing user
        /// </summary>
        [HttpPut("{id:guid}")]
        public async Task<ActionResult<UserDto>> UpdateUser(Guid id, [FromBody] UpdateUserDto updateUserDto)
        {
            try
            {
                var user = await _userService.UpdateUserAsync(id, updateUserDto);
                return Ok(user);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning("User update failed for {UserId}: {Message}", id, ex.Message);
                return BadRequest(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user {UserId}", id);
                return StatusCode(500, new { Message = "An error occurred while updating the user" });
            }
        }

        /// <summary>
        /// Delete a user
        /// </summary>
        [HttpDelete("{id:guid}")]
        public async Task<ActionResult> DeleteUser(Guid id)
        {
            try
            {
                var success = await _userService.DeleteUserAsync(id);
                if (!success)
                {
                    return NotFound(new { Message = "User not found" });
                }
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user {UserId}", id);
                return StatusCode(500, new { Message = "An error occurred while deleting the user" });
            }
        }

        /// <summary>
        /// Activate a user
        /// </summary>
        [HttpPost("{id:guid}/activate")]
        public async Task<ActionResult> ActivateUser(Guid id)
        {
            try
            {
                var success = await _userService.ActivateUserAsync(id);
                if (!success)
                {
                    return NotFound(new { Message = "User not found" });
                }
                return Ok(new { Message = "User activated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error activating user {UserId}", id);
                return StatusCode(500, new { Message = "An error occurred while activating the user" });
            }
        }

        /// <summary>
        /// Deactivate a user
        /// </summary>
        [HttpPost("{id:guid}/deactivate")]
        public async Task<ActionResult> DeactivateUser(Guid id)
        {
            try
            {
                var success = await _userService.DeactivateUserAsync(id);
                if (!success)
                {
                    return NotFound(new { Message = "User not found" });
                }
                return Ok(new { Message = "User deactivated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deactivating user {UserId}", id);
                return StatusCode(500, new { Message = "An error occurred while deactivating the user" });
            }
        }

        /// <summary>
        /// Lock a user account
        /// </summary>
        [HttpPost("{id:guid}/lock")]
        public async Task<ActionResult> LockUser(Guid id)
        {
            try
            {
                var success = await _userService.LockUserAsync(id);
                if (!success)
                {
                    return NotFound(new { Message = "User not found" });
                }
                return Ok(new { Message = "User locked successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error locking user {UserId}", id);
                return StatusCode(500, new { Message = "An error occurred while locking the user" });
            }
        }

        /// <summary>
        /// Unlock a user account
        /// </summary>
        [HttpPost("{id:guid}/unlock")]
        public async Task<ActionResult> UnlockUser(Guid id)
        {
            try
            {
                var success = await _userService.UnlockUserAsync(id);
                if (!success)
                {
                    return NotFound(new { Message = "User not found" });
                }
                return Ok(new { Message = "User unlocked successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unlocking user {UserId}", id);
                return StatusCode(500, new { Message = "An error occurred while unlocking the user" });
            }
        }

        /// <summary>
        /// Change user password
        /// </summary>
        [HttpPost("{id:guid}/change-password")]
        public async Task<ActionResult> ChangePassword(Guid id, [FromBody] ChangePasswordDto changePasswordDto)
        {
            try
            {
                var success = await _userService.ChangePasswordAsync(id, changePasswordDto);
                if (!success)
                {
                    return BadRequest(new { Message = "Current password is incorrect" });
                }
                return Ok(new { Message = "Password changed successfully" });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning("Password change failed for {UserId}: {Message}", id, ex.Message);
                return BadRequest(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing password for user {UserId}", id);
                return StatusCode(500, new { Message = "An error occurred while changing the password" });
            }
        }

        /// <summary>
        /// Reset user password (admin action)
        /// </summary>
        [HttpPost("{id:guid}/reset-password")]
        public async Task<ActionResult> ResetPassword(Guid id)
        {
            try
            {
                // This should generate a temporary password or send reset email
                var success = await _userService.ResetPasswordAsync(string.Empty); // Needs user email
                if (!success)
                {
                    return NotFound(new { Message = "User not found" });
                }
                return Ok(new { Message = "Password reset initiated" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting password for user {UserId}", id);
                return StatusCode(500, new { Message = "An error occurred while resetting the password" });
            }
        }

        /// <summary>
        /// Assign role to user
        /// </summary>
        [HttpPost("{id:guid}/roles/{roleId:guid}")]
        public async Task<ActionResult> AssignRole(Guid id, Guid roleId)
        {
            try
            {
                var success = await _userService.AssignRoleAsync(id, roleId);
                if (!success)
                {
                    return BadRequest(new { Message = "Failed to assign role" });
                }
                return Ok(new { Message = "Role assigned successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning role {RoleId} to user {UserId}", roleId, id);
                return StatusCode(500, new { Message = "An error occurred while assigning the role" });
            }
        }

        /// <summary>
        /// Remove role from user
        /// </summary>
        [HttpDelete("{id:guid}/roles/{roleId:guid}")]
        public async Task<ActionResult> RemoveRole(Guid id, Guid roleId)
        {
            try
            {
                var success = await _userService.RemoveRoleAsync(id, roleId);
                if (!success)
                {
                    return BadRequest(new { Message = "Failed to remove role" });
                }
                return Ok(new { Message = "Role removed successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing role {RoleId} from user {UserId}", roleId, id);
                return StatusCode(500, new { Message = "An error occurred while removing the role" });
            }
        }

        /// <summary>
        /// Get user roles
        /// </summary>
        [HttpGet("{id:guid}/roles")]
        public async Task<ActionResult<List<RoleDto>>> GetUserRoles(Guid id)
        {
            try
            {
                var roles = await _userService.GetUserRolesAsync(id);
                return Ok(roles);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving roles for user {UserId}", id);
                return StatusCode(500, new { Message = "An error occurred while retrieving user roles" });
            }
        }

        /// <summary>
        /// Update user preferences
        /// </summary>
        [HttpPut("{id:guid}/preferences")]
        public async Task<ActionResult<UserDto>> UpdateUserPreferences(Guid id, [FromBody] Dictionary<string, object> preferences)
        {
            try
            {
                var user = await _userService.UpdateUserPreferencesAsync(id, preferences);
                return Ok(user);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning("Preferences update failed for {UserId}: {Message}", id, ex.Message);
                return BadRequest(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating preferences for user {UserId}", id);
                return StatusCode(500, new { Message = "An error occurred while updating user preferences" });
            }
        }

        /// <summary>
        /// Send email verification
        /// </summary>
        [HttpPost("{id:guid}/send-verification")]
        public async Task<ActionResult> SendEmailVerification(Guid id)
        {
            try
            {
                var success = await _userService.SendEmailVerificationAsync(id);
                if (!success)
                {
                    return BadRequest(new { Message = "Failed to send verification email" });
                }
                return Ok(new { Message = "Verification email sent successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending verification email for user {UserId}", id);
                return StatusCode(500, new { Message = "An error occurred while sending verification email" });
            }
        }
    }
}