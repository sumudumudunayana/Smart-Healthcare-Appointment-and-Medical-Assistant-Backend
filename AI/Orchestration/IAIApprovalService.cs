using SmartHealthcare.API.Models;

namespace SmartHealthcare.API.AI.Orchestration;

public interface IAIApprovalService
{
    Task<AIApproval> CreateApprovalAsync(
        Guid workflowId,
        string? comments = null);

    Task<List<AIApproval>> GetPendingApprovalsAsync();

    Task<AIApproval?> GetApprovalAsync(
        Guid approvalId);

    Task<AIApproval?> ApproveAsync(
        Guid approvalId,
        Guid approvedBy,
        string? comments = null);

    Task<AIApproval?> RejectAsync(
        Guid approvalId,
        Guid rejectedBy,
        string? comments = null);
}