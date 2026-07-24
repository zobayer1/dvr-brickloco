using UnityEngine;
using UnityModManagerNet;

namespace BrickLoco
{
    /// <summary>
    /// Unity Mod Manager entry point — named by "EntryMethod" in the deployed Info.json.
    /// Owns the mod lifecycle: settings, logging, and the host GameObject that runs
    /// <see cref="BrickLocoBehaviour"/>.
    ///
    /// UMM calls <see cref="Load"/> at Derail Valley's StartingPoint
    /// (DV.Globals.AssignSelfToHelpers), then OnToggle with the current enabled state and on
    /// every later toggle from the UMM window (Ctrl+F10).
    /// </summary>
    public static class Loader
    {
        internal static Settings Settings { get; private set; }
        internal static ModLog Log { get; private set; }

        /// <summary>Raised when the user edits a setting in the UMM window (Settings.OnChange).</summary>
        internal static event System.Action SettingsChanged;

        private static GameObject host;

        internal static void RaiseSettingsChanged()
        {
            System.Action handler = SettingsChanged;
            if (handler != null)
                handler();
        }

        public static bool Load(UnityModManager.ModEntry modEntry)
        {
            Settings = UnityModManager.ModSettings.Load<Settings>(modEntry);
            Log = new ModLog(modEntry.Logger);

            modEntry.OnToggle = OnToggle;
            modEntry.OnGUI = OnGUI;
            modEntry.OnSaveGUI = OnSaveGUI;

            Log.LogInfo("BrickLoco loaded");
            return true;
        }

        private static bool OnToggle(UnityModManager.ModEntry modEntry, bool enabled)
        {
            if (enabled && host == null)
            {
                host = new GameObject("BrickLoco");
                Object.DontDestroyOnLoad(host);
                host.AddComponent<BrickLocoBehaviour>();
            }
            else if (!enabled && host != null)
            {
                // Destroying the host dismounts the player and restores every disabled DV
                // script (BrickLocoBehaviour.OnDestroy). The spawned car itself stays in the
                // world until the game is restarted.
                Object.Destroy(host);
                host = null;
            }

            return true;
        }

        private static void OnGUI(UnityModManager.ModEntry modEntry)
        {
            Settings.Draw(modEntry);
        }

        private static void OnSaveGUI(UnityModManager.ModEntry modEntry)
        {
            Settings.Save(modEntry);
        }
    }
}
