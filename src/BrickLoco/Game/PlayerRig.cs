using BrickLoco.Logic;
using UnityEngine;

namespace BrickLoco.Game
{
    /// <summary>
    /// Locates and caches the handful of player transforms and components the mod needs.
    ///
    /// Derail Valley exposes no API for "the player's controller root" or "the active camera
    /// holder", so each is found by a chain of fallbacks, most specific first.
    /// </summary>
    internal class PlayerRig
    {
        /// <summary>The object tagged "Player".</summary>
        public Transform Root { get; private set; }

        /// <summary>
        /// The transform the mod parents to a seat — the CharacterController's transform when
        /// there is one, otherwise <see cref="Root"/>.
        /// </summary>
        public Transform ControllerRoot { get; private set; }

        public Transform CameraTransform { get; private set; }

        /// <summary>Reassigned at mount time, since the active camera rig changes when boarding.</summary>
        public Transform CameraHolderTransform { get; set; }

        public CharacterController CharacterController { get; private set; }
        public Rigidbody Rigidbody { get; private set; }

        public bool IsCached { get { return Root != null; } }

        /// <summary>Resolves everything from the tagged Player object. False if there is no player yet.</summary>
        public bool TryCache(ModLog log)
        {
            var player = GameObject.FindWithTag("Player");
            if (player == null)
                return false;

            Root = player.transform;

            CharacterController = Root.GetComponentInChildren<CharacterController>(true);
            Rigidbody = Root.GetComponentInChildren<Rigidbody>(true);

            ControllerRoot = CharacterController != null ? CharacterController.transform : Root;
            CameraTransform = FindCameraTransform(Root, ControllerRoot);
            CameraHolderTransform = FindCameraHolderTransform(ControllerRoot, CameraTransform);

            log.LogInfo(
                $"Player cached. ControllerRoot: {NameOrNull(ControllerRoot)}, " +
                $"Camera: {NameOrNull(CameraTransform)}");
            log.LogInfo(
                $"Player cached. CharacterController: {NameOrNull(CharacterController)}, " +
                $"Rigidbody: {NameOrNull(Rigidbody)}");

            return true;
        }

        private static string NameOrNull(Object o)
        {
            return o != null ? o.name : "null";
        }

        /// <summary>Re-derives ControllerRoot if it was lost. Cheap, so callers may do this defensively.</summary>
        public void EnsureControllerRoot()
        {
            if (ControllerRoot == null)
                ControllerRoot = CharacterController != null ? CharacterController.transform : Root;
        }

        /// <summary>
        /// Finds a MonoBehaviour under the controller root by short type name. Used to reach DV
        /// scripts the mod cannot reference at compile time.
        /// </summary>
        public MonoBehaviour FindScript(string shortTypeName)
        {
            if (ControllerRoot == null || string.IsNullOrEmpty(shortTypeName))
                return null;

            var scripts = ControllerRoot.GetComponentsInChildren<MonoBehaviour>(true);
            if (scripts == null)
                return null;

            for (int i = 0; i < scripts.Length; i++)
            {
                MonoBehaviour mb = scripts[i];
                if (mb == null)
                    continue;
                if (mb.GetType().Name == shortTypeName)
                    return mb;
            }

            return null;
        }

        private static Transform FindCameraTransform(Transform playerRoot, Transform controllerRoot)
        {
            // Prefer a camera under the controller hierarchy (most reliable for our mount logic).
            if (controllerRoot != null)
            {
                Camera camUnderController = controllerRoot.GetComponentInChildren<Camera>(true);
                if (camUnderController != null)
                    return camUnderController.transform;
            }

            // Next, try a camera under the player hierarchy.
            if (playerRoot != null)
            {
                Camera camUnderPlayer = playerRoot.GetComponentInChildren<Camera>(true);
                if (camUnderPlayer != null)
                    return camUnderPlayer.transform;
            }

            // Then prefer MainCamera.
            if (Camera.main != null)
                return Camera.main.transform;

            // Fallback: scan all cameras and pick an enabled one.
            var cams = Resources.FindObjectsOfTypeAll<Camera>();
            if (cams != null)
            {
                for (int i = 0; i < cams.Length; i++)
                {
                    Camera c = cams[i];
                    if (c == null)
                        continue;
                    if (!c.enabled)
                        continue;
                    return c.transform;
                }
            }

            return null;
        }

        private static Transform FindCameraHolderTransform(Transform controllerRoot, Transform cameraTransform)
        {
            if (cameraTransform != null && cameraTransform.parent != null)
            {
                if (TransformNaming.LooksLikeCameraHolder(cameraTransform.parent.name))
                {
                    // Only accept this if it's actually under the controller; otherwise it's
                    // likely a different camera rig.
                    if (controllerRoot != null && cameraTransform.parent.IsChildOf(controllerRoot))
                        return cameraTransform.parent;
                }
            }

            if (controllerRoot == null)
                return null;

            var all = controllerRoot.GetComponentsInChildren<Transform>(true);
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
