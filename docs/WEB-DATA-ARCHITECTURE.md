# Loper Family Tree Builder Web Data Architecture

Status: Approved direction for future Loper.family launch

## Core principle

The public-facing application code, private family data, and private media must be separated. GitHub stores source code only. GitHub must never be used as the storage location for genealogy records, medical information, DNA data, photos, documents, credentials, database backups, or other private family archive content.

## Production architecture

### 1. Web application

- Hosted separately from the production data stores.
- Serves the Loper.family application UI and authenticated API endpoints.
- No private family database files are deployed inside the public web root.
- No production secrets are committed to source control.

### 2. Central production database

Use a managed relational database suitable for the final hosting platform, with PostgreSQL or SQL Server as approved implementation choices.

The production database is the authoritative shared record for web access and future synchronized desktop access. It stores structured data including:

- people and stable internal IDs;
- Robert J. Loper Legacy Numbers as immutable historical text;
- relationships;
- life events and dated locations;
- sources and citations;
- medical and family health data;
- permissions and security classifications;
- DNA metadata and clustering results where explicitly authorized;
- user accounts and application authorization data as appropriate;
- audit history;
- media metadata and pointers to private object storage.

The database must not silently modify historical genealogy data. AI-generated changes remain proposal-first and require review.

### 3. Private media/object storage

Large binary files are stored outside the relational database unless a specific feature requires otherwise. This includes:

- photographs;
- scanned records;
- census images;
- military records;
- PDFs;
- audio and oral-history recordings;
- video;
- transcriptions and derivative media;
- other archival files.

Objects must use private access by default. The application grants access through authenticated, authorized requests or short-lived signed access URLs. Direct anonymous public object access is prohibited unless the owner explicitly marks an item for public sharing.

### 4. Authentication and authorization

All non-public family information is protected behind authentication. Authorization is enforced server-side for every protected request.

The initial permission model must support at least:

- Owner / System Administrator;
- Family Administrator;
- Editor / Researcher;
- Contributor;
- Read Only Family Member;
- Limited / Guest access;
- configurable public-share access for specifically approved records only.

Sensitive data requires additional permissions. At minimum, access controls must distinguish:

- deceased-person genealogy data;
- living-person data;
- contact/address information;
- private photos/documents;
- medical and family health information;
- DNA information;
- editing rights;
- approval rights;
- administrative and security functions.

Medical and DNA information must default to the most restrictive appropriate access level.

### 5. Encryption and secrets

- HTTPS/TLS required for all production web traffic.
- Production database encryption at rest enabled where supported by the hosting provider.
- Private media storage encryption at rest enabled.
- Passwords stored only using approved salted password hashing through the identity framework; never reversible plaintext storage.
- Secrets, connection strings, API keys, encryption keys, and credentials stored in the hosting platform's secret-management facility or equivalent protected configuration.
- Secrets must never be committed to GitHub.

### 6. Audit and provenance

Security-sensitive and historical-data changes must be auditable. Record, where applicable:

- who performed the action;
- when it occurred;
- record affected;
- previous and new values for material genealogy changes;
- source/citation provenance;
- approval/rejection state for AI proposals;
- permission changes;
- medical/DNA access-relevant administrative changes;
- backup and restore operations.

Robert J. Loper Legacy Numbers remain immutable historical values and must never be automatically renumbered, normalized, or replaced.

## Backup architecture

Production must use multiple independent backup layers.

### Cloud backup

- Automated database backups.
- Point-in-time recovery where supported.
- Versioning or equivalent recovery controls for private media storage.
- Backup retention policy appropriate for a permanent family archive.

### Owner-controlled backup

The system must support scheduled export/backup to owner-controlled storage, including a Windows/network-drive destination when the desktop or backup agent has access to that location.

The owner-controlled backup should be capable of including:

- full structured database export/backup;
- media files;
- manifests and integrity hashes;
- application configuration needed for recovery, excluding plaintext secrets;
- a human-readable backup report.

Backups must be restorable and periodically verified. A backup is not considered sufficient merely because files were copied successfully.

## Desktop and web relationship

The future target architecture is a central authoritative production database rather than independent conflicting web and desktop master trees.

Until web launch is explicitly approved, the desktop/local application remains the active private development and archive environment. Migration to the central production data store must include:

- pre-migration backup;
- validation of Legacy Numbers and stable IDs;
- database schema migration testing;
- media integrity validation;
- duplicate/conflict review rather than automatic destructive merging;
- permission assignment review;
- post-migration verification and rollback capability.

Future desktop access may use secured APIs and/or a synchronization layer, but it must not create an unmanaged second authoritative database.

## Deployment safety gates

Loper.family must not be considered ready for production until all of the following are verified:

- authentication enabled;
- role and record-level authorization tested;
- living-person privacy rules tested;
- medical and DNA restrictions tested;
- HTTPS enforced;
- secrets removed from source control;
- production database isolated from the public web root;
- private media storage confirmed non-public by default;
- backup and restore test passed;
- audit logging verified;
- upgrade/migration rollback tested;
- penetration/security review appropriate to the hosting model completed;
- owner explicitly approves production launch.

## Source-control rule

The repository may contain schemas, migrations, seed structures, test fixtures containing fictional data, deployment templates, and documentation. It must not contain real private family archive data, production database dumps, photographs, medical records, DNA records, credentials, secrets, or production backups.
