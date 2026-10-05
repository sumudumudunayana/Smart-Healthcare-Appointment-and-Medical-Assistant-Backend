using SmartHealthcare.API.AI.LLM;
using SmartHealthcare.API.DTOs.AI;

namespace SmartHealthcare.API.AI.Agents;

public class PatientTriageAgent : IHealthcareAgent
{
    private readonly ILLMService _llmService;

    public string AgentName =>
        "PatientTriageAgent";

    public PatientTriageAgent(
        ILLMService llmService)
    {
        _llmService = llmService;
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
                Message = "Patient symptoms are required.",
                WorkflowId = workflowId
            };
        }

        string systemPrompt = """
            You are the Patient Triage Agent
            for a smart healthcare appointment system.

            Your responsibility is to analyze the patient's
            reported symptoms and provide a preliminary
            triage recommendation.

            Analyze:

            - Urgency level
            - Recommended medical specialization
            - Whether the symptoms may indicate an emergency
            - Reasoning for the recommendation

            Important safety rules:

            1. Do not provide a definitive medical diagnosis.
            2. Do not prescribe medication.
            3. Do not provide treatment instructions.
            4. Do not create or modify appointments.
            5. Do not invent symptoms or medical history.
            6. If information is insufficient, state that clearly.
            7. Emergency indicators should be treated conservatively.
            8. This output is a recommendation for the healthcare
               workflow and may require human review.
            9. Return only the requested structured information.

            Urgency levels should use one of:

            - Routine
            - Soon
            - Urgent
            - Emergency
            """;

        string jsonSchema = """
        {
            "type": "object",
            "properties": {
                "urgencyLevel": {
                    "type": "string",
                    "enum": [
                        "Routine",
                        "Soon",
                        "Urgent",
                        "Emergency"
                    ]
                },
                "recommendedSpecialization": {
                    "type": "string"
                },
                "emergencyIndicator": {
                    "type": "boolean"
                },
                "reasoning": {
                    "type": "string"
                }
            },
            "required": [
                "urgencyLevel",
                "recommendedSpecialization",
                "emergencyIndicator",
                "reasoning"
            ]
        }
        """;

        PatientTriageResult result =
            await _llmService.GenerateStructuredAsync<PatientTriageResult>(
                systemPrompt,
                request.Request,
                jsonSchema);

        return new AgentResponse
        {
            Success = true,
            AgentName = AgentName,
            Message = "Patient symptoms analyzed for triage.",
            Output =
                System.Text.Json.JsonSerializer.Serialize(result),
            RequiresHumanApproval =
                result.EmergencyIndicator,
            WorkflowId = workflowId
        };
    }
}