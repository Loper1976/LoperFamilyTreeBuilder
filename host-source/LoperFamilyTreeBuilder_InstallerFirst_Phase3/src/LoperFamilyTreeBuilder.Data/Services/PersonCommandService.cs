using System.Text.Json;
using LoperFamilyTreeBuilder.Core.Entities;
using LoperFamilyTreeBuilder.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace LoperFamilyTreeBuilder.Data.Services;

public sealed class PersonCommandService(
    IDbContextFactory<FamilyTreeDbContext> contextFactory)
{
    public async Task<Guid> CreateAsync(
        CreatePersonRequest request,
        string actor,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var givenName = request.GivenName.Trim();
        var surname = request.Surname.Trim();

        if (string.IsNullOrWhiteSpace(givenName) &&
            string.IsNullOrWhiteSpace(surname))
        {
            throw new InvalidOperationException(
                "A person must have at least a given name or surname.");
        }

        await using var db =
            await contextFactory.CreateDbContextAsync(cancellationToken);

        var person = new Person(givenName, surname);

        person.UpdateName(
            givenName,
            request.MiddleName.Trim(),
            surname,
            request.Suffix.Trim());

        person.SetBirthDate(request.BirthDate);

        if (request.DeathDate.HasValue)
        {
            person.SetDeathDate(request.DeathDate);
        }
        else
        {
            person.SetLivingStatus(request.IsLiving);
        }

        if (!string.IsNullOrEmpty(request.LegacyNumber))
        {
            var exactLegacy = request.LegacyNumber;

            var duplicateLegacy = await db.PersonIdentifiers
                .AsNoTracking()
                .AnyAsync(identifier =>
                    identifier.IdentifierType ==
                        PersonIdentifierType.LegacyNumber &&
                    identifier.Value == exactLegacy,
                    cancellationToken);

            if (duplicateLegacy)
            {
                throw new InvalidOperationException(
                    "That Robert J. Loper Legacy Number is already assigned to another person. It was not changed or reused.");
            }

            person.AddLegacyNumber(exactLegacy);
        }

        db.People.Add(person);

        if (request.FamilyBranchId.HasValue)
        {
            await EnsureBranchExistsAsync(
                db,
                request.FamilyBranchId.Value,
                cancellationToken);

            db.BranchMemberships.Add(
                new BranchMembership(
                    person.Id,
                    request.FamilyBranchId.Value,
                    isPrimary: true));
        }

        var auditPayload = JsonSerializer.Serialize(new
        {
            person.Id,
            person.GivenName,
            person.MiddleName,
            person.Surname,
            person.Suffix,
            person.BirthDate,
            person.DeathDate,
            person.IsLiving,
            LegacyNumber = request.LegacyNumber,
            request.FamilyBranchId
        });

        db.AuditEvents.Add(
            new AuditEvent(
                action: "Create",
                entityType: nameof(Person),
                entityId: person.Id.ToString(),
                actor: actor,
                summary: $"Created person record: {person.DisplayName}",
                newValueJson: auditPayload));

        await db.SaveChangesAsync(cancellationToken);

        return person.Id;
    }

    public async Task UpdateAsync(
        Guid personId,
        UpdatePersonRequest request,
        string actor,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var givenName = request.GivenName.Trim();
        var surname = request.Surname.Trim();

        if (string.IsNullOrWhiteSpace(givenName) &&
            string.IsNullOrWhiteSpace(surname))
        {
            throw new InvalidOperationException(
                "A person must have at least a given name or surname.");
        }

        await using var db =
            await contextFactory.CreateDbContextAsync(cancellationToken);

        var person = await db.People
            .Include(item => item.Identifiers)
            .SingleOrDefaultAsync(
                item => item.Id == personId,
                cancellationToken);

        if (person is null)
        {
            throw new InvalidOperationException(
                "The person record no longer exists.");
        }

        var legacyNumber = person.Identifiers
            .Where(identifier =>
                identifier.IdentifierType ==
                    PersonIdentifierType.LegacyNumber)
            .Select(identifier => identifier.Value)
            .SingleOrDefault();

        var previousPrimaryBranchId = await db.BranchMemberships
            .AsNoTracking()
            .Where(membership =>
                membership.PersonId == personId &&
                membership.IsPrimary)
            .Select(membership => (Guid?)membership.FamilyBranchId)
            .FirstOrDefaultAsync(cancellationToken);

        var previousJson = JsonSerializer.Serialize(new
        {
            person.Id,
            person.GivenName,
            person.MiddleName,
            person.Surname,
            person.Suffix,
            person.BirthDate,
            person.DeathDate,
            person.IsLiving,
            LegacyNumber = legacyNumber,
            PrimaryFamilyBranchId = previousPrimaryBranchId
        });

        person.UpdateName(
            givenName,
            request.MiddleName.Trim(),
            surname,
            request.Suffix.Trim());

        person.SetBirthDate(request.BirthDate);
        person.SetDeathDate(request.DeathDate);

        if (!request.DeathDate.HasValue)
        {
            person.SetLivingStatus(request.IsLiving);
        }

        await SetPrimaryBranchAsync(
            db,
            personId,
            request.PrimaryFamilyBranchId,
            cancellationToken);

        var newJson = JsonSerializer.Serialize(new
        {
            person.Id,
            person.GivenName,
            person.MiddleName,
            person.Surname,
            person.Suffix,
            person.BirthDate,
            person.DeathDate,
            person.IsLiving,
            LegacyNumber = legacyNumber,
            request.PrimaryFamilyBranchId
        });

        db.AuditEvents.Add(
            new AuditEvent(
                action: "Update",
                entityType: nameof(Person),
                entityId: person.Id.ToString(),
                actor: actor,
                summary: $"Updated person record: {person.DisplayName}. Protected Legacy Number remained unchanged.",
                previousValueJson: previousJson,
                newValueJson: newJson));

        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task SetPrimaryBranchAsync(
        FamilyTreeDbContext db,
        Guid personId,
        Guid? primaryFamilyBranchId,
        CancellationToken cancellationToken)
    {
        if (primaryFamilyBranchId.HasValue)
        {
            await EnsureBranchExistsAsync(
                db,
                primaryFamilyBranchId.Value,
                cancellationToken);
        }

        var memberships = await db.BranchMemberships
            .Where(membership => membership.PersonId == personId)
            .ToListAsync(cancellationToken);

        foreach (var membership in memberships)
        {
            membership.SetPrimary(
                primaryFamilyBranchId.HasValue &&
                membership.FamilyBranchId ==
                    primaryFamilyBranchId.Value);
        }

        if (!primaryFamilyBranchId.HasValue)
        {
            return;
        }

        if (memberships.Any(membership =>
            membership.FamilyBranchId ==
                primaryFamilyBranchId.Value))
        {
            return;
        }

        db.BranchMemberships.Add(
            new BranchMembership(
                personId,
                primaryFamilyBranchId.Value,
                isPrimary: true));
    }

    private static async Task EnsureBranchExistsAsync(
        FamilyTreeDbContext db,
        Guid branchId,
        CancellationToken cancellationToken)
    {
        var branchExists = await db.FamilyBranches
            .AsNoTracking()
            .AnyAsync(
                branch => branch.Id == branchId,
                cancellationToken);

        if (!branchExists)
        {
            throw new InvalidOperationException(
                "The selected family branch no longer exists.");
        }
    }
}
