namespace SmartHealthcare.API.DTOs.AI;

public class AIRecommendationResponse
{
    public Guid RecommendationId { get; set; }

    public Guid WorkflowId { get; set; }

    public string RecommendationType { get; set; } = string.Empty;

    public string Recommendation { get; set; } = string.Empty;

    public string? Reasoning { get; set; }

    public string Status { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
}