---
change_id: testing-critical-path-persistence
title: Critical-path persistence coverage
status: implemented
created: 2026-09-14
updated: 2026-09-14
archived_at: null
---

## Notes

Open a change folder for rollout Phase 1 of context/foundation/test-plan.md: "Critical-path persistence coverage".
Risks covered: #1 (create/open project -> pin task on plan -> save -> close/reopen -> task and exact marker position preserved end-to-end; only unit-tested in pieces today).
Test types planned: integration.
Risk response intent: prove that after create/open project, click plan, enter task title, save, and reload (new store/view-model instance, not just re-reading a variable), the reloaded project exposes the task with identical title and identical marker coordinates within the established rounding margin; challenge the assumption that existing unit tests for PlanCoordinateMapper/project model/JSON store already prove this end-to-end.
