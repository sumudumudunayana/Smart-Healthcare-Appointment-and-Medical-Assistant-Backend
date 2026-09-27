using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartHealthcare.API.DTOs.InsurancePolicies;
using SmartHealthcare.API.Services;

namespace SmartHealthcare.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class InsurancePoliciesController : ControllerBase
{
    private readonly InsurancePolicyService _insurancePolicyService;

    public InsurancePoliciesController(
        InsurancePolicyService insurancePolicyService)
    {
        _insurancePolicyService = insurancePolicyService;
    }

    // POST: api/InsurancePolicies
    // Administrator or Receptionist creates an insurance policy
    [HttpPost]
    [Authorize(Roles = "Administrator,Receptionist")]
    public async Task<ActionResult<InsurancePolicyResponse>> Create(
        CreateInsurancePolicyRequest request)
    {
        try
        {
            Guid userId = GetUserId();

            InsurancePolicyResponse response =
                await _insurancePolicyService.CreateAsync(
                    userId,
                    request);

            return CreatedAtAction(
                nameof(GetById),
                new { insurancePolicyId = response.InsurancePolicyId },
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

    // GET: api/InsurancePolicies/my
    // Patient gets their own insurance policies
    [HttpGet("my")]
    [Authorize(Roles = "Patient")]
    public async Task<ActionResult<List<InsurancePolicyResponse>>> GetMy()
    {
        try
        {
            Guid userId = GetUserId();

            List<InsurancePolicyResponse> response =
                await _insurancePolicyService.GetMyAsync(userId);

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

    // GET: api/InsurancePolicies
    // Administrator or Receptionist gets all policies
    [HttpGet]
    [Authorize(Roles = "Administrator,Receptionist")]
    public async Task<ActionResult<List<InsurancePolicyResponse>>> GetAll()
    {
        try
        {
            Guid userId = GetUserId();

            List<InsurancePolicyResponse> response =
                await _insurancePolicyService.GetAllAsync(userId);

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

    // GET: api/InsurancePolicies/{insurancePolicyId}
    // Administrator, Receptionist, or owning Patient
    [HttpGet("{insurancePolicyId:guid}")]
    public async Task<ActionResult<InsurancePolicyResponse>> GetById(
        Guid insurancePolicyId)
    {
        try
        {
            Guid userId = GetUserId();

            InsurancePolicyResponse? response =
                await _insurancePolicyService.GetByIdAsync(
                    userId,
                    insurancePolicyId);

            if (response == null)
            {
                return NotFound(new
                {
                    message = "Insurance policy was not found."
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