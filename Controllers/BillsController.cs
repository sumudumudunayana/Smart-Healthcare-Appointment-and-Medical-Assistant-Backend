using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartHealthcare.API.DTOs.Bills;
using SmartHealthcare.API.Services;

namespace SmartHealthcare.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BillsController : ControllerBase
{
    private readonly BillService _billService;

    public BillsController(BillService billService)
    {
        _billService = billService;
    }

    // POST: api/Bills
    // Administrator or Receptionist creates a bill
    [HttpPost]
    [Authorize(Roles = "Administrator,Receptionist")]
    public async Task<ActionResult<BillResponse>> Create(
        CreateBillRequest request)
    {
        try
        {
            Guid userId = GetUserId();

            BillResponse response =
                await _billService.CreateAsync(userId, request);

            return CreatedAtAction(
                nameof(GetById),
                new { billId = response.BillId },
                response);
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
            return BadRequest(new
            {
                message = ex.Message
            });
        }
    }

    // GET: api/Bills/{billId}
    // Administrator, Receptionist, Doctor, or owning Patient
    [HttpGet("{billId:guid}")]
    public async Task<ActionResult<BillResponse>> GetById(
        Guid billId)
    {
        try
        {
            Guid userId = GetUserId();

            BillResponse? response =
                await _billService.GetByIdAsync(userId, billId);

            if (response == null)
            {
                return NotFound(new
                {
                    message = "Bill was not found."
                });
            }

            return Ok(response);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
    }

    // GET: api/Bills/my
    // Patient gets their own bills
    [HttpGet("my")]
    [Authorize(Roles = "Patient")]
    public async Task<ActionResult<List<BillResponse>>> GetMy()
    {
        try
        {
            Guid userId = GetUserId();

            List<BillResponse> response =
                await _billService.GetMyAsync(userId);

            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
    }

    private Guid GetUserId()
    {
        string? userIdValue =
            User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(userIdValue, out Guid userId))
        {
            throw new UnauthorizedAccessException(
                "Invalid user identity.");
        }

        return userId;
    }
}