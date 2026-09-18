namespace SmartHealthcare.API.Models;

public class AIWorkflow
{
    public Guid WorkflowId { get; set; }

    public Guid? AppointmentId { get; set; }

    public Guid? PatientId { get; set; }

    public string WorkflowType { get; set; } = string.Empty;

    public string Status { get; set; } = "Started";

    public string? UserRequest { get; set; }

    public DateTime StartedAt { get; set; } = DateTime.UtcNow;

    public DateTime? CompletedAt { get; set; }

    public Appointment? Appointment { get; set; }

    public Patient? Patient { get; set; }

    public ICollection<AIWorkflowStep> Steps { get; set; }
        = new List<AIWorkflowStep>();

    public ICollection<AIRecommendation> Recommendations { get; set; }
        = new List<AIRecommendation>();

    public ICollection<AIApproval> Approvals { get; set; }
        = new List<AIApproval>();
}