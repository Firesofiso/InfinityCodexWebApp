# Feature Specification: DKP Tracker

**Feature Branch**: `dkp-tracker`
**Created**: 2026-04-19
**Status**: Draft
**Input**: User description: "DKP Tracker"

## User Scenarios & Testing *(mandatory)*

<!--
  User stories are ordered by priority. Each story is independently deployable
  and delivers standalone value.
-->

### User Story 1 - View DKP Balance (Priority: P1)

Any logged-in user can view the current DKP balance for any member, including their own. The roster (Member Info) already surfaces each member's DKP total; this story covers the dedicated balance view with history.

**Why this priority**: The balance is the core data this feature exists to display. All other journeys depend on balances being visible and accurate.

**Independent Test**: Can be tested by seeding a member with earn and spend records, then verifying the displayed balance matches the expected sum.

**Acceptance Scenarios**:

1. **Given** a member has DKP records, **When** a user views that member's DKP summary, **Then** the current balance is displayed along with a chronological list of earn and spend events.
2. **Given** a member has a zero balance, **When** their DKP summary is viewed, **Then** a zero balance is shown with an empty history rather than an error.

---

### User Story 2 - Record a DKP Earn Event (Priority: P1)

An officer records a DKP earn event (a run, a kill, or a specific contribution) by selecting the event type, the point value, and the list of attending members. DKP is awarded in bulk to all attendees.

**Why this priority**: Without a mechanism to award DKP, there is nothing to spend. This is the primary write path for the whole feature.

**Independent Test**: Can be tested by an officer creating a run event with two attendees and confirming both members' balances increase by the awarded amount.

**Acceptance Scenarios**:

1. **Given** an officer selects event type, point value, and two or more attendees, **When** they submit the earn event, **Then** each attendee's DKP balance increases by the specified amount and a DKP history entry is created for each.
2. **Given** an officer submits an earn event, **When** the event is saved, **Then** an event record is stored with the event type, point value, date, and attendee list for audit purposes.
3. **Given** an invalid point value (e.g. negative or zero), **When** an officer attempts to submit the earn event, **Then** submission is blocked with a descriptive validation message.

---

### User Story 3 - Browse and Bid on Active Auctions (Priority: P1)

A member views the list of currently open item auctions and places a bid. The system validates that the bid meets the item's minimum bid requirement, the member has sufficient DKP balance, and the member's characters meet the item's job and level requirements.

**Why this priority**: Bidding is the primary spend mechanic and the most interactive member-facing workflow.

**Independent Test**: Can be tested by opening an auction for a specific item, placing a valid bid as an eligible member, and confirming the bid is recorded and the current highest bid updates.

**Acceptance Scenarios**:

1. **Given** an auction is open and a member meets all eligibility requirements, **When** they submit a bid, **Then** the bid is recorded as final; no further changes are permitted by that member on this auction.
2. **Given** a member has insufficient DKP to cover their bid amount, **When** they attempt to submit the bid, **Then** the bid is rejected with a message indicating their current balance.
3. **Given** an item has a minimum bid requirement, **When** a member submits a bid below the minimum, **Then** the bid is rejected with the minimum amount shown.
4. **Given** none of a member's characters meet the job or level requirements for an item, **When** they attempt to bid, **Then** the bid is rejected and the unmet requirements are listed.
5. **Given** a member (including an officer who is bidding) views the active auction list, **When** the list loads, **Then** each auction shows the item name, minimum bid (if any), and the time the auction was opened — but not bids placed by any other participant. The running officer (who opened the auction) can additionally see all bids placed so far.
6. **Given** a member has already placed a bid on an open auction, **When** they attempt to bid again, **Then** the second bid is rejected; each member may only bid once per auction.

---

### User Story 4 - Open and Close Auctions (Priority: P1)

An officer opens an auction for a specific item from the Item Catalog. Only the officer who opened the auction can see all bids while it is active and is responsible for closing it. All other users — including other officers — can only see their own bid. When the auction closes, the running officer reviews all bids, confirms or manually selects the winner (required for ties resolved by in-game lots), and the winner's DKP is deducted.

**Why this priority**: Officers need full control over the auction lifecycle. Without open/close actions, members cannot bid and DKP is never spent.

**Independent Test**: Can be tested by an officer opening an auction, placing bids as two members, then closing the auction and confirming the winner's balance decreased by their bid and the item is recorded as awarded.

**Acceptance Scenarios**:

