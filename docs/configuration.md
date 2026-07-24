# Configuration

BrickLoco exposes its tuning values through Unity Mod Manager's settings system, so you can
change behaviour without recompiling — and, for most values, without even restarting.

**In-game:** press <kbd>Ctrl</kbd>+<kbd>F10</kbd> to open the UMM window, find *Brick Loco*,
and edit the fields directly.

**On disk:** `Derail Valley/Mods/BrickLoco/Settings.xml`, written when you press *Save* in
the UMM window. You do not need to create it; defaults live in code (`Settings.cs`) and the
file appears on first save.

Settings are plain fields read every time the mod uses them, and physics values are
re-applied to the spawned car the moment you edit them (`Settings.OnChange`), so edits in
the UMM window apply **immediately** — no restart. The one exception is noted below.

---

## Physics and propulsion

| Setting | Type | Default | What it does |
| --- | --- | --- | --- |
| `MaxSpeed` | float | `20` | Speed cap in m/s. Above it, further force in that direction is ignored. The sign is ignored. |
| `Force` | float | `7000` | Propulsion force in Newtons, applied every `FixedUpdate` while <kbd>G</kbd>/<kbd>H</kbd> is held. |
| `Mass` | float | `20000` | Mass assigned to the car body's Rigidbody, in kg. **Only applied when `LetGameOwnPhysics` is off.** |
| `LetGameOwnPhysics` | bool | `true` | Leave the car's mass, centre of mass and rotation constraints to the game's `TrainMassController` and bogie suspension. This is the default because overriding them desynced the bogie joint springs and sank the wheels into the rail. Turn it off only to A/B the legacy placeholder overrides (`Mass`, `ComHeight`, `FreezeCarTilt`). |
| `DriveViaBogies` | bool | `true` | Push through the game's own `Bogie.ApplyForce` (force follows the rail) instead of shoving the carbody along the car's axis. Turning this off restores the old jittery behaviour — useful for A/B comparison. |
| `FreezeCarTilt` | bool | `true` | The roll/pitch freeze that stands in for real suspension. **Only applied when `LetGameOwnPhysics` is off.** Off = the body is free to tilt (and, in principle, roll over). |
| `ComHeight` | float | `0.5` | Centre-of-mass height above the car origin, in metres. Lower fights roll-over harder. **Only applied when `LetGameOwnPhysics` is off.** |
| `SmoothBogies` | bool | `true` | Interpolate the bogie rigidbodies like the body. Without it the (now visible) wheels stutter relative to the body in first person. |
| `LateMountRepin` | bool | `true` | Keep the mounted player re-pin in `LateUpdate`. That pass runs *after* DV's camera scripts, so its correction lands a frame late — turn this off to test whether it is the source of mounted-view jitter. |

With `LetGameOwnPhysics` on (the default) the mod does not touch the car's mass, centre of
mass or rotation constraints at all — the spawned car behaves like any vanilla flatcar,
which is why the wheels sit correctly on the rail and the car rerails and derails normally.
The `Mass`, `ComHeight` and `FreezeCarTilt` values below it are the legacy placeholder path,
kept only for comparison.

These apply **live** — editing a physics value in the UMM window re-tunes the spawned car
immediately, so tuning is an in-game loop, not a respawn loop.

`Force` and `Mass` interact: acceleration is roughly `a = F/m`, so **raising `Mass` makes
the same `Force` feel weaker**. If the car feels sluggish, either raise `Force` or lower
`Mass` — changing both at the same ratio changes nothing.

`MaxSpeed` gates per direction. Over the cap moving forward, <kbd>G</kbd> stops
contributing while <kbd>H</kbd> still works, so you can always brake. Setting `MaxSpeed = 0`
pins the car: neither key drives it once it is moving at all.

Note that `Mass` writes the body Rigidbody directly, bypassing the game's
`TrainMassController` (which normally distributes mass between body and bogies). That bypass
is exactly what sank the wheels, which is why `LetGameOwnPhysics` defaults to on and leaves
mass to the game.

---

## Debug logging

