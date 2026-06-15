# Feedback and Responses

How playtest and course feedback changed **Rhythm Playground**. Linked commits are in [`COMMITS.md`](COMMITS.md).

| Source | Feedback | Response | Evidence |
|--------|----------|----------|----------|
| Self (manual test) | Hand-placed prefabs on the beat were too hard to author routes | Switched to template-based dynamic generation (`VerticalBeatSpawner`, Advanced chart tables) | May 2026 commits; [`PROJECT_EVOLUTION.md`](PROJECT_EVOLUTION.md) |
| Friends (cold start) | Early bunny runner was boring; mechanics too simple | Redesigned into four age-appropriate modes using music-education goals | May 26–28 mode commits; GDD |
| Friends (5–10 age) | Long forced Ocean tutorial was unacceptable | Removed guided tutorial startup; Free Pond + intro/beat cards only | `PROJECT_MEMORY` 2026-06-04; OceanRhythmManager |
| Friend playtest | Vertical tutorial too long | Shortened tutorial route; banana/parrot appear earlier | `VerticalBeatSpawner` route steps |
| Friend playtest | Wrong parrot side felt unfair (instant fail) | Jump to chosen side, warning, then recover | `VerticalRunnerPlayer` |
| Friend playtest | Heart death too harsh for children | Miss counter; run continues | `VerticalRunnerManager` Jun 5 |
| Friend playtest | Beat slot for jump vs actions confusing | Several slot-ownership iterations; final: Space on jump slot, actions on other slots | `PROJECT_MEMORY` Jun 5 |
| Self / peer | Advanced HUD beats did not match falling targets | Unified chart generation on beat slots 0 and 2 | AdvancedRunner Jun 7–8 |
| Self | Tutorial and Game BPM mixed in Advanced | Separate music stages + single `AdvancedRunnerAudio` clock | `PROJECT_MEMORY` Jun 7 |
| Self | BucketAlbum clicks selected fish underneath | Overlay guards, raycast fixes on pond fish | `OceanRhythmUIController` Jun 10 |
| Child playtest (7 y/o, consent) | Validated UI clarity and engagement for target age | Minor copy and prompt timing tweaks | Manual notes; prompt timing commits Jun 10 |
| Git / push | Large audio files broke remote push | Removed oversized assets from history; `.gitignore` hardened | Commit `61ab3ce`; [`REFLECTION.md`](REFLECTION.md) |
| Course (initial assessment) | Needed stable runnable baseline before polish | Tagged recoverable commits; focused on playable vertical slice | Jun 9–11 `playable` milestone |

## Adding teacher feedback

If tutors gave verbal feedback in class, add rows here (English) with date, quote/summary, and file/commit changed.

## Related

- Testing evidence: [`TESTING.md`](TESTING.md)
- Kanban playtest cards: [`Kanban/CARDS.md`](Kanban/CARDS.md) (RR-035, RR-038, RR-048)
