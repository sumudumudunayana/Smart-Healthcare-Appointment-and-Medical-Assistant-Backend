using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartHealthcare.API.DTOs.MedicalRecords;
using SmartHealthcare.API.Services;

namespace SmartHealthcare.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MedicalRecordsController : ControllerBase
{
    private readonly MedicalRecordService _medicalRecordService;

    public MedicalRecordsController(
        MedicalRecordService medicalRecordService)
    {
        _medicalRecordService = medicalRecordService;
    }

    [HttpPost]
    [Authorize(Roles = "Doctor")]
    public async Task<ActionResult<MedicalRecordResponse>> Create(
        CreateMedicalRecordRequest request)
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
            MedicalRecordResponse record =
                await _medicalRecordService.CreateAsync(
                    userId,
                    request);

            return Ok(record);
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

    [HttpGet("appointment/{appointmentId:guid}")]
    [Authorize(Roles = "Doctor")]
    public async Task<ActionResult<MedicalRecordResponse>>
        GetByAppointment(Guid appointmentId)
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
            MedicalRecordResponse? record =
                await _medicalRecordService.GetByAppointmentAsync(
                    userId,
                    appointmentId);

            if (record == null)
            {
                return NotFound(new
                {
                    message = "Medical record was not found."
                });
            }

            return Ok(record);
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
    public async Task<ActionResult<List<MedicalRecordResponse>>> GetMyRecords()
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
            List<MedicalRecordResponse> records =
                await _medicalRecordService.GetMyRecordsAsync(userId);

            return Ok(records);
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