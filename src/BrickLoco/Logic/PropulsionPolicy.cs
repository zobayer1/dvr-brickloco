using System;

namespace BrickLoco.Logic
{
    /// <summary>
    /// Speed-gating rules for propulsion. Kept free of UnityEngine types so it can be
    /// unit tested outside the game; see tests/BrickLoco.Tests.
    /// </summary>
    public static class PropulsionPolicy
    {
        /// <summary>
        /// Decides whether a propulsion force may be applied this physics step.
        /// </summary>
        /// <param name="force">Signed force. Positive drives forward, negative reverses.</param>
        /// <param name="forwardSpeed">Current speed along the car's forward axis (m/s, signed).</param>
        /// <param name="maxSpeed">Speed cap (m/s). The sign is ignored.</param>
        /// <returns>True when the force should be applied.</returns>
        public static bool ShouldApplyForce(float force, float forwardSpeed, float maxSpeed)
        {
            float limit = Math.Abs(maxSpeed);

            // Only gate in the direction we are pushing, so a car over the cap can always
            // still be slowed down by pushing the other way.
            if (force > 0f && forwardSpeed >= limit)
                return false;

            if (force < 0f && forwardSpeed <= -limit)
                return false;

            return true;
        }
    }
}
