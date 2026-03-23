# Agent Handoff Guide

This file is meant to help future Codex agents or new threads contribute without spending a full session rediscovering the project.

## Repo Shape

Top-level backend:

- ASP.NET Core Web API
- EF Core with SQLite
- Discord OAuth and cookie auth

Nested frontend:

- [infinity-webapp](E:\Repos\InfinityCodexWebApp\infinity-webapp)
- Angular 21
- Tailwind CSS
- FullCalendar

## Main Files To Read First

If a future thread needs the fastest path into context, start here:

- [Program.cs](E:\Repos\InfinityCodexWebApp\Program.cs)
- [Controllers/AuthController.cs](E:\Repos\InfinityCodexWebApp\Controllers\AuthController.cs)
- [Data/ApplicationDbContext.cs](E:\Repos\InfinityCodexWebApp\Data\ApplicationDbContext.cs)
- [infinity-webapp/src/app/home/home.component.ts](E:\Repos\InfinityCodexWebApp\infinity-webapp\src\app\home\home.component.ts)
- [infinity-webapp/src/services/auth.service.ts](E:\Repos\InfinityCodexWebApp\infinity-webapp\src\services\auth.service.ts)
- [docs/CURRENT_STATE.md](E:\Repos\InfinityCodexWebApp\docs\CURRENT_STATE.md)

## Current Truths Agents Should Assume

- The app compiles on both backend and frontend.
- The domain model is ahead of the API surface.
- The frontend UI is ahead of the actual connected product flows.
- Authentication is the only clearly implemented backend feature.
- A lot of important functionality is implied by the schema but not yet exposed.

## Things Agents Should Be Careful About

- Do not assume the frontend is tracked cleanly in git. The current worktree shows the Angular app as untracked.
- Do not revert moved model files or repo structure changes unless the user asks.
- Watch for hard-coded environment values before adding more integrations.
- Treat `appsettings.json` secrets as sensitive and avoid duplicating them in new files or docs.

## Best Next Workstreams For Separate Threads

These are clean parallelizable slices for agents:

### Thread A: Environment And Security Cleanup

Goals:

- move OAuth secrets out of source control
- centralize frontend API base URLs
- restore safe return URL validation

Expected files:

- [appsettings.json](E:\Repos\InfinityCodexWebApp\appsettings.json)
- [appsettings.Development.json](E:\Repos\InfinityCodexWebApp\appsettings.Development.json)
- [Controllers/AuthController.cs](E:\Repos\InfinityCodexWebApp\Controllers\AuthController.cs)
- frontend environment or config files under [infinity-webapp/src](E:\Repos\InfinityCodexWebApp\infinity-webapp\src)

### Thread B: Backend Domain APIs

Goals:

- add CRUD endpoints for characters
- add endpoints for job levels and item needs
- define DTOs instead of returning EF entities directly

Expected files:

- new controllers under [Controllers](E:\Repos\InfinityCodexWebApp\Controllers)
- DTOs and mapping code
- possible service layer additions

### Thread C: Frontend Data Wiring

Goals:

- replace hard-coded character data with API-backed data
- create Angular services for characters and dashboard data
- route the home page through real app state

Expected files:

- [infinity-webapp/src/app/home](E:\Repos\InfinityCodexWebApp\infinity-webapp\src\app\home)
- [infinity-webapp/src/services](E:\Repos\InfinityCodexWebApp\infinity-webapp\src\services)
- new UI components as needed

### Thread D: Product Design And Feature Framing

Goals:

- define the first usable dashboard workflow
- decide what the calendar is for
- turn the schema into user-facing flows

Use:

- [docs/FEATURE_BRAINSTORM.md](E:\Repos\InfinityCodexWebApp\docs\FEATURE_BRAINSTORM.md)

## Definition Of Done For Future Work

Future feature threads should try to leave behind:

- updated docs when the project shape changes
- real configuration instead of hard-coded URLs
- a build pass after code changes
- a clear note separating prototype UI from real user-ready functionality
