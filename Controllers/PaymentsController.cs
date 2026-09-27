using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartHealthcare.API.DTOs.Payments;
using SmartHealthcare.API.Services;

namespace SmartHealthcare.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PaymentsController : ControllerBase
{
    private readonly PaymentService _paymentService;

    public PaymentsController(PaymentService paymentService)
    {
        _paymentService = paymentService;
    }

    // POST: api/Payments
    // Administrator or Receptionist processes a payment
    [HttpPost]
    [Authorize(Roles = "Administrator,Receptionist")]
    public async Task<ActionResult<PaymentResponse>> Create(
        CreatePaymentRequest request)
    {
        try
        {
            Guid userId = GetUserId();

            PaymentResponse response =
                await _paymentService.CreateAsync(userId, request);

            return Ok(response);
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

    // GET: api/Payments/bill/{billId}
    // Administrator, Receptionist, or owning Patient
    [HttpGet("bill/{billId:guid}")]
    public async Task<ActionResult<List<PaymentResponse>>> GetByBill(
        Guid billId)
    {
        try
        {
            Guid userId = GetUserId();

            List<PaymentResponse> response =
                await _paymentService.GetByBillAsync(
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