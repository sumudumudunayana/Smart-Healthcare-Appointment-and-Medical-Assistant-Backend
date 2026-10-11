using Microsoft.EntityFrameworkCore;
using SmartHealthcare.API.AI.Orchestration;
using SmartHealthcare.API.Data;
using SmartHealthcare.API.Models;
using Xunit;

namespace SmartHealthcare.API.Tests;

public class AIApprovalServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly AIApprovalService _service;

    private static readonly Guid DoctorUserId =
        Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    public AIApprovalServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _service = new AIApprovalService(_context);
    }

    // AIA01: Create an approval successfully.
    [Fact(DisplayName = "AIA01 - Create approval successfully")]
    public async Task CreateApprovalAsync_ShouldCreatePendingApproval()
    {
        var workflow = await CreateWorkflowAsync();

        var approval = await _service.CreateApprovalAsync(
            workflow.WorkflowId,
            "Requires doctor review");

        Assert.NotEqual(Guid.Empty, approval.ApprovalId);
        Assert.Equal(workflow.WorkflowId, approval.WorkflowId);
        Assert.Equal("Pending", approval.Decision);
        Assert.Equal("Requires doctor review", approval.Comments);
        Assert.Null(approval.ApprovedBy);
        Assert.Null(approval.DecidedAt);
        Assert.True(approval.RequestedAt <= DateTime.UtcNow);

        var savedApproval = await _context.AIApprovals
            .FirstOrDefaultAsync(a => a.ApprovalId == approval.ApprovalId);

        Assert.NotNull(savedApproval);
        Assert.Equal("Pending", savedApproval.Decision);
    }

    // AIA02: Create an approval without optional comments.
    [Fact(DisplayName = "AIA02 - Create approval without comments")]
    public async Task CreateApprovalAsync_ShouldAllowNullComments()
    {
        var workflow = await CreateWorkflowAsync();

        var approval = await _service.CreateApprovalAsync(
            workflow.WorkflowId);

        Assert.NotNull(approval);
        Assert.Null(approval.Comments);
        Assert.Equal("Pending", approval.Decision);
    }

    // AIA03: Reject approval creation for a nonexistent workflow.
    [Fact(DisplayName = "AIA03 - Reject approval for nonexistent workflow")]
    public async Task CreateApprovalAsync_ShouldThrowWhenWorkflowDoesNotExist()
    {
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CreateApprovalAsync(Guid.NewGuid()));

        Assert.Equal("AI workflow was not found.", exception.Message);

        Assert.Empty(await _context.AIApprovals.ToListAsync());
    }

    // AIA04: Reuse an existing pending approval for the same workflow.
    [Fact(DisplayName = "AIA04 - Return existing pending approval")]
    public async Task CreateApprovalAsync_ShouldReturnExistingPendingApproval()
    {
        var workflow = await CreateWorkflowAsync();

        var firstApproval = await _service.CreateApprovalAsync(
            workflow.WorkflowId,
            "Initial comment");

        var secondApproval = await _service.CreateApprovalAsync(
            workflow.WorkflowId,
            "Another comment");

        Assert.Equal(firstApproval.ApprovalId, secondApproval.ApprovalId);
        Assert.Equal("Initial comment", secondApproval.Comments);

        var approvals = await _context.AIApprovals
            .Where(a => a.WorkflowId == workflow.WorkflowId)
            .ToListAsync();

        Assert.Single(approvals);
    }

    // AIA05: Retrieve pending approvals.
    [Fact(DisplayName = "AIA05 - Retrieve pending approvals")]
    public async Task GetPendingApprovalsAsync_ShouldReturnOnlyPendingApprovals()
    {
        var workflow1 = await CreateWorkflowAsync();
        var workflow2 = await CreateWorkflowAsync();

        var pendingApproval = new AIApproval
        {
            ApprovalId = Guid.NewGuid(),
            WorkflowId = workflow1.WorkflowId,
            Decision = "Pending",
            Comments = "Awaiting review",
            RequestedAt = DateTime.UtcNow.AddMinutes(-10)
        };

        var approvedApproval = new AIApproval
        {
            ApprovalId = Guid.NewGuid(),
            WorkflowId = workflow2.WorkflowId,
            Decision = "Approved",
            ApprovedBy = DoctorUserId,
            RequestedAt = DateTime.UtcNow.AddMinutes(-20),
            DecidedAt = DateTime.UtcNow
        };

        _context.AIApprovals.AddRange(
            pendingApproval,
            approvedApproval);

        await _context.SaveChangesAsync();

        var results = await _service.GetPendingApprovalsAsync();

        Assert.Single(results);
        Assert.Equal(pendingApproval.ApprovalId, results[0].ApprovalId);
        Assert.Equal("Pending", results[0].Decision);
    }

    // AIA06: Retrieve pending approvals oldest first.
    [Fact(DisplayName = "AIA06 - Order pending approvals by request time")]
    public async Task GetPendingApprovalsAsync_ShouldReturnOldestFirst()
    {
        var workflow1 = await CreateWorkflowAsync();
        var workflow2 = await CreateWorkflowAsync();

        var olderApproval = new AIApproval
        {
            ApprovalId = Guid.NewGuid(),
            WorkflowId = workflow1.WorkflowId,
            Decision = "Pending",
            RequestedAt = DateTime.UtcNow.AddMinutes(-20)
        };

        var newerApproval = new AIApproval
        {
            ApprovalId = Guid.NewGuid(),
            WorkflowId = workflow2.WorkflowId,
            Decision = "Pending",
            RequestedAt = DateTime.UtcNow
        };

        _context.AIApprovals.AddRange(
            newerApproval,
            olderApproval);

        await _context.SaveChangesAsync();

        var results = await _service.GetPendingApprovalsAsync();

        Assert.Equal(2, results.Count);

        Assert.Equal(
            olderApproval.ApprovalId,
            results[0].ApprovalId);

        Assert.Equal(
            newerApproval.ApprovalId,
            results[1].ApprovalId);
    }

    // AIA07: Retrieve an existing approval.
    [Fact(DisplayName = "AIA07 - Retrieve existing approval")]
    public async Task GetApprovalAsync_ShouldReturnApproval()
    {
        var workflow = await CreateWorkflowAsync();

        var approval = new AIApproval
        {
            ApprovalId = Guid.NewGuid(),
            WorkflowId = workflow.WorkflowId,
            Decision = "Pending",
            Comments = "Review this recommendation",
            RequestedAt = DateTime.UtcNow
        };

        _context.AIApprovals.Add(approval);
        await _context.SaveChangesAsync();

        var result = await _service.GetApprovalAsync(approval.ApprovalId);

        Assert.NotNull(result);
        Assert.Equal(approval.ApprovalId, result.ApprovalId);
        Assert.Equal(workflow.WorkflowId, result.WorkflowId);
    }

    // AIA08: Return null for a nonexistent approval.
    [Fact(DisplayName = "AIA08 - Return null for nonexistent approval")]
    public async Task GetApprovalAsync_ShouldReturnNullWhenApprovalDoesNotExist()
    {
        var result = await _service.GetApprovalAsync(Guid.NewGuid());

        Assert.Null(result);
    }

   // AIA09: Approve a pending approval.
