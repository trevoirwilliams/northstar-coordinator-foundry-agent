using System.Text.Json;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Northstar.Coordinator.Models;

namespace Northstar.Coordinator.Workflows;

public static class CaseInvestigationWorkflow
{
    public static Workflow Create(
        AIAgent entitlementSpecialist,
        AIAgent diagnosticsSpecialist,
        AIAgent northstarCoordinator,
        System.Diagnostics.ActivitySource activitySource)
    {
        // ---------------------------------------------------------
        // Workflow entry point
        // ---------------------------------------------------------

        ExecutorBinding requestExecutor =
            ((Func<InvestigationRequest, InvestigationRequest>)(
                request => request))
            .BindAsExecutor("case-request");


        // ---------------------------------------------------------
        // Entitlement branch
        // ---------------------------------------------------------

        async ValueTask<SpecialistResult>
            RunEntitlementAsync(InvestigationRequest request)
        {
            Console.WriteLine();
            Console.WriteLine(
                "[WORKFLOW] Starting Entitlement Specialist");

            AgentSession session =
                await entitlementSpecialist.CreateSessionAsync();

            AgentResponse<SpecialistAssessment> response =
                await entitlementSpecialist
                    .RunAsync<SpecialistAssessment>(
                        request.Prompt,
                        session);

            Console.WriteLine(
                "[WORKFLOW] Entitlement Specialist completed");

            return new SpecialistResult(
                SpecialistKind.Entitlement,
                request.Prompt,
                response.Result);
        }

        ExecutorBinding entitlementExecutor =
            ((Func<
                InvestigationRequest,
                ValueTask<SpecialistResult>>)
                RunEntitlementAsync)
            .BindAsExecutor(
                "entitlement-specialist");


        // ---------------------------------------------------------
        // Diagnostics branch
        // ---------------------------------------------------------

        async ValueTask<SpecialistResult>
            RunDiagnosticsAsync(InvestigationRequest request)
        {
            Console.WriteLine();
            Console.WriteLine(
                "[WORKFLOW] Starting Diagnostics Specialist");

            AgentSession session =
                await diagnosticsSpecialist.CreateSessionAsync();

            AgentResponse<SpecialistAssessment> response =
                await diagnosticsSpecialist
                    .RunAsync<SpecialistAssessment>(
                        request.Prompt,
                        session);

            Console.WriteLine(
                "[WORKFLOW] Diagnostics Specialist completed");

            return new SpecialistResult(
                SpecialistKind.Diagnostics,
                request.Prompt,
                response.Result);
        }

        ExecutorBinding diagnosticsExecutor =
            ((Func<
                InvestigationRequest,
                ValueTask<SpecialistResult>>)
                RunDiagnosticsAsync)
            .BindAsExecutor(
                "diagnostics-specialist");


        // ---------------------------------------------------------
        // Fan-in synthesis
        // ---------------------------------------------------------

        var synthesisExecutor =
            new NorthstarSynthesisExecutor(
                northstarCoordinator);


        // ---------------------------------------------------------
        // Workflow graph
        // ---------------------------------------------------------

        return new WorkflowBuilder(requestExecutor)
            
            .AddFanOutEdge(
                requestExecutor,
                [
                    entitlementExecutor,
                    diagnosticsExecutor
                ],
                "independent-evidence-analysis")

            .AddFanInBarrierEdge(
                [
                    entitlementExecutor,
                    diagnosticsExecutor
                ],
                synthesisExecutor,
                "wait-for-both-specialists")

            .WithOutputFrom(
                synthesisExecutor)

            .WithName(
                "northstar-case-investigation")

            .WithDescription(
                """
                Runs entitlement and diagnostics assessments
                concurrently, then returns both assessments to
                the existing Northstar Coordinator for synthesis.
                """)
                
            .WithOpenTelemetry(
                configure: cfg => cfg.EnableSensitiveData = true,
                activitySource: activitySource)
            .Build();
    }
}


// =============================================================
// NORTHSTAR COORDINATOR SYNTHESIS EXECUTOR
// =============================================================

