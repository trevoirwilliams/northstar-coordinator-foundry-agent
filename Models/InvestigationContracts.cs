namespace Northstar.Coordinator.Models;

public sealed record InvestigationRequest(string Prompt);

public enum SpecialistKind
{
    Entitlement,
    Diagnostics
}

public sealed record SpecialistResult(
    SpecialistKind Kind,
    string OriginalRequest,
    SpecialistAssessment Assessment);

public sealed class NorthstarInvestigationResult
{
    public required string OriginalRequest { get; init; }

    public required SpecialistAssessment EntitlementAssessment
    {
        get;
        init;
    }

    public required SpecialistAssessment DiagnosticsAssessment
    {
        get;
        init;
    }

    public required string FinalResponse { get; init; }
}