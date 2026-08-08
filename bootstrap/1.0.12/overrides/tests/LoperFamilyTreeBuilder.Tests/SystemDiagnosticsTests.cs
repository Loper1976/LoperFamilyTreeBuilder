using LoperFamilyTreeBuilder.Core.Models;

namespace LoperFamilyTreeBuilder.Tests;

public sealed class SystemDiagnosticsTests
{
    [Fact]
    public void Required_failures_block_release_readiness()
    {
        var report = new SystemDiagnosticsReport(
            "1.0.12",
            DateTimeOffset.UtcNow,
            1,
            1,
            0,
            0,
            0,
            0,
            0,
            0,
            "migration",
            [
                new SystemDiagnosticCheck("Database", "Connection", true, true, "ok"),
                new SystemDiagnosticCheck("Storage", "Archive", false, true, "missing"),
                new SystemDiagnosticCheck("Recovery", "Backup", false, false, "none")
            ]);

        Assert.False(report.IsReady);
    }

    [Fact]
    public void Notices_do_not_block_release_readiness()
    {
        var report = new SystemDiagnosticsReport(
            "1.0.12",
            DateTimeOffset.UtcNow,
            1,
            1,
            0,
            0,
            0,
            0,
            0,
            0,
            "migration",
            [
                new SystemDiagnosticCheck("Database", "Connection", true, true, "ok"),
                new SystemDiagnosticCheck("Recovery", "Backup", false, false, "none")
            ]);

        Assert.True(report.IsReady);
    }
}
