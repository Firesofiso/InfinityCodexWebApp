# Feature Brainstorm

This file is not the final roadmap. It is a structured starting point for deciding what to build after the current-state analysis.

## Product Questions To Answer First

Before building more surface area, decide:

1. Who is the first target user?
2. What is the single most valuable workflow for that user?
3. What should the dashboard calendar actually represent?
4. Is this primarily a personal tracker, a group planning tool, or both?
5. Which parts of the current schema are essential for v1, and which can wait?

## Features Already Implied By The Schema

The existing backend model suggests these product areas:

- account and user identity
- character roster management
- job and level progression
- inventory or equipment tracking
- missing-item or wish-list tracking
- content-source reference data
- role-based access for team or community management

## Candidate Feature Buckets

### 1. Core Account And Character Setup

Potential features:

- Discord sign-in with first-time user provisioning
- create, edit, archive, and switch between characters
- choose data source or import source for each character
- manage active versus inactive characters

Why it matters:

- This turns the current auth flow into a real onboarding path.

### 2. Character Progression Tracking

Potential features:

- edit job levels
- visualize capped and uncapped jobs
- track progression milestones over time
- compare current state against target builds

Why it matters:

- The current UI already hints at this and could become the first real value prop quickly.

### 3. Item And Need Management

Potential features:

- search or browse item catalog
- mark items owned by a character
- mark items as needed, wanted, or farming
- attach notes to why an item matters
- filter needs by job, slot, or required level

Why it matters:

- This matches a large part of the existing schema and creates a clear gameplay-planning loop.

### 4. Content Planning

Potential features:

- define content sources such as bosses, events, or zones
- map items to sources
- show which content to run for current needs
- add a calendar of runs, resets, or events

Why it matters:

- This is probably the reason the calendar was introduced, but it needs a sharper product definition.

### 5. Collaboration And Roles

Potential features:

- admin-only management screens
- contributor workflows for shared data curation
- shared planning views for groups or linkshells
- audit or moderation tools for content data

Why it matters:

- Roles already exist in code, so this could become a later multi-user expansion path.

## Suggested v1 Options

If the goal is to narrow scope, these are the strongest choices:

### Option A: Personal Character Tracker

Build:

- Discord login
- user provisioning
- create characters
- edit job levels
- track wanted and owned items

This is the simplest path from current code to real user value.

### Option B: Gear Planning Dashboard

Build:

- character overview
- item catalog
- needed items by character and job
- source mapping for each needed item

This uses more of the schema and creates a stronger planning product.

### Option C: Group Content Planner

Build:

- shared roster
- content schedule calendar
- item and source planning by group
- roles for admins and contributors

This is the most ambitious and should probably wait until the personal workflows are stable.

## Recommended Next Brainstorm Session

In the next thread, turn this into decisions rather than ideas:

1. Pick one v1 option.
2. Define the main user journey from login to first success.
3. Decide which existing tables are truly needed for that v1.
4. Convert that into a prioritized implementation backlog.
