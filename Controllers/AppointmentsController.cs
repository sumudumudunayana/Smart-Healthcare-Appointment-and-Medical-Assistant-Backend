using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartHealthcare.API.DTOs.Appointments;
using SmartHealthcare.API.Services;

namespace SmartHealthcare.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AppointmentsController : ControllerBase
{
    private readonly AppointmentService _appointmentService;

    public AppointmentsController(
        AppointmentService appointmentService)
    {
        _appointmentService = appointmentService;
    }

    // ============================================================
    // BOOK APPOINTMENT
    // ============================================================
    [HttpPost]
    [Authorize(Roles = "Patient")]
    public async Task<ActionResult<AppointmentResponse>>
        Create(CreateAppointmentRequest request)
    {
        string? userIdClaim =
            User.FindFirstValue(
                ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(
                userIdClaim,
                out Guid userId))
        {
            return Unauthorized(new
            {
                message = "Invalid user identity."
            });
        }

        try
        {
            AppointmentResponse appointment =
                await _appointmentService.CreateAsync(
                    userId,
                    request);

            return Ok(appointment);
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
            return Conflict(new
            {
                message = ex.Message
            });
        }
    }




    [HttpGet("my")]
    [Authorize(Roles = "Patient")]
    public async Task<ActionResult<List<AppointmentResponse>>>
    GetMyAppointments()
    {
        string? userIdClaim =
            User.FindFirstValue(
                ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(
                userIdClaim,
                out Guid userId))
        {
            return Unauthorized(new
            {
                message = "Invalid user identity."
            });
        }

        try
        {
            List<AppointmentResponse> appointments =
                await _appointmentService
                    .GetMyAppointmentsAsync(userId);

            return Ok(appointments);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new
            {
                message = ex.Message
            });
        }
    }




    [HttpGet("{appointmentId:guid}")]
    [Authorize(Roles = "Patient")]
    public async Task<ActionResult<AppointmentResponse>>
    GetById(Guid appointmentId)
    {
        string? userIdClaim =
            User.FindFirstValue(
                ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(
                userIdClaim,
                out Guid userId))
        {
            return Unauthorized(new
            {
                message = "Invalid user identity."
            });
        }

        try
        {
            AppointmentResponse? appointment =
                await _appointmentService.GetByIdAsync(
                    userId,
                    appointmentId);

            if (appointment == null)
            {
                return NotFound(new
                {
                    message = "Appointment was not found."
                });
            }

            return Ok(appointment);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new
            {
                message = ex.Message
            });
        }
    }


    [HttpPatch("{appointmentId:guid}/cancel")]
    [Authorize(Roles = "Patient")]
    public async Task<ActionResult<AppointmentResponse>>
    Cancel(Guid appointmentId)
    {
        string? userIdClaim =
            User.FindFirstValue(
                ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(
                userIdClaim,
                out Guid userId))
        {
            return Unauthorized(new
            {
                message = "Invalid user identity."
            });
        }

        try
        {
            AppointmentResponse? appointment =
                await _appointmentService.CancelAsync(
                    userId,
                    appointmentId);

            if (appointment == null)
            {
                return NotFound(new
                {
                    message = "Appointment was not found."
                });
            }

            return Ok(appointment);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new
            {
                message = ex.Message
            });
        }
    }





    [HttpGet("doctor/my")]
    [Authorize(Roles = "Doctor")]
    public async Task<ActionResult<List<AppointmentResponse>>>
    GetDoctorAppointments()
    {
        string? userIdClaim =
            User.FindFirstValue(
                ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(
                userIdClaim,
                out Guid userId))
        {
            return Unauthorized(new
            {
                message = "Invalid user identity."
            });
        }

        try
        {
            List<AppointmentResponse> appointments =
                await _appointmentService
                    .GetDoctorAppointmentsAsync(userId);

            return Ok(appointments);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new
            {
                message = ex.Message
            });
        }
    }
}