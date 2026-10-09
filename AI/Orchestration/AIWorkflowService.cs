using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SmartHealthcare.API.Data;
using SmartHealthcare.API.Models;

namespace SmartHealthcare.API.AI.Orchestration;

public class AIWorkflowService : IAIWorkflowService
{
    private readonly ApplicationDbContext _context;

    public AIWorkflowService(
        ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<AIWorkflow> CreateWorkflowAsync(
        string workflowType,
        string userRequest,
        Guid? patientId = null,
        Guid? appointmentId = null)
    {
        AIWorkflow workflow = new()
        {
            WorkflowId = Guid.NewGuid(),
            WorkflowType = workflowType,
            Status = "Started",
            UserRequest = userRequest,
            PatientId = patientId,
            AppointmentId = appointmentId,
            StartedAt = DateTime.UtcNow
        };

        _context.AIWorkflows.Add(workflow);

        await _context.SaveChangesAsync();

        return workflow;
    }

    public async Task<AIWorkflowStep> StartStepAsync(
        Guid workflowId,
        string agentName,
        int stepOrder,
        string? inputData = null)
    {
        AIWorkflowStep step = new()
        {
            StepId = Guid.NewGuid(),
            WorkflowId = workflowId,
            AgentName = agentName,
            StepOrder = stepOrder,
            Status = "Started",
            InputData = inputData,
            StartedAt = DateTime.UtcNow
        };

        _context.AIWorkflowSteps.Add(step);

        await _context.SaveChangesAsync();

        return step;
    }

    public async Task CompleteStepAsync(
        Guid stepId,
        string status,
        string? outputData = null)
    {
        AIWorkflowStep? step =
            await _context.AIWorkflowSteps
                .FirstOrDefaultAsync(
                    existingStep =>
                        existingStep.StepId == stepId);

        if (step == null)
        {
            throw new InvalidOperationException(
                "AI workflow step was not found.");
        }

        step.Status = status;
        step.OutputData = outputData;
        step.CompletedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
    }

    public async Task CompleteWorkflowAsync(
        Guid workflowId,
        string status)
    {
        AIWorkflow? workflow =
            await _context.AIWorkflows
                .FirstOrDefaultAsync(
                    existingWorkflow =>
                        existingWorkflow.WorkflowId == workflowId);

        if (workflow == null)
        {
            throw new InvalidOperationException(
                "AI workflow was not found.");
        }

        workflow.Status = status;
        workflow.CompletedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
    }


    public async Task<List<AIWorkflow>> GetWorkflowsAsync()
    {
        return await _context.AIWorkflows
            .AsNoTracking()
            .Include(workflow => workflow.Steps)
            .Include(workflow => workflow.Recommendations)
            .Include(workflow => workflow.Approvals)
            .OrderByDescending(workflow => workflow.StartedAt)
            .ToListAsync();
    }

    public async Task<AIWorkflow?> GetWorkflowAsync(
        Guid workflowId)
    {
        return await _context.AIWorkflows
            .AsNoTracking()
            .Include(workflow => workflow.Steps)
            .Include(workflow => workflow.Recommendations)
            .Include(workflow => workflow.Approvals)
            .FirstOrDefaultAsync(
                workflow =>
                    workflow.WorkflowId == workflowId);
    }
}