# Data retrieval audit and redesign

## Decision and inventory

Use a shared, read-only GraphQL reader for the primary Avalonia frontend. Keep the existing REST adapter as a compatibility path for WinForms and its explicitly single-page API browser; REST Admin is a legacy API. All shipped GraphQL documents are schema-validated against 2026-10. The operator still chooses the API URL/version; this change does not silently alter a remembered profile or add access scopes.

| Feature | Baseline finding | Result and boundaries |
| --- | --- | --- |
| Products | REST pagination existed; modern product model and optional-image mapping needed review | GraphQL productVariants cursor pages return all variants with parent product metadata, including the mandatory default variant; image URLs, legacy IDs and price/null values are retained. Never create or split products as part of a read. |
| Categories | REST collection_listings is scoped to publication/channel listings, not the complete collection set | Primary UI reads collections; smart view uses an explicit smart-collection filter. Compatibility UI combines full custom_collections and smart_collections. |
| Smart collections | Async REST path existed but shared loading state was incomplete | Full GraphQL pagination in the primary UI, guarded REST pagination in compatibility UI. |
| API browser | Async GET existed but had no consistent loading/contradictory-action feedback | Explicit read-only single-response view, same-origin guard, wait state, disabled input and cancellation on window close/logout. It is not advertised as a complete export. |
| Suppliers | No implemented supplier master flow; a disabled menu was not evidence of a working feature | Explicit unavailable explanation and #11. Product vendor strings are not supplier master records or supplier order snapshots. |
| Supplier purchase orders | No implementation | Current official docs describe new stable reads, but the available 2026-10 validator rejects their root fields. Qualification and supplier workflow are tracked in #11; do not ship unvalidated queries or present failure as zero orders. |
| Transfers | No implemented flow | Validated inventoryTransfers plus inventoryTransfer.lineItems reads, with every parent and child page retrieved. Requires read_inventory_transfers and user permission; denied/missing support is an error, not an empty list. No receiving, cancellation, stock booking or mutations. |
| XLSX | Existing Windows shell/file-dialog coupling | Export only a complete current dataset. Toolkit file picker and in-memory generation; account changes cancel export before opening/writing the selected destination. No automatic shell launch. |

## Completeness and transactional behavior

GraphQL pages require nodes, boolean hasNextPage and a usable advancing endCursor. Reject duplicate/missing record IDs, repeated cursors, malformed pages and GraphQL errors even if the response also contains partial data. Transfer line connections are paginated independently for every parent; a transfer with no lines keeps a header row with unknown quantities rather than a fabricated zero. A 10,000-page safety bound fails explicitly rather than truncating successfully.

Products are listed at technical variant level with nested product columns. Different variants remain distinct rows keyed by their variant GID; parent GID and legacyResourceId are separate fields. This preserves existing/historical multi-variant data without combining stock or creating assortment variants. Tabular columns now reflect explicit API names (for example product.id, product.title, price); this intentional export schema change replaces ambiguous product-versus-variant columns. Optional fields remain null/blank, never an assumed numeric zero. Arrays retain their JSON structure.

REST next links are parsed with relation attributes and resolved against the current resource, including query-only relative links. Validate every destination against the configured HTTPS API origin/path before sending credentials. Canonical loop and duplicate-ID checks reject repeated pages. The model base now explicitly requires ID; concrete product/collection implementations retain their serialized ID contract.

The shared RetrievalPresenter clears the old view at refresh start and commits records only after successful complete retrieval. Only one refresh runs at a time. UI loading/progress/wait cursor, cancellation, action availability and export gating are restored on success, failure, cancellation and timeout. Logout immediately clears/invalidate results and cancels the old operation; a late response/finally block cannot change a new account's results or busy state. DataTable mapping and XLSX generation run outside the UI thread. Lists are subject to ordinary concurrent shop edits; no point-in-time shop snapshot is promised, and detectable inconsistencies fail rather than silently duplicating rows.

## Transport and errors

Authenticated GET and the internal read-only GraphQL POST use the same origin-bound transport. Redirects remain disabled. Read requests retry HTTP 429/500/502/503/504 at most twice. Respect Retry-After delta/date; waits above 30 seconds fail instead of retrying earlier than requested. Fallback waits are bounded. HTTP-200 GraphQL THROTTLED responses retry at most twice. Cancellation/logout interrupts requests and waits. Per-request timeout is 30 seconds, and a refresh has a ten-minute overall cancellation bound.

Do not echo server error bodies, raw network exception messages or authentication material. Permission/version failures explain that access/configuration must be checked. Automatic empty/partial exports are prohibited. Necessary live verification must be targeted and read-only; this change uses controlled responses and does not claim live shop completeness.

## Verification

NUnit covers empty/single/100-item boundary/201-record pagination, exact counts/unique IDs, relative and unsafe REST links, loops/duplicates, later-page errors, partial GraphQL errors, transfer parent/child pages, rate limits/Retry-After, cancellation, timeout, contradictory refreshes and late old-account responses. Avalonia control-tree tests cover responsive pending I/O, wait/loading state, cancellation, logout clearing, failure cleanup and export gating. WinForms compatibility tests cover full manual+automated collections and REST regression cases. CI runs shared and Avalonia tests plus native startup on Windows/Linux/macOS, with Windows-only DPAPI/WinForms checks guarded.

## Primary references and remaining qualification

- [REST legacy status and Product migration](https://shopify.dev/docs/api/admin-rest/latest/resources/product)
- [Cursor pagination](https://shopify.dev/docs/api/usage/pagination-graphql)
- [Collections query](https://shopify.dev/docs/api/admin-graphql/2026-10/queries/collections)
- [Inventory transfers and required scope](https://shopify.dev/docs/api/admin-graphql/2026-10/queries/inventoryTransfers)
- [New purchase-order public reads and scope](https://shopify.dev/docs/apps/build/inventory-management-apps/physical-inventory-feature-preview)

Supplier master/purchase-order qualification is explicit in #11. Deployment and clean-machine qualification remain #3/#4. No live shop mutation, product/variant restructure or production release is part of this audit.
