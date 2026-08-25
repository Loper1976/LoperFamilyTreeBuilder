# Reconstructed Host Architecture

## Reconstruction

`bootstrap/Reconstruct-Source.ps1` reconstructs the corrected source archive and
applies the repository's 1.0.17 person-profile and first-run overrides. The
corrected archive tail is `part07a`, `part07b`, `part07c`, and `part07d1`; the
older `part07`/`part08` pair differs in the payload and is not used.

The normal working tree is under
`host-source/LoperFamilyTreeBuilder_InstallerFirst_Phase3`.

## Solution structure

- `LoperFamilyTreeBuilder.Core`: entities, models, and Legacy Number policy.
- `LoperFamilyTreeBuilder.Data`: EF Core context, SQL Server mappings,
  migrations, and query/command services.
- `LoperFamilyTreeBuilder.Infrastructure`: application paths, archive settings,
  and storage validation.
- `LoperFamilyTreeBuilder.Web`: Blazor Server host, permanent navigation, and
  dashboard/person pages.
- `LoperFamilyTreeBuilder.Launcher`: Windows Forms launcher and first-run setup.
- `LoperFamilyTreeBuilder.ImportExport` and `.Reporting`: currently assembly
  boundaries with minimal implementation.
- `LoperFamilyTreeBuilder.Tests`: xUnit domain protection tests.
- `installer/LoperFamilyTreeBuilder.Msi`: WiX MSI package.
- `installer/LoperFamilyTreeBuilder.Setup`: WiX Burn bundle that chains SQL
  Server LocalDB and the application MSI.

There is no solution file in the encoded package; projects are built directly.

## Database and migrations

The provider is EF Core 10 for SQL Server LocalDB. The database is attached from
`%LOCALAPPDATA%/Loper Family Tree Builder/Database/LoperFamilyTreeBuilder.mdf`.
Startup calls `Database.MigrateAsync` and then the deterministic baseline-data
initializer. Migrations are timestamped C# migrations in the Data project.

The accepted-tree schema contains people, person identifiers, family branches,
branch memberships, parent-child relationships, accepted facts, research
approvals, and audit events. `AddResearchPromotion` adds immutable research
claim and approval provenance to accepted facts.

## Person identity and Legacy Numbers

`Person.Id` is a generated `Guid` and is the stable internal person ID used by
foreign keys and routes. Robert J. Loper Legacy Numbers are separate
`PersonIdentifier` rows. Their values use exact binary SQL collation, are unique
for their identifier type, and cannot be replaced through normal person editing.
The 1.0.17 update includes regression tests that ensure edit requests do not
contain a Legacy Number field and audit snapshots retain the exact value.

Every person also receives a protected sequential LOPER ID in the format
`LOPER-000001`. A SQL Server sequence prevents reuse; a filtered unique index
enforces uniqueness; the migration deterministically backfills existing people.
LOPER IDs are searchable and displayed separately from historical Legacy
Numbers and internal GUIDs.

## Public archive and library search

`NaraCatalogSearchProvider` connects Research Center searches to the public
read-only Catalog route used by NARA's official web application. It supports
only public-historical queries, returns citation-only candidates, and cannot
write accepted-tree facts. See `docs/NATIONAL_ARCHIVES_SEARCH.md`.

`LibraryOfCongressSearchProvider` follows the same boundary using the official
`loc.gov` JSON API. NYPL, ResearchGuides.net immigration, and AccessGenealogy
are exposed as clearly labeled guided links because they do not offer a current
supported public search API suitable for the host. See
`docs/PUBLIC_RESEARCH_PROVIDERS.md`.

## Startup and navigation

The Windows Forms launcher validates LocalDB and starts the ASP.NET Core host.
The host initializes the database before accepting normal use. The UI is Blazor
Server with `MainLayout.razor` and a permanent left navigation menu in
`NavMenu.razor`. Person routes use the stable `Guid`.

## Audit and backup

Person creation, edits, approvals, and research promotion append `AuditEvent`
rows with actor, summary, and JSON before/after snapshots. The host implements
`IHostBackupGate` with SQL Server `BACKUP DATABASE ... WITH CHECKSUM` followed
by `RESTORE VERIFYONLY`. Its integration test also restores a temporary copy,
queries it, and drops it. `GuardedPromotionCoordinator` refuses accepted-tree
promotion until the matching approval is persisted and the backup gate passes.

## Installer

Fee-free WiX 4 builds an application MSI and a single Burn setup executable. The bundle
chains the SQL Server 2022 LocalDB MSI before the application MSI. A clean-machine
install and uninstall test is still required; source inspection or compilation
alone does not satisfy Alpha readiness.

## Baseline validation findings (2026-08-23)

- .NET SDK 10.0.400 is installed on the Windows development environment.
- Deep workspace paths exceed a legacy MSBuild copy path; using a short
  `--artifacts-path` allows Core and Infrastructure to compile.
- The reconstructed 1.0.17 test project omitted `global using Xunit;`, so the
  untouched baseline tests failed to compile. `GlobalUsings.cs` repairs that
  source-package defect in the working tree.
- The independently added ResearchAgent test project had the same missing xUnit
  global import; its working tree now contains the equivalent repair.
- Empty-root host startup and packaged-runtime startup both applied migrations
  and returned `ok` from `/health`.
- ResearchAgent tests pass 31/31 and host tests pass 13/13. The host suite includes an
  old-schema migration test proving that existing people receive both a protected
  LOPER ID and a matching immutable audit event.
- The installer builds cleanly and its MSI administrative image runs, but Alpha
  remains gated on the disposable clean-Windows bundle workflow passing.
