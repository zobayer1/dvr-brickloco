using BepInEx;
using DV.ThingTypes;
using UnityEngine;

namespace BrickLoco
{
    [BepInPlugin("com.zobayer.brickloco", "Brick Loco", "0.0.1")]
    public class BrickLocoPlugin : BaseUnityPlugin
    {
        private void Start()
        {
            StartCoroutine(WaitForPlayerAndSpawn());
        }

        private void Awake()
        {
            Logger.LogInfo("BrickLoco loaded");
        }

        private static int GetVisibleLayerForCamera(Camera cam)
        {
            if (cam == null)
            {
                return 0;
            }

            for (int i = 0; i < 32; i++)
            {
                if ((cam.cullingMask & (1 << i)) != 0)
                {
                    return i;
                }
            }

            return 0;
        }
        
        private System.Collections.IEnumerator WaitForPlayerAndSpawn()
        {
            GameObject player = null;
            while (player == null)
            {
                player = GameObject.FindWithTag("Player");
                yield return null;
            }

            Camera cam = (Camera.main ?? player.GetComponentInChildren<Camera>(true)) ?? FindObjectOfType<Camera>();
            if (cam == null)
            {
                Logger.LogWarning("Player found, but no Camera found; spawning may be off-screen.");
            }

            LogAllTrainCarLiveries();
            TestCarSpawnerVisibility();
            SpawnFlatbedShort(player.transform.position);

            yield break;
        }

        private void LogAllTrainCarLiveries()
        {
            var liveries = Resources.FindObjectsOfTypeAll<TrainCarLivery>();

            Logger.LogInfo($"Found {liveries.Length} TrainCarLivery assets");

            foreach (var livery in liveries)
            {
                string prefabName = livery.prefab != null ? livery.prefab.name : "NULL";
                Logger.LogInfo($"Livery id={livery.id}, prefab={prefabName}, hidden={livery.isHidden}");
            }
        }

        private void TestCarSpawnerVisibility()
        {
            var spawner = FindObjectOfType<CarSpawner>();
            Logger.LogInfo($"CarSpawner found: {spawner != null}");
        }

        private TrainCarLivery FindLiveryById(string id)
        {
            var liveries = Resources.FindObjectsOfTypeAll<TrainCarLivery>();

            foreach (var livery in liveries)
            {
                if (livery.id == id)
                    return livery;
            }

            return null;
        }

        private void SpawnFlatbedShort(Vector3 position)
        {
            var spawner = FindObjectOfType<CarSpawner>();
            if (spawner == null)
            {
                Logger.LogError("CarSpawner not found!");
                return;
            }

            var livery = FindLiveryById("FlatbedShort");
            if (livery == null)
            {
                Logger.LogError("FlatbedShort livery not found!");
                return;
            }

            TrainCar car = spawner.SpawnCarOnClosestTrack(
                position,
                livery,
                flipRotation: false,
                playerSpawnedCar: true,
                uniqueCar: true
            );

            if (car != null)
            {
                Logger.LogInfo($"Spawned TrainCar: {car.name}");
                ReplaceVisualsWithCube(car);
            }
            else
            {
                Logger.LogError("SpawnCarOnClosestTrack returned null");
            }
        }

        private void ReplaceVisualsWithCube(TrainCar car)
        {
            // Disable all existing renderers
            var renderers = car.GetComponentsInChildren<Renderer>(true);
            foreach (var r in renderers)
            {
                r.enabled = false;
            }

            // Create a cube as the new visual
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = "BrickLoco_Visual";

            // Parent to the TrainCar
            cube.transform.SetParent(car.transform, false);

            // Position & scale (temporary values)
            cube.transform.localPosition = Vector3.up * 1.2f;
            cube.transform.localScale = new Vector3(2f, 1f, 1f);

            // Layer fix (same trick as before)
            Camera cam = Camera.main;
            cube.layer = GetVisibleLayerForCamera(cam);

            // Material (reuse what already works)
            var renderer = cube.GetComponent<Renderer>();
            renderer.material = new Material(renderer.material)
            {
                color = Color.red
            };

            // Remove physics from visual
            Destroy(cube.GetComponent<Collider>());

            Logger.LogInfo("Replaced TrainCar visuals with brick cube");
        }
    }
}
