using BrickLoco.Game;
using BrickLoco.Logic;
using UnityEngine;

namespace BrickLoco.Mount
{
    /// <summary>
    /// Mitigation for the mounted sink-and-reset jitter loop triggered by Ctrl / X / Space
    /// (crouch, lean, jump).
    ///
    /// Pressing one of those keys opens a short window during which a set of suspected DV
    /// movement scripts is switched off, then restored when the window closes. This is a
    /// mitigation built from log analysis, not a root-cause fix — see docs/mounting.md.
    /// </summary>
    internal class ProblemKeySuppressor
    {
        /// <summary>
        /// Scripts switched off for the duration of the window, in the order they are applied.
        /// Each is a suspect narrowed down from jitter telemetry, not a confirmed cause.
        /// </summary>
        private static readonly string[] SuppressedTypeNames =
        {
            "CameraAnchorLeanCrouch",           // applies lean/crouch camera offsets
            "MovementFlagUpdater",              // recomputes movement state flags
            "FallThroughTerrainFix",            // teleports the player up out of geometry
            "TeleportForbiddenOverlapSafety",   // teleports the player out of forbidden overlaps
            "WalkableControlOverlapDisabler",   // toggles walkable-surface controls on overlap
            "WorldBoundaryEnforcer"             // pushes the player back inside world bounds
        };

        private class Suppressed
        {
            public string TypeName;
            public MonoBehaviour Script;
            public bool WasEnabled;
            public bool IsDisabled;
        }

        private readonly Suppressed[] entries;
        private readonly PlayerRig rig;
        private readonly ModLog log;

        private float activeUntil;

        public ProblemKeySuppressor(PlayerRig rig, ModLog log)
        {
            this.rig = rig;
            this.log = log;

            entries = new Suppressed[SuppressedTypeNames.Length];
            for (int i = 0; i < SuppressedTypeNames.Length; i++)
                entries[i] = new Suppressed { TypeName = SuppressedTypeNames[i] };
        }

        public bool IsActive(float now)
        {
            return SuppressionWindow.IsActive(activeUntil, now);
        }

        /// <summary>
        /// Opens or extends the window. Extending rather than replacing means mashing keys
        /// cannot cut short a window that is already running longer.
        /// </summary>
        public void Extend(float now, float seconds)
        {
            activeUntil = SuppressionWindow.Extend(activeUntil, now, seconds);
        }

        /// <summary>Clears the window and all bookkeeping. Called when a mount begins.</summary>
        public void Reset()
        {
            activeUntil = 0f;

            for (int i = 0; i < entries.Length; i++)
            {
                entries[i].Script = null;
                entries[i].IsDisabled = false;
                entries[i].WasEnabled = false;
            }
        }

        /// <summary>
        /// Disables any not-yet-disabled suppressed script. Called every frame while the
        /// window is open: a script that is missing or already disabled is retried next frame,
        /// since DV creates and re-enables these dynamically.
        /// </summary>
        public void Apply(bool verbose)
        {
            for (int i = 0; i < entries.Length; i++)
            {
                Suppressed e = entries[i];
                if (e.IsDisabled)
                    continue;

                e.Script = rig.FindScript(e.TypeName);
                if (e.Script == null)
                    continue;

                e.WasEnabled = e.Script.enabled;
                if (!e.WasEnabled)
                    continue;

                e.Script.enabled = false;
                e.IsDisabled = true;

                if (verbose)
                    log.LogInfo($"[Mitigation] Disabled {e.TypeName} during suppression window.");
            }
        }

        /// <summary>Restores everything this suppressor disabled. Safe to call every frame.</summary>
        public void RestoreAll(bool verbose)
        {
            for (int i = 0; i < entries.Length; i++)
            {
                Suppressed e = entries[i];
                if (!e.IsDisabled)
                    continue;

                if (e.Script != null)
                    e.Script.enabled = e.WasEnabled;

                e.Script = null;
                e.IsDisabled = false;

                if (verbose)
                    log.LogInfo($"[Mitigation] Restored {e.TypeName} after suppression window.");
            }
        }
    }
}
