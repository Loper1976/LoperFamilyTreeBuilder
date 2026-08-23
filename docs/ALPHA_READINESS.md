# Alpha Readiness Evidence

Last evaluated: 2026-08-23 on Windows with .NET SDK 10.0.400, SQL Server
LocalDB 17.0.4025.3, Ollama 0.32.15, and `qwen2.5:3b`.

| Gate | State | Evidence |
|---|---|---|
| Host application starts | Pass | Empty isolated data root created a new LocalDB database, applied all migrations, initialized baseline branches, and `/health` returned `ok`. |
| Research Center integrated | Pass | Packaged runtime served `/research-center` with HTTP 200; navigation and person-scoped summary use host stable GUIDs. |
| Research This Person integrated | Pass | Person profile action opens the scoped Research Center; the command produces and persists proposal-first tasks without accepted-tree writes. |
| Local AI configured | Pass on validation machine | Ollama `qwen2.5:3b` returned the deterministic JSON `{"status":"ok"}`. Settings persist only endpoint/model, never credentials. |
| Source intake works | Pass in automated core tests | Immutable hashing, archive metadata, candidate ingestion, and citation-only fallback tests pass. |
| Evidence and citations work | Pass in automated core tests | Evidence scoring, citation proposals, field validation, conflict preservation, and proof-packet tests pass. |
| Adversarial review works | Pass in automated core tests | Researcher/skeptic routing and hallucination-penalty tests pass; AI remains zero-weight analysis. |
| Accepted tree protected | Pass | LocalDB integration test requires persisted matching approval and reviewed supporting evidence; conflicting/unverified claims are rejected; promotion writes an immutable provenance link and audit event. |
| Backup and restore verified | Pass | Integration test creates a SQL `BACKUP DATABASE ... WITH CHECKSUM`, runs `RESTORE VERIFYONLY`, restores a temporary database, queries it, and drops the test copy before promotion completes. |
| Installer builds | Pass | WiX 4 application MSI and Burn bundle build with zero warnings/errors. The bundle SHA-256 was `DDC18B239FA490CD1CB6A97082FC1CBAE9A692DA80CF800AE4DB56EB4A7D9D37`. |
| Tests pass | Pass | 26/26 ResearchAgent tests and 10/10 host tests passed. |
| Clean Windows bundle install | **Not yet proven** | MSI administrative extraction succeeded and its packaged web executable initialized a clean database and served health/research/settings. The repository now includes a disposable Windows-runner check for bundle install, first database creation, health/research/settings, and uninstall. This gate remains open until that workflow passes. |

## Decision

Do not recommend Alpha installation yet. The explicit integration plan requires a
clean-machine bundle installation test. Administrative extraction and packaged
runtime startup materially reduce risk but do not prove prerequisite chaining,
elevation, installed-file ACLs, uninstall, and first launch on a clean OS.
