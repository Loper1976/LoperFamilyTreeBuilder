# P0 Evidence and Provenance Model

This document is the implementation contract for the first Genealogy Research OS data layer. It is intentionally storage-engine-neutral until the existing packaged application schema is reconstructed and migration compatibility is verified.

## Required entities

### ResearchSource
Represents one preserved source item.
- Id: stable GUID/internal identifier
- Title
- SourceType
- RepositoryName
- OriginalUrl
- RetrievedUtc
- OriginalFileName
- ArchivedRelativePath
- Sha256
- MimeType
- FileSize
- PrivacyClass
- IsOriginalImmutable
- Notes

### Citation
Identifies the exact portion of a source used by a claim.
- Id
- SourceId
- Page
- ImageNumber
- LineNumber
- HouseholdNumber
- EntryNumber
- FieldName
- LocatorText
- QuotedOrTranscribedText
- CreatedUtc

### ResearchClaim
A proposed assertion. It is never automatically an accepted-tree fact.
- Id
- SubjectPersonId
- ClaimType
- ProposedValue
- NormalizedValue
- Status
- Confidence
- CreatedUtc
- UpdatedUtc
- AcceptedTreeFactId nullable

Statuses: Unverified, Possible, Probable, Verified, Conflicting, Rejected.

### EvidenceLink
Connects a citation to a claim.
- Id
- ClaimId
- CitationId
- Direction: Supports, Conflicts, Neutral
- EvidenceClass: Direct, Indirect, Negative
- SourceOriginality: Original, Derivative, AuthoredNarrative, Unknown
- InformantKnowledge: Primary, Secondary, Unknown
- ProximityScore
- IndependenceGroup
- Weight
- Notes

### ResearchHypothesis
Allows aggressive research without contaminating the accepted tree.
- Id
- SubjectPersonId
- HypothesisType
- Description
- Status
- CreatedUtc
- ClosedUtc

### CandidateIdentityMatch
A comparison between a source/person candidate and an existing person.
- Id
- ExistingPersonId
- CandidateReference
- NameScore
- DateScore
- PlaceScore
- HouseholdScore
- RelativeScore
- OccupationScore
- AssociateScore
- NegativeEvidencePenalty
- TotalScore
- ExplanationJson
- Decision: Unreviewed, SamePersonProposed, DifferentPersonProposed, Rejected

No score may automatically merge people.

### ResearchConflict
- Id
- SubjectPersonId
- ClaimType
- ClaimAId
- ClaimBId
- Severity
- Explanation
- ResolutionStatus
- ResolvedUtc

### AiAnalysis
AI analysis has zero evidentiary weight.
- Id
- RelatedClaimId nullable
- RelatedHypothesisId nullable
- Provider
- Model
- Role: Extractor, Researcher, Skeptic, SourceCritic, TimelineReviewer, Synthesizer
- PromptHash
- ResponseText
- StructuredResponseJson
- Confidence
- CreatedUtc
- PrivacyRoute

### ResearchTask
- Id
- SubjectPersonId nullable
- RelatedClaimId nullable
- Question
- TaskType
- Priority
- ExpectedInformationGain
- ExpectedEvidenceStrength
- EstimatedCost
- Status
- CreatedUtc
- CompletedUtc

### ApprovalRecord
- Id
- EntityType
- EntityId
- Action
- Actor
- Reason
- CreatedUtc

### ProofPacket
- Id
- SubjectPersonId
- Question
- GeneratedUtc
- FinalStatus
- DeterministicScore
- PacketJson
- ApprovedUtc nullable

## Invariants
1. Research claims and hypotheses are separate from accepted-tree facts.
2. AI analysis never counts as evidence.
3. Original archived sources are immutable after hashing.
4. A hash mismatch raises an integrity warning and cannot be silently repaired.
5. All accepted-tree promotions create an ApprovalRecord.
6. Conflicting evidence remains queryable after a conflict is resolved.
7. CandidateIdentityMatch cannot perform a merge.
8. Existing stable person IDs and historical Legacy Numbers are not replaced by this model.
9. High-risk migrations require backup and verification.
10. Real genealogy fixtures are prohibited from the public repository.

## First migration strategy
Before adding physical tables, reconstruct the existing application schema from the bootstrap source and identify its database technology/migration mechanism. Then add the research entities as additive tables with foreign keys to existing stable person IDs where safe. Do not rename or repurpose existing columns during P0.
