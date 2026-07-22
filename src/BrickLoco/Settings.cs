using UnityModManagerNet;

namespace BrickLoco
{
    /// <summary>
    /// Every tunable the mod exposes. Unity Mod Manager persists this to Settings.xml in the
    /// mod folder and renders the [Draw] fields in its in-game window (Ctrl+F10), so most
    /// values apply live — no game restart, unlike the old BepInEx .cfg.
    ///
    /// Field names are part of the mod's public surface: they are the XML element names in
    /// users' Settings.xml. Renaming one silently resets that value to its default.
    /// </summary>
    public class Settings : UnityModManager.ModSettings, IDrawable
    {
        [Draw("Max speed (m/s)")]
        public float MaxSpeed = 20f;

        [Draw("Propulsion force (N, applied while holding G/H)")]
        public float Force = 7000f;

        [Draw("Car mass (kg, applied at spawn)")]
        public float Mass = 20000f;

        [Draw("Debug: mount telemetry logging")]
        public bool MountTelemetry = true;

        [Draw("Debug: dump player components on mount/dismount")]
        public bool DumpOnMount = false;

        [Draw("Disable movement scripts while mounted")]
        public bool DisableScriptsWhileMounted = true;

        [Draw("Scripts to disable while mounted (comma-separated)")]
        public string ScriptsToDisableWhileMounted =
            "LocomotionInputWrapper,CharacterReparenting,CameraAnchorLeanCrouch";

        [Draw("Disable CharacterController while mounted")]
        public bool DisableCharacterControllerWhileMounted = true;

        [Draw("Always disable the critical script set")]
        public bool AlwaysDisableCriticalScripts = true;

        [Draw("Critical scripts (comma-separated)")]
        public string CriticalScriptsToDisable =
            "LocomotionInputWrapper,CharacterReparenting,CameraAnchorLeanCrouch";

        [Draw("Suppress Ctrl/X/Space side effects while mounted")]
        public bool SuppressProblemKeysWhileMounted = true;

        [Draw("Suppression window length (seconds)")]
        public float SuppressProblemKeysSeconds = 1.5f;

        /// <summary>Shorthand for the diagnostic master switch, which gates a lot of logging.</summary>
        public bool Verbose { get { return MountTelemetry; } }

        public override void Save(UnityModManager.ModEntry modEntry)
        {
            Save(this, modEntry);
        }

        public void OnChange()
        {
        }
    }
}
