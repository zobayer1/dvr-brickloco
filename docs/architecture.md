# Architecture

How the plugin is laid out and what runs when.

---

## Shape of the code

```
src/BrickLoco/
  Loader.cs                 UMM entry point: settings, logging, host GameObject lifecycle
  Settings.cs               every tunable, rendered in the UMM window via [Draw]
  BrickLocoBehaviour.cs     the only MonoBehaviour: Unity lifecycle, input, wiring
  ModLog.cs                 adapter keeping LogInfo/LogWarning/LogError over UMM's logger

  Game/                     the game world
    PlayerRig.cs              locating and caching player transforms
    BrickCar.cs               a spawned car the mod has taken over
    BrickCarBuilder.cs        spawning, physics tuning, visuals, seat
    TransformPath.cs          transform-to-string for logs

  Mount/                    keeping the player on the seat
    MountController.cs        mount/dismount and per-frame enforcement
    DisabledScriptSet.cs      track / disable / restore a set of MonoBehaviours
    ProblemKeySuppressor.cs   the Ctrl/X/Space mitigation window

  Diagnostics/
    MountDiagnostics.cs       dumps, telemetry, jitter snapshots

  Logic/                    pure C#, zero UnityEngine references
    PropulsionPolicy.cs       speed gating
    MountScriptPolicy.cs      which scripts get disabled on mount
    TransformNaming.cs        interior / camera-holder name heuristics
    CameraLayers.cs           culling-mask arithmetic
    SuppressionWindow.cs      problem-key window timing
```

`BrickLocoBehaviour` is the **only** `MonoBehaviour`. Everything else is a plain C# object
it constructs in `Awake()` and drives from the Unity callbacks. That keeps engine lifecycle
concerns in exactly one file, and means the rest can be reasoned about — and in the case of
`Logic/`, tested — without a running game.

The `Logic/` split is load-bearing in a second way. Anything under `Logic/` is compiled
directly into the test assembly, so it can be verified with `dotnet test` in under a second
instead of a quit-deploy-launch-reproduce cycle. Anything touching `GameObject`,
`Transform`, `Input` or `Time` cannot be tested that way and stays in the plugin. See
[Testing](testing.md).

When you find yourself writing a decision — a threshold, a name match, a set of rules —
that decision belongs in `Logic/`. The plugin should read as *gather Unity state → ask a
policy → apply the answer*.

---

## Entry point

```csharp
public static bool Load(UnityModManager.ModEntry modEntry)   // Loader.cs
```

Unity Mod Manager finds the mod through `Mods/BrickLoco/Info.json` (generated at deploy
from csproj properties — `Id`, `Version`, `EntryMethod`) and calls `Loader.Load` at DV's
StartingPoint. `Loader` loads the settings, wires the UMM callbacks (`OnToggle`, `OnGUI`,
`OnSaveGUI`), and on enable creates a `DontDestroyOnLoad` host GameObject carrying
`BrickLocoBehaviour`:

```csharp
[DefaultExecutionOrder(10000)]
public class BrickLocoBehaviour : MonoBehaviour
```

Toggling the mod off in the UMM window destroys the host, which dismounts the player and
restores every disabled DV script (`OnDestroy`). The spawned car stays until restart.

`DefaultExecutionOrder(10000)` puts the behaviour's callbacks **after** DV's own scripts in
each frame. That is what makes the mounted-state enforcement work: the game's movement
scripts move the player, then BrickLoco puts them back.

---

## Startup sequence

| Step | Where | What happens |
| --- | --- | --- |
| 0 | `Loader.Load()` | UMM entry: loads Settings.xml, wires UMM callbacks, logs `BrickLoco loaded`. |
| 0b | `Loader.OnToggle(true)` | Creates the host GameObject with `BrickLocoBehaviour`. |
| 1 | `Awake()` | Constructs the rig, diagnostics and mount controller from `Loader`'s statics. |
| 2 | `Start()` | Kicks off the `WaitForPlayerAndSpawn` coroutine. |
| 3 | `PlayerRig.TryCache()` | Polled once per frame; returns false until an object tagged `Player` exists. |
| 4 | `PlayerRig` | Resolves and caches the player's controller root, camera, camera holder, `CharacterController` and `Rigidbody`. |
| 5 | `BrickCarBuilder.Spawn()` | Spawns the car, retunes it, restyles it, creates the seat. |

The frame-by-frame poll in step 3 exists because the plugin loads long before a save is
loaded — there is no player in the menu scene. The coroutine simply waits.

Because it never re-runs, **the car spawns exactly once per game session**, tied to
wherever the player was standing when the save finished loading.

---

## Spawn pipeline

`BrickCarBuilder.Spawn(position, mass, ...)` does four things, then hands back a `BrickCar`
wrapping the car and its seat:

