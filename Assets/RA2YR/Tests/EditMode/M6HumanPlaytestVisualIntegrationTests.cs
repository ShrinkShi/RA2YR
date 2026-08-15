using System;
using NUnit.Framework;
using RA2YR.Presentation;
using RA2YR.UnityIntegration;

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
    }
}
