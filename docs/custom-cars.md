# Custom Cars (CCL Authoring)

Setting up the **Custom Car Loader** authoring pipeline — the toolchain that turns a model
into a car the game spawns with full vanilla behaviour (bogies, coupling, brakes, walkable
colliders). This is the direction the project is moving in; see
[Roadmap → Direction](roadmap.md#direction-2026-07).

> **Two separate toolchains.** Building the BrickLoco *DLL* uses the `dotnet` CLI and never
> needs Unity ([Getting Started](getting-started.md)). Authoring a *custom car* uses Unity +
> CCL and never touches the DLL. This page is the second one. They are independent — a custom
> car loads through CCL whether or not BrickLoco is installed.

---

## 1. Artifacts and versions

Everything downloaded to stand this up, with the exact versions used. **The CCL runtime and
the CarCreator authoring package must share the same major version** — a car exported by
3.1.7 tools against a 2.0.x runtime can fail to load.

| Artifact | Version | Where it goes | Download |
| --- | --- | --- | --- |
| Unity Editor | **2019.4.40f1** | Unity Hub → Installs (Archive) | Unity download archive |
| CarCreator package | **3.1.7** | Imported into the Unity project | [CarCreator.unitypackage](https://github.com/derail-valley-modding/custom-car-loader/releases/download/v3.1.7/CarCreator.unitypackage) |
| Custom Car Loader (runtime) | **3.1.7** | `Derail Valley/Mods/DVCustomCarLoader` | [DVCustomCarLoader.zip](https://github.com/derail-valley-modding/custom-car-loader/releases/download/v3.1.7/DVCustomCarLoader.zip) |
| DVLangHelper (CCL dependency) | **1.2.1** | `Derail Valley/Mods/DVLangHelper` | [DVLangHelper.zip](https://github.com/derail-valley-modding/language-helper/releases/download/v1.2.1/DVLangHelper.zip) |
| Unity Mod Manager | **0.27.3** | The loader (already installed) | [nexusmods.com/site/mods/21](https://www.nexusmods.com/site/mods/21) |

Reference: [CCL wiki](https://github.com/derail-valley-modding/custom-car-loader/wiki),
[CCL on Nexus](https://www.nexusmods.com/derailvalley/mods/324).

---

## 2. Install the runtime mods

CCL declares `"Requirements": ["DVLangHelper"]` in its `Info.json`. Without DVLangHelper, CCL
sits in UMM's **"needs restart"** state forever and never goes green — the single most common
cause of that symptom.

1. Install **DVLangHelper.zip** and **DVCustomCarLoader.zip** through the UMM installer (drag
   each into its *Mods* tab), or unzip into `Derail Valley/Mods/`.
2. **Fully relaunch** the game (a save reload is not enough — UMM loads mods at startup).
3. In the UMM window (<kbd>Ctrl</kbd>+<kbd>F10</kbd>), confirm both **DVLangHelper** and
   **Custom Car Loader 3.1.7** show `OK`.

> `LoadAfter` entries in CCL's manifest (SkinManager, DVCustomLicenses, DVCustomCargo) are
> **optional** ordering hints, not requirements. CCL runs fine without them.

---

## 3. Set up the Unity authoring project

A **new, separate** Unity project — not the BrickLoco repo. Four settings were changed from
the defaults, and each matters:

| Setting | Value | Why |
| --- | --- | --- |
| Unity version | **2019.4.40f1** exactly | A bundle built by any other version returns `null` from `AssetBundle.LoadFromFile` **silently**. Not 2019.4.39, not 2020+. |
| License | free **Personal** | An unvalidated/expired license throws `MethodAccessException: Requires team license`. Personal is sufficient; sign in to Unity Hub and issue a current one. |
| **Color Space** (Player → Other Settings) | **Linear** | DV renders in linear space. Gamma makes every custom material look washed out / wrong-brightness in-game. |
| **XR Settings** (Player) | **Virtual Reality Supported** on, **Stereo Rendering Mode = Single Pass** | DV is VR-capable; CCL bundles are built expecting this even when you play flat. Mismatched XR settings can break rendering. |
| **Build target** (File → Build Settings) | **PC, Mac & Linux Standalone**, Target **Windows**, Architecture **x86_64** | DV loads **StandaloneWindows64** bundles. **Not** Universal Windows Platform (UWP) — a UWP bundle will not load. |

Then import the tooling:

- Right-click **Assets → Import Package → Custom Package…** → select **CarCreator.unitypackage**.
- After import you have a **CarCreator** folder in Assets and a **CCL** menu in the top menu bar.
  That menu is the entire authoring surface.

---

## 4. Create a car type

**CCL → Create New Car Type** opens the wizard. Fields and the choices made for the first
car (a plain freight flatcar — the minimum that exercises the whole pipeline):

| Field | Value | Note |
| --- | --- | --- |
| Car Name | `Brick Flatcar` | Human-readable, shown in-game. |
| Car ID | `Zobayer.BrickFlatbed` | Must be globally unique; namespace it, no spaces. |
| Kind | **Car** | Not Locomotive — a loco adds cab, controls, powered wheels and a full simulation, i.e. four more ways a first export can fail. `Kind` is per-car-type, so a loco is a *new* type later that reuses this bogie/mesh work. |
| Base Type | **flatbed** | Inherits the default bogie (wheel radius **0.459 m**) and standard couplers. |
| Role | neutral / none | No job-cargo integration for a test car. |
| Create Pack | **yes** | A "pack" is the mod container that exports to `Mods/`. Required to produce a loadable mod. |
| Author | your handle | Baked into the exported `Info.json`. |

The wizard generates a `_cartype` asset, a livery, prefab(s), and a **Pack** asset under
`Assets/_CCL_CARS/<name>/`.

---

## 5. Export

Export is **pack-level in 3.1.7** — there is no per-cartype export button.

1. Select the **Pack** asset → in the Inspector click **Export Pack**. This runs the
   **Car Validator**.
2. **Errors** block export; **warnings** do not. On the stock template these two warnings are
   benign and safe to skip:
   - *No icon* — the car shows a blank tile in the spawn list. Cosmetic; add a sprite later.
   - *CustomizationPlacementMeshesProxy* — livery/decal placement hints; irrelevant to loading.
3. Point the destination at a subfolder under **`Derail Valley/Mods/`** (watch for a default
   `C:` path if your game is on another drive).

A successful export writes a self-contained mod folder:

```
Mods/Zobayer.BrickFlatbed/
├─ Info.json                 <- Requirements: ["DVCustomCarLoader"], ManagerVersion 0.27.3
├─ ccl_bundle(.manifest)     <- the asset bundle (the actual car)
└─ Zobayer.BrickFlatbed(.manifest)   <- car-type / livery data
```

---

## 6. Load and spawn

1. **Fully relaunch** Derail Valley.
2. UMM shows the pack (e.g. **Zobayer.BrickFlatbed**) `OK`, alongside CCL and DVLangHelper.
   A red/unresolved entry is a load error — check the CCL log.
3. Enable the car spawner if needed (**ESC → settings**; it's a sandbox feature), take out the
   **comms remote**, switch to **Car Spawner** mode, find the car, and spawn it on a track.

**Pipeline validated (July 2026):** the stock flatbed spawns green and sits correctly on its
bogies, with working couplers, air hoses, brake wheel, and a walkable collider — all vanilla
behaviour, no BrickLoco code involved. From this baseline, every later change (a custom body
mesh, then a custom bogie) has a known-good state to fall back to.

---

## Troubleshooting

| Symptom | Cause | Fix |
| --- | --- | --- |
| CCL stuck at "needs restart" in UMM | DVLangHelper missing | Install DVLangHelper, relaunch (§2). |
| `MethodAccessException: Requires team license` in Unity | Stale/absent Personal license | Sign in to Unity Hub, issue a current free Personal license. |
| Custom material renders **magenta** in-game | Shader did not survive the bundle | Use a CCL/DV-supported shader; see [CCL wiki — Models and Textures](https://github.com/derail-valley-modding/custom-car-loader/wiki/Models-and-Textures). |
| Car exported but never appears | Wrong build target (UWP) or exported to the wrong drive's `Mods/` | Rebuild as StandaloneWindows64; export into the correct install's `Mods/`. |
| Wheel rotation looks wrong on a custom bogie | Wheel radius mismatch | Match the base bogie's radius (default **0.459 m**); see [Roadmap](roadmap.md). |

---

## Next

Replace the stock white body with a custom flat-board mesh (keeping vanilla bogies — this
isolates the model/shader path), then swap in a custom bogie
(`BogieF/bogie_car/[axle]*` hierarchy, wheels spun by the game, never hand-animated). Tracked
in [Roadmap](roadmap.md).
