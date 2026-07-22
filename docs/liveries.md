# Liveries

Every `TrainCarLivery` asset discovered at runtime via
`Resources.FindObjectsOfTypeAll<TrainCarLivery>()`.

The **id** is what you pass to `FindLiveryById(...)`; the prefab name is what the spawned
`GameObject` is called (with a `(Clone)` suffix). BrickLoco currently spawns `FlatbedShort`.

> Captured from a single game version. Ids can change between DV releases — if a lookup
> starts returning null after a patch, re-run discovery rather than trusting this table.
> How: [Game API Notes](game-api.md#updating-for-a-new-game-version).

---

## Locomotives

| Livery id | Prefab |
| --- | --- |
| `LocoDE2` | `LocoDE2` |
| `LocoDE6` | `LocoDE6` |
| `LocoDE6Slug` | `LocoDE6Slug` |
| `LocoDH4` | `LocoDH4` |
| `LocoDM3` | `LocoDM3` |
| `LocoDM1U` | `LocoDM1U` |
| `LocoS282A` | `LocoS282A` |
| `LocoS282B` | `LocoS282B` |
| `LocoS060` | `LocoS060` |
| `LocoMicroshunter` | `LocoMicroshunter` |
| `HandCar` | `LocoHandcar` |

## Flatbeds

| Livery id | Prefab |
| --- | --- |
| `FlatbedShort` | `CarFlatcarShort` |
| `FlatbedEmpty` | `CarFlatcar` |
| `FlatbedStakes` | `CarFlatcarStakes` |
| `FlatbedMilitary` | `CarFlatcarMilitary` |

## Boxcars

| Livery id | Prefab |
| --- | --- |
| `BoxcarBrown` | `CarBoxcar_Brown` |
| `BoxcarGreen` | `CarBoxcar_Green` |
| `BoxcarPink` | `CarBoxcar_Pink` |
| `BoxcarRed` | `CarBoxcar_Red` |
| `BoxcarMilitary` | `CarBoxcarMilitary` |

## Autoracks

| Livery id | Prefab |
| --- | --- |
| `AutorackBlue` | `CarAutorack_Blue` |
| `AutorackGreen` | `CarAutorack_Green` |
| `AutorackRed` | `CarAutorack_Red` |
| `AutorackYellow` | `CarAutorack_Yellow` |

## Gondolas

| Livery id | Prefab |
| --- | --- |
| `GondolaGray` | `CarGondola_Grey` |
| `GondolaGreen` | `CarGondola_Green` |
| `GondolaRed` | `CarGondola_Red` |

## Hoppers

| Livery id | Prefab |
| --- | --- |
| `HopperBrown` | `CarHopper_Brown` |
| `HopperTeal` | `CarHopper_Teal` |
| `HopperYellow` | `CarHopper_Yellow` |
| `HopperCoveredBrown` | `CarHopperCovered` |

## Tankers

| Livery id | Prefab |
| --- | --- |
| `TankBlack` | `CarTankBlack` |
| `TankBlue` | `CarTankBlue` |
| `TankOrange` | `CarTankOrange` |
| `TankChrome` | `CarTankChrome` |
| `TankWhite` | `CarTankWhite` |
| `TankYellow` | `CarTankYellow` |
| `TankShortMilk` | `CarTankShort_Milk` |

## Passenger and stock

| Livery id | Prefab |
| --- | --- |
| `PassengerBlue` | `CarPassengerBlue` |
| `PassengerGreen` | `CarPassengerGreen` |
| `PassengerRed` | `CarPassengerRed` |
| `StockRed` | `CarStock_Red` |
| `StockGreen` | `CarStock_Green` |
| `StockBrown` | `CarStock_Brown` |
| `CabooseRed` | `CarCabooseRed` |

## Specials

| Livery id | Prefab |
| --- | --- |
| `RefrigeratorWhite` | `CarRefrigerator_White` |
| `NuclearFlask` | `CarNuclearFlask` |

---

## Re-running discovery

The list is produced by scanning for the assets at runtime:

```csharp
foreach (var livery in Resources.FindObjectsOfTypeAll<TrainCarLivery>())
    Logger.LogInfo($"{livery.id} -> {livery.prefab?.name}");
```

`FindObjectsOfTypeAll` is required here — liveries are `ScriptableObject` assets, not scene
objects, so `FindObjectOfType` returns nothing.
