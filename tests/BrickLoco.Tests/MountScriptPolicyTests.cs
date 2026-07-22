using System.Collections.Generic;
using BrickLoco.Logic;
using Xunit;

namespace BrickLoco.Tests
{
    public class MountScriptPolicyTests
    {
        private const string Defaults = "LocomotionInputWrapper,CharacterReparenting,CameraAnchorLeanCrouch";

        [Fact]
        public void IncludesConfiguredNames()
        {
            var set = MountScriptPolicy.BuildDisableSet(Defaults, "", includeCritical: false);

            Assert.Contains("CameraAnchorLeanCrouch", set);
        }

        /// <summary>
        /// These two are what actually hold the player on the seat. An empty config must not
        /// silently produce a mount that the player can walk out of.
        /// </summary>
        [Fact]
        public void AlwaysIncludesTheMovementLockScripts()
        {
            var set = MountScriptPolicy.BuildDisableSet("", "", includeCritical: false);

            Assert.Contains("LocomotionInputWrapper", set);
            Assert.Contains("CharacterReparenting", set);
        }

        [Fact]
        public void AlwaysIncludesTheMovementLockScriptsEvenWhenConfigIsNull()
        {
            var set = MountScriptPolicy.BuildDisableSet(null, null, includeCritical: true);

            Assert.Contains("LocomotionInputWrapper", set);
            Assert.Contains("CharacterReparenting", set);
        }

        /// <summary>Disabling the look controller would leave the player mounted with a frozen camera.</summary>
        [Fact]
        public void NeverDisablesTheLookControllerEvenWhenConfigured()
        {
            var set = MountScriptPolicy.BuildDisableSet("CustomFirstPersonController", "", includeCritical: false);

            Assert.DoesNotContain("CustomFirstPersonController", set);
        }

        [Fact]
        public void NeverDisablesTheLookControllerWhenSmuggledInViaCriticalList()
        {
            var set = MountScriptPolicy.BuildDisableSet("", "CustomFirstPersonController", includeCritical: true);

            Assert.DoesNotContain("CustomFirstPersonController", set);
        }

        [Fact]
        public void MergesCriticalNamesOnlyWhenEnabled()
        {
            var withCritical = MountScriptPolicy.BuildDisableSet("", "WorldBoundaryEnforcer", includeCritical: true);
            var withoutCritical = MountScriptPolicy.BuildDisableSet(
                "", "WorldBoundaryEnforcer", includeCritical: false);

            Assert.Contains("WorldBoundaryEnforcer", withCritical);
            Assert.DoesNotContain("WorldBoundaryEnforcer", withoutCritical);
        }

        [Fact]
        public void DeduplicatesAcrossBothLists()
        {
            var set = MountScriptPolicy.BuildDisableSet(Defaults, Defaults, includeCritical: true);

            Assert.Equal(3, set.Count);
        }

        /// <summary>Hand-edited .cfg files routinely have spaces after commas.</summary>
        [Fact]
        public void TrimsWhitespaceAroundNames()
        {
            var set = MountScriptPolicy.BuildDisableSet("  Alpha , Beta  ", "", includeCritical: false);

            Assert.Contains("Alpha", set);
            Assert.Contains("Beta", set);
        }

        [Fact]
        public void SkipsEmptyEntriesFromTrailingOrDoubledCommas()
        {
            var set = new HashSet<string>();
            MountScriptPolicy.AddCommaSeparatedNames(set, "Alpha,,Beta,");

            Assert.Equal(new[] { "Alpha", "Beta" }, set);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(",")]
        [InlineData("   ")]
        public void AddCommaSeparatedNamesIgnoresDegenerateInput(string raw)
        {
            var set = new HashSet<string>();
            MountScriptPolicy.AddCommaSeparatedNames(set, raw);

            Assert.Empty(set);
        }

        [Fact]
        public void AddCommaSeparatedNamesToleratesNullDestination()
        {
            MountScriptPolicy.AddCommaSeparatedNames(null, "Alpha");
        }

        /// <summary>
        /// Documents that the set is never empty, which is why the plugin's
        /// "if (toDisable.Count == 0) return" guard is unreachable in practice.
        /// </summary>
        [Fact]
        public void ResultIsNeverEmpty()
        {
            var set = MountScriptPolicy.BuildDisableSet("", "", includeCritical: false);

            Assert.NotEmpty(set);
        }
    }
}
