---
change_id: add-ui-illustrations
title: Add UI illustrations
status: implemented
created: 2026-09-14
updated: 2026-09-14
archived_at: null
---

## Notes

Add decorative illustrations to existing WPF views: `login-illustration.png`
on Login/Register (right side, form on left), `recent-projects-illustration.png`
on Recent Projects (bottom center). Both PNGs already exist in `Assets/` but
are unused — not referenced in XAML and not wired into the `.csproj`.
Minimal WPF-native Grid + Image changes only; no new ViewModels, no behavior
changes.
