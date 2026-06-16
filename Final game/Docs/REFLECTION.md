# Reflection

Personal reflection for **Rhythm Playground** (individual capstone).

## Organisation

I used MoSCoW in the GDD to keep scope under control, a Kanban board ([`Kanban/BOARD.md`](Kanban/BOARD.md)) for weekly tasks, and GitHub Issues linked to the same cards. I also kept a running change log in [`PROJECT_MEMORY.md`](../PROJECT_MEMORY.md) so I could trace bugs across scene refactors.

## Time management

Milestones M0–M5 (menu, four scenes, core modes, beat system) were done by early June. I spent the second half of June on polish, tests, and documentation in parallel. When playtests showed the Ocean forced tutorial was too long, I cut it instead of slipping the playable deadline — see [`FEEDBACK.md`](FEEDBACK.md).

The largest schedule risk was pushing oversized audio to GitHub. I rewrote history once (`61ab3ce`), tightened `.gitignore`, and documented the baseline so I could recover without losing the build.

## Independent work

This is solo work: design, scenes, scripts, and most testing. The four-mode structure comes from my own music background (18 years piano), not from a generic runner template. I used ChatGPT and Cursor for drafts and debugging help, but I verified everything in Unity myself — see [`AI_USAGE.md`](AI_USAGE.md).

## Professionalism

I listed honest limitations (Global Music content depth, no PlayMode suite) in [`LIMITATIONS.md`](LIMITATIONS.md). Legacy bunny code is archived in [`../BunnyLegacyArchive`](../BunnyLegacyArchive), not silently deleted. Kenney CC0 packs and my own asset statements are in [`ASSET_LICENSES.md`](../Assets/AssetLicenses/ASSET_LICENSES.md). Child playtesting was done with consent and gentle failure feedback in the younger modes.

## What I would do differently

- Add EditMode scene tests earlier — they would have caught Ocean UI node loss sooner.
- Use English commit messages from the start for clearer history.
- Capture screenshots at each milestone instead of only at the end.

## Module behaviour

I kept a stable Play path from Start through all four modes, a known-issues list, and this GitHub repo as the single source of evidence for the module.
