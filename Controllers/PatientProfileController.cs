using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartHealthcare.API.DTOs.Patients;
using SmartHealthcare.API.Services;

namespace SmartHealthcare.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Patient")]
public class PatientProfileController : ControllerBase
{
    private readonly PatientService _patientService;

    public PatientProfileController(PatientService patientService)
    {
        _patientService = patientService;
    }

    // GET: api/PatientProfile/my
    [HttpGet("my")]
    public async Task<ActionResult<PatientResponse>> GetMyProfile()
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
            PatientResponse? patient =
                await _patientService.GetMyProfileAsync(userId);

            if (patient == null)
            {
                return NotFound(new
                {
                    message = "Patient profile not found."
                });
            }

            return Ok(patient);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new
            {
                message = ex.Message
            });
        }
    }

    // PUT: api/PatientProfile/my
    [HttpPut("my")]
    public async Task<ActionResult<PatientResponse>> UpdateMyProfile(
        UpdatePatientRequest request)
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
            PatientResponse? patient =
                await _patientService.UpdateMyProfileAsync(
                    userId,
                    request);

            if (patient == null)
            {
                return NotFound(new
                {
                    message = "Patient profile not found."
                });
            }

            return Ok(patient);
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
}