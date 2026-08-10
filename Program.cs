using Azure.AI.Projects;
using Azure.AI.Projects.Agents;
using Azure.Identity;
using Microsoft.Agents.AI.Foundry;

var projectEndpoint = "";
var agentName = "";
var agentVersion = "";

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

Console.WriteLine();
Console.WriteLine("Request");
Console.WriteLine("-------");
Console.WriteLine(prompt);
Console.WriteLine();

var response = await northstar.RunAsync(
    prompt,
    session);


Console.WriteLine(response);