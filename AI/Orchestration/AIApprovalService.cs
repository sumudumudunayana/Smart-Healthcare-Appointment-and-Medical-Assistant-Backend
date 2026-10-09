using Microsoft.EntityFrameworkCore;
using SmartHealthcare.API.Data;
using SmartHealthcare.API.Models;

namespace SmartHealthcare.API.AI.Orchestration;

public class AIApprovalService : IAIApprovalService
{
    private readonly ApplicationDbContext _context;

    public AIApprovalService(
        ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<AIApproval> CreateApprovalAsync(
        Guid workflowId,
        string? comments = null)
    {
        AIWorkflow? workflow =
            await _context.AIWorkflows
                .FirstOrDefaultAsync(
                    existingWorkflow =>
                        existingWorkflow.WorkflowId ==
                        workflowId);

        if (workflow == null)
        {
            throw new InvalidOperationException(
                "AI workflow was not found.");
        }

        AIApproval? existingPendingApproval =
            await _context.AIApprovals
                .FirstOrDefaultAsync(
                    approval =>
                        approval.WorkflowId == workflowId &&
                        approval.Decision == "Pending");

        if (existingPendingApproval != null)
        {
            return existingPendingApproval;
        }

        AIApproval approval = new()
        {
            ApprovalId = Guid.NewGuid(),
            WorkflowId = workflowId,
            Decision = "Pending",
            Comments = comments,
            RequestedAt = DateTime.UtcNow
        };

        _context.AIApprovals.Add(approval);

        await _context.SaveChangesAsync();

        return approval;
    }

    public async Task<List<AIApproval>> GetPendingApprovalsAsync()
    {
        return await _context.AIApprovals
            .AsNoTracking()
            .Include(approval => approval.Workflow)
            .Where(approval =>
                approval.Decision == "Pending")
            .OrderBy(
                approval => approval.RequestedAt)
            .ToListAsync();
    }

    public async Task<AIApproval?> GetApprovalAsync(
        Guid approvalId)
    {
        return await _context.AIApprovals
            .AsNoTracking()
            .Include(approval => approval.Workflow)
            .FirstOrDefaultAsync(
                approval =>
                    approval.ApprovalId == approvalId);
    }

    public async Task<AIApproval?> ApproveAsync(
        Guid approvalId,
        Guid approvedBy,
        string? comments = null)
    {
        AIApproval? approval =
            await _context.AIApprovals
                .Include(existingApproval =>
                    existingApproval.Workflow)
                .FirstOrDefaultAsync(
                    existingApproval =>
                        existingApproval.ApprovalId ==
                        approvalId);

        if (approval == null)
        {
            return null;
        }

        if (approval.Decision != "Pending")
        {
            throw new InvalidOperationException(
                "This approval has already been decided.");
        }

        User? approver =
            await _context.Users
                .FirstOrDefaultAsync(
                    user =>
                        user.UserId == approvedBy);

        if (approver == null)
        {
            throw new InvalidOperationException(
                "Approving user was not found.");
        }

        approval.Decision = "Approved";
        approval.ApprovedBy = approvedBy;
        approval.Comments = comments;
        approval.DecidedAt = DateTime.UtcNow;

        if (approval.Workflow != null)
        {
            approval.Workflow.Status = "Approved";
        }

        await _context.SaveChangesAsync();

        return approval;
    }

    public async Task<AIApproval?> RejectAsync(
        Guid approvalId,
        Guid rejectedBy,
        string? comments = null)
    {
        AIApproval? approval =
            await _context.AIApprovals
                .Include(existingApproval =>
                    existingApproval.Workflow)
                .FirstOrDefaultAsync(
                    existingApproval =>
                        existingApproval.ApprovalId ==
                        approvalId);

        if (approval == null)
        {
            return null;
        }

        if (approval.Decision != "Pending")
        {
            throw new InvalidOperationException(
                "This approval has already been decided.");
        }

        User? approver =
            await _context.Users
                .FirstOrDefaultAsync(
                    user =>
                        user.UserId == rejectedBy);

        if (approver == null)
        {
            throw new InvalidOperationException(
                "Rejecting user was not found.");
        }

        approval.Decision = "Rejected";
        approval.ApprovedBy = rejectedBy;
        approval.Comments = comments;
        approval.DecidedAt = DateTime.UtcNow;

        if (approval.Workflow != null)
        {
            approval.Workflow.Status = "Rejected";
        }

        await _context.SaveChangesAsync();

        return approval;
    }
}