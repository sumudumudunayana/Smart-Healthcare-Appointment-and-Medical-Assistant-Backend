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
    private readonly IAIRecommendationService
        _recommendationService;

    public AIRecommendationController(
        IAIRecommendationService recommendationService)
    {
        _recommendationService =
            recommendationService;
    }

    [HttpGet("workflow/{workflowId:guid}")]
    public async Task<
        ActionResult<List<AIRecommendationResponse>>>
        GetWorkflowRecommendations(
            Guid workflowId)
    {
        List<AIRecommendation> recommendations =
            await _recommendationService
                .GetWorkflowRecommendationsAsync(
                    workflowId);

        List<AIRecommendationResponse> response =
            recommendations
                .Select(
                    recommendation =>
                        new AIRecommendationResponse
                        {
                            RecommendationId =
                                recommendation
                                    .RecommendationId,

                            WorkflowId =
                                recommendation.WorkflowId,

                            RecommendationType =
                                recommendation
                                    .RecommendationType,

                            Recommendation =
                                recommendation
                                    .Recommendation,

                            Reasoning =
                                recommendation
                                    .Reasoning,

                            Status =
                                recommendation.Status,

                            CreatedAt =
                                recommendation.CreatedAt
                        })
                .ToList();

        return Ok(response);
    }
}