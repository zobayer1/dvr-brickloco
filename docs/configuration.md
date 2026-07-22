# Configuration

BrickLoco exposes its tuning values through Unity Mod Manager's settings system, so you can
change behaviour without recompiling — and, for most values, without even restarting.

**In-game:** press <kbd>Ctrl</kbd>+<kbd>F10</kbd> to open the UMM window, find *Brick Loco*,
and edit the fields directly.

**On disk:** `Derail Valley/Mods/BrickLoco/Settings.xml`, written when you press *Save* in
the UMM window. You do not need to create it; defaults live in code (`Settings.cs`) and the
file appears on first save.

Settings are plain fields read every time the mod uses them, so edits in the UMM window
apply **immediately** — no restart. The exceptions are noted below.

---

## Physics and propulsion

| Setting | Type | Default | What it does |
| --- | --- | --- | --- |
| `MaxSpeed` | float | `20` | Speed cap in m/s. Above it, further force in that direction is ignored. The sign is ignored. Applies live. |
| `Force` | float | `7000` | Propulsion force in Newtons, applied every `FixedUpdate` while <kbd>G</kbd>/<kbd>H</kbd> is held. Applies live. |
| `Mass` | float | `20000` | Mass assigned to the spawned car's Rigidbody, in kg. **Applied once at spawn** — changing it after the car exists does nothing until the next session. |

`Force` and `Mass` interact: acceleration is roughly `a = F/m`, so **raising `Mass` makes
the same `Force` feel weaker**. If the car feels sluggish, either raise `Force` or lower
`Mass` — changing both at the same ratio changes nothing.

`MaxSpeed` gates per direction. Over the cap moving forward, <kbd>G</kbd> stops
contributing while <kbd>H</kbd> still works, so you can always brake. Setting `MaxSpeed = 0`
pins the car: neither key drives it once it is moving at all.

---

## Debug logging

| Setting | Type | Default | What it does |
| --- | --- | --- | --- |
| `MountTelemetry` | bool | `true` | Master switch for diagnostic logging: mount telemetry, key-down traces, jitter snapshots, and the <kbd>F9</kbd> dump. |
| `DumpOnMount` | bool | `false` | Also dump the full player component tree on every mount and dismount. Requires `MountTelemetry`. |

`MountTelemetry` is on by default because the mod is still in discovery. It is noisy —
every mounted <kbd>Ctrl</kbd>/<kbd>X</kbd>/<kbd>Space</kbd> press triggers a 30-frame
snapshot coroutine that logs 15 lines. Turn it off for normal play.

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
