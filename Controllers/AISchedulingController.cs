using Google.GenAI;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartHealthcare.API.AI.Agents;
using SmartHealthcare.API.DTOs.AI;

namespace SmartHealthcare.API.Controllers;

[ApiController]
[Route("api/ai/scheduling")]
[Authorize]
public class AISchedulingController : ControllerBase
{
    private readonly IHealthcareAgent _schedulingAgent;
    private readonly ILogger<AISchedulingController> _logger;

    public AISchedulingController(
        IEnumerable<IHealthcareAgent> agents,
        ILogger<AISchedulingController> logger)
    {
        _schedulingAgent =
            agents.FirstOrDefault(
                agent => agent.AgentName == "AppointmentSchedulingAgent")
            ?? throw new InvalidOperationException(
                "AppointmentSchedulingAgent is not registered. " +
                "Check the IHealthcareAgent registrations in Program.cs.");

        _logger = logger;
    }

    [HttpPost("test")]
    public async Task<ActionResult<AgentResponse>> TestScheduling(
        [FromBody] AgentRequest request)
    {
        Guid workflowId = Guid.NewGuid();

        try
        {
            AgentResponse response =
                await _schedulingAgent.ExecuteAsync(
                    request,
                    workflowId);

            return Ok(response);
        }
        catch (ServerError ex)
        {
            _logger.LogError(
                ex,
                "Gemini failed while processing appointment scheduling. " +
                "WorkflowId: {WorkflowId}",
                workflowId);

            return StatusCode(
                StatusCodes.Status503ServiceUnavailable,
                new AgentResponse
                {
                    Success = false,
                    AgentName = "AppointmentSchedulingAgent",
                    Message =
                        "The AI assistant is temporarily unavailable. " +
                        "Please try again shortly.",
                    RequiresHumanApproval = false,
                    WorkflowId = workflowId
                });
        }
    }
}