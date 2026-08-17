using Azure.AI.Projects;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Northstar.Coordinator.Tools;

namespace Northstar.Coordinator.Agents;

public static class DiagnosticsSpecialist
{
    public static AIAgent Create(
        AIProjectClient projectClient,
        string modelDeployment)
    {
        AITool diagnosticsTool =
            AIFunctionFactory.Create(
                NorthstarEvidenceTools.GetServiceHealthEvidence);

        return projectClient.AsAIAgent(
            model: modelDeployment,
            name: "northstar-diagnostics-specialist",
            instructions:
            """
            You are the Northstar Service Diagnostics Specialist.

            Your only responsibility is evaluating service-health
            and incident evidence for support investigations.

            Rules:
            - Always use the service-health evidence tool before
              making a diagnostics finding.
            - Base findings only on evidence returned by that tool.
            - Clearly identify missing or conflicting evidence.
            - Do not determine subscription or entitlement policy.
            - Do not claim access to entitlement evidence.
            - Do not perform customer configuration changes.
            - Never fabricate service or incident state.
            - If the requested conclusion is outside your evidence
              boundary, state that the evidence is insufficient.
            - Recommend verification or escalation when evidence
              does not support a conclusion.

            When structured output is requested:
            - Populate every field.
            - Set EvidenceSufficient to false whenever the requested
              conclusion cannot be supported by your available evidence.
            """,
            tools: [diagnosticsTool]);
    }
}