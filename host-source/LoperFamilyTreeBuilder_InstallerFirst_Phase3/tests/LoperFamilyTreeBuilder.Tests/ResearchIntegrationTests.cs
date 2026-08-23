using LoperFamilyTreeBuilder.Core.Models;
using LoperFamilyTreeBuilder.Data;
using LoperFamilyTreeBuilder.Data.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ResearchAgent.Core.Domain;
using ResearchAgent.Core.Integration;
using ResearchAgent.Core.Persistence;
using ResearchAgent.Core.Review;

namespace LoperFamilyTreeBuilder.Tests;

public sealed class ResearchIntegrationTests
{
    [Fact]
    public async Task ReviewedClaimRequiresVerifiedBackupBeforeAuditedPromotion()
    {
        var validationRoot = Path.Combine(Path.GetTempPath(), "LoperFamilyTreeBuilder-AlphaIntegration");
        Environment.SetEnvironmentVariable("LOPER_FAMILY_TREE_ROOT", validationRoot);
        try
        {
            var services = new ServiceCollection().AddFamilyTreeData().BuildServiceProvider();
            await services.InitializeFamilyTreeDatabaseAsync();
            await using var scope = services.CreateAsyncScope();
            var personId = await scope.ServiceProvider.GetRequiredService<PersonCommandService>()
                .CreateAsync(new CreatePersonRequest { GivenName = "Ada", Surname = "Example", IsLiving = false }, "Alpha Integration Test");
            var hostPerson = await scope.ServiceProvider.GetRequiredService<IHostPersonReader>().GetPersonAsync(personId);
            Assert.NotNull(hostPerson);
            Assert.Equal(personId, hostPerson.StablePersonId);

            var store = scope.ServiceProvider.GetRequiredService<IResearchStore>();
            var now = DateTimeOffset.UtcNow;
            var claim = new ResearchClaim(Guid.NewGuid(), personId, "birth-place", "Exampleville",
                "exampleville", ClaimStatus.Probable, 80, now, now);
            await store.SaveClaimAsync(claim);
            await store.SaveEvidenceAsync(new EvidenceLink(Guid.NewGuid(), claim.Id, Guid.NewGuid(),
                EvidenceDirection.Supports, EvidenceClass.Direct, SourceOriginality.Original,
                InformantKnowledge.Primary, 100, "alpha-fixture", 80));

            var decisionService = new ResearchDecisionService(store,
                scope.ServiceProvider.GetRequiredService<IApprovalStore>());
            var approval = await decisionService.DecideClaimAsync(claim,
                ResearchDecision.AcceptForPromotionReview, "Alpha Integration Test", "Fictional reviewed evidence supports the claim.");

            var coordinator = new GuardedPromotionCoordinator(
                scope.ServiceProvider.GetRequiredService<IHostBackupGate>(),
                scope.ServiceProvider.GetRequiredService<IAcceptedTreePromotionService>());
            var factId = await coordinator.PromoteAsync(claim.Id, approval);

            var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<FamilyTreeDbContext>>();
            await using var db = await factory.CreateDbContextAsync();
            Assert.True(await db.AcceptedFacts.AnyAsync(x => x.Id == factId && x.ResearchClaimId == claim.Id));
            Assert.True(await db.AuditEvents.AnyAsync(x => x.Action == "PromoteResearchClaim" && x.EntityId == factId.ToString()));
            var backup = Directory.GetFiles(Path.Combine(validationRoot, "Backups"), "*.bak")
                .OrderByDescending(File.GetLastWriteTimeUtc).First();
            await scope.ServiceProvider.GetRequiredService<HostDatabaseBackupService>()
                .RestoreAndQueryTestCopyAsync(backup);
        }
        finally
        {
            Environment.SetEnvironmentVariable("LOPER_FAMILY_TREE_ROOT", null);
        }
    }
}
