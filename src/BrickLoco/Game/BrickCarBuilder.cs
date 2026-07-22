using BrickLoco.Logic;
using DV.ThingTypes;
using UnityEngine;

namespace BrickLoco.Game
{
    /// <summary>
    /// Spawns a Derail Valley car and converts it into a <see cref="BrickCar"/>:
    /// retuned Rigidbody, placeholder cube visuals, and a seat.
    /// </summary>
    internal static class BrickCarBuilder
    {
        private const string LiveryId = "FlatbedShort";

        /// <summary>Logs whether a CarSpawner is reachable. Startup sanity probe.</summary>
        public static void LogSpawnerVisibility(ModLog log)
        {
            var spawner = Object.FindObjectOfType<CarSpawner>();
            log.LogInfo($"CarSpawner found: {spawner != null}");
        }

        /// <summary>
        /// Spawns the brick car on the track closest to <paramref name="position"/>.
        /// Returns null (having logged the reason) if any step fails.
        /// </summary>
        public static BrickCar Spawn(Vector3 position, float mass, ModLog log, bool verbose)
        {
            var spawner = Object.FindObjectOfType<CarSpawner>();
            if (spawner == null)
            {
                log.LogError("CarSpawner not found!");
                return null;
            }

            var livery = FindLiveryById(LiveryId);
            if (livery == null)
            {
                log.LogError($"{LiveryId} livery not found!");
                return null;
            }

            // SpawnCarOnClosestTrack (rather than a raw Instantiate) is what gets the car
            // correctly bogied and railed.
            TrainCar car = spawner.SpawnCarOnClosestTrack(
                position,
                livery,
                flipRotation: false,
                playerSpawnedCar: true,
                uniqueCar: true
            );

            if (car == null)
            {
                log.LogError("SpawnCarOnClosestTrack returned null");
                return null;
            }

            log.LogInfo($"Spawned TrainCar: {car.name}");
            if (verbose)
                log.LogInfo($"Spawned TrainCar position: {car.transform.position}");

            TuneRigidbody(car, mass);
            ReplaceVisualsWithCube(car, log);
            Transform seat = CreateSeat(car, log, verbose);

            return new BrickCar(car, seat);
        }

        private static TrainCarLivery FindLiveryById(string id)
        {
            // Liveries are ScriptableObject assets, not scene objects, so FindObjectOfType
            // will not see them.
            var liveries = Resources.FindObjectsOfTypeAll<TrainCarLivery>();

            foreach (var livery in liveries)
            {
                if (livery.id == id)
                    return livery;
            }

            return null;
        }

        private static void TuneRigidbody(TrainCar car, float mass)
        {
            var rb = car.GetComponent<Rigidbody>();
            if (rb == null)
                return;

            rb.mass = mass;
            rb.centerOfMass = new Vector3(0f, 0.5f, 0f);

            // Placeholder for real bogie physics: hard-freezing pitch and roll is also why
            // the car currently cannot derail.
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
        }

        private static void ReplaceVisualsWithCube(TrainCar car, ModLog log)
        {
            var renderers = car.GetComponentsInChildren<Renderer>(true);
            foreach (var r in renderers)
            {
                r.enabled = false;
            }

            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = "BrickLoco_Visual";

            cube.transform.SetParent(car.transform, false);
            cube.transform.localPosition = Vector3.up * 1.2f;
            cube.transform.localScale = new Vector3(2f, 1f, 1f);

            // Primitives are created on layer 0, which the DV camera may not render — the cube
            // would exist but be invisible. Put it on a layer the camera actually draws.
            cube.layer = VisibleLayerFor(Camera.main);

            var renderer = cube.GetComponent<Renderer>();
            renderer.material = new Material(renderer.material)
            {
                color = Color.red
            };

            // The cube is decoration; its collider would fight the car's real ones.
            Object.Destroy(cube.GetComponent<Collider>());

            log.LogInfo("Replaced TrainCar visuals with brick cube");
        }

        private static Transform CreateSeat(TrainCar car, ModLog log, bool verbose)
        {
            GameObject seat = new GameObject("BrickLoco_Seat");
            seat.transform.SetParent(car.transform, false);
            seat.transform.localPosition = BrickCar.SeatLocalPosition;
            seat.transform.localRotation = Quaternion.identity;

            if (verbose)
                log.LogInfo(
                    $"Created seat under car root: {TransformPath.Of(car.transform)} " +
                    $"seatPos={seat.transform.position}");

            return seat.transform;
        }

        private static int VisibleLayerFor(Camera cam)
        {
            if (cam == null)
                return 0;

            return CameraLayers.FirstVisibleLayer(cam.cullingMask);
        }
    }
}
