namespace ResearchAgent.Core.UI;

public sealed record AlphaReadinessInput(
    bool HostApplicationStarts,
    bool ResearchCenterIntegrated,
    bool ResearchThisPersonIntegrated,
    bool LocalAiConfigured,
    bool SourceIntakeWorks,
    bool EvidenceCitationsWork,
    bool AdversarialReviewWorks,
    bool AcceptedTreeProtected,
    bool BackupRestoreVerified,
    bool InstallerBuilds,
    bool TestsPass);

public sealed record AlphaReadinessResult(bool Ready, IReadOnlyList<string> Missing);

public static class AlphaReadiness
{
    public static AlphaReadinessResult Evaluate(AlphaReadinessInput x)
    {
        var missing = new List<string>();
        if (!x.HostApplicationStarts) missing.Add("Host application startup validation");
        if (!x.ResearchCenterIntegrated) missing.Add("Research Center UI integration");
        if (!x.ResearchThisPersonIntegrated) missing.Add("Research This Person UI integration");
        if (!x.LocalAiConfigured) missing.Add("Local AI configuration validation");
        if (!x.SourceIntakeWorks) missing.Add("Source intake end-to-end validation");
        if (!x.EvidenceCitationsWork) missing.Add("Evidence/citation validation");
        if (!x.AdversarialReviewWorks) missing.Add("Researcher/skeptic validation");
        if (!x.AcceptedTreeProtected) missing.Add("Accepted-tree protection validation");
        if (!x.BackupRestoreVerified) missing.Add("Backup and restore verification");
        if (!x.InstallerBuilds) missing.Add("Installer build validation");
        if (!x.TestsPass) missing.Add("Passing automated test suite");
        return new(missing.Count == 0, missing);
    }
}
