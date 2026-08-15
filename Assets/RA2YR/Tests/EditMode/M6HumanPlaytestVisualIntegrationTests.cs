using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using RA2YR.Presentation;
using RA2YR.Simulation;
using RA2YR.UnityIntegration;
using UnityEngine;

namespace RA2YR.Tests.EditMode
{
    public sealed class M6HumanPlaytestVisualIntegrationTests
    {
        [Test]
        public void SyntheticOnlyProfileDoesNotResolveExternalVisuals()
        {
            ExternalLegacyVisualProvider provider = ExternalLegacyVisualProvider.Create(
                new HumanPlaytestVisualProfile(
                    HumanPlaytestVisualMode.SyntheticOnly,
                    "Config/ExternalContent.local.xml"),
                ".");
            try
            {
                Assert.That(provider.IsAvailable, Is.False);
                Assert.That(provider.Status.IsConfigured, Is.False);
                VisualAssetProviderResult result = provider.Resolve(new VisualAssetId("external-legacy/playtest/Unit"));
                Assert.That(result.Status, Is.EqualTo(VisualAssetProviderResolutionStatus.Missing));
            }
            finally
            {
                provider.Dispose();
            }
        }

        [Test]
        public void MissingExternalConfigurationFailsClosedWithoutLeakingPath()
        {
            ExternalLegacyVisualProvider provider = ExternalLegacyVisualProvider.Create(
                new HumanPlaytestVisualProfile(
                    HumanPlaytestVisualMode.ExternalLegacyPreferred,
                    "missing-external-content.xml"),
                ".");
            try
            {
                Assert.That(provider.IsAvailable, Is.False);
                Assert.That(provider.Status.IsConfigured, Is.True);
                Assert.That(provider.Status.Message, Does.Not.Contain("missing-external-content.xml"));
                Assert.That(provider.Status.Message, Does.Not.Contain("\\"));
            }
            finally
            {
                provider.Dispose();
            }
        }

        [Test]
        public void ExternalProviderUsesStableProviderIdentity()
        {
            ExternalLegacyVisualProvider provider = ExternalLegacyVisualProvider.Create(
                new HumanPlaytestVisualProfile(
                    HumanPlaytestVisualMode.SyntheticOnly,
                    "Config/ExternalContent.local.xml"),
                ".");
            try
            {
                Assert.That(provider.ProviderId, Is.EqualTo("external-legacy"));
                Assert.That(provider.Resolve(new VisualAssetId("synthetic/playtest/Unit")).Status,
                    Is.EqualTo(VisualAssetProviderResolutionStatus.Missing));
            }
            finally
            {
                provider.Dispose();
            }
        }

