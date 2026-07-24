using System.Collections.Generic;
using BrickLoco.Diagnostics;
using BrickLoco.Game;
using BrickLoco.Logic;
using UnityEngine;

namespace BrickLoco.Mount
{
    /// <summary>
    /// Owns the mounted state: attaching the player to the car's seat and keeping them there.
    ///
    /// Attaching is the easy part. Derail Valley's movement scripts continuously assert where
    /// the player should be, so staying attached means re-asserting the mount twice a frame and
    /// switching the conflicting scripts off for the duration. See docs/mounting.md.
    /// </summary>
    internal class MountController
    {
        /// <summary>How close the player must be to the seat to mount, in metres.</summary>
        private const float MountRadius = 5f;

        private readonly Settings config;
        private readonly PlayerRig rig;
        private readonly MountDiagnostics diagnostics;
        private readonly ModLog log;

        private readonly DisabledScriptSet disabledScripts = new DisabledScriptSet();
        private readonly ProblemKeySuppressor suppressor;

        private BrickCar car;
        private Transform originalPlayerParent;
        private Vector3 mountedLocalPosition;
        private bool cachedCharacterControllerEnabled;
        private bool didDisableCharacterControllerThisMount;
        private float nextEnforcementLogTime;

        public bool IsMounted { get; private set; }

        public Transform Seat { get { return car != null ? car.Seat : null; } }

        /// <summary>Camera-holder position captured at mount time. Baseline for the suppression window.</summary>
        public Vector3 MountedCameraHolderLocalPosition { get; private set; }

        /// <summary>Camera position captured at mount time. Diagnostic baseline only.</summary>
        public Vector3 MountedCameraLocalPosition { get; private set; }

        public MountController(Settings config, PlayerRig rig, MountDiagnostics diagnostics, ModLog log)
        {
            this.config = config;
            this.rig = rig;
            this.diagnostics = diagnostics;
            this.log = log;

            suppressor = new ProblemKeySuppressor(rig, log);
        }

        public void SetCar(BrickCar brickCar)
        {
            car = brickCar;
        }

        public void Toggle()
        {
            if (config.Verbose)
            {
                string seatInfo = Seat != null
                    ? $"seat={TransformPath.Of(Seat)} seatPos={Seat.position}"
                    : "seat=null";
                string playerInfo = rig.ControllerRoot != null
                    ? $"playerPos={rig.ControllerRoot.position}"
                    : "playerPos=null";

                log.LogInfo(
                    $"[Input] M pressed. isMounted={IsMounted}, spawnedCar={(car != null ? car.Name : "null")}, " +
                    $"{seatInfo}, {playerInfo}");
            }

            if (!IsMounted)
                TryMount();
            else
                Dismount();
        }

        // ---------------------------------------------------------------- mount / dismount

        private void TryMount()
        {
            if (Seat == null || rig.Root == null)
            {
                if (config.Verbose)
                    log.LogInfo(
                        $"TryMount aborted: seat={TransformPath.Of(Seat)}, " +
                        $"playerRoot={TransformPath.Of(rig.Root)}");
                return;
            }

            rig.EnsureControllerRoot();

            float dist = Vector3.Distance(rig.ControllerRoot.position, Seat.position);
            if (dist > MountRadius)
            {
                if (config.Verbose)
                    log.LogInfo(
                        $"TryMount aborted: too far (dist={dist:0.00}m). " +
                        $"playerPos={rig.ControllerRoot.position} seatPos={Seat.position} " +
                        $"seat={TransformPath.Of(Seat)}");
                return;
            }

            originalPlayerParent = rig.ControllerRoot.parent;

            AnchorSeatForMount();

            // Snap the controller root cleanly to the seat.
            rig.ControllerRoot.SetParent(Seat, worldPositionStays: false);
            mountedLocalPosition = Vector3.zero;
            rig.ControllerRoot.localPosition = mountedLocalPosition;

            IsMounted = true;

            CaptureMountBaselines();

            if (config.Verbose)
                log.LogInfo(
                    $"Mounted Brick Loco. seat={TransformPath.Of(Seat)} seatPos={Seat.position} " +
                    $"carPos={(car != null ? car.Transform.position.ToString() : "null")}");
            else
                log.LogInfo("Mounted Brick Loco");

            DisableCharacterController();

            if (config.DisableScriptsWhileMounted)
                DisableScripts();

            if (config.Verbose && config.DumpOnMount)
                diagnostics.DumpPlayerComponents("AfterMount");
        }

