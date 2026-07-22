using System;

namespace BrickLoco.Logic
{
    /// <summary>
    /// Timing for the mounted "problem key" (Ctrl / X / Space) mitigation window.
    /// Kept free of UnityEngine types so it can be unit tested outside the game.
    /// </summary>
    public static class SuppressionWindow
    {
        /// <summary>
        /// Extends an active suppression window rather than replacing it, so mashing keys
        /// cannot shorten a window that is already running longer.
        /// </summary>
        /// <param name="currentUntil">Existing deadline (game time, seconds).</param>
        /// <param name="now">Current game time in seconds.</param>
        /// <param name="seconds">Configured window length. Negative values are treated as zero.</param>
        /// <returns>The deadline to store.</returns>
        public static float Extend(float currentUntil, float now, float seconds)
        {
            float clamped = Math.Max(0f, seconds);
            return Math.Max(currentUntil, now + clamped);
        }

        /// <summary>True while the window is open.</summary>
        public static bool IsActive(float currentUntil, float now)
        {
            return now < currentUntil;
        }
    }
}
