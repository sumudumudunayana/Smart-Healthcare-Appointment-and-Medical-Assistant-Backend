using System.Text.Json;
using SmartHealthcare.API.AI.LLM;
using SmartHealthcare.API.AI.Tools;
using SmartHealthcare.API.DTOs.AI;

namespace SmartHealthcare.API.AI.Agents;

public class AppointmentSchedulingAgent : IHealthcareAgent
{
    private readonly ILLMService _llmService;
    private readonly IAppointmentSchedulingTool _schedulingTool;

    public string AgentName =>
        "AppointmentSchedulingAgent";

    public AppointmentSchedulingAgent(
        ILLMService llmService,
        IAppointmentSchedulingTool schedulingTool)
    {
        _llmService = llmService;
        _schedulingTool = schedulingTool;
    }

    public async Task<AgentResponse> ExecuteAsync(
        AgentRequest request,
        Guid workflowId)
    {
        if (string.IsNullOrWhiteSpace(request.Request))
        {
            return new AgentResponse
            {
                Success = false,
                AgentName = AgentName,
                Message = "Appointment request is required.",
                WorkflowId = workflowId
            };
        }

        string systemPrompt = """
            You are the Appointment Scheduling Agent
            for a smart healthcare appointment system.

            Your responsibility is to understand a patient's
            appointment request.

            Extract:

            - Medical specialization
            - Preferred appointment date
            - Preferred appointment time period
            - Additional scheduling preferences

            Rules:

            1. Do not create an appointment.
            2. Do not modify an appointment.
            3. Do not select a doctor.
            4. Do not invent information.
            5. If information is missing, return an empty string.
            6. Return only the requested structured information.
            """;

        string jsonSchema = """
        {
            "type": "object",
            "properties": {
                "specialization": {
                    "type": "string"
                },
                "preferredDate": {
                    "type": "string"
                },
                "preferredTimePeriod": {
                    "type": "string"
                },
                "additionalPreferences": {
                    "type": "string"
                }
            },
            "required": [
                "specialization",
                "preferredDate",
                "preferredTimePeriod",
                "additionalPreferences"
            ]
        }
        """;

        AppointmentSchedulingRequest schedulingRequest =
            await _llmService.GenerateStructuredAsync<AppointmentSchedulingRequest>(
                systemPrompt,
                request.Request,
                jsonSchema);

        List<AppointmentAvailabilityResult> availableSlots =
            await _schedulingTool.FindAvailableSlotsAsync(
                schedulingRequest);

        return new AgentResponse
        {
            Success = true,
            AgentName = AgentName,
            Message =
                availableSlots.Count > 0
                    ? "Available appointment slots found."
                    : "No available appointment slots were found.",

            Output = JsonSerializer.Serialize(
                new
                {
                    Request = schedulingRequest,
                    AvailableSlots = availableSlots
                }),

            RequiresHumanApproval = false,
            WorkflowId = workflowId
        };
    }
}