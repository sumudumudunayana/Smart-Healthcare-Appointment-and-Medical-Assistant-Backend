using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartHealthcare.API.DTOs.Patients;
using SmartHealthcare.API.Services;

namespace SmartHealthcare.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Receptionist,Administrator")]
public class PatientsController : ControllerBase
{
    private readonly PatientService _patientService;
    public PatientsController(PatientService patientService) => _patientService = patientService;

    [HttpGet]
    public async Task<ActionResult<List<PatientResponse>>> GetAll() => Ok(await _patientService.GetAllAsync());

    [HttpPost]
    public async Task<ActionResult<PatientResponse>> Create(CreatePatientRequest request)
    {
        try { return Ok(await _patientService.CreateAsync(request)); }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
    }



    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PatientResponse>> GetById(Guid id)
    {
        var patient = await _patientService.GetByIdAsync(id);

        if (patient == null)
            return NotFound(new { message = "Patient not found." });

        return Ok(patient);
    }


    [HttpPut("{id:guid}")]
    public async Task<ActionResult<PatientResponse>> Update(
        Guid id,
        UpdatePatientRequest request)
    {
        try
        {
            var patient = await _patientService.UpdateAsync(id, request);

            if (patient == null)
                return NotFound(new { message = "Patient not found." });

            return Ok(patient);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }



    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Deactivate(Guid id)
    {
        try
        {
            bool deactivated = await _patientService.DeactivateAsync(id);

            if (!deactivated)
                return NotFound(new { message = "Patient not found." });

            return Ok(new { message = "Patient account deactivated successfully." });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }
}



