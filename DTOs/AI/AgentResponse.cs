namespace SmartHealthcare.API.DTOs.AI;

public class AgentResponse
{
    public bool Success { get; set; }
    public string AgentName { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Output { get; set; }
    public bool RequiresHumanApproval { get; set; }
    public Guid? WorkflowId { get; set; }
}