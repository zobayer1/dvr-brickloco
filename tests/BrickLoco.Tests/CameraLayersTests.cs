using BrickLoco.Logic;
using Xunit;

namespace BrickLoco.Tests
{
    public class CameraLayersTests
    {
        [Fact]
        public void ReturnsLowestSetLayer()
        {
            int mask = (1 << 8) | (1 << 3);

            Assert.Equal(3, CameraLayers.FirstVisibleLayer(mask));
        }

        [Fact]
        public void ReturnsZeroWhenDefaultLayerIsVisible()
        {
            Assert.Equal(0, CameraLayers.FirstVisibleLayer(1));
        }

        [Fact]
        public void FindsTheOnlyVisibleLayer()
        {
            Assert.Equal(17, CameraLayers.FirstVisibleLayer(1 << 17));
        }

        /// <summary>Layer 31 is the highest Unity supports; the scan must not stop short of it.</summary>
        [Fact]
        public void FindsTheHighestLayer()
        {
            Assert.Equal(31, CameraLayers.FirstVisibleLayer(1 << 31));
        }

        /// <summary>An empty mask falls back to layer 0 (Default) rather than an invalid -1.</summary>
        [Fact]
        public void FallsBackToDefaultLayerForEmptyMask()
        {
            Assert.Equal(0, CameraLayers.FirstVisibleLayer(0));
        }

        [Fact]
        public void EverythingMaskResolvesToDefaultLayer()
        {
            Assert.Equal(0, CameraLayers.FirstVisibleLayer(~0));
        }
    }
}
