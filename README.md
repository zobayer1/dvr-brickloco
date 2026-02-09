# BrickLoco – Derail Valley Mod Setup Guide

This document describes the **exact steps** required to set up a working
C# mod project for **Derail Valley** using **BepInEx**, **Unity (reference-only)**, and **.NET**.

The goal is a **clean, minimal, reproducible setup** that loads a plugin DLL
into the game and proves end-to-end integration by spawning a real `TrainCar`,
replacing its visuals with a placeholder cube, and applying simple keyboard-controlled propulsion.

---

## Milestones

- [x] BepInEx installed and running (LogOutput.log created)
- [x] Plugin loads and logs from `Awake()`
- [x] Explicit deploy workflow (manual MSBuild target + VS Code task)
- [x] Spawn a real `TrainCar` and replace visuals with a visible cube
- [x] Reference DV gameplay assemblies (e.g., `Assembly-CSharp.dll`) for deeper integration
- [x] Hook into gameplay events / world systems
- [x] Hold `G` / `H` to apply forward / reverse propulsion to spawned car (speed-limited, tunable force, only when nearby)
- [ ] Make the cube a stable body

---

## 1. Prerequisites

### Software
- Derail Valley (Windows)
- Unity Hub
- Unity **2019.4 LTS** (reference-only; match the game's major version; DV currently reports Unity 2019.4.x in LogOutput.log)
- VS Code
- .NET Framework **4.7.2 Developer Pack**
- .NET SDK **6 or 8** (for `dotnet` CLI only)

### Notes
- Unity is used **only for inspection and reference**, not for building the game.
- The mod DLL must target **.NET Framework 4.7.2** and **C# 7.3**.

---

## 2. Install Unity (Reference Only)

1. Open Unity Hub
2. Install **Unity 2019.4 LTS**
   - Enable: **Windows Build Support (Mono)**
   - Disable: WebGL, Mobile, IL2CPP
3. (Optional) Create a throwaway project named `DV_Reference_2020`
   - Used only to inspect prefabs, axes, and components

⚠️ Do NOT use Unity 6.x / 2022+ — serialized data and components will not match.

---

## 3. Install BepInEx

1. Download **BepInEx 5.x – Windows x64**
   - https://github.com/BepInEx/BepInEx/releases
2. Extract **all contents** directly into the Derail Valley install folder
3. Allow overwriting:
   - `winhttp.dll`
   - `doorstop_config.ini`
4. Launch Derail Valley once, then quit

After first launch, the folder structure should include:

```

Derail Valley/
├─ BepInEx/
│  ├─ core/
│  ├─ plugins/
│  ├─ config/
│  └─ LogOutput.log

```

---

## 4. Create the Mod Project

```bash
dotnet new classlib -n BrickLoco
cd BrickLoco
code .
```

Edit `BrickLoco.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net472</TargetFramework>
    <LangVersion>7.3</LangVersion>
    <Nullable>disable</Nullable>
    <ImplicitUsings>false</ImplicitUsings>

    <!-- Used by the optional deploy/clean MSBuild targets (see section 7) -->
    <DeployToDerailValley>false</DeployToDerailValley>
    <DerailValleyPluginsDir>C:\\Path\\To\\Derail Valley\\BepInEx\\plugins</DerailValleyPluginsDir>
  </PropertyGroup>
</Project>
```

Clean and build once:

```bash
dotnet clean
dotnet build
```

Expected result:

```
Build succeeded.
```

---

## 5. Add Required DLL References

### 5.1. Using ILSpy to discover classes/namespaces (workflow)

Derail Valley modding often involves using game types that have little/no public documentation.
The practical workflow is:

1. Open the game's managed assemblies in an inspector/decompiler (I use **ILSpy**).
  - Folder: `Derail Valley/DerailValley_Data/Managed/`
2. Search for the type name you care about (examples: `CarSpawner`, `TrainCar`, `TrainCarLivery`).
3. Note two things:
  - The **namespace** (what you `using ...;`)
  - The **assembly** it lives in (which DLL you must reference in `BrickLoco.csproj`)
4. Add the DLL as a `<Reference>` in `BrickLoco.csproj` (with a matching `HintPath`).

This is also useful for discovering related types (components, methods, fields) by browsing the class tree around the type you found.
It’s the fastest way to answer: “Which DLL contains this class?”

Update `BrickLoco.csproj` to reference game assemblies:

NOTE: The `HintPath` values must match your Derail Valley install path.
In this repo they are set to the author's local Steam library path.

```xml
<ItemGroup>
  <Reference Include="BepInEx">
    <HintPath>
      C:\Program Files (x86)\Steam\steamapps\common\Derail Valley\BepInEx\core\BepInEx.dll
    </HintPath>
  </Reference>

  <Reference Include="Assembly-CSharp">
    <HintPath>
      C:\Program Files (x86)\Steam\steamapps\common\Derail Valley\DerailValley_Data\Managed\Assembly-CSharp.dll
    </HintPath>
  </Reference>

  <Reference Include="DV.ThingTypes">
    <HintPath>
      C:\Program Files (x86)\Steam\steamapps\common\Derail Valley\DerailValley_Data\Managed\DV.ThingTypes.dll
    </HintPath>
  </Reference>

  <Reference Include="DV.Utils">
    <HintPath>
      C:\Program Files (x86)\Steam\steamapps\common\Derail Valley\DerailValley_Data\Managed\DV.Utils.dll
    </HintPath>
  </Reference>

  <Reference Include="UnityEngine">
    <HintPath>
      C:\Program Files (x86)\Steam\steamapps\common\Derail Valley\Derail Valley_Data\Managed\UnityEngine.dll
    </HintPath>
  </Reference>

  <Reference Include="UnityEngine.CoreModule">
    <HintPath>
      C:\Program Files (x86)\Steam\steamapps\common\Derail Valley\Derail Valley_Data\Managed\UnityEngine.CoreModule.dll
    </HintPath>
  </Reference>

  <Reference Include="UnityEngine.PhysicsModule">
    <HintPath>
      C:\Program Files (x86)\Steam\steamapps\common\Derail Valley\Derail Valley_Data\Managed\UnityEngine.PhysicsModule.dll
    </HintPath>
  </Reference>

  <Reference Include="UnityEngine.InputLegacyModule">
    <HintPath>
      C:\Program Files (x86)\Steam\steamapps\common\Derail Valley\Derail Valley_Data\Managed\UnityEngine.InputLegacyModule.dll
    </HintPath>
  </Reference>
</ItemGroup>
```

(Adjust paths if the game is installed elsewhere.)

Reload VS Code and rebuild:

```bash
dotnet build
```

---

## 6. Create the Plugin Entry Point

Delete `Class1.cs`.

Create `src/BrickLocoPlugin.cs`:

```csharp
using BepInEx;
using UnityEngine;

namespace BrickLoco
{
    [BepInPlugin("com.zobayer.brickloco", "Brick Loco", "0.0.1")]
    public class BrickLocoPlugin : BaseUnityPlugin
    {
        private void Awake()
        {
            Logger.LogInfo("BrickLoco loaded");
        }
    }
}
```

Build again:

```bash
dotnet build
```

---

## 7. Deploy the Mod

Copy the built DLL:

```
BrickLoco/bin/Debug/net472/BrickLoco.dll
```

Into:

```
Derail Valley/BepInEx/plugins/
```

Launch the game and verify `BepInEx/LogOutput.log` contains:

```
[Info   :   BepInEx] Loading [Brick Loco 0.0.1]
[Info   :Brick Loco] BrickLoco loaded
```

This confirms:

* BepInEx is installed correctly
* The mod DLL is loading
* The plugin code is executing in-game

---

## 7.1. Config (Tune Without Recompiling)

This mod uses the BepInEx config system (`Config.Bind(...)`) to expose tuning values.

How it works:

- You do **not** need to ship a config file for BepInEx to work.
- On first launch after the plugin loads, BepInEx will automatically create the config file with default values.

Where the file is:

- `Derail Valley/BepInEx/config/com.zobayer.brickloco.cfg`

What you edit:

- `MaxSpeed` (default `20`)
- `Force` (default `7000`)
- `Mass` (default `20000`)

Shipping defaults vs shipping a preset:

- **Defaults** are shipped in code (the values passed to `Config.Bind`).
- If you want to ship a *starter preset* (optional), you can include a sample file like `com.zobayer.brickloco.cfg.example` in your repo/release ZIP and tell users to copy it into `BepInEx/config/`.

### Optional: explicit deploy command + VS Code task

This repo includes an **explicit** deploy target (it does not run on normal builds).

The deploy/clean targets use the `DerailValleyPluginsDir` property in `BrickLoco.csproj` to know where your Derail Valley `BepInEx/plugins` folder is.

- Normal build (no deploy):

```bash
dotnet build -c Debug
```

- Deploy when you want (overwrites the DLL in `BepInEx/plugins`):

```bash
dotnet msbuild -t:DeployToDerailValley -p:Configuration=Debug -p:DeployToDerailValley=true
```

### VS Code tasks

- **Build (Debug)**
  - `dotnet build -c Debug`
- **Deploy Mod (Derail Valley)**
  - `dotnet msbuild -t:DeployToDerailValley -p:Configuration=Debug -p:DeployToDerailValley=true`
- **Clean Deployed Mod (Derail Valley)** (un-deploy)
  - Deletes the deployed DLL from your game install so the mod stops loading.
  - `dotnet msbuild -t:CleanDeployedFromDerailValley -p:Configuration=Debug`

---

## 8. Spawn a TrainCar + Replace Visuals (Visible)

Once the plugin loads, the next milestone is to spawn a real piece of rolling stock and make a clearly visible placeholder visual.

Implementation notes (current approach):

- Wait for a `GameObject` tagged `Player`.
- Use `CarSpawner.SpawnCarOnClosestTrack(player.position, ...)` to spawn a `FlatbedShort` on the closest track to the player.
- Disable the original car renderers.
- Parent a primitive cube to the spawned car as a temporary stand-in mesh.
- Set the cube layer to a layer the main camera actually renders (derived from the camera culling mask).
- Remove the cube collider so it doesn't interfere with the car physics.

Expected log lines:

```
[Info   :Brick Loco] CarSpawner found: True
[Info   :Brick Loco] Spawned TrainCar: ...
[Info   :Brick Loco] Replaced TrainCar visuals with brick cube
```

---

## 8.1. New Progress (Vehicle Work)

As of Feb 2026, the project has moved beyond the initial "spawn a dummy cube" milestone and into spawning and modifying real rolling stock.

This section describes the *current working behavior* implemented in `src/BrickLocoPlugin.cs`.

Current milestones reached:

- Inspect `TrainCarLivery` prefab assets ("CarLiveries") in-game via `Resources.FindObjectsOfTypeAll<TrainCarLivery>()`.
- Spawn a `FlatbedShort` (Short Flat Car) on the closest track using `CarSpawner.SpawnCarOnClosestTrack(...)`.
- Replace the carbody visuals by disabling existing renderers and parenting a custom cube as a temporary stand-in mesh.
- Hold `G` / `H` to apply forward / reverse propulsion to the spawned car; gated by player proximity and capped by a max speed.
- Adjust tuning values for experimentation (`Force`, `Mass`, `MaxSpeed`) via the BepInEx config file.
- Verify basic interactions still work: coupling and handbrake operation.

Notes on tuning semantics (current code):

- `Force` is treated as a physics force (Newtons) applied every `FixedUpdate`.
- `Mass` now affects acceleration (roughly $a = F/m$), so increasing mass makes the same `Force` feel weaker.

Known missing piece:

- Wheels / proper bogies are not implemented yet (visual + physics).

## 8.2. Liveries Discovery Output Log

These are `TrainCarLivery` assets discovered at runtime (the `TrainCarLivery` type is defined by the game in `Assembly-CSharp.dll`).

Livery id → prefab name:

- `FlatbedShort` → `CarFlatcarShort`
- `AutorackBlue` → `CarAutorack_Blue`
- `AutorackGreen` → `CarAutorack_Green`
- `AutorackRed` → `CarAutorack_Red`
- `AutorackYellow` → `CarAutorack_Yellow`
- `BoxcarBrown` → `CarBoxcar_Brown`
- `BoxcarGreen` → `CarBoxcar_Green`
- `BoxcarPink` → `CarBoxcar_Pink`
- `BoxcarRed` → `CarBoxcar_Red`
- `BoxcarMilitary` → `CarBoxcarMilitary`
- `CabooseRed` → `CarCabooseRed`
- `FlatbedEmpty` → `CarFlatcar`
- `FlatbedMilitary` → `CarFlatcarMilitary`
- `FlatbedStakes` → `CarFlatcarStakes`
- `GondolaGray` → `CarGondola_Grey`
- `GondolaGreen` → `CarGondola_Green`
- `GondolaRed` → `CarGondola_Red`
- `HandCar` → `LocoHandcar`
- `HopperBrown` → `CarHopper_Brown`
- `HopperTeal` → `CarHopper_Teal`
- `HopperYellow` → `CarHopper_Yellow`
- `HopperCoveredBrown` → `CarHopperCovered`
- `LocoDE2` → `LocoDE2`
- `LocoDE6` → `LocoDE6`
- `LocoDE6Slug` → `LocoDE6Slug`
- `LocoDH4` → `LocoDH4`
- `LocoDM3` → `LocoDM3`
- `LocoS282A` → `LocoS282A`
- `LocoS282B` → `LocoS282B`
- `LocoS060` → `LocoS060`
- `NuclearFlask` → `CarNuclearFlask`
- `PassengerBlue` → `CarPassengerBlue`
- `PassengerGreen` → `CarPassengerGreen`
- `PassengerRed` → `CarPassengerRed`
- `StockRed` → `CarStock_Red`
- `StockGreen` → `CarStock_Green`
- `StockBrown` → `CarStock_Brown`
- `RefrigeratorWhite` → `CarRefrigerator_White`
- `TankBlack` → `CarTankBlack`
- `TankBlue` → `CarTankBlue`
- `TankOrange` → `CarTankOrange`
- `TankChrome` → `CarTankChrome`
- `TankWhite` → `CarTankWhite`
- `TankYellow` → `CarTankYellow`
- `TankShortMilk` → `CarTankShort_Milk`
- `LocoMicroshunter` → `LocoMicroshunter`
- `LocoDM1U` → `LocoDM1U`

## 9. Known Constraints

* Unity version locked to **2019.4**
* Language version locked to **C# 7.3**
* Target framework locked to **net472**
* No modern C# features (nullable refs, global usings, file-scoped namespaces)

---

## 10. Next Steps

* Make the placeholder cube a stable body (center of mass, rotation constraints, and attachment)
* Reconcile propulsion tuning (decide whether mass should affect acceleration)
* Identify the correct rigidbody/physics components to adjust for realistic roll/derail behavior
* Replace placeholder cube with real LEGO-style meshes (later: Unity asset workflow)