1. **Given** an officer selects an item from the Item Catalog, **When** they open an auction, **Then** the auction appears in the active list and members can place bids.
2. **Given** an open auction has a clear highest bidder, **When** an officer closes the auction, **Then** all bids are revealed, the highest bidder is highlighted as the suggested winner, and the officer confirms before the auction is finalised.
3. **Given** an open auction has a tie (two or more equal top bids), **When** an officer closes the auction, **Then** the tied bidders are clearly flagged, the officer manually selects the winner (after in-game lots), and that member's DKP is deducted by their bid amount.
4. **Given** an open auction has no bids, **When** an officer closes it, **Then** the auction is marked closed with no DKP deducted and no winner recorded.
5. **Given** an auction is closed, **When** any user views it, **Then** the winning bid, winner, and close date are displayed. Bid/close controls are no longer shown.

---

### User Story 5 - Manually Adjust DKP (Priority: P2)

An officer manually adjusts a member's DKP balance up or down, with a required reason string. All adjustments are logged with the officer's identity and timestamp.

**Why this priority**: Manual corrections are infrequent but necessary for correcting data entry errors and edge-case rulings. The audit trail prevents disputes.

**Independent Test**: Can be tested by an officer applying a positive and a negative adjustment to a member, then verifying the balance reflects both and the history shows each adjustment with reason and officer name.

**Acceptance Scenarios**:

1. **Given** an officer enters an adjustment amount and reason, **When** they submit the adjustment, **Then** the member's balance changes accordingly and the adjustment appears in their DKP history with the reason and officer name.
2. **Given** an officer submits an adjustment with no reason, **When** the form is submitted, **Then** submission is blocked until a reason is provided.
3. **Given** a negative adjustment would reduce a member's balance below zero, **When** submitted, **Then** the adjustment is rejected with a message showing the member's current balance and the maximum deductible amount.

---

### User Story 6 - View Full DKP History (Priority: P2)

Any logged-in user can view the complete DKP transaction log for any member: every earn event, bid win, and manual adjustment, in chronological order with running balance.

**Why this priority**: The audit trail is critical for maintaining member trust and resolving disputes. It is read-only and does not require new data models beyond what earlier stories create.

**Independent Test**: Can be tested by seeding a mix of earn events, bid wins, and adjustments for a member, then confirming the history lists them all with accurate running balances.

**Acceptance Scenarios**:

1. **Given** a member has a mix of earn, spend, and adjustment records, **When** a user views their DKP history, **Then** all entries appear in chronological order with a running balance column.
2. **Given** a large history, **When** the history page loads, **Then** entries are paginated or virtualized so the page remains responsive.

---

### User Story 7 - Record Earn Events via Companion Addon (Priority: P3)

The companion addon POSTs attendance data after detecting in-game event messages, awarding DKP to the characters present without requiring officer UI interaction.

**Why this priority**: Reduces manual officer data entry but is not required for the feature to function — officers can always record events manually.

**Independent Test**: Can be tested by POSTing an earn event payload with a valid API key and attendee list, then confirming each member's balance increases and a history entry is created.

**Acceptance Scenarios**:

1. **Given** a valid LS API key and a list of character names with an event type and point value, **When** a POST is made to the earn event endpoint, **Then** DKP is awarded to each identified member and earn records are created.
2. **Given** an invalid or missing API key, **When** a POST is made to any write endpoint, **Then** a 401 response is returned and no DKP is changed.
3. **Given** a character name in the payload does not match any registered member, **When** the POST is processed, **Then** that character is skipped and the response lists which characters were not found.

---

### Edge Cases

