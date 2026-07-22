using System.Collections;
using System.Collections.Generic;
using BrickLoco.Game;
using BrickLoco.Mount;
using UnityEngine;

namespace BrickLoco.Diagnostics
{
    /// <summary>
    /// Discovery logging for the mount system. All of it is gated behind the MountTelemetry
    /// config switch, because the mod is still working out why mounted players jitter.
    ///
    /// Log tags used here: [Dump], [MountTelemetry], [MountedKey], [JitterSnap].
    /// </summary>
    internal class MountDiagnostics
    {
        /// <summary>
        /// Scripts whose enabled state is sampled during a jitter snapshot. A superset of what
        /// <see cref="ProblemKeySuppressor"/> switches off — CharacterControllerMover is watched
        /// but never disabled.
        /// </summary>
        private static readonly string[] WatchedScripts =
        {
            "FallThroughTerrainFix",
            "MovementFlagUpdater",
            "CameraAnchorLeanCrouch",
            "CharacterControllerMover",
            "TeleportForbiddenOverlapSafety",
            "WalkableControlOverlapDisabler",
            "WorldBoundaryEnforcer"
        };

        private const int SnapshotFrames = 30;

        private readonly MonoBehaviour host;
        private readonly PlayerRig rig;
        private readonly ModLog log;

        private float nextTelemetryLogTime;
        private int snapshotSeq;

        public MountDiagnostics(MonoBehaviour host, PlayerRig rig, ModLog log)
        {
            this.host = host;
            this.rig = rig;
            this.log = log;
        }

        /// <summary>
        /// Walks the player hierarchy and logs every Behaviour with its type, enabled state,
        /// transform path, and whether it sits on the camera chain. This is how the scripts
        /// named in the mount config were found in the first place.
        /// </summary>
        public void DumpPlayerComponents(string reason)
        {
            if (rig.ControllerRoot == null)
            {
                log.LogInfo($"[Dump] {reason}: playerControllerRoot=null");
                return;
            }

            log.LogInfo($"[Dump] === {reason} ===");
            log.LogInfo($"[Dump] ControllerRoot: {TransformPath.Of(rig.ControllerRoot)}");

            // TransformPath.Of already renders null as "null".
            log.LogInfo($"[Dump] Camera: {TransformPath.Of(rig.CameraTransform)}");

            var cameraChain = BuildCameraChain();

            var mono = rig.ControllerRoot.GetComponentsInChildren<MonoBehaviour>(true);
            log.LogInfo($"[Dump] MonoBehaviours under controller: {(mono != null ? mono.Length : 0)}");
            if (mono != null)
            {
                for (int i = 0; i < mono.Length; i++)
                {
                    MonoBehaviour mb = mono[i];
                    if (mb == null)
                        continue;

                    log.LogInfo(DescribeBehaviour("MB", i, mb, cameraChain));
                }
            }

            var behaviours = rig.ControllerRoot.GetComponentsInChildren<Behaviour>(true);
            int nonMonoCount = 0;
            if (behaviours != null)
            {
                for (int i = 0; i < behaviours.Length; i++)
                {
                    Behaviour b = behaviours[i];
                    if (b == null || b is MonoBehaviour)
                        continue;
                    nonMonoCount++;
                }
            }

            log.LogInfo($"[Dump] Non-Mono Behaviours under controller: {nonMonoCount}");
            if (behaviours != null)
            {
                for (int i = 0; i < behaviours.Length; i++)
                {
                    Behaviour b = behaviours[i];
                    if (b == null || b is MonoBehaviour)
                        continue;

                    log.LogInfo(DescribeBehaviour("B", i, b, cameraChain));
                }
            }
        }

        private static string DescribeBehaviour(string prefix, int index, Behaviour b, HashSet<Transform> cameraChain)
        {
            return $"[Dump] {prefix}[{index}] enabled={b.enabled} onCamChain={cameraChain.Contains(b.transform)} " +
                   $"type={b.GetType().FullName} path={TransformPath.Of(b.transform)}";
        }

        /// <summary>
        /// Logs mount state when a movement key goes down, rate-limited to twice a second so a
        /// held key does not flood the log.
        /// </summary>
        public void LogMovementKeyTelemetry(Transform controllerRoot, Transform seat)
        {
            if (!MovementKeyWentDown())
                return;

            if (Time.time < nextTelemetryLogTime)
                return;

            nextTelemetryLogTime = Time.time + 0.5f;

            Vector3 worldDelta = controllerRoot.position - seat.position;
            string holderInfo = rig.CameraHolderTransform != null
                ? $", holderLocalPos={rig.CameraHolderTransform.localPosition}"
                : ", holder=null";
            string camInfo = rig.CameraTransform != null
                ? $", camLocalPos={rig.CameraTransform.localPosition}" +
                  $", camLocalRot={rig.CameraTransform.localRotation.eulerAngles}"
                : ", cam=null";

            log.LogInfo(
                $"[MountTelemetry] input key down. parent={controllerRoot.parent.name}, " +
                $"localPos={controllerRoot.localPosition}, worldDelta={worldDelta}{holderInfo}{camInfo}");
        }

