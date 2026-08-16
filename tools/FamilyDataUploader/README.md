# Loper Family Data Uploader

Windows bulk-ingestion utility for the private `Loper1976/LoperFamilyTreeData` repository.

## Workflow

1. Select a source folder.
2. Scan every file recursively.
3. Compute SHA-256 hashes and identify duplicate content inside the selected batch.
4. Block files above the configured 2 GB Git LFS safety limit.
5. Preserve the original relative folder structure under `INBOX/<batch-id>/originals/`.
6. Route files above 95 MiB through Git LFS.
7. Write `manifest.json` with path, size, SHA-256, LFS status, and duplicate references.
8. Commit and push the batch to the private family-data repository.

The source folder is never deleted or modified.

## PC requirements

- Windows 10 or later
- Git for Windows or GitHub Desktop
- Git LFS
- GitHub authentication for `Loper1976/LoperFamilyTreeData`

GitHub Desktop includes Git LFS. The application uses the existing Windows Git credential flow and does not store a GitHub token in the program.

## Safety

- Originals are immutable archive inputs.
- No automatic person matching or renaming occurs during ingestion.
- Classification happens after upload and can be reviewed.
- Robert J. Loper Legacy Numbers are never generated, normalized, or changed by this uploader.
