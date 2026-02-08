# BrickLoco – Derail Valley Mod Setup Guide

This document describes the **exact steps** required to set up a working
C# mod project for **Derail Valley** using **BepInEx**, **Unity (reference-only)**, and **.NET**.

The goal is a **clean, minimal, reproducible setup** that loads a plugin DLL
into the game before any vehicle or gameplay logic is added.

---

## Milestones

- [x] BepInEx installed and running (LogOutput.log created)
- [x] Plugin loads and logs from `Awake()`
- [x] Explicit deploy workflow (manual MSBuild target + VS Code task)
- [x] Visible test cube spawns in-world (renderer visible)
- [x] Reference DV gameplay assemblies (e.g., `Assembly-CSharp.dll`) for deeper integration
- [x] Hook into gameplay events / world systems
- [x] Press-and-hold `G` applies forward force to spawned car (simple linear acceleration)
- [ ] Make the cube a stable body

---

## 1. Prerequisites

### Software
- Derail Valley (Windows)
- Unity Hub
- Unity **2019.4 LTS** (reference-only; match the game's major version)
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

Update `BrickLoco.csproj` to reference game assemblies:

```xml
<ItemGroup>
  <Reference Include="BepInEx">
    <HintPath>
      C:\Program Files (x86)\Steam\steamapps\common\Derail Valley\BepInEx\core\BepInEx.dll
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

## 8. Spawn a Test Cube (Visible)

Once the plugin loads, the next milestone is to spawn a clearly visible object in the world.

Implementation notes (current approach):

- Wait for a `GameObject` tagged `Player`.
- Find the player's camera (`Camera.main` fallback to any camera).
- Spawn a cube **in front of the camera**, not using an arbitrary scene layer.
- Set the cube layer to a layer that the camera actually renders (derived from the camera culling mask).
- Use an **Unlit/Color** (or Standard + emission) material so lighting doesn't hide it.

Expected log lines:

```
[Info   :Brick Loco] Spawned test cube near player at (...)
[Info   :Brick Loco] Cube renderer enabled: True, isVisible (any camera): True
```

If you see `isVisible: False`, the object exists but isn’t being rendered (layer/culling-mask issue).

---

## 8.1. New Progress (Vehicle Work)

As of Feb 2026, the project has moved beyond the initial "spawn a dummy cube" milestone and into spawning and modifying real rolling stock.

Current milestones reached:

- Inspect `TrainCarLivery` prefab assets ("CarLiveries") in-game via `Resources.FindObjectsOfTypeAll<TrainCarLivery>()`.
- Spawn a `FlatbedShort` (Short Flat Car) on the closest track using `CarSpawner.SpawnCarOnClosestTrack(...)`.
- Replace the carbody visuals by disabling existing renderers and parenting a custom cube as a temporary stand-in mesh.
- Press and hold `G` to apply forward force to the spawned car (simple linear acceleration).
- Verify basic interactions still work: coupling and handbrake operation.

Known missing piece:

- Wheels / proper bogies are not implemented yet (visual + physics).

## 9. Known Constraints

* Unity version locked to **2020.3**
* Language version locked to **C# 7.3**
* Target framework locked to **net472**
* No modern C# features (nullable refs, global usings, file-scoped namespaces)

---

## 10. Next Steps

* Spawn test objects in the world
* Reference additional DV assemblies (`Assembly-CSharp.dll`)
* Register a custom vehicle
* Implement wheel, brake, and coupling logic
* Replace placeholder meshes with real models