        /// <summary>Logs the state captured the moment a problem key is pressed while mounted.</summary>
        public void LogProblemKey(string key, MountController mount)
        {
            string holder = rig.CameraHolderTransform != null
                ? $"holder={TransformPath.Of(rig.CameraHolderTransform)} " +
                  $"relatedToMount={mount.IsCameraHolderRelated(rig.CameraHolderTransform)} " +
                  $"holderLocalPos={rig.CameraHolderTransform.localPosition} " +
                  $"(baseline={mount.MountedCameraHolderLocalPosition})"
                : "holder=null";

            string cam = "cam=null";
            if (rig.CameraTransform != null)
            {
                string underController = rig.ControllerRoot != null
                    ? rig.CameraTransform.IsChildOf(rig.ControllerRoot).ToString()
                    : "null";

                cam = $"cam={TransformPath.Of(rig.CameraTransform)} underController={underController} " +
                      $"camWorldPos={rig.CameraTransform.position} " +
                      $"camLocalPos={rig.CameraTransform.localPosition} " +
                      $"(baseline={mount.MountedCameraLocalPosition}) " +
                      $"camLocalRot={rig.CameraTransform.localRotation.eulerAngles}";
            }

            string cc = rig.CharacterController != null
                ? $"cc(enabled={rig.CharacterController.enabled}, " +
                  $"height={rig.CharacterController.height:0.00}, " +
                  $"center={rig.CharacterController.center})"
                : "cc=null";

            log.LogInfo($"[MountedKey] {key} down. {holder}, {cam}, {cc}");

            host.StartCoroutine(JitterSnapshot(key, ++snapshotSeq, mount));
        }

        /// <summary>
        /// Samples player drift for 30 frames after a problem key, logging the first ten frames
        /// then every fifth. These are the lines to attach to a jitter bug report.
        /// </summary>
        private IEnumerator JitterSnapshot(string key, int seq, MountController mount)
        {
            for (int i = 0; i < SnapshotFrames; i++)
            {
                yield return null;

                if (!mount.IsMounted)
                    yield break;

                if (rig.ControllerRoot == null || mount.Seat == null)
                    yield break;

                bool shouldLog = (i < 10) || (i == 14) || (i == 19) || (i == 24) || (i == 29);
                if (!shouldLog)
                    continue;

                Vector3 localPos = rig.ControllerRoot.localPosition;
                Vector3 seatDelta = rig.ControllerRoot.position - mount.Seat.position;

                string holder = rig.CameraHolderTransform != null
                    ? $"holderLocalPos={rig.CameraHolderTransform.localPosition} " +
                      $"holderWorldPos={rig.CameraHolderTransform.position}"
                    : "holder=null";
                string cam = rig.CameraTransform != null
                    ? $"camWorldPos={rig.CameraTransform.position} " +
                      $"camLocalPos={rig.CameraTransform.localPosition} " +
                      $"camLocalRot={rig.CameraTransform.localRotation.eulerAngles}"
                    : "cam=null";

                log.LogInfo(
                    $"[JitterSnap #{seq}] key={key} frame={i + 1}/{SnapshotFrames} localPos={localPos} " +
                    $"seatDelta={seatDelta} {holder} {cam} {DescribeWatchedScripts()}");
            }
        }

        private string DescribeWatchedScripts()
        {
            var parts = new string[WatchedScripts.Length];
            for (int i = 0; i < WatchedScripts.Length; i++)
            {
                MonoBehaviour mb = rig.FindScript(WatchedScripts[i]);
                parts[i] = $"{WatchedScripts[i]}={(mb != null ? mb.enabled.ToString() : "null")}";
            }

            return string.Join(", ", parts);
        }

        private HashSet<Transform> BuildCameraChain()
        {
            var chain = new HashSet<Transform>();
            if (rig.CameraTransform == null)
                return chain;

            Transform t = rig.CameraTransform;
            while (t != null)
            {
                chain.Add(t);
                if (t == rig.ControllerRoot)
                    break;
                t = t.parent;
            }

            return chain;
        }

        private static bool MovementKeyWentDown()
        {
            return Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.A) ||
                   Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.D) ||
                   Input.GetKeyDown(KeyCode.Space) ||
                   Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift) ||
                   Input.GetKeyDown(KeyCode.LeftControl) || Input.GetKeyDown(KeyCode.RightControl) ||
                   Input.GetKeyDown(KeyCode.X);
        }
    }
}
