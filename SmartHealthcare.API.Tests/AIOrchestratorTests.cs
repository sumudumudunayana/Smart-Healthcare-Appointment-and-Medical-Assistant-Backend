using Microsoft.EntityFrameworkCore;
using SmartHealthcare.API.AI.Agents;
using SmartHealthcare.API.AI.Orchestration;
using SmartHealthcare.API.Data;
using SmartHealthcare.API.DTOs.AI;
using Xunit;

namespace SmartHealthcare.API.Tests;

public class AIOrchestratorTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly AIWorkflowService _workflowService;
    private readonly AIApprovalService _approvalService;
    private readonly AIRecommendationService _recommendationService;

    public AIOrchestratorTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);

        _workflowService = new AIWorkflowService(_context);
        _approvalService = new AIApprovalService(_context);
        _recommendationService = new AIRecommendationService(_context);
    }

    private AIOrchestrator CreateOrchestrator(
        params FakeHealthcareAgent[] agents)
    {
        return new AIOrchestrator(
            agents,
            _workflowService,
            _approvalService,
            _recommendationService);
    }

    private static FakeHealthcareAgent CreateSuccessfulAgent(
        string agentName,
        string output = "AI analysis completed",
        bool requiresHumanApproval = false)
    {
        return new FakeHealthcareAgent(
            agentName,
            new AgentResponse
            {
                Success = true,
                AgentName = agentName,
                Message = "AI analysis completed successfully.",
                Output = output,
                RequiresHumanApproval = requiresHumanApproval
            });
    }

    // AIO01: Empty requests should be rejected.
    [Theory(DisplayName = "AIO01 - Reject empty AI requests")]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("   ")]
    public async Task ProcessAsync_ShouldRejectEmptyRequest(
        string request)
    {
        var agent = CreateSuccessfulAgent(
            "PatientTriageAgent");

        var orchestrator = CreateOrchestrator(agent);

        var result = await orchestrator.ProcessAsync(
            new AgentRequest
            {
                Request = request
            });

        Assert.False(result.Success);
        Assert.Equal("AIOrchestrator", result.AgentName);
        Assert.Equal("AI request is required.", result.Message);
        Assert.Null(result.WorkflowId);

        Assert.Empty(_context.AIWorkflows);
        Assert.Equal(0, agent.ExecutionCount);
    }

    // AIO02: Route symptom-related requests to the triage agent.
    [Fact(DisplayName = "AIO02 - Route symptom request to triage agent")]
    public async Task ProcessAsync_ShouldSelectTriageAgent()
    {
        var agent = CreateSuccessfulAgent(
            "PatientTriageAgent",
            "Recommended urgency: Medium");

        var orchestrator = CreateOrchestrator(agent);

        var result = await orchestrator.ProcessAsync(
            new AgentRequest
            {
                Request = "The patient has a headache and fever."
            });

        Assert.True(result.Success);
        Assert.Equal("PatientTriageAgent", result.AgentName);
        Assert.NotNull(result.WorkflowId);
        Assert.Equal(1, agent.ExecutionCount);

        var workflow = await _context.AIWorkflows
            .FirstAsync(w => w.WorkflowId == result.WorkflowId);

        Assert.Equal("HealthcareAI", workflow.WorkflowType);
        Assert.Equal("Completed", workflow.Status);

        var step = await _context.AIWorkflowSteps
            .FirstAsync(s => s.WorkflowId == workflow.WorkflowId);

        Assert.Equal("PatientTriageAgent", step.AgentName);
        Assert.Equal("Completed", step.Status);
        Assert.Equal("Recommended urgency: Medium", step.OutputData);

        var recommendation = await _context.AIRecommendations
            .FirstAsync(r => r.WorkflowId == workflow.WorkflowId);

        Assert.Equal("PatientTriage", recommendation.RecommendationType);
        Assert.Equal(
            "Recommended urgency: Medium",
            recommendation.Recommendation);
        Assert.Equal("Pending", recommendation.Status);
    }

    // AIO03: Route medical history requests to the summary agent.
    [Fact(DisplayName = "AIO03 - Route medical history request")]
    public async Task ProcessAsync_ShouldSelectMedicalSummaryAgent()
    {
        var agent = CreateSuccessfulAgent(
            "MedicalSummaryAgent",
            "Medical history summary generated.");

        var orchestrator = CreateOrchestrator(agent);

        var result = await orchestrator.ProcessAsync(
            new AgentRequest
            {
                Request = "Summarize the patient's medical history."
            });

        Assert.True(result.Success);
        Assert.Equal("MedicalSummaryAgent", result.AgentName);

        var recommendation = await _context.AIRecommendations
            .FirstAsync(r => r.WorkflowId == result.WorkflowId);

        Assert.Equal("MedicalSummary", recommendation.RecommendationType);
        Assert.Equal(
            "Medical history summary generated.",
            recommendation.Recommendation);
    }

    // AIO04: Route billing requests to the billing agent.
    [Fact(DisplayName = "AIO04 - Route billing request")]
    public async Task ProcessAsync_ShouldSelectBillingAgent()
    {
        var agent = CreateSuccessfulAgent(
            "BillingValidationAgent",
            "Billing validation completed.");

        var orchestrator = CreateOrchestrator(agent);

        var result = await orchestrator.ProcessAsync(
            new AgentRequest
            {
                Request = "Validate the patient's medical bill."
            });

        Assert.True(result.Success);
        Assert.Equal("BillingValidationAgent", result.AgentName);

        var recommendation = await _context.AIRecommendations
            .FirstAsync(r => r.WorkflowId == result.WorkflowId);

        Assert.Equal("BillingValidation", recommendation.RecommendationType);
        Assert.Equal(
            "Billing validation completed.",
            recommendation.Recommendation);
    }

    // AIO05: Route appointment requests to the scheduling agent.
    [Fact(DisplayName = "AIO05 - Route appointment request")]
    public async Task ProcessAsync_ShouldSelectSchedulingAgent()
    {
        var agent = CreateSuccessfulAgent(
            "AppointmentSchedulingAgent",
            "Available appointment slots found.");

        var orchestrator = CreateOrchestrator(agent);

        var result = await orchestrator.ProcessAsync(
            new AgentRequest
            {
                Request = "Find an available doctor for an appointment."
            });

        Assert.True(result.Success);
        Assert.Equal("AppointmentSchedulingAgent", result.AgentName);

        var recommendation = await _context.AIRecommendations
            .FirstAsync(r => r.WorkflowId == result.WorkflowId);

        Assert.Equal(
            "AppointmentScheduling",
            recommendation.RecommendationType);
    }

    // AIO06: Create an approval when human review is required.
    [Fact(DisplayName = "AIO06 - Create approval for human review")]
    public async Task ProcessAsync_ShouldCreateApprovalWhenRequired()
    {
        var agent = CreateSuccessfulAgent(
            "PatientTriageAgent",
            "Urgent clinical review recommended.",
            requiresHumanApproval: true);

        var orchestrator = CreateOrchestrator(agent);

        var result = await orchestrator.ProcessAsync(
            new AgentRequest
            {
                Request = "Assess the patient's emergency symptoms."
            });

        Assert.True(result.Success);
        Assert.True(result.RequiresHumanApproval);
        Assert.NotNull(result.WorkflowId);

        var workflow = await _context.AIWorkflows
            .FirstAsync(w => w.WorkflowId == result.WorkflowId);

        Assert.Equal("AwaitingApproval", workflow.Status);

        var approval = await _context.AIApprovals
            .FirstOrDefaultAsync(a => a.WorkflowId == workflow.WorkflowId);

        Assert.NotNull(approval);
        Assert.Equal("Pending", approval.Decision);
        Assert.Equal(result.Message, approval.Comments);

        var recommendation = await _context.AIRecommendations
            .FirstOrDefaultAsync(r => r.WorkflowId == workflow.WorkflowId);

        Assert.NotNull(recommendation);
    }

    // AIO07: Fail when no suitable agent is available.
    [Fact(DisplayName = "AIO07 - Handle request without suitable agent")]
    public async Task ProcessAsync_ShouldFailWhenNoAgentMatches()
    {
        var agent = CreateSuccessfulAgent(
            "PatientTriageAgent");

        var orchestrator = CreateOrchestrator(agent);

        var result = await orchestrator.ProcessAsync(
            new AgentRequest
            {
                Request = "Tell me an interesting fact about space."
            });

        Assert.False(result.Success);
        Assert.Equal("AIOrchestrator", result.AgentName);
        Assert.Equal(
            "No suitable AI agent was found.",
            result.Message);
        Assert.NotNull(result.WorkflowId);

        var workflow = await _context.AIWorkflows
            .FirstAsync(w => w.WorkflowId == result.WorkflowId);

        Assert.Equal("Failed", workflow.Status);
        Assert.Equal(0, agent.ExecutionCount);
        Assert.Empty(_context.AIWorkflowSteps);
        Assert.Empty(_context.AIRecommendations);
    }

    // AIO08: Mark the workflow as failed when an agent returns failure.
    [Fact(DisplayName = "AIO08 - Handle unsuccessful agent response")]
    public async Task ProcessAsync_ShouldFailWhenAgentReturnsFailure()
    {
        var agent = new FakeHealthcareAgent(
            "PatientTriageAgent",
            new AgentResponse
            {
                Success = false,
                AgentName = "PatientTriageAgent",
                Message = "Unable to complete triage."
            });

        var orchestrator = CreateOrchestrator(agent);

        var result = await orchestrator.ProcessAsync(
            new AgentRequest
            {
                Request = "Assess the patient's symptoms."
            });

        Assert.False(result.Success);
        Assert.Equal(
            "Unable to complete triage.",
            result.Message);
        Assert.NotNull(result.WorkflowId);

        var workflow = await _context.AIWorkflows
            .FirstAsync(w => w.WorkflowId == result.WorkflowId);

        Assert.Equal("Failed", workflow.Status);

        var step = await _context.AIWorkflowSteps
            .FirstAsync(s => s.WorkflowId == workflow.WorkflowId);

        Assert.Equal("Failed", step.Status);
        Assert.Equal("Unable to complete triage.", step.OutputData);

        Assert.Empty(_context.AIRecommendations);
    }

    // AIO09: Handle exceptions raised during agent execution.
    [Fact(DisplayName = "AIO09 - Handle agent execution exception")]
    public async Task ProcessAsync_ShouldHandleAgentException()
    {
        var agent = new FakeHealthcareAgent(
            "PatientTriageAgent",
            exception: new InvalidOperationException(
                "Simulated agent failure."));

        var orchestrator = CreateOrchestrator(agent);

        var result = await orchestrator.ProcessAsync(
            new AgentRequest
            {
                Request = "Assess the patient's chest pain."
            });

        Assert.False(result.Success);
        Assert.Equal("PatientTriageAgent", result.AgentName);
        Assert.Equal(
            "AI agent execution failed.",
            result.Message);
        Assert.Equal(
            "Simulated agent failure.",
            result.Output);
        Assert.NotNull(result.WorkflowId);

        var workflow = await _context.AIWorkflows
            .FirstAsync(w => w.WorkflowId == result.WorkflowId);

        Assert.Equal("Failed", workflow.Status);

        var step = await _context.AIWorkflowSteps
            .FirstAsync(s => s.WorkflowId == workflow.WorkflowId);

        Assert.Equal("Failed", step.Status);
        Assert.Equal("Simulated agent failure.", step.OutputData);

        Assert.Empty(_context.AIRecommendations);
    }

    // AIO10: Pass patient and appointment IDs to the workflow and agent.
    [Fact(DisplayName = "AIO10 - Preserve patient and appointment IDs")]
    public async Task ProcessAsync_ShouldPreserveRequestIdentifiers()
    {
        var patientId = Guid.NewGuid();
        var appointmentId = Guid.NewGuid();

        var agent = CreateSuccessfulAgent(
            "AppointmentSchedulingAgent");

        var orchestrator = CreateOrchestrator(agent);

        var result = await orchestrator.ProcessAsync(
            new AgentRequest
            {
                Request = "Schedule a doctor appointment.",
                PatientId = patientId,
                AppointmentId = appointmentId
            });

        Assert.True(result.Success);
        Assert.NotNull(agent.LastRequest);

        Assert.Equal(patientId, agent.LastRequest.PatientId);
        Assert.Equal(appointmentId, agent.LastRequest.AppointmentId);

        var workflow = await _context.AIWorkflows
            .FirstAsync(w => w.WorkflowId == result.WorkflowId);

        Assert.Equal(patientId, workflow.PatientId);
        Assert.Equal(appointmentId, workflow.AppointmentId);
    }

    // AIO11: Extract reasoning from JSON agent output.
    [Fact(DisplayName = "AIO11 - Extract recommendation reasoning")]
    public async Task ProcessAsync_ShouldExtractReasoningFromJsonOutput()
    {
        const string output =
            """{"summary":"Patient needs follow-up","reasoning":"Symptoms indicate further clinical assessment."}""";

        var agent = CreateSuccessfulAgent(
            "PatientTriageAgent",
            output);

        var orchestrator = CreateOrchestrator(agent);

        var result = await orchestrator.ProcessAsync(
            new AgentRequest
            {
                Request = "Assess the patient's fever."
            });

        Assert.True(result.Success);

        var recommendation = await _context.AIRecommendations
            .FirstAsync(r => r.WorkflowId == result.WorkflowId);

        Assert.Equal(output, recommendation.Recommendation);
        Assert.Equal(
            "Symptoms indicate further clinical assessment.",
            recommendation.Reasoning);
    }

    // AIO12: Use the message when agent output is null.
    [Fact(DisplayName = "AIO12 - Use message when output is null")]
    public async Task ProcessAsync_ShouldUseMessageWhenOutputIsNull()
    {
        var agent = new FakeHealthcareAgent(
            "PatientTriageAgent",
            new AgentResponse
            {
                Success = true,
                AgentName = "PatientTriageAgent",
                Message = "Triage recommendation generated.",
                Output = null
            });

        var orchestrator = CreateOrchestrator(agent);

        var result = await orchestrator.ProcessAsync(
            new AgentRequest
            {
                Request = "Assess the patient's nausea."
            });

        Assert.True(result.Success);

        var step = await _context.AIWorkflowSteps
            .FirstAsync(s => s.WorkflowId == result.WorkflowId);

        Assert.Equal(
            "Triage recommendation generated.",
            step.OutputData);

        var recommendation = await _context.AIRecommendations
            .FirstAsync(r => r.WorkflowId == result.WorkflowId);

        Assert.Equal(
            "Triage recommendation generated.",
            recommendation.Recommendation);
        Assert.Null(recommendation.Reasoning);
    }

    // AIO13: Triage keywords take precedence over scheduling keywords.
    [Fact(DisplayName = "AIO13 - Prioritize triage when request contains symptom keywords")]
    public async Task ProcessAsync_ShouldPrioritizeTriageAgent()
    {
        var triageAgent = CreateSuccessfulAgent(
            "PatientTriageAgent");

        var schedulingAgent = CreateSuccessfulAgent(
            "AppointmentSchedulingAgent");

        var orchestrator = CreateOrchestrator(
            schedulingAgent,
            triageAgent);

        var result = await orchestrator.ProcessAsync(
            new AgentRequest
            {
                Request = "I have chest pain and need an appointment."
            });

        Assert.True(result.Success);
        Assert.Equal("PatientTriageAgent", result.AgentName);
        Assert.Equal(1, triageAgent.ExecutionCount);
        Assert.Equal(0, schedulingAgent.ExecutionCount);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    private sealed class FakeHealthcareAgent : IHealthcareAgent
    {
        private readonly AgentResponse _response;
        private readonly Exception? _exception;

        public string AgentName { get; }

        public int ExecutionCount { get; private set; }

        public AgentRequest? LastRequest { get; private set; }

        public Guid? LastWorkflowId { get; private set; }

        public FakeHealthcareAgent(
            string agentName,
            AgentResponse? response = null,
            Exception? exception = null)
        {
            AgentName = agentName;
            _response = response ?? new AgentResponse
            {
                Success = true,
                AgentName = agentName,
                Message = "AI analysis completed successfully.",
                Output = "AI analysis completed"
            };

            _exception = exception;
        }

        public Task<AgentResponse> ExecuteAsync(
            AgentRequest request,
            Guid workflowId)
        {
            ExecutionCount++;
            LastRequest = request;
            LastWorkflowId = workflowId;

            if (_exception != null)
            {
                throw _exception;
            }

            return Task.FromResult(_response);
        }
    }
}