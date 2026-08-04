using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using RA2YR.Core.Content;
using RA2YR.Core.Content.ShpTs.Audit;
using RA2YR.Core.Content.ShpTs.Forensics;

namespace RA2YR.Tests.EditMode.Content.ShpTs.Forensics
{
    public sealed class ShpTsRleForensicAuditTests
    {
        [Test]
        public void LockedBaselineAggregateIsAccepted()
        {
            Assert.DoesNotThrow(() => ShpTsRleForensicAuditService
                .ValidateBaselineLockSnapshot(
                    257, 257, 257, 257, 14, 202, 137, 120, true));
        }

        [TestCase(256, 257, 257, 257, 14, 202, 137, 120, true)]
        [TestCase(257, 256, 257, 257, 14, 202, 137, 120, true)]
        [TestCase(257, 257, 256, 257, 14, 202, 137, 120, true)]
        [TestCase(257, 257, 257, 256, 14, 202, 137, 120, true)]
        [TestCase(257, 257, 257, 257, 13, 202, 137, 120, true)]
        [TestCase(257, 257, 257, 257, 14, 203, 137, 120, true)]
        [TestCase(257, 257, 257, 257, 14, 202, 136, 121, true)]
        [TestCase(257, 257, 257, 257, 14, 202, 137, 120, false)]
        public void BaselineAggregateDriftFailsClosed(
            int candidates,
            int failures,
            int rowZeroFailures,
            int overflowFailures,
            int minimumWidth,
            int maximumWidth,
            int oddWidths,
            int evenWidths,
            bool allWidthPlusOne)
        {
            ShpTsRleForensicAuditException exception = Assert.Throws<
                ShpTsRleForensicAuditException>(() =>
                ShpTsRleForensicAuditService.ValidateBaselineLockSnapshot(
                    candidates,
                    failures,
                    rowZeroFailures,
                    overflowFailures,
                    minimumWidth,
                    maximumWidth,
                    oddWidths,
                    evenWidths,
                    allWidthPlusOne));

            Assert.That(exception.Code,
                Is.EqualTo(ShpTsRleForensicAuditFailureCode.BaselineProbeInputDrift));
        }

        [Test]
        public void AllRowsUsingFinalZeroRunGuardReachA1()
        {
            ShpTsRleForensicFrameRecord[] records = Categories()
                .Select((category, index) => Record(
                    "sample-" + index,
                    category,
                    index,
                    Guard(4, 0),
                    Guard(4, 0),
                    Guard(4, 1)))
                .ToArray();

            Assert.That(ShpTsRleForensicAuditService.Decide(records, true),
                Is.EqualTo(ShpTsRleForensicDecision.A1));
        }

        [Test]
        public void MixedRowsSharedAcrossCategoriesReachB()
        {
            ShpTsRleForensicFrameRecord[] records = Categories()
                .Select((category, index) => Record(
                    "sample-" + index,
                    category,
                    index,
                    Guard(4, 0),
                    Guard(4, 0),
                    Exact(4, 1)))
                .ToArray();

            Assert.That(ShpTsRleForensicAuditService.Decide(records, true),
                Is.EqualTo(ShpTsRleForensicDecision.B));
        }

        [Test]
        public void CategorySpecificRowContractsReachD()
        {
            ShpTsRleForensicFrameRecord[] records =
            {
                Record("building", ShpTsRleForensicCategory.Building, 0,
                    Guard(4, 0), Guard(4, 0)),
                Record("infantry", ShpTsRleForensicCategory.Infantry, 1,
                    Guard(4, 0), Exact(4, 0))
            };

            Assert.That(ShpTsRleForensicAuditService.Decide(records, true),
                Is.EqualTo(ShpTsRleForensicDecision.D));
        }

        [Test]
        public void LiteralOverflowReachesC()
        {
            ShpTsRleForensicFrameRecord[] records =
            {
                Record("literal", ShpTsRleForensicCategory.Animation, 0,
                    Guard(4, 0), LiteralOverflow(4, 0))
            };

            Assert.That(ShpTsRleForensicAuditService.Decide(records, true),
                Is.EqualTo(ShpTsRleForensicDecision.C));
        }

        [Test]
        public void StageAWithoutGuardGateRemainsE()
        {
            ShpTsRleForensicFrameRecord[] records =
            {
                new ShpTsRleForensicFrameRecord(
                    "exact",
                    ShpTsRleForensicCategory.Animation,
                    0,
                    4,
                    1,
                    Exact(4, 0),
                    null)
            };

            Assert.That(ShpTsRleForensicAuditService.Decide(records, false),
                Is.EqualTo(ShpTsRleForensicDecision.E));
        }

