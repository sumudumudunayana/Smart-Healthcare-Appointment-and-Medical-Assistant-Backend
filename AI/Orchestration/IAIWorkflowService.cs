using SmartHealthcare.API.Models;

namespace SmartHealthcare.API.AI.Orchestration;

public interface IAIWorkflowService
{
    Task<AIWorkflow> CreateWorkflowAsync(
        string workflowType,
        string userRequest,
        Guid? patientId = null,
        Guid? appointmentId = null);

    Task<AIWorkflowStep> StartStepAsync(
        Guid workflowId,
        string agentName,
        int stepOrder,
        string? inputData = null);

    Task CompleteStepAsync(
        Guid stepId,
        string status,
        string? outputData = null);

    Task CompleteWorkflowAsync(
        Guid workflowId,
        string status);

    Task<List<AIWorkflow>> GetWorkflowsAsync();

    Task<AIWorkflow?> GetWorkflowAsync(
        Guid workflowId);
}