# Feedback and Responses

How playtest and course feedback changed **Rhythm Playground**. Commits: [`COMMITS.md`](COMMITS.md).

| Source | Feedback | Response | Evidence |
|--------|----------|----------|----------|
| Self (manual test) | Hand-placed prefabs on the beat were too hard to author routes | Template-based generation (`VerticalBeatSpawner`, Advanced chart tables) | May 2026; [`PROJECT_EVOLUTION.md`](PROJECT_EVOLUTION.md) |
| Friends (cold start) | Early bunny runner was boring; mechanics too simple | Four age-appropriate modes from music-education goals | May 26–28; GDD |
| Friends (5–10 age) | Long forced Ocean tutorial was unacceptable | Removed guided tutorial; Free Pond + intro/beat cards | `PROJECT_MEMORY` 2026-06-04 |
| Friend playtest | Vertical tutorial too long | Shorter route; banana/parrot appear earlier | `VerticalBeatSpawner` |
| Friend playtest | Wrong parrot side felt unfair (instant fail) | Jump to chosen side, warning, then recover | `VerticalRunnerPlayer` |
| Friend playtest | Heart death too harsh for children | Miss counter; run continues | `VerticalRunnerManager` Jun 5 |
| Friend playtest | Beat slot for jump vs actions confusing | Iterated slot ownership; final: Space on jump slot, actions on other slots | `PROJECT_MEMORY` Jun 5 |
| Self / peer | Advanced HUD beats did not match falling targets | Chart generation on beat slots 0 and 2 | AdvancedRunner Jun 7–8 |
| Self | Tutorial and Game BPM mixed in Advanced | Separate music stages + single `AdvancedRunnerAudio` clock | `PROJECT_MEMORY` Jun 7 |
| Self | BucketAlbum clicks selected fish underneath | Overlay guards; fish `raycastTarget=false` | `OceanRhythmUIController` Jun 10 |
| Child playtest (7 y/o, consent) | UI clarity and engagement check | Prompt timing and copy tweaks | Jun 10 prompt commits |
| Git / push | Large audio files broke remote push | Removed oversized assets from history; `.gitignore` updated | Commit `61ab3ce`; [`REFLECTION.md`](REFLECTION.md) |
| Course (initial assessment) | Needed stable runnable baseline before polish | Recoverable `61ab3ce` baseline; `playable` milestone Jun 11 | `0b44d94` |
| Module / demo prep | Show four modes clearly from Start; keep scope honest | Unified scenes, short UI copy, limitations doc | Start.unity; [`LIMITATIONS.md`](LIMITATIONS.md) |

## Related

- [`TESTING.md`](TESTING.md)
- [`Kanban/CARDS.md`](Kanban/CARDS.md) — playtest tasks RR-035, RR-038, RR-048
