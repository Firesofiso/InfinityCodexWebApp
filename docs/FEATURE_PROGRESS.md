# Feature Progress Tracker

Last updated: 2026-04-11

This tracker summarizes feature progress from the current feature specs.

Progress meaning in this file:

- `Proposed`: feature scoped, not yet marked active
- `Active`: feature currently prioritized and implementation-ready

## Snapshot

- Total tracked features: 4
- Active: 2
- Proposed: 2

## Feature List And Progress

| Feature | Stage | Priority | Current Progress | Next Milestone |
| --- | --- | --- | --- | --- |
| [Player Character Detail Page](E:/Repos/InfinityCodexWebApp/docs/features/player-character-detail-page.md) | Active | High | Parent feature scoped; child feature boundaries defined | Implement page read model and panel load states |
| [Gear Wishlist Management](E:/Repos/InfinityCodexWebApp/docs/features/gear-wishlist-management.md) | Active | High | Player-scoped wishlist CRUD defined; optional character assignment defined | Implement read/list + add/edit/remove with ownership checks |
| [Item Catalog Management](E:/Repos/InfinityCodexWebApp/docs/features/item-catalog-management.md) | Proposed | High | Catalog curation scope and acceptance criteria defined | Confirm role policy, then implement catalog read/list endpoints |
| [DKP Management](E:/Repos/InfinityCodexWebApp/docs/features/dkp-management.md) | Proposed | High | DKP read/adjustment model and audit requirements defined | Finalize adjustment roles and implement balance/history endpoints |

## Dependency Notes

- [Player Character Detail Page](E:/Repos/InfinityCodexWebApp/docs/features/player-character-detail-page.md) depends on:
  - [Gear Wishlist Management](E:/Repos/InfinityCodexWebApp/docs/features/gear-wishlist-management.md) for full wishlist CRUD
  - [DKP Management](E:/Repos/InfinityCodexWebApp/docs/features/dkp-management.md) for deeper DKP workflows beyond read-only display
- [Gear Wishlist Management](E:/Repos/InfinityCodexWebApp/docs/features/gear-wishlist-management.md) depends on [Item Catalog Management](E:/Repos/InfinityCodexWebApp/docs/features/item-catalog-management.md) for curated item source quality.

## Recommended Build Order

1. Player Character Detail Page (read models + mission status edits)
2. Gear Wishlist Management
3. Item Catalog Management
4. DKP Management
