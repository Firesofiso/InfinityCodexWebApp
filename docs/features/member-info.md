# Feature Specification: Member Info

**Feature Branch**: `member-info`
**Created**: 2026-04-19
**Status**: Draft
**Input**: User description: "member info, lets create a feature document for member information."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - View Member Profile (Priority: P1)

An officer or member opens a player's profile page and sees all relevant information in one place: mission progress, job levels, crafting levels, and gear needs across all content.

**Why this priority**: This is the core read path for the feature. Everything else builds on being able to view a member's data. It replaces the most-used lookup workflow from the spreadsheet.

**Independent Test**: Can be tested by seeding a member record and verifying all profile sections render correctly with accurate data.

**Acceptance Scenarios**:

1. **Given** a member record exists, **When** a user navigates to that member's profile, **Then** the page displays mission progress, job levels, crafting levels, and gear needs grouped by content type.
2. **Given** a member has no gear needs recorded, **When** their profile is viewed, **Then** the gear needs section shows an empty state rather than an error.

---

### User Story 2 - Edit Member Info (Priority: P2)

A member updates their own mission progress via the webapp UI. Officers can update any member's mission progress. Job and craft levels are not manually entered; they are fetched from the HorizonXI website.

**Why this priority**: Mission progress still requires manual tracking, but job/craft data entry is eliminated by the HorizonXI integration.

**Independent Test**: Can be tested by updating a member's mission progress and confirming the updated value persists and displays correctly on the profile.

**Acceptance Scenarios**:

1. **Given** an officer is viewing a member profile, **When** they update mission progress and save, **Then** the updated value is persisted and visible on the profile immediately.
2. **Given** a member profile is viewed, **When** the page loads, **Then** job levels and craft levels are displayed from the latest HorizonXI data for that character.
3. **Given** the HorizonXI site is unreachable, **When** a member profile is viewed, **Then** job and craft levels show the last cached values alongside a staleness indicator.

---

### User Story 3 - Manage Gear Needs / Wishlist (Priority: P2)

Gear needs are tracked per character (not per member profile). A member manages their own character's gear needs; officers can manage any member's gear needs. Some items are flagged as "1 per player" — meaning a player may only receive that item once across all their characters until every eligible player has received one.

**Why this priority**: Gear needs are the most operationally useful data — officers use this during loot distribution to quickly see who needs what.

**Independent Test**: Can be tested by adding a gear need to a specific character, then confirming it appears under that character filtered correctly by content type.

**Acceptance Scenarios**:

1. **Given** a character profile is open, **When** an officer adds a gear need item under "Dynamis", **Then** the item appears in the Dynamis gear needs list for that character.
2. **Given** a character has a gear need recorded, **When** an officer marks it as obtained, **Then** it is removed from the active needs list for that character.
3. **Given** a character has gear needs across multiple content types, **When** viewing the profile, **Then** needs are grouped and filterable by content type.
4. **Given** an item is flagged as "1 per player", **When** viewing that item's needs list, **Then** the system shows whether any character on a player's profile has already received it.

---

### User Story 4 - Register as a Member (Priority: P1)

A player self-registers via Discord OAuth — the system checks they are in the LS Discord guild. They then link up to 3 HorizonXI characters by searching/selecting from the HorizonXI site. The UI enforces the 3-character limit and only surfaces valid character names, so these are not UI error states. Officers can manually register anyone, bypassing the Discord guild check.

**Why this priority**: Without the ability to add members, no other member info functionality is usable.

**Independent Test**: Can be tested by logging in via Discord as a guild member, selecting a HorizonXI character, and verifying the member appears in the roster with job/craft data populated.

**Acceptance Scenarios**:

1. **Given** a user logs in via Discord OAuth and is in the LS Discord guild, **When** they complete registration and link a HorizonXI character, **Then** their profile is created and job/craft levels are fetched from HorizonXI.
2. **Given** a user logs in via Discord OAuth but is not in the LS Discord guild, **When** they attempt to register, **Then** registration is blocked with a clear message.
3. **Given** an officer manually registers a member, **When** they submit a Discord identifier and at least one HorizonXI character, **Then** the profile is created regardless of guild membership status.
4. **Given** a HorizonXI character is already linked to another member profile, **When** submitted via UI or API, **Then** an error is returned and no duplicate link is created.
5. **Given** a direct API POST attempts to link more than 3 characters or a non-existent character name, **When** the request is processed, **Then** a 400 response is returned with a descriptive message.

---

### User Story 5 - Member List / Roster View (Priority: P1)

