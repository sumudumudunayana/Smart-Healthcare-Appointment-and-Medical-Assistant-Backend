using Microsoft.EntityFrameworkCore;
using SmartHealthcare.API.Data;
using SmartHealthcare.API.Models;

namespace SmartHealthcare.API.AI.Orchestration;

public class AIRecommendationService : IAIRecommendationService
{
    private readonly ApplicationDbContext _context;

    public AIRecommendationService(
        ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<AIRecommendation> CreateRecommendationAsync(
        Guid workflowId,
        string recommendationType,
        string recommendation,
        string? reasoning = null)
    {
        AIWorkflow? workflow =
            await _context.AIWorkflows
                .FirstOrDefaultAsync(
                    existingWorkflow =>
                        existingWorkflow.WorkflowId ==
                        workflowId);

        if (workflow == null)
        {
            throw new InvalidOperationException(
                "AI workflow was not found.");
        }

        AIRecommendation aiRecommendation = new()
        {
            RecommendationId = Guid.NewGuid(),
            WorkflowId = workflowId,
            RecommendationType = recommendationType,
            Recommendation = recommendation,
            Reasoning = reasoning,
            Status = "Pending",
            CreatedAt = DateTime.UtcNow
        };

        _context.AIRecommendations.Add(
            aiRecommendation);

        await _context.SaveChangesAsync();

        return aiRecommendation;
    }

    public async Task<List<AIRecommendation>>
        GetWorkflowRecommendationsAsync(
            Guid workflowId)
    {
        return await _context.AIRecommendations
            .AsNoTracking()
            .Where(
                recommendation =>
                    recommendation.WorkflowId ==
                    workflowId)
            .OrderBy(
                recommendation =>
                    recommendation.CreatedAt)
            .ToListAsync();
    }
}