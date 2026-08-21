using System.ComponentModel;
using System.Text.Json;

namespace Northstar.Coordinator.Tools;

public static class NorthstarEvidenceTools
{
    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);

    [Description(
        "Gets current subscription and analytics entitlement evidence " +
        "for a Northstar support case.")]
    public static string GetEntitlementEvidence(
        [Description("The Northstar support case identifier.")]
        string caseId)
    {
        Console.WriteLine(
            $"[TOOL][ENTITLEMENT] Retrieving evidence for {caseId}");

        object result = caseId.Equals(
            "C-1048",
            StringComparison.OrdinalIgnoreCase)
            ? new
            {
                found = true,
                caseId = "C-1048",
                subscriptionPlan = "Enterprise",
                subscriptionStatus = "Active",
                analyticsPolicy =
                    "Enterprise subscriptions include analytics access.",
                analyticsEntitlementState = "NotProvisioned",
                source =
                    "Northstar synthetic entitlement service"
            }
            : new
            {
                found = false,
                caseId,
                source =
                    "Northstar synthetic entitlement service"
            };

        return JsonSerializer.Serialize(result, JsonOptions);
    }

    [Description(
        "Gets current analytics service-health and incident evidence " +
        "for a Northstar support case.")]
    public static string GetServiceHealthEvidence(
        [Description("The Northstar support case identifier.")]
        string caseId)
    {
        Console.WriteLine(
            $"[TOOL][DIAGNOSTICS] Retrieving evidence for {caseId}");

        object result = caseId.Equals(
            "C-1048",
            StringComparison.OrdinalIgnoreCase)
            ? new
            {
                found = true,
                caseId = "C-1048",
                service = "Analytics",
                serviceState = "Operational",
                activeMatchingIncident = false,
                recentMatchingIncident = false,
                source =
                    "Northstar synthetic service-health service"
            }
            : new
            {
                found = false,
                caseId,
                source =
                    "Northstar synthetic service-health service"
            };

        return JsonSerializer.Serialize(result, JsonOptions);
    }
}