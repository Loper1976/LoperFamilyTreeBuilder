# Autonomous Development Instructions

## Objective
Develop Loper Family Tree Builder into the Loper Genealogy Research OS described in `docs/GENEALOGY_AGENT_ARCHITECTURE.md` while preserving existing application behavior and data compatibility.

## Working rules
- Read README.md and architecture/backlog docs before changing code.
- Work from P0 to P2 unless a dependency requires otherwise.
- Prefer small testable changes.
- Run existing tests after each meaningful change.
- Add tests for every new evidence, identity, ingestion, timeline, or GEDCOM behavior.
- Do not weaken or remove existing backup, audit, stable-ID, Legacy Number, duplicate-merge, or AI proposal-first protections.
- Do not commit secrets, tokens, credentials, private genealogy data, living-person data, or production database contents.
- Use fictional/sample genealogy fixtures in this public repository.
- Treat AI responses as untrusted proposed analysis.
- Validate structured model output before persistence.
- Deterministic code owns hashes, calculations, evidence thresholds, migrations, file paths, IDs, audit records and accepted-tree safeguards.
- Network AI/provider failures must degrade gracefully.
- Local/free processing is preferred for routine tasks. Premium providers are optional escalation adapters.
- Never silently merge people or silently promote a research hypothesis into the accepted tree.

## Cost hierarchy
1. No model when deterministic code can do the job.
2. Local/free model for extraction/classification/routine analysis.
3. Free cloud provider when permitted by privacy classification.
4. Low-cost paid model for harder analysis.
5. Strong model only for genuinely ambiguous cross-source reasoning or final adversarial review.

## Security boundary
`LoperFamilyTreeBuilder` is public code. Real family data belongs outside this repository, currently in the private data repository or configured local data location. `.env`, keys and secrets must be ignored.

## Completion behavior
When a task is implemented:
1. run relevant tests;
2. document migrations/config changes;
3. update the backlog checkbox only when the feature is actually functional;
4. report remaining failures plainly;
5. do not mark placeholders or stubs complete.

## First validation target
Use fictional test fixtures in code, but design the end-to-end workflow so Argus R. Loper can be the first real validation subject when the application is pointed at the private data repository.