        [Test]
        public void VisualProfileRejectsInvalidProbeBudget()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new HumanPlaytestVisualProfile(
                HumanPlaytestVisualMode.ExternalLegacyPreferred,
                "Config/ExternalContent.local.xml",
                maxProbeEntries: -1));
        }

        [Test]
        public void TypedRulesArtResolutionKeepsDistinctRoleIdentity()
        {
            HumanPlaytestVisualRoleProfile profile = new HumanPlaytestVisualRoleProfile(new[]
            {
                new HumanPlaytestVisualRoleBinding(HumanPlaytestVisualRole.HumanBasicUnit, HumanPlaytestRulesRegistry.InfantryTypes, "A"),
                new HumanPlaytestVisualRoleBinding(HumanPlaytestVisualRole.HumanHarvester, HumanPlaytestRulesRegistry.VehicleTypes, "B")
            });
            HumanPlaytestRoleResolutionResult result = HumanPlaytestVisualRoleResolver.Resolve(profile,
                new[]
                {
                    new HumanPlaytestRoleDescriptor(HumanPlaytestVisualRole.HumanBasicUnit, "A", "alpha", false, null),
                    new HumanPlaytestRoleDescriptor(HumanPlaytestVisualRole.HumanHarvester, "B", "bravo", false, null)
                },
                new[]
                {
                    new HumanPlaytestAssetAvailability("alpha.shp", true, false, false, false),
                    new HumanPlaytestAssetAvailability("bravo.shp", true, false, false, false),
                    new HumanPlaytestAssetAvailability("unittem.pal", false, false, false, true)
                });
            Assert.That(result.Resolved.Select(value => value.VisualAssetId).Distinct().Count(), Is.EqualTo(2));
            Assert.That(result.Find(HumanPlaytestVisualRole.HumanBasicUnit).ImageLogicalName, Is.EqualTo("alpha.shp"));
            Assert.That(result.Find(HumanPlaytestVisualRole.HumanHarvester).ImageLogicalName, Is.EqualTo("bravo.shp"));
        }

        [Test]
        public void VoxelAndShpRolesDoNotShareFormatOrFallback()
        {
            HumanPlaytestVisualRoleProfile profile = new HumanPlaytestVisualRoleProfile(new[]
            {
                new HumanPlaytestVisualRoleBinding(HumanPlaytestVisualRole.HumanFactory, HumanPlaytestRulesRegistry.BuildingTypes, "FACT"),
                new HumanPlaytestVisualRoleBinding(HumanPlaytestVisualRole.HumanHarvester, HumanPlaytestRulesRegistry.VehicleTypes, "VXL")
            });
            HumanPlaytestRoleResolutionResult result = HumanPlaytestVisualRoleResolver.Resolve(profile,
                new[]
                {
                    new HumanPlaytestRoleDescriptor(HumanPlaytestVisualRole.HumanFactory, "FACT", "building", false, null),
                    new HumanPlaytestRoleDescriptor(HumanPlaytestVisualRole.HumanHarvester, "VXL", "vehicle", true, null)
                },
                new[]
                {
                    new HumanPlaytestAssetAvailability("building.shp", true, false, false, false),
                    new HumanPlaytestAssetAvailability("vehicle.vxl", false, true, false, false),
                    new HumanPlaytestAssetAvailability("building.vxl", false, true, false, false),
                    new HumanPlaytestAssetAvailability("unittem.pal", false, false, false, true)
                });
            Assert.That(result.Find(HumanPlaytestVisualRole.HumanFactory).Format, Is.EqualTo(HumanPlaytestVisualFormat.Shp));
            Assert.That(result.Find(HumanPlaytestVisualRole.HumanHarvester).Format, Is.EqualTo(HumanPlaytestVisualFormat.VxlStatic));
            Assert.That(result.Find(HumanPlaytestVisualRole.HumanFactory).VisualAssetId, Is.Not.EqualTo(result.Find(HumanPlaytestVisualRole.HumanHarvester).VisualAssetId));
        }

        [Test]
        public void MissingVoxelDoesNotFallbackToUnrelatedShp()
        {
            HumanPlaytestVisualRoleProfile profile = new HumanPlaytestVisualRoleProfile(new[]
            {
                new HumanPlaytestVisualRoleBinding(HumanPlaytestVisualRole.HumanHarvester, HumanPlaytestRulesRegistry.VehicleTypes, "VXL")
            });
            HumanPlaytestRoleResolutionResult result = HumanPlaytestVisualRoleResolver.Resolve(profile,
                new[] { new HumanPlaytestRoleDescriptor(HumanPlaytestVisualRole.HumanHarvester, "VXL", "vehicle", true, null) },
                new[]
                {
                    new HumanPlaytestAssetAvailability("vehicle.shp", true, false, false, false),
                    new HumanPlaytestAssetAvailability("unittem.pal", false, false, false, true)
                });
            Assert.That(result.Resolved, Is.Empty);
            Assert.That(result.Diagnostics.Any(value => value.Code == HumanPlaytestRoleDiagnosticCode.MissingVisualAsset), Is.True);
        }

        [Test]
        public void MissingHvaIsDiagnosedWithoutChangingStaticVoxelFormat()
        {
            HumanPlaytestVisualRoleProfile profile = new HumanPlaytestVisualRoleProfile(new[]
            {
                new HumanPlaytestVisualRoleBinding(HumanPlaytestVisualRole.HumanHarvester, HumanPlaytestRulesRegistry.VehicleTypes, "VXL")
            });
            HumanPlaytestRoleResolutionResult result = HumanPlaytestVisualRoleResolver.Resolve(profile,
                new[] { new HumanPlaytestRoleDescriptor(HumanPlaytestVisualRole.HumanHarvester, "VXL", "vehicle", true, null) },
                new[]
                {
                    new HumanPlaytestAssetAvailability("vehicle.vxl", false, true, false, false),
                    new HumanPlaytestAssetAvailability("unittem.pal", false, false, false, true)
                });
            Assert.That(result.Find(HumanPlaytestVisualRole.HumanHarvester).Format, Is.EqualTo(HumanPlaytestVisualFormat.VxlStatic));
            Assert.That(result.Diagnostics.Any(value => value.Code == HumanPlaytestRoleDiagnosticCode.HvaBindingMissing), Is.True);
        }

        [Test]
        public void SourcePaletteOnlyIsExplicitAndDoesNotGuessRemap()
        {
            HumanPlaytestVisualRoleProfile profile = HumanPlaytestVisualRoleProfile.CreateDefault();
            Assert.That(profile.RemapProfile, Is.EqualTo(HumanPlaytestRemapProfile.SourcePaletteOnly));
            Assert.That(profile.Bindings.Count, Is.GreaterThan(0));
        }

        [Test]
        public void MissingRoleRemainsFallbackWithoutSyntheticAssetResolution()
        {
            HumanPlaytestVisualRoleProfile profile = new HumanPlaytestVisualRoleProfile(new[]
            {
                new HumanPlaytestVisualRoleBinding(HumanPlaytestVisualRole.EnemyBase, HumanPlaytestRulesRegistry.BuildingTypes, "NBASE")
            });
            HumanPlaytestRoleResolutionResult result = HumanPlaytestVisualRoleResolver.Resolve(profile, Array.Empty<HumanPlaytestRoleDescriptor>(), Array.Empty<HumanPlaytestAssetAvailability>());
            Assert.That(result.Unresolved, Does.Contain(HumanPlaytestVisualRole.EnemyBase));
            Assert.That(result.Find(HumanPlaytestVisualRole.EnemyBase), Is.Null);
        }

        [Test]
        public void ProviderSourceDoesNotContainPhysicalVisualCatalog()
        {
            string path = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Assets/RA2YR/UnityIntegration/ExternalLegacyVisualProvider.cs");
            string source = File.ReadAllText(path);
            Assert.That(source, Does.Not.Contain("CandidateLogicalNames"));
            Assert.That(source, Does.Not.Contain("yatech.shp"));
            Assert.That(source, Does.Contain("ResolveTypedIni"));
            Assert.That(source, Does.Contain("BuildRules"));
            Assert.That(source, Does.Contain("BuildArt"));
            Assert.That(source, Does.Contain("TryGetVoxelMesh(HumanPlaytestVisualRole"));
        }

        [Test]
        public void ConfiguredExternalRoleAggregateIsSanitized()
        {
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            string configurationPath = Path.Combine(projectRoot, "Config", "ExternalContent.local.xml");
            if (!File.Exists(configurationPath)) Assert.Ignore("Configured external source is not present on this host.");
            ExternalLegacyVisualProvider provider = ExternalLegacyVisualProvider.Create(
                new HumanPlaytestVisualProfile(HumanPlaytestVisualMode.ExternalLegacyPreferred, configurationPath), projectRoot);
            try
            {
                ExternalLegacyVisualStatus status = provider.Status;
                UnityEngine.Debug.Log("M6_ROLE_AGGREGATE roles=" + status.VisualRolesTotal + ";resolved=" + status.VisualRolesResolvedExternal + ";fallback=" + status.VisualRolesFallback + ";shp=" + status.ShpRolesResolved + ";vxl=" + status.VxlRolesResolved + ";hva=" + status.HvaBindingsResolved + ";palette=" + status.PaletteBindingsResolved + ";humanUnits=" + status.HumanUnitsExternal + ";humanStructures=" + status.HumanStructuresExternal + ";enemyUnits=" + status.EnemyUnitsExternal + ";enemyStructures=" + status.EnemyStructuresExternal + ";unresolved=" + status.UnresolvedRoles + ";fingerprintStable=" + status.SourceFingerprintStable + ";terrain=" + status.TerrainSource);
                Assert.That(status.VisualRolesTotal, Is.GreaterThan(0));
            }
            finally
            {
                provider.Dispose();
            }
        }

        [Test]
        public void VisualProviderResolutionDoesNotMutateSimulationHash()
        {
            HumanPlaytestRuntime runtime = new HumanPlaytestRuntime(HumanPlaytestRuntimeConfig.Default);
            string before = runtime.ComputeStateHash();
            ExternalLegacyVisualProvider provider = ExternalLegacyVisualProvider.Create(
                new HumanPlaytestVisualProfile(HumanPlaytestVisualMode.SyntheticOnly, "Config/ExternalContent.local.xml"), ".");
            try
            {
                Assert.That(runtime.ComputeStateHash(), Is.EqualTo(before));
                Assert.That(provider.Status.RemapProfile, Is.EqualTo(HumanPlaytestRemapProfile.SourcePaletteOnly));
            }
            finally
            {
                provider.Dispose();
            }
        }
    }
}
