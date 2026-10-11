using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartHealthcare.API.AI.Agents;
using SmartHealthcare.API.Controllers;
using SmartHealthcare.API.DTOs.AI;
using Xunit;

namespace SmartHealthcare.API.Tests;

public class AISchedulingControllerTests
{
    private static FakeHealthcareAgent CreateSchedulingAgent(
        AgentResponse? response = null,
        Exception? exception = null)
    {
        return new FakeHealthcareAgent(
            "AppointmentSchedulingAgent",
            response ?? new AgentResponse
            {
                Success = true,
                AgentName = "AppointmentSchedulingAgent",
                Message = "Scheduling analysis completed.",
                Output = "Available appointment slots found."
            },
            exception);
    }

    // AISC01: Successfully process a scheduling request.
    [Fact(DisplayName = "AISC01 - Process scheduling request successfully")]
    public async Task TestScheduling_ShouldReturnOkWithAgentResponse()
    {
        var expectedResponse = new AgentResponse
        {
            Success = true,
            AgentName = "AppointmentSchedulingAgent",
            Message = "Scheduling analysis completed.",
            Output = "Available appointment slots found."
        };

        var agent = CreateSchedulingAgent(expectedResponse);
        var controller = new AISchedulingController(
            new IHealthcareAgent[] { agent });

        var request = new AgentRequest
        {
            Request = "Find an available doctor appointment."
        };

        var result = await controller.TestScheduling(request);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);

        Assert.Same(expectedResponse, okResult.Value);
        Assert.Equal(200, okResult.StatusCode);

