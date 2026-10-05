using SmartHealthcare.API.DTOs.AI;

namespace SmartHealthcare.API.AI.Agents;

public interface IHealthcareAgent
{
    string AgentName { get; }

    Task<AgentResponse> ExecuteAsync(
        AgentRequest request,
        Guid workflowId);
}