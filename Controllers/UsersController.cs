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
}