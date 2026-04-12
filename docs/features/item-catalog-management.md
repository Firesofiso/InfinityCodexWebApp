# Feature: Item Catalog Management

## Status

- Stage: Proposed
- Priority: High
- Last updated: 2026-04-11
- Related features:
  - `docs/features/gear-wishlist-management.md`
  - `docs/features/player-character-detail-page.md`

## Goal

Provide a managed item catalog where authorized users can create, edit, and maintain item metadata used by wishlist and planning features.

## User Value

Players get a reliable source of item information for wishlist and planning workflows, while maintainers can keep item data clean and current.

## In Scope

- List catalog items with basic filters and search
- Create catalog items
- Edit catalog item metadata
- Archive or deactivate catalog items
- Manage item-job eligibility mappings
- Manage item source tags and references
- Validate required metadata and prevent invalid duplicates
- Role-based permissions for who can curate catalog data

## Out Of Scope

- Player-specific wishlist CRUD behavior
- DKP behavior
- Mission tracking
- Horizon job-level synchronization
- Group scheduling features

## Data Ownership And Source Rules

- Own database is the source of truth for item catalog data.
- Horizon integration is not a source for catalog metadata.
- Import files may seed catalog data, but persisted catalog records remain authoritative after import.

## Domain Model (Minimum)

Each catalog item should minimally support:

- Item identifier
- Name
- Slot or category
- Required level (if applicable)
- Optional notes
- Active flag
- Updated timestamp

Related models:

- Item allowed jobs mapping
- Item source mapping

## Backend Requirements

### Read endpoints

- Get item catalog list with filter and search parameters
- Get item detail by identifier

### Write endpoints

- Create item
- Update item
- Archive or deactivate item
- Manage item allowed jobs
- Manage item source mappings

### Validation

- Auth required for all endpoints
- Role authorization required for all write endpoints
- Reject duplicate active items by canonicalized name plus slot/category
- Validate required fields and allowed ranges
- Validate referenced jobs and sources exist

## Frontend Requirements

## UX behavior

- Authorized users can browse and search catalog items
- Authorized users can create and edit item metadata
- Authorized users can manage allowed jobs and source mappings
- Unauthorized users get read-only experience or denied actions based on role policy
- Save state and validation feedback are clear and actionable

## Error and fallback behavior

- Read failures show retry actions with non-destructive UI state
- Write failures preserve user input and display field-level errors when available
- Conflicts (duplicate rules) show clear resolution guidance

## Acceptance Criteria

1. Authorized user can create a new catalog item with required metadata.
2. Authorized user can edit item metadata and persist changes.
3. Authorized user can archive or deactivate an item without deleting history.
4. Authorized user can manage allowed jobs for an item.
5. Authorized user can manage source mappings for an item.
6. Duplicate item creation is rejected according to uniqueness rules.
7. Unauthorized users cannot perform catalog write operations.
8. Wishlist feature consumes catalog items from this source without requiring catalog edits.

## Suggested Implementation Order

1. Confirm role policy and authorization boundaries.
2. Implement catalog read/list endpoints and UI list view.
3. Implement create and edit flows with validation.
4. Implement archive/deactivate behavior.
5. Implement job and source mapping management.
6. Add permission and validation test coverage.

## Test Coverage Targets

- Backend:
  - Authorization tests for read/write paths
  - Duplicate and validation tests
  - Mapping integrity tests for jobs and sources
- Frontend:
  - List/filter/search behavior
  - Create/edit/archive flows
  - Role-based UI action gating
  - Error and conflict handling

## Open Questions

- Which roles can create/edit/archive catalog items in v1?
- Should archiving hide items from wishlist pickers by default?
- Do we need revision history for item metadata changes?
- Should imports upsert existing catalog entries or only create new entries?

## Async Prompt Shortcut

Use this when kicking off implementation in another thread:

"Implement only docs/features/item-catalog-management.md. Keep scope to catalog curation, metadata editing, role authorization, and acceptance criteria."