        Assert.True(agent.ExecutionCount == 1);
        Assert.Same(request, agent.LastRequest);
        Assert.NotEqual(Guid.Empty, agent.LastWorkflowId);
    }

    // AISC02: Forward the original request to the scheduling agent.
    [Fact(DisplayName = "AISC02 - Forward request to scheduling agent")]
    public async Task TestScheduling_ShouldForwardOriginalRequest()
    {
        var agent = CreateSchedulingAgent();

        var controller = new AISchedulingController(
            new IHealthcareAgent[] { agent });

        var request = new AgentRequest
        {
            Request = "Book an appointment with a specialist.",
            PatientId = Guid.NewGuid(),
            AppointmentId = Guid.NewGuid(),
            BillId = Guid.NewGuid()
        };

        await controller.TestScheduling(request);

        Assert.NotNull(agent.LastRequest);

        Assert.Same(request, agent.LastRequest);
        Assert.Equal(request.Request, agent.LastRequest.Request);
        Assert.Equal(request.PatientId, agent.LastRequest.PatientId);
        Assert.Equal(
            request.AppointmentId,
            agent.LastRequest.AppointmentId);
        Assert.Equal(request.BillId, agent.LastRequest.BillId);
    }

    // AISC03: Generate and pass a non-empty workflow ID.
    [Fact(DisplayName = "AISC03 - Generate workflow ID")]
    public async Task TestScheduling_ShouldPassGeneratedWorkflowId()
    {
        var agent = CreateSchedulingAgent();

        var controller = new AISchedulingController(
            new IHealthcareAgent[] { agent });

        await controller.TestScheduling(
            new AgentRequest
            {
                Request = "Find an available appointment."
            });

        Assert.NotNull(agent.LastWorkflowId);
        Assert.NotEqual(Guid.Empty, agent.LastWorkflowId.Value);
    }

    // AISC04: Select the appointment scheduling agent when multiple agents exist.
    [Fact(DisplayName = "AISC04 - Select appointment scheduling agent")]
    public async Task Constructor_ShouldSelectSchedulingAgent()
    {
        var triageAgent = new FakeHealthcareAgent(
            "PatientTriageAgent",
            new AgentResponse
            {
                Success = true,
                AgentName = "PatientTriageAgent",
                Message = "Triage completed."
            });

        var schedulingAgent = CreateSchedulingAgent();

        var summaryAgent = new FakeHealthcareAgent(
            "MedicalSummaryAgent",
            new AgentResponse
            {
                Success = true,
                AgentName = "MedicalSummaryAgent",
                Message = "Medical summary completed."
            });

        var controller = new AISchedulingController(
            new IHealthcareAgent[]
            {
                triageAgent,
                summaryAgent,
                schedulingAgent
            });

        await controller.TestScheduling(
            new AgentRequest
            {
                Request = "Schedule an appointment."
            });

        Assert.Equal(1, schedulingAgent.ExecutionCount);
        Assert.Equal(0, triageAgent.ExecutionCount);
        Assert.Equal(0, summaryAgent.ExecutionCount);
    }

    // AISC05: Fail during construction when no scheduling agent exists.
    [Fact(DisplayName = "AISC05 - Throw when scheduling agent is missing")]
    public void Constructor_ShouldThrowWhenSchedulingAgentDoesNotExist()
    {
        var triageAgent = new FakeHealthcareAgent(
            "PatientTriageAgent",
            new AgentResponse
            {
                Success = true,
                AgentName = "PatientTriageAgent",
                Message = "Triage completed."
            });

        var summaryAgent = new FakeHealthcareAgent(
            "MedicalSummaryAgent",
            new AgentResponse
            {
                Success = true,
                AgentName = "MedicalSummaryAgent",
                Message = "Medical summary completed."
            });

        Assert.Throws<InvalidOperationException>(
            () => new AISchedulingController(
                new IHealthcareAgent[]
                {
                    triageAgent,
                    summaryAgent
                }));
    }

    // AISC06: Propagate exceptions thrown by the scheduling agent.
    [Fact(DisplayName = "AISC06 - Propagate scheduling agent exception")]
    public async Task TestScheduling_ShouldPropagateAgentException()
    {
        var agentException = new InvalidOperationException(
            "Scheduling service failed.");

        var agent = CreateSchedulingAgent(
            exception: agentException);

        var controller = new AISchedulingController(
            new IHealthcareAgent[] { agent });

        var request = new AgentRequest
        {
            Request = "Find an available appointment."
        };

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => controller.TestScheduling(request));

        Assert.Same(agentException, exception);
        Assert.Equal(1, agent.ExecutionCount);
    }

    // AISC07: Return the agent's unsuccessful response without modifying it.
    [Fact(DisplayName = "AISC07 - Return unsuccessful agent response")]
    public async Task TestScheduling_ShouldReturnUnsuccessfulAgentResponse()
    {
        var expectedResponse = new AgentResponse
        {
            Success = false,
            AgentName = "AppointmentSchedulingAgent",
            Message = "No appointment slots are currently available.",
            Output = null
        };

        var agent = CreateSchedulingAgent(expectedResponse);

        var controller = new AISchedulingController(
            new IHealthcareAgent[] { agent });

        var result = await controller.TestScheduling(
            new AgentRequest
            {
                Request = "Find an available appointment."
            });

        var okResult = Assert.IsType<OkObjectResult>(result.Result);

        var response = Assert.IsType<AgentResponse>(okResult.Value);

        Assert.Same(expectedResponse, response);
        Assert.False(response.Success);
        Assert.Equal(
            "No appointment slots are currently available.",
            response.Message);
    }

    // AISC08: Verify the controller's route and authorization attributes.
    [Fact(DisplayName = "AISC08 - Verify route and authorization attributes")]
    public void Controller_ShouldHaveExpectedRouteAndAuthorization()
    {
        var controllerType = typeof(AISchedulingController);

        var routeAttribute = controllerType
            .GetCustomAttributes(typeof(RouteAttribute), false)
            .Cast<RouteAttribute>()
            .Single();

        Assert.Equal("api/ai/scheduling", routeAttribute.Template);

        var authorizeAttribute = controllerType
            .GetCustomAttributes(typeof(AuthorizeAttribute), false)
            .Cast<AuthorizeAttribute>()
            .Single();

        Assert.Null(authorizeAttribute.Roles);
    }

    private sealed class FakeHealthcareAgent : IHealthcareAgent
    {
        private readonly AgentResponse _response;
        private readonly Exception? _exception;

        public string AgentName { get; }

        public int ExecutionCount { get; private set; }

        public AgentRequest? LastRequest { get; private set; }

        public Guid? LastWorkflowId { get; private set; }

        public FakeHealthcareAgent(
            string agentName,
            AgentResponse response,
            Exception? exception = null)
        {
            AgentName = agentName;
            _response = response;
            _exception = exception;
        }

        public Task<AgentResponse> ExecuteAsync(
            AgentRequest request,
            Guid workflowId)
        {
            ExecutionCount++;
            LastRequest = request;
            LastWorkflowId = workflowId;

            if (_exception != null)
            {
                throw _exception;
            }

            return Task.FromResult(_response);
        }
    }
}