# File Map

Quick reference for agents and developers. Organised by concern — use this before reaching for grep.

---

## Backend (.NET / C#)

### Controllers

| File | Route prefix | Endpoints |
|---|---|---|
| `Controllers/AuthController.cs` | `/auth/discord`, `/auth/` | Discord OAuth login/callback, session, registration context & complete, logout |
| `Controllers/UsersController.cs` | `/api/users` | Roster, access overview, role update, impersonation start/stop, dev fake-players |
| `Controllers/CharactersController.cs` | `/api/characters` | Search, workspace list, set-main, get/update character detail, missions, dynamis clears, wishlist |
| `Controllers/DkpController.cs` | `/api/dkp` | Character transaction history, manual DKP adjust, bulk earn event |
| `Controllers/ItemsController.cs` | `/api/items` | List/get items, reference data, create/update item, archive item |

### Data Models (`Data/Model/`)

| File | Table | Notes |
|---|---|---|
| `User.cs` | `Users` | Core user record. `IsActive`, `IsRegistrationComplete`, `IsImported`, `DkpBalance` |
| `Character.cs` | `Characters` | Owned by a User. `DataSource` = `"horizon-api"` (live) or `"import"` (placeholder). `IsMain` flags primary character |
| `DkpTransaction.cs` | `DkpTransactions` | Ledger rows. `SourceType` constants in `DkpTransactionSourceTypes` (same file) |
| `DkpEarnEvent.cs` | `DkpEarnEvents` | Bulk earn events that back DkpTransactions |
| `Item.cs` | `Items` | Catalog item |
| `ItemArmorStats.cs` | `ItemArmorStats` | 1-to-1 with Item |
| `ItemWeaponStats.cs` | `ItemWeaponStats` | 1-to-1 with Item |
| `ItemAccessoryStats.cs` | `ItemAccessoryStats` | 1-to-1 with Item |
| `ItemStatModifier.cs` | `ItemStatModifiers` | Many per Item |
| `ItemAllowedJob.cs` | `ItemAllowedJobs` | Composite PK (ItemId, JobCode) |
| `ItemSource.cs` | `ItemSources` | Links Items to ContentSources |
| `ContentSource.cs` | `ContentSources` | e.g. "Dynamis-Sandy" |
| `ContentGroup.cs` | `ContentGroups` | e.g. "Dynamis" |
| `CharacterItem.cs` | `CharacterItems` | Items a character has obtained |
| `CharacterItemNeed.cs` | `CharacterItemNeeds` | Items on a character's wishlist |
| `UserPreferredJob.cs` | `UserPreferredJobs` | Preferred job codes per user |

### Other Backend Files

| File | Purpose |
|---|---|
| `Data/ApplicationDbContext.cs` | EF Core context — DbSets, indexes, FK config |
| `Authorization/RoleAuthorization.cs` | Role constants, permission sets, `RolePermissions.GetPermissionsForRole()` |
| `Migrations/` | EF migration history. Latest: `20260530164733_AddIsImportedToUser` |
| `Program.cs` | App startup, service registration, middleware pipeline |

---

## Frontend (Angular + Tailwind)

Root: `infinity-webapp/src/`

### Routes (`app/app.routes.ts`)

| Path | Component file |
|---|---|
| `/` | `app/home/home.component.ts` |
| `/register` | `app/containers/register/register.component.ts` |
| `/app/characters` | `app/containers/character-workspace/character-workspace.component.ts` |
| `/app/characters/:userId` | same — loads another user's workspace |
| `/app/roster` | `app/containers/roster/roster.component.ts` |
| `/app/access` | `app/containers/access-management/access-management.component.ts` |
| `/app/catalog` | `app/containers/item-catalog/item-catalog.component.ts` |
| `/app/dashboard` | `app/containers/dashboard/dashboard.component.ts` |

### Services (`services/`)

