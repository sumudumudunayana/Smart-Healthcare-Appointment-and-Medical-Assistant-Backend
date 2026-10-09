using System.Security.Claims;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using SmartHealthcare.API.DTOs.DoctorSchedules;
using SmartHealthcare.API.Services;

namespace SmartHealthcare.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DoctorSchedulesController : ControllerBase
{
    private readonly DoctorScheduleService _scheduleService;

    public DoctorSchedulesController(
        DoctorScheduleService scheduleService)
    {
        _scheduleService = scheduleService;
    }

    // ============================================================
    // GET SCHEDULES FOR A DOCTOR
    // ============================================================
    [HttpGet("doctor/{doctorId:guid}")]
    [Authorize(Roles = "Administrator,Doctor,Receptionist,Patient")]
    public async Task<ActionResult<List<DoctorScheduleResponse>>>
     GetByDoctorId(Guid doctorId)
    {
        try
        {
            bool isAdministrator =
                User.IsInRole("Administrator");

            bool isReceptionist =
                User.IsInRole("Receptionist");

            bool isPatient =
                User.IsInRole("Patient");

            // Administrator, Receptionist and Patient
            // can view any doctor's schedules.
            if (isAdministrator ||
                isReceptionist ||
                isPatient)
            {
                List<DoctorScheduleResponse> schedules =
                    await _scheduleService.GetByDoctorIdAsync(
                        doctorId);

                return Ok(schedules);
            }

            // Doctor can only view their own schedules.
            Guid authenticatedUserId =
                GetAuthenticatedUserId();

            Guid? authenticatedDoctorId =
                await _scheduleService.GetDoctorIdFromUserAsync(
                    authenticatedUserId);

            if (authenticatedDoctorId == null)
            {
                return NotFound(new
                {
                    message = "Doctor profile was not found."
                });
            }

            if (authenticatedDoctorId.Value != doctorId)
            {
                return Forbid();
            }

            List<DoctorScheduleResponse> doctorSchedules =
                await _scheduleService.GetByDoctorIdAsync(
                    doctorId);

            return Ok(doctorSchedules);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new
            {
                message = ex.Message
            });
        }
    }

    // ============================================================
    // CREATE DOCTOR SCHEDULE
    // ============================================================
    [HttpPost]
    [Authorize(Roles = "Administrator,Doctor")]
    public async Task<ActionResult<DoctorScheduleResponse>>
        Create(CreateDoctorScheduleRequest request)
    {
        try
        {
            bool isAdministrator = User.IsInRole("Administrator");

            Guid userId = GetAuthenticatedUserId();

            Guid doctorId;

            // ----------------------------------------------------
            // Administrator
            // ----------------------------------------------------
            // Administrator can create a schedule for the doctor
            // supplied in the request.
            //
            // Doctor
            // ----------------------------------------------------
            // DoctorId from the request is ignored.
            // The doctor is resolved from the authenticated user.
            // ----------------------------------------------------
            if (isAdministrator)
            {
                doctorId = request.DoctorId;
            }
            else
            {
                Guid? authenticatedDoctorId =
                    await _scheduleService.GetDoctorIdFromUserAsync(
                        userId);

                if (authenticatedDoctorId == null)
                {
                    return NotFound(new
                    {
                        message = "Doctor profile was not found."
                    });
                }

                doctorId = authenticatedDoctorId.Value;
            }

            request.DoctorId = doctorId;

            DoctorScheduleResponse schedule =
                await _scheduleService.CreateAsync(request);

            return Ok(schedule);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new
            {
                message = ex.Message
            });
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

    // ============================================================
    // UPDATE DOCTOR SCHEDULE
    // ============================================================
    [HttpPut("{scheduleId:guid}")]
    [Authorize(Roles = "Administrator,Doctor")]
    public async Task<ActionResult<DoctorScheduleResponse>>
        Update(
            Guid scheduleId,
            UpdateDoctorScheduleRequest request)
    {
        try
        {
            bool isAdministrator = User.IsInRole("Administrator");

            Guid? authenticatedDoctorId = null;

            if (!isAdministrator)
            {
                authenticatedDoctorId =
                    await _scheduleService.GetDoctorIdFromUserAsync(
                        GetAuthenticatedUserId());

                if (authenticatedDoctorId == null)
                {
                    return NotFound(new
                    {
                        message = "Doctor profile was not found."
                    });
                }
            }

            DoctorScheduleResponse? schedule =
                await _scheduleService.UpdateAsync(
                    scheduleId,
                    request,
                    authenticatedDoctorId);

            if (schedule == null)
            {
                return NotFound(new
                {
                    message = "Schedule was not found."
                });
            }

            return Ok(schedule);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new
            {
                message = ex.Message
            });
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

    // ============================================================
    // DELETE DOCTOR SCHEDULE
    // ============================================================
    [HttpDelete("{scheduleId:guid}")]
    [Authorize(Roles = "Administrator,Doctor")]
    public async Task<IActionResult>
        Delete(Guid scheduleId)
    {
        try
        {
            bool isAdministrator = User.IsInRole("Administrator");

            Guid? authenticatedDoctorId = null;

            if (!isAdministrator)
            {
                authenticatedDoctorId =
                    await _scheduleService.GetDoctorIdFromUserAsync(
                        GetAuthenticatedUserId());

                if (authenticatedDoctorId == null)
                {
                    return NotFound(new
                    {
                        message = "Doctor profile was not found."
                    });
                }
            }

            bool deleted =
                await _scheduleService.DeleteAsync(
                    scheduleId,
                    authenticatedDoctorId);

            if (!deleted)
            {
                return NotFound(new
                {
                    message = "Schedule was not found."
                });
            }

            return Ok(new
            {
                message = "Doctor schedule deleted successfully."
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new
            {
                message = ex.Message
            });
        }
    }

    // ============================================================
    // GET AUTHENTICATED USER ID
    // ============================================================
    private Guid GetAuthenticatedUserId()
    {
        string? userIdClaim =
            User.FindFirstValue(
                ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(
                userIdClaim,
                out Guid userId))
        {
            throw new UnauthorizedAccessException(
                "Invalid user identity."
            );
        }

        return userId;
    }
}