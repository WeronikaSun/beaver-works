---
change_id: test-plan-refresh-2026-09-14
title: Add Risk #2 rollout phase for ProjectWorkspaceViewModel save/refresh
status: archived
created: 2026-09-14
updated: 2026-09-14
archived_at: 2026-09-14T10:04:16Z
---

## Notes

Refresh context/foundation/test-plan.md: the author-stated single-test scope override in Section 1 no longer applies. Add Risk #2 (ProjectWorkspaceViewModel mutation/save/refresh regression) as a new rollout Phase 2, minimal scope, one additional integration test, no E2E/UI automation. Risk Response Guidance to add for Risk #2: prove a task mutation via ProjectWorkspaceViewModel is persisted via save AND reflected in both dependent panel ViewModels (task list, plan canvas/markers), not just that the underlying model mutated; must challenge the assumption that in-memory mutation implies the panels refresh; research must ground which command drives edit/remove, how TaskListViewModel/PlanCanvasViewModel observe Project.Tasks, and save-then-refresh ordering; likely cheapest layer is integration (no FlaUI), same style as the Phase 1 test; avoid the anti-pattern of asserting only the underlying collection changed without checking dependent panel ViewModels reflect it. After creating the folder, follow the downstream continuation rule.
