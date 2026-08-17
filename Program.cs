using System.Text.Json;
using Azure.AI.Projects;
using Azure.AI.Projects.Agents;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Foundry;
using Northstar.Coordinator.Agents;
using Northstar.Coordinator.Models;

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

AgentSession entitlementSession =
    await entitlementSpecialist.CreateSessionAsync();

AgentSession diagnosticsSession =
    await diagnosticsSpecialist.CreateSessionAsync();

Console.WriteLine();
Console.WriteLine("Request");
Console.WriteLine("-------");
Console.WriteLine(prompt);
Console.WriteLine();

var response = await northstar.RunAsync(
    prompt,
    session);


Console.WriteLine(response);


Console.WriteLine();
Console.WriteLine("2. Entitlement Specialist");
Console.WriteLine("-------------------------");

AgentResponse<SpecialistAssessment> entitlementResponse =
    await entitlementSpecialist.RunAsync<SpecialistAssessment>(
        prompt,
        entitlementSession);

var entitlementResponseJson = JsonSerializer.Serialize(
    entitlementResponse.Result,
    new JsonSerializerOptions
    {
        WriteIndented = true
    });
Console.WriteLine(entitlementResponseJson);


Console.WriteLine();
Console.WriteLine("3. Diagnostics Specialist");
Console.WriteLine("-------------------------");

AgentResponse<SpecialistAssessment> diagnosticsResponse =
    await diagnosticsSpecialist.RunAsync<SpecialistAssessment>(
        prompt,
        diagnosticsSession);

var diagnosticsResponseJson = JsonSerializer.Serialize(
    entitlementResponse.Result,
    new JsonSerializerOptions
    {
        WriteIndented = true
    });
Console.WriteLine(diagnosticsResponseJson);