| Setting | Type | Default | What it does |
| --- | --- | --- | --- |
| `MountTelemetry` | bool | `false` | Master switch for diagnostic logging: mount telemetry, key-down traces, jitter snapshots, and the <kbd>F9</kbd> dump. |
| `DumpOnMount` | bool | `false` | Also dump the full player component tree on every mount and dismount. Requires `MountTelemetry`. |

`MountTelemetry` is **off by default**. When on it re-logs the mounted-player enforcement
every frame (including a CharacterController re-disable line that is not rate-limited), which
is continuous disk I/O while mounted and contributes to frame hitches at speed. Turn it on
only while actively debugging the mount, not for normal play.

`DumpOnMount` is nested under `MountTelemetry`: setting it `true` alone does nothing.

Log output goes to the UMM console (<kbd>Ctrl</kbd>+<kbd>F10</kbd>) and to
`Derail Valley/DerailValley_Data/Managed/UnityModManager/Log.txt`, prefixed `[BrickLoco]`.

---

## Mounted-player behaviour

These control the fight with Derail Valley's own movement scripts. Background in
[Mounting](mounting.md).

| Setting | Type | Default | What it does |
| --- | --- | --- | --- |
| `DisableScriptsWhileMounted` | bool | `true` | Master switch for disabling player movement scripts during a mount. |
| `ScriptsToDisableWhileMounted` | string | `LocomotionInputWrapper,CharacterReparenting,CameraAnchorLeanCrouch` | Comma-separated MonoBehaviour type names to disable. |
| `DisableCharacterControllerWhileMounted` | bool | `true` | Disables the `CharacterController` component, which otherwise resizes its collider on crouch/jump and causes jitter. |
| `AlwaysDisableCriticalScripts` | bool | `true` | Merges `CriticalScriptsToDisable` into the set regardless of what `ScriptsToDisableWhileMounted` says. |
| `CriticalScriptsToDisable` | string | `LocomotionInputWrapper,CharacterReparenting,CameraAnchorLeanCrouch` | The names merged in when the above is `true`. |
| `SuppressProblemKeysWhileMounted` | bool | `true` | Enables the <kbd>Ctrl</kbd>/<kbd>X</kbd>/<kbd>Space</kbd> mitigation window. |
| `SuppressProblemKeysSeconds` | float | `1.5` | How long that window stays open. Negative values are clamped to zero. |

The script lists are evaluated at **mount time**, so changing them while mounted takes
effect on the next mount.

### Two rules the settings cannot override

Whatever you put in these lists, the final set is adjusted:

- **`LocomotionInputWrapper` and `CharacterReparenting` are always added.** They are what
  actually keep the player on the seat; without them the mount does not hold. Clearing both
  strings does not produce an empty set.
- **`CustomFirstPersonController` is always removed.** It is the primary look/camera
  script — disabling it leaves you mounted and unable to look around. Naming it in either
  list has no effect.

Both rules live in `MountScriptPolicy` and are covered by unit tests.

### Why two lists

`ScriptsToDisableWhileMounted` is the list you are expected to edit while experimenting.
`CriticalScriptsToDisable` is a floor that survives that experimentation — so you can empty
the first list to test a theory without also losing the scripts that make mounting work at
all. With `AlwaysDisableCriticalScripts = true` (the default) the two are merged and
de-duplicated.

Names are **short type names**, not namespace-qualified, and are matched case-sensitively
against MonoBehaviours found under the player controller root. A name that matches nothing
is reported in the log rather than failing:

```
[BrickLoco] [MountDisable] Requested but not found under controller: SomeTypo
```

---

## Resetting

Delete `Mods/BrickLoco/Settings.xml` and relaunch — defaults come from code. Undeploying
removes the whole mod folder, Settings.xml included, so a redeploy always starts from
defaults unless you back the file up.

## Legacy BepInEx config

Before migrating to Unity Mod Manager this mod was a BepInEx plugin configured via
`BepInEx/config/com.zobayer.brickloco.cfg`. That file is no longer read.
`.\scripts\undeploy.ps1 -PurgeConfig` deletes it if present.
