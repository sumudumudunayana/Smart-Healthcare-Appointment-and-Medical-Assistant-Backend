using SmartHealthcare.API.DTOs.AI;

namespace SmartHealthcare.API.AI.Orchestration;

public interface IAIOrchestrator
{
    Task<AgentResponse> ProcessAsync(
        AgentRequest request);
}