        /// <summary>
        /// Parents the seat to the car — or to the car's interior when the player is already
        /// standing in it. DV interiors are a smoothed frame that lags the rigidbody root
        /// slightly, which makes for a visibly steadier ride than mounting to the raw root.
        /// </summary>
        private void AnchorSeatForMount()
        {
            if (car == null)
                return;

            Transform desiredParent = car.Transform;
            if (LooksLikeCarInterior(originalPlayerParent, car))
                desiredParent = originalPlayerParent;

            if (Seat.parent != desiredParent)
            {
                Seat.SetParent(desiredParent, true);
                if (config.Verbose)
                    log.LogInfo($"Seat parent set for mount: {TransformPath.Of(desiredParent)}");
            }

            // Keep the seat anchored near the spawned car.
            Seat.position = car.Transform.TransformPoint(BrickCar.SeatLocalPosition);
            Seat.rotation = car.Transform.rotation;
        }

        private void CaptureMountBaselines()
        {
            rig.CameraHolderTransform = FindMountedCameraHolder();
            MountedCameraHolderLocalPosition = rig.CameraHolderTransform != null
                ? rig.CameraHolderTransform.localPosition
                : Vector3.zero;
            MountedCameraLocalPosition = rig.CameraTransform != null ? rig.CameraTransform.localPosition : Vector3.zero;
            suppressor.Reset();
        }

        private void Dismount()
        {
            if (rig.Root == null)
                return;

            rig.EnsureControllerRoot();

            IsMounted = false;

            disabledScripts.RestoreAll();
            suppressor.RestoreAll(config.Verbose);

            if (rig.CharacterController != null && didDisableCharacterControllerThisMount)
            {
                rig.CharacterController.enabled = cachedCharacterControllerEnabled;
                if (config.Verbose)
                    log.LogInfo(
                        "CharacterController restored after dismount " +
                        $"(enabled={rig.CharacterController.enabled})");
            }

            // worldPositionStays keeps the player where they are rather than teleporting them
            // to wherever the old parent has moved to.
            rig.ControllerRoot.SetParent(originalPlayerParent, true);
            log.LogInfo("Dismounted Brick Loco");

            if (config.Verbose && config.DumpOnMount)
                diagnostics.DumpPlayerComponents("AfterDismount");
        }

        // ---------------------------------------------------------------- per-frame enforcement

        /// <summary>
        /// Runs early in the frame, so DV's movement scripts see the mounted state before they
        /// process this frame's input.
        /// </summary>
        public void EnforceEarly()
        {
            if (!IsMounted)
                return;

            EnforceDisabledScripts();
            EnforcePinning();
            EnforceCharacterController();
        }

        /// <summary>
        /// Runs after everything else in the frame — including after DV's camera scripts, which
        /// is the double-edge: it corrects anything that moved the player during their updates,
        /// but a correction here lands *after* the camera computed its pose, one frame late.
        /// LateMountRepin exists to A/B whether this pass causes the mounted-view jitter.
        /// </summary>
        public void EnforceLate()
        {
            if (!IsMounted)
                return;

            if (Seat == null || rig.ControllerRoot == null)
                return;

            if (config.LateMountRepin)
            {
                if (rig.ControllerRoot.parent != Seat)
                {
                    // If something auto-unparents the controller during input, reattach it.
                    rig.ControllerRoot.SetParent(Seat, worldPositionStays: false);
                }

                rig.ControllerRoot.localPosition = mountedLocalPosition;
            }

            EnforceCharacterController();
            ApplySuppressionWindow();

            // Some DV scripts re-enable movement components after hierarchy/parenting changes.
            EnforceDisabledScripts();

            if (config.Verbose)
                diagnostics.LogMovementKeyTelemetry(rig.ControllerRoot, Seat);
        }

