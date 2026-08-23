using LoperFamilyTreeBuilder.Core.Entities;

namespace LoperFamilyTreeBuilder.Tests;

public sealed class AuditEventTests
{
    [Fact]
    public void AuditEvent_RecordsRequiredTraceabilityFields()
    {
        var audit = new AuditEvent(
            action: "Create",
            entityType: "Person",
            entityId: Guid.NewGuid().ToString(),
            actor: "Local Application",
            summary: "Created person.");

        Assert.Equal("Create", audit.Action);
        Assert.Equal("Person", audit.EntityType);
        Assert.Equal("Local Application", audit.Actor);
        Assert.False(string.IsNullOrWhiteSpace(audit.EntityId));
        Assert.True(audit.OccurredUtc <= DateTimeOffset.UtcNow);
    }
}
