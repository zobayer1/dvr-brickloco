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

            LogAllTrainCarLiveries();

            Camera cam = (Camera.main ?? player.GetComponentInChildren<Camera>(true)) ?? FindObjectOfType<Camera>();
            if (cam == null)
            {
                Logger.LogWarning("Player found, but no Camera found; spawning may be off-screen.");
            }

            Vector3 forward = cam != null ? cam.transform.forward : player.transform.forward;
            Vector3 origin = cam != null ? cam.transform.position : player.transform.position;
            Vector3 spawnPosition = origin + forward * 6f;

            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            
            cube.name = "BrickLoco_TestCube";
            cube.transform.position = spawnPosition;
            cube.transform.localScale = Vector3.one;
            DontDestroyOnLoad(cube);

            cube.layer = GetVisibleLayerForCamera(cam);

            Renderer cubeRenderer = cube.GetComponent<Renderer>();
            if (cubeRenderer != null)
            {
                Shader shader = Shader.Find("Unlit/Color") ?? Shader.Find("Standard");
                if (shader != null)
                {
                    Material mat = new Material(shader)
                    {
                        color = Color.red
                    };

                    if (mat.HasProperty("_EmissionColor"))
                    {
                        mat.EnableKeyword("_EMISSION");
                        mat.SetColor("_EmissionColor", Color.red * 2f);
                    }
                    cubeRenderer.material = mat;
                }
                else
                {
                    Logger.LogWarning("Could not find a suitable shader for the test cube material.");
                }
            }

            Rigidbody rb = cube.AddComponent<Rigidbody>();
            rb.mass = 50f;
            rb.useGravity = false;
            rb.isKinematic = true;

            Debug.DrawLine(
                player.transform.position,
                spawnPosition,
                Color.green,
                10f
            );

            Logger.LogInfo($"Cube activeSelf: {cube.activeSelf}, activeInHierarchy: {cube.activeInHierarchy}");
            Logger.LogInfo($"Cube layer: {cube.layer}");
            if (cam != null)
            {
                Logger.LogInfo($"Camera: {cam.name}, cullingMask: 0x{cam.cullingMask:X8}");
            }

            Logger.LogInfo($"Spawned test cube near player at {spawnPosition}");

            float distance = Vector3.Distance(player.transform.position, spawnPosition);
            Logger.LogInfo($"Distance from player: {distance}");

            yield return new WaitForSeconds(0.5f);
            if (cubeRenderer != null)
            {
                Logger.LogInfo($"Cube renderer enabled: {cubeRenderer.enabled}, isVisible (any camera): {cubeRenderer.isVisible}");
            }
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
    }
}