        [Test]
        public void SanitizedSummaryContainsOnlyAggregatesAndUpdatedHypotheses()
        {
            ShpTsRleForensicFrameRecord[] records = Categories()
                .Select((category, index) => Record(
                    "sample-" + index,
                    category,
                    index,
                    Guard(4, 0),
                    Guard(4, 0),
                    Exact(4, 1)))
                .ToArray();
            var model = new ShpTsRleForensicAuditModel(
                Source(),
                Sha('a'),
                Sha('b'),
                records,
                true,
                ShpTsRleForensicDecision.B,
                true,
                new DateTime(2026, 8, 3, 0, 0, 0, DateTimeKind.Utc),
                new DateTime(2026, 8, 3, 0, 0, 1, DateTimeKind.Utc));
            var external = new ShpTsAuditExternalManifestReference(
                "m2-shp1/forensic/test.json",
                10,
                Sha('c'));

            string summary = ShpTsRleForensicSerializer.SerializeSanitizedSummary(
                model,
                external);

            Assert.That(summary, Does.Contain("\"decision\":\"B\""));
            Assert.That(summary, Does.Contain(
                "final-zero-run-boundary-explains-only-width-plus-one-rows"));
            Assert.That(summary, Does.Contain(
                "supported-for-width-plus-one-rows-but-not-universal"));
            Assert.That(summary, Does.Not.Contain("\"records\":["));
            Assert.That(summary, Does.Not.Contain("\"frameIndex\":"));
            Assert.That(summary, Does.Not.Contain(Path.GetTempPath()));
        }

        [Test]
        public void ExternalManifestBudgetFailsClosed()
        {
            ShpTsRleForensicFrameRecord record = Record(
                "sample",
                ShpTsRleForensicCategory.Building,
                0,
                Guard(4, 0),
                Guard(4, 0));
            var model = new ShpTsRleForensicAuditModel(
                Source(),
                Sha('a'),
                Sha('b'),
                new[] { record },
                true,
                ShpTsRleForensicDecision.A1,
                true,
                DateTime.UtcNow,
                DateTime.UtcNow);

            ShpTsRleForensicAuditException exception = Assert.Throws<
                ShpTsRleForensicAuditException>(() =>
                ShpTsRleForensicSerializer.SerializeExternalManifestUtf8(model, 1));

            Assert.That(exception.Code,
                Is.EqualTo(ShpTsRleForensicAuditFailureCode.ManifestBudgetExceeded));
        }

        private static ShpTsRleForensicFrameRecord Record(
            string sampleId,
            ShpTsRleForensicCategory category,
            int frameIndex,
            ShpTsRleForensicRowScalar stageA,
            params ShpTsRleForensicRowScalar[] rows)
        {
            return new ShpTsRleForensicFrameRecord(
                sampleId,
                category,
                frameIndex,
                4,
                checked((ushort)rows.Length),
                stageA,
                ShpTsRleForensicFrameAnalysis.Success(
                    frameIndex,
                    4,
                    checked((ushort)rows.Length),
                    rows));
        }

        private static ShpTsRleForensicRowScalar Exact(ushort width, int row)
        {
            return Row(
                width,
                row,
                width,
                width,
                ShpTsRleForensicExtraSource.None,
                ShpTsRleForensicCommandKind.Literal,
                false,
                false);
        }

        private static ShpTsRleForensicRowScalar Guard(ushort width, int row)
        {
            return Row(
                width,
                row,
                checked((long)width + 1),
                width,
                ShpTsRleForensicExtraSource.ZeroRun,
                ShpTsRleForensicCommandKind.ZeroRun,
                false,
                true);
        }

        private static ShpTsRleForensicRowScalar LiteralOverflow(ushort width, int row)
        {
            return Row(
                width,
                row,
                checked((long)width + 1),
                checked((long)width + 1),
                ShpTsRleForensicExtraSource.Literal,
                ShpTsRleForensicCommandKind.Literal,
                true,
                false);
        }

        private static ShpTsRleForensicRowScalar Row(
            ushort width,
            int row,
            long mechanical,
            long xccVisible,
            ShpTsRleForensicExtraSource extraSource,
            ShpTsRleForensicCommandKind finalCommand,
            bool literalOverflow,
            bool guard)
        {
            bool hasExtra = extraSource != ShpTsRleForensicExtraSource.None;
            return new ShpTsRleForensicRowScalar(
                row,
                width,
                6,
                4,
                finalCommand == ShpTsRleForensicCommandKind.Literal ? 4 : 2,
                finalCommand == ShpTsRleForensicCommandKind.ZeroRun ? 1 : 0,
                0,
                mechanical,
                xccVisible,
                0,
                true,
                extraSource,
                guard,
                hasExtra,
                guard,
                guard,
                finalCommand,
                guard ? 2 : 0,
                guard ? width - 1L : 0,
                hasExtra ? 1 : 0,
                0,
                ShpTsRleForensicRemainingClass.End,
                true,
                literalOverflow,
                guard);
        }

        private static ShpTsRleForensicCategory[] Categories()
        {
            return new[]
            {
                ShpTsRleForensicCategory.Building,
                ShpTsRleForensicCategory.Infantry,
                ShpTsRleForensicCategory.Animation,
                ShpTsRleForensicCategory.MapAddon
            };
        }

        private static ExternalContentSourceDescriptor Source()
        {
            return new ExternalContentSourceDescriptor(
                "synthetic-source",
                ContentSourceKind.Patched,
                Path.GetTempPath(),
                0,
                "test",
                true);
        }

        private static string Sha(char value)
        {
            return new string(value, 64);
        }
    }
}
