using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartHealthcare.API.AI.Orchestration;
using SmartHealthcare.API.Data;
using SmartHealthcare.API.DTOs.AI;

namespace SmartHealthcare.API.Controllers;

[ApiController]
[Route("api/ai")]
[Authorize]
public class AIOrchestratorController : ControllerBase
{
    private readonly IAIOrchestrator _orchestrator;
    private readonly ApplicationDbContext _context;

    public AIOrchestratorController(
        IAIOrchestrator orchestrator,
        ApplicationDbContext context)
    {
        _orchestrator = orchestrator;
        _context = context;
    }

    [HttpPost("process")]
    public async Task<ActionResult<AgentResponse>> Process(
        [FromBody] AgentRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Request))
        {
            return BadRequest(new
            {
                message = "AI request is required."
            });
        }

        string? userIdClaim =
            User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(userIdClaim, out Guid userId))
        {
            return Unauthorized(new
            {
                message = "Invalid user identity."
            });
        }

        string? userRole =
            User.FindFirstValue(ClaimTypes.Role);

        /*
         * Patients:
         * Automatically attach their own PatientId.
         *
         * Receptionists/Admins/Doctors:
         * They can send PatientId, BillId or AppointmentId
         * according to the AI operation they are performing.
         */
        if (string.Equals(userRole, "Patient", StringComparison.OrdinalIgnoreCase))
        {
            Guid? patientId = await _context.Patients
                .Where(patient => patient.UserId == userId)
                .Select(patient => (Guid?)patient.PatientId)
                .FirstOrDefaultAsync();

            if (patientId == null)
            {
                return NotFound(new
                {
                    message = "Patient profile was not found."
                });
            }

            request.PatientId = patientId;
        }

        /*
         * Receptionist Billing AI:
         * A receptionist must provide a BillId.
         *
         * The Billing Validation Agent uses the BillId
         * to retrieve and validate the billing information.
         */
        if (string.Equals(userRole, "Receptionist", StringComparison.OrdinalIgnoreCase))
        {
            if (request.BillId == null)
            {
                return BadRequest(new
                {
                    message = "BillId is required for receptionist billing AI requests."
                });
            }

            bool billExists = await _context.Bills
                .AnyAsync(bill => bill.BillId == request.BillId.Value);

            if (!billExists)
            {
                return NotFound(new
                {
                    message = "The selected bill was not found."
                });
            }
        }

        /*
         * Administrators and Doctors can use the supplied
         * PatientId, BillId or AppointmentId without requiring
         * a Patient profile for the logged-in user.
         */
        AgentResponse response =
            await _orchestrator.ProcessAsync(request);

        return Ok(response);
    }
}