- DKP balances cannot go below zero, so a zero-balance member can view auctions but any bid will be rejected by the balance validation check (FR-005b).
- If two members submit identical bid amounts (a tie), both cast lots in-game (/random) and the officer manually marks the winner when closing the auction. The officer must be able to select any bidder as the winner regardless of bid rank.
- An auction for an item that has been removed from the Item Catalog: the auction record retains a snapshot of the item name and requirements at the time it was opened.
- Deactivated members: their DKP history is preserved. [NEEDS CLARIFICATION: can a deactivated member still appear as an auction winner in historical records?] Yes — historical records are immutable.
- If an earn event is submitted via the addon with a duplicate idempotency key, the second POST is rejected with a 409 response and no DKP is re-awarded.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST display the current DKP balance for each member, sourced from the sum of all earn, spend, and adjustment records.
- **FR-002**: System MUST allow officers to create earn events specifying: a description label (free text), point value, date, and one or more attending member characters. DKP is a single universal pool — event type does not restrict how or where points are spent.
- **FR-003**: System MUST award DKP to all attendees of an earn event atomically — either all balances update or none do.
- **FR-004**: System MUST allow officers to open an auction for any item in the Item Catalog. The officer who opens the auction is the designated running officer for that auction.
- **FR-004a**: System MUST restrict visibility of bids on an open auction to only the running officer. All other users — including other officers and members who have placed bids — can only see their own bid. This allows officers to participate as bidders fairly.
- **FR-004b**: System MUST restrict the close/resolve action to the running officer of each auction.
- **FR-005**: System MUST validate bids against three conditions before accepting: (a) bid ≥ item's minimum bid, (b) bidder's current DKP balance ≥ bid amount, (c) at least one of the bidder's active characters meets the item's job and level requirements.
- **FR-006**: System MUST allow officers to close an auction by reviewing all revealed bids, confirming or manually overriding the winner selection, then deducting the winning bid from the selected winner's balance and recording the item as awarded. Manual override is required when bids are tied (in-game lots determine the winner).
- **FR-007**: System MUST allow officers to manually adjust any member's DKP balance with a required reason string; adjustment is logged with officer identity and timestamp. A negative adjustment that would reduce the balance below zero MUST be rejected.
- **FR-008**: System MUST expose a complete, chronological DKP history per member including: event type, point delta, running balance, date, and associated event/auction/adjustment reference.
- **FR-009**: System MUST expose POST endpoints for earn events, authenticated via shared LS API key.
- **FR-010**: System MUST reject duplicate addon earn event payloads via idempotency key with a 409 response.
- **FR-011**: System MUST enforce that only officers can create earn events, open/close auctions, and apply manual adjustments.
- **FR-012**: System MUST enforce that members can only bid on open auctions using their own account.
- **FR-013**: System MUST prevent members from bidding on auctions for items none of their active characters can equip.
- **FR-014**: System MUST store an item name snapshot on each auction record so historical data is not broken if an item is renamed or removed from the catalog.

### Key Entities

- **DKPEarnEvent**: A recorded earn event. Attributes: description label (free text, e.g. "Sky run", "Kirin kill"), point value, date, officer who recorded it, list of attending characters. Relationships: has many DKPEarnRecords. DKP is a single universal pool — no per-event restrictions on spending.
- **DKPEarnRecord**: A single member's DKP award from one event. Attributes: member reference, earn event reference, point delta, date. This is the ledger row for an earn.
- **Auction**: An item auction. Attributes: Item Catalog reference (+ item name snapshot), minimum bid (nullable), open date, close date (nullable), status (open/closed), winner member reference (nullable), winning bid amount (nullable).
- **Bid**: A single blind bid on an auction. Attributes: auction reference, member reference, bid amount, timestamp. One bid per member per auction; bids are final and not visible to other members while the auction is open.
- **DKPAdjustment**: A manual officer adjustment. Attributes: member reference, point delta (positive or negative), reason, officer reference, timestamp.
- **DKPBalance**: [Derived, not stored] — computed as sum of all DKPEarnRecords + DKPAdjustments − sum of winning Bid amounts for that member.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: An officer can record a DKP earn event with a full attendee list in under 60 seconds.
- **SC-002**: A member can browse active auctions, review eligibility, and place a bid in under 30 seconds.
- **SC-003**: After an auction is closed, the winner's balance is updated and the result is visible to all users immediately.
- **SC-004**: The DKP history for any member loads completely (all-time records) with no perceptible delay for up to 500 transactions.
- **SC-005**: The addon can POST a DKP earn event and receive a success or descriptive error response without requiring any UI interaction.

## Assumptions

- DKP balances are per-member (not per-character). A member's balance reflects all their characters combined.
- Bids are placed at the member level, but eligibility is checked against any of the member's active characters.
- Items available for auction come from the existing Item Catalog. Auction cannot be created for items not in the catalog.
- Earn events use a free-text description label (e.g. "Sky run", "Kirin kill"). There are no system-defined event type categories. DKP earned from any event can be spent on any auction.
- The addon uses the shared LS API key (same model as Member Info addon integration).
- There is no real-time push to members when a new auction opens or closes — members refresh the auction list to see updates.
- DKP balances cannot go below zero. Manual adjustments that would result in a negative balance are blocked at the API level.
- Bidding is blind — only the running officer (the one who opened the auction) can see all bids while it is open. All other participants, including officers who have placed their own bid, can only see their own bid. Each participant may bid once; bids are final.
- Officers are identified by their logged-in Discord identity (same role model as Member Info).
