# Kanban — Rhythm Runner Basic v1 (1 week)

**Goal:** Smallest playable 2D runner — auto-run forward, jump, hit obstacles and lose, restart. No rhythm, coins, or boost yet (save for v2).

**Scope:** `Final game/` Unity 2D project  
**Target:** Playable loop by end of week (May 19 → May 26, 2026)

---

## Board

### Backlog (later / out of scope this week)

| ID | Card | Notes |
|----|------|--------|
| B-01 | Rhythm beat timing | After basic movement feels good |
| B-02 | Coin pickups + UI counter | v2 economy |
| B-03 | Boost / sprint on beat | v2 ability |
| B-04 | Duck / slide | v2 ability |
| B-05 | Main menu + settings | v2 flow |
| B-06 | Multiple levels / endless spawner | v2 content |
| B-07 | Music + SFX pass | v2 polish |
| B-08 | Blind mode / difficulty tiers | Capstone features |

---

### To do (this week)

| ID | Card | Est. | Suggested day |
|----|------|------|----------------|
| T-01 | Create Unity 2D project in `Final game/` | 0.5d | Mon |
| T-02 | Player character: auto-move right | 0.5d | Mon |
| T-03 | Ground / platforms + collisions | 0.5d | Tue |
| T-04 | Jump (single action, feel OK) | 0.5d | Tue |
| T-05 | Obstacle prefab: touch = fail state | 0.5d | Wed |
| T-06 | Camera follows player | 0.25d | Wed |
| T-07 | One test scene: flat run + 3–5 obstacles | 0.5d | Thu |
| T-08 | Start flow: press Start → run begins | 0.25d | Thu |
| T-09 | Game Over screen + Restart | 0.5d | Fri |
| T-10 | Simple score (distance or survival time) | 0.25d | Fri |
| T-11 | Playtest + fix game-breaking bugs | 0.5d | Sat |
| T-12 | Buffer / doc “what’s in Basic v1” | 0.5d | Sun |

**Definition of done (Basic v1):** Open project → Start → character runs → jump over gaps/obstacles → die on bad hit → see score → Restart works without restarting Unity.

---

### In progress

| ID | Card | Owner | Started |
|----|------|-------|---------|
| — | *(move cards here when you start)* | | |

---

### Done

| ID | Card | Completed |
|----|------|-----------|
| — | *(move finished cards here)* | |

---

## Week at a glance

| Day | Focus | Cards |
|-----|--------|--------|
| **Mon** | Project + movement | T-01, T-02 |
| **Tue** | World + jump | T-03, T-04 |
| **Wed** | Fail state + camera | T-05, T-06 |
| **Thu** | Level + start | T-07, T-08 |
| **Fri** | Loop + score | T-09, T-10 |
| **Sat** | Playtest | T-11 |
| **Sun** | Buffer / ship checklist | T-12 |

---

## How to use this file

1. Copy each **To do** row into your Kanban tool (GitHub Project, Notion, Trello, etc.) as a card in **Backlog** or **This week**.
2. When you start work, move the row to **In progress** (or your tool’s Doing column).
3. When **Definition of done** is met for that card, move it to **Done**.

If you use **GitHub Issues**, suggested titles: `[Basic v1] T-01 Create Unity 2D project`, etc.
