using System.Collections;
using BrickLoco.Diagnostics;
using BrickLoco.Game;
using BrickLoco.Mount;
using UnityEngine;

namespace BrickLoco
{
    /// <summary>
    /// The mod's only MonoBehaviour, hosted on the DontDestroyOnLoad GameObject that
    /// <see cref="Loader"/> creates. Owns the Unity lifecycle and input, and delegates
    /// everything else.
    ///
    /// DefaultExecutionOrder(10000) puts these callbacks after Derail Valley's own scripts in
    /// each frame, which is what lets the mount enforcement undo what they just did.
    /// </summary>
    [DefaultExecutionOrder(10000)]
    public class BrickLocoBehaviour : MonoBehaviour
    {
        private Settings config;
        private ModLog log;
        private PlayerRig rig;
        private MountDiagnostics diagnostics;
        private MountController mount;
        private BrickCar car;

        private void Awake()
        {
            config = Loader.Settings;
            log = Loader.Log;

            rig = new PlayerRig();
            diagnostics = new MountDiagnostics(this, rig, log);
            mount = new MountController(config, rig, diagnostics, log);

            // Physics tunables (mass, COM height, tilt freeze) re-apply the moment the user
            // edits them in the UMM window — no respawn needed.
            Loader.SettingsChanged += OnSettingsChanged;
        }

        private void OnSettingsChanged()
        {
            if (car != null && !car.IsGone)
                car.ApplyPhysicsSettings(config);
        }

        private void Start()
        {
            StartCoroutine(WaitForPlayerAndSpawn());
        }

        /// <summary>
        /// UMM loads the mod at DV's StartingPoint, which can be long before a save does —
        /// there is no player to find in the menu scene. Poll until there is, then spawn.
        /// This runs once per host lifetime.
        /// </summary>
        private IEnumerator WaitForPlayerAndSpawn()
        {
            while (!rig.TryCache(log))
                yield return null;

            if (config.Verbose)
                diagnostics.DumpPlayerComponents("CachePlayer");

            // PlayerRig already tried the controller camera, the player camera, Camera.main and
            // any enabled camera — a null here means the whole chain came up empty.
            if (rig.CameraTransform == null)
                log.LogWarning("Player found, but no Camera found; spawning may be off-screen.");

            BrickCarBuilder.LogSpawnerVisibility(log);

            car = BrickCarBuilder.Spawn(rig.Root.position, config, log);
            mount.SetCar(car);

            if (car != null)
                StartCoroutine(RetuneWhenBogiesReady());
        }

        /// <summary>
        /// Bogie rigidbodies are created a few frames after the car spawns, so the physics
        /// settings applied at spawn miss them. Re-apply once the game reports them ready.
        /// </summary>
        private IEnumerator RetuneWhenBogiesReady()
        {
            while (car != null && !car.IsGone && !car.Car.AreBogiesFullyInitialized())
                yield return null;

            if (car != null && !car.IsGone)
                car.ApplyPhysicsSettings(config);
        }

        private void Update()
        {
            if (mount.IsMounted)
            {
                mount.EnforceEarly();

                // Discovery + mitigation for the reported problem keys. These should not affect
                // normal mounted camera/look unless the keys are actually pressed.
                if (Input.GetKeyDown(KeyCode.Space))
                    mount.OnProblemKey("Space");
                if (Input.GetKeyDown(KeyCode.LeftControl) || Input.GetKeyDown(KeyCode.RightControl))
                    mount.OnProblemKey("Ctrl");
                if (Input.GetKeyDown(KeyCode.X))
                    mount.OnProblemKey("X");
            }

            if (config.Verbose && Input.GetKeyDown(KeyCode.F9))
                diagnostics.DumpPlayerComponents(mount.IsMounted ? "F9WhileMounted" : "F9");

            if (Input.GetKeyDown(KeyCode.M))
                mount.Toggle();
        }

        private void LateUpdate()
        {
            mount.EnforceLate();
        }

        private void FixedUpdate()
        {
            if (car == null || car.IsGone)
                return;

            if (!mount.IsMounted)
                return;

            if (Input.GetKey(KeyCode.G))
                car.ApplyForwardForce(config.Force, config.MaxSpeed, config.DriveViaBogies);

            if (Input.GetKey(KeyCode.H))
                car.ApplyForwardForce(-config.Force, config.MaxSpeed, config.DriveViaBogies);
        }

        private void OnDestroy()
        {
            Loader.SettingsChanged -= OnSettingsChanged;

            // The mod was toggled off in UMM (or the game is quitting): put the player and
            // every disabled DV script back before the host disappears.
            if (mount != null && mount.IsMounted)
                mount.Toggle();
        }
    }
}
