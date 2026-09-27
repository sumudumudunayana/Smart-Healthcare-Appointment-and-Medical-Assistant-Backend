namespace SmartHealthcare.API.Models;

public class AIWorkflowStep
{
    public Guid StepId { get; set; }

    public Guid WorkflowId { get; set; }

    public string AgentName { get; set; } = string.Empty;

    public int StepOrder { get; set; }

    public string Status { get; set; } = "Started";

    public string? InputData { get; set; }

    public string? OutputData { get; set; }

    public DateTime StartedAt { get; set; } = DateTime.UtcNow;

    public DateTime? CompletedAt { get; set; }

    public AIWorkflow? Workflow { get; set; }
}