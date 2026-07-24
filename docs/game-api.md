# Game API Notes

Derail Valley's types have little public documentation. This is the practical workflow for
finding what you need.

---

## Finding a type with ILSpy

The recurring question is *"which DLL contains this class, and what namespace is it in?"*
A decompiler answers both.

1. Open the game's managed assemblies in [ILSpy](https://github.com/icsharpcode/ILSpy)
   (or dnSpy / JetBrains dotPeek — any will do).
   Folder: `Derail Valley/DerailValley_Data/Managed/`
2. Search for the type name — `CarSpawner`, `TrainCar`, `TrainCarLivery`.
3. Note two things:
   - the **namespace**, which becomes your `using`
   - the **assembly**, which becomes a `<Reference>` in `src/BrickLoco/BrickLoco.csproj`
4. Add the reference using the existing `$(DerailValleyManagedDir)` property:

```xml
<Reference Include="DV.Something">
  <HintPath>$(DerailValleyManagedDir)\DV.Something.dll</HintPath>
</Reference>
```

Browsing the class tree *around* the type you found is usually as valuable as the type
itself — it is how you discover the related components, fields and methods that make a
feature possible.

---

## Referenced assemblies

What BrickLoco currently references and why:

| Assembly | Location | Provides |
| --- | --- | --- |
| `UnityModManager` | `DerailValley_Data/Managed/UnityModManager/` | `ModEntry`, `ModSettings`, `[Draw]`, the mod logger. `0Harmony.dll` sits in the same folder for future Harmony patches. |
| `Assembly-CSharp` | `DerailValley_Data/Managed/` | DV gameplay types: `CarSpawner`, `TrainCar` |
| `DV.ThingTypes` | `DerailValley_Data/Managed/` | `TrainCarLivery` |
| `DV.Utils` | `DerailValley_Data/Managed/` | DV helper types |
| `UnityEngine` | `DerailValley_Data/Managed/` | Umbrella assembly |
| `UnityEngine.CoreModule` | `DerailValley_Data/Managed/` | `GameObject`, `Transform`, `Camera`, `Time` |
| `UnityEngine.PhysicsModule` | `DerailValley_Data/Managed/` | `Rigidbody`, `Collider`, `CharacterController` |
| `UnityEngine.InputLegacyModule` | `DerailValley_Data/Managed/` | `Input.GetKey`, `KeyCode` |

Unity 2019 splits the engine into modules. If a Unity type will not resolve, the usual cause
is a missing module assembly rather than a wrong namespace.

> Reference DV assemblies, never copy them next to your DLL. UMM loads your mod into the
> game's own process, where the real assemblies are already loaded — a copy would either be
> redundant or, worse, a version mismatch.

---

## Types this mod uses

### `CarSpawner`

Found with `FindObjectOfType<CarSpawner>()`. The method that matters:

```csharp
TrainCar SpawnCarOnClosestTrack(
    Vector3 position,
    TrainCarLivery livery,
    bool flipRotation,
    bool playerSpawnedCar,
    bool uniqueCar);
```

This handles track snapping and bogie placement. Instantiating a car prefab directly does
not, and gives you a car that is not really on the rails.

### `TrainCarLivery`

A `ScriptableObject` asset describing one variant of rolling stock — its `id` and its
prefab. They are assets, not scene objects, so `FindObjectOfType` will not find them:

```csharp
Resources.FindObjectsOfTypeAll<TrainCarLivery>()
```

`FindObjectsOfTypeAll` returns assets and inactive objects, including prefabs. That is what
makes it work here — and also why it is the wrong tool for finding *scene* objects, where it
will happily hand you a prefab that shares a name with the thing you wanted.

Every id discovered at runtime is listed in [Liveries](liveries.md).

### `TrainCar`

The spawned component. BrickLoco uses `car.transform`, `car.name`,
`car.GetComponent<Rigidbody>()`, `car.Bogies` and `car.AreBogiesFullyInitialized()`; see
[Architecture](architecture.md#spawn-pipeline) for the Rigidbody values it sets. Also
notable: `car.massController` (a `TrainMassController`) owns mass distribution between body
and bogies — BrickLoco currently bypasses it.

### `Bogie`

One per truck, with its own public `Rigidbody rb`. The method that matters:

```csharp
public void ApplyForce(float inputForce)   // rb.AddForce(transform.forward * inputForce)
```

This is the game's own traction path: force applied at the bogie, along the bogie's
forward — which follows the rail. BrickLoco drives through it (`DriveViaBogies`), because
pushing the carbody along the *car's* forward diverges from the rail on curves and fights
the bogie joints. Found by decompiling `Assembly-CSharp.dll` with `ilspycmd -t Bogie`.

---

## Working without an API

Two habits that pay off:

**Name matching is a heuristic, not an API.** DV gives no reliable way to ask "what is this
car's interior?", so the mod matches names — then confirms with a hierarchy check. Name
matching alone will eventually attach you to the wrong object; the confirming check is not
optional. The heuristics live in `TransformNaming` and are unit tested.

**Dump before you guess.** `MountDiagnostics.DumpPlayerComponents` (bound to <kbd>F9</kbd>) walks
the player hierarchy and logs every `MonoBehaviour` and `Behaviour` with its type, enabled
state, transform path, and whether it sits on the camera chain. Nearly every script named
in [Mounting](mounting.md) was found that way, not by guessing.

---

## Updating for a new game version

DV updates can rename types, move them between assemblies, or change component layouts.
When the mod stops working after a patch:

1. Rebuild. Missing-reference errors point straight at what moved.
2. Re-run the livery discovery — ids do change between versions.
3. Press <kbd>F9</kbd> in-game and diff the component dump against the script names in
   `CriticalScriptsToDisable`. A renamed script silently matches nothing, and the log's
   `missing=` count is the tell.
