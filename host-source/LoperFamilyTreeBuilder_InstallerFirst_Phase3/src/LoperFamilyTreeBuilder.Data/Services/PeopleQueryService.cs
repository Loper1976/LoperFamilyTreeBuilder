using LoperFamilyTreeBuilder.Core.Entities;
using LoperFamilyTreeBuilder.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace LoperFamilyTreeBuilder.Data.Services;

public sealed class PeopleQueryService(
    IDbContextFactory<FamilyTreeDbContext> contextFactory)
{
    public async Task<PagedResult<PersonListItem>> SearchAsync(
        PeopleSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 10, 100);

        await using var db =
            await contextFactory.CreateDbContextAsync(cancellationToken);

        IQueryable<Person> query = db.People.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.SearchText))
        {
            var search = request.SearchText.Trim();

            query = query.Where(person =>
                person.GivenName.Contains(search) ||
                person.MiddleName.Contains(search) ||
                person.Surname.Contains(search) ||
                person.Suffix.Contains(search) ||
                person.Identifiers.Any(identifier =>
                    identifier.Value.Contains(search)));
        }

        if (!string.IsNullOrWhiteSpace(request.Surname))
        {
            var surname = request.Surname.Trim();

            query = query.Where(person =>
                person.Surname.StartsWith(surname));
        }

        if (!string.IsNullOrWhiteSpace(request.LegacyNumberPrefix))
        {
            var prefix = request.LegacyNumberPrefix;

            query = query.Where(person =>
                person.Identifiers.Any(identifier =>
                    identifier.IdentifierType ==
                        PersonIdentifierType.LegacyNumber &&
                    identifier.Value.StartsWith(prefix)));
        }

        if (request.FamilyBranchId.HasValue)
        {
            var branchId = request.FamilyBranchId.Value;

            query = query.Where(person =>
                person.BranchMemberships.Any(membership =>
                    membership.FamilyBranchId == branchId));
        }

        query = request.LivingStatus switch
        {
            LivingStatusFilter.Living =>
                query.Where(person => person.IsLiving),

            LivingStatusFilter.Deceased =>
                query.Where(person => !person.IsLiving),

            _ => query
        };

        query = ApplySort(query, request.SortBy, request.Descending);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(person => new PersonListItem(
                person.Id,
                person.GivenName,
                person.MiddleName,
                person.Surname,
                person.Suffix,
                person.BirthDate,
                person.DeathDate,
                person.IsLiving,
                person.Identifiers
                    .Where(identifier =>
                        identifier.IdentifierType == PersonIdentifierType.LoperId)
                    .Select(identifier => identifier.Value)
                    .FirstOrDefault(),
                person.Identifiers
                    .Where(identifier =>
                        identifier.IdentifierType ==
                            PersonIdentifierType.LegacyNumber)
                    .Select(identifier => identifier.Value)
                    .FirstOrDefault(),
                person.BranchMemberships
                    .OrderByDescending(membership => membership.IsPrimary)
                    .Select(membership => membership.FamilyBranch.Name)
                    .FirstOrDefault()))
            .ToListAsync(cancellationToken);

        return new PagedResult<PersonListItem>(
            items,
            page,
            pageSize,
            totalCount);
    }

    public async Task<IReadOnlyList<PotentialDuplicatePerson>>
        FindPotentialDuplicatesAsync(
            CreatePersonRequest request,
            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var given = request.GivenName.Trim();
        var surname = request.Surname.Trim();

        if (string.IsNullOrWhiteSpace(given) &&
            string.IsNullOrWhiteSpace(surname))
        {
            return [];
        }

        await using var db =
            await contextFactory.CreateDbContextAsync(cancellationToken);

        var candidates = await db.People
            .AsNoTracking()
            .Where(person =>
                person.Surname == surname &&
                person.GivenName == given)
            .OrderBy(person => person.BirthDate)
            .Take(20)
            .Select(person => new
            {
                person.Id,
                person.GivenName,
                person.MiddleName,
                person.Surname,
                person.Suffix,
                person.BirthDate,
                person.DeathDate,
                LegacyNumber = person.Identifiers
                    .Where(identifier =>
                        identifier.IdentifierType ==
                            PersonIdentifierType.LegacyNumber)
                    .Select(identifier => identifier.Value)
                    .FirstOrDefault()
            })
            .ToListAsync(cancellationToken);

        return candidates
            .Select(candidate =>
            {
                var displayName = string.Join(
                    " ",
                    new[]
                    {
                        candidate.GivenName,
                        candidate.MiddleName,
                        candidate.Surname,
                        candidate.Suffix
                    }
                    .Where(value => !string.IsNullOrWhiteSpace(value)));

                var reason =
                    request.BirthDate.HasValue &&
                    candidate.BirthDate == request.BirthDate
                        ? "Same given name, surname, and birth date."
                        : "Same given name and surname.";

                return new PotentialDuplicatePerson(
                    candidate.Id,
                    displayName,
                    candidate.BirthDate,
                    candidate.DeathDate,
                    candidate.LegacyNumber,
                    reason);
            })
            .ToList();
    }

    private static IQueryable<Person> ApplySort(
        IQueryable<Person> query,
        string sortBy,
        bool descending)
    {
        var normalized = (sortBy ?? string.Empty).Trim().ToLowerInvariant();

        return normalized switch
        {
            "given" when descending =>
                query.OrderByDescending(person => person.GivenName)
                    .ThenByDescending(person => person.Surname),

            "given" =>
                query.OrderBy(person => person.GivenName)
                    .ThenBy(person => person.Surname),

            "birth" when descending =>
                query.OrderByDescending(person => person.BirthDate)
                    .ThenBy(person => person.Surname),

            "birth" =>
                query.OrderBy(person => person.BirthDate)
                    .ThenBy(person => person.Surname),

            _ when descending =>
                query.OrderByDescending(person => person.Surname)
                    .ThenByDescending(person => person.GivenName),

            _ =>
                query.OrderBy(person => person.Surname)
                    .ThenBy(person => person.GivenName)
        };
    }
}
