using System.Text.Json;
using System.Text.Json.Nodes;
using SmartHealthcare.API.AI.Agents;
using SmartHealthcare.API.DTOs.AI;
using SmartHealthcare.API.Models;

namespace SmartHealthcare.API.AI.Orchestration;

public class AIOrchestrator : IAIOrchestrator
{
    private readonly IEnumerable<IHealthcareAgent> _agents;
    private readonly IAIWorkflowService _workflowService;
    private readonly IAIApprovalService _approvalService;
    private readonly IAIRecommendationService _recommendationService;

    public AIOrchestrator(
        IEnumerable<IHealthcareAgent> agents,
        IAIWorkflowService workflowService,
        IAIApprovalService approvalService,
        IAIRecommendationService recommendationService)
    {
        _agents = agents;
        _workflowService = workflowService;
        _approvalService = approvalService;
        _recommendationService = recommendationService;
    }

    public async Task<AgentResponse> ProcessAsync(
        AgentRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Request))
        {
            return new AgentResponse
            {
                Success = false,
                AgentName = "AIOrchestrator",
                Message = "AI request is required."
            };
        }

        AIWorkflow workflow =
            await _workflowService.CreateWorkflowAsync(
                workflowType: "HealthcareAI",
                userRequest: request.Request,
                patientId: request.PatientId,
                appointmentId: request.AppointmentId);

        IHealthcareAgent? selectedAgent =
            SelectAgent(request.Request);

        if (selectedAgent == null)
        {
            await _workflowService.CompleteWorkflowAsync(
                workflow.WorkflowId,
                "Failed");

            return new AgentResponse
            {
                Success = false,
                AgentName = "AIOrchestrator",
                Message = "No suitable AI agent was found.",
                WorkflowId = workflow.WorkflowId
            };
        }

        AIWorkflowStep step =
            await _workflowService.StartStepAsync(
                workflow.WorkflowId,
                selectedAgent.AgentName,
                1,
                JsonSerializer.Serialize(request));

        try
        {
            AgentResponse response =
                await selectedAgent.ExecuteAsync(
                    request,
                    workflow.WorkflowId);

            string status =
                response.Success
                    ? "Completed"
                    : "Failed";

            await _workflowService.CompleteStepAsync(
                step.StepId,
                status,
                response.Output ?? response.Message);

            if (response.Success)
            {
                await CreateRecommendationAsync(
                    workflow.WorkflowId,
                    response);
            }

            if (response.RequiresHumanApproval)
            {
                await _approvalService.CreateApprovalAsync(
                    workflow.WorkflowId,
                    response.Message);

                await _workflowService.CompleteWorkflowAsync(
                    workflow.WorkflowId,
                    "AwaitingApproval");
            }
            else
            {
                await _workflowService.CompleteWorkflowAsync(
                    workflow.WorkflowId,
                    status);
            }

            response.WorkflowId =
                workflow.WorkflowId;

            return response;
        }
        catch (Exception exception)
        {
            await _workflowService.CompleteStepAsync(
                step.StepId,
                "Failed",
                exception.Message);

            await _workflowService.CompleteWorkflowAsync(
                workflow.WorkflowId,
                "Failed");

            return new AgentResponse
            {
                Success = false,
                AgentName = selectedAgent.AgentName,
                Message = "AI agent execution failed.",
                Output = exception.Message,
                WorkflowId = workflow.WorkflowId
            };
        }
    }

    private async Task CreateRecommendationAsync(
        Guid workflowId,
        AgentResponse response)
    {
        string recommendationType =
            GetRecommendationType(
                response.AgentName);

        string recommendation =
            response.Output
            ?? response.Message;

        string? reasoning =
            ExtractReasoning(
                response.Output);

        await _recommendationService.CreateRecommendationAsync(
            workflowId,
            recommendationType,
            recommendation,
            reasoning);
    }

    private string GetRecommendationType(
        string agentName)
    {
        return agentName switch
        {
            "AppointmentSchedulingAgent" =>
                "AppointmentScheduling",

            "PatientTriageAgent" =>
                "PatientTriage",

            "MedicalSummaryAgent" =>
                "MedicalSummary",

            "BillingValidationAgent" =>
                "BillingValidation",

            _ =>
                "HealthcareAI"
        };
    }

    private string? ExtractReasoning(
        string? output)
    {
        if (string.IsNullOrWhiteSpace(output))
        {
            return null;
        }

        try
        {
            JsonNode? json =
                JsonNode.Parse(output);

            return json?["reasoning"]?.ToString()
                ?? json?["Reasoning"]?.ToString();
        }
        catch
        {
            return null;
        }
    }

    private IHealthcareAgent? SelectAgent(
        string request)
    {
        string normalizedRequest =
            request.ToLowerInvariant();

        bool isTriageRequest =
            normalizedRequest.Contains("symptom") ||
            normalizedRequest.Contains("pain") ||
            normalizedRequest.Contains("fever") ||
            normalizedRequest.Contains("bleeding") ||
            normalizedRequest.Contains("breathing") ||
            normalizedRequest.Contains("dizziness") ||
            normalizedRequest.Contains("vomiting") ||
            normalizedRequest.Contains("nausea") ||
            normalizedRequest.Contains("headache") ||
            normalizedRequest.Contains("chest") ||
            normalizedRequest.Contains("injury") ||
            normalizedRequest.Contains("emergency") ||
            normalizedRequest.Contains("sick") ||
            normalizedRequest.Contains("feel unwell");

        if (isTriageRequest)
        {
            return _agents.FirstOrDefault(
                agent =>
                    agent.AgentName ==
                    "PatientTriageAgent");
        }

        bool isMedicalSummaryRequest =
            normalizedRequest.Contains("medical history") ||
            normalizedRequest.Contains("medical summary") ||
            normalizedRequest.Contains("patient history") ||
            normalizedRequest.Contains("health history") ||
            normalizedRequest.Contains("summarize medical") ||
            normalizedRequest.Contains("summarize history") ||
            normalizedRequest.Contains("medical records");

        if (isMedicalSummaryRequest)
        {
            return _agents.FirstOrDefault(
                agent =>
                    agent.AgentName ==
                    "MedicalSummaryAgent");
        }

        bool isBillingRequest =
            normalizedRequest.Contains("bill") ||
            normalizedRequest.Contains("billing") ||
            normalizedRequest.Contains("payment") ||
            normalizedRequest.Contains("insurance") ||
            normalizedRequest.Contains("claim");

        if (isBillingRequest)
        {
            return _agents.FirstOrDefault(
                agent =>
                    agent.AgentName ==
                    "BillingValidationAgent");
        }

        bool isSchedulingRequest =
            normalizedRequest.Contains("appointment") ||
            normalizedRequest.Contains("book") ||
            normalizedRequest.Contains("schedule") ||
            normalizedRequest.Contains("doctor") ||
            normalizedRequest.Contains("specialist") ||
            normalizedRequest.Contains("available");

        if (isSchedulingRequest)
        {
            return _agents.FirstOrDefault(
                agent =>
                    agent.AgentName ==
                    "AppointmentSchedulingAgent");
        }

        return null;
    }
}