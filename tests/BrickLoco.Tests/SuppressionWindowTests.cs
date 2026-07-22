using BrickLoco.Logic;
using Xunit;

namespace BrickLoco.Tests
{
    public class SuppressionWindowTests
    {
        [Fact]
        public void OpensAWindowFromTheCurrentTime()
        {
            Assert.Equal(11.5f, SuppressionWindow.Extend(currentUntil: 0f, now: 10f, seconds: 1.5f));
        }

        /// <summary>
        /// Mashing Ctrl/X/Space must never cut a longer window short, or the mitigation
        /// stops covering the jitter it exists to absorb.
        /// </summary>
        [Fact]
        public void NeverShortensAnActiveWindow()
        {
            Assert.Equal(20f, SuppressionWindow.Extend(currentUntil: 20f, now: 10f, seconds: 1.5f));
        }

        [Fact]
        public void ExtendsWhenTheNewWindowReachesFurther()
        {
            Assert.Equal(15f, SuppressionWindow.Extend(currentUntil: 12f, now: 10f, seconds: 5f));
        }

        /// <summary>A negative SuppressProblemKeysSeconds must not push the deadline into the past.</summary>
        [Fact]
        public void ClampsNegativeDurationToZero()
        {
            Assert.Equal(10f, SuppressionWindow.Extend(currentUntil: 0f, now: 10f, seconds: -5f));
        }

        [Fact]
        public void IsActiveBeforeTheDeadline()
        {
            Assert.True(SuppressionWindow.IsActive(currentUntil: 11.5f, now: 10f));
        }

        [Fact]
        public void IsInactiveAtAndAfterTheDeadline()
        {
            Assert.False(SuppressionWindow.IsActive(currentUntil: 10f, now: 10f));
            Assert.False(SuppressionWindow.IsActive(currentUntil: 10f, now: 11f));
        }

        /// <summary>The zero default means "never suppressed" until a key is actually pressed.</summary>
        [Fact]
        public void IsInactiveWithTheDefaultDeadline()
        {
            Assert.False(SuppressionWindow.IsActive(currentUntil: 0f, now: 0f));
        }
    }
}
