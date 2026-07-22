using System.Collections.Generic;

namespace BrickLoco.Logic
{
    /// <summary>
    /// Decides which player MonoBehaviours get disabled while the player is mounted.
    /// Kept free of UnityEngine types so it can be unit tested outside the game.
    /// </summary>
    public static class MountScriptPolicy
    {
        /// <summary>
        /// Disabled on every mount regardless of config. These two are what actually stop the
        /// player from walking off the seat; without them the mount does not hold.
        /// </summary>
        public static readonly string[] AlwaysDisabled =
        {
            "LocomotionInputWrapper",
            "CharacterReparenting"
        };

        /// <summary>
        /// Never disabled, even if a user config names it. This is the primary look/controller
        /// script — disabling it leaves the player mounted with a frozen camera.
        /// </summary>
        public const string NeverDisabled = "CustomFirstPersonController";

        /// <summary>
        /// Builds the final set of script type names to disable for a mount.
        /// </summary>
        /// <param name="configuredNames">Comma-separated value of ScriptsToDisableWhileMounted.</param>
        /// <param name="criticalNames">Comma-separated value of CriticalScriptsToDisable.</param>
        /// <param name="includeCritical">Value of AlwaysDisableCriticalScripts.</param>
        public static HashSet<string> BuildDisableSet(
            string configuredNames, string criticalNames, bool includeCritical)
        {
            var toDisable = new HashSet<string>();

            AddCommaSeparatedNames(toDisable, configuredNames);

            if (includeCritical)
                AddCommaSeparatedNames(toDisable, criticalNames);

            for (int i = 0; i < AlwaysDisabled.Length; i++)
                toDisable.Add(AlwaysDisabled[i]);

            toDisable.Remove(NeverDisabled);

            return toDisable;
        }

        /// <summary>
        /// Adds trimmed, non-empty entries from a comma-separated list. Null/empty input is a no-op.
        /// </summary>
        public static void AddCommaSeparatedNames(HashSet<string> dest, string raw)
        {
            if (dest == null)
                return;

            if (string.IsNullOrEmpty(raw))
                return;

            string[] parts = raw.Split(',');
            for (int i = 0; i < parts.Length; i++)
            {
                string p = parts[i];
                if (p == null)
                    continue;

                string trimmed = p.Trim();
                if (trimmed.Length == 0)
                    continue;

                dest.Add(trimmed);
            }
        }
    }
}