        private void ApplySuppressionWindow()
        {
            if (config.SuppressProblemKeysWhileMounted && suppressor.IsActive(Time.time))
            {
                PinCameraHolder();
                suppressor.Apply(config.Verbose);
            }
            else
            {
                suppressor.RestoreAll(config.Verbose);
            }
        }

        /// <summary>
        /// Pins the camera holder's local position to its mount-time baseline. Position only —
        /// rotation is untouched, so looking around keeps working throughout the window.
        /// </summary>
        private void PinCameraHolder()
        {
            Transform holder = rig.CameraHolderTransform;
            if (holder == null || !IsCameraHolderRelated(holder))
                return;

            if ((holder.localPosition - MountedCameraHolderLocalPosition).sqrMagnitude > 0.000001f)
                holder.localPosition = MountedCameraHolderLocalPosition;
        }

        private void EnforcePinning()
        {
            if (Seat == null || rig.ControllerRoot == null)
                return;

            if (rig.ControllerRoot.parent != Seat)
                rig.ControllerRoot.SetParent(Seat, worldPositionStays: false);

            if (rig.ControllerRoot.localPosition != mountedLocalPosition)
                rig.ControllerRoot.localPosition = mountedLocalPosition;
        }

        /// <summary>Called when Ctrl / X / Space goes down while mounted.</summary>
        public void OnProblemKey(string key)
        {
            if (!IsMounted)
                return;

            if (config.SuppressProblemKeysWhileMounted)
                suppressor.Extend(Time.time, config.SuppressProblemKeysSeconds);

            if (!config.Verbose)
                return;

            diagnostics.LogProblemKey(key, this);
        }

        // ---------------------------------------------------------------- CharacterController

        private void DisableCharacterController()
        {
            didDisableCharacterControllerThisMount = false;

            if (!config.DisableCharacterControllerWhileMounted)
                return;
            if (rig.CharacterController == null || !rig.CharacterController.enabled)
                return;

            cachedCharacterControllerEnabled = rig.CharacterController.enabled;
            rig.CharacterController.enabled = false;
            didDisableCharacterControllerThisMount = true;

            if (config.Verbose)
                log.LogInfo(
                    "CharacterController disabled while mounted " +
                    $"(wasEnabled={cachedCharacterControllerEnabled})");
        }

        /// <summary>The game re-enables the CharacterController on crouch/jump; put it back.</summary>
        private void EnforceCharacterController()
        {
            if (!config.DisableCharacterControllerWhileMounted)
                return;
            if (rig.CharacterController == null)
                return;

            // Only enforce if we intended to disable it for this mount.
            if (!didDisableCharacterControllerThisMount)
                return;

            if (rig.CharacterController.enabled)
            {
                rig.CharacterController.enabled = false;
                if (config.Verbose)
                    log.LogInfo("[MountDisable] Re-disabled CharacterController that was re-enabled by the game.");
            }
        }

        // ---------------------------------------------------------------- script disabling

        private void DisableScripts()
        {
            disabledScripts.RestoreAll();

            if (rig.ControllerRoot == null)
                return;

            string raw = config.ScriptsToDisableWhileMounted ?? string.Empty;
            string criticalRaw = config.CriticalScriptsToDisable ?? string.Empty;

            // See MountScriptPolicy for the always-disable / never-disable rules.
            HashSet<string> toDisable = MountScriptPolicy.BuildDisableSet(
                raw, criticalRaw, config.AlwaysDisableCriticalScripts);

            var scripts = rig.ControllerRoot.GetComponentsInChildren<MonoBehaviour>(true);
            int disabledCount = 0;
            int matchedCount = 0;
            var foundNames = new HashSet<string>();
            var actuallyDisabledNames = new HashSet<string>();
            var matchedButAlreadyDisabledNames = new HashSet<string>();

            if (scripts != null)
            {
                for (int i = 0; i < scripts.Length; i++)
                {
                    MonoBehaviour mb = scripts[i];
                    if (mb == null)
                        continue;

                    string typeName = mb.GetType().Name;
                    foundNames.Add(typeName);
                    if (!toDisable.Contains(typeName))
                        continue;

                    matchedCount++;
                    if (disabledScripts.Track(mb))
                    {
                        disabledCount++;
                        actuallyDisabledNames.Add(typeName);
                    }
                    else
                    {
                        matchedButAlreadyDisabledNames.Add(typeName);
                    }
                }
            }

            var missing = new List<string>();
            foreach (string requested in toDisable)
            {
                if (!foundNames.Contains(requested))
                    missing.Add(requested);
            }

            string sources = config.AlwaysDisableCriticalScripts
                ? $"cfg=({raw}) + critical=({criticalRaw})"
                : $"cfg=({raw})";

            log.LogInfo(
                $"Disabled scripts while mounted: disabledNow={disabledCount}, " +
                $"matched={matchedCount}, missing={missing.Count} ({sources})");

            if (!config.Verbose)
                return;

            if (actuallyDisabledNames.Count > 0)
                log.LogInfo($"[MountDisable] Actually disabled: {string.Join(",", actuallyDisabledNames)}");
            if (matchedButAlreadyDisabledNames.Count > 0)
                log.LogInfo(
                    "[MountDisable] Matched but already disabled: " +
                    string.Join(",", matchedButAlreadyDisabledNames));
            if (missing.Count > 0)
                log.LogInfo($"[MountDisable] Requested but not found under controller: {string.Join(",", missing)}");
        }

