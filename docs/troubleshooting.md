# Troubleshooting

Work top-down: most problems are visible in the Unity Mod Manager console
(<kbd>Ctrl</kbd>+<kbd>F10</kbd> in-game) or its file at
`Derail Valley/DerailValley_Data/Managed/UnityModManager/Log.txt`, and the log tells you
which stage failed.

---

## Reading the log

A healthy startup looks like this:

```
[Manager] Reading file '...\Mods\BrickLoco\Info.json'.   <- UMM found the manifest
[BrickLoco] BrickLoco loaded                             <- Loader.Load ran
```

Then, after a save finishes loading:

```
[BrickLoco] Player cached. ControllerRoot: ..., Camera: ...
[BrickLoco] CarSpawner found: True
[BrickLoco] Spawned TrainCar: CarFlatcarShort(Clone)
[BrickLoco] Replaced TrainCar visuals with brick cube
```

Whichever line is *missing* tells you which section below to read.

---

## Unity Mod Manager never loaded

**Symptom:** no UMM window on <kbd>Ctrl</kbd>+<kbd>F10</kbd>, and
`Managed/UnityModManager/Log.txt` is missing or has an old modified time.

The usual cause: **a game update replaced the assembly UMM had patched**, silently
uninstalling it. Confirm in one command — zero means the patch is gone:

```powershell
Select-String -Path "D:\...\Derail Valley\DerailValley_Data\Managed\UnityEngine.CoreModule.dll" `
  -Pattern "UnityModManager" | Measure-Object | Select-Object Count
