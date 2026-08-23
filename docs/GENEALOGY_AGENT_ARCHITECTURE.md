# Loper Genealogy Research OS

## Mission
Build a local-first, evidence-first genealogy research system that can discover, analyze, challenge, preserve, and document family-history conclusions without allowing AI opinion to become evidence.

## Non-negotiable rules
1. AI output is analysis, never evidence.
2. Original source files are immutable.
3. Every accepted fact must be traceable to one or more sources.
4. Research hypotheses remain separate from the accepted family tree.
5. No automatic duplicate-person merges.
6. Conflicting evidence is preserved, not overwritten.
7. Private data and API secrets never enter the public code repository.
8. Living-person and sensitive family material defaults to local-only AI processing.
9. Expensive models are escalation tools, not routine processors.
10. Every automated action is auditable.

## Two-layer genealogy model
### Research graph
Contains candidates, hypotheses, conflicting facts, aliases, possible relationships, FAN-network connections, rejected matches, and unresolved questions.

### Accepted tree
Contains only facts and relationships that meet configured evidence thresholds or receive explicit human approval.

## Core engines
- Person and relationship graph
- Evidence and provenance graph
- Source archive
- Document intelligence pipeline
- Identity-resolution engine
- Timeline consistency engine
- Historical-place engine
- FAN research engine
- Research planner and priority queue
- Multi-model AI router
- Adversarial/skeptic review
- Deterministic evidence scorer
- Proof-packet generator
- GEDCOM 7 / GEDZIP interoperability
- Reporting and audit engine

## AI roles
### Extractor
Reads documents and proposes structured fields. Prefer local/free models.

### Researcher
Builds candidate conclusions from supplied evidence.

### Skeptic
Attempts to disprove the researcher's identity or relationship conclusion.

### Source critic
Evaluates informant knowledge, record proximity to event, original/derivative status, direct/indirect evidence, and internal consistency.

### Timeline reviewer
Looks for impossible chronology, overlapping households, age problems, post-death events, and implausible movement.

### Synthesizer
Summarizes competing analyses. It cannot upgrade documentary evidence merely because multiple models agree.

## Model routing
Tier 0: deterministic code.
Tier 1: local models through Ollama and other local runtimes.
Tier 2: approved free cloud providers with failover.
Tier 3: premium OpenAI/Anthropic escalation for hard cases.

Provider availability and free quotas change, so providers are adapters and genealogy logic never depends on one vendor.

## Evidence statuses
- UNVERIFIED
- POSSIBLE
- PROBABLE
- VERIFIED
- CONFLICTING
- REJECTED

Confidence and status are calculated from evidence characteristics, not AI vote counts.

## Proof packet
Every major identity or relationship conclusion can produce a packet containing:
- question
- candidate conclusion
- supporting evidence
- conflicting evidence
- source citations
- identity fingerprint
- timeline
- location history
- household/FAN evidence
- researcher analysis
- skeptic analysis
- source criticism
- deterministic score
- unresolved questions
- recommended next record
- final status and approval history

## Identity fingerprint
Each person accumulates name variants, dates, places, relatives, households, occupations, addresses, associates, source IDs, rejected matches, and negative evidence. New records are compared against the complete fingerprint rather than name alone.

## Source preservation
For every acquired source retain original file, SHA-256, original filename, source URL/repository, retrieval timestamp, citation metadata, extracted text, normalized structured data, and AI analyses. Derived files never replace the original.

## Research loop
Question -> search plan -> source discovery -> preserve original -> extract -> propose claims -> identity matching -> timeline/place checks -> researcher review -> skeptic review -> deterministic evidence scoring -> proof packet -> human/threshold approval -> accepted-tree update -> generate next research question.

## Initial validation case
Argus R. Loper is the first end-to-end validation subject. The system must demonstrate source preservation, immediate-family verification, identity resolution, conflict handling, proof packets, and research-queue generation before autonomous expansion is enabled.
