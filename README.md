# Rhythm Runner (Unity)

This repository contains the Unity project for **Rhythm Runner**.

## This Session Gameplay Updates

### 1) Main Menu Background FX (2D)
- Added `Assets/Scripts/UI/MainMenuBackgroundFX.cs`.
- Final style is 2D-only visual animation (no position movement, no 3D floating).
- Supports:
  - subtle color pulse
  - optional alpha pulse

### 2) Obstacle Instant Game Over
- Added `Assets/Scripts/Health&Damage/ObstacleGameOverOnTouch.cs`.
- When player touches obstacle (trigger or collision), it triggers `GameManager.instance.GameOver()`.
- Configurable options:
  - target tag filter (default `Player`)
  - destroy player on hit
  - destroy obstacle on hit

### 3) Score-Based Obstacle Unlock
- Added `Assets/Scripts/Utility/ObstacleUnlockByScore.cs`.
- Used to show hidden obstacles at score thresholds.
- Current intended setup:
  - one obstacle appears when score > 10
  - one obstacle appears when score > 20

### 4) Score-Based Shooter Unlock + Difficulty Spike
- Added `Assets/Scripts/Utility/ShooterUnlockByScore.cs`.
- Enables newly added shooter objects at score thresholds.
- Supports per-shooter unlock fire-rate override.
- Added global speed-up phase:
  - when score > 20, all configured shooters get faster fire rate
  - intended for the three-shooter difficulty upgrade.

### 5) Page Flow / Scene Navigation Improvements
- Improved page switching logic and overall menu/page transition flow.
- Reduced unexpected navigation states and made UI flow clearer during play.

### 6) Visual & Audio Polish
- Updated background presentation for better scene atmosphere.
- Improved sound effects and overall audio feedback quality.
- Added additional hovering/float-style animation polish for UI or scene elements.

### 7) What Was Improved
- Progression pacing is stronger with score-driven hazard and shooter activation.
- Player feedback is clearer through improved transitions, visuals, and sound cues.
- Challenge escalation now feels more intentional and readable during gameplay.

## Notes
- Project uses Unity 2D gameplay patterns (Collider2D / Rigidbody2D / trigger & collision events).
- `Library`, `Temp`, and other generated directories are ignored via `.gitignore`.
