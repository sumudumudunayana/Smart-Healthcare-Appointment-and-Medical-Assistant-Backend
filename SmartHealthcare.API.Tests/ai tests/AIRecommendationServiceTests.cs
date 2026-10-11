using Microsoft.EntityFrameworkCore;
using SmartHealthcare.API.AI.Orchestration;
using SmartHealthcare.API.Data;
using SmartHealthcare.API.Models;
using Xunit;

namespace SmartHealthcare.API.Tests;

public class AIRecommendationServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly AIRecommendationService _service;

    public AIRecommendationServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _service = new AIRecommendationService(_context);
    }

    // AIR01: Create a recommendation successfully.
    [Fact(DisplayName = "AIR01 - Create recommendation successfully")]
    public async Task CreateRecommendationAsync_ShouldCreateRecommendation()
    {
        var workflow = await CreateWorkflowAsync();

        var recommendation = await _service.CreateRecommendationAsync(
            workflow.WorkflowId,
            "PatientTriage",
            "Consult a healthcare professional",
            "Based on the symptoms provided");

        Assert.NotEqual(Guid.Empty, recommendation.RecommendationId);
        Assert.Equal(workflow.WorkflowId, recommendation.WorkflowId);
        Assert.Equal("PatientTriage", recommendation.RecommendationType);
        Assert.Equal(
            "Consult a healthcare professional",
            recommendation.Recommendation);
        Assert.Equal(
            "Based on the symptoms provided",
            recommendation.Reasoning);
        Assert.Equal("Pending", recommendation.Status);
        Assert.True(recommendation.CreatedAt <= DateTime.UtcNow);

        var savedRecommendation = await _context.AIRecommendations
            .FirstOrDefaultAsync(
                r => r.RecommendationId == recommendation.RecommendationId);

        Assert.NotNull(savedRecommendation);
        Assert.Equal(
            recommendation.RecommendationId,
            savedRecommendation.RecommendationId);
    }

    // AIR02: Create a recommendation without optional reasoning.
    [Fact(DisplayName = "AIR02 - Create recommendation without reasoning")]
    public async Task CreateRecommendationAsync_ShouldAllowNullReasoning()
    {
        var workflow = await CreateWorkflowAsync();

        var recommendation = await _service.CreateRecommendationAsync(
            workflow.WorkflowId,
            "MedicalSummary",
            "Generate a summary of the patient's medical history");

        Assert.NotNull(recommendation);
        Assert.Null(recommendation.Reasoning);
        Assert.Equal("Pending", recommendation.Status);

        var savedRecommendation = await _context.AIRecommendations
            .FirstOrDefaultAsync(
                r => r.RecommendationId == recommendation.RecommendationId);

        Assert.NotNull(savedRecommendation);
        Assert.Null(savedRecommendation.Reasoning);
    }

    // AIR03: Reject a recommendation for a nonexistent workflow.
    [Fact(DisplayName = "AIR03 - Reject recommendation for nonexistent workflow")]
    public async Task CreateRecommendationAsync_ShouldThrowWhenWorkflowDoesNotExist()
    {
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CreateRecommendationAsync(
                Guid.NewGuid(),
                "PatientTriage",
                "Test recommendation",
                "Test reasoning"));

        Assert.Equal(
            "AI workflow was not found.",
            exception.Message);

        Assert.Empty(await _context.AIRecommendations.ToListAsync());
    }

    // AIR04: Retrieve recommendations belonging to a workflow.
    [Fact(DisplayName = "AIR04 - Retrieve recommendations for a workflow")]
    public async Task GetWorkflowRecommendationsAsync_ShouldReturnRecommendationsForWorkflow()
    {
        var workflow = await CreateWorkflowAsync();

        var recommendation1 = new AIRecommendation
        {
            RecommendationId = Guid.NewGuid(),
            WorkflowId = workflow.WorkflowId,
            RecommendationType = "PatientTriage",
            Recommendation = "Recommendation one",
            Reasoning = "Reason one",
            Status = "Pending",
            CreatedAt = DateTime.UtcNow.AddMinutes(-10)
        };

        var recommendation2 = new AIRecommendation
        {
            RecommendationId = Guid.NewGuid(),
            WorkflowId = workflow.WorkflowId,
            RecommendationType = "MedicalSummary",
            Recommendation = "Recommendation two",
            Reasoning = "Reason two",
            Status = "Pending",
            CreatedAt = DateTime.UtcNow.AddMinutes(-5)
        };

        var unrelatedRecommendation = new AIRecommendation
        {
            RecommendationId = Guid.NewGuid(),
            WorkflowId = Guid.NewGuid(),
            RecommendationType = "BillingValidation",
            Recommendation = "Unrelated recommendation",
            Reasoning = "Unrelated reason",
            Status = "Pending",
            CreatedAt = DateTime.UtcNow
        };

        _context.AIRecommendations.AddRange(
            recommendation1,
            recommendation2,
            unrelatedRecommendation);

        await _context.SaveChangesAsync();

        var results = await _service.GetWorkflowRecommendationsAsync(
            workflow.WorkflowId);

        Assert.Equal(2, results.Count);

        Assert.All(
            results,
            recommendation =>
                Assert.Equal(workflow.WorkflowId, recommendation.WorkflowId));

        Assert.Contains(
            results,
            recommendation =>
                recommendation.RecommendationId ==
                recommendation1.RecommendationId);

        Assert.Contains(
            results,
            recommendation =>
                recommendation.RecommendationId ==
                recommendation2.RecommendationId);

        Assert.DoesNotContain(
            results,
            recommendation =>
                recommendation.RecommendationId ==
                unrelatedRecommendation.RecommendationId);
    }

    // AIR05: Return recommendations in ascending creation-time order.
    [Fact(DisplayName = "AIR05 - Return recommendations in creation-time order")]
    public async Task GetWorkflowRecommendationsAsync_ShouldReturnOldestFirst()
    {
        var workflow = await CreateWorkflowAsync();

        var olderRecommendation = new AIRecommendation
        {
            RecommendationId = Guid.NewGuid(),
            WorkflowId = workflow.WorkflowId,
            RecommendationType = "PatientTriage",
            Recommendation = "Older recommendation",
            Status = "Pending",
            CreatedAt = DateTime.UtcNow.AddMinutes(-20)
        };

        var newerRecommendation = new AIRecommendation
        {
            RecommendationId = Guid.NewGuid(),
            WorkflowId = workflow.WorkflowId,
            RecommendationType = "PatientTriage",
            Recommendation = "Newer recommendation",
            Status = "Pending",
            CreatedAt = DateTime.UtcNow
        };

        // Add the newer record first to ensure the service sorts by CreatedAt.
        _context.AIRecommendations.AddRange(
            newerRecommendation,
            olderRecommendation);

        await _context.SaveChangesAsync();

        var results = await _service.GetWorkflowRecommendationsAsync(
            workflow.WorkflowId);

        Assert.Equal(2, results.Count);

        Assert.Equal(
            olderRecommendation.RecommendationId,
            results[0].RecommendationId);

        Assert.Equal(
            newerRecommendation.RecommendationId,
            results[1].RecommendationId);
    }

    // AIR06: Return an empty list when no recommendations exist.
    [Fact(DisplayName = "AIR06 - Return empty list when no recommendations exist")]
    public async Task GetWorkflowRecommendationsAsync_ShouldReturnEmptyListWhenNoneExist()
    {
        var workflow = await CreateWorkflowAsync();

        var results = await _service.GetWorkflowRecommendationsAsync(
            workflow.WorkflowId);

        Assert.NotNull(results);
        Assert.Empty(results);
    }

    // AIR07: Return an empty list for a nonexistent workflow ID.
    [Fact(DisplayName = "AIR07 - Return empty list for nonexistent workflow ID")]
    public async Task GetWorkflowRecommendationsAsync_ShouldReturnEmptyListForUnknownWorkflow()
    {
        var results = await _service.GetWorkflowRecommendationsAsync(
            Guid.NewGuid());

        Assert.NotNull(results);
        Assert.Empty(results);
    }

    private async Task<AIWorkflow> CreateWorkflowAsync()
    {
        var workflow = new AIWorkflow
        {
            WorkflowId = Guid.NewGuid(),
            WorkflowType = "HealthcareAI",
            Status = "Started",
            UserRequest = "Test healthcare AI request",
            StartedAt = DateTime.UtcNow
        };

        _context.AIWorkflows.Add(workflow);
        await _context.SaveChangesAsync();

        return workflow;
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}