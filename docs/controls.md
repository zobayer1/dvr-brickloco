# Controls

Every key BrickLoco binds. All are hardcoded — they are not yet configurable.

---

## In-game

| Key | Action | Available when |
| --- | --- | --- |
| <kbd>M</kbd> | Mount / dismount the brick car | Mount requires you within **5 m** of the seat |
| <kbd>G</kbd> | Hold to drive forward | **Mounted only** |
| <kbd>H</kbd> | Hold to drive in reverse | **Mounted only** |
| <kbd>F9</kbd> | Dump player component tree to the log | `MountTelemetry = true` |

### Mounting

<kbd>M</kbd> toggles. Mounting fails silently if the seat does not exist yet (the car has
not spawned) or if you are further than 5 m from it. With `MountTelemetry = true` the log
says which of the two it was:

```
[Info   :Brick Loco] TryMount aborted: too far (dist=8.13m). ...
```

The seat sits 2.5 m above the car origin, so measure from there, not from the cube.

### Driving

<kbd>G</kbd> and <kbd>H</kbd> apply a continuous force each physics step while held — they
are not impulses, so tapping does very little. Force magnitude is `Force`, and the car
stops accelerating at `MaxSpeed`. Both are tunable; see [Configuration](configuration.md).

Two things commonly surprise people:

- **Driving requires being mounted.** Standing next to the car and holding <kbd>G</kbd>
  does nothing.
- **The speed cap is directional.** Over the cap going forward, <kbd>G</kbd> stops
  contributing but <kbd>H</kbd> still works, so you can always slow down.

---

## Keys the mod reacts to but does not own

While mounted, these are still the game's keys — BrickLoco only watches them:

| Key | What BrickLoco does |
| --- | --- |
| <kbd>Ctrl</kbd>, <kbd>X</kbd>, <kbd>Space</kbd> | Opens the suppression window (crouch/jump/lean cause sink-and-reset jitter while mounted) |
| <kbd>W</kbd><kbd>A</kbd><kbd>S</kbd><kbd>D</kbd>, <kbd>Shift</kbd> | Logs mount telemetry on key-down, when `MountTelemetry = true` |

The suppression window is the mod's workaround for a jitter loop, not a rebind. What it
does and why is covered in [Mounting → The problem-key suppression window](mounting.md#the-problem-key-suppression-window).

---

## Reserved / not yet bound

Nothing else is bound. If you add a key, note that <kbd>M</kbd> is checked in `Update()`
unconditionally — including when not mounted — so new bindings should follow the same
pattern and guard on `isMounted` themselves.
