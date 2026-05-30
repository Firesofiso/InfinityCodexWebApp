# Plan: Import DKP Balances and Placeholder Users

## Context

The linkshell has existing DKP balances tracked in `ImportDocuments/old-dkp.txt`
(tab-separated: Name, DKP Total). The database is empty — no users have registered yet.

Goal: seed the DB with one placeholder User + Character per member (with their DKP
balance), so that when members register via Discord and enter their character name,
their DKP history is already there waiting for them.

No session-by-session history is needed; the spreadsheet is the historical record.

---

## Part 0 — Add IsImported to User model + migration

**File:** `Data/Model/User.cs` — add one property:
```csharp
public bool IsImported { get; set; }
```

**Migration:** add a new EF migration (`AddIsImportedToUser`) that adds the column:
```sql
ALTER TABLE Users ADD COLUMN IsImported INTEGER NOT NULL DEFAULT 0;
```

No `OnModelCreating` change needed (simple bool with default false).

---

## Part 1 — Import script

**File:** `ImportDocuments/import_dkp_balances.py`

Follow the exact pattern of the existing `ImportDocuments/DynamisImport/import_dynamis_csvs.py`:
- Read `old-dkp.txt` with `\t` delimiter (skip header row)
- Connect to `app.db` (two directories up from the script location, same as existing script)
- For each row (Name, DKP Total):
  - Generate a random UUID as a placeholder `DiscordId` (keeps the field valid and unique
    without encoding anything meaningful into it)
  - INSERT OR IGNORE into `Users`:
    - `DiscordId = <uuid4>`
    - `DisplayName = <name>`
    - `Role = "member"`
    - `IsActive = 0`
    - `IsRegistrationComplete = 0`
    - `IsImported = 1`
    - `DkpBalance = <dkp_total>`
    - `CreatedAt = CURRENT_TIMESTAMP`
  - INSERT OR IGNORE into `Characters` (using the user id from the insert above):
    - `Name = <name>`
    - `OwnerUserId = <just-inserted user id>`
    - `IsActive = 0`
    - `DataSource = "import"`
    - No other fields set (mission progress, job levels all NULL)
- Print a summary of rows inserted vs skipped (idempotent — safe to re-run)

**Idempotency:** match on `DisplayName + IsImported = 1` before inserting to avoid
duplicates on re-run (since DiscordId is a UUID it changes each run). Use a
`SELECT Id FROM Users WHERE DisplayName = ? AND IsImported = 1` check first.

---

## Part 2 — Character claiming in CompleteRegistration

**File:** `Controllers/AuthController.cs` — `CompleteRegistration` method (line 329)

The existing flow (lines ~372–430) processes `normalizedCharacterNames` by:
1. Loading existing horizon-api characters owned by the user
2. Activating/deactivating them based on the submitted list
3. Creating new characters for names not yet in the DB

**Change:** Replace the character-processing section with one pass that handles three cases per name:

```
foreach characterName in normalizedCharacterNames:
    case A — already owned by current user (DataSource = "horizon-api"):
        activate it (existing behaviour)

    case B — claimable placeholder found (Character where Name matches case-insensitively
              AND owner User.IsImported == true):
        - character.OwnerUserId = user.Id
        - character.IsActive = true
        - character.DataSource = HorizonCharacterDataSource   // "horizon-api"
        - character.LastSyncedAt = DateTime.UtcNow
        - placeholderDkp = load owner User.DkpBalance
        - user.DkpBalance += placeholderDkp
        - create DkpTransaction:
            UserId = user.Id, CharacterId = character.Id,
            SourceType = "manual_adjustment",
            Reason = "Claimed historical DKP balance on registration",
            Amount = placeholderDkp,
            BalanceAfter = user.DkpBalance,
            CreatedByUserId = user.Id,
            CreatedAt = DateTime.UtcNow

    case C — no match anywhere:
        create new Character (existing behaviour)
```

To find claimable placeholders efficiently, load them in one query before the loop:
```csharp
var claimableCandidates = await _dbContext.Characters
    .Where(c => normalizedNameSet.Contains(c.Name) && c.DataSource == "import")
    .Join(_dbContext.Users, c => c.OwnerUserId, u => u.Id, (c, u) => new { Character = c, Owner = u })
    .Where(x => x.Owner.IsImported)
    .ToListAsync();
```

**No new endpoint needed.** The existing `POST /auth/registration/complete` handles this.

---

## Critical files

| File | Change |
|---|---|
| `Data/Model/User.cs` | Add `IsImported` bool property |
| `Migrations/<timestamp>_AddIsImportedToUser.cs` | New EF migration |
| `ImportDocuments/import_dkp_balances.py` | New — create this script |
| `Controllers/AuthController.cs` | Modify `CompleteRegistration` (lines ~372–430) |

Existing utilities to reuse from `import_dynamis_csvs.py`:
- `null_if_blank`, `int_or_default` helper functions
- SQLite connection pattern (`sqlite3.connect(DB_PATH)`)
- `WHERE NOT EXISTS` idempotency idiom

Note: the `IsImported` column must be added via EF migration before the Python script
is run, otherwise the INSERT will fail (column not present in schema).

---

## Verification

1. Apply the EF migration (`dotnet ef database update`).
2. Run `python ImportDocuments/import_dkp_balances.py` — confirm ~40 users inserted on first run, 0 inserted on re-run.
3. Inspect `app.db` — confirm `Users` rows have correct `DkpBalance`, `IsImported = 1`, `IsActive = 0`.
4. Register a test user via Discord whose character name matches a placeholder → confirm after `CompleteRegistration`:
   - Character appears in the user's account
   - `User.DkpBalance` matches the imported value
   - A `DkpTransaction` row exists with `Reason = "Claimed historical DKP balance on registration"`
5. Re-run the import script — no duplicates created.
