# Genealogy Agent Build Backlog

## P0 - Foundation
- [ ] Define evidence/provenance schema migrations without breaking existing stable IDs or Legacy Numbers.
- [ ] Add research-graph entities separate from accepted-tree entities.
- [ ] Add sources, citations, claims, evidence links, conflicts, rejected matches, research tasks, AI reviews, and approval history.
- [ ] Add immutable-source SHA-256 hashing and archive metadata.
- [ ] Add privacy classification: public historical, private family, living-person sensitive.
- [ ] Add secrets/configuration layer; never commit keys.
- [ ] Add automated database backup before high-risk migrations.

## P0 - Source ingestion
- [ ] Ingest PDF, JPEG, PNG, TIFF, TXT, CSV, JSON, GEDCOM and GEDZIP metadata.
- [ ] Preserve originals unchanged.
- [ ] Extract embedded text before OCR.
- [ ] OCR only when needed.
- [ ] Create normalized source metadata and citation records.
- [ ] Generate proposed claims; never silently write accepted facts.
- [ ] Maintain field-level provenance.

## P0 - Identity resolution
- [ ] Name normalization and alias handling.
- [ ] Date-range/tolerance comparison.
- [ ] Birthplace/residence comparison.
- [ ] Household-member comparison.
- [ ] Relative/spouse/parent/child comparison.
- [ ] Occupation/address/associate comparison.
- [ ] Negative-evidence model.
- [ ] Candidate match score with explainable components.
- [ ] Never auto-merge people.

## P0 - Evidence engine
- [ ] Claim-specific source weighting.
- [ ] Original vs derivative classification.
- [ ] Primary/secondary informant assessment.
- [ ] Direct/indirect/negative evidence.
- [ ] Independent corroboration detection.
- [ ] Conflict preservation.
- [ ] Deterministic status thresholds.
- [ ] Proof-packet generation.

## P1 - Multi-model router
- [ ] Provider interface.
- [ ] Ollama/local provider.
- [ ] OpenAI-compatible provider adapter.
- [ ] Free-provider health checks and fallback.
- [ ] Rate-limit handling.
- [ ] Privacy-aware routing.
- [ ] Task/model capability benchmark scores.
- [ ] Premium escalation policy.
- [ ] Researcher/skeptic/source-critic/timeline-reviewer roles.

## P1 - Timeline and geography
- [ ] Life-event timeline.
- [ ] Chronological contradiction checks.
- [ ] Age-at-event calculations.
- [ ] Simultaneous-residence warnings.
- [ ] Parent/child biological chronology checks.
- [ ] Original and normalized place names.
- [ ] Historical jurisdiction support.
- [ ] Migration path representation.

## P1 - Research planner
- [ ] Research questions linked to people/claims.
- [ ] Priority scoring by evidentiary value, availability, cost and expected information gain.
- [ ] Generate next-best-source recommendations.
- [ ] Generate new tasks from discoveries.
- [ ] Stop conditions to prevent uncontrolled recursive research.
- [ ] Human approval gates for autonomous expansion.

## P1 - FAN network
- [ ] Neighbors.
- [ ] Witnesses.
- [ ] Land transaction parties.
- [ ] Probate participants.
- [ ] Employers/business associates.
- [ ] Church/cemetery associations.
- [ ] Recurring surname/place detection.

## P1 - GEDCOM
- [ ] Preserve current import behavior.
- [ ] GEDCOM 7 import/export.
- [ ] GEDZIP media support.
- [ ] Stable internal IDs remain independent of external IDs.
- [ ] Round-trip tests.

## P2 - User interface
- [ ] Research workbench.
- [ ] Evidence viewer.
- [ ] Side-by-side source and extracted claims.
- [ ] Identity comparison screen.
- [ ] Conflict center.
- [ ] Proof-packet viewer.
- [ ] Research queue.
- [ ] AI provider status/cost/privacy panel.
- [ ] Family graph and timeline.

## P2 - Quality system
- [ ] Gold-standard genealogy benchmark cases.
- [ ] Extraction accuracy tests.
- [ ] Duplicate-person discrimination tests.
- [ ] Hallucinated-parent tests.
- [ ] Timeline-conflict tests.
- [ ] Evidence-status regression tests.
- [ ] GEDCOM round-trip tests.
- [ ] Provider benchmark harness.

## Definition of done for autonomous research v1
The system can take one selected deceased historical person, preserve supplied/discovered public sources, extract proposed claims, distinguish research graph from accepted tree, compare candidate identities, identify conflicts, generate a proof packet, recommend the next source, and maintain a complete audit trail without automatically accepting unsupported relationships.
