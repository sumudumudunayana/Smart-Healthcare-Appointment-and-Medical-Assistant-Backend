using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartHealthcare.API.DTOs.Doctors;
using SmartHealthcare.API.Services;

namespace SmartHealthcare.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DoctorsController : ControllerBase
{
    private readonly DoctorService _doctorService;

    public DoctorsController(
        DoctorService doctorService)
    {
        _doctorService = doctorService;
    }

    [HttpPost]
    [Authorize(Roles = "Administrator")]
    public async Task<ActionResult<DoctorResponse>> Create(
        CreateDoctorRequest request)
    {
        try
        {
            DoctorResponse doctor =
                await _doctorService.CreateAsync(request);

            return Ok(doctor);
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

    [HttpGet]
    public async Task<ActionResult<List<DoctorResponse>>>
        GetAll(
            [FromQuery] string? search,
            [FromQuery] Guid? specializationId,
            [FromQuery] Guid? departmentId)
    {
        List<DoctorResponse> doctors =
            await _doctorService.GetAllAsync(
                search,
                specializationId,
                departmentId);

        return Ok(doctors);
    }

    [HttpGet("{doctorId:guid}")]
    public async Task<ActionResult<DoctorResponse>>
        GetById(Guid doctorId)
    {
        DoctorResponse? doctor =
            await _doctorService.GetByIdAsync(
                doctorId);

        if (doctor == null)
        {
            return NotFound(new
            {
                message = "Doctor was not found."
            });
        }

        return Ok(doctor);
    }
}