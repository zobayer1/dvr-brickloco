using BrickLoco.Logic;
using Xunit;

namespace BrickLoco.Tests
{
    public class PropulsionPolicyTests
    {
        [Fact]
        public void AppliesForwardForceBelowCap()
        {
            Assert.True(PropulsionPolicy.ShouldApplyForce(force: 7000f, forwardSpeed: 5f, maxSpeed: 20f));
        }

        [Fact]
        public void BlocksForwardForceAtCap()
        {
            Assert.False(PropulsionPolicy.ShouldApplyForce(force: 7000f, forwardSpeed: 20f, maxSpeed: 20f));
        }

        [Fact]
        public void BlocksForwardForceAboveCap()
        {
            Assert.False(PropulsionPolicy.ShouldApplyForce(force: 7000f, forwardSpeed: 25f, maxSpeed: 20f));
        }

        [Fact]
        public void BlocksReverseForceAtNegativeCap()
        {
            Assert.False(PropulsionPolicy.ShouldApplyForce(force: -7000f, forwardSpeed: -20f, maxSpeed: 20f));
        }

        /// <summary>
        /// The gate is directional: a car already over the forward cap must still be able to brake.
        /// If this regresses, a runaway car becomes unstoppable.
        /// </summary>
        [Fact]
        public void AllowsReverseForceWhileOverForwardCap()
        {
            Assert.True(PropulsionPolicy.ShouldApplyForce(force: -7000f, forwardSpeed: 25f, maxSpeed: 20f));
        }

        [Fact]
        public void AllowsForwardForceWhileOverReverseCap()
        {
            Assert.True(PropulsionPolicy.ShouldApplyForce(force: 7000f, forwardSpeed: -25f, maxSpeed: 20f));
        }

        /// <summary>A user typing a negative MaxSpeed into the .cfg should not invert the gate.</summary>
        [Theory]
        [InlineData(20f)]
        [InlineData(-20f)]
        public void TreatsMaxSpeedSignAsIrrelevant(float maxSpeed)
        {
            Assert.False(PropulsionPolicy.ShouldApplyForce(7000f, 25f, maxSpeed));
            Assert.True(PropulsionPolicy.ShouldApplyForce(7000f, 5f, maxSpeed));
        }

        /// <summary>A zero cap pins the car: neither direction may be driven.</summary>
        [Fact]
        public void ZeroMaxSpeedBlocksBothDirectionsOnceMoving()
        {
            Assert.False(PropulsionPolicy.ShouldApplyForce(7000f, 0.1f, 0f));
            Assert.False(PropulsionPolicy.ShouldApplyForce(-7000f, -0.1f, 0f));
        }

        /// <summary>Zero force is not gated; it is simply a no-op push at the call site.</summary>
        [Fact]
        public void ZeroForceIsNeverGated()
        {
            Assert.True(PropulsionPolicy.ShouldApplyForce(0f, 999f, 20f));
        }
    }
}
