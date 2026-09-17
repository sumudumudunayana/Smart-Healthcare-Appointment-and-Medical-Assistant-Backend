using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartHealthcare.API.DTOs.InsuranceClaims;
using SmartHealthcare.API.Services;

namespace SmartHealthcare.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class InsuranceClaimsController : ControllerBase
{
    private readonly InsuranceClaimService _insuranceClaimService;

    public InsuranceClaimsController(
        InsuranceClaimService insuranceClaimService)
    {
        _insuranceClaimService = insuranceClaimService;
    }

    // POST: api/InsuranceClaims
    // Administrator or Receptionist creates an insurance claim
    [HttpPost]
    [Authorize(Roles = "Administrator,Receptionist")]
    public async Task<ActionResult<InsuranceClaimResponse>> Create(
        CreateInsuranceClaimRequest request)
    {
        try
        {
            Guid userId = GetUserId();

            InsuranceClaimResponse response =
                await _insuranceClaimService.CreateAsync(
                    userId,
                    request);

            return CreatedAtAction(
                nameof(GetByBill),
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
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    // GET: api/InsuranceClaims/bill/{billId}
    // Administrator, Receptionist, or owning Patient
    [HttpGet("bill/{billId:guid}")]
    public async Task<ActionResult<List<InsuranceClaimResponse>>> GetByBill(
        Guid billId)
    {
        try
        {
            Guid userId = GetUserId();

            List<InsuranceClaimResponse> response =
                await _insuranceClaimService.GetByBillAsync(
                    userId,
                    billId);

            return Ok(response);
        }
        catch (ArgumentException ex)
        {
            return NotFound(new
            {
                message = ex.Message
            });
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

    // GET: api/InsuranceClaims/my
    // Patient gets their own insurance claims
    [HttpGet("my")]
    [Authorize(Roles = "Patient")]
    public async Task<ActionResult<List<InsuranceClaimResponse>>> GetMy()
    {
        try
        {
            Guid userId = GetUserId();

            List<InsuranceClaimResponse> response =
                await _insuranceClaimService.GetMyAsync(userId);

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