# Roadmap

Where the project is and what comes next.

---

## Done

- [x] Mod loader working end to end (BepInEx originally; migrated to Unity Mod Manager
      2026‑07 — see [Direction](#direction-2026-07))
- [x] Mod loads and logs from its entry point
- [x] Explicit deploy / undeploy workflow — scripts, MSBuild targets, VS Code tasks
- [x] Reference DV gameplay assemblies (`Assembly-CSharp`, `DV.ThingTypes`, `DV.Utils`)
- [x] Discover `TrainCarLivery` assets at runtime ([Liveries](liveries.md))
- [x] Spawn a real `TrainCar` on the closest track
- [x] Replace car visuals with a placeholder cube
- [x] Tune the car Rigidbody (mass, centre of mass, rotation constraints)
- [x] Mount / dismount with a proximity gate
- [x] Hold <kbd>G</kbd>/<kbd>H</kbd> for speed-limited propulsion
- [x] Expose tuning through in-game settings (UMM window, live-editable)
- [x] Verify coupling and handbrake still work on the spawned car
- [x] Extract pure decision logic and unit test it ([Testing](testing.md))
- [x] Split the plugin into single-responsibility classes ([Architecture](architecture.md))

- [x] Drive through the game's traction path (`Bogie.ApplyForce`) instead of shoving the
      carbody — the suspected root cause of the on-track jitter
- [x] Keep the car's real bogies/wheels visible instead of hiding every renderer
- [x] Physics tunables (`Mass`, `ComHeight`, `FreezeCarTilt`, `DriveViaBogies`) apply live
      from the UMM window
- [x] **Let the game own car physics** (`LetGameOwnPhysics`, default on). Overriding
      `rb.mass` bypassed `TrainMassController`, which desynced the bogie suspension joints and
      sank the wheels into the rail (only became visible once the wheels were shown). Handing
      mass/COM/constraints back to the game makes the car sit, drive, rerail and derail like a
      vanilla flatcar. The `Mass`/`ComHeight`/`FreezeCarTilt` overrides are now legacy A/B only.

## In progress

- [ ] **Fix the mounted jitter loop.** Confirmed 2026-07-23 to be the mount enforcement
      fight, not car physics: the car is smooth in every detached view (F2/F3, bystander),
      a rerailed tilted car keeps juddering *only while mounted*, and unmounting stops it
      immediately. The per-frame re-pin + CharacterController re-disable loop is the cause.
      Deliberately **not** being chased before the CCL pivot — a real CCL cab boards the
      player through the game's own system and removes the pinning fight entirely. What ships
      today is a [time-boxed mitigation](mounting.md#the-problem-key-suppression-window).

---

## Next

### Physics
- Retire the legacy override path (`Mass`, `ComHeight`, `FreezeCarTilt`) once
  `LetGameOwnPhysics` has proven itself over more play — it is A/B scaffolding now.

### Spawning
- Respawn on save reload. Spawn runs once per mod-host lifetime, so reloading a save
  destroys the car without spawning a new one; the workaround is toggling the mod off/on in
  UMM. A CCL-registered car spawns through the game's own system and sidesteps this.

### Mounting
- Find the actual cause of the jitter loop using `[JitterSnap]` telemetry, then delete most
  of the suppression machinery.
- Make the 5 m proximity radius configurable.
- Give the seat a real position instead of a hardcoded `(0, 2.5, 0)` offset.

### Visuals
- Replace the placeholder cube with LEGO-style meshes via a Unity asset workflow.
  See [Replacing the placeholder model](#replacing-the-placeholder-model) below.

### Controls
- Make keybindings configurable rather than hardcoded.
- Consider a proper interaction prompt for mounting instead of a bare <kbd>M</kbd>.
- <kbd>F</kbd> does not exit the brick car the way it does on vanilla locos (the custom
  mount bypasses DV's boarding system, so vanilla exit mappings never see it). Deferred to
  the player movement-mapping work; a real CCL cab would likely give this for free.

---

## Cleanup worth doing

Tracked in more detail under
[Architecture → Known rough edges](architecture.md#known-rough-edges):

- Untangle the two-way dependency between `MountDiagnostics` and `MountController`.
- Split `MountController` (~400 lines) further if it keeps growing — it already has clear
  internal sections.
- Remove `PlayerRig.Rigidbody` and `MountedCameraLocalPosition` if they never grow a use
  beyond log output.

---

## Replacing the placeholder model

Not started. Recorded here because it is the largest pending change and it reaches outside
the C# codebase.

**First check Custom Car Loader** (see [Direction](#direction-2026-07)) — its authoring
pipeline (Unity 2019.4.40 + `CarCreator.unitypackage` → assetbundle) may make most of the
table below unnecessary.

Failing that, the hand-rolled route: a mesh cannot be created in code the way
`GameObject.CreatePrimitive` makes the cube. Author the model in **Unity 2019.4**, export
an **AssetBundle** built for `StandaloneWindows64`, ship it beside the DLL, and load it at
runtime with `AssetBundle.LoadFromFile` + `LoadAsset<GameObject>`.

What that changes here:

| Area | Change |
| --- | --- |
| `BrickCarBuilder` | `ReplaceVisualsWithCube` becomes "load bundle, instantiate; fall back to the cube". |
| Deploy | The `Deploy` target copies DLL + PDB only. It must also copy the `.assetbundle`. `Undeploy` already removes the whole directory, so that side needs nothing. |
| Unity | Currently "reference only, optional". It becomes a **hard requirement at exactly 2019.4** — a bundle built by another version returns null from `LoadFromFile`. |
| Layers | `cube.layer = ...` sets one object. A model with child meshes needs the layer applied down the whole hierarchy. `CameraLayers.FirstVisibleLayer` still supplies the value. |
| Colliders | The cube's collider is destroyed outright. A real model needs a deliberate decision per mesh. |

Two things to plan for rather than discover:

- **Shaders are the usual failure.** Materials in a bundle reference shaders by name, and a
  shader compiled in your own Unity project frequently does not survive into the game's
  render setup — the model renders magenta. The normal fix is to rebind materials at runtime
  to shaders taken from the game itself (for example off the original car's renderers before
  disabling them) rather than trusting what the bundle carries.
- **Keep the cube as a fallback.** If the bundle is missing or refuses to load, falling back
  to the placeholder plus a loud log line beats an invisible car that looks like the mod
  failed to start.

The restructure already isolates this: visuals live entirely inside `BrickCarBuilder`, so
nothing in `Mount/`, `Diagnostics/` or the plugin needs to change. Bundle path resolution
and the "which renderers to hide" predicate are both pure decisions that belong in `Logic/`
where they can be unit tested.

---

## Direction (2026-07)

The end goal is **Zobayer's own engine models running in the game with vanilla behaviour** —
shared simulation, coupling, rail interaction, HUD and mount/unmount — with art authored in
FreeCAD/SolidWorks + Blender and audio recorded separately.

The Derail Valley ecosystem already has the platform for exactly that:
**[Custom Car Loader](https://github.com/derail-valley-modding/custom-car-loader)** (CCL),
a UMM mod that adds custom cars and locomotives with working simulation, cab controls and
indicators. That is why this repo migrated from BepInEx to Unity Mod Manager: every
relevant mod (CCL, ZCouplers, Skin Manager) is UMM, and inter-mod dependencies
(`LoadAfter` in Info.json) only work inside one loader.

The full authoring setup and the export/spawn workflow are written up in
[Custom Cars (CCL)](custom-cars.md).

Planned sequence:

1. Take one model through the CCL pipeline end to end, before writing more C#.
   - [x] **Stock flatbed** exported and spawned green in-game (validates the whole
         Unity → bundle → mod → spawn chain). — *done 2026-07*
   - [x] **Custom flat-board body on vanilla bogies** — one custom Blender mesh with a
         solid-colour Standard material, swapped into the `_template` prefab's `Model` slot,
         riding coupled and walkable in-game. Isolated and proved the mesh/material path
         (no magenta; shader survives the bundle). — *done 2026-07*
   - [ ] **Custom bogie mesh** — `BogieF/BogieR → bogie_car → [axle]*` hierarchy with
         `Bogie2BrakePads`, built to the vanilla wheelbase and **0.459 m** wheel radius;
         wheels spun by the game's `WheelRotationViaCode`, never hand-animated; axle origins
         on the centreline; +Z forward; livery bogie option set to **Custom**.
2. Decide where custom logic lives: CCL sim definitions, a companion UMM mod, or
   contributions to CCL itself.
3. Keep this repo as the logic/tooling learning ground; retire the parts CCL makes
   unnecessary (most of `Mount/`).

## Not planned

- BepInEx support. The mod migrated to Unity Mod Manager in July 2026; the BepInEx-era
  install is swept up by `Undeploy`.
- Multiplayer / networking.
- Steam Workshop or mod-manager packaging, until the mod does something worth distributing.