[Fact(DisplayName = "AIA09 - Approve pending approval")]
public async Task ApproveAsync_ShouldApproveAndUpdateWorkflow()
{
    var workflow = await CreateWorkflowAsync();
    await CreateDoctorUserAsync();

    var approval = await CreateApprovalAsync(workflow.WorkflowId);

    var result = await _service.ApproveAsync(
        approval.ApprovalId,
        DoctorUserId,
        "Reviewed and approved");

    Assert.NotNull(result);
    Assert.Equal("Approved", result.Decision);
    Assert.Equal(DoctorUserId, result.ApprovedBy);
    Assert.Equal("Reviewed and approved", result.Comments);
    Assert.NotNull(result.DecidedAt);

    var updatedWorkflow = await _context.AIWorkflows
        .FirstAsync(w => w.WorkflowId == workflow.WorkflowId);

    Assert.Equal("Approved", updatedWorkflow.Status);
}

// AIA10: Reject a pending approval.
[Fact(DisplayName = "AIA10 - Reject pending approval")]
public async Task RejectAsync_ShouldRejectAndUpdateWorkflow()
{
    var workflow = await CreateWorkflowAsync();
    await CreateDoctorUserAsync();

    var approval = await CreateApprovalAsync(workflow.WorkflowId);

    var result = await _service.RejectAsync(
        approval.ApprovalId,
        DoctorUserId,
        "Additional clinical review required");

    Assert.NotNull(result);
    Assert.Equal("Rejected", result.Decision);
    Assert.Equal(DoctorUserId, result.ApprovedBy);
    Assert.Equal(
        "Additional clinical review required",
        result.Comments);
    Assert.NotNull(result.DecidedAt);

    var updatedWorkflow = await _context.AIWorkflows
        .FirstAsync(w => w.WorkflowId == workflow.WorkflowId);

    Assert.Equal("Rejected", updatedWorkflow.Status);
}
    // AIA11: Return null when approving a nonexistent approval.
    [Fact(DisplayName = "AIA11 - Approve nonexistent approval")]
    public async Task ApproveAsync_ShouldReturnNullWhenApprovalDoesNotExist()
    {
        var result = await _service.ApproveAsync(
            Guid.NewGuid(),
            DoctorUserId);

        Assert.Null(result);
    }

    // AIA12: Return null when rejecting a nonexistent approval.
    [Fact(DisplayName = "AIA12 - Reject nonexistent approval")]
    public async Task RejectAsync_ShouldReturnNullWhenApprovalDoesNotExist()
    {
        var result = await _service.RejectAsync(
            Guid.NewGuid(),
            DoctorUserId);

        Assert.Null(result);
    }

    // AIA13: Reject approval when the approving user does not exist.
    [Fact(DisplayName = "AIA13 - Approve with nonexistent user")]
    public async Task ApproveAsync_ShouldThrowWhenApproverDoesNotExist()
    {
        var workflow = await CreateWorkflowAsync();
        var approval = await CreateApprovalAsync(workflow.WorkflowId);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.ApproveAsync(
                approval.ApprovalId,
                Guid.NewGuid()));

        Assert.Equal("Approving user was not found.", exception.Message);

        var unchangedApproval = await _context.AIApprovals
            .FirstAsync(a => a.ApprovalId == approval.ApprovalId);

        Assert.Equal("Pending", unchangedApproval.Decision);
    }

    // AIA14: Reject approval when the rejecting user does not exist.
    [Fact(DisplayName = "AIA14 - Reject with nonexistent user")]
    public async Task RejectAsync_ShouldThrowWhenRejectorDoesNotExist()
    {
        var workflow = await CreateWorkflowAsync();
        var approval = await CreateApprovalAsync(workflow.WorkflowId);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.RejectAsync(
                approval.ApprovalId,
                Guid.NewGuid()));

        Assert.Equal("Rejecting user was not found.", exception.Message);

        var unchangedApproval = await _context.AIApprovals
            .FirstAsync(a => a.ApprovalId == approval.ApprovalId);

        Assert.Equal("Pending", unchangedApproval.Decision);
    }

    // AIA15: Prevent approving an already decided approval.
    [Fact(DisplayName = "AIA15 - Prevent approving an already decided approval")]
    public async Task ApproveAsync_ShouldThrowWhenApprovalIsAlreadyDecided()
    {
        var workflow = await CreateWorkflowAsync();
        await CreateDoctorUserAsync();

        var approval = await CreateApprovalAsync(workflow.WorkflowId);

        await _service.ApproveAsync(
            approval.ApprovalId,
            DoctorUserId);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.ApproveAsync(
                approval.ApprovalId,
                DoctorUserId));

        Assert.Equal(
            "This approval has already been decided.",
            exception.Message);
    }

    // AIA16: Prevent rejecting an already decided approval.
    [Fact(DisplayName = "AIA16 - Prevent rejecting an already decided approval")]
    public async Task RejectAsync_ShouldThrowWhenApprovalIsAlreadyDecided()
    {
        var workflow = await CreateWorkflowAsync();
        await CreateDoctorUserAsync();

        var approval = await CreateApprovalAsync(workflow.WorkflowId);

        await _service.RejectAsync(
            approval.ApprovalId,
            DoctorUserId);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.RejectAsync(
                approval.ApprovalId,
                DoctorUserId));

        Assert.Equal(
            "This approval has already been decided.",
            exception.Message);
    }

    private async Task<AIWorkflow> CreateWorkflowAsync()
    {
        var workflow = new AIWorkflow
        {
            WorkflowId = Guid.NewGuid(),
            WorkflowType = "HealthcareAI",
            Status = "AwaitingApproval",
            UserRequest = "Test AI request",
            StartedAt = DateTime.UtcNow
        };

        _context.AIWorkflows.Add(workflow);
        await _context.SaveChangesAsync();

        return workflow;
    }

    private async Task<AIApproval> CreateApprovalAsync(Guid workflowId)
    {
        var approval = new AIApproval
        {
            ApprovalId = Guid.NewGuid(),
            WorkflowId = workflowId,
            Decision = "Pending",
            RequestedAt = DateTime.UtcNow
        };

        _context.AIApprovals.Add(approval);
        await _context.SaveChangesAsync();

        return approval;
    }

    private async Task CreateDoctorUserAsync()
    {
        var doctorRoleId = Guid.Parse(
            "22222222-2222-2222-2222-222222222222");

        _context.Users.Add(new User
        {
            UserId = DoctorUserId,
            FullName = "Test Doctor",
            Email = "test-doctor@example.com",
            PasswordHash = "test-hash",
            Phone = "0771234567",
            Status = "Active",
            CreatedAt = DateTime.UtcNow,
            RoleId = doctorRoleId
        });

        await _context.SaveChangesAsync();
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}