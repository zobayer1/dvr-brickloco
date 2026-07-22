# Getting Started

One-time setup for a machine that will build BrickLoco. If you only want to *run* the mod,
you need none of this — grab a built DLL and read [Build & Deploy](build-and-deploy.md).

---

## 1. Prerequisites

| Software | Version | Why |
| --- | --- | --- |
| Derail Valley | Windows build | The target game. |
| .NET SDK | 8 (or 6) | The `dotnet` CLI. |
| .NET Framework Developer Pack | 4.7.2 | Reference assemblies the mod compiles against. |
| Unity Mod Manager | 0.27+ | The mod loader the DV ecosystem uses. |
| Unity | 2019.4 LTS | Optional, inspection only. |
| ILSpy | any recent | Optional, for reading DV's assemblies. |

The mod DLL targets **.NET Framework 4.7.2** and **C# 7.3**. These are not preferences —
they are what the game's Mono runtime accepts. See [Known Constraints](#known-constraints).

---

## 2. Install Unity Mod Manager

UMM is the loader the Derail Valley modding ecosystem runs on — Custom Car Loader,
ZCouplers, Skin Manager and the rest are all UMM mods, declare dependencies on each other
through it, and will not load under anything else.

1. Download **Unity Mod Manager** from
   [nexusmods.com/site/mods/21](https://www.nexusmods.com/site/mods/21).
2. Run `UnityModManager.exe`, pick **Derail Valley** in the game list, point it at your
   install folder, and press **Install**.
3. Launch Derail Valley once. The UMM window opens on start (later: <kbd>Ctrl</kbd>+<kbd>F10</kbd>).

Afterwards the install should contain:

```
Derail Valley/
├─ Mods/                                <- one folder per mod, each with an Info.json
├─ DerailValley_Data/
│  └─ Managed/
│     └─ UnityModManager/
│        ├─ UnityModManager.dll        <- referenced by BrickLoco.csproj
│        ├─ 0Harmony.dll
│        └─ Log.txt                    <- the loader + mod log
└─ DerailValley.exe
```

If `Mods/` or the `UnityModManager` folder is missing, the installer did not run against
this copy of the game. See [Troubleshooting](troubleshooting.md#unity-mod-manager-never-loaded).

---

## 3. Install Unity (optional, reference only)

Unity is used to *inspect* prefabs, axes and components. It never builds anything here.

1. In Unity Hub, install **Unity 2019.4 LTS**.
   - Enable **Windows Build Support (Mono)**.
   - Disable WebGL, mobile, IL2CPP — none are needed.
2. Optionally create a throwaway project to open assets in.

> ⚠️ Do not use Unity 6.x / 2022+. Serialized data and component layouts will not match
> what the game ships, so anything you learn there may be wrong.

---

## 4. Clone and build

```bash
git clone <this repo>
cd BrickLoco
dotnet build
```

The build resolves DV assemblies from your install directory. If it fails with missing
references, your game path differs from the repo default — set it once:

```powershell
[Environment]::SetEnvironmentVariable('DERAIL_VALLEY_DIR', 'C:\Path\To\Derail Valley', 'User')
```

Restart your shell, then `dotnet build` again. Path resolution is explained in full under
[Build & Deploy → Pointing at your game install](build-and-deploy.md#pointing-at-your-game-install).

---

## 5. Deploy and verify

```powershell
.\scripts\deploy.ps1
```

Launch the game and open the UMM window (<kbd>Ctrl</kbd>+<kbd>F10</kbd>): **Brick Loco**
should be listed — enable it if it is not. The console there (and
`DerailValley_Data/Managed/UnityModManager/Log.txt`) should show:

```
[BrickLoco] BrickLoco loaded
```

That confirms UMM found `Mods/BrickLoco/Info.json`, loaded the DLL, and ran the entry
method. Shortly after loading a save you should also see:

```
[BrickLoco] CarSpawner found: True
[BrickLoco] Spawned TrainCar: CarFlatcarShort(Clone)
[BrickLoco] Replaced TrainCar visuals with brick cube
```

Walk up to the red cube and press <kbd>M</kbd> to mount. Full list in [Controls](controls.md).

---

## Known Constraints

These are fixed by the game's runtime, not by choice:

- **Unity 2019.4** — the engine version DV ships.
- **C# 7.3** — no nullable reference types, no global usings, no file-scoped namespaces,
  no switch expressions, no records.
- **net472** — the mod assembly's target framework.

The one exception is the test project, which targets `net8.0` because it never touches
Unity. See [Testing](testing.md#why-the-tests-target-net80).

---

## Next steps

- [Architecture](architecture.md) — what the plugin actually does, in order.
- [Configuration](configuration.md) — tune behaviour without recompiling.
- [Game API Notes](game-api.md) — how to find the DV type you need.
