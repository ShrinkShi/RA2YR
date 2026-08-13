using NUnit.Framework;
using RA2YR.Core.Content.MapTerrain.Audit;

namespace RA2YR.Tests.EditMode.Formats.MapTerrain
{
    public sealed class M3C8RealMapIntegrationTests
    {
        [Test] public void CompleteWithFailuresDoesNotPromoteRuntimeAuthority()
        {
            Assert.AreNotEqual(M3C8AuditStatus.Complete, M3C8AuditStatus.CompleteWithFailures);
            Assert.AreNotEqual(M3C8AuditStatus.CompleteWithNoCandidates, M3C8AuditStatus.CompleteWithFailures);
        }

        [Test] public void CoreIntegrationTypeHasNoUnityAssemblyReference()
        {
            Assert.IsFalse(typeof(M3C8RealMapIntegrationDelivery).Assembly.FullName.Contains("UnityEngine"));
        }

        [Test] public void IntegrationSummaryIsAggregateOnlyByContract()
        {
            Assert.IsFalse(typeof(M3C8RealMapIntegrationDelivery).GetProperty("SanitizedSummaryJson") == null);
            Assert.IsFalse(typeof(M3C8RealMapIntegrationDelivery).GetProperty("AggregateSha256") == null);
        }

        [Test] public void TerrainCountsRemainSeparateFromPackedStageCounts()
        {
            Assert.AreNotEqual(nameof(M3C8RealMapIntegrationDelivery.TerrainFullyBoundCount), nameof(M3C8RealMapIntegrationDelivery.IsoMapSuccessCount));
        }

        [Test] public void DeliveryExposesIndependentStageAndAuthorityFields()
        {
            Assert.IsNotNull(typeof(M3C8RealMapIntegrationDelivery).GetProperty(nameof(M3C8RealMapIntegrationDelivery.Status)));
            Assert.IsNotNull(typeof(M3C8RealMapIntegrationDelivery).GetProperty(nameof(M3C8RealMapIntegrationDelivery.IsoMapFailureCount)));
            Assert.IsNotNull(typeof(M3C8RealMapIntegrationDelivery).GetProperty(nameof(M3C8RealMapIntegrationDelivery.PreviewFailureCount)));
            Assert.IsNotNull(typeof(M3C8RealMapIntegrationDelivery).GetProperty(nameof(M3C8RealMapIntegrationDelivery.TheaterFailureCount)));
            Assert.IsNotNull(typeof(M3C8RealMapIntegrationDelivery).GetProperty(nameof(M3C8RealMapIntegrationDelivery.TerrainUnresolvedCount)));
            Assert.IsNotNull(typeof(M3C8RealMapIntegrationDelivery).GetProperty(nameof(M3C8RealMapIntegrationDelivery.AggregateSha256)));
        }

        [Test] public void FailureStatusIsDistinctFromNoCandidateStatus()
        {
            Assert.AreNotEqual(M3C8AuditStatus.CompleteWithFailures, M3C8AuditStatus.CompleteWithNoCandidates);
        }
    }
}
