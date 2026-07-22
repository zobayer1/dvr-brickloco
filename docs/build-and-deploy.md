# Build & Deploy

How source becomes a DLL the game loads, and how to take it back out again.

---

## The short version

```powershell
dotnet build                # compile only, touches nothing in the game folder
.\scripts\deploy.ps1        # build + copy into the game
.\scripts\undeploy.ps1      # remove it from the game
dotnet test                 # run the unit tests
```

Deployment is never a side effect of building. A plain `dotnet build` will not touch your
game install, so you can compile freely while the game is running.

---

## Pointing at your game install

Everything — the eight assembly references *and* the deploy targets — derives from a single
MSBuild property, `DerailValleyDir`. It resolves in this order, first non-empty winning:

| Order | Source | Use when |
| --- | --- | --- |
| 1 | `-p:DerailValleyDir="..."` on the command line | One-off override |
| 2 | `DERAIL_VALLEY_DIR` environment variable | Your machine differs from the repo default |
| 3 | The default in `src/BrickLoco/BrickLoco.csproj` | You are the repo author |

Setting it once per machine, without touching the tracked `.csproj`:

```powershell
[Environment]::SetEnvironmentVariable('DERAIL_VALLEY_DIR', 'C:\Path\To\Derail Valley', 'User')
```

Restart your shell afterwards so the variable is visible.

The derived paths are:

| Property | Value |
| --- | --- |
| `DerailValleyManagedDir` | `$(DerailValleyDir)\DerailValley_Data\Managed` |
| `UnityModManagerDir` | `$(DerailValleyManagedDir)\UnityModManager` |
| `DerailValleyModsDir` | `$(DerailValleyDir)\Mods` |
| `ModDeployDir` | `$(DerailValleyModsDir)\BrickLoco` |

---

## Deploying

```powershell
.\scripts\deploy.ps1
.\scripts\deploy.ps1 -Configuration Release
.\scripts\deploy.ps1 -GameDir "C:\Program Files (x86)\Steam\steamapps\common\Derail Valley"
.\scripts\deploy.ps1 -Force          # deploy even though the game is running
```

The scripts resolve the project root from their own location, so they work from any
working directory.

What a deploy does:

1. **Refuses to run if `DerailValley.exe` is up.** A running game holds a file lock on the
   deployed DLL; without this check the copy fails with an opaque IO error. `-Force` skips
   the check if you want to try anyway.
2. **Validates the game path** and fails with a readable message rather than silently
   copying into a directory that does not exist.
3. **Builds** the configuration you asked for.
4. **Copies `BrickLoco.dll` and `BrickLoco.pdb`** into `Mods/BrickLoco/` and **generates
   `Info.json`** there — the manifest Unity Mod Manager reads to discover the mod. It is
   generated from csproj properties (`Version`, `Product`, `ModEntryMethod`) so the version
   number has exactly one source.

The `.pdb` matters more than it looks: without it, stack traces have no line numbers,
which makes every in-game exception significantly harder to chase.

### The UMM layout

One folder per mod under `Mods/`, holding `Info.json` plus the assembly, is UMM's
convention — every mod in the ecosystem (`DVCustomCarLoader`, `ZCouplers`, …) follows it.
UMM also stores the mod's `Settings.xml` in the same folder once you save settings in-game.

---

## Undeploying

```powershell
.\scripts\undeploy.ps1
.\scripts\undeploy.ps1 -PurgeConfig   # also delete the legacy BepInEx .cfg
```

Undeploy removes `Mods/BrickLoco/` — including `Settings.xml`, since UMM keeps it inside
the mod folder; back it up first if you care about your tuning values. It also sweeps up
any BepInEx-era install (`BepInEx/plugins/BrickLoco/` or a loose DLL) from before the
migration. It is safe to run when nothing is deployed — it reports that and exits cleanly.

---

## Calling MSBuild directly

The `.ps1` files are wrappers. The targets underneath are usable on their own — useful in
CI, or anywhere PowerShell execution policy is in the way:

```bash
dotnet build src/BrickLoco/BrickLoco.csproj -t:Deploy -c:Debug
dotnet build src/BrickLoco/BrickLoco.csproj -t:Undeploy
dotnet build src/BrickLoco/BrickLoco.csproj -t:Undeploy -p:PurgeConfig=true
```

Name the project file explicitly. Bare `dotnet build -t:Deploy` at the repo root resolves
to `BrickLoco.sln`, and the target does not exist on the test project.

You lose the running-game check when calling MSBuild directly. That is the only thing the
wrapper adds.

| Target | Effect |
| --- | --- |
| `Deploy` | Depends on `Build`. Copies DLL + PDB to `ModDeployDir`, generates `Info.json`. |
| `Undeploy` | Deletes the deploy dir, sweeps BepInEx-era locations, optionally the legacy `.cfg`. |
| `ValidateDerailValleyDir` | Precondition check: game dir exists, UMM is installed. |

---

## VS Code tasks

`Ctrl+Shift+P` → *Tasks: Run Task*:

- **Build (Debug)** — default build task, `Ctrl+Shift+B`
- **Deploy Mod (Derail Valley)**
- **Undeploy Mod (Derail Valley)**
- **Run Tests**

---

## Project layout

```
BrickLoco.sln
.editorconfig
src/
  BrickLoco/
    BrickLoco.csproj       mod assembly (net472, C# 7.3)
    Loader.cs              UMM entry point (Info.json EntryMethod)
    Settings.cs            every tunable, drawn in the UMM window
    BrickLocoBehaviour.cs  the only MonoBehaviour
    ModLog.cs              logging adapter
    Game/ Mount/ Diagnostics/
    Logic/                 pure C#, no UnityEngine — unit tested
tests/
  BrickLoco.Tests/         xUnit, net8.0
scripts/
  deploy.ps1
  undeploy.ps1
docs/
```

This is the standard .NET repo layout: one directory per project under `src/` and `tests/`,
with the solution at the root. Build output lands in `src/BrickLoco/bin/`, keeping the
repository root free of `bin/` and `obj/`.

Note this is **not** a Unity project layout — there is no `Assets/`, no `ProjectSettings/`
and no `.meta` files. BrickLoco is a class library that *references* Unity assemblies; it is
never opened by the Unity editor.

---

## Rebuild loop while developing

Derail Valley loads plugin DLLs once at startup; there is no hot reload. Each iteration is:

1. Quit the game.
2. `.\scripts\deploy.ps1`
3. Launch, load a save, reproduce.
4. Read the UMM console (<kbd>Ctrl</kbd>+<kbd>F10</kbd>) or
   `DerailValley_Data/Managed/UnityModManager/Log.txt`.

Because that loop is slow, prefer moving decisions into `src/BrickLoco/Logic/` where a
`dotnet test` run answers the question in under a second. See [Testing](testing.md).

Values under [Configuration](configuration.md) only need a game restart, not a redeploy —
that is what they are for.
