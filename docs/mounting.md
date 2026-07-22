# Mounting

The mount system, and why it is the most defensive code in the mod.

---

## The problem

Derail Valley's player is not a simple transform you can parent to a moving object. It is a
`CharacterController` driven by a stack of scripts that continuously assert where the player
should be — walking, crouching, jumping, snapping to walkable surfaces, enforcing world
boundaries, and re-parenting the player when they board rolling stock.

Parenting the player to a seat on a moving car puts BrickLoco in direct conflict with all of
them. The mod's answer is not to fight each frame but to **temporarily switch the
conflicting scripts off, remembering their prior state, and restore them on dismount**.

---

## Mount sequence

<kbd>M</kbd> → `TryMount()`:

1. **Preconditions.** A seat and a player root must exist. With `MountTelemetry` on, a
   failure logs which precondition failed.
2. **Proximity check.** Distance from the controller root to the seat must be ≤ **5 m**.
   Hardcoded — not currently a config value.
3. **Seat placement.** The seat is parented under the spawned car (or under the car's
   interior transform if the player is already parented there), then positioned at car-local
   `(0, 2.5, 0)` with the car's rotation.
4. **Attach.** `playerControllerRoot.SetParent(seatTransform, worldPositionStays: false)`
   and `localPosition = Vector3.zero` — a clean snap to the seat origin.
5. **Baselines.** Camera-holder and camera local positions are captured for the suppression
   window; all suppression bookkeeping is reset.
6. **Disable.** The `CharacterController` is disabled (if configured), then the script
   disable set is applied.

### Why prefer the interior transform

If the player is already parented under the car's interior, the seat is moved there too.
DV interiors are a *smoothed* frame — they lag the rigidbody root slightly and carry less
per-step physics jitter. Mounting to the smoothed frame gives a visibly steadier ride than
mounting to the raw rigidbody root.

Whether a transform counts as "this car's interior" is decided by
`TransformNaming.IsInteriorName` plus either a car-name prefix match or a hierarchy check.
The name test alone is not enough: two cars of the same livery differ only by clone suffix,
and matching loosely would attach the seat to a *different* car's interior.

---

## Staying mounted

Attaching is the easy part. Staying attached takes enforcement in two callbacks per frame:

| Guard | What it undoes |
| --- | --- |
| Re-parent to seat | A DV script detached the controller from the seat |
| Re-pin `localPosition` | Something moved the player within the seat frame |
| Re-disable scripts | The game re-enabled a script the mod had switched off |
| Re-disable `CharacterController` | The game re-enabled it — typically on crouch/jump |

`Update()` enforces early, so the game's movement scripts see the mounted state before they
process this frame's input. `LateUpdate()` enforces again after everything else has run.
Both are needed; with only one, the player visibly drifts for part of each frame.

Re-disable events are logged at most once per second, to keep a per-frame fight from
flooding the log.

---

## The script disable set

Built by `MountScriptPolicy.BuildDisableSet` from two config strings, then adjusted by two
rules the config cannot override:

- **Always added:** `LocomotionInputWrapper`, `CharacterReparenting`. These are what stop
  the player walking off the seat. An empty config still gets them.
- **Always removed:** `CustomFirstPersonController`. It drives looking around; disabling it
  leaves you mounted with a frozen camera. Naming it in either config list is ignored.

Matching is by **short type name**, case-sensitive, against MonoBehaviours under the player
controller root. Requested names that match nothing are reported rather than treated as an
error:

```
Disabled scripts while mounted: disabledNow=3, matched=3, missing=1 (cfg=(...) + critical=(...))
[MountDisable] Actually disabled: LocomotionInputWrapper,CharacterReparenting,CameraAnchorLeanCrouch
[MountDisable] Requested but not found under controller: SomeTypo
```

Every disabled script is recorded with its **prior** enabled state, so restore puts back
what was there — a script that was already disabled before the mount stays disabled after.

---

## The problem-key suppression window

### Symptom

While mounted, pressing <kbd>Ctrl</kbd>, <kbd>X</kbd> or <kbd>Space</kbd> — crouch, lean,
jump — triggers a sink-and-reset loop: the player drops through the seat, gets snapped back,
and oscillates.

### Mitigation

Those keys open a time-boxed window (`SuppressProblemKeysSeconds`, default 1.5 s). While it
is open, the mod:

1. **Pins the camera holder's `localPosition`** to its mount-time baseline — but only if the
   holder belongs to the same interior/car as the seat, so unrelated camera rigs are left
   alone.
2. **Temporarily disables six scripts**, restoring each when the window closes:

   | Script | Suspected role in the loop |
   | --- | --- |
   | `CameraAnchorLeanCrouch` | Applies lean/crouch camera offsets |
   | `MovementFlagUpdater` | Recomputes movement state flags |
   | `FallThroughTerrainFix` | Teleports the player up out of geometry |
   | `TeleportForbiddenOverlapSafety` | Teleports the player out of forbidden overlaps |
   | `WalkableControlOverlapDisabler` | Toggles walkable-surface controls on overlap |
   | `WorldBoundaryEnforcer` | Pushes the player back inside world bounds |

Windows **extend rather than replace**: mashing keys cannot shorten a window already
running longer (`SuppressionWindow.Extend`).

### Scope

The window deliberately does **not** touch rotation and does not disable the look
controller. Camera freedom is preserved throughout — only positional offsets are pinned,
and only for the window's duration.

### Status

This is a mitigation built from observed behaviour, not a fix — the six scripts are
*suspects* narrowed down by log analysis, not a confirmed root cause. `MountTelemetry`
exists to keep narrowing it: each problem-key press starts a 30-frame jitter snapshot
(`MountDiagnostics`) that logs the player's local position, seat delta, camera and
camera-holder positions, and the enabled state of all seven watched scripts —
the six above plus `CharacterControllerMover`, which is watched but never disabled.

Once the actual culprit is identified, most of this should collapse to a targeted fix.

---

## Dismount

<kbd>M</kbd> again → `Dismount()`:

1. `isMounted = false`
2. Restore every recorded script to its prior enabled state.
3. Restore the `CharacterController` — but only if *this* mount disabled it.
4. `SetParent(originalPlayerParent, worldPositionStays: true)` — the player keeps their
   world position rather than being teleported to wherever the old parent is now.

---

## If you are debugging this

1. Set `MountTelemetry = true` (default).
2. Set `DumpOnMount = true` to get the full component tree at mount and dismount.
3. Press <kbd>F9</kbd> any time for an on-demand dump.
4. Reproduce, then read `DerailValley_Data/Managed/UnityModManager/Log.txt` for
   `[JitterSnap #n]` lines — they show
   frame-by-frame drift and which scripts were enabled at each frame.

Log tags to grep for: `[MountTelemetry]`, `[MountedKey]`, `[JitterSnap`, `[MountDisable]`,
`[Mitigation]`, `[Dump]`.
