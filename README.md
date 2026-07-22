# BrickLoco

A LEGO-style locomotive mod for **Derail Valley**, built on **Unity Mod Manager** and
Unity 2019.4.

It spawns a real `TrainCar` on the track nearest the player, replaces its visuals with a
placeholder brick, and lets you mount it and drive it with the keyboard.

> **Status: early.** The brick is a red cube, there are no wheels yet, and the mount system
> is still fighting the game's movement scripts. See the [roadmap](docs/roadmap.md).

---

## Requirements

- Derail Valley (Windows)
- [Unity Mod Manager](https://www.nexusmods.com/site/mods/21) (0.27+), installed against
  Derail Valley — the loader the DV modding ecosystem (Custom Car Loader, ZCouplers, …) runs on
- .NET SDK 8 (or 6) and the .NET Framework 4.7.2 Developer Pack

First time on this machine? Follow [Getting Started](docs/getting-started.md).

---

## Build

```bash
dotnet build
```

The build reads DV's assemblies straight out of your game install. If the path differs from
the repo default, set it once and restart your shell:

```powershell
[Environment]::SetEnvironmentVariable('DERAIL_VALLEY_DIR', 'C:\Path\To\Derail Valley', 'User')
```

## Deploy

```powershell
.\scripts\deploy.ps1        # build + install into Mods/BrickLoco/ (with generated Info.json)
.\scripts\undeploy.ps1      # remove it again
```

A plain `dotnet build` never touches your game folder — deployment is always explicit.
Both scripts refuse to run while the game is open, since it holds a lock on the DLL.

Details, flags, and the MSBuild targets underneath: [Build & Deploy](docs/build-and-deploy.md).

## Test

```bash
dotnet test
```

55 tests over the mod's pure decision logic. No game required. See [Testing](docs/testing.md).

---

## Run

1. Launch Derail Valley. Press <kbd>Ctrl</kbd>+<kbd>F10</kbd> and make sure **Brick Loco**
   is enabled (status *Active*).
2. Load a save. Look for a red cube on the nearest track — that is the brick car.
3. Walk within 5 m and press <kbd>M</kbd> to mount.
4. Hold <kbd>G</kbd> to drive forward, <kbd>H</kbd> to reverse. <kbd>M</kbd> again to dismount.

Verify it loaded in the UMM console (<kbd>Ctrl</kbd>+<kbd>F10</kbd>) or in
`Derail Valley/DerailValley_Data/Managed/UnityModManager/Log.txt`:

```
[BrickLoco] BrickLoco loaded
[BrickLoco] Spawned TrainCar: CarFlatcarShort(Clone)
```

Nothing there? [Troubleshooting](docs/troubleshooting.md).

## Configure

Open the UMM window (<kbd>Ctrl</kbd>+<kbd>F10</kbd>) and edit Brick Loco's settings
in-game — most apply immediately, no restart. Saved to `Mods/BrickLoco/Settings.xml`.

The three you will reach for first are `MaxSpeed` (20 m/s), `Force` (7000 N) and `Mass`
(20000 kg). Every setting is documented in [Configuration](docs/configuration.md).

---

## Documentation

Full docs live in **[docs/](docs/README.md)**.

**Using the mod**
- [Getting Started](docs/getting-started.md) — one-time machine setup
- [Controls](docs/controls.md) — every key the mod binds
- [Configuration](docs/configuration.md) — every `.cfg` key and what it changes
- [Troubleshooting](docs/troubleshooting.md) — when it does not load, spawn, or mount

**Working on the mod**
- [Build & Deploy](docs/build-and-deploy.md) — the full workflow
- [Architecture](docs/architecture.md) — layout, startup, the per-frame loop
- [Mounting](docs/mounting.md) — the mount system and the jitter fight
- [Game API Notes](docs/game-api.md) — finding DV types with ILSpy
- [Liveries](docs/liveries.md) — every `TrainCarLivery` id
- [Testing](docs/testing.md) — what is tested and why the rest is not
- [Roadmap](docs/roadmap.md) — done, in progress, next

---

## Repository layout

```
src/BrickLoco/
  BrickLoco.csproj        mod assembly (net472, C# 7.3 — fixed by the game's runtime)
  Loader.cs               UMM entry point (Info.json EntryMethod)
  Settings.cs             every tunable, rendered in the UMM window
  BrickLocoBehaviour.cs   the only MonoBehaviour: Unity lifecycle, input, wiring
  ModLog.cs               logging adapter
  Game/                   player rig, car spawning, visuals
  Mount/                  mount state, script suppression
  Diagnostics/            dumps, telemetry, jitter snapshots
  Logic/                  pure C#, no UnityEngine — this is what the tests cover
tests/BrickLoco.Tests/    xUnit (net8.0)
scripts/                  deploy.ps1, undeploy.ps1
docs/
```

Standard .NET layout, not a Unity project layout — BrickLoco is a class library that
references Unity assemblies and is never opened in the Unity editor.

---

## License

[MIT](LICENSE)
