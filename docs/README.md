# BrickLoco Documentation

Wiki-style docs for the BrickLoco Derail Valley mod. Each page is self-contained;
start wherever your question lives.

## Setting up

| Page | What it covers |
| --- | --- |
| [Getting Started](getting-started.md) | Prerequisites, Unity, Unity Mod Manager, first build. Do this once. |
| [Build & Deploy](build-and-deploy.md) | The build/deploy/undeploy workflow in full detail. |
| [Custom Cars (CCL)](custom-cars.md) | The Unity + Custom Car Loader authoring pipeline: artifacts, versions, project settings, export. |
| [Troubleshooting](troubleshooting.md) | The mod did not load, the car did not spawn, deploy failed. |

## Using the mod

| Page | What it covers |
| --- | --- |
| [Controls](controls.md) | Every key the mod binds. |
| [Configuration](configuration.md) | Every setting, its default, and what it actually changes. |

## Working on the mod

| Page | What it covers |
| --- | --- |
| [Architecture](architecture.md) | How the plugin is laid out and what runs when. |
| [Mounting](mounting.md) | The mount system and the fight with DV's movement scripts. |
| [Game API Notes](game-api.md) | Finding DV types with ILSpy; which assembly holds what. |
| [Liveries](liveries.md) | Every `TrainCarLivery` id discovered at runtime. |
| [Testing](testing.md) | What is unit tested, what cannot be, and why. |
| [Roadmap](roadmap.md) | Milestones reached and what is next. |

## Conventions used in these docs

- Paths are relative to the repository root unless stated otherwise.
- `Derail Valley/` means your game install directory, wherever that is on your machine.
- Log excerpts come from `Derail Valley/DerailValley_Data/Managed/UnityModManager/Log.txt`
  (also visible in-game via <kbd>Ctrl</kbd>+<kbd>F10</kbd>).
