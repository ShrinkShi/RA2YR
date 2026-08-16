using System;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using RA2YR.Core.Formats.Mix;
using RA2YR.Core.Formats.Mix.Writing;
using RA2YR.Presentation;
using RA2YR.Simulation;
using RA2YR.Tests.EditMode.Content;
using RA2YR.Tests.EditMode.Formats.ShpTs;
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
                Assert.That(provider.Status.RouteGateStatus, Is.EqualTo(ExternalVisualRouteGateStatus.SourceNotConfigured));
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
                Assert.That(provider.Status.IsConfigured, Is.False);
                Assert.That(provider.Status.RouteGateStatus, Is.EqualTo(ExternalVisualRouteGateStatus.SourceNotConfigured));
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
                new HumanPlaytestVisualProfile(
                    HumanPlaytestVisualMode.ExternalLegacyPreferred,
                    configurationPath,
                    artImagePolicy: HumanPlaytestArtImagePolicy.ExplicitOrSectionIdentifier),
                projectRoot);
            try
            {
                ExternalLegacyVisualStatus status = provider.Status;
                ExternalVisualRouteDiagnostics route = status.RouteDiagnostics;
                UnityEngine.Debug.Log(
                    "M6_EXTERNAL_VISUAL_ROUTE_DIAGNOSTIC" +
                    ";gate=" + route.GateStatus +
                    ";rootMix=" + route.RootMixCount +
                    ";archives=" + route.MountedArchiveCount +
                    ";entries=" + route.MountedEntryCount +
                    ";rulesCandidates=" + route.RulesCandidateCount +
                    ";rulesParsed=" + route.RulesParseSuccessCount +
                    ";rulesComplete=" + route.RulesResolutionComplete +
                    ";artCandidates=" + route.ArtCandidateCount +
                    ";artParsed=" + route.ArtParseSuccessCount +
                    ";artComplete=" + route.ArtResolutionComplete +
                    ";rulesRegistries=" + route.TypedRulesRegistryCount +
                    ";rulesEntries=" + route.TypedRulesEntryCount +
                    ";artRecords=" + route.TypedArtRecordCount +
                    ";artImage=" + route.ArtRecordsWithExplicitImage +
                    ";artNoImage=" + route.ArtRecordsWithoutExplicitImage +
                    ";targetArtImage=" + route.TargetArtRecordsWithExplicitImage +
                    ";targetArtNoImage=" + route.TargetArtRecordsWithoutExplicitImage +
                    ";roleRequests=" + route.RoleBindingsRequested +
                    ";rulesTypeSections=" + route.RulesTypeSectionMatches +
                    ";rulesImageOverrides=" + route.RulesImageOverrides +
                    ";ruleMatches=" + route.RoleRuleMatches +
                    ";artMatches=" + route.RoleArtMatches +
                    ";roleDescriptors=" + route.RoleDescriptorsCreated +
                    ";imageRequests=" + route.ImageLogicalRequests +
                    ";imageMatches=" + route.ImageVfsMatches +
                    ";vxlRequests=" + route.VxlLogicalRequests +
                    ";vxlMatches=" + route.VxlVfsMatches +
                    ";hvaRequests=" + route.HvaLogicalRequests +
                    ";hvaMatches=" + route.HvaVfsMatches +
                    ";paletteRequests=" + route.PaletteLogicalRequests +
                    ";paletteMatches=" + route.PaletteVfsMatches +
                    ";shpDecoded=" + route.ShpDecodeSuccess +
                    ";shpFailed=" + route.ShpDecodeFailed +
                    ";vxlDecoded=" + route.VxlDecodeSuccess +
                    ";vxlFailed=" + route.VxlDecodeFailed +
                    ";hvaBound=" + route.HvaBindSuccess +
                    ";hvaFailed=" + route.HvaBindFailed +
                    ";vehicleMeshRoles=" + route.VehicleMeshRoles +
                    ";sectionAwareRoles=" + route.SectionAwareRoles +
                    ";hvaAppliedRoles=" + route.HvaAppliedRoles +
                    ";paletteColoredRoles=" + route.PaletteColoredRoles +
                    ";maxWidth=" + route.MaxPresentationWidthCells.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture) +
                    ";maxHeight=" + route.MaxPresentationHeightCells.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture) +
                    ";presentationSanity=" + route.PresentationSanityPassed +
                    ";externalRoles=" + route.FinalExternalRoles +
                    ";humanUnits=" + status.HumanUnitsExternal +
                    ";humanStructures=" + status.HumanStructuresExternal +
                    ";enemyUnits=" + status.EnemyUnitsExternal +
                    ";enemyStructures=" + status.EnemyStructuresExternal);
                Assert.That(status.RouteGateStatus, Is.EqualTo(ExternalVisualRouteGateStatus.ExternalVisualsResolved));
                Assert.That(status.IsLocalExternalVisualReady, Is.True);
                Assert.That(status.VisualRolesResolvedExternal, Is.GreaterThanOrEqualTo(2));
                Assert.That(status.HumanUnitsExternal, Is.GreaterThan(0));
                Assert.That(status.EnemyUnitsExternal, Is.GreaterThan(0));
                Assert.That(route.VehicleMeshRoles, Is.GreaterThan(0));
                Assert.That(route.SectionAwareRoles, Is.EqualTo(route.VehicleMeshRoles));
                Assert.That(route.HvaAppliedRoles, Is.EqualTo(route.VehicleMeshRoles));
                Assert.That(route.PaletteColoredRoles, Is.EqualTo(route.VehicleMeshRoles));
                Assert.That(route.MaxPresentationWidthCells, Is.LessThanOrEqualTo(1.5f));
                Assert.That(route.MaxPresentationHeightCells, Is.LessThanOrEqualTo(1.5f));
                Assert.That(route.PresentationSanityPassed, Is.True);

                ResolvedLegacyVisual human;
                bool hasHuman = provider.TryGetResolvedVisual(HumanPlaytestVisualRole.HumanBasicUnit, out human) ||
                                provider.TryGetResolvedVisual(HumanPlaytestVisualRole.HumanHarvester, out human);
                ResolvedLegacyVisual enemy;
                Assert.That(hasHuman, Is.True);
                Assert.That(provider.TryGetResolvedVisual(HumanPlaytestVisualRole.EnemyBasicUnit, out enemy), Is.True);
                Assert.That(human.VisualAssetId, Is.Not.EqualTo(enemy.VisualAssetId));
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

        [Test]
        public void SyntheticNestedMixPipelineResolvesExplicitAndSectionIdentityVoxels()
        {
            using (var temporary = new TemporaryContentTestDirectory())
            {
                HumanPlaytestVisualRoleProfile roles = new HumanPlaytestVisualRoleProfile(new[]
                {
                    new HumanPlaytestVisualRoleBinding(HumanPlaytestVisualRole.HumanBasicUnit, HumanPlaytestRulesRegistry.VehicleTypes, "ALPHA"),
                    new HumanPlaytestVisualRoleBinding(HumanPlaytestVisualRole.EnemyBasicUnit, HumanPlaytestRulesRegistry.VehicleTypes, "BRAVO"),
                    new HumanPlaytestVisualRoleBinding(HumanPlaytestVisualRole.HumanHarvester, HumanPlaytestRulesRegistry.VehicleTypes, "UNSUPPORTED")
                });
                string rules = "[VehicleTypes]\r\n0=ALPHA\r\n1=BRAVO\r\n2=UNSUPPORTED\r\n[ALPHA]\r\n[BRAVO]\r\n[UNSUPPORTED]\r\n";
                string art = "[ALPHA]\r\nImage=alpha-body\r\nVoxel=yes\r\n[BRAVO]\r\nVoxel=yes\r\n";
                ExternalLegacyVisualProvider provider = CreateSyntheticProvider(
                    temporary,
                    rules,
                    art,
                    roles,
                    HumanPlaytestArtImagePolicy.ExplicitOrSectionIdentifier,
                    new[]
                    {
                        Entry("alpha-body.vxl", BuildVxl("BODY", 7)),
                        Entry("alpha-body.hva", BuildHva("BODY")),
                        Entry("BRAVO.vxl", BuildVxl("BODY", 11)),
                        Entry("BRAVO.hva", BuildHva("BODY"))
                    });
                try
                {
                    Assert.That(provider.Status.RouteGateStatus, Is.EqualTo(ExternalVisualRouteGateStatus.ExternalVisualsResolved));
                    Assert.That(provider.Status.RouteDiagnostics.MountedArchiveCount, Is.EqualTo(2));
                    Assert.That(provider.Status.RouteDiagnostics.VxlDecodeSuccess, Is.EqualTo(2));
                    Assert.That(provider.Status.RouteDiagnostics.HvaBindSuccess, Is.EqualTo(2));
                    Assert.That(provider.Status.RouteDiagnostics.PaletteVfsMatches, Is.EqualTo(2));
                    Assert.That(provider.Status.RouteDiagnostics.VehicleMeshRoles, Is.EqualTo(2));
                    Assert.That(provider.Status.RouteDiagnostics.SectionAwareRoles, Is.EqualTo(2));
                    Assert.That(provider.Status.RouteDiagnostics.HvaAppliedRoles, Is.EqualTo(2));
                    Assert.That(provider.Status.RouteDiagnostics.PaletteColoredRoles, Is.EqualTo(2));
                    Assert.That(provider.Status.RouteDiagnostics.PresentationSanityPassed, Is.True);
                    Assert.That(provider.Status.VisualRolesResolvedExternal, Is.EqualTo(2));
                    Assert.That(provider.Status.VisualRolesFallback, Is.EqualTo(1));
                    ResolvedLegacyVisual human;
                    ResolvedLegacyVisual enemy;
                    Assert.That(provider.TryGetResolvedVisual(HumanPlaytestVisualRole.HumanBasicUnit, out human), Is.True);
                    Assert.That(provider.TryGetResolvedVisual(HumanPlaytestVisualRole.EnemyBasicUnit, out enemy), Is.True);
                    Assert.That(human.VisualAssetId, Is.Not.EqualTo(enemy.VisualAssetId));
                    Assert.That(provider.TryGetResolvedVisual(HumanPlaytestVisualRole.HumanHarvester, out _), Is.False);
                    Assert.That(provider.TryGetVoxelMesh(HumanPlaytestVisualRole.HumanBasicUnit, out Mesh humanMesh), Is.True);
                    Assert.That(provider.TryGetVoxelMesh(HumanPlaytestVisualRole.EnemyBasicUnit, out Mesh enemyMesh), Is.True);
                    Assert.That(humanMesh, Is.Not.SameAs(enemyMesh));
                    VxlPresentationAsset humanPresentation;
                    Assert.That(provider.TryGetVoxelPresentation(HumanPlaytestVisualRole.HumanBasicUnit, out humanPresentation), Is.True);
                    Assert.That(humanPresentation.Sections.Count, Is.GreaterThan(0));
                    Assert.That(humanPresentation.Metrics.HvaAppliedSectionCount, Is.EqualTo(humanPresentation.Metrics.SectionCount));
                    Assert.That(humanPresentation.Metrics.DistinctColorCount, Is.GreaterThanOrEqualTo(2));
                    Assert.That(humanPresentation.Metrics.Bounds.WidthCells, Is.LessThanOrEqualTo(1.5f));
                }
                finally
                {
                    provider.Dispose();
                }
            }
        }

        [Test]
        public void ExplicitImagePolicyDoesNotInventMissingArtImageIdentity()
        {
            using (var temporary = new TemporaryContentTestDirectory())
            {
                HumanPlaytestVisualRoleProfile roles = new HumanPlaytestVisualRoleProfile(new[]
                {
                    new HumanPlaytestVisualRoleBinding(HumanPlaytestVisualRole.HumanBasicUnit, HumanPlaytestRulesRegistry.VehicleTypes, "ALPHA")
                });
                ExternalLegacyVisualProvider provider = CreateSyntheticProvider(
                    temporary,
                    "[VehicleTypes]\r\n0=ALPHA\r\n[ALPHA]\r\n",
                    "[ALPHA]\r\nVoxel=yes\r\n",
                    roles,
                    HumanPlaytestArtImagePolicy.ExplicitOnly,
                    new[]
                    {
                        Entry("ALPHA.vxl", BuildVxl("BODY", 5)),
                        Entry("ALPHA.hva", BuildHva("BODY"))
                    });
                try
                {
                    Assert.That(provider.Status.RouteGateStatus, Is.EqualTo(ExternalVisualRouteGateStatus.TypedArtAvailableButNoRoleDescriptors));
                    Assert.That(provider.Status.VisualRolesResolvedExternal, Is.Zero);
                    Assert.That(provider.IsAvailable, Is.False);
                }
                finally
                {
                    provider.Dispose();
                }
            }
        }

        [Test]
        public void SyntheticNestedMixPipelineResolvesStrictRawShpAndChildPalette()
        {
            using (var temporary = new TemporaryContentTestDirectory())
            {
                HumanPlaytestVisualRoleProfile roles = new HumanPlaytestVisualRoleProfile(new[]
                {
                    new HumanPlaytestVisualRoleBinding(HumanPlaytestVisualRole.HumanBase, HumanPlaytestRulesRegistry.BuildingTypes, "STRUCT")
                });
                byte[] shp = ShpTsSyntheticFixtureFactory.Build(
                    1,
                    1,
                    ShpTsSyntheticFixtureFactory.Raw(1, 1, 0, 9));
                ExternalLegacyVisualProvider provider = CreateSyntheticProvider(
                    temporary,
                    "[BuildingTypes]\r\n0=STRUCT\r\n[STRUCT]\r\n",
                    "[STRUCT]\r\nImage=structure-image\r\nVoxel=no\r\n",
                    roles,
                    HumanPlaytestArtImagePolicy.ExplicitOnly,
                    new[] { Entry("structure-image.shp", shp) });
                try
                {
                    Assert.That(provider.Status.RouteGateStatus, Is.EqualTo(ExternalVisualRouteGateStatus.ExternalVisualsResolved));
                    Assert.That(provider.Status.RouteDiagnostics.ShpDecodeSuccess, Is.EqualTo(1));
                    Assert.That(provider.Status.RouteDiagnostics.PaletteVfsMatches, Is.EqualTo(1));
                    Assert.That(provider.TryGetSprite(HumanPlaytestVisualRole.HumanBase, out Sprite sprite), Is.True);
                    Assert.That(sprite, Is.Not.Null);
                }
                finally
                {
                    provider.Dispose();
                }
            }
        }

        private static ExternalLegacyVisualProvider CreateSyntheticProvider(
            TemporaryContentTestDirectory temporary,
            string rules,
            string art,
            HumanPlaytestVisualRoleProfile roles,
            HumanPlaytestArtImagePolicy imagePolicy,
            MixWriteEntry[] visualEntries)
        {
            byte[] visuals = BuildMix(visualEntries);
            byte[] palettes = BuildMix(Entry("unittem.pal", BuildPalette()));
            byte[] root = BuildMix(
                Entry("rules.ini", Encoding.ASCII.GetBytes(rules)),
                Entry("art.ini", Encoding.ASCII.GetBytes(art)),
                Entry("conquer.mix", visuals),
                Entry("cache.mix", palettes));
            temporary.WriteBytes("External/ra2.mix", root);
            string configuration = temporary.WriteText(
                "Repository/Config/ExternalContent.xml",
                "<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
                "<ExternalContent schemaVersion=\"1\" cachePath=\"../../Cache\"><Sources>" +
                "<Source id=\"synthetic-source\" kind=\"Unpacked\" path=\"../../External\" priority=\"100\" version=\"synthetic\" enabled=\"true\" />" +
                "</Sources></ExternalContent>");
            ExternalLegacyVisualProvider provider = ExternalLegacyVisualProvider.Create(
                new HumanPlaytestVisualProfile(
                    HumanPlaytestVisualMode.ExternalLegacyPreferred,
                    configuration,
                    sourceId: "synthetic-source",
                    roleProfile: roles,
                    artImagePolicy: imagePolicy),
                temporary.GetPath("Repository"));
            ExternalVisualRouteDiagnostics route = provider.Status.RouteDiagnostics;
            TestContext.WriteLine(
                "M6_SYNTHETIC_VISUAL_ROUTE" +
                ";gate=" + route.GateStatus +
                ";roots=" + route.RootMixCount +
                ";archives=" + route.MountedArchiveCount +
                ";entries=" + route.MountedEntryCount +
                ";rulesCandidates=" + route.RulesCandidateCount +
                ";rulesParsed=" + route.RulesParseSuccessCount +
                ";rulesComplete=" + route.RulesResolutionComplete +
                ";rulesRegistries=" + route.TypedRulesRegistryCount +
                ";rulesEntries=" + route.TypedRulesEntryCount +
                ";artCandidates=" + route.ArtCandidateCount +
                ";artParsed=" + route.ArtParseSuccessCount +
                ";artComplete=" + route.ArtResolutionComplete +
                ";artRecords=" + route.TypedArtRecordCount +
                ";vehicleMeshRoles=" + route.VehicleMeshRoles +
                ";sectionAwareRoles=" + route.SectionAwareRoles +
                ";hvaAppliedRoles=" + route.HvaAppliedRoles +
                ";paletteColoredRoles=" + route.PaletteColoredRoles +
                ";maxWidth=" + route.MaxPresentationWidthCells.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture) +
                ";maxHeight=" + route.MaxPresentationHeightCells.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture) +
                ";presentationSanity=" + route.PresentationSanityPassed);
            return provider;
        }

        private static MixWriteEntry Entry(string logicalName, byte[] payload)
        {
            return new MixWriteEntry(MixFileId.ComputeCandidateId(logicalName), payload);
        }

        private static byte[] BuildMix(params MixWriteEntry[] entries)
        {
            MixWriteResult result = MixArchiveWriter.Build(entries, MixWriteOptions.ClassicDeterministic);
            if (!result.IsSuccess) throw new InvalidOperationException("Synthetic MIX construction failed.");
            return result.GetArchiveBytes();
        }

        private static byte[] BuildPalette()
        {
            var bytes = new byte[768];
            for (int index = 0; index < bytes.Length; index++) bytes[index] = checked((byte)(index % 64));
            return bytes;
        }

        private static byte[] BuildVxl(string sectionName, byte colorIndex)
        {
            const int headerLength = 802;
            const int sectionHeaderLength = 28;
            const int bodyLength = 26;
            const int tailerLength = 92;
            var bytes = new byte[headerLength + sectionHeaderLength + bodyLength + tailerLength];
            WriteAscii(bytes, 0, "Voxel Animation", 16);
            Write32(bytes, 16, 1);
            Write32(bytes, 20, 1);
            Write32(bytes, 24, 1);
            Write32(bytes, 28, bodyLength);
            WriteAscii(bytes, headerLength, sectionName, 16);
            int body = headerLength + sectionHeaderLength;
            Write32(bytes, body, 0);
            Write32(bytes, body + 4, 5);
            Write32(bytes, body + 8, 4);
            Write32(bytes, body + 12, 9);
            bytes[body + 16] = 0;
            bytes[body + 17] = 1;
            bytes[body + 18] = colorIndex;
            bytes[body + 19] = 2;
            bytes[body + 20] = 1;
            bytes[body + 21] = 0;
            bytes[body + 22] = 1;
            bytes[body + 23] = (byte)(colorIndex + 1);
            bytes[body + 24] = 2;
            bytes[body + 25] = 1;
            int tailer = body + bodyLength;
            Write32(bytes, tailer, 0);
            Write32(bytes, tailer + 4, 8);
            Write32(bytes, tailer + 8, 16);
            Write32(bytes, tailer + 12, 0x3f800000);
            bytes[tailer + 88] = 2;
            bytes[tailer + 89] = 1;
            bytes[tailer + 90] = 1;
            bytes[tailer + 91] = 2;
            return bytes;
        }

        private static byte[] BuildHva(string sectionName)
        {
            var bytes = new byte[24 + 16 + 48];
            Write32(bytes, 16, 1);
            Write32(bytes, 20, 1);
            WriteAscii(bytes, 24, sectionName, 16);
            Write32(bytes, 40, 0x3f800000);
            Write32(bytes, 60, 0x3f800000);
            Write32(bytes, 80, 0x3f800000);
            return bytes;
        }

        private static void WriteAscii(byte[] bytes, int offset, string value, int maxLength)
        {
            byte[] encoded = Encoding.ASCII.GetBytes(value);
            Buffer.BlockCopy(encoded, 0, bytes, offset, Math.Min(encoded.Length, maxLength));
        }

        private static void Write32(byte[] bytes, int offset, int value)
        {
            bytes[offset] = (byte)value;
            bytes[offset + 1] = (byte)(value >> 8);
            bytes[offset + 2] = (byte)(value >> 16);
            bytes[offset + 3] = (byte)(value >> 24);
        }
    }
}