        private void EnforceDisabledScripts()
        {
            if (!config.DisableScriptsWhileMounted)
                return;
            if (disabledScripts.Count == 0)
                return;

            int reDisabled = disabledScripts.Enforce();

            // Rate-limited: a per-frame fight with the game would otherwise flood the log.
            if (reDisabled > 0 && config.Verbose && Time.time >= nextEnforcementLogTime)
            {
                nextEnforcementLogTime = Time.time + 1.0f;
                log.LogInfo($"[MountDisable] Re-disabled {reDisabled} script(s) that were re-enabled by the game.");
            }
        }

        // ---------------------------------------------------------------- interior / camera lookup

        /// <summary>
        /// True when a transform is this car's interior. The name test alone is not enough —
        /// two cars of the same livery differ only by clone suffix — so it is confirmed with
        /// either a car-name prefix or a hierarchy check.
        /// </summary>
        private static bool LooksLikeCarInterior(Transform maybeInterior, BrickCar car)
        {
            if (maybeInterior == null || car == null || car.IsGone)
                return false;

            if (!TransformNaming.IsInteriorName(maybeInterior.name))
                return false;

            // Common DV pattern: "<CarName> [interior]" or an interior object under the car root.
            if (TransformNaming.BelongsToCarByName(maybeInterior.name, car.Name))
                return true;

            return maybeInterior.IsChildOf(car.Transform);
        }

        /// <summary>
        /// True when a camera holder belongs to the same interior/car we mounted into, so
        /// unrelated camera rigs are left alone.
        /// </summary>
        public bool IsCameraHolderRelated(Transform holder)
        {
            if (holder == null)
                return false;

            Transform seatParent = Seat != null ? Seat.parent : null;
            if (seatParent != null && holder.IsChildOf(seatParent))
                return true;

            if (car != null && !car.IsGone && holder.IsChildOf(car.Transform))
                return true;

            return false;
        }

        /// <summary>
        /// Finds the camera holder for the mounted rig. In DV the active camera is often not a
        /// child of the controller transform while inside a cab, so this searches the interior
        /// we mounted into rather than the player hierarchy.
        /// </summary>
        private Transform FindMountedCameraHolder()
        {
            if (rig.CameraTransform != null && rig.CameraTransform.parent != null)
            {
                if (TransformNaming.LooksLikeCameraHolder(rig.CameraTransform.parent.name))
                    return rig.CameraTransform.parent;
            }

            Transform searchRoot = null;
            if (Seat != null && Seat.parent != null)
                searchRoot = Seat.parent;
            else if (car != null && !car.IsGone)
                searchRoot = car.Transform;

            if (searchRoot == null)
                return null;

            var all = searchRoot.GetComponentsInChildren<Transform>(true);
            if (all == null)
                return null;

            for (int i = 0; i < all.Length; i++)
            {
                Transform t = all[i];
                if (t == null)
                    continue;
                if (TransformNaming.IsCameraHolderName(t.name))
                    return t;
            }

            return null;
        }
    }
}
