using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartHealthcare.API.DTOs.Specializations;
using SmartHealthcare.API.Services;

namespace SmartHealthcare.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SpecializationsController : ControllerBase
{
    private readonly SpecializationService _specializationService;

    public SpecializationsController(
        SpecializationService specializationService)
    {
        _specializationService = specializationService;
    }

    [HttpGet]
    [Authorize(Roles = "Administrator")]
    public async Task<ActionResult<List<SpecializationResponse>>>
        GetAll()
    {
        List<SpecializationResponse> specializations =
            await _specializationService.GetAllAsync();

        return Ok(specializations);
    }

    [HttpPost]
    [Authorize(Roles = "Administrator")]
    public async Task<ActionResult<SpecializationResponse>>
        Create(CreateSpecializationRequest request)
    {
        try
        {
            SpecializationResponse specialization =
                await _specializationService.CreateAsync(
                    request
                );

            return Ok(specialization);
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