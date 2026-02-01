using BepInEx;
using UnityEngine;

namespace BrickLoco
{
    [BepInPlugin("com.zobayer.brickloco", "Brick Loco", "0.0.1")]
    public class BrickLocoPlugin : BaseUnityPlugin
    {
        private void Awake()
        {
            Logger.LogInfo("BrickLoco loaded");
        }
    }
}