```

- Re-run `UnityModManager.exe`, pick Derail Valley, press **Install**. (This happened on
  this machine on 2026-07-23, after a game update on Feb 27 wiped the patch.)
- Prefer the **Doorstop Proxy** injection method if offered — it lives in `winhttp.dll`,
  which game updates do not touch, so it survives where Assembly patch does not.
- The installer must point at the same copy of the game you launch (watch out for a second
  Steam library).

---

## The mod is not loading

**Symptom:** UMM runs, but Brick Loco is missing from its window — or listed but inactive.

- **Listed, but disabled** → enable it. The checkbox state persists in UMM's `Params.xml`;
  a freshly deployed mod starts enabled, but one you disabled earlier stays disabled.
  (Every other mod in your list being disabled has the same cause.)
- **Not listed at all** → confirm the files are there:

  ```powershell
  Get-ChildItem "D:\Games\Steam\steamapps\common\Derail Valley\Mods\BrickLoco"
  ```

  Expected: `BrickLoco.dll`, `BrickLoco.pdb`, `Info.json`. Nothing there → the deploy did
  not run, or ran against a different install; run `.\scripts\deploy.ps1` and read the path
  it prints.
- **Listed with an error status** → hover the status in the UMM window and check `Log.txt`.
  Usual causes: `Info.json` names a `ManagerVersion` newer than your installed UMM
  (update UMM), or the DLL targets the wrong framework — it must be `net472`.

---

## Deploy fails

### "Derail Valley is running"

Expected behaviour. The running game holds a lock on the DLL. Quit it, or pass `-Force` to
try anyway (the copy will most likely still fail).

### "Derail Valley not found at '...'"

The build has the wrong game path. Set it for your machine:

```powershell
[Environment]::SetEnvironmentVariable('DERAIL_VALLEY_DIR', 'C:\Path\To\Derail Valley', 'User')
```

Restart your shell. Or override once: `.\scripts\deploy.ps1 -GameDir "C:\..."`.
Full resolution order: [Build & Deploy](build-and-deploy.md#pointing-at-your-game-install).

### "Unity Mod Manager not found under '...'"

The path points at a real game install, but UMM has not been installed against it.
See [Getting Started](getting-started.md#2-install-unity-mod-manager).

### The build cannot find `Assembly-CSharp` or `UnityEngine`

Same root cause — wrong `DerailValleyDir`. All eight references derive from it, so they
fail together. If only *some* fail, your DV version may have moved or renamed an assembly;
see [Game API Notes](game-api.md#updating-for-a-new-game-version).

### `MSB3275` + `CS0246: 'UnityModManagerNet' could not be found`

Appears after a UMM update: newer UMM builds ship a `0Harmony.dll` marked **net48**, which
makes MSBuild silently drop the `UnityModManager` reference from this net472 project. The
runtime pairing is fine (Unity's Mono ignores the attribute), so the csproj sets
`ResolveAssemblyReferenceIgnoreTargetFrameworkAttributeVersionMismatch=true` to make
build-time resolution ignore it too. If this error is back, that property was probably lost
in a csproj edit.

### `error CS0579: Duplicate 'TargetFrameworkAttribute'`

The mod project is compiling sources it should not — normally another project's `obj/`,
picked up by the SDK's default source glob. This is why the project lives in its own
directory (`src/BrickLoco/`) rather than at the repository root, where the glob would reach
`tests/`. Delete the stale `obj/` and rebuild; if it persists, check that nothing under
`src/BrickLoco/` belongs to another project.

### `error MSB1009` / the Deploy target is not found

You ran `dotnet build -t:Deploy` at the repo root, which resolves to `BrickLoco.sln` — and
the test project has no such target. Name the project:
`dotnet build src/BrickLoco/BrickLoco.csproj -t:Deploy`. The `.ps1` scripts already do.

---

## No car spawns

**Symptom:** the plugin loads, but no `Spawned TrainCar:` line appears.

The spawn waits for `GameObject.FindWithTag("Player")` and only runs **once per host
lifetime**, so:

- **In the main menu?** Nothing spawns until a save is loaded. Expected.
- **`CarSpawner found: False`** → the spawn ran before the world was ready. Toggle the mod
  off and on (see below) after the save has loaded.
- **`FlatbedShort livery not found!`** → the livery id changed in a DV update. Re-run
  discovery; see [Liveries](liveries.md#re-running-discovery).
- **`SpawnCarOnClosestTrack returned null`** → no track nearby. Walk closer to a rail and
  toggle the mod off and on.

### Respawning the car

Toggling **Brick Loco** off and back on in the UMM window (<kbd>Ctrl</kbd>+<kbd>F10</kbd>)
destroys and recreates the mod's host object, which re-runs the spawn — a new cube appears
on the track closest to you. Use it after deleting the car, or if it spawned somewhere
inconvenient.

Reloading a save does **not** respawn: the host survives scene loads
(`DontDestroyOnLoad`) and its spawn coroutine has already completed, so you come back to
no car until you toggle or restart.

---

## The car spawned but is invisible

The cube's layer is derived from the main camera's culling mask at spawn time. If
`Camera.main` was null or unusual at that moment, the cube can land on a layer the camera
does not render.

Look for `Replaced TrainCar visuals with brick cube` — if present, the cube exists. Check
the log for `Spawned TrainCar position:` (needs `MountTelemetry = true`) and compare against
where you actually are. The car spawns on the closest track, which may be behind you.

---

## Mounting does nothing

Press <kbd>M</kbd> with `MountTelemetry = true` and read the log:

| Log line | Cause |
| --- | --- |
| `TryMount aborted: too far (dist=...)` | You are beyond 5 m. The seat is 2.5 m *above* the car origin — measure to there. |
| `TryMount aborted: seat=null` | The car never spawned. See above. |
| *(no line at all)* | The key was not seen — another mod or the game consumed it. |

---

## Mounted, but the player sinks or jitters

Known issue, and the reason most of the `[BrickLoco.Mount]` config section exists. It
triggers on <kbd>Ctrl</kbd>, <kbd>X</kbd> and <kbd>Space</kbd> while mounted.

Things to try:

- Raise `SuppressProblemKeysSeconds` (default `1.5`) — a longer window covers more of it.
- Confirm `DisableCharacterControllerWhileMounted = true`.
- Check the log for `missing=` in the `Disabled scripts while mounted:` line. A non-zero
  count means a configured script name matched nothing, usually a rename in a DV update.

Full background: [Mounting](mounting.md#the-problem-key-suppression-window). If you can
capture `[JitterSnap #n]` lines from a reproduction, they are the most useful thing to
attach to a bug report.

---

## The log is drowning in output

`MountTelemetry = true` is the default because the mod is still in discovery. Each mounted
problem-key press starts a 30-frame snapshot coroutine that logs 15 lines.

Turn `MountTelemetry` off in the UMM window (<kbd>Ctrl</kbd>+<kbd>F10</kbd>) — it applies
immediately, no restart.

---

## Settings changes do nothing

Most settings apply live, but two do not:

- `Mass` is applied **once at spawn** — it only takes effect in the next session.
- The script-disable lists are evaluated at **mount time** — dismount and remount.

If you edited `Settings.xml` by hand while the game was running, the in-game values win and
overwrite the file on save. Edit in the UMM window instead, or with the game closed.

A `BepInEx/config/com.zobayer.brickloco.cfg` from before the UMM migration is **not read
anymore** — edits there do nothing.

---

## Starting clean

```powershell
.\scripts\undeploy.ps1 -PurgeConfig    # remove the mod (Settings.xml goes with the folder)
dotnet clean
.\scripts\deploy.ps1
```

Then launch and re-check the log from the top of this page.
