namespace SmartHealthcare.API.DTOs.AI;

public class AIWorkflowStepResponse
{
    public Guid StepId { get; set; }

    public Guid WorkflowId { get; set; }

    public string AgentName { get; set; } = string.Empty;

    public int StepOrder { get; set; }

    public string Status { get; set; } = string.Empty;

    public string? InputData { get; set; }

    public string? OutputData { get; set; }

    public DateTime StartedAt { get; set; }

    public DateTime? CompletedAt { get; set; }
}