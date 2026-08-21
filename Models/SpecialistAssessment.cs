namespace Northstar.Coordinator.Models;

public sealed class SpecialistAssessment
{
    public string Specialist { get; set; } = string.Empty;

    public string CaseId { get; set; } = string.Empty;

    public string Finding { get; set; } = string.Empty;

    public bool EvidenceSufficient { get; set; }

    public List<string> Evidence { get; set; } = [];

    public List<string> Unknowns { get; set; } = [];

    public string RecommendedNextStep { get; set; } = string.Empty;
}