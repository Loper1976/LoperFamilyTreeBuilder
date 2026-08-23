# Alpha Readiness Evidence

Last evaluated: 2026-08-23 on Windows with .NET SDK 10.0.400, SQL Server
LocalDB 17.0.4025.3, Ollama 0.32.15, and `qwen2.5:3b`.

| Gate | State | Evidence |
|---|---|---|
| Host application starts | Pass | Empty isolated data root created a new LocalDB database, applied all migrations, initialized baseline branches, and `/health` returned `ok`. |
| Research Center integrated | Pass | Packaged runtime served `/research-center` with HTTP 200; navigation and person-scoped summary use host stable GUIDs. Live NARA and Library of Congress searches returned review-only candidates; NYPL, ResearchGuides.net immigration, and AccessGenealogy are clearly labeled guided links. |
| Research This Person integrated | Pass | Person profile action opens the scoped Research Center; the command produces and persists proposal-first tasks without accepted-tree writes. |
| Local AI configured | Pass on validation machine | Ollama `qwen2.5:3b` returned the deterministic JSON `{"status":"ok"}`. Settings persist only endpoint/model, never credentials. |
| Source intake works | Pass in automated core tests | Immutable hashing, archive metadata, candidate ingestion, and citation-only fallback tests pass. |
| Evidence and citations work | Pass in automated core tests | Evidence scoring, citation proposals, field validation, conflict preservation, and proof-packet tests pass. |
| Adversarial review works | Pass in automated core tests | Researcher/skeptic routing and hallucination-penalty tests pass; AI remains zero-weight analysis. |
| Accepted tree protected | Pass | LocalDB integration test requires persisted matching approval and reviewed supporting evidence; conflicting/unverified claims are rejected; promotion writes an immutable provenance link and audit event. |
| Backup and restore verified | Pass | Integration test creates a SQL `BACKUP DATABASE ... WITH CHECKSUM`, runs `RESTORE VERIFYONLY`, restores a temporary database, queries it, and drops the test copy before promotion completes. |
| Installer builds | Pass | WiX 4 application MSI and Burn bundle build with zero warnings/errors. The 1.0.3 bundle SHA-256 is `B8092E6FE3E3B84D61051B57E937169392BDA50C6A24A3DEB7F57C1921E4F5FC`. |
| Tests pass | Pass | 31/31 ResearchAgent tests and 13/13 host tests passed in the clean Windows workflow, including NARA/LOC parsing and privacy, protected LOPER ID behavior, and audited migration backfill. |
| Clean Windows bundle install | Pass | [Disposable Windows 2025 run 32666987441](https://github.com/Loper1976/LoperFamilyTreeBuilder/actions/runs/32666987441) installed the 1.0.3 Burn bundle from commit `678070a`, initialized a new database, returned HTTP 200 for health/research/settings, and uninstalled the application. Evidence artifact digest: `b68e927dd9f4550a1fd8e2ddaffe57df1017a26fa00a5ad9c0dd51aee4b08dcd`. |
| Existing-data upgrade | Pass | A checksum-verified backup was created before upgrading the Windows validation machine from 1.0.2 to 1.0.3. First launch preserved the one existing person, protected `LOPER-000001`, and all seven audit events; it served all main pages and returned live Library of Congress candidates. |

## Decision

Every `AlphaReadinessInput` flag now has demonstrated evidence, including the
explicit clean-machine bundle gate. Alpha is ready for a controlled test
installation. It is not a production release; use a fresh backup and retain the
prior installer before testing with private genealogy data.
