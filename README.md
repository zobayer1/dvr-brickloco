# BrickLoco – Derail Valley Mod Setup Guide

This document describes the **exact steps** required to set up a working
C# mod project for **Derail Valley** using **BepInEx**, **Unity 2020**, and **.NET**.

The goal is a **clean, minimal, reproducible setup** that loads a plugin DLL
into the game before any vehicle or gameplay logic is added.

---

## 1. Prerequisites

### Software
- Derail Valley (Windows)
- Unity Hub
- Unity **2020.3 LTS** (exact major version matters)
- VS Code
- .NET Framework **4.7.2 Developer Pack**
- .NET SDK **6 or 8** (for `dotnet` CLI only)

### Notes
- Unity is used **only for inspection and reference**, not for building the game.
- The mod DLL must target **.NET Framework 4.7.2** and **C# 7.3**.

---

## 2. Install Unity (Reference Only)

1. Open Unity Hub
2. Install **Unity 2020.3 LTS**
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

````

---

## 4. Create the Mod Project

```bash
dotnet new classlib -n BrickLoco
cd BrickLoco
code .
````

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

Create `BrickLocoPlugin.cs`:

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

## 8. Known Constraints

* Unity version locked to **2020.3**
* Language version locked to **C# 7.3**
* Target framework locked to **net472**
* No modern C# features (nullable refs, global usings, file-scoped namespaces)

---

## 9. Next Steps

* Spawn test objects in the world
* Reference additional DV assemblies (`Assembly-CSharp.dll`)
* Register a custom vehicle
* Implement wheel, brake, and coupling logic
* Replace placeholder meshes with real models
