using System;

namespace BrickLoco.Logic
{
    /// <summary>
    /// Name-based heuristics for locating DV scene objects. Derail Valley gives us no stable
    /// API for "the interior of this car" or "the camera holder", so we match on names and
    /// then confirm via the transform hierarchy at the call site.
    ///
    /// Kept free of UnityEngine types so it can be unit tested outside the game.
    /// </summary>
    public static class TransformNaming
    {
        private const string InteriorToken = "interior";
        private const string CameraHolderName = "CameraHolder";

        /// <summary>
        /// True when a transform name looks like a car interior root, e.g. "CarFlatcarShort(Clone) [interior]".
        /// A positive result is necessary but not sufficient — callers still confirm the hierarchy.
        /// </summary>
        public static bool IsInteriorName(string transformName)
        {
            if (string.IsNullOrEmpty(transformName))
                return false;

            return transformName.IndexOf(InteriorToken, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        /// <summary>
        /// True when an interior's name is prefixed with the car's name, which is DV's usual
        /// "&lt;CarName&gt; [interior]" pattern. Lets us claim an interior without walking the hierarchy.
        /// </summary>
        public static bool BelongsToCarByName(string transformName, string carName)
        {
            if (string.IsNullOrEmpty(transformName) || string.IsNullOrEmpty(carName))
                return false;

            return transformName.StartsWith(carName, StringComparison.Ordinal);
        }

        /// <summary>
        /// Loose match used when inspecting a camera's parent: any name *containing* "CameraHolder".
        /// DV decorates these names per camera rig, so an exact match misses real holders.
        /// </summary>
        public static bool LooksLikeCameraHolder(string transformName)
        {
            if (string.IsNullOrEmpty(transformName))
                return false;

            return transformName.IndexOf(CameraHolderName, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        /// <summary>
        /// Strict match used when scanning a whole subtree, where the loose match would pull in
        /// unrelated children such as "CameraHolderAnchor".
        /// </summary>
        public static bool IsCameraHolderName(string transformName)
        {
            if (string.IsNullOrEmpty(transformName))
                return false;

            return string.Equals(transformName, CameraHolderName, StringComparison.OrdinalIgnoreCase);
        }
    }
}
