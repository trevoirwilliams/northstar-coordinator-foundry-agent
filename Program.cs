using System.Diagnostics;
using System.Text.Json;
using Azure.AI.Projects;
using Azure.AI.Projects.Agents;
using Azure.Identity;
using Azure.Monitor.OpenTelemetry.Exporter;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Foundry;
using Microsoft.Agents.AI.Workflows;
using Northstar.Coordinator.Agents;
using Northstar.Coordinator.Models;
using Northstar.Coordinator.Workflows;
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var projectEndpoint = "https://northstar-foundry-dev-tw2608.services.ai.azure.com/api/projects/northstar-ops-dev"; // Replace with your actual project endpoint
var agentName = "northstar-coordinator";
var agentVersion = "12"; // Replace with your actual agent version
var modelDeployment = "gpt-5.4-mini"; // Replace with your actual model deployment

const string telemetrySourceName = "Northstar.Coordinator";
const string telemetryServiceName = "northstar-coordinator-local";

string applicationInsightsConnectionString = Environment.GetEnvironmentVariable(
        "APPLICATIONINSIGHTS_CONNECTION_STRING");
var otlpEndpoint = "http://localhost:4317";
using var activitySource = new ActivitySource(telemetrySourceName);


var otelResourceBuilder =
    ResourceBuilder
        .CreateDefault()
        .AddService(serviceName: telemetryServiceName,
            serviceVersion: "1.0.0");

using var tracerProvider =
    Sdk.CreateTracerProviderBuilder()
        .SetResourceBuilder(otelResourceBuilder)
        .AddSource(telemetrySourceName)
        .AddOtlpExporter(options => options.Endpoint = new Uri(otlpEndpoint))
        .Build();

using var appInsightsTracerProvider =
    Sdk.CreateTracerProviderBuilder()
        .SetResourceBuilder(otelResourceBuilder)
        .AddSource(telemetrySourceName)
        .AddAzureMonitorTraceExporter(options => options.ConnectionString = applicationInsightsConnectionString)
        .Build();

Console.Write("Enter a prompt for the Northstar Coordinator agent: ");
var prompt = Console.ReadLine();

if (string.IsNullOrWhiteSpace(prompt))
{
    Console.WriteLine("A prompt is required.");
    return;
}

Console.WriteLine();
Console.WriteLine("Northstar Coordinator Invocation");
Console.WriteLine("--------------------------------");
Console.WriteLine($"Agent:   {agentName}");
Console.WriteLine($"Version: {agentVersion}");
Console.WriteLine("Auth:    Microsoft Entra ID via Azure CLI");
Console.WriteLine();


var credential = new AzureCliCredential();

var projectClient = new AIProjectClient(
    new Uri(projectEndpoint),
    credential);

ProjectsAgentVersion testedVersion =
    await projectClient.AgentAdministrationClient.GetAgentVersionAsync(
        agentName,
        agentVersion);

Console.WriteLine($"Resolved Foundry agent: {testedVersion.Name} version {testedVersion.Version}");

AIAgent northstar =
    projectClient.AsAIAgent(testedVersion)
    .AsBuilder()
    .UseOpenTelemetry(sourceName: telemetrySourceName,
            configure: cfg => cfg.EnableSensitiveData = true)
            .Build();

AIAgent entitlementSpecialist =
    EntitlementSpecialist.Create(
        projectClient,
        modelDeployment,
        telemetrySourceName);

AIAgent diagnosticsSpecialist =
    DiagnosticsSpecialist.Create(
        projectClient,
        modelDeployment,
        telemetrySourceName);

Workflow investigationWorkflow =
    CaseInvestigationWorkflow.Create(
        entitlementSpecialist,
        diagnosticsSpecialist,
        northstar,
        activitySource);

var investigationRequest = new InvestigationRequest(prompt);

Console.WriteLine();

Console.WriteLine("Support Request");

Console.WriteLine("---------------");

Console.WriteLine(prompt);

Console.WriteLine();

NorthstarInvestigationResult? result = null;
string? workflowFailure = null;

await using StreamingRun workflowRun = await InProcessExecution
    .RunStreamingAsync(investigationWorkflow, investigationRequest);

await foreach (var workflowEvent in workflowRun.WatchStreamAsync())
{
    switch (workflowEvent)
    {
        case WorkflowOutputEvent outputEvent
            when outputEvent.Is<NorthstarInvestigationResult>():

            result = outputEvent.As<NorthstarInvestigationResult>();

            break;


        case WorkflowErrorEvent errorEvent:

            workflowFailure = errorEvent.Exception?.Message ??
                "Unknown workflow error.";

            Console.WriteLine();
            Console.WriteLine("[WORKFLOW ERROR]");
            Console.WriteLine(workflowFailure);

            break;


        case ExecutorFailedEvent executorFailed:

            workflowFailure = $"Executor failed: {executorFailed.ExecutorId}";

            Console.WriteLine();
            Console.WriteLine("[EXECUTOR FAILED]");
            Console.WriteLine(executorFailed.ExecutorId);

            break;
    }
}

if (result is null)
{
    Console.WriteLine();
    Console.WriteLine("=================================");

    Console.WriteLine("INVESTIGATION INCOMPLETE");

    Console.WriteLine("=================================");

    Console.WriteLine();

    Console.WriteLine(workflowFailure ?? "The workflow completed without producing a result.");

    Console.WriteLine();

    Console.WriteLine("No support recommendation should be treated as approved.");

    return;
}


var jsonOptions =
    new JsonSerializerOptions(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

Console.WriteLine();

Console.WriteLine("=================================");

Console.WriteLine("SPECIALIST EVIDENCE");

Console.WriteLine("=================================");


Console.WriteLine();

Console.WriteLine("Entitlement Specialist");

Console.WriteLine("----------------------");

Console.WriteLine(JsonSerializer.Serialize(
        result.EntitlementAssessment,
        jsonOptions));


Console.WriteLine();

Console.WriteLine("Diagnostics Specialist");

Console.WriteLine("----------------------");

Console.WriteLine(JsonSerializer.Serialize(
        result.DiagnosticsAssessment,
        jsonOptions));


Console.WriteLine();

Console.WriteLine("=================================");

Console.WriteLine("NORTHSTAR COORDINATOR");

Console.WriteLine("=================================");

Console.WriteLine();

Console.WriteLine(result.FinalResponse);