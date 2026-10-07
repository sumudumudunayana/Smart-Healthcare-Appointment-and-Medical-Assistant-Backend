using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartHealthcare.API.Data;
using SmartHealthcare.API.DTOs.AI;

namespace SmartHealthcare.API.Controllers;

[ApiController]
[Route("api/ai/workflows")]
[Authorize(Roles = "Administrator,Doctor")]
public class AIWorkflowController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public AIWorkflowController(ApplicationDbContext context)
    {
        _context = context;
    }

    // =========================================================
    // GET ALL WORKFLOWS
    // =========================================================

    [HttpGet]
    public async Task<ActionResult<List<AIWorkflowResponse>>> GetWorkflows()
    {
        List<AIWorkflowResponse> workflows =
            await _context.AIWorkflows
                .AsNoTracking()

                // Explicitly load the relationships needed for display



                .OrderByDescending(workflow => workflow.StartedAt)

                .Select(workflow => new AIWorkflowResponse
                {
                    WorkflowId = workflow.WorkflowId,

                    AppointmentId = workflow.AppointmentId,

                    PatientId = workflow.PatientId,

                    // Patient -> User -> FullName
                    PatientName = workflow.Patient != null
                        ? workflow.Patient.User != null
                            ? workflow.Patient.User.FullName
                            : null
                        : null,

                    // Appointment -> Doctor -> User -> FullName
                    DoctorName = workflow.Appointment != null
                        ? workflow.Appointment.Doctor != null
                            ? workflow.Appointment.Doctor.User != null
                                ? workflow.Appointment.Doctor.User.FullName
                                : null
                            : null
                        : null,

                    AppointmentDate = workflow.Appointment != null
                        ? workflow.Appointment.AppointmentDate
                        : null,

                    AppointmentTime = workflow.Appointment != null
                        ? workflow.Appointment.AppointmentTime
                        : null,

                    WorkflowType = workflow.WorkflowType,

                    // First agent used in the workflow
                    AgentName = workflow.Steps
                        .OrderBy(step => step.StepOrder)
                        .Select(step => step.AgentName)
                        .FirstOrDefault(),

                    Status = workflow.Status,

                    UserRequest = workflow.UserRequest,

                    StartedAt = workflow.StartedAt,

                    CompletedAt = workflow.CompletedAt,

                    // =================================================
                    // WORKFLOW STEPS
                    // =================================================

                    Steps = workflow.Steps
                        .OrderBy(step => step.StepOrder)
                        .Select(step => new AIWorkflowStepResponse
                        {
                            StepId = step.StepId,

                            WorkflowId = step.WorkflowId,

                            AgentName = step.AgentName,

                            StepOrder = step.StepOrder,

                            Status = step.Status,

                            InputData = step.InputData,

                            OutputData = step.OutputData,

                            StartedAt = step.StartedAt,

                            CompletedAt = step.CompletedAt
                        })
                        .ToList(),

                    // =================================================
                    // AI RECOMMENDATIONS
                    // =================================================

                    Recommendations = workflow.Recommendations
                        .Select(recommendation =>
                            new AIRecommendationResponse
                            {
                                RecommendationId =
                                    recommendation.RecommendationId,

                                WorkflowId =
                                    recommendation.WorkflowId,

                                RecommendationType =
                                    recommendation.RecommendationType,

                                Recommendation =
                                    recommendation.Recommendation,

                                Reasoning =
                                    recommendation.Reasoning,

                                Status =
                                    recommendation.Status,

                                CreatedAt =
                                    recommendation.CreatedAt
                            })
                        .ToList(),

                    // =================================================
                    // AI APPROVALS
                    // =================================================

                    Approvals = workflow.Approvals
                        .Select(approval =>
                            new AIApprovalResponse
                            {
                                ApprovalId =
                                    approval.ApprovalId,

                                WorkflowId =
                                    approval.WorkflowId,

                                Decision =
                                    approval.Decision,

                                Comments =
                                    approval.Comments,

                                RequestedAt =
                                    approval.RequestedAt,

                                DecidedAt =
                                    approval.DecidedAt,

                                PatientId =
                                    workflow.PatientId,

                                AppointmentId =
                                    workflow.AppointmentId,

                                WorkflowType =
                                    workflow.WorkflowType,

                                WorkflowStatus =
                                    workflow.Status,

                                UserRequest =
                                    workflow.UserRequest
                            })
                        .ToList()
                })
                .ToListAsync();

        return Ok(workflows);
    }


    // =========================================================
    // GET SINGLE WORKFLOW
    // =========================================================

    [HttpGet("{workflowId:guid}")]
    public async Task<ActionResult<AIWorkflowResponse>> GetWorkflow(
        Guid workflowId)
    {
        AIWorkflowResponse? workflow =
            await _context.AIWorkflows
                .AsNoTracking()



                .Where(existingWorkflow =>
                    existingWorkflow.WorkflowId == workflowId)

                .Select(existingWorkflow =>
                    new AIWorkflowResponse
                    {
                        WorkflowId =
                            existingWorkflow.WorkflowId,

                        AppointmentId =
                            existingWorkflow.AppointmentId,

                        PatientId =
                            existingWorkflow.PatientId,

                        // Patient -> User -> FullName
                        PatientName =
                            existingWorkflow.Patient != null
                                ? existingWorkflow.Patient.User != null
                                    ? existingWorkflow.Patient.User.FullName
                                    : null
                                : null,

                        // Appointment -> Doctor -> User -> FullName
                        DoctorName =
                            existingWorkflow.Appointment != null
                                ? existingWorkflow.Appointment.Doctor != null
                                    ? existingWorkflow.Appointment.Doctor.User != null
                                        ? existingWorkflow.Appointment.Doctor.User.FullName
                                        : null
                                    : null
                                : null,

                        AppointmentDate =
                            existingWorkflow.Appointment != null
                                ? existingWorkflow.Appointment.AppointmentDate
                                : null,

                        AppointmentTime =
                            existingWorkflow.Appointment != null
                                ? existingWorkflow.Appointment.AppointmentTime
                                : null,

                        WorkflowType =
                            existingWorkflow.WorkflowType,

                        AgentName =
                            existingWorkflow.Steps
                                .OrderBy(step => step.StepOrder)
                                .Select(step => step.AgentName)
                                .FirstOrDefault(),

                        Status =
                            existingWorkflow.Status,

                        UserRequest =
                            existingWorkflow.UserRequest,

                        StartedAt =
                            existingWorkflow.StartedAt,

                        CompletedAt =
                            existingWorkflow.CompletedAt,

                        // =================================================
                        // WORKFLOW STEPS
                        // =================================================

                        Steps =
                            existingWorkflow.Steps
                                .OrderBy(step => step.StepOrder)
                                .Select(step =>
                                    new AIWorkflowStepResponse
                                    {
                                        StepId =
                                            step.StepId,

                                        WorkflowId =
                                            step.WorkflowId,

                                        AgentName =
                                            step.AgentName,

                                        StepOrder =
                                            step.StepOrder,

                                        Status =
                                            step.Status,

                                        InputData =
                                            step.InputData,

                                        OutputData =
                                            step.OutputData,

                                        StartedAt =
                                            step.StartedAt,

                                        CompletedAt =
                                            step.CompletedAt
                                    })
                                .ToList(),

                        // =================================================
                        // AI RECOMMENDATIONS
                        // =================================================

                        Recommendations =
                            existingWorkflow.Recommendations
                                .Select(recommendation =>
                                    new AIRecommendationResponse
                                    {
                                        RecommendationId =
                                            recommendation
                                                .RecommendationId,

                                        WorkflowId =
                                            recommendation
                                                .WorkflowId,

                                        RecommendationType =
                                            recommendation
                                                .RecommendationType,

                                        Recommendation =
                                            recommendation
                                                .Recommendation,

                                        Reasoning =
                                            recommendation
                                                .Reasoning,

                                        Status =
                                            recommendation
                                                .Status,

                                        CreatedAt =
                                            recommendation
                                                .CreatedAt
                                    })
                                .ToList(),

                        // =================================================
                        // AI APPROVALS
                        // =================================================

                        Approvals =
                            existingWorkflow.Approvals
                                .Select(approval =>
                                    new AIApprovalResponse
                                    {
                                        ApprovalId =
                                            approval.ApprovalId,

                                        WorkflowId =
                                            approval.WorkflowId,

                                        Decision =
                                            approval.Decision,

                                        Comments =
                                            approval.Comments,

                                        RequestedAt =
                                            approval.RequestedAt,

                                        DecidedAt =
                                            approval.DecidedAt,

                                        PatientId =
                                            existingWorkflow.PatientId,

                                        AppointmentId =
                                            existingWorkflow.AppointmentId,

                                        WorkflowType =
                                            existingWorkflow
                                                .WorkflowType,

                                        WorkflowStatus =
                                            existingWorkflow.Status,

                                        UserRequest =
                                            existingWorkflow
                                                .UserRequest
                                    })
                                .ToList()
                    })
                .FirstOrDefaultAsync();

        if (workflow == null)
        {
            return NotFound(new
            {
                message = "AI workflow was not found."
            });
        }

        return Ok(workflow);
    }
}