using System.Text.Json;
using Azure.AI.Projects;
using Azure.AI.Projects.Agents;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Foundry;
using Microsoft.Agents.AI.Workflows;
using Northstar.Coordinator.Agents;
using Northstar.Coordinator.Models;
using Northstar.Coordinator.Workflows;

var projectEndpoint = "https://northstar-foundry-dev-tw2608.services.ai.azure.com/api/projects/northstar-ops-dev";
var agentName = "northstar-coordinator";
var agentVersion = "10";
var modelDeployment = "gpt-5.4-mini";

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

Console.WriteLine(
    $"Resolved Foundry agent: {testedVersion.Name} " +
    $"version {testedVersion.Version}");

FoundryAgent northstar =
    projectClient.AsAIAgent(testedVersion);

var session = await northstar.CreateSessionAsync();


AIAgent entitlementSpecialist =
    EntitlementSpecialist.Create(
        projectClient,
        modelDeployment);

AIAgent diagnosticsSpecialist =
    DiagnosticsSpecialist.Create(
        projectClient,
        modelDeployment);

Workflow investigationWorkflow =
    CaseInvestigationWorkflow.Create(
        entitlementSpecialist,
        diagnosticsSpecialist,
        northstar);

var investigationRequest =
    new InvestigationRequest(
        prompt);


Console.WriteLine();

Console.WriteLine(
    "Support Request");

Console.WriteLine(
    "---------------");

Console.WriteLine(
    prompt);

Console.WriteLine();

NorthstarInvestigationResult? result = null;

await using StreamingRun workflowRun = await InProcessExecution
    .Concurrent.RunStreamingAsync(investigationWorkflow, investigationRequest);

await foreach (var workflowEvent in workflowRun.WatchStreamAsync())
{
    switch (workflowEvent)
    {
        case WorkflowOutputEvent outputEvent
            when outputEvent.Is<NorthstarInvestigationResult>():

            result = outputEvent.As<NorthstarInvestigationResult>();

            break;


        case WorkflowErrorEvent errorEvent:

            Console.WriteLine();

            Console.WriteLine("[WORKFLOW ERROR]");

            Console.WriteLine(errorEvent.Exception?.Message ??
                "Unknown workflow error.");

            break;


        case ExecutorFailedEvent executorFailed:

            Console.WriteLine();

            Console.WriteLine($"[EXECUTOR FAILED] " +
                $"{executorFailed.ExecutorId}");

            break;
    }
}

if (result is null)
{
    throw new InvalidOperationException(
        "The workflow did not produce a final " +
        "Northstar investigation result.");
}


var jsonOptions =
    new JsonSerializerOptions(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

Console.WriteLine();

Console.WriteLine(
    "=================================");

Console.WriteLine(
    "SPECIALIST EVIDENCE");

Console.WriteLine(
    "=================================");


Console.WriteLine();

Console.WriteLine(
    "Entitlement Specialist");

Console.WriteLine(
    "----------------------");

Console.WriteLine(
    JsonSerializer.Serialize(
        result.EntitlementAssessment,
        jsonOptions));


Console.WriteLine();

Console.WriteLine(
    "Diagnostics Specialist");

Console.WriteLine(
    "----------------------");

Console.WriteLine(
    JsonSerializer.Serialize(
        result.DiagnosticsAssessment,
        jsonOptions));


Console.WriteLine();

Console.WriteLine(
    "=================================");

Console.WriteLine(
    "NORTHSTAR COORDINATOR");

Console.WriteLine(
    "=================================");

Console.WriteLine();

Console.WriteLine(
    result.FinalResponse);