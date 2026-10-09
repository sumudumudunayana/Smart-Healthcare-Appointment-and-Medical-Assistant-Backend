using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartHealthcare.API.DTOs.Receptionists;
using SmartHealthcare.API.Services;

namespace SmartHealthcare.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ReceptionistsController : ControllerBase
{
    private readonly ReceptionistService _receptionistService;

    public ReceptionistsController(
        ReceptionistService receptionistService)
    {
        _receptionistService = receptionistService;
    }

    // ============================================================
    // CREATE RECEPTIONIST
    // POST: /api/Receptionists
    // ============================================================

    [HttpPost]
    [Authorize(Roles = "Administrator")]
    public async Task<ActionResult<ReceptionistResponse>> Create(
        CreateReceptionistRequest request)
    {
        try
        {
            ReceptionistResponse receptionist =
                await _receptionistService.CreateAsync(
                    request
                );

            return Ok(receptionist);
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
    // GET ALL RECEPTIONISTS
    // GET: /api/Receptionists
    // ============================================================

    [HttpGet]
    [Authorize(Roles = "Administrator")]
    public async Task<ActionResult<List<ReceptionistResponse>>>
        GetAll()
    {
        List<ReceptionistResponse> receptionists =
            await _receptionistService.GetAllAsync();

        return Ok(receptionists);
    }
}