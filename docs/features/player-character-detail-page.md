# Feature: Player Character Detail Page

## Status

- Stage: Active
- Priority: High
- Last updated: 2026-04-11

## Goal

Provide a single page where an authenticated player can view and manage personal and character progression data.

## User Value

This page gives players one place to answer:

- What is my current account-level status?
- What does this character still need?
- What is this character's progression state right now?

## In Scope

### Player-specific information

- DKP
  - Read-only
  - Source: own database
- Gear Wishlist
  - Summary and entry point only in this feature
  - Source: own database
  - Full CRUD is specified in `docs/features/gear-wishlist-management.md`

### Character-specific information

- Mission Status
  - Editable
  - Source: own database
- Job Levels
  - Read-only
  - Source: Horizon integration

## Out Of Scope (for this feature slice)

- DKP editing or admin adjustments from this page
- Gear Wishlist CRUD behavior details (moved to dedicated feature spec)
- Job level editing from this page
- Full inventory management beyond wishlist interactions
- Group or linkshell-wide views
- New auth flows

## Data Ownership And Source Rules

- Own database is the source of truth for:
  - DKP
  - Gear Wishlist
  - Mission Status
- Horizon integration is the source of truth for:
  - Job Levels

If Horizon is unavailable, show the last known snapshot timestamp (if available) and a non-blocking warning state for job levels.

## Backend Requirements

### Read endpoints

- Get player detail payload:
  - Includes DKP and Gear Wishlist
- Get character detail payload:
  - Includes Mission Status and Job Levels

### Write endpoints

- Update Mission Status

### Validation

- Auth required for all endpoints
- A player can only access their own player profile and owned characters
- Reject invalid mission status transitions if the domain defines transition rules

## Frontend Requirements

## Page layout

- Two-zone layout:
  - Primary zone for player-owned information (DKP and wishlist)
  - Secondary zone for character-specific information (mission status and job levels)
- Header:
  - Character identity summary (name, optional avatar, primary job if known)
- Player section:
  - DKP panel (read-only)
  - Gear Wishlist panel (summary + navigation handoff)
- Character section:
  - Mission Status panel (editable)
  - Job Levels panel (read-only, Horizon sourced)

## UX requirements

- Distinguish read-only fields clearly from editable fields
- Save interactions should be explicit for editable sections
- Show loading, success, and error states per panel
- Avoid blocking unrelated panels when one panel fails to load

## Error and fallback behavior

- If own-database data fails to load, show panel-level error and retry action
- If Horizon data fails to load, keep page usable and show job-levels panel warning state
- If write fails for mission status, preserve unsaved changes in the UI and show actionable error text

## Acceptance Criteria

1. Authenticated user can open detail page for an owned character.
2. Page displays DKP as read-only from own database.
3. Page displays Gear Wishlist summary and links into the dedicated Gear Wishlist workflow defined in `docs/features/gear-wishlist-management.md`.
4. User can edit Mission Status and persist changes.
5. Page displays Job Levels as read-only from Horizon data.
6. User cannot edit Job Levels in this feature.
7. User cannot view or modify another player's data.
8. Panel-level errors and retries behave without breaking the full page.

## Suggested Implementation Order

1. Build read models and page load API shape.
2. Implement read-only panels (DKP, Job Levels).
3. Implement Gear Wishlist summary panel and navigation handoff to dedicated feature flow.
4. Implement Mission Status edits.
5. Add auth/ownership guards and test coverage.
6. Add panel-level error and fallback states.

## Test Coverage Targets

- Backend:
  - Auth and ownership checks for read and write endpoints
  - Validation behavior for wishlist and mission status updates
  - Horizon unavailable fallback behavior
- Frontend:
  - Panel render states (loading/success/error)
  - Handoff flow from detail page into Gear Wishlist feature
  - Edit and save flows for mission status
  - Read-only enforcement for DKP and Job Levels

## Open Questions

- What is the canonical mission status model and allowed transitions?
- Do we cache Horizon job levels, and if so, what freshness SLA should be shown?
- Should this page support selecting between multiple owned characters, or be single-character route only?

## Async Prompt Shortcut

Use this when kicking off implementation in another thread:

"Implement only the feature spec in docs/features/player-character-detail-page.md. Keep scope limited to listed in-scope items and acceptance criteria."
