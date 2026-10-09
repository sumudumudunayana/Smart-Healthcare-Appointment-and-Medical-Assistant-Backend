namespace SmartHealthcare.API.DTOs.AI;

public class AIApprovalResponse
{
    public Guid ApprovalId { get; set; }

    public Guid WorkflowId { get; set; }

    public string Decision { get; set; } = string.Empty;

    public string? Comments { get; set; }

    public DateTime RequestedAt { get; set; }

    public DateTime? DecidedAt { get; set; }

    public string WorkflowType { get; set; } = string.Empty;

    public string WorkflowStatus { get; set; } = string.Empty;

    public string? UserRequest { get; set; }

    public Guid? PatientId { get; set; }

    public Guid? AppointmentId { get; set; }
}