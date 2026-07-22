# Testing

```powershell
dotnet test
```

55 tests, ~20 ms. No game, no Unity, no deploy.

---

## What can and cannot be tested

Most of this mod is unit-testable in principle and untestable in practice. `GameObject`,
`Transform`, `Input`, `Time` and `MonoBehaviour` need a live Unity player loop; you cannot
`new` them up in a test process. Testing them properly would mean the Unity Test Framework
inside a real Unity project — a large amount of setup for a mod whose Unity-facing code is
mostly "find object, set field".

So the line is drawn at **decisions**:

| Testable | Not testable |
| --- | --- |
| Should force be applied at this speed? | Applying it to a `Rigidbody` |
| Which script names get disabled? | Finding those MonoBehaviours in a hierarchy |
| Does this name look like a car interior? | Walking the transform tree to confirm |
| Which layer does this culling mask show? | Reading `Camera.cullingMask` |
| Is the suppression window still open? | Reading `Time.time` |

The right-hand column is thin glue. The left-hand column is where the bugs live — a
directional comparison, a set-membership rule, an off-by-one in a bit scan. Those all live
in `src/BrickLoco/Logic/` and are covered.

---

## How the test project is wired

`tests/BrickLoco.Tests/` does **not** reference the mod project. It compiles the pure
sources directly:

```xml
<Compile Include="..\..\src\BrickLoco\Logic\*.cs" LinkBase="Logic" />
```

A `ProjectReference` would drag in `BrickLoco.dll`, which links against `UnityEngine` and
`Assembly-CSharp` — assemblies that only load inside the game process. Linking the sources
sidesteps that completely, and keeps the mod shipping as a single DLL with no runtime
dependency on a separate logic assembly.

The wildcard means **new files in `src/BrickLoco/Logic/` are picked up automatically**. No csproj edit
needed.

### Why the tests target net8.0

The mod must be `net472` because that is what the game's Mono runtime loads. The tests have
no such constraint — they never touch Unity — so they target `net8.0` for faster, better
supported tooling. The linked sources are written in C# 7.3 and compile fine under both.

---

## Current coverage

| Suite | Covers |
| --- | --- |
| `PropulsionPolicyTests` | Speed gating: direction, cap boundaries, negative/zero `MaxSpeed`, braking while over the cap |
| `MountScriptPolicyTests` | Always-disabled and never-disabled rules, config merging, comma parsing, degenerate input |
| `TransformNamingTests` | Interior detection, car-name prefixing, the loose vs strict camera-holder matchers |
| `CameraLayersTests` | Culling-mask scan including layer 0, layer 31, and the empty mask |
| `SuppressionWindowTests` | Window extension never shortening, negative clamping, boundary of "active" |

Several tests exist to pin down behaviour that is easy to break by accident:

- **Braking while over the cap.** The gate is directional. Making it symmetric would look
  like a simplification and would make a runaway car unstoppable.
- **`CustomFirstPersonController` is never disabled**, even when named in either config
  list. Breaking this leaves the player mounted with a frozen camera.
- **The two camera-holder matchers differ on purpose** — one loose, one strict. A test
  documents each, so "why aren't these the same function?" has an answer.

---

## Adding tests

1. Put the logic in `src/BrickLoco/Logic/` as a `static` class with no `using UnityEngine`.
2. Call it from whichever class needs it.
3. Add a test file in `tests/BrickLoco.Tests/`. It is picked up automatically.

Step 2 matters. Logic that is extracted but not called is dead code with a green test suite
attached — worse than no test at all.

If a rule is subtle enough that you had to think about it, write the *why* in the test's
doc comment. Several of the existing ones read as explanations rather than assertions, and
that is the intent.

---

## Verifying the tests actually test something

A passing suite proves nothing on its own. Break the logic on purpose and confirm the suite
notices:

```powershell
# In PropulsionPolicy.cs, change:  float limit = Math.Abs(maxSpeed);
#                             to:  float limit = maxSpeed;
dotnet test        # expect: Failed: 1
```

This was done for each policy while writing them. It is worth repeating whenever you add a
test that passes on the first run — that is exactly when a vacuous assertion slips through.

---

## What is not covered

- Everything outside `src/BrickLoco/Logic/` — the plugin, `PlayerRig`, `BrickCarBuilder`,
  `MountController`. Verified by playing the game and reading `LogOutput.log`.
- The mount/dismount sequence end to end.
- Physics tuning. Whether `Force = 7000` *feels* right is not a unit-testable question.

There is no CI. `dotnet test` is a local pre-commit habit, not an enforced gate.
