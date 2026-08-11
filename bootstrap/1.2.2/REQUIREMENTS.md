# Loper Family Tree Builder 1.2.2 Requirements

## Release name
Ancestry / Family Tree Maker Media Migration + Private Family Messaging + Futuristic HUD Theme

## Media migration
- Import media folders exported from Ancestry and Family Tree Maker without changing genealogy automatically.
- Preserve original files, filenames, timestamps, EXIF/GPS metadata, checksums, provenance and source-folder information.
- Detect exact duplicates by SHA-256 and flag probable visual/filename duplicates for review.
- Match media to people conservatively using filename, GEDCOM/FTM identifiers, existing media references and user review.
- Never alter or recalculate Robert J. Loper Legacy Numbers.
- Uncertain media-person matches go to a review queue.
- Original files are immutable; previews/thumbnails are derivative working files.

## Private member messaging
- Authenticated family members can send private messages to another active member.
- Inbox, Sent, Archived and Conversation views.
- Read/unread status and sent/read timestamps.
- Messages remain active for 120 days from sent date, then move automatically to archive state.
- Archiving does not delete message content. Archived messages remain retrievable by authorized participants.
- Search archived and active messages by participant, subject and message text.
- Sender and recipient can restore an archived conversation to their active view without changing the other participant's archive state.
- Owner/admin does not receive blanket access to private message content by default. Administrative access must be explicit, auditable and reserved for future policy controls.
- Message audit metadata may record sender, recipient, timestamps and status for system integrity without exposing message content in normal admin screens.
- Future attachment support should use the existing private archive permission model; 1.2.2 may include attachment-ready data structures but must not expose private archive files.
- No message or message metadata changes genealogy or Legacy Numbers.

## Messaging retention
- Auto-archive threshold: 120 days after SentUtc.
- Archive is logical, not destructive.
- Archived content is retained until an explicit future retention policy is configured.
- Retrieval must be available from the Archived Messages screen and search.

## Visual design direction
Use the approved futuristic neon command-center/HUD style throughout the application, matching the uploaded reference:
- Deep black/navy/obsidian background.
- Cyan/teal neon primary accents.
- Violet secondary glow/accent.
- Thin luminous panel borders and restrained outer glows.
- Compact telemetry-style labels and uppercase section headers where appropriate.
- Dark glass/command-center cards.
- Clear high-contrast readable body text.
- Permanent left navigation remains the primary desktop navigation.
- Navigation groups must remain clean and collapsible; do not return to a long unstructured menu.
- Messaging should resemble a secure communications console rather than a generic email client.
- Preserve usability for approximately 1,600 people and large archives.

## Messaging UI
- Left column: conversations/inbox with search, unread count and participant.
- Main workspace: conversation thread with message bubbles/panels, timestamps and read state.
- Header: participant identity, secure/private indicator, archive status and actions.
- Compose panel: recipient selector, subject and message body.
- Separate Archived view with date range, participant and keyword filters.
- HUD styling must remain professional, not decorative at the expense of readability.

## Release safety
- Cumulative upgrade from 1.2.1.
- Pre-upgrade backup remains mandatory.
- Local/private operation only. No public deployment to loper.family.
- Existing living-person privacy, medical privacy and family-role permissions remain intact.
- Robert J. Loper Legacy Numbers remain immutable historical data.