**1. Find the spawner and the livery.**
`FindObjectOfType<CarSpawner>()`, then `FindLiveryById("FlatbedShort")` which scans
`Resources.FindObjectsOfTypeAll<TrainCarLivery>()` for a matching `id`. Every discovered id
is listed in [Liveries](liveries.md).

**2. Spawn on the nearest track.**

```csharp
spawner.SpawnCarOnClosestTrack(position, livery,
    flipRotation: false, playerSpawnedCar: true, uniqueCar: true);
```

`SpawnCarOnClosestTrack` — not a raw `Instantiate` — is what gets the car correctly bogied
and railed. Spawning off-track is not something the mod tries to do.

**3. Retune the Rigidbody.**

| Property | Value | Why |
| --- | --- | --- |
| `mass` | `Mass` config | Sets acceleration response together with `Force`. |
| `centerOfMass` | `(0, 0.5, 0)` | Lowers the COM to fight roll-over. |
| `constraints` | `FreezeRotationX \| FreezeRotationZ` | Hard-stops pitch and roll — the current stand-in for real bogie physics. |
| `interpolation` | `Interpolate` | Smooths visible motion between physics steps. |

The rotation freeze is a placeholder. It is also why the car cannot currently derail — see
[Roadmap](roadmap.md).

**4. Restyle and furnish.**
Every `Renderer` under the car is disabled and a red cube primitive is parented at local
`(0, 1.2, 0)`, scaled `(2, 1, 1)`. An empty `BrickLoco_Seat` transform is added at
`BrickCar.SeatLocalPosition` — local `(0, 2.5, 0)`.

Two details worth knowing:

- The cube's **collider is destroyed**. It is decoration; leaving it would fight the car's
  real colliders.
- The cube's **layer is derived from the camera**, via
  `CameraLayers.FirstVisibleLayer(cam.cullingMask)`. A primitive is created on layer 0,
  which the DV camera may not render — the cube would exist but be invisible.

---

## The per-frame loop

Three Unity callbacks in `BrickLocoBehaviour`, each delegating to `MountController`.

### `FixedUpdate()` — propulsion

Guards on a live car and `mount.IsMounted`, then calls `BrickCar.ApplyForwardForce` for
<kbd>G</kbd>/<kbd>H</kbd>. That projects current velocity onto the car's forward axis, asks
`PropulsionPolicy.ShouldApplyForce`, and calls `rb.AddForce(...)` if allowed.

### `Update()` — input and early enforcement

While mounted, `MountController.EnforceEarly()` runs in order: re-disable any scripts the
game re-enabled, re-pin the player to the seat, re-disable the `CharacterController`. Then
the plugin checks for problem keys. Enforcement runs *first* so movement scripts do not get
a chance to process this frame's input.

Outside the mounted branch it handles <kbd>M</kbd> (mount/dismount) and <kbd>F9</kbd> (dump).

### `LateUpdate()` — final enforcement

`MountController.EnforceLate()`, after everything else in the frame. Re-parents the
controller to the seat if something detached it, re-pins `localPosition`, applies the
suppression window, and re-enforces the disabled-script set.

The duplication between `Update` and `LateUpdate` is intentional: DV scripts run at both
points, so a single enforcement pass leaves a visible half-frame of drift.

---

## State

Mutable state lives with the object that owns it:

- **`PlayerRig`** — cached player transforms and components. Resolved once at startup;
  `CameraHolderTransform` is re-resolved at mount time, since the active camera rig changes
  when boarding.
- **`BrickCar`** — the spawned car and its seat. Created once per session.
- **`MountController`** — `IsMounted`, `originalPlayerParent`, `mountedLocalPosition` and the
  camera baselines. Valid only between mount and dismount.
- **`DisabledScriptSet`** and **`ProblemKeySuppressor`** — restore bookkeeping: each records
  every script it switched off along with that script's *prior* enabled state.

Everything the mod switches off records its prior value and is restored — on dismount, or
when the suppression window closes. Nothing is assumed to have been on.

---

## Known rough edges

Honest notes for whoever works on this next.

| Issue | Where |
| --- | --- |
| `PlayerRig.Rigidbody` is cached and only ever used in a log line. | `PlayerRig` |
| `MountedCameraLocalPosition` is a baseline captured only for log output. | `MountController` |
| `MountDiagnostics` reaches back into `MountController` for `Seat` and the baselines. Workable, but the dependency runs both ways. | `MountDiagnostics` |
| The `ProblemKeySuppressor` script list is six *suspects*, not a confirmed cause. Most of that class should disappear once the real culprit is found. | `ProblemKeySuppressor` |

`MountController` is still the largest file at ~400 lines. It has a clear internal split —
mount/dismount, enforcement, `CharacterController`, script disabling, interior lookup — and
could be divided further if it keeps growing.

---

## See also

- [Mounting](mounting.md) — the mount system in depth.
- [Testing](testing.md) — what the `Logic` split buys.
- [Game API Notes](game-api.md) — locating DV types.
