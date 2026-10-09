namespace SmartHealthcare.API.DTOs.AI;

public class AIWorkflowResponse
{
    public Guid WorkflowId { get; set; }

    public Guid? AppointmentId { get; set; }

    public Guid? PatientId { get; set; }

    public string? PatientName { get; set; }

    public string? DoctorName { get; set; }

    public DateOnly? AppointmentDate { get; set; }

    public TimeOnly? AppointmentTime { get; set; }

    public string WorkflowType { get; set; } = string.Empty;

    public string? AgentName { get; set; }

    public string Status { get; set; } = string.Empty;

    public string? UserRequest { get; set; }

    public DateTime StartedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    public List<AIWorkflowStepResponse> Steps { get; set; } = new();

    public List<AIRecommendationResponse> Recommendations { get; set; } = new();

    public List<AIApprovalResponse> Approvals { get; set; } = new();
}