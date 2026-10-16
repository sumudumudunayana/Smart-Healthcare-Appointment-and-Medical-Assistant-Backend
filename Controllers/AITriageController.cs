using Google.GenAI;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartHealthcare.API.AI.Agents;
using SmartHealthcare.API.DTOs.AI;

namespace SmartHealthcare.API.Controllers;

[ApiController]
[Route("api/ai/triage")]
[Authorize]
public class AITriageController : ControllerBase
{
    private readonly IHealthcareAgent _triageAgent;
    private readonly ILogger<AITriageController> _logger;

    public AITriageController(
        IEnumerable<IHealthcareAgent> agents,
        ILogger<AITriageController> logger)
    {
        _triageAgent = agents.FirstOrDefault(
            agent => agent.AgentName == "PatientTriageAgent"
        ) ?? throw new InvalidOperationException(
            "PatientTriageAgent is not registered. " +
            "Check the IHealthcareAgent registrations in Program.cs."
        );

        _logger = logger;
    }

    [HttpPost("test")]
    public async Task<ActionResult<AgentResponse>> TestTriage(
        [FromBody] AgentRequest request)
    {
        Guid workflowId = Guid.NewGuid();

        if (string.IsNullOrWhiteSpace(request.Request))
        {
            return BadRequest(new
            {
                message = "Please describe the symptoms you are experiencing."
            });
        }

        try
        {
            AgentResponse response = await _triageAgent.ExecuteAsync(
                request,
                workflowId
            );

            return Ok(response);
        }
        catch (ServerError ex)
        {
            _logger.LogError(
                ex,
                "Gemini failed during patient triage. WorkflowId: {WorkflowId}",
                workflowId
            );

            return StatusCode(
                StatusCodes.Status503ServiceUnavailable,
                new
                {
                    message =
                        "The symptom assessment service is temporarily unavailable. " +
                        "Please seek professional medical advice if you are concerned."
                }
            );
        }
    }
}