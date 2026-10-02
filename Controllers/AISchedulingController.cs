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

    public AISchedulingController(
        IEnumerable<IHealthcareAgent> agents)
    {
        _schedulingAgent =
            agents.First(agent =>
                agent.AgentName == "AppointmentSchedulingAgent");
    }

    [HttpPost("test")]
    public async Task<ActionResult<AgentResponse>> TestScheduling(
        [FromBody] AgentRequest request)
    {
        Guid workflowId = Guid.NewGuid();

        AgentResponse response =
            await _schedulingAgent.ExecuteAsync(
                request,
                workflowId);

        return Ok(response);
    }
}