[YieldsOutput(typeof(NorthstarInvestigationResult))]
internal sealed partial class NorthstarSynthesisExecutor(
    AIAgent northstarCoordinator)
    : Executor<SpecialistResult>(
        "northstar-coordinator-synthesis")
{
    private readonly AIAgent _northstarCoordinator =
        northstarCoordinator;

    private readonly List<SpecialistResult> _results = [];

    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web)
        {
            WriteIndented = true
        };


    public override ValueTask HandleAsync(
        SpecialistResult message,
        IWorkflowContext context,
        CancellationToken cancellationToken = default)
    {
        _results.Add(message);

        return ValueTask.CompletedTask;
    }


    protected override async ValueTask OnMessageDeliveryFinishedAsync(
            IWorkflowContext context,
            CancellationToken cancellationToken = default)
    {
        if (_results.Count == 0)
        {
            return;
        }

        if (_results.Count != 2)
        {
            throw new InvalidOperationException(
                $"Expected 2 specialist results but received " +
                $"{_results.Count}.");
        }


        SpecialistResult entitlementResult =
            _results.Single(
                result =>
                    result.Kind ==
                    SpecialistKind.Entitlement);


        SpecialistResult diagnosticsResult =
            _results.Single(
                result =>
                    result.Kind ==
                    SpecialistKind.Diagnostics);


        if (!string.Equals(
                entitlementResult.OriginalRequest,
                diagnosticsResult.OriginalRequest,
                StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "Specialist results belong to different requests.");
        }


        Console.WriteLine();
        Console.WriteLine(
            "[WORKFLOW] Both specialist assessments received.");

        Console.WriteLine(
            "[WORKFLOW] Returning evidence to Northstar Coordinator.");


        string synthesisPrompt =
            BuildSynthesisPrompt(
                entitlementResult.OriginalRequest,
                entitlementResult.Assessment,
                diagnosticsResult.Assessment);


        AgentSession coordinatorSession =
            await _northstarCoordinator
                .CreateSessionAsync();


        AgentResponse coordinatorResponse =
            await _northstarCoordinator.RunAsync(
                synthesisPrompt,
                coordinatorSession);


        var investigationResult =
            new NorthstarInvestigationResult
            {
                OriginalRequest =
                    entitlementResult.OriginalRequest,

                EntitlementAssessment =
                    entitlementResult.Assessment,

                DiagnosticsAssessment =
                    diagnosticsResult.Assessment,

                FinalResponse =
                    coordinatorResponse.Text
            };


        _results.Clear();


        await context.YieldOutputAsync(
            investigationResult,
            cancellationToken);
    }


    private static string BuildSynthesisPrompt(
        string originalRequest,
        SpecialistAssessment entitlementAssessment,
        SpecialistAssessment diagnosticsAssessment)
    {
        string entitlementJson =
            JsonSerializer.Serialize(
                entitlementAssessment,
                JsonOptions);

        string diagnosticsJson =
            JsonSerializer.Serialize(
                diagnosticsAssessment,
                JsonOptions);

        return
        $"""
        You are the Northstar Coordinator.

        This is the synthesis and review stage of a delegated
        support investigation.

        ORIGINAL SUPPORT REQUEST
        ------------------------
        {originalRequest}

        ENTITLEMENT SPECIALIST ASSESSMENT
        ---------------------------------
        {entitlementJson}

        SERVICE DIAGNOSTICS SPECIALIST ASSESSMENT
        -----------------------------------------
        {diagnosticsJson}

        First, correlate the supplied specialist assessments and
        form a proposed support recommendation.

        Then you MUST invoke the configured Northstar Compliance
        Reviewer through the A2A capability.

        Send the Compliance Reviewer:
        - the original support request;
        - the entitlement specialist assessment;
        - the diagnostics specialist assessment;
        - your proposed recommendation.

        Do not return the final support response until the
        Compliance Reviewer has responded.

        FINAL RESPONSE REQUIREMENTS

        Evidence:
        - State the relevant observed evidence.
        - Do not invent or retrieve additional entitlement or
          service-health evidence.

        Assessment:
        - Identify the best-supported explanation.
        - Clearly distinguish evidence from inference.
        - State remaining uncertainty.

        Compliance Review:
        - State the reviewer's decision.
        - State whether the reviewer found the recommendation
          aligned with the supplied evidence.
        - State any concerns raised by the reviewer.

        Recommended Action:
        - Recommend the next support action.
        - Do not claim that the action was executed.

        Approval:
        - Clearly state whether human approval is required.

        If the Compliance Reviewer cannot be reached:
        - state that compliance review is unavailable;
        - preserve the investigation evidence;
        - do not describe the recommendation as approved;
        - do not authorize customer access changes.

        Respond to the support employee in clear, concise language.
        """;
    }
}