using System.Text.Json;
using SmartHealthcare.API.AI.LLM;
using SmartHealthcare.API.AI.Tools;
using SmartHealthcare.API.DTOs.AI;

namespace SmartHealthcare.API.AI.Agents;

public class MedicalSummaryAgent : IHealthcareAgent
{
    private readonly ILLMService _llmService;
    private readonly IMedicalSummaryTool _medicalSummaryTool;

    public string AgentName => "MedicalSummaryAgent";

    public MedicalSummaryAgent(
        ILLMService llmService,
        IMedicalSummaryTool medicalSummaryTool)
    {
        _llmService = llmService;
        _medicalSummaryTool = medicalSummaryTool;
    }

    public async Task<AgentResponse> ExecuteAsync(
        AgentRequest request,
        Guid workflowId)
    {
        if (!request.PatientId.HasValue)
        {
            return new AgentResponse
            {
                Success = false,
                AgentName = AgentName,
                Message = "Patient ID is required for medical summary.",
                WorkflowId = workflowId
            };
        }

        MedicalSummaryData? patientData =
            await _medicalSummaryTool.GetPatientMedicalDataAsync(
                request.PatientId.Value);

        if (patientData == null)
        {
            return new AgentResponse
            {
                Success = false,
                AgentName = AgentName,
                Message = "Patient medical data was not found.",
                WorkflowId = workflowId
            };
        }

        string patientDataJson =
            JsonSerializer.Serialize(
                patientData,
                new JsonSerializerOptions
                {
                    WriteIndented = true
                });

        string systemPrompt = """
            You are the Medical Summary Agent
            for a smart healthcare appointment system.

            Your responsibility is to summarize the patient's
            existing medical information for authorized healthcare
            professionals.

            The supplied patient data comes from the healthcare
            system database.

            Summarize:

            - Patient overview
            - Known allergies
            - Chronic conditions
            - Recent diagnoses
            - Recent treatments
            - Recent medications
            - Recent laboratory reports
            - Overall clinical summary
            - Limitations of the available information

            Important rules:

            1. Use only information provided in the patient data.
            2. Do not invent medical history.
            3. Do not create a diagnosis.
            4. Do not prescribe medication.
            5. Do not recommend treatment.
            6. Do not modify patient records.
            7. Do not modify appointments.
            8. Clearly indicate when information is unavailable.
            9. Keep the summary factual and concise.
            10. This summary is an AI-generated summary of existing
                records and should not replace professional clinical
                judgment.
            11. Return only the requested structured information.
            """;

        string userPrompt =
            $"""
            Generate a medical summary using the following
            patient information:

            {patientDataJson}

            Additional instructions:
            {request.Request}
            """;

        string jsonSchema = """
        {
            "type": "object",
            "properties": {
                "patientOverview": {
                    "type": "string"
                },
                "allergies": {
                    "type": "array",
                    "items": {
                        "type": "string"
                    }
                },
                "chronicConditions": {
                    "type": "array",
                    "items": {
                        "type": "string"
                    }
                },
                "recentDiagnoses": {
                    "type": "array",
                    "items": {
                        "type": "string"
                    }
                },
                "recentTreatments": {
                    "type": "array",
                    "items": {
                        "type": "string"
                    }
                },
                "recentMedications": {
                    "type": "array",
                    "items": {
                        "type": "string"
                    }
                },
                "recentLabReports": {
                    "type": "array",
                    "items": {
                        "type": "string"
                    }
                },
                "clinicalSummary": {
                    "type": "string"
                },
                "limitations": {
                    "type": "string"
                }
            },
            "required": [
                "patientOverview",
                "allergies",
                "chronicConditions",
                "recentDiagnoses",
                "recentTreatments",
                "recentMedications",
                "recentLabReports",
                "clinicalSummary",
                "limitations"
            ]
        }
        """;

        MedicalSummaryResult result =
            await _llmService.GenerateStructuredAsync<MedicalSummaryResult>(
                systemPrompt,
                userPrompt,
                jsonSchema);

        return new AgentResponse
        {
            Success = true,
            AgentName = AgentName,
            Message = "Medical summary generated successfully.",
            Output = JsonSerializer.Serialize(result),
            RequiresHumanApproval = false,
            WorkflowId = workflowId
        };
    }
}