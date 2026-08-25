# National Archives Catalog Search

The Research Center includes a read-only search adapter for the U.S. National
Archives and Records Administration (NARA) Catalog.

## Data boundary

- Automatic searches run only for `PublicHistorical` research profiles.
- Living-person and sensitive profiles are never sent to NARA.
- The manual search box warns the user to enter only deceased-person or public
  historical search terms.
- Results are candidate citations, not evidence and not accepted-tree facts.
- The adapter does not download originals, bypass access controls, or write
  tags, comments, or transcriptions to NARA.

## Interface

NARA documents Catalog API v2 at
`https://catalog.archives.gov/api/v2/api-docs/`. Its official documentation
states that direct API access uses an issued API key and is limited by a monthly
query allowance. No NARA key or other credential is stored in this public
repository.

The current read-only adapter uses the public `/proxy/records/search` route
used by the official Catalog web application. The provider is isolated behind
`ISourceSearchProvider`, so a future issued v2 API key or endpoint change can be
handled without changing genealogy logic. Network or response failures degrade
to a provider error and never change the accepted tree.

Official references:

- https://www.archives.gov/developer
- https://github.com/usnationalarchives/Catalog-API
