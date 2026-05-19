# 2Dinclass — In-class 2D shooter (Unity)

Unity project for the **in-class 2D space shooter** (course exercises). Part of the [Rhythm Runner](../README.md) monorepo.

## This session — gameplay updates

### 1) Main menu background FX (2D)
- Added `Assets/Scripts/UI/MainMenuBackgroundFX.cs`.
- Final style is 2D-only visual animation (no position movement, no 3D floating).
- Supports:
  - subtle color pulse
  - optional alpha pulse

### 2) Obstacle instant game over
- Added `Assets/Scripts/Health&Damage/ObstacleGameOverOnTouch.cs`.
- When the player touches an obstacle (trigger or collision), it calls `GameManager.instance.GameOver()`.
- Configurable options:
  - target tag filter (default `Player`)
  - destroy player on hit
  - destroy obstacle on hit

### 3) Score-based obstacle unlock
- Added `Assets/Scripts/Utility/ObstacleUnlockByScore.cs`.
- Reveals hidden obstacles at score thresholds.
- Current intended setup:
  - one obstacle appears when score > 10
  - one obstacle appears when score > 20

### 4) Score-based shooter unlock + difficulty spike
- Added `Assets/Scripts/Utility/ShooterUnlockByScore.cs`.
- Enables shooter objects at score thresholds.
- Supports per-shooter unlock fire-rate override.
- Global speed-up phase:
  - when score > 20, all configured shooters fire faster
  - intended for the three-shooter difficulty upgrade

### 5) Page flow / scene navigation
- Improved page switching and menu/page transitions.
- Fewer unexpected navigation states; clearer UI flow during play.

### 6) Visual and audio polish
- Updated backgrounds for stronger scene atmosphere.
- Improved SFX and overall audio feedback.
- Added hover/float-style animation polish for UI or scene elements.

### 7) Summary
- Stronger progression via score-driven hazards and shooters.
- Clearer player feedback through transitions, visuals, and sound.
- Challenge escalation is more intentional and readable in play.

## Notes
- Uses Unity 2D patterns (`Collider2D`, `Rigidbody2D`, triggers and collisions).
- `Library`, `Temp`, and other generated folders are ignored via `.gitignore`.
- Capstone design doc (Rhythm Runner): [Rhythm Runner Design.md](Rhythm%20Runner%20Design.md)
