using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartHealthcare.API.DTOs.Notifications;
using SmartHealthcare.API.Services;

namespace SmartHealthcare.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly NotificationService _notificationService;

    public NotificationsController(
        NotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    // POST: api/Notifications
    // Administrator or Receptionist creates a notification
    [HttpPost]
    [Authorize(Roles = "Administrator,Receptionist")]
    public async Task<ActionResult<NotificationResponse>> Create(
        CreateNotificationRequest request)
    {
        try
        {
            Guid userId = GetUserId();

            NotificationResponse response =
                await _notificationService.CreateAsync(
                    userId,
                    request);

            return Ok(response);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new
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
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    // GET: api/Notifications/my
    // Logged-in user gets their own notifications
    [HttpGet("my")]
    public async Task<ActionResult<List<NotificationResponse>>> GetMy()
    {
        try
        {
            Guid userId = GetUserId();

            List<NotificationResponse> response =
                await _notificationService.GetMyAsync(userId);

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

    // PATCH: api/Notifications/{notificationId}/read
    // User marks their own notification as read
    [HttpPatch("{notificationId:guid}/read")]
    public async Task<ActionResult<NotificationResponse>> MarkAsRead(
        Guid notificationId)
    {
        try
        {
            Guid userId = GetUserId();

            NotificationResponse? response =
                await _notificationService.MarkAsReadAsync(
                    userId,
                    notificationId);

            if (response == null)
            {
                return NotFound(new
                {
                    message = "Notification was not found."
                });
            }

            return Ok(response);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    private Guid GetUserId()
    {
        string? userIdValue =
            User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(userIdValue, out Guid userId))
        {
            throw new UnauthorizedAccessException(
                "Invalid user identity.");
        }

        return userId;
    }


    // GET: api/Notifications/users
    // Administrator or Receptionist gets users who can receive notifications
    [HttpGet("users")]
    [Authorize(Roles = "Administrator,Receptionist")]
    public async Task<ActionResult<List<object>>> GetUsers()
    {
        List<object> users =
            await _notificationService.GetNotificationUsersAsync();

        return Ok(users);
    }
}