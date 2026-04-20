# Member Info — Remaining Tasks

Breakdown of pending tasks from `member-info.md` (tasks 10–25), grouped by dependency and logical order.

---

## Batch A — Quick wins (no new models needed)

### ✅ A1: Remove orphan component (Task 25)
Deleted `character-profile-detail` component and removed all references from `dashboard.component`.

### A2: Crafting levels in UI (Task 20)
Data already exists in the model and API. Add a crafting levels section to `character-detail-panel`.

### ✅ A3: Mark gear need as obtained (Task 17)
Added `POST /api/characters/workspace/{characterId}/wishlist/{itemId}/obtained` endpoint. Adds "Mark obtained" button per wishlist item in the UI, visible only for the selected character's assignments. Removes the item from active needs locally on success.

---

## Batch B — Content access & derivation

### ✅ B0: Dynamis clear data entry (Milestone 1 item 1)
Added `PUT /api/characters/workspace/{characterId}/dynamis` endpoint. Added Dynamis Clears section to `character-detail-panel` with checkboxes grouped by tier (Cities / Icelands / Dreamlands / Tavnazia). Icelands disabled until all 4 cities are checked; Tavnazia disabled until all 3 Dreamlands are checked. Auto-saves with 700ms debounce. Dynamis clears now included in `GET /workspace/{characterId}` response.

### B1: Content access derivation logic (Task 12)
Add a static helper that reads mission and Dynamis fields from `Character` and returns Sky/Sea/Limbus/Dynamis access flags. No new DB columns — pure logic. Rules:
- **Dynamis (cities)**: Nation mission 5-2 complete
- **Dynamis (Icelands)**: All city zones cleared (Windurst, Bastok, San d'Oria, Jeuno)
- **Dynamis (Dreamlands)**: CoP 3-5 complete
- **Dynamis (Tavnazia)**: All Dreamland zones cleared (Valkurm, Buburimu, Qufim)
- **Sky**: ZM mission 13 complete
- **Sea**: CoP 7-5 complete
- **Limbus**: CoP 7-5 complete

### B2: HorizonXI staleness indicator (Task 21)
`Character.LastSyncedAt` already exists. Show a warning badge in the UI when data is older than a threshold (e.g. 24 hours).

---

## Batch C — Roster

### C1: Member roster API (Task 10)
New `GET /api/members` endpoint returning all active users with their characters, level-75 jobs, content access flags (from B1), and a DKP placeholder (0 until the DKP feature lands).

### C2: Roster page + search/filter (Tasks 10, 11)
New `/app/roster` Angular page. Table rows showing: character name, job preferences, level-75 jobs, content access icons. Search/filter by character name or Discord name.

### C3: DKP total stub (Task 22)
Show DKP as "—" on the roster and character workspace until the DKP feature is built. Wire up the column placeholder so the roster isn't blocked.

---

## Batch D — Permissions & grouping

### D1: Officer write access (Task 15)
Add officer role check to `CharactersController`. If `user.Role == Officer`, allow reading/writing any character's data. Otherwise enforce ownership.

### D2: Gear needs grouped by content type (Task 18)
Group wishlist items by content type (Dynamis/Sky/Sea/Limbus/HENM/Misc) in the character detail panel. Items already have `ItemSource` links.

---

## Batch E — Lifecycle management

### E1: Retire character + link replacement (Task 14)
`POST /api/characters/{id}/retire` sets `Character.IsActive = false`. Reuse the existing character search/link flow to add a replacement. Retired characters preserve gear needs history.

### E2: Member deactivation (Task 23)
`POST /api/members/{id}/deactivate` sets `User.IsActive = false`. Roster hides inactive members by default; officer toggle surfaces them with a visual indicator.

### E3: Unique character name constraint (Task 24)
Add a unique index on `Character.Name` via migration. The API already validates against HorizonXI but there is no DB-level guard.

---

## Batch F — Addon & advanced

### F1: Dynamis clear recording API (Task 13)
New endpoint authenticated by shared LS API key. `POST` a Dynamis zone clear for a character by name. Recalculates content access after recording.

### F2: Officer manual member registration (Task 16)
Extend the registration flow so an officer can `POST` a Discord ID + characters, bypassing the guild membership check.

### F3: 1-per-player item restriction (Task 19)
Add `IsOnePerPlayer` flag to `Item`. At API level: if any character on the member profile has the item marked obtained, block adding it again on any other character.

---

## Suggested implementation order

```
A2 → B1 → B2 → C1 → C2 → C3 → D1 → D2 → E1 → E2 → E3 → F1 → F2 → F3
```

Next up: **B1** (content access derivation) feeds into the roster (C1/C2) so tackle it before building the roster page.
