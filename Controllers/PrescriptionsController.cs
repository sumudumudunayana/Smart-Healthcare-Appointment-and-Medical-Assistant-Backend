using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartHealthcare.API.DTOs.Prescriptions;
using SmartHealthcare.API.Services;

namespace SmartHealthcare.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PrescriptionsController : ControllerBase
{
    private readonly PrescriptionService _prescriptionService;

    public PrescriptionsController(
        PrescriptionService prescriptionService)
    {
        _prescriptionService = prescriptionService;
    }

    [HttpPost]
    [Authorize(Roles = "Doctor")]
    public async Task<ActionResult<PrescriptionResponse>> Create(
        CreatePrescriptionRequest request)
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
            PrescriptionResponse prescription =
                await _prescriptionService.CreateAsync(
                    userId,
                    request);

            return Ok(prescription);
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

    [HttpGet("record/{recordId:guid}")]
    [Authorize(Roles = "Doctor")]
    public async Task<ActionResult<List<PrescriptionResponse>>>
        GetByRecord(Guid recordId)
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
            List<PrescriptionResponse> prescriptions =
                await _prescriptionService.GetByRecordAsync(
                    userId,
                    recordId);

            return Ok(prescriptions);
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
    public async Task<ActionResult<List<PrescriptionResponse>>>
        GetMy()
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
            List<PrescriptionResponse> prescriptions =
                await _prescriptionService.GetMyAsync(userId);

            return Ok(prescriptions);
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