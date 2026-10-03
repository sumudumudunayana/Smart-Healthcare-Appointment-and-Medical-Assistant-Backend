using System.Text.Json;
using SmartHealthcare.API.AI.LLM;
using SmartHealthcare.API.AI.Tools;
using SmartHealthcare.API.DTOs.AI;

namespace SmartHealthcare.API.AI.Agents;

public class BillingValidationAgent : IHealthcareAgent
{
    private readonly ILLMService _llmService;
    private readonly IBillingValidationTool _billingValidationTool;

    public string AgentName => "BillingValidationAgent";

    public BillingValidationAgent(
        ILLMService llmService,
        IBillingValidationTool billingValidationTool)
    {
        _llmService = llmService;
        _billingValidationTool = billingValidationTool;
    }

    public async Task<AgentResponse> ExecuteAsync(
        AgentRequest request,
        Guid workflowId)
    {
        if (!request.Request.Contains("bill", StringComparison.OrdinalIgnoreCase) &&
            !request.Request.Contains("billing", StringComparison.OrdinalIgnoreCase) &&
            !request.Request.Contains("payment", StringComparison.OrdinalIgnoreCase) &&
            !request.Request.Contains("insurance", StringComparison.OrdinalIgnoreCase))
        {
            return new AgentResponse
            {
                Success = false,
                AgentName = AgentName,
                Message = "A billing-related request is required.",
                WorkflowId = workflowId
            };
        }

        Guid? billId = request.BillId;

        if (!billId.HasValue)
        {
            return new AgentResponse
            {
                Success = false,
                AgentName = AgentName,
                Message = "Bill ID is required for billing validation.",
                WorkflowId = workflowId
            };
        }

        BillingValidationData? billingData =
            await _billingValidationTool.GetBillingDataAsync(
                billId.Value);

        if (billingData == null)
        {
            return new AgentResponse
            {
                Success = false,
                AgentName = AgentName,
                Message = "Billing information was not found.",
                WorkflowId = workflowId
            };
        }

        List<string> deterministicChecks = new();

        decimal successfulPayments =
            billingData.Payments
                .Where(payment =>
                    payment.PaymentStatus.Equals(
                        "Completed",
                        StringComparison.OrdinalIgnoreCase))
                .Sum(payment => payment.Amount);

        if (successfulPayments > billingData.TotalAmount)
        {
            deterministicChecks.Add(
                $"Completed payments ({successfulPayments}) exceed " +
                $"the bill total ({billingData.TotalAmount}).");
        }

        decimal totalClaimAmount =
            billingData.InsuranceClaims
                .Where(claim =>
                    !claim.Status.Equals(
                        "Rejected",
                        StringComparison.OrdinalIgnoreCase))
                .Sum(claim => claim.ClaimAmount);

        if (totalClaimAmount > billingData.TotalAmount)
        {
            deterministicChecks.Add(
                $"Insurance claim amounts ({totalClaimAmount}) exceed " +
                $"the bill total ({billingData.TotalAmount}).");
        }

        foreach (var claim in billingData.InsuranceClaims)
        {
            var policy =
                billingData.InsurancePolicies
                    .FirstOrDefault(
                        existingPolicy =>
                            existingPolicy.InsurancePolicyId ==
                            claim.InsurancePolicyId);

            if (policy == null)
            {
                deterministicChecks.Add(
                    $"Insurance claim {claim.ClaimId} references " +
                    "a policy that could not be found.");
                continue;
            }

            if (claim.ClaimAmount > policy.CoverageAmount)
            {
                deterministicChecks.Add(
                    $"Insurance claim {claim.ClaimId} exceeds " +
                    $"the policy coverage amount.");
            }

            if (billingData.GeneratedDate < policy.StartDate ||
                billingData.GeneratedDate > policy.EndDate)
            {
                deterministicChecks.Add(
                    $"Insurance claim {claim.ClaimId} is associated " +
                    "with a policy outside its active date range.");
            }
        }

        string billingDataJson =
            JsonSerializer.Serialize(
                billingData,
                new JsonSerializerOptions
                {
                    WriteIndented = true
                });

        string deterministicChecksJson =
            JsonSerializer.Serialize(
                deterministicChecks,
                new JsonSerializerOptions
                {
                    WriteIndented = true
                });

        string systemPrompt = """
            You are the Billing Validation Agent
            for a smart healthcare appointment system.

            Your responsibility is to review billing information
            and identify potential inconsistencies, duplicate
            payments, insurance issues, or suspicious billing
            patterns.

            The supplied billing information comes from the
            healthcare system database.

            Important rules:

            1. Use only the supplied billing information.
            2. Do not invent billing information.
            3. Do not modify bills.
            4. Do not create payments.
            5. Do not approve insurance claims.
            6. Do not reject insurance claims.
            7. Do not make financial transactions.
            8. Treat deterministic backend validation checks as
               factual findings.
            9. Identify possible issues from the supplied data.
            10. Clearly distinguish potential issues from confirmed
                validation failures.
            11. Financial corrections and insurance decisions may
                require human review.
            12. Return only the requested structured information.
            """;

        string userPrompt =
            $"""
            Review the following billing information.

            Billing data:
            {billingDataJson}

            Deterministic backend validation checks:
            {deterministicChecksJson}

            Additional request:
            {request.Request}

            Determine whether the billing information appears
            internally consistent based only on the supplied data.
            """;

        string jsonSchema = """
        {
            "type": "object",
            "properties": {
                "isValid": {
                    "type": "boolean"
                },
                "overallAssessment": {
                    "type": "string"
                },
                "issues": {
                    "type": "array",
                    "items": {
                        "type": "string"
                    }
                },
                "warnings": {
                    "type": "array",
                    "items": {
                        "type": "string"
                    }
                },
                "validationChecks": {
                    "type": "array",
                    "items": {
                        "type": "string"
                    }
                },
                "requiresHumanReview": {
                    "type": "boolean"
                },
                "reasoning": {
                    "type": "string"
                }
            },
            "required": [
                "isValid",
                "overallAssessment",
                "issues",
                "warnings",
                "validationChecks",
                "requiresHumanReview",
                "reasoning"
            ]
        }
        """;

        BillingValidationResult result =
            await _llmService.GenerateStructuredAsync<BillingValidationResult>(
                systemPrompt,
                userPrompt,
                jsonSchema);

        bool requiresHumanReview =
            result.RequiresHumanReview ||
            deterministicChecks.Count > 0;

        return new AgentResponse
        {
            Success = true,
            AgentName = AgentName,
            Message = "Billing information validated successfully.",
            Output = JsonSerializer.Serialize(
                new
                {
                    Result = result,
                    DeterministicChecks = deterministicChecks
                }),
            RequiresHumanApproval = requiresHumanReview,
            WorkflowId = workflowId
        };
    }
}