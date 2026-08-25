using LoperFamilyTreeBuilder.Core.Entities;
using LoperFamilyTreeBuilder.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;

namespace LoperFamilyTreeBuilder.Tests;

public sealed class LoperIdMigrationBackfillTests
{
    [Fact]
    public async Task ExistingPersonReceivesProtectedLoperIdAndAuditEvent()
    {
        var validationRoot = Path.Combine(
            Path.GetTempPath(),
            $"LoperFamilyTreeBuilder-LoperIdMigration-{Guid.NewGuid():N}");
        Environment.SetEnvironmentVariable("LOPER_FAMILY_TREE_ROOT", validationRoot);

        try
        {
            var services = new ServiceCollection().AddFamilyTreeData().BuildServiceProvider();
            await using var scope = services.CreateAsyncScope();
            var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<FamilyTreeDbContext>>();

            await using (var db = await factory.CreateDbContextAsync())
            {
                var migrator = db.Database.GetService<IMigrator>();
                await migrator.MigrateAsync("20260823010000_AddResearchPromotion");

                var existingPerson = new Person("Migration", "Example");
                existingPerson.SetLivingStatus(false);
                db.People.Add(existingPerson);
                await db.SaveChangesAsync();

                await migrator.MigrateAsync();
            }

            await using var verificationDb = await factory.CreateDbContextAsync();
            var person = await verificationDb.People.SingleAsync();
            var identifier = await verificationDb.PersonIdentifiers.SingleAsync(x =>
                x.PersonId == person.Id &&
                x.IdentifierType == PersonIdentifierType.LoperId);

            Assert.Equal("LOPER-000001", identifier.Value);
            Assert.True(identifier.IsProtected);
            Assert.True(await verificationDb.AuditEvents.AnyAsync(x =>
                x.Action == "AssignLoperId" &&
                x.EntityType == "Person" &&
                x.EntityId == person.Id.ToString() &&
                x.Actor == "LOPER ID Migration" &&
                x.NewValueJson == "{\"LoperId\":\"LOPER-000001\"}"));
        }
        finally
        {
            Environment.SetEnvironmentVariable("LOPER_FAMILY_TREE_ROOT", null);
        }
    }
}