| File | Responsibility |
|---|---|
| `auth.service.ts` | Session state, current user, permission checks (`canManageDkp()` etc.) |
| `user.service.ts` | Roster, access overview, role update, impersonation, dev tools. Owns `RosterMember`, `RosterRow`, `RosterResponse` interfaces |
| `character-workspace.service.ts` | Character workspace CRUD, missions, dynamis, wishlist, DKP adjust, bulk earn |
| `character-search.service.ts` | Character search (HorizonXI API proxy) |
| `item-catalog.service.ts` | Item catalog list, get, create, update, archive, reference data |

### Shared Components (`app/components/`)

| File | Purpose |
|---|---|
| `master-list/master-list.component.ts` | Reusable sortable/searchable table. Accepts `columns`, `data`, `actions`. Supports `routerLink`, `externalLink`, `format` per column |
| `master-list/master-list.component.html` | Template for the above |

### Layout (`app/layout/`)

| File | Purpose |
|---|---|
| `main-layout.component.ts` | Authenticated shell with sidebar |
| `sidebar.component.ts` | Nav links, current user display |

### Guards / Interceptors

| File | Purpose |
|---|---|
| `app/guards/auth.guard.ts` | Redirects unauthenticated users to `/` |
| `app/guards/registration.guard.ts` | Redirects incomplete registrations |
| `app/interceptors/auth-error.interceptor.ts` | Handles 401/403 globally |

### Page Components

| File | Route | Template |
|---|---|---|
| `app/containers/roster/roster.component.ts` | `/app/roster` | `roster.component.html` |
| `app/containers/character-workspace/character-workspace.component.ts` | `/app/characters` | `character-workspace.component.html` |
| `app/containers/character-workspace/character-detail-panel.component.ts` | (panel within workspace) | `character-detail-panel.component.html` |
| `app/containers/character-workspace/mission-catalog.ts` | (data only — mission list constants) | — |
| `app/containers/register/register.component.ts` | `/register` | `register.component.html` |
| `app/containers/access-management/access-management.component.ts` | `/app/access` | `access-management.component.html` |
| `app/containers/item-catalog/item-catalog.component.ts` | `/app/catalog` | `item-catalog.component.html` |
| `app/containers/character-search/character-search.component.ts` | (modal/panel) | `character-search.component.html` |
| `app/containers/dashboard/dashboard.component.ts` | `/app/dashboard` | `dashboard.component.html` |
| `app/home/home.component.ts` | `/` | `home.component.html` |

---

## Database Scripts (`db/`)

Run `python db/reset.py` from the repo root for a full fresh start.

| File | Purpose |
|---|---|
| `db/reset.py` | **Entrypoint.** Backs up `app.db`, runs `dotnet ef database update`, then runs the import |
| `db/import_dkp_balances.py` | Seeds placeholder Users + Characters from `db/old-dkp.txt`. Idempotent — safe to re-run |
| `db/old-dkp.txt` | Source data: tab-separated Name / DKP Total for ~39 historical members |

## Docs (`docs/`)

| File | Purpose |
|---|---|
| `docs/file-map.md` | This file — codebase navigation reference |
| `docs/deployment.md` | Step-by-step first deployment guide (EC2 + Route 53 + Docker + GitHub Actions) |
| `docs/features/dkp-migration-plan.md` | Design doc for the DKP import and character-claiming registration flow |

## Import Data (`ImportDocuments/`)

| File | Purpose |
|---|---|
| `DynamisImport/import_dynamis_csvs.py` | Seeds item catalog from Dynamis CSVs. Pattern reference for future import scripts |

---

## Key Conventions

- **DataSource values**: `"horizon-api"` = live synced character, `"import"` = placeholder seeded from old-dkp.txt
- **IsImported**: `User.IsImported = true` means the user is an unclaimed placeholder (no Discord login yet)
- **Roster query**: `Users WHERE IsActive = true OR IsImported = true`, must have a character with `IsMain = true`. Imported placeholders appear with no Discord alias until claimed
- **DKP ledger**: every balance change should produce a `DkpTransaction` row in addition to updating `User.DkpBalance`
- **Permissions**: defined in `Authorization/RoleAuthorization.cs`; checked via `authService.can*()` methods on the frontend
