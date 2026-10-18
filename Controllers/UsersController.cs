using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartHealthcare.API.DTOs.Users;
using SmartHealthcare.API.Services;

namespace SmartHealthcare.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly UserService _userService;

    public UsersController(UserService userService)
    {
        _userService = userService;
    }

    [HttpGet("me")]
    public async Task<ActionResult<UserProfileResponse>> GetMyProfile()
    {
        string? userIdClaim =
            User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(userIdClaim, out Guid userId))
        {
            return Unauthorized(new
            {
                message = "Invalid user identity."
            });
        }

        UserProfileResponse? profile =
            await _userService.GetProfileAsync(userId);

        if (profile == null)
        {
            return NotFound(new
            {
                message = "User profile was not found."
            });
        }

        return Ok(profile);
    }

    [HttpPut("me")]
    public async Task<ActionResult<UserProfileResponse>>
        UpdateMyProfile(
            UpdateUserProfileRequest request)
    {
        string? userIdClaim =
            User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(userIdClaim, out Guid userId))
        {
            return Unauthorized(new
            {
                message = "Invalid user identity."
            });
        }

        try
        {
            UserProfileResponse? profile =
                await _userService.UpdateProfileAsync(
                    userId,
                    request
                );

            if (profile == null)
            {
                return NotFound(new
                {
                    message = "User profile was not found."
                });
            }

            return Ok(profile);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
    }


    [HttpGet]
    [Authorize(Roles = "Administrator")]
    public async Task<ActionResult<List<UserResponse>>> GetAll()
    {
        List<UserResponse> users =
            await _userService.GetAllAsync();

        return Ok(users);
    }


    [HttpPut("{userId:guid}")]
    [Authorize(Roles = "Administrator")]
    public async Task<ActionResult<UserResponse>> UpdateUser(
    Guid userId,
    AdminUpdateUserRequest request)
    {
        try
        {
            UserResponse? user =
                await _userService.UpdateUserAsync(userId, request);

            if (user == null)
            {
                return NotFound(new
                {
                    message = "User was not found."
                });
            }

            return Ok(user);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
    }

    [HttpPut("{userId:guid}/status")]
    [Authorize(Roles = "Administrator")]
    public async Task<ActionResult<UserResponse>> UpdateUserStatus(
        Guid userId,
        UpdateUserStatusRequest request)
    {
        string? currentUserIdClaim =
            User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(currentUserIdClaim, out Guid currentUserId))
        {
            return Unauthorized(new
            {
                message = "Invalid user identity."
            });
        }

        // Prevent an administrator from accidentally deactivating
        // their own account and locking themselves out.
        if (currentUserId == userId &&
            string.Equals(
                request.Status?.Trim(),
                "Inactive",
                StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new
            {
                message = "You cannot deactivate your own account."
            });
        }

        try
        {
            UserResponse? user =
    await _userService.UpdateUserStatusAsync(
        userId,
        request.Status ?? string.Empty);

            if (user == null)
            {
                return NotFound(new
                {
                    message = "User was not found."
                });
            }

            return Ok(user);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
    }
}