Any logged-in user can view a filterable/searchable roster of all LS members. Each row shows the member's main character name, job preferences, all level 75 jobs, and their current DKP total.

**Why this priority**: The roster is the entry point to all member profiles and is used during events to quickly assess who is available and what they play.

**Independent Test**: Can be tested by seeding multiple members and verifying each row displays main character name, job preferences, 75-capped jobs, and DKP total.

**Acceptance Scenarios**:

1. **Given** multiple member records exist, **When** a user visits the roster, **Then** each row displays the member's main character name, job preferences, level 75 jobs, DKP total, and a content access indicator per content type.
2. **Given** a user types a name into the search field, **When** results update, **Then** only members whose character name matches are shown.
3. **Given** a member has no level 75 jobs yet, **When** their roster row is displayed, **Then** the 75 jobs column shows an empty state rather than an error.
6. **Given** a member has been deactivated, **When** an officer views the roster without the "show inactive" toggle enabled, **Then** the deactivated member does not appear.
7. **Given** an officer enables the "show inactive" toggle, **When** the roster refreshes, **Then** deactivated members appear with a visual indicator distinguishing them from active members.
4. **Given** a member has completed RoZ mission 13, **When** their roster row is displayed, **Then** the Sky access indicator is shown as unlocked.
5. **Given** a member has completed CoP 7-5, **When** their roster row is displayed, **Then** both Sea and Limbus access indicators are shown as unlocked.

---

### User Story 6 - Retire a Character and Link a Replacement (Priority: P2)

A member retires one of their linked characters (e.g. the character was deleted on HorizonXI) and links a new one using the same character search available during registration. Officers can do this on behalf of any member. The retired character's history is preserved but removed from the active character list.

**Why this priority**: Character turnover will happen regularly. Members need to manage this themselves without requiring officer intervention, though officers can also action it.

**Independent Test**: Can be tested by a member retiring one of their characters and linking a new one, then verifying the new character appears on their profile and the retired one no longer shows as active.

**Acceptance Scenarios**:

1. **Given** a member views their own profile, **When** they mark a character as retired, **Then** it is removed from their active character list but its gear needs history is preserved.
2. **Given** an officer views any member's profile, **When** they mark a character as retired, **Then** the same outcome applies — the character is deactivated and history preserved.
3. **Given** a member or officer links a new HorizonXI character after a retirement, **When** the character search confirms the name exists, **Then** the new character appears on the profile and job/craft levels are fetched from HorizonXI.
4. **Given** the 3-character limit is based on active characters only, **When** a member has 3 active characters, **Then** retiring one frees a slot for a new character to be linked.

---

### User Story 7 - Record Dynamis Clears via API (Priority: P3)

The companion addon POSTs Dynamis zone clears and mission progress updates using the shared LS API key, reducing manual data entry for officers.

**Why this priority**: Addon integration reduces manual data entry but is not required for the feature to be usable.

**Independent Test**: Can be tested by POSTing a Dynamis zone clear for an existing member and confirming the clear is recorded and content access is updated accordingly.

**Acceptance Scenarios**:

1. **Given** a valid LS API key and an existing member character name, **When** a POST is made to record a Dynamis zone clear, **Then** the clear is recorded and the member's content access is recalculated.
2. **Given** an invalid or missing API key, **When** a POST is made to any write endpoint, **Then** a 401 response is returned and no data is changed.

---

### Edge Cases

