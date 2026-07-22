using System.Collections.Generic;
using UnityEngine;

namespace BrickLoco.Game
{
    /// <summary>Transform-to-string helpers for log output.</summary>
    internal static class TransformPath
    {
        /// <summary>
        /// Full scene path of a transform, root first ("Player/Controller/CameraHolder/Camera").
        /// Returns "null" rather than throwing, since this is only ever used in log lines.
        /// </summary>
        public static string Of(Transform t)
        {
            if (t == null)
                return "null";

            var names = new List<string>();
            Transform cur = t;
            while (cur != null)
            {
                names.Add(cur.name);
                cur = cur.parent;
            }

            names.Reverse();
            return string.Join("/", names.ToArray());
        }
    }
}
