using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartHealthcare.API.DTOs.LabReports;
using SmartHealthcare.API.Services;

namespace SmartHealthcare.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class LabReportsController : ControllerBase
{
    private readonly LabReportService _labReportService;

    public LabReportsController(
        LabReportService labReportService)
    {
        _labReportService = labReportService;
    }

    [HttpPost]
    [Authorize(Roles = "Doctor")]
    public async Task<ActionResult<LabReportResponse>> Create(
        CreateLabReportRequest request)
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
            LabReportResponse report =
                await _labReportService.CreateAsync(
                    userId,
                    request);

            return Ok(report);
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
    public async Task<ActionResult<List<LabReportResponse>>>
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
            List<LabReportResponse> reports =
                await _labReportService.GetByRecordAsync(
                    userId,
                    recordId);

            return Ok(reports);
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
    public async Task<ActionResult<List<LabReportResponse>>>
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
            List<LabReportResponse> reports =
                await _labReportService.GetMyAsync(userId);

            return Ok(reports);
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