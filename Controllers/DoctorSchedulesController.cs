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
    public async Task<ActionResult<List<DoctorScheduleResponse>>>
        GetByDoctorId(Guid doctorId)
    {
        List<DoctorScheduleResponse> schedules =
            await _scheduleService.GetByDoctorIdAsync(
                doctorId);

        return Ok(schedules);
    }

    // ============================================================
    // CREATE DOCTOR SCHEDULE
    // ============================================================
    [HttpPost]
    [Authorize(Roles = "Administrator")]
    public async Task<ActionResult<DoctorScheduleResponse>>
        Create(CreateDoctorScheduleRequest request)
    {
        try
        {
            DoctorScheduleResponse schedule =
                await _scheduleService.CreateAsync(request);

            return Ok(schedule);
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



    [HttpPut("{scheduleId:guid}")]
    [Authorize(Roles = "Administrator")]
    public async Task<ActionResult<DoctorScheduleResponse>>
    Update(
        Guid scheduleId,
        UpdateDoctorScheduleRequest request)
    {
        try
        {
            DoctorScheduleResponse? schedule =
                await _scheduleService.UpdateAsync(
                    scheduleId,
                    request);

            if (schedule == null)
            {
                return NotFound(new
                {
                    message = "Schedule was not found."
                });
            }

            return Ok(schedule);
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



    [HttpDelete("{scheduleId:guid}")]
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> Delete(
    Guid scheduleId)
    {
        bool deleted =
            await _scheduleService.DeleteAsync(
                scheduleId);

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
}