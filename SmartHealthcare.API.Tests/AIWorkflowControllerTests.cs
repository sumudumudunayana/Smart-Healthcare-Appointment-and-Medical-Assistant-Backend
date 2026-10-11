using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartHealthcare.API.Controllers;
using SmartHealthcare.API.Data;
using SmartHealthcare.API.DTOs.AI;
using SmartHealthcare.API.Models;
using Xunit;

namespace SmartHealthcare.API.Tests;

public class AIWorkflowControllerTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly AIWorkflowController _controller;

    public AIWorkflowControllerTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _controller = new AIWorkflowController(_context);
    }

    // AICW01: Return an empty list when no workflows exist.
    [Fact(DisplayName = "AICW01 - Return empty workflow list")]
    public async Task GetWorkflows_ShouldReturnEmptyListWhenNoWorkflowsExist()
    {
        var result = await _controller.GetWorkflows();

        var okResult = Assert.IsType<OkObjectResult>(result.Result);

        var workflows = Assert.IsType<List<AIWorkflowResponse>>(
            okResult.Value);

        Assert.Empty(workflows);
    }

    // AICW02: Return all existing workflows.
    [Fact(DisplayName = "AICW02 - Retrieve all workflows")]
    public async Task GetWorkflows_ShouldReturnAllWorkflows()
    {
        var workflow1 = CreateWorkflow(
            "PatientTriage",
            "Assess patient symptoms");

        var workflow2 = CreateWorkflow(
            "MedicalSummary",
            "Summarize patient history");

        _context.AIWorkflows.AddRange(workflow1, workflow2);
        await _context.SaveChangesAsync();

        var result = await _controller.GetWorkflows();

        var okResult = Assert.IsType<OkObjectResult>(result.Result);

        var workflows = Assert.IsType<List<AIWorkflowResponse>>(
            okResult.Value);

        Assert.Equal(2, workflows.Count);

        Assert.Contains(
            workflows,
            w => w.WorkflowId == workflow1.WorkflowId);

        Assert.Contains(
            workflows,
            w => w.WorkflowId == workflow2.WorkflowId);
    }

    // AICW03: Return workflows ordered by newest start time first.
    [Fact(DisplayName = "AICW03 - Order workflows newest first")]
    public async Task GetWorkflows_ShouldReturnNewestWorkflowFirst()
    {
        var olderWorkflow = CreateWorkflow(
            "PatientTriage",
            "Older workflow",
            DateTime.UtcNow.AddHours(-2));

        var newerWorkflow = CreateWorkflow(
            "MedicalSummary",
            "Newer workflow",
            DateTime.UtcNow);

        _context.AIWorkflows.AddRange(
            olderWorkflow,
            newerWorkflow);

        await _context.SaveChangesAsync();

        var result = await _controller.GetWorkflows();

        var okResult = Assert.IsType<OkObjectResult>(result.Result);

        var workflows = Assert.IsType<List<AIWorkflowResponse>>(
            okResult.Value);

        Assert.Equal(2, workflows.Count);

        Assert.Equal(
            newerWorkflow.WorkflowId,
            workflows[0].WorkflowId);

        Assert.Equal(
            olderWorkflow.WorkflowId,
            workflows[1].WorkflowId);
    }

    // AICW04: Return workflow details correctly.
    [Fact(DisplayName = "AICW04 - Return workflow details")]
    public async Task GetWorkflow_ShouldReturnCorrectWorkflowDetails()
    {
        var workflow = CreateWorkflow(
            "PatientTriage",
            "The patient reports a headache.");

        workflow.Status = "Completed";

        _context.AIWorkflows.Add(workflow);
        await _context.SaveChangesAsync();

        var result = await _controller.GetWorkflow(
            workflow.WorkflowId);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);

        var response = Assert.IsType<AIWorkflowResponse>(
            okResult.Value);

        Assert.Equal(workflow.WorkflowId, response.WorkflowId);
        Assert.Equal(workflow.WorkflowType, response.WorkflowType);
        Assert.Equal(workflow.Status, response.Status);
        Assert.Equal(workflow.UserRequest, response.UserRequest);
        Assert.Equal(workflow.StartedAt, response.StartedAt);
        Assert.Null(response.CompletedAt);
        Assert.Null(response.AgentName);
        Assert.Empty(response.Steps);
        Assert.Empty(response.Recommendations);
        Assert.Empty(response.Approvals);
    }

    // AICW05: Return NotFound when a workflow does not exist.
    [Fact(DisplayName = "AICW05 - Return 404 for nonexistent workflow")]
    public async Task GetWorkflow_ShouldReturnNotFoundWhenWorkflowDoesNotExist()
    {
        var workflowId = Guid.NewGuid();

        var result = await _controller.GetWorkflow(workflowId);

        var notFoundResult = Assert.IsType<NotFoundObjectResult>(
            result.Result);

        Assert.Equal(404, notFoundResult.StatusCode);

        Assert.NotNull(notFoundResult.Value);

        var messageProperty = notFoundResult.Value
            .GetType()
            .GetProperty("message");

        Assert.NotNull(messageProperty);

        var message = messageProperty.GetValue(
            notFoundResult.Value);

        Assert.Equal(
            "AI workflow was not found.",
            message);
    }

    // AICW06: Include workflow steps, recommendations and approvals.
    [Fact(DisplayName = "AICW06 - Return workflow with related AI data")]
    public async Task GetWorkflow_ShouldReturnRelatedEntities()
    {
        var workflow = CreateWorkflow(
            "HealthcareAI",
            "Analyze patient information");

        var step = new AIWorkflowStep
        {
            StepId = Guid.NewGuid(),
            WorkflowId = workflow.WorkflowId,
            AgentName = "PatientTriageAgent",
            StepOrder = 1,
            Status = "Completed",
            InputData = "Patient reports fever",
            OutputData = "Medium urgency",
            StartedAt = DateTime.UtcNow.AddMinutes(-2),
            CompletedAt = DateTime.UtcNow.AddMinutes(-1)
        };

        var recommendation = new AIRecommendation
        {
            RecommendationId = Guid.NewGuid(),
            WorkflowId = workflow.WorkflowId,
            RecommendationType = "PatientTriage",
            Recommendation = "Consult a healthcare professional",
            Reasoning = "Based on reported symptoms",
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

        _context.AIWorkflows.Add(workflow);
        _context.AIWorkflowSteps.Add(step);
        _context.AIRecommendations.Add(recommendation);
        _context.AIApprovals.Add(approval);

        await _context.SaveChangesAsync();

        // Clear tracked entities so the controller reads the database.
        _context.ChangeTracker.Clear();

        var result = await _controller.GetWorkflow(
            workflow.WorkflowId);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);

        var response = Assert.IsType<AIWorkflowResponse>(
            okResult.Value);

        Assert.Equal(workflow.WorkflowId, response.WorkflowId);

        // Verify workflow steps.
        var returnedStep = Assert.Single(response.Steps);

        Assert.Equal(step.StepId, returnedStep.StepId);
        Assert.Equal(step.WorkflowId, returnedStep.WorkflowId);
        Assert.Equal(step.AgentName, returnedStep.AgentName);
        Assert.Equal(step.StepOrder, returnedStep.StepOrder);
        Assert.Equal(step.Status, returnedStep.Status);
        Assert.Equal(step.InputData, returnedStep.InputData);
        Assert.Equal(step.OutputData, returnedStep.OutputData);

        // Verify the first agent is exposed as AgentName.
        Assert.Equal("PatientTriageAgent", response.AgentName);

        // Verify recommendations.
        var returnedRecommendation = Assert.Single(
            response.Recommendations);

        Assert.Equal(
            recommendation.RecommendationId,
            returnedRecommendation.RecommendationId);

        Assert.Equal(
            recommendation.RecommendationType,
            returnedRecommendation.RecommendationType);

        Assert.Equal(
            recommendation.Recommendation,
            returnedRecommendation.Recommendation);

        Assert.Equal(
            recommendation.Reasoning,
            returnedRecommendation.Reasoning);

        Assert.Equal(
            recommendation.Status,
            returnedRecommendation.Status);

        // Verify approvals.
        var returnedApproval = Assert.Single(response.Approvals);

        Assert.Equal(
            approval.ApprovalId,
            returnedApproval.ApprovalId);

        Assert.Equal(
            approval.WorkflowId,
            returnedApproval.WorkflowId);

        Assert.Equal(
            approval.Decision,
            returnedApproval.Decision);

        Assert.Equal(
            approval.Comments,
            returnedApproval.Comments);

        Assert.Equal(
            workflow.WorkflowId,
            returnedApproval.WorkflowId);

        Assert.Equal(
            workflow.WorkflowType,
            returnedApproval.WorkflowType);

        Assert.Equal(
            workflow.Status,
            returnedApproval.WorkflowStatus);

        Assert.Equal(
            workflow.UserRequest,
            returnedApproval.UserRequest);
    }

    // AICW07: Return the earliest workflow step as the agent name.
    [Fact(DisplayName = "AICW07 - Select first agent by step order")]
    public async Task GetWorkflow_ShouldReturnFirstAgentByStepOrder()
    {
        var workflow = CreateWorkflow(
            "HealthcareAI",
            "Run multiple AI agents");

        var secondStep = new AIWorkflowStep
        {
            StepId = Guid.NewGuid(),
            WorkflowId = workflow.WorkflowId,
            AgentName = "MedicalSummaryAgent",
            StepOrder = 2,
            Status = "Completed",
            StartedAt = DateTime.UtcNow
        };

        var firstStep = new AIWorkflowStep
        {
            StepId = Guid.NewGuid(),
            WorkflowId = workflow.WorkflowId,
            AgentName = "PatientTriageAgent",
            StepOrder = 1,
            Status = "Completed",
            StartedAt = DateTime.UtcNow
        };

        _context.AIWorkflows.Add(workflow);

        // Add in reverse order to verify ordering is handled by the query.
        _context.AIWorkflowSteps.AddRange(secondStep, firstStep);

        await _context.SaveChangesAsync();

        var result = await _controller.GetWorkflow(
            workflow.WorkflowId);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);

        var response = Assert.IsType<AIWorkflowResponse>(
            okResult.Value);

        Assert.Equal(
            "PatientTriageAgent",
            response.AgentName);

        Assert.Equal(2, response.Steps.Count);

        Assert.Equal(
            "PatientTriageAgent",
            response.Steps[0].AgentName);

        Assert.Equal(
            "MedicalSummaryAgent",
            response.Steps[1].AgentName);
    }

    private static AIWorkflow CreateWorkflow(
        string workflowType,
        string userRequest,
        DateTime? startedAt = null)
    {
        return new AIWorkflow
        {
            WorkflowId = Guid.NewGuid(),
            WorkflowType = workflowType,
            Status = "Started",
            UserRequest = userRequest,
            StartedAt = startedAt ?? DateTime.UtcNow
        };
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}