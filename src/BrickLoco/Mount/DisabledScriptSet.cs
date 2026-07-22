using System.Collections.Generic;
using UnityEngine;

namespace BrickLoco.Mount
{
    /// <summary>
    /// Tracks MonoBehaviours the mod has switched off, remembering each one's prior state so
    /// restore puts back what was actually there — a script already disabled before a mount
    /// stays disabled afterwards.
    /// </summary>
    internal class DisabledScriptSet
    {
        private struct Entry
        {
            public MonoBehaviour Script;
            public bool WasEnabled;
        }

        private readonly List<Entry> entries = new List<Entry>();

        public int Count { get { return entries.Count; } }

        /// <summary>
        /// Records a script and disables it if needed.
        /// </summary>
        /// <returns>True if the script was enabled and has now been disabled.</returns>
        public bool Track(MonoBehaviour script)
        {
            if (script == null)
                return false;

            bool wasEnabled = script.enabled;
            entries.Add(new Entry { Script = script, WasEnabled = wasEnabled });

            if (!wasEnabled)
                return false;

            script.enabled = false;
            return true;
        }

        /// <summary>
        /// Re-disables anything the game switched back on. DV re-enables movement components
        /// after hierarchy changes, so this runs every frame while mounted.
        /// </summary>
        /// <returns>How many scripts had to be re-disabled.</returns>
        public int Enforce()
        {
            int reDisabled = 0;

            for (int i = 0; i < entries.Count; i++)
            {
                Entry e = entries[i];
                if (e.Script == null)
                    continue;
                if (!e.Script.enabled)
                    continue;

                e.Script.enabled = false;
                reDisabled++;
            }

            return reDisabled;
        }

        /// <summary>Restores every tracked script to its prior state and clears the set.</summary>
        public void RestoreAll()
        {
            for (int i = 0; i < entries.Count; i++)
            {
                Entry e = entries[i];
                if (e.Script == null)
                    continue;

                e.Script.enabled = e.WasEnabled;
            }

            entries.Clear();
        }
    }
}