- Deactivated members are hidden from the roster by default. Officers can toggle an "show inactive" filter to display them. Their data (DKP history, split records, gear needs) is fully preserved.
- HorizonXI does not support character renames. If a player deletes and recreates a character, the old character is retired (marked inactive) by the member via the UI, and a new character is linked using the same search available during registration. Retired characters retain their gear needs history but no longer appear as active.
- Concurrent edits by two officers are handled with last-write-wins. No conflict detection or locking is implemented.
- Job level max is 75 (HorizonXI cap). Job levels are sourced from HorizonXI — "not set" is represented by absence of data from the external source.
- If HorizonXI is unreachable, job and craft level sections display the last cached values with a staleness indicator. No error state is shown.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST allow members to self-register via Discord OAuth, subject to LS Discord guild membership verification.
- **FR-002**: System MUST allow officers to manually register any member by Discord identifier, bypassing the guild membership check.
- **FR-003**: System MUST allow up to 3 active HorizonXI characters per member profile. Retired characters do not count toward this limit.
- **FR-004**: System MUST prevent the same HorizonXI character name from being linked to more than one member profile.
- **FR-005**: System MUST fetch job levels and crafting levels from HorizonXI by character name — no manual entry.
- **FR-006**: System MUST store mission progress per member across: CoP (implemented), ZM (implemented), ToAU (planned), Nation Missions — San d'Oria / Bastok / Windurst (in scope), Epilogue (in scope). WoTG and others are out of scope for now.
- **FR-006a**: System MUST store Dynamis zone clears per member to support content access inference.
- **FR-006b**: System MUST derive content access from mission progress and Dynamis clears using these rules:
  - **Dynamis (cities)**: Nation mission 5-2 complete
  - **Dynamis (Icelands)**: All city zones cleared (Windurst, Bastok, San d'Oria, Jeuno)
  - **Dynamis (Dreamlands)**: CoP 3-5 complete
  - **Dynamis (Tavnazia)**: All Dreamland zones cleared (Valkurm, Buburimu, Qufim)
  - **Sky**: ZM mission 13 complete
  - **Sea**: CoP 7-5 complete
  - **Limbus**: CoP 7-5 complete
- **FR-007**: System MUST store gear needs per character (not per member profile) as references to Item Catalog entries, categorised by content type: Dynamis, Limbus, Sky, Sea, HENM, misc NMs.
- **FR-007a**: For items flagged "1 per player" in the Item Catalog, the system MUST treat the restriction at the member profile level — if any character on a profile has obtained the item, the player is considered satisfied.
- **FR-008**: System MUST allow gear needs to be marked as obtained.
- **FR-009**: System MUST enforce that members can only write their own profile data. Officers can write any member's data and perform privileged actions.
- **FR-010**: System MUST expose POST endpoints for member data updates, authenticated via shared LS API key.
- **FR-011**: System MUST validate at the API level that character names exist on HorizonXI and that the 3-character limit is not exceeded.
- **FR-012**: System MUST allow members to be deactivated without permanently deleting their history.
- **FR-013**: System MUST allow filtering and searching the member roster by Discord name or character name.

### Key Entities

- **Member**: A linkshell member identified by Discord name/ID. Key attributes: Discord name, Discord ID, join date, active status. Has up to 3 linked Characters.
- **Character**: A HorizonXI character linked to a Member. Key attributes: character name (unique, from HorizonXI), member reference. Job and craft levels are read from HorizonXI by character name — not stored locally beyond caching.
- **MissionProgress**: A member's progress through each mission storyline. Attributes: member, mission line (CoP/ZM/ToAU/Nation/Epilogue), current stage/rank.
- **DynamisClear**: A record that a member has cleared a specific Dynamis zone. Attributes: member, zone name, date cleared. Used to infer Icelands and Dreamlands/Tavnazia access.
- **GearNeed**: A single gear item a character needs. Attributes: character reference, Item Catalog reference, content type (Dynamis/Limbus/Sky/Sea/HENM/Misc), obtained flag, date added, date obtained. Item Catalog provides name, stats, image, and wiki link. Some items carry a "1 per player" flag — enforced at the member profile level across all linked characters.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: An officer can create a new member and populate their full profile (jobs, missions, crafts, gear needs) without leaving the webapp.
- **SC-002**: A gear need can be added and marked as obtained in under 30 seconds from the member profile page.
- **SC-003**: The member roster loads and is searchable with no perceptible delay for a roster of up to 100 members.
- **SC-004**: The addon can POST a Dynamis zone clear or mission progress update and receive a success response without requiring any webapp UI interaction.
- **SC-005**: Zero duplicate member records exist after normal officer usage (enforced by the system, not convention).

## Assumptions

- The app targets HorizonXI, a 75-cap private server. Job level max is 75.
- Members are identified by Discord identity; each profile supports up to 3 linked HorizonXI characters.
- Self-registration requires Discord OAuth and LS guild membership. Officers can bypass guild check for manual registration.
- Job and craft levels are sourced from the HorizonXI website — not entered manually.
- Mission lines in scope: CoP, ZM (both implemented), ToAU (planned), Nation Missions (Sandy/Bastok/Windy), Epilogue. WoTG and others are out of scope for now.
- All crafting skills are tracked (Smithing, Goldsmithing, Clothcraft, Leathercraft, Bonecraft, Woodworking, Alchemy, Cooking, Fishing).
- Gear needs are linked to Item Catalog entries. The Item Catalog stores item name, stats, image, and a link to the official FFXI wiki.
- Members can edit their own profile data (mission progress, gear needs, linked characters). Officers have elevated access to edit any member's data and perform privileged actions (awarding DKP, calculating splits, manually registering members).
- Member deletion is soft (deactivate, not destroy) to preserve historical DKP and split records.
