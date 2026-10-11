using System.Text.Json;
using SmartHealthcare.API.AI.Agents;
using SmartHealthcare.API.AI.LLM;
using SmartHealthcare.API.DTOs.AI;

namespace SmartHealthcare.API.Tests;

public class PatientTriageAgentTests
{
    [Fact]
    public async Task ExecuteAsync_EmergencyResult_RequiresHumanApproval()
    {
        var llmService = new StubLlmService(new PatientTriageResult
        {
            UrgencyLevel = "Emergency",
            RecommendedSpecialization = "Emergency Medicine",
            EmergencyIndicator = true,
            Reasoning = "Reported symptoms indicate immediate risk."
        });
        var agent = new PatientTriageAgent(llmService);
        Guid workflowId = Guid.NewGuid();

        AgentResponse response = await agent.ExecuteAsync(
            new AgentRequest
            {
                Request = "Severe chest pain and difficulty breathing"
            },
            workflowId);

        Assert.True(response.Success);
        Assert.Equal("PatientTriageAgent", response.AgentName);
        Assert.Equal(workflowId, response.WorkflowId);
        Assert.True(response.RequiresHumanApproval);
        Assert.Equal(1, llmService.StructuredCallCount);

        PatientTriageResult? result =
            JsonSerializer.Deserialize<PatientTriageResult>(response.Output!);
        Assert.NotNull(result);
        Assert.Equal("Emergency", result.UrgencyLevel);
        Assert.True(result.EmergencyIndicator);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ExecuteAsync_EmptySymptoms_ReturnsFailureWithoutCallingLlm(
        string symptoms)
    {
        var llmService = new StubLlmService(new PatientTriageResult());
        var agent = new PatientTriageAgent(llmService);
        Guid workflowId = Guid.NewGuid();

        AgentResponse response = await agent.ExecuteAsync(
            new AgentRequest { Request = symptoms },
            workflowId);

        Assert.False(response.Success);
        Assert.Equal("Patient symptoms are required.", response.Message);
        Assert.Equal(workflowId, response.WorkflowId);
        Assert.Equal(0, llmService.StructuredCallCount);
    }

    private sealed class StubLlmService : ILLMService
    {
        private readonly object _result;

        public StubLlmService(object result)
        {
            _result = result;
        }

        public int StructuredCallCount { get; private set; }

        public Task<string> GenerateAsync(
            string systemPrompt,
            string userPrompt)
        {
            throw new NotSupportedException();
        }

        public Task<T> GenerateStructuredAsync<T>(
            string systemPrompt,
            string userPrompt,
            string jsonSchema)
        {
            StructuredCallCount++;
            T result = JsonSerializer.Deserialize<T>(
                JsonSerializer.Serialize(_result))!;
            return Task.FromResult(result);
        }
    }
}
