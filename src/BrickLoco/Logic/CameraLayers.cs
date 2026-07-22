namespace BrickLoco.Logic
{
    /// <summary>
    /// Culling-mask arithmetic. Kept free of UnityEngine types so it can be unit tested
    /// outside the game; callers pass <c>Camera.cullingMask</c> in.
    /// </summary>
    public static class CameraLayers
    {
        /// <summary>Unity supports exactly 32 layers, so a culling mask is a 32-bit set.</summary>
        public const int LayerCount = 32;

        /// <summary>
        /// Returns the lowest layer index the mask renders, so a placeholder mesh can be put
        /// on a layer the camera actually draws. Falls back to layer 0 (Default) for an empty mask.
        /// </summary>
        public static int FirstVisibleLayer(int cullingMask)
        {
            for (int i = 0; i < LayerCount; i++)
            {
                if ((cullingMask & (1 << i)) != 0)
                    return i;
            }

            return 0;
        }
    }
}
