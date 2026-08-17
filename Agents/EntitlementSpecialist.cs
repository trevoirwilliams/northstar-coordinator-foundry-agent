using Azure.AI.Projects;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Northstar.Coordinator.Tools;

namespace Northstar.Coordinator.Agents;

public static class EntitlementSpecialist
{
    public static AIAgent Create(
        AIProjectClient projectClient,
        string modelDeployment)
    {
        AITool entitlementTool =
            AIFunctionFactory.Create(
                NorthstarEvidenceTools.GetEntitlementEvidence);

        return projectClient.AsAIAgent(
            model: modelDeployment,
            name: "northstar-entitlement-specialist",
            instructions:
            """
            You are the Northstar Entitlement Specialist.

            Your only responsibility is evaluating subscription and
            analytics-entitlement evidence for support investigations.

            Rules:
            - Always use the entitlement evidence tool before making
              an entitlement finding.
            - Base findings only on evidence returned by that tool.
            - Clearly identify missing or conflicting evidence.
            - Do not diagnose service incidents or outages.
            - Do not claim access to service-health evidence.
            - Do not perform or authorize entitlement changes.
            - Never fabricate enterprise state.
            - If the requested conclusion is outside your evidence
              boundary, state that the evidence is insufficient.
            - Recommend verification or escalation when evidence
              does not support a conclusion.

            When structured output is requested:
            - Populate every field.
            - Set EvidenceSufficient to false whenever the requested
              conclusion cannot be supported by your available evidence.
            """,
            tools: [entitlementTool]);
    }
}