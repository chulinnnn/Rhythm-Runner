# Rhythm Runner

**Rhythm Runner** is a Unity 2D runner demo that is being expanded into a rhythm-driven platformer. The current version already has a small but playable parkour loop: the character runs through side-scrolling map segments, jumps over hazards, collects coins, uses power-ups, and tries to survive for a longer distance.

The next goal is to connect the running actions with music timing, so the player is not only reacting to obstacles visually, but also listening to the beat and operating in rhythm.

## Current Demo

This demo currently includes:

- **Basic runner movement**: the stage scrolls from right to left while the player controls jump and slide actions.
- **Jumping and bounce points**: the player can jump over obstacles, and special upper colliders can trigger an extra upward bounce.
- **Coins and scoring**: small coins increase the score by `1`, larger bonus coins increase it by `5`.
- **Coin magnet power-up**: collecting the magnet item pulls nearby coins toward the player for a short time.
- **Speed-up / flying power-up**: collecting the jetpack-like speed item temporarily fixes the player in a flying state and increases the scrolling speed.
- **Ground enemies and obstacles**: touching enemy barriers ends the run.
- **Flying villain / enemy assets**: the project contains flying enemy animation assets that can be used as airborne threats.
- **Easy and hard scenes**: `Game` and `Game2` use different map prefab sets and difficulty settings.
- **Music and sound effects**: each gameplay scene has looping background music, with coin and failure sound effects.
- **Local leaderboard**: the game stores best distances separately for easy and hard mode.

## Current Gameplay Loop

The player starts from the menu, chooses a difficulty, then enters an endless side-scrolling run. Distance increases over time based on background speed. Coins reward survival and clean routing. Obstacles force the player to jump, slide, or use level objects correctly. Power-ups temporarily change the rhythm of the run by speeding up movement or making coins easier to collect.

At this stage, the game already has the foundation of a basic 2D parkour demo. However, music currently works mostly as background audio. Player inputs, obstacle placement, scoring, and difficulty are not yet strongly bound to the beat.

## Next Development Goal: Rhythm Integration

The main next step is to make the game feel like a rhythm runner instead of a normal runner with music. The intended experience is:

> Players need to operate on or near the beat to keep the run smooth and survive reliably.

This can be built as a minimal rhythm system first, without redesigning the whole game.

### Minimum Rhythm Features

- **Beat tracking**: define BPM, song start time, and beat interval for each music track.
- **Timing window**: judge player input as `Perfect`, `Good`, or `Miss` depending on how close the action is to the nearest beat.
- **Rhythm jump / slide**: jumping or sliding near the beat should feel better, give feedback, or provide a small advantage.
- **Beat-aligned obstacles**: place obstacles, bounce points, coins, and power-ups so they arrive according to musical beats instead of only random spacing.
- **Rhythm feedback**: show simple UI or visual effects when the player hits the beat correctly.
- **Rhythm scoring**: reward beat-accurate actions with extra score, combo, or distance bonus.

## Suggested MVP Implementation Plan

1. Add a `RhythmManager` that reads the active song's BPM and calculates the nearest beat using Unity audio time.
2. Add an input judge method, for example `GetTimingResult()`, returning `Perfect`, `Good`, or `Miss`.
3. Connect the judge to `PlayerController` jump and slide input.
4. Add simple feedback text or effects such as `Perfect!`, `Good`, and `Miss`.
5. Start with rewards rather than harsh punishment: accurate rhythm inputs can give bonus score, slightly stronger jump, or combo growth.
6. After the timing system feels stable, adjust map segments so coins, enemies, bounce points, and power-ups appear on predictable beat positions.
7. Finally, tune hard mode so it requires stricter timing windows and denser beat-based obstacle patterns.

## Design Direction

The game should remain a simple 2D runner at its core. Rhythm should make the existing actions more meaningful instead of adding too many new controls. A good first target is:

- Easy mode teaches the rhythm with generous timing windows.
- Hard mode uses faster scrolling, denser obstacles, and stricter timing.
- Coins and power-ups guide the player toward the beat.
- Obstacles punish ignoring the beat, but the demo should still feel fair and readable.

## Current Status

This is still a small Unity 2D demo, not a finished game. The current build is useful as a prototype foundation because the main runner systems already exist. The next development work should focus on turning the existing movement and level flow into a music-synchronized experience.

---

Part of the [Rhythm Runner monorepo](../README.md). For full design notes, see [Rhythm Runner Design.md](../2Dinclass/Rhythm%20Runner%20Design.md) in `2Dinclass/`.
