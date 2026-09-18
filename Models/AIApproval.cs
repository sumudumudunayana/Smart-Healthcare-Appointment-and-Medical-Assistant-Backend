namespace SmartHealthcare.API.Models;

public class AIApproval
{
    public Guid ApprovalId { get; set; }

    public Guid WorkflowId { get; set; }

    public Guid? ApprovedBy { get; set; }

    public string Decision { get; set; } = "Pending";

    public string? Comments { get; set; }

    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;

    public DateTime? DecidedAt { get; set; }

    public AIWorkflow? Workflow { get; set; }

    public User? Approver { get; set; }
}