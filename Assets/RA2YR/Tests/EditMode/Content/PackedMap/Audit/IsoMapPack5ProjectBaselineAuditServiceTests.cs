using System;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using RA2YR.Core.Content;
using RA2YR.Core.Content.Mix;
using RA2YR.Core.Content.PackedMap.Audit;
using RA2YR.Core.Formats.Mix;
using RA2YR.Core.Formats.Mix.Writing;
using RA2YR.Tests.EditMode.Content;

namespace RA2YR.Tests.EditMode.Content.PackedMap.Audit
{
    public sealed class IsoMapPack5ProjectBaselineAuditServiceTests
    {
        [Test]
        public void SyntheticAuditPublishesOnlyAggregateSanitizedSummary()
        {
            using (var fixture = AuditFixture.Create())
            {
                IsoMapPack5ProjectBaselineAuditDelivery delivery =
                    IsoMapPack5ProjectBaselineAuditService.Run(fixture.Configuration);

                Assert.That(delivery.Status, Is.EqualTo(IsoMapPack5ProjectBaselineAuditStatus.Complete));
                Assert.That(delivery.CandidateSectionCount, Is.EqualTo(1));
                Assert.That(delivery.SuccessfulSectionCount, Is.EqualTo(1));
                Assert.That(delivery.FailedSectionCount, Is.Zero);
                Assert.That(delivery.DecodedRecordCount, Is.EqualTo(1));
                Assert.That(delivery.SourceFingerprintAfter, Is.EqualTo(delivery.SourceFingerprint));
                Assert.That(delivery.SanitizedSummaryJson, Does.Contain("RA2YR.IsoMapPack5ProjectBaselineAuditSanitized"));
                Assert.That(delivery.SanitizedSummaryJson, Does.Contain("\"decodedRecordCount\":1"));
                Assert.That(delivery.SanitizedSummaryJson, Does.Not.Contain(Convert.ToBase64String(fixture.RecordBytes)));
                Assert.That(delivery.SanitizedSummaryJson, Does.Not.Contain("\"records\":["));
                Assert.That(delivery.SanitizedSummaryJson, Does.Not.Contain(fixture.RepositoryPath));
                Assert.That(delivery.SanitizedSummaryJson, Does.Not.Contain(fixture.SourcePath));
                Assert.That(delivery.SanitizedSummaryJson, Does.Not.Contain(fixture.CachePath));
            }
        }

        [Test]
        public void SyntheticAuditAggregateHashIsDeterministic()
        {
            using (var fixture = AuditFixture.Create())
            {
                IsoMapPack5ProjectBaselineAuditDelivery first = IsoMapPack5ProjectBaselineAuditService.Run(fixture.Configuration);
                IsoMapPack5ProjectBaselineAuditDelivery second = IsoMapPack5ProjectBaselineAuditService.Run(fixture.Configuration);
                Assert.That(second.AggregateSha256, Is.EqualTo(first.AggregateSha256));
                Assert.That(second.SourceFingerprintAfter, Is.EqualTo(first.SourceFingerprintAfter));
            }
        }

        private sealed class AuditFixture : IDisposable
        {
            private readonly TemporaryContentTestDirectory temporary;
            public ExternalContentConfiguration Configuration { get; }
            public string RepositoryPath { get; }
            public string SourcePath { get; }
            public string CachePath { get; }
            public byte[] RecordBytes { get; }

            private AuditFixture(TemporaryContentTestDirectory temporary)
            {
                this.temporary = temporary;
                RepositoryPath = temporary.CreateDirectory("repository");
                SourcePath = temporary.CreateDirectory("source");
                CachePath = temporary.GetPath("cache");
                RecordBytes = Record(7, 9, 0x12345678u, 2, 3, 4);
                byte[] ini = Encoding.ASCII.GetBytes(
                    "[IsoMapPack5]\r\n1=" + Convert.ToBase64String(Envelope(InitialLiteral(RecordBytes), RecordBytes.Length)) + "\r\n");
                temporary.WriteBytes("source/root.mix", BuildMix(new MixWriteEntry(MixFileId.ComputeCandidateId("map.ini"), ini)));
                Configuration = new ExternalContentConfiguration(
                    ExternalContentConfigurationLoader.SupportedSchemaVersion,
                    temporary.GetPath("config/ExternalContent.local.xml"),
                    RepositoryPath,
                    CachePath,
                    new[] { new ExternalContentSourceDescriptor(
                        IsoMapPack5ProjectBaselineAuditService.BaselineLogicalName,
                        ContentSourceKind.Patched,
                        SourcePath,
                        300,
                        "synthetic-project-baseline",
                        true) });
            }

            public static AuditFixture Create() { return new AuditFixture(new TemporaryContentTestDirectory()); }
            public void Dispose() { temporary.Dispose(); }

            private static byte[] InitialLiteral(byte[] raw)
            {
                return Concat(new[] { checked((byte)(raw.Length + 17)) }, raw, new byte[] { 0x11, 0x00, 0x00 });
            }

            private static byte[] Envelope(byte[] compressed, int outputLength)
            {
                return Concat(new byte[] { (byte)compressed.Length, (byte)(compressed.Length >> 8), (byte)outputLength, (byte)(outputLength >> 8) }, compressed);
            }

            private static byte[] Record(ushort x, ushort y, uint tile, byte subTile, byte level, byte tail)
            {
                return new byte[] { (byte)x, (byte)(x >> 8), (byte)y, (byte)(y >> 8), (byte)tile, (byte)(tile >> 8), (byte)(tile >> 16), (byte)(tile >> 24), subTile, level, tail };
            }

            private static byte[] BuildMix(params MixWriteEntry[] entries)
            {
                MixWriteResult result = MixArchiveWriter.Build(entries, new MixWriteOptions(
                    MixWriteOrder.DeterministicRebuild, MixWriteHeaderKind.Classic, false, null, 32, 1024 * 1024));
                if (!result.IsSuccess) throw new InvalidOperationException("Synthetic MIX construction failed.");
                return result.GetArchiveBytes();
            }

            private static byte[] Concat(params byte[][] arrays)
            {
                int length = arrays.Sum(value => value.Length);
                var result = new byte[length];
                int offset = 0;
                foreach (byte[] array in arrays) { Buffer.BlockCopy(array, 0, result, offset, array.Length); offset += array.Length; }
                return result;
            }
        }
    }
}
