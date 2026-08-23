# Public Research Providers

Research Center distinguishes native provider searches from guided link-outs so
users can see what the application actually searched. Every result remains a
candidate until a human reviews and cites it. No provider writes to the accepted
tree, and automated person searches run only for deceased/public historical
profiles.

## Native in-app searches

- **National Archives Catalog** uses the public read-only Catalog proxy.
- **Library of Congress** uses the official `loc.gov` JSON API. Results are
  citation-only candidates and original files are not automatically archived.

Neither integration needs or stores a credential.

## Guided external resources

- **New York Public Library Digital Collections** opens a query in the public
  search interface. The former Digital Collections API was retired on
  2026-08-01 with no public replacement, so the application does not depend on
  that obsolete authenticated API.
- **ResearchGuides.net Immigration** opens the curated passenger-list and
  immigration guide. It is a guide rather than a searchable record API.
- **AccessGenealogy** opens its site search. The UI warns that the site's own
  general search does not cover its individual databases, which may need to be
  searched separately.

Guided links use `noopener noreferrer` and do not send stored person records;
only text the user deliberately enters into the generic Research Center box is
included in the NYPL and AccessGenealogy search URLs.
