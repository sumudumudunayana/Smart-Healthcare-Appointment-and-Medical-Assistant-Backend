using SmartHealthcare.API.Models;

namespace SmartHealthcare.API.AI.Orchestration;

public interface IAIRecommendationService
{
    Task<AIRecommendation> CreateRecommendationAsync(
        Guid workflowId,
        string recommendationType,
        string recommendation,
        string? reasoning = null);

    Task<List<AIRecommendation>> GetWorkflowRecommendationsAsync(
        Guid workflowId);
}