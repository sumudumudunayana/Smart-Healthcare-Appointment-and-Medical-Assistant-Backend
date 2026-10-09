using Microsoft.AspNetCore.Mvc;

using SmartHealthcare.API.DTOs.Authentication;
using SmartHealthcare.API.Services;

namespace SmartHealthcare.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AuthService _authService;

    public AuthController(AuthService authService)
    {
        _authService = authService;
    }

    // ============================================================
    // REGISTER
    // POST: /api/Auth/register
    // ============================================================

    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register(
        RegisterRequest request)
    {
        try
        {
            AuthResponse response =
                await _authService.RegisterAsync(request);

            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
    }

    // ============================================================
    // LOGIN
    // POST: /api/Auth/login
    // ============================================================

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(
        LoginRequest request)
    {
        try
        {
            AuthResponse response =
                await _authService.LoginAsync(request);

            return Ok(response);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new
            {
                message = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
    }

    // ============================================================
    // REFRESH TOKEN
    // POST: /api/Auth/refresh
    // ============================================================

    [HttpPost("refresh")]
    public async Task<ActionResult<AuthResponse>> Refresh(
        RefreshTokenRequest request)
    {
        try
        {
            AuthResponse response =
                await _authService.RefreshTokenAsync(
                    request.RefreshToken
                );

            return Ok(response);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new
            {
                message = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
    }

    // ============================================================
    // LOGOUT
    // POST: /api/Auth/logout
    // ============================================================

    [HttpPost("logout")]
    public async Task<IActionResult> Logout(
        RefreshTokenRequest request)
    {
        await _authService.RevokeRefreshTokenAsync(
            request.RefreshToken
        );

        return NoContent();
    }
}