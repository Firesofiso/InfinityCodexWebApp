# Feature: DKP Management

## Status

- Stage: Proposed
- Priority: High
- Last updated: 2026-04-11
- Related features:
  - `docs/features/player-character-detail-page.md`
  - `docs/features/gear-wishlist-management.md`

## Goal

Provide a clear, secure DKP workflow where players can view their DKP, and authorized roles can perform adjustments with traceable history.

## User Value

- Players can trust and quickly verify their current DKP balance.
- Authorized staff can maintain accurate DKP balances with accountability.

## In Scope

- Read current DKP balance for authenticated player
- Read DKP transaction history for authenticated player
- Authorized adjustments (add, subtract, correction)
- Adjustment reason capture and audit trail
- Optional filters for DKP history (date range, adjustment type)
- Validation and permission enforcement for adjustments

## Out Of Scope

- Wishlist CRUD behavior
- Item catalog curation
- Mission progress updates
- Job level synchronization
- Group event scheduling UI

## Data Ownership And Source Rules

- Own database is the source of truth for DKP balances and DKP transactions.
- DKP balance should be derivable from transaction history or maintained with transaction-safe updates.

## Domain Model (Minimum)

- Player identifier
- Current DKP balance
- DKP transaction record:
  - Transaction identifier
  - Player identifier
  - Delta amount (positive or negative)
  - Reason
  - Actor identifier
  - Timestamp
  - Optional metadata (event/ref note)

## Backend Requirements

### Read endpoints

- Get DKP balance for authenticated player
- Get DKP transaction history for authenticated player
- Admin/manager endpoint to query another player's DKP history (role-gated)

### Write endpoints

- Authorized endpoint to create DKP adjustment transaction

### Validation

- Auth required for all endpoints
- Players can only read their own DKP unless elevated role is present
- Only authorized roles can mutate DKP
- Reject zero-value adjustments
- Require non-empty reason for all adjustments
- Ensure balance updates and transaction writes are atomic

## Frontend Requirements

## UX behavior

- Player-facing view shows current balance and recent history
- Adjustment controls only appear for authorized roles
- Adjustment form requires amount and reason
- Save states and error states are visible and clear

## Error and fallback behavior

- Read failure shows retry and does not crash surrounding page
- Write failure preserves unsaved form values
- Permission failures show explicit unauthorized messaging

## Acceptance Criteria

1. Authenticated player can view their own DKP balance.
2. Authenticated player can view their own DKP transaction history.
3. Unauthorized player cannot view another player's DKP data.
4. Authorized role can submit positive and negative DKP adjustments.
5. Every adjustment persists a transaction with actor, reason, and timestamp.
6. Balance and transaction history remain consistent after adjustments.
7. Unauthorized roles cannot access DKP adjustment endpoints.
8. UI clearly separates read-only player view from privileged adjustment actions.

## Suggested Implementation Order

1. Define DKP transaction schema and consistency model.
2. Implement player read endpoints (balance and history).
3. Implement authorized adjustment endpoint with audit fields.
4. Implement player-facing DKP panel UI.
5. Implement role-gated adjustment UI.
6. Add permission, validation, and consistency test coverage.

## Test Coverage Targets

- Backend:
  - Auth and authorization tests for all read/write endpoints
  - Atomic consistency tests for balance plus transaction writes
  - Validation tests for amount and reason rules
- Frontend:
  - Player read view rendering and error handling
  - Role-gated visibility for adjustment controls
  - Adjustment submission success and failure states

## Open Questions

- Which exact roles can perform DKP adjustments in v1?
- Can DKP balance go negative, and if yes, are there limits?
- Should manual adjustments require secondary approval?
- Should history include soft-deleted/corrected transaction visibility rules?

## Async Prompt Shortcut

Use this when kicking off implementation in another thread:

"Implement only docs/features/dkp-management.md. Keep scope to DKP reads, authorized adjustments, validation, audit trail, and acceptance criteria."
