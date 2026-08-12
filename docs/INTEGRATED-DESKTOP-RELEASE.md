# Loper Family Tree Builder — Integrated Desktop Release

Status: Active development
Branch: `agent/integrated-desktop-release`
Delivery target: one Windows installer after integrated validation

## Non-negotiable preservation rules

- Robert J. Loper Legacy Numbers remain immutable historical text.
- Stable internal IDs remain separate from Legacy Numbers.
- Original source records, media, citations, and imported archive data are never silently rewritten.
- Automated genealogy corrections are review-first. The system may flag or propose; it must not silently alter historical evidence.
- No automatic duplicate merges.
- Medical, DNA, and other sensitive family information must remain permission-controlled.
- Development and testing remain local/private. No public deployment is part of this release.
- Existing user data must survive an application upgrade.

## Release architecture

The integrated release will be developed and validated in modules, but delivered to the end user as a single Windows installer. The installer must install or upgrade the application without requiring Visual Studio, PowerShell, Command Prompt, manual .NET commands, or manual SQL Server configuration.

### 1. Application shell and HUD design system

- Permanent left navigation optimized for large family trees.
- Futuristic neon command-center visual system across every module.
- Dark navy/black surfaces with cyan/teal and violet accents.
- Readable typography and high contrast.
- Consistent cards, data grids, forms, dialogs, alerts, status indicators, filters, and action buttons.
- Independent workspace scrolling where appropriate.
- Global search available throughout the application.

### 2. Dashboard

- Tree statistics and research status.
- Recent people, records, photos, documents, and edits.
- Data quality alerts.
- Research tasks and unresolved evidence.
- Medical/family health pattern alerts without diagnosis.
- Backup status.
- Quick actions for Add Person, Import, Search, Research, Documents, Photos, and Reports.

### 3. People and relationships

- High-volume person list with search, filter, sorting, paging/virtualization.
- Person profile workspace.
- Parents, spouses, partners, children, siblings, and relationship evidence.
- Birth, marriage, divorce, death, burial, residence, education, employment, military, travel, and custom events.
- Names, alternate names, nicknames, prefixes, suffixes, and notes.
- Robert J. Loper Legacy Number displayed prominently but never auto-renumbered.

### 4. Family tree visualization

- Pedigree and descendant views.
- Relationship navigation.
- Expand/collapse branches.
- Large-tree performance safeguards.
- Evidence and data-quality indicators.

### 5. Timeline and biography

- Chronological life timeline.
- Evidence-linked events.
- Narrative biography workspace.
- Historical context notes separated from sourced personal facts.

### 6. Geography and lifetime migration

- Dated historical addresses and locations.
- Birth, marriage, school, work, military assignment, residence, cemetery, death, and documented travel locations.
- Coordinates stored separately from original place text.
- Lifetime movement timeline/map.
- Date ranges and uncertainty preserved.
- Source links for every mapped event where available.

### 7. Documents and sources

- Source repository with citations and provenance.
- Historical records, certificates, census records, military records, obituaries, correspondence, and custom documents.
- Original file preservation.
- Transcription and notes kept separately from the original.
- Source-to-person and source-to-event linking.

### 8. Photos and media

- Photo/media library.
- Person and event tagging.
- Caption, date, place, photographer/source, copyright/provenance, and notes.
- EXIF metadata extraction when present, including GPS coordinates and capture date.
- Original media preservation.

### 9. AI cursive transcriber

- Upload handwritten, faded, or difficult historical documents.
- Produce a proposed transcription.
- Preserve original image, generated transcription, confidence/uncertainty, corrections, citation, and provenance.
- Human approval before transcription becomes accepted archive text.

### 10. Automated tree error checker

Flag for review rather than silently changing data, including:

- death before birth;
- impossible or highly improbable parent/child ages;
- child born after a parent's death beyond plausible limits;
- conflicting dates;
- duplicated/conflicting events;
- incompatible marriage/relationship chronology;
- suspicious Legacy Number relationships without modifying the Legacy Number itself.

### 11. Census timeline

- Structured census households.
- Compare household composition across census years.
- Track ages, relationships, occupations, birthplaces, addresses, and changes over time.
- Link every assertion to its census source.

### 12. Medical & Family Health History

- Conditions per person.
- Diagnosis/onset date or age.
- Status: active, resolved, historical, suspected, confirmed, family-reported.
- Severity and hereditary relevance flags.
- Providers/facilities.
- Hospitalizations, surgery, injuries, strokes, heart attacks, cancer, hospice, and custom medical events.
- Structured cause of death and contributing conditions.
- Documents and citations.
- Family Health Pattern report.
- Search a condition across relatives.
- Medical pedigree/genogram.
- No automatic diagnosis.

### 13. DNA match clustering

- Private DNA match imports.
- Graph/network clustering for likely shared ancestral lines.
- Evidence and confidence indicators.
- No sensitive relationship conclusion exposed without evidence and user review.

### 14. Research intelligence

- Research task queue.
- Candidate record tracking.
- AI-assisted summaries and suggestions clearly labeled as proposals.
- Evidence comparison.
- Research log and provenance.
- Global archive-search framework for future authorized online research integrations.

### 15. GEDCOM

- Import validation before committing data.
- Complete and branch exports.
- Preserve unsupported/custom data wherever possible.
- Duplicate candidates shown for review.
- Never use GEDCOM import as a reason to rewrite existing Legacy Numbers.

### 16. Reports

- Individual report.
- Family group report.
- Ancestor/descendant reports.
- Source and research reports.
- Data-quality report.
- Medical/family health pattern reports with privacy controls.
- PDF generation using the existing reporting stack.

### 17. Security and permissions foundation

Even while running locally, data access rules must be modeled for future authenticated hosting:

- owner/admin;
- editor;
- contributor;
- read-only family member;
- restricted medical/DNA permissions;
- record/photo/document-specific visibility where needed;
- audit history for sensitive changes.

### 18. Backup, restore, and archive safety

- Configurable data folder.
- Configurable backup folder, including local/network destinations.
- Manual backup.
- Automatic scheduled backup foundation.
- Restore validation.
- Pre-upgrade backup.
- Database plus attached media/documents included in the backup manifest.
- Backup verification and clear success/failure status.

### 19. Installer and upgrade behavior

- Official product name: Loper Family Tree Builder.
- Publisher: Phil Loper.
- Single Windows EXE installer.
- Desktop and Start Menu shortcuts.
- Does not auto-start with Windows.
- Opens the application after launch/install as configured.
- First-run data and backup folder selection.
- Existing data and configuration retained on upgrade.
- Code signing remains a future packaging step until a certificate is obtained.

## Integrated release gates

The installer is not considered ready for user review until all of the following pass:

1. Preservation tests for Legacy Numbers and original evidence.
2. Database migration/upgrade test using an existing installation.
3. GEDCOM validation and round-trip test.
4. Medical privacy tests.
5. Backup/restore test.
6. Installer clean-install test.
7. Installer upgrade test.
8. Dashboard/navigation smoke test.
9. Large-tree performance test using a dataset sized at or above the expected ~1,600-person baseline.
10. No public web deployment or anonymous family-data exposure.

## Delivery model

Development can produce internal CI artifacts as needed, but Phil receives the integrated desktop application only when a release candidate passes the gates above. The intended user-facing deliverable is:

`LoperFamilyTreeBuilderSetup.exe`
