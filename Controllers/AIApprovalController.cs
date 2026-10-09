using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartHealthcare.API.AI.Orchestration;
using SmartHealthcare.API.DTOs.AI;
using SmartHealthcare.API.Models;

namespace SmartHealthcare.API.Controllers;

[ApiController]
[Route("api/ai/approvals")]
[Authorize(Roles = "Administrator,Doctor")]
public class AIApprovalController : ControllerBase
{
    private readonly IAIApprovalService _approvalService;

    public AIApprovalController(
        IAIApprovalService approvalService)
    {
        _approvalService = approvalService;
    }

    [HttpGet("pending")]
    public async Task<ActionResult<List<AIApprovalResponse>>> GetPendingApprovals()
    {
        List<AIApproval> approvals =
            await _approvalService.GetPendingApprovalsAsync();

        List<AIApprovalResponse> response =
            approvals
                .Where(approval => approval.Workflow != null)
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

                        WorkflowType =
                            approval.Workflow!.WorkflowType,

                        WorkflowStatus =
                            approval.Workflow.Status,

                        UserRequest =
                            approval.Workflow.UserRequest,

                        PatientId =
                            approval.Workflow.PatientId,

                        AppointmentId =
                            approval.Workflow.AppointmentId
                    })
                .ToList();

        return Ok(response);
    }

    [HttpGet("{approvalId:guid}")]
    public async Task<ActionResult<AIApprovalResponse>> GetApproval(
        Guid approvalId)
    {
        AIApproval? approval =
            await _approvalService.GetApprovalAsync(
                approvalId);

        if (approval == null)
        {
            return NotFound(
                new
                {
                    message = "AI approval was not found."
                });
        }

        if (approval.Workflow == null)
        {
            return NotFound(
                new
                {
                    message =
                        "The workflow associated with this approval was not found."
                });
        }

        AIApprovalResponse response =
            new()
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

                WorkflowType =
                    approval.Workflow.WorkflowType,

                WorkflowStatus =
                    approval.Workflow.Status,

                UserRequest =
                    approval.Workflow.UserRequest,

                PatientId =
                    approval.Workflow.PatientId,

                AppointmentId =
                    approval.Workflow.AppointmentId
            };

        return Ok(response);
    }

    [HttpPost("{approvalId:guid}/approve")]
    public async Task<ActionResult<AIApprovalResponse>> Approve(
        Guid approvalId,
        [FromBody] AIApprovalDecisionRequest request)
    {
        Guid userId =
            GetCurrentUserId();

        AIApproval? approval =
            await _approvalService.ApproveAsync(
                approvalId,
                userId,
                request.Comments);

        if (approval == null)
        {
            return NotFound(
                new
                {
                    message = "AI approval was not found."
                });
        }

        return Ok(
            MapApprovalResponse(approval));
    }

    [HttpPost("{approvalId:guid}/reject")]
    public async Task<ActionResult<AIApprovalResponse>> Reject(
        Guid approvalId,
        [FromBody] AIApprovalDecisionRequest request)
    {
        Guid userId =
            GetCurrentUserId();

        AIApproval? approval =
            await _approvalService.RejectAsync(
                approvalId,
                userId,
                request.Comments);

        if (approval == null)
        {
            return NotFound(
                new
                {
                    message = "AI approval was not found."
                });
        }

        return Ok(
            MapApprovalResponse(approval));
    }

    private Guid GetCurrentUserId()
    {
        string? userId =
            User.FindFirst(
                System.Security.Claims.ClaimTypes.NameIdentifier)
            ?.Value;

        if (!Guid.TryParse(userId, out Guid parsedUserId))
        {
            throw new UnauthorizedAccessException(
                "Authenticated user ID was not found.");
        }

        return parsedUserId;
    }

    private static AIApprovalResponse MapApprovalResponse(
        AIApproval approval)
    {
        return new AIApprovalResponse
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

            WorkflowType =
                approval.Workflow?.WorkflowType
                ?? string.Empty,

            WorkflowStatus =
                approval.Workflow?.Status
                ?? string.Empty,

            UserRequest =
                approval.Workflow?.UserRequest,

            PatientId =
                approval.Workflow?.PatientId,

            AppointmentId =
                approval.Workflow?.AppointmentId
        };
    }
}