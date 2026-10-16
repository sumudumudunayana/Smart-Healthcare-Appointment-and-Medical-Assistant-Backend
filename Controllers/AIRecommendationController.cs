using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartHealthcare.API.AI.Orchestration;
using SmartHealthcare.API.DTOs.AI;
using SmartHealthcare.API.Models;

namespace SmartHealthcare.API.Controllers;

[ApiController]
[Route("api/ai/recommendations")]
[Authorize(Roles = "Administrator,Doctor")]
public class AIRecommendationController : ControllerBase
{
    private readonly IAIRecommendationService _recommendationService;
    private readonly ILogger<AIRecommendationController> _logger;

    public AIRecommendationController(
        IAIRecommendationService recommendationService,
        ILogger<AIRecommendationController> logger)
    {
        _recommendationService = recommendationService;
        _logger = logger;
    }

    [HttpGet("workflow/{workflowId:guid}")]
    public async Task<ActionResult<List<AIRecommendationResponse>>>
        GetWorkflowRecommendations(Guid workflowId)
    {
        try
        {
            List<AIRecommendation> recommendations =
                await _recommendationService
                    .GetWorkflowRecommendationsAsync(workflowId);

            List<AIRecommendationResponse> response =
                recommendations
                    .Select(recommendation => new AIRecommendationResponse
                    {
                        RecommendationId = recommendation.RecommendationId,
                        WorkflowId = recommendation.WorkflowId,
                        RecommendationType = recommendation.RecommendationType,
                        Recommendation = recommendation.Recommendation,
                        Reasoning = recommendation.Reasoning,
                        Status = recommendation.Status,
                        CreatedAt = recommendation.CreatedAt
                    })
                    .ToList();

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to retrieve AI recommendations for workflow {WorkflowId}",
                workflowId);

            return Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Failed to retrieve AI recommendations.",
                detail: "An unexpected error occurred while retrieving recommendations.");
        }
    }
}