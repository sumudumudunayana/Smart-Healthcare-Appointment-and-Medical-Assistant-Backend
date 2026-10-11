using Microsoft.EntityFrameworkCore;
using SmartHealthcare.API.AI.Orchestration;
using SmartHealthcare.API.Data;
using SmartHealthcare.API.Models;
using Xunit;

namespace SmartHealthcare.API.Tests;

public class AIWorkflowServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly AIWorkflowService _service;

    public AIWorkflowServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _service = new AIWorkflowService(_context);
    }

    // AIW01: Create a workflow successfully.
    [Fact(DisplayName = "AIW01 - Create workflow successfully")]
    public async Task CreateWorkflowAsync_ShouldCreateWorkflow()
    {
        var workflow = await _service.CreateWorkflowAsync(
            "PatientTriage",
            "Patient reports a headache");

        Assert.NotEqual(Guid.Empty, workflow.WorkflowId);
        Assert.Equal("PatientTriage", workflow.WorkflowType);
        Assert.Equal("Started", workflow.Status);
        Assert.Equal("Patient reports a headache", workflow.UserRequest);
        Assert.True(workflow.StartedAt <= DateTime.UtcNow);
        Assert.Null(workflow.CompletedAt);

        var savedWorkflow = await _context.AIWorkflows
            .FirstOrDefaultAsync(w => w.WorkflowId == workflow.WorkflowId);

        Assert.NotNull(savedWorkflow);
        Assert.Equal(workflow.WorkflowId, savedWorkflow.WorkflowId);
    }

    // AIW02: Create a workflow with patient and appointment IDs.
    [Fact(DisplayName = "AIW02 - Create workflow with patient and appointment IDs")]
    public async Task CreateWorkflowAsync_ShouldSavePatientAndAppointmentIds()
    {
        var patientId = Guid.NewGuid();
        var appointmentId = Guid.NewGuid();

        var workflow = await _service.CreateWorkflowAsync(
            "AppointmentScheduling",
            "Find an available doctor",
            patientId,
            appointmentId);

        Assert.Equal(patientId, workflow.PatientId);
        Assert.Equal(appointmentId, workflow.AppointmentId);

        var savedWorkflow = await _context.AIWorkflows
            .FindAsync(workflow.WorkflowId);

        Assert.NotNull(savedWorkflow);
        Assert.Equal(patientId, savedWorkflow.PatientId);
        Assert.Equal(appointmentId, savedWorkflow.AppointmentId);
    }

    // AIW03: Start a workflow step successfully.
    [Fact(DisplayName = "AIW03 - Start workflow step successfully")]
    public async Task StartStepAsync_ShouldCreateWorkflowStep()
    {
        var workflow = await _service.CreateWorkflowAsync(
            "HealthcareAI",
            "Summarize patient history");

        var inputData = """{"patientId":"test-patient"}""";

        var step = await _service.StartStepAsync(
            workflow.WorkflowId,
            "MedicalSummaryAgent",
            1,
            inputData);

        Assert.NotEqual(Guid.Empty, step.StepId);
        Assert.Equal(workflow.WorkflowId, step.WorkflowId);
        Assert.Equal("MedicalSummaryAgent", step.AgentName);
        Assert.Equal(1, step.StepOrder);
        Assert.Equal("Started", step.Status);
        Assert.Equal(inputData, step.InputData);
        Assert.Null(step.OutputData);
        Assert.Null(step.CompletedAt);

        var savedStep = await _context.AIWorkflowSteps
            .FirstOrDefaultAsync(s => s.StepId == step.StepId);

        Assert.NotNull(savedStep);
        Assert.Equal(workflow.WorkflowId, savedStep.WorkflowId);
    }

    // AIW04: Complete an existing workflow step.
    [Fact(DisplayName = "AIW04 - Complete workflow step successfully")]
    public async Task CompleteStepAsync_ShouldUpdateStep()
    {
        var workflow = await _service.CreateWorkflowAsync(
            "PatientTriage",
            "Patient has a fever");

        var step = await _service.StartStepAsync(
            workflow.WorkflowId,
            "PatientTriageAgent",
            1,
            "Fever reported");

        var outputData = "Recommended urgency: Medium";

        await _service.CompleteStepAsync(
            step.StepId,
            "Completed",
            outputData);

        var updatedStep = await _context.AIWorkflowSteps
            .FirstOrDefaultAsync(s => s.StepId == step.StepId);

        Assert.NotNull(updatedStep);
        Assert.Equal("Completed", updatedStep.Status);
        Assert.Equal(outputData, updatedStep.OutputData);
        Assert.NotNull(updatedStep.CompletedAt);
        Assert.True(updatedStep.CompletedAt <= DateTime.UtcNow);
    }

    // AIW05: Completing a nonexistent step should throw an exception.
    [Fact(DisplayName = "AIW05 - Complete nonexistent workflow step")]
    public async Task CompleteStepAsync_ShouldThrowWhenStepDoesNotExist()
    {
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CompleteStepAsync(
                Guid.NewGuid(),
                "Completed",
                "Test output"));

        Assert.Equal(
            "AI workflow step was not found.",
            exception.Message);
    }

    // AIW06: Complete an existing workflow.
    [Fact(DisplayName = "AIW06 - Complete workflow successfully")]
    public async Task CompleteWorkflowAsync_ShouldUpdateWorkflow()
    {
        var workflow = await _service.CreateWorkflowAsync(
            "HealthcareAI",
            "Validate a medical bill");

        await _service.CompleteWorkflowAsync(
            workflow.WorkflowId,
            "Completed");

        var updatedWorkflow = await _context.AIWorkflows
            .FirstOrDefaultAsync(w => w.WorkflowId == workflow.WorkflowId);

        Assert.NotNull(updatedWorkflow);
        Assert.Equal("Completed", updatedWorkflow.Status);
        Assert.NotNull(updatedWorkflow.CompletedAt);
        Assert.True(updatedWorkflow.CompletedAt <= DateTime.UtcNow);
    }

    // AIW07: Completing a nonexistent workflow should throw an exception.
    [Fact(DisplayName = "AIW07 - Complete nonexistent workflow")]
    public async Task CompleteWorkflowAsync_ShouldThrowWhenWorkflowDoesNotExist()
    {
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CompleteWorkflowAsync(
                Guid.NewGuid(),
                "Completed"));

        Assert.Equal(
            "AI workflow was not found.",
            exception.Message);
    }

    // AIW08: Retrieve an existing workflow.
    [Fact(DisplayName = "AIW08 - Retrieve existing workflow")]
    public async Task GetWorkflowAsync_ShouldReturnWorkflow()
    {
        var workflow = await _service.CreateWorkflowAsync(
            "MedicalSummary",
            "Summarize medical history");

        var result = await _service.GetWorkflowAsync(workflow.WorkflowId);

        Assert.NotNull(result);
        Assert.Equal(workflow.WorkflowId, result.WorkflowId);
        Assert.Equal("MedicalSummary", result.WorkflowType);
        Assert.Equal("Started", result.Status);
        Assert.Equal("Summarize medical history", result.UserRequest);
    }

    // AIW09: Retrieving a nonexistent workflow should return null.
    [Fact(DisplayName = "AIW09 - Return null for nonexistent workflow")]
    public async Task GetWorkflowAsync_ShouldReturnNullWhenWorkflowDoesNotExist()
    {
        var result = await _service.GetWorkflowAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    // AIW10: Retrieve workflows ordered by StartedAt descending.
    [Fact(DisplayName = "AIW10 - Retrieve workflows in descending start-time order")]
    public async Task GetWorkflowsAsync_ShouldReturnNewestFirst()
    {
        var olderWorkflow = new AIWorkflow
        {
            WorkflowId = Guid.NewGuid(),
            WorkflowType = "OlderWorkflow",
            Status = "Completed",
            UserRequest = "Older request",
            StartedAt = DateTime.UtcNow.AddMinutes(-20)
        };

        var newerWorkflow = new AIWorkflow
        {
            WorkflowId = Guid.NewGuid(),
            WorkflowType = "NewerWorkflow",
            Status = "Started",
            UserRequest = "Newer request",
            StartedAt = DateTime.UtcNow
        };

        _context.AIWorkflows.AddRange(olderWorkflow, newerWorkflow);
        await _context.SaveChangesAsync();

        var workflows = await _service.GetWorkflowsAsync();

        Assert.Equal(2, workflows.Count);
        Assert.Equal(newerWorkflow.WorkflowId, workflows[0].WorkflowId);
        Assert.Equal(olderWorkflow.WorkflowId, workflows[1].WorkflowId);
    }

    // AIW11: Retrieve a workflow with its related entities.
    [Fact(DisplayName = "AIW11 - Retrieve workflow with steps, recommendations and approvals")]
    public async Task GetWorkflowAsync_ShouldIncludeRelatedEntities()
    {
        var workflow = await _service.CreateWorkflowAsync(
            "HealthcareAI",
            "Run healthcare AI analysis");

        var step = new AIWorkflowStep
        {
            StepId = Guid.NewGuid(),
            WorkflowId = workflow.WorkflowId,
            AgentName = "PatientTriageAgent",
            StepOrder = 1,
            Status = "Completed",
            InputData = "Patient symptoms",
            OutputData = "Triage completed",
            StartedAt = DateTime.UtcNow.AddMinutes(-2),
            CompletedAt = DateTime.UtcNow.AddMinutes(-1)
        };

        var recommendation = new AIRecommendation
        {
            RecommendationId = Guid.NewGuid(),
            WorkflowId = workflow.WorkflowId,
            RecommendationType = "PatientTriage",
            Recommendation = "Consult a healthcare professional",
            Reasoning = "Based on the reported symptoms",
            Status = "Pending",
            CreatedAt = DateTime.UtcNow
        };

        var approval = new AIApproval
        {
            ApprovalId = Guid.NewGuid(),
            WorkflowId = workflow.WorkflowId,
            Decision = "Pending",
            Comments = "Awaiting doctor review",
            RequestedAt = DateTime.UtcNow
        };

        _context.AIWorkflowSteps.Add(step);
        _context.AIRecommendations.Add(recommendation);
        _context.AIApprovals.Add(approval);

        await _context.SaveChangesAsync();

        // Clear tracked entities so the service must retrieve the
        // workflow and its related entities through its query.
        _context.ChangeTracker.Clear();

        var result = await _service.GetWorkflowAsync(workflow.WorkflowId);

        Assert.NotNull(result);

        Assert.Contains(
            result.Steps,
            s => s.StepId == step.StepId);

        Assert.Contains(
            result.Recommendations,
            r => r.RecommendationId == recommendation.RecommendationId);

        Assert.Contains(
            result.Approvals,
            a => a.ApprovalId == approval.ApprovalId);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}