# Current State

This document is a snapshot of what the project currently contains as of 2026-03-21.

## Product Direction Visible In The Code

The app appears to be moving toward a game companion or codex experience centered on:

- Discord-based sign-in
- user-owned characters
- job and level tracking
- item ownership and item need tracking
- content source mapping for items
- a calendar-oriented dashboard view

The current implementation is still early. The backend data model is more developed than the exposed API surface, and the frontend is more of a visual shell than a fully connected application.

## What Is Built Today

### Backend

Implemented and working at the project structure level:

- ASP.NET Core `net8.0` web app bootstrapped in [Program.cs](E:\Repos\InfinityCodexWebApp\Program.cs)
- Entity Framework Core with SQLite configured through `DefaultConnection`
- Swagger enabled in development
- cookie authentication configured for a Discord login flow
- CORS configured for Angular dev at `http://localhost:4200`
- health endpoint at `/health`
- Discord auth controller in [Controllers/AuthController.cs](E:\Repos\InfinityCodexWebApp\Controllers\AuthController.cs)

Available auth endpoints:

- `GET /auth/discord/login`
- `GET /auth/discord/callback`
- `GET /auth/discord/me`
- `GET /auth/discord/session`
- `POST /auth/logout`

### Data Model

The database model is the strongest implemented part of the app right now. `ApplicationDbContext` includes tables for:

- users
- characters
- character jobs
- character job levels
- character items
- character item needs
- items
- item allowed jobs
- item sources
- content sources

The schema includes several useful uniqueness constraints:

- one character name per owner
- one job entry per character and job code
- one character item pair
- one character item need pair
- composite keys for job levels, item allowed jobs, and item sources

### Frontend

Implemented in the Angular app:

- Angular 21 application scaffold
- one route: `/`
- persistent main layout with sidebar and top bar
- styled dashboard shell
- Discord login entry point
- session check on page load
- static character job-level card
- FullCalendar month view rendered on the home page
- Tailwind CSS integrated through global styles

## What Looks Like Prototype Or Placeholder Work

These pieces exist, but they are not yet connected into a complete product flow:

- the character profile component is hard-coded with sample data
- the calendar renders, but no events are loaded
- the login flow redirects to a hard-coded ngrok backend URL from the frontend
- the frontend auth service is not using Angular dependency injection in the normal app-wide pattern
- there are no frontend routes for characters, items, content, settings, or admin tools
- there are no backend CRUD controllers for characters, items, needs, or content sources
- there is no persistence step connecting a Discord-authenticated session to the `Users` table
- role-based authorization policies exist, but no known endpoint is using them yet

## Gaps Between Data Model And Product Surface

The codebase suggests a richer product than what users can currently do.

Defined in the model but not exposed as a usable product flow:

- create and manage characters
- assign jobs and job levels
- track collected items per character
- track missing or desired items
- browse or search items
- map items to content sources
- manage active/inactive users or characters
- use roles for admin, manager, contributor, and reader experiences

## Risks And Technical Debt

### Security

- `appsettings.json` contains a Discord OAuth client secret in source control.
- OAuth return URL validation is commented out in the callback action, which makes redirect handling riskier than it should be.

### Configuration

- The frontend points directly at `https://unrealistic-skyla-demagogically.ngrok-free.dev/auth`, which is environment-specific and brittle.
- The backend CORS setup is narrow and dev-only, which is fine for local work but not enough for production setup.

### Architecture

- The backend currently exposes only auth endpoints, so most of the domain model is unreachable from the UI.
- The frontend is using mostly page-local state and hard-coded demo data instead of a shared domain service layer.
- There is no visible test suite for the backend domain or API behavior.

### Repo Hygiene

- The worktree is already in the middle of structural changes, including moved model files and an untracked frontend folder.
- Generated frontend build output and dependencies exist locally, which can make repo scanning noisy during agent work.

## Verified Build Status

Verified on 2026-03-21:

- `dotnet build` from [E:\Repos\InfinityCodexWebApp](E:\Repos\InfinityCodexWebApp) succeeds
- `npm run build` from [E:\Repos\InfinityCodexWebApp\infinity-webapp](E:\Repos\InfinityCodexWebApp\infinity-webapp) succeeds

This means the current baseline compiles even though many features are still incomplete.

## Suggested Near-Term Milestones

The most logical next implementation slices are:

1. Move secrets and URLs into environment-based configuration.
2. Add domain APIs for characters, jobs, items, and item needs.
3. Replace hard-coded frontend data with API-backed services.
4. Decide what the first real dashboard workflow should be.

That sets the project up for meaningful feature brainstorming in the next phase.
