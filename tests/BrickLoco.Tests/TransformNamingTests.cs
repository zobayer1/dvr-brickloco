using BrickLoco.Logic;
using Xunit;

namespace BrickLoco.Tests
{
    public class TransformNamingTests
    {
        [Theory]
        [InlineData("CarFlatcarShort(Clone) [interior]")]
        [InlineData("Interior")]
        [InlineData("INTERIOR_LOD0")]
        public void RecognisesInteriorNames(string name)
        {
            Assert.True(TransformNaming.IsInteriorName(name));
        }

        [Theory]
        [InlineData("CarFlatcarShort(Clone)")]
        [InlineData("Bogie_F")]
        [InlineData("")]
        [InlineData(null)]
        public void RejectsNonInteriorNames(string name)
        {
            Assert.False(TransformNaming.IsInteriorName(name));
        }

        [Fact]
        public void MatchesInteriorPrefixedWithCarName()
        {
            Assert.True(TransformNaming.BelongsToCarByName(
                "CarFlatcarShort(Clone) [interior]", "CarFlatcarShort(Clone)"));
        }

        /// <summary>
        /// Two spawned cars of the same livery differ only by clone suffix, so a prefix match
        /// must not claim another car's interior.
        /// </summary>
        [Fact]
        public void DoesNotMatchADifferentCarsInterior()
        {
            Assert.False(TransformNaming.BelongsToCarByName(
                "CarBoxcarBrown(Clone) [interior]", "CarFlatcarShort(Clone)"));
        }

        [Theory]
        [InlineData(null, "Car")]
        [InlineData("Car [interior]", null)]
        [InlineData("", "")]
        public void BelongsToCarByNameRejectsMissingInput(string transformName, string carName)
        {
            Assert.False(TransformNaming.BelongsToCarByName(transformName, carName));
        }

        /// <summary>
        /// The two camera-holder matchers differ on purpose: the loose one inspects a known
        /// camera's parent, the strict one scans an entire subtree where decorated names abound.
        /// </summary>
        [Fact]
        public void LooseCameraHolderMatchAcceptsDecoratedNames()
        {
            Assert.True(TransformNaming.LooksLikeCameraHolder("PlayerCameraHolder"));
            Assert.True(TransformNaming.LooksLikeCameraHolder("CameraHolderAnchor"));
        }

        [Fact]
        public void StrictCameraHolderMatchRejectsDecoratedNames()
        {
            Assert.True(TransformNaming.IsCameraHolderName("CameraHolder"));
            Assert.False(TransformNaming.IsCameraHolderName("CameraHolderAnchor"));
            Assert.False(TransformNaming.IsCameraHolderName("PlayerCameraHolder"));
        }

        [Fact]
        public void BothCameraHolderMatchersIgnoreCase()
        {
            Assert.True(TransformNaming.LooksLikeCameraHolder("cameraholder"));
            Assert.True(TransformNaming.IsCameraHolderName("cameraholder"));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void CameraHolderMatchersRejectMissingInput(string name)
        {
            Assert.False(TransformNaming.LooksLikeCameraHolder(name));
            Assert.False(TransformNaming.IsCameraHolderName(name));
        }
    }
}
