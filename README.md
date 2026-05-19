# Rhythm Runner

This repository is a **collection of work** from a game development course: early design concepts, the in-class 2D shooter built week by week, classroom activity archives, and the capstone project **Rhythm Runner**.

**Remote repository:** [github.com/chulinnnn/Rhythm-Runner](https://github.com/chulinnnn/Rhythm-Runner)

---

## Repository layout

| Directory | Description |
|-----------|-------------|
| [`2Dinclass/`](2Dinclass/) | **Unity 2D space shooter** built during class (main in-class exercise) |
| [`prototype/`](prototype/) | **Early-course** game ideas and prototype assets |
| [`inclass activity/`](inclass%20activity/) | **Classroom activities** across the term (assignments, notes, deliverables) |
| [`Final game/`](Final%20game/) | **Capstone project** (playable Rhythm Runner build goes here) |

---

## 2Dinclass — In-class 2D shooter

A Unity **2022.3 LTS** 2D project that grew with the course: shooting, enemies, UI, scoring, and difficulty progression.

- **How to open:** In Unity Hub, add and open the `2Dinclass/` folder.
- **Main scenes:** `Assets/Scenes/MainMenu.unity`, `Level1.unity`, `Level2.unity`
- **Details:** See [`2Dinclass/README.md`](2Dinclass/README.md) for session-by-session gameplay updates.

The capstone design doc is also kept under `2Dinclass/` for reference while implementing:

- [`2Dinclass/Rhythm Runner Design.md`](2Dinclass/Rhythm%20Runner%20Design.md) — Full Rhythm Runner spec (screens, beat timing, difficulty, Blind mode, etc.)

---

## prototype — Early game concepts

Gameplay and level ideas from the start of the course, before a full Unity project existed.

Current example:

- `prototype/escaping/` — Sketches for an “escape”-style concept (`map-overview.jpg`, `room-detail.jpg`, etc.)

Add more prototype branches here as themed subfolders when needed.

---

## inclass activity — Classroom records

Documents and deliverables organized by **class date** (assignments, group materials, required outputs).

Example layout:

```
inclass activity/
└── 5.19/                    # e.g. class on May 19
    ├── activity/            # Activity files (individual/group submissions)
    └── output/              # Required final deliverables
```

Files may be `.docx`, `.pdf`, and similar formats—check the folder for each date.

---

## Final game — Capstone (Rhythm Runner)

**Rhythm Runner** is the namesake capstone: a 2D side-scrolling **rhythm runner**—auto-run to the right, jump/crouch/boost on the beat, click pickups for score, multi-screen UI, difficulty tiers, and Blind mode (see the design doc).

- **Design doc:** [`2Dinclass/Rhythm Runner Design.md`](2Dinclass/Rhythm%20Runner%20Design.md)
- **Project location:** The playable Unity project lives in `Final game/`, separate from the in-class shooter in `2Dinclass/` for easier maintenance and submission.

> Use `Final game/` for the final buildable, runnable project. If the project has not been moved in yet, treat whatever is actually in that folder as the source of truth.

---

## Requirements

| Item | Recommended |
|------|-------------|
| Unity | 2022.3.x LTS (`2Dinclass` currently uses **2022.3.62f3c1**) |
| Template | 2D Core |

After cloning, do not commit Unity-generated folders such as `Library/` or `Temp/` (`2Dinclass/.gitignore` already excludes them).

---

## Quick start

```bash
git clone git@github.com:chulinnnn/Rhythm-Runner.git
cd Rhythm-Runner
```

1. **In-class shooter:** Unity Hub → Open → select `2Dinclass/`
2. **Capstone:** Unity Hub → Open → select `Final game/` (once the project is ready)
3. **Class records:** Browse dated folders under `inclass activity/`

---

## Links

- GitHub: [chulinnnn/Rhythm-Runner](https://github.com/chulinnnn/Rhythm-Runner)
- In-class project: [2Dinclass/README.md](2Dinclass/README.md)
- Capstone design: [2Dinclass/Rhythm Runner Design.md](2Dinclass/Rhythm%20Runner%20Design.md)
