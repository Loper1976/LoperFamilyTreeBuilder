# Loper Family Tree Builder - Installer-First Phase 3

## End-user model

The finished user receives one file:

LoperFamilyTreeBuilderSetup.exe

The end user does not need Visual Studio and does not manually use
PowerShell, Command Prompt, dotnet commands, SQL setup tools, or a web
server.

## Phase 3 additions

### Automatic database prerequisite

The Windows cloud build now downloads Microsoft SQL Server 2022 Express
installation media and extracts SqlLocalDB.msi. The Burn bundle embeds
SqlLocalDB.msi before the application MSI so the required LocalDB
component is installed as part of the single setup executable.

The launcher also checks for SqlLocalDB.exe before starting the
application and directs the user to Repair if the required component is
missing.

### Audit foundation

A new AuditEvent entity records:

- UTC occurrence time
- action
- entity type
- entity ID
- actor
- summary
- previous JSON value when applicable
- new JSON value when applicable

Person creation and baseline branch initialization now create audit
events.

### High-volume People work center

The People page now uses server-side database queries with:

- search text
- surname prefix
- Legacy Number prefix
- family branch
- living/deceased status
- sorting
- pagination
- maximum page size of 100 records

The page never loads all detailed person records at once.

### Add Person

The Add Person workflow now includes:

- names
- birth/death dates
- living status
- family branch
- exact Robert J. Loper Legacy Number text
- duplicate-candidate warning
- explicit Create Anyway action
- duplicate Legacy Number rejection
- audit event creation

No duplicate is merged automatically.

### Legacy Number protection

Legacy Numbers remain text values with binary SQL collation for exact
character preservation. The create path does not trim or normalize the
Legacy Number field. Once stored as protected historical data, normal
domain editing cannot change it.

### Family branches

Baseline Loper and Beadle branches are created only as branch names.
No modern branch-code format is assigned because that format remains an
open project-owner decision.

## Current build limitation

This package is authored for Windows, .NET 10, EF Core 10, SQL Server
LocalDB, and WiX. The current artifact environment is Linux and cannot
compile or execute the Windows installer. The included Windows cloud
build workflow is the intended compiler path.

## Next implementation section

- full Person Profile
- edit workflow with audit history
- protected Legacy Number display
- family relationships
- branch membership management
- recent people
- favorites groundwork
- stronger duplicate review
- migration preparation for the existing archive
