# Feature: Gear Wishlist Management

## Status

- Stage: Active
- Priority: High
- Last updated: 2026-04-11
- Parent feature: `docs/features/player-character-detail-page.md`

## Goal

Provide a dedicated workflow where a player can add, edit, and remove gear wishlist entries at the player level, with optional character assignment, reliable persistence, and clear save behavior.

## User Value

This feature lets players maintain a meaningful, current list of target gear instead of only seeing static item data.

## In Scope

- List wishlist entries for the authenticated player
- Require at least one owned character assignment for every wishlist entry
- Optionally clear character assignment from an existing entry
- Add wishlist entries
- Edit wishlist entries
- Remove wishlist entries
- Persist all changes to own database
- Show panel-level loading, success, and error states
- Preserve unsaved edits when writes fail

## Out Of Scope

- Item catalog curation and item metadata authoring (tracked in `docs/features/item-catalog-management.md`)
- DKP management (tracked in `docs/features/dkp-management.md`)
- Job level updates
- Group-shared wishlist management
- Admin moderation tools

## Data Ownership And Source Rules

- Own database is the source of truth for all wishlist entries.
- Horizon integration is not a source for wishlist data.

## Domain Model (Minimum)

Each wishlist entry should minimally support:

- Player identifier
- Item identifier
- Optional character identifier
- Optional note
- Optional priority
- Updated timestamp

If priority and notes are not implemented yet, start with item identifier plus timestamps and keep API forward-compatible.

## Backend Requirements

### Read endpoints

- Get wishlist entries for authenticated player
- Optional filter by owned character identifier
- Get selectable candidate items (or use existing catalog endpoint)

### Write endpoints

- Add wishlist entry
- Update wishlist entry
- Delete wishlist entry

### Validation

- Auth required for all endpoints
- Ownership check: caller can only read and mutate their own wishlist entries
- If character identifier is supplied, caller must own that character
- Reject unknown item identifiers
- Prevent duplicate wishlist entries per player, item, and character scope
  - One unassigned entry per item per player
  - One assigned entry per item per character per player
- Validate note length and priority range if present

## Frontend Requirements

## UX behavior

- Player can clearly see current wishlist entries
- Player can add from available items
- Player can update editable fields on existing entries
- Player can assign or unassign a wishlist entry to one or more owned characters
- First-time add defaults to the currently viewed character and allows adding more characters in the same action
- Player can remove entries with a clear action
- Save state is visible: idle, dirty, saving, saved, error

## Error and fallback behavior

- Read failure shows retry action without breaking rest of page
- Write failure keeps pending edits in UI
- Duplicate-add attempts show actionable validation message

## Acceptance Criteria

1. Authenticated player can load wishlist entries at player scope.
2. Player can optionally filter wishlist entries by owned character.
3. Player can add a wishlist entry with one or more owned character assignments and see it persisted after reload.
4. Player can edit a wishlist entry, including character assignment, and see changes persisted after reload.
5. Player can remove a wishlist entry and see deletion persisted after reload.
6. Duplicate wishlist entries for the same player, item, and character scope are rejected.
7. Player cannot read or mutate wishlist entries owned by another player.
8. Player cannot assign an entry to a character they do not own.
9. Save and error states are visible and understandable during add/edit/remove operations.

## Assignment Rules

- A wishlist entry is invalid unless assigned to at least one owned character.
- New entries default to the currently selected character.
- A user may assign a single wishlist entry to multiple owned characters.
- Removing the final assignment removes the wishlist entry from active needs.

## Suggested Implementation Order

1. Confirm backend contract and uniqueness constraints.
2. Implement read and list rendering states.
3. Implement add flow.
4. Implement edit flow.
5. Implement remove flow.
6. Add validations and ownership tests.
7. Harden UI error handling and unsaved-state behavior.

## Test Coverage Targets

- Backend:
  - Auth and ownership tests for player-scoped read and write operations
  - Character-ownership validation when assignment is provided
  - Duplicate prevention tests
  - Validation tests for payload shape, note length, and priority range
- Frontend:
  - Add/edit/remove flows
  - Assign and unassign character flows
  - Save-state transitions
  - Retry and failure preservation behavior

## Open Questions

- Do we need soft-delete history for wishlist entries?
- Should priority be numeric, tiered labels, or omitted for v1?
- Should notes be per entry or globally attached to item+scope pairs?
- Do we need bulk-add or bulk-remove interactions in v1?

## Async Prompt Shortcut

Use this when kicking off implementation in another thread:

"Implement only docs/features/gear-wishlist-management.md. Keep scope to wishlist add/edit/remove, persistence, ownership checks, and acceptance criteria."
