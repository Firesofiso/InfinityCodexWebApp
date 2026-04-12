# Infinity Codex Docs

This folder is the working documentation set for continuing development with Codex agents and separate threads.

## Start Here

- [Current State](E:\Repos\InfinityCodexWebApp\docs\CURRENT_STATE.md): what is already built, what is only scaffolded, and the main risks discovered during analysis.
- [Agent Handoff Guide](E:\Repos\InfinityCodexWebApp\docs\AGENT_HANDOFF.md): how to approach this repo in future agent threads without re-discovering the same context.
- [Feature Brainstorm](E:\Repos\InfinityCodexWebApp\docs\FEATURE_BRAINSTORM.md): a structured backlog starter for deciding what to build next.
- [Feature Progress Tracker](E:\Repos\InfinityCodexWebApp\docs\FEATURE_PROGRESS.md): one-page status view of tracked features and next milestones.
- [Feature Specs](E:\Repos\InfinityCodexWebApp\docs\features\README.md): implementation-focused docs for individual features so async threads can target a single scope.

## Verified Baseline

- Backend: `dotnet build` succeeds from the repo root.
- Frontend: `npm run build` succeeds from `infinity-webapp`.
- Current architecture: ASP.NET Core Web API (`net8.0`) plus an Angular 21 frontend.

## Important Notes

- The git worktree is already dirty. Some files appear to have been moved from the repo root into `Data/Model/`, and the frontend folder is still untracked.
- `appsettings.json` currently contains a Discord OAuth client secret. Treat that as a security issue and move it out of source control before production work.
