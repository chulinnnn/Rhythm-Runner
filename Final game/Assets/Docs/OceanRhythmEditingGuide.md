# Ocean Rhythm Editing Guide

This guide is only for the Ocean Rhythm section.

## Where To Change Textures

Primary rule: change Ocean Rhythm textures on `OceanRhythmManager` in `Assets/Scenes/OceanRhythm.unity`.

Use the `Sprites - single source for Ocean Rhythm visuals` section:

- `fishSprite`, `octopusSprite`, `turtleSprite`, `jellyfishSprite`: pond animal images.
- `netSprite`: the player cursor net.
- `bucketSprite`: the bucket button and bucket workshop preview.
- `bucketSlotSprite`: the visible target spots on the bucket.
- `shellSprite`, `singingShellSprite`, `mysteryFishSprite`: shell and mystery visuals.
- `seaweedSprite`, `starSprite`, `flagSprite`, `pearlSprite`, `bellCharmSprite`, `glowStarSprite`, `waveRibbonSprite`: bucket decoration icons.
- `bucketDecorationSprites`: optional exact override array for decorations. Index order follows `OceanDecorationReward`.

Do not assign separate bucket/fish images directly on child UI objects. The UI now refreshes from `OceanRhythmManager`.

## Where To Change Sizes

Primary UI sizes are centralized in `Assets/Scripts/OceanRhythmUIController.cs`, inside `OceanVisual`:

- `BucketButtonSize`: clickable bucket button size in the pond.
- `BucketIconSize`: bucket icon size inside the button.
- `BucketPreviewSize`: large bucket image in the workshop.
- `BucketSlotSize`: bucket decoration target size.

Animal pond objects are currently created in `OceanRhythmUIController.ShowFreePond` and `SpawnMysteryFish`, where the object rect size is `300 x 300`.

The net cursor size is in `Assets/Scripts/OceanNetCursor.cs`:

- `Mathf.Lerp(300f, 350f, captureRatio)` controls normal and capture-ready cursor size.

## Click Rules

Most visual `Image` components must not receive raycasts. The UI normalizes this in `OceanRhythmUIController.NormalizeOceanRaycasts`.

Clickable elements are intentionally limited to:

- Buttons such as `CatchBucketButton`, `TapButton`, navigation, overlays, and close buttons.
- `OceanBucketSlot` backgrounds.
- `OceanDecorationDragItem` cards in the decoration library.

If a new decorative image blocks clicks, set its `raycastTarget` to `false` or add it to the normalization rules.

## Bucket Workshop

Bucket slot behavior is in `Assets/Scripts/OceanBucketSlot.cs`.

This script owns:

- Empty, filled, and highlighted slot colors.
- Slot labels.
- Slot click feedback.
- Decoration placement pulse.

The bucket workshop layout is created by these methods in `OceanRhythmUIController.cs`:

- `CreateBucketUi`
- `CreateBucketAlbum`
- `CreateBucketPreview`
- `CreateBucketSlot`
- `CreateDecorationLibrary`

## Visual Polish

Current Ocean Rhythm visual effects are intentionally lightweight:

- `WaterRippleController` creates mouse ripple feedback.
- `CreateAnimatedWater` creates soft non-interactive wave bands.
- `OceanPondAnimal.Update` controls swimming, capture pulse, glow, and fly-to-bucket animation.
- `OceanNetCursor.Update` smooths cursor movement and color changes.

Keep new effects non-interactive unless they are real controls. Decorative layers should not receive raycasts.
