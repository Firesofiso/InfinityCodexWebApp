# Feature Roadmap

High-level feature list for InfinityCodex — a webapp replacing the linkshell's Excel spreadsheets for Final Fantasy XI endgame management.

## Context

This app serves an FFXI linkshell (LS) and replaces 4–5 separate spreadsheets. A companion in-game addon will POST data directly to the API by reading the FFXI chatlog, so API endpoints must be robust and well-documented enough to support that integration.

---

## Features

| # | Feature | Priority | Status | Spec |
|---|---------|----------|--------|------|
| 1 | DKP Tracker | High | Planned | — |
| 2 | Member Info | High | Planned | — |
| 3 | LS Pop Sets (Sky / Sea / Limbus) | Medium | Planned | — |
| 4 | Dynamis Split Calculator | Medium | Planned | — |
| 5 | Bank Management | Low | Planned | — |
| 6 | Party Composition Planner | Low | Planned | — |

---

## Feature Summaries

### 1. DKP Tracker (HIGH)
Track DKP (Dragon Kill Points) earned and spent by LS members. Replaces the DKP spreadsheet.
- Earn events: attendance at runs, kills, specific contributions
- Spend events: open auctions per item — members browse active auctions, place bids, and the highest bidder wins; DKP is deducted on win
  - Bid validation: user must meet any minimum bid requirement for the item
  - Bid validation: user must have sufficient DKP balance to cover the bid amount
  - Bid validation: user must be able to equip the item (job and level requirements met)
- Member balance history
- Admin controls to adjust points

### 2. LS Pop Sets (Sky / Sea / Limbus) (MEDIUM)
Track the linkshell's inventory of pop items / entry materials for Sky, Sea, and Limbus events.
- Per-zone set tracking (e.g. Kirin, Jailer of Love, Proto-Ultima sets)
- Quantity on hand vs. sets completable
- Who contributed items / when

### 3. Member Info (HIGH)
Central record for every LS member. Replaces the member info spreadsheet.
- Mission progress (CoP, ZM, ToAU, WoTG, etc.)
- Job levels (main + subs)
- Crafting levels (all crafts)
- Gear needs/wishlist by content: Dynamis, Limbus, Sky, Sea, HENM, misc NMs

### 4. Dynamis Split Calculator (MEDIUM)
Calculate how Dynamis currency is split across members after a run.
- Buy-in = 1,000,000 gil ÷ number of attendees
- Attendance tracked in 30-minute increments; time-spent multiplier adjusts each member's payout
- Input: total currency dropped, attendee list with check-in/check-out times
- Output: per-member share, remainder handling
- History of past splits

### 5. Bank Management (LOW)
Track items and gil held in the LS bank / mule characters.
- Item inventory with quantities
- Check-in / check-out log
- Current holder per item stack

### 6. Party Composition Planner (LOW)
Plan and save party compositions for events.
- Slot members into jobs/roles
- Validate job balance (e.g. tank/healer/DD/support coverage)
- Save named compositions for recurring events
- Pull from Member Info to see available jobs per member

---

## Companion Addon Requirements (cross-cutting) (LOW)

All features that can receive data from in-game must have API endpoints supporting:
- Authenticated POST from the addon (API key or token per character/player)
- Chatlog-parseable payloads (the addon reads FFXI chatlog and maps fields)
- Idempotent writes where possible (re-sending the same event shouldn't duplicate data)
- Clear error responses returned to the caller (how they are handled is the addon's concern)

---

## Open Questions

- [x] What auth model does the addon use? → Shared LS API key (single key for all addon users)
- [x] Which features need real-time updates vs. batch import from chatlog? → The addon detects in-game system messages (e.g. item obtained), parses player + item, and POSTs to the API in real time. The webapp just receives and records the request.
- [x] Is there a priority order for implementation beyond "bank is low priority"? → High: DKP Tracker, Member Info. Medium: LS Pop Sets, Dynamis Split Calculator. Low: Bank Management, Party Composition Planner.
- [x] Are there multiple LS groups or is this single-linkshell? → Single linkshell
- [x] DKP: is this a bid system, fixed-price, or open-roll style? → Bid system. Some items are restricted with a minimum bid requirement.
- [x] Dynamis splits: fixed equal split, or weighted by attendance/contribution? → Weighted by time attended. Buy-in = 1,000,000 gil ÷ number of attendees. Each member's payout is then modified by a time-spent multiplier, tracked in 30-minute increments.
