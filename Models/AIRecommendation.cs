namespace SmartHealthcare.API.Models;

public class AIRecommendation
{
    public Guid RecommendationId { get; set; }

    public Guid WorkflowId { get; set; }

    public string RecommendationType { get; set; } = string.Empty;

    public string Recommendation { get; set; } = string.Empty;

    public string? Reasoning { get; set; }

    public string Status { get; set; } = "Pending";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public AIWorkflow? Workflow { get; set; }
}