using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using NUnit.Framework;
using RA2YR.Core.Binary;
using RA2YR.Core.Content;
using RA2YR.Core.Content.Csf.Audit;
using RA2YR.Core.Content.Mix;
using RA2YR.Core.Formats.Csf;
using RA2YR.Core.Formats.Mix;
using RA2YR.Core.Formats.Mix.Writing;
using RA2YR.Tests.EditMode.Content;

namespace RA2YR.Tests.EditMode.Content.Csf.Audit
{
    public sealed class CsfProjectBaselineAuditServiceTests
    {
        private static readonly DateTime StartedUtc =
            new DateTime(2026, 8, 3, 1, 0, 0, DateTimeKind.Utc);
        private static readonly DateTime CompletedUtc =
            new DateTime(2026, 8, 3, 1, 0, 1, DateTimeKind.Utc);

        [Test]
        public void FixedChainPublishesExternalRecordsAndSanitizedSummary()
        {
            using (AuditFixture fixture = AuditFixture.Create(FixtureLayout.Fixed))
            {
                CsfProjectBaselineAuditDelivery delivery =
                    CsfProjectBaselineAuditService.RunForTesting(
                        fixture.Configuration,
                        fixture.Profile(),
                        value => new ContentIndexer().Build(value),
                        FixedClock());

                Assert.That(delivery.Status,
                    Is.EqualTo(CsfProjectBaselineAuditStatus.Complete));
                Assert.That(delivery.DocumentCount, Is.EqualTo(1));
                Assert.That(delivery.ExternalManifestCacheRelativePath,
                    Does.StartWith("wp02e/csf-audits/YR1001_ProjectBaseline/"));
                Assert.That(delivery.ExternalManifestSha256, Has.Length.EqualTo(64));

                string summary = delivery.SanitizedSummaryJson;
                Assert.That(summary, Does.Contain(
                    "\"manifestType\":\"RA2YR.CsfProjectBaselineAuditSanitized\""));
                Assert.That(summary, Does.Contain("\"rootArchive\":\"langmd.mix\""));
                Assert.That(summary, Does.Contain("\"entryId\":\"0xBD835079\""));
                Assert.That(summary, Does.Contain("\"labelRecordCount\":2"));
                Assert.That(summary, Does.Contain("\"totalValueCount\":3"));
                Assert.That(summary, Does.Contain("\"normalValueCount\":2"));
                Assert.That(summary, Does.Contain("\"extendedValueCount\":1"));
                Assert.That(summary, Does.Contain("\"emptyValueCount\":1"));
                Assert.That(summary, Does.Contain("\"duplicateLabelCount\":1"));
                Assert.That(summary, Does.Not.Contain("SecretLabel"));
                Assert.That(summary, Does.Not.Contain("PrivateValue"));
                Assert.That(summary, Does.Not.Contain("\"records\":["));
                Assert.That(summary, Does.Not.Contain(fixture.RepositoryPath));
                Assert.That(summary, Does.Not.Contain(fixture.SourcePath));
                Assert.That(summary, Does.Not.Contain(fixture.CachePath));

                string externalPath = Path.Combine(
                    fixture.CachePath,
                    delivery.ExternalManifestCacheRelativePath.Replace(
                        '/',
                        Path.DirectorySeparatorChar));
                Assert.That(File.Exists(externalPath), Is.True);
                Assert.That(new FileInfo(externalPath).Length,
                    Is.EqualTo(delivery.ExternalManifestLength));
                Assert.That(Sha256(File.ReadAllBytes(externalPath)),
                    Is.EqualTo(delivery.ExternalManifestSha256));
                string external = File.ReadAllText(externalPath);
                Assert.That(external, Does.Contain(
                    "\"manifestType\":\"RA2YR.CsfProjectBaselineAuditExternal\""));
                Assert.That(external, Does.Contain("\"records\":["));
                Assert.That(external, Does.Contain("SecretLabel"));
                Assert.That(external, Does.Contain("PrivateValue"));
                Assert.That(external, Does.Not.Contain(fixture.RepositoryPath));
                Assert.That(external, Does.Not.Contain(fixture.SourcePath));
                Assert.That(external, Does.Not.Contain(fixture.CachePath));
            }
        }

        [Test]
        public void ChangedPayloadHashFailsBeforePublishingCache()
        {
            using (AuditFixture fixture = AuditFixture.Create(FixtureLayout.Fixed))
            {
                CsfProjectBaselineAuditException exception =
                    Assert.Throws<CsfProjectBaselineAuditException>(() =>
                        CsfProjectBaselineAuditService.RunForTesting(
                            fixture.Configuration,
                            fixture.Profile(payloadHashMismatch: true),
                            value => new ContentIndexer().Build(value),
                            FixedClock()));

                Assert.That(exception.Code,
                    Is.EqualTo(CsfProjectBaselineAuditFailureCode.TargetHashMismatch));
                Assert.That(Directory.Exists(fixture.CachePath), Is.False);
            }
        }

        [Test]
        public void ChangedCanonicalModelFailsBeforePublishingCache()
        {
            using (AuditFixture fixture = AuditFixture.Create(FixtureLayout.Fixed))
            {
                CsfProjectBaselineAuditException exception =
                    Assert.Throws<CsfProjectBaselineAuditException>(() =>
                        CsfProjectBaselineAuditService.RunForTesting(
                            fixture.Configuration,
                            fixture.Profile(modelHashMismatch: true),
                            value => new ContentIndexer().Build(value),
                            FixedClock()));

                Assert.That(exception.Code, Is.EqualTo(
                    CsfProjectBaselineAuditFailureCode.NormalizedModelHashMismatch));
                Assert.That(Directory.Exists(fixture.CachePath), Is.False);
            }
        }

        [Test]
        public void InvalidCsfWithMatchingPayloadHashFailsStrictParsing()
        {
            using (AuditFixture fixture = AuditFixture.Create(FixtureLayout.InvalidSignature))
            {
                CsfProjectBaselineAuditException exception =
                    Assert.Throws<CsfProjectBaselineAuditException>(() =>
                        CsfProjectBaselineAuditService.RunForTesting(
                            fixture.Configuration,
                            fixture.Profile(),
                            value => new ContentIndexer().Build(value),
                            FixedClock()));

                Assert.That(exception.Code,
                    Is.EqualTo(CsfProjectBaselineAuditFailureCode.CsfParseFailed));
                Assert.That(Directory.Exists(fixture.CachePath), Is.False);
            }
        }

        [Test]
        public void MissingTargetFailsClosed()
        {
            using (AuditFixture fixture = AuditFixture.Create(FixtureLayout.MissingTarget))
            {
                CsfProjectBaselineAuditException exception =
                    Assert.Throws<CsfProjectBaselineAuditException>(() =>
                        CsfProjectBaselineAuditService.RunForTesting(
                            fixture.Configuration,
                            fixture.Profile(),
                            value => new ContentIndexer().Build(value),
                            FixedClock()));

                Assert.That(exception.Code,
                    Is.EqualTo(CsfProjectBaselineAuditFailureCode.TargetMissing));
            }
        }

        [Test]
        public void MissingRootArchiveFailsClosed()
        {
            using (AuditFixture fixture = AuditFixture.Create(FixtureLayout.MissingRoot))
            {
                CsfProjectBaselineAuditException exception =
                    Assert.Throws<CsfProjectBaselineAuditException>(() =>
                        CsfProjectBaselineAuditService.RunForTesting(
                            fixture.Configuration,
                            fixture.Profile(),
                            value => new ContentIndexer().Build(value),
                            FixedClock()));

                Assert.That(exception.Code,
                    Is.EqualTo(CsfProjectBaselineAuditFailureCode.RootArchiveMissing));
            }
        }

        [Test]
        public void LooseCsfCandidateIsRejectedBeforeMounting()
        {
            using (AuditFixture fixture = AuditFixture.Create(FixtureLayout.Fixed))
            {
                fixture.WriteLooseCsf();
                CsfProjectBaselineAuditException exception =
                    Assert.Throws<CsfProjectBaselineAuditException>(() =>
                        CsfProjectBaselineAuditService.RunForTesting(
                            fixture.Configuration,
                            fixture.Profile(),
                            value => new ContentIndexer().Build(value),
                            FixedClock()));

                Assert.That(exception.Code, Is.EqualTo(
                    CsfProjectBaselineAuditFailureCode.LooseCsfCandidateFound));
            }
        }

        [Test]
        public void BaselineFingerprintChangeFailsBeforeManifestPublication()
        {
            using (AuditFixture fixture = AuditFixture.Create(FixtureLayout.Fixed))
            {
                int calls = 0;
                CsfProjectBaselineAuditException exception =
                    Assert.Throws<CsfProjectBaselineAuditException>(() =>
                        CsfProjectBaselineAuditService.RunForTesting(
                            fixture.Configuration,
                            fixture.Profile(),
                            value =>
                            {
                                ContentIndexResult result = new ContentIndexer().Build(value);
                                if (calls++ == 0)
                                {
                                    fixture.WriteAdditionalFile();
                                }

                                return result;
                            },
                            FixedClock()));

                Assert.That(exception.Code, Is.EqualTo(
                    CsfProjectBaselineAuditFailureCode.BaselineChangedDuringAudit));
                Assert.That(Directory.Exists(fixture.CachePath), Is.False);
            }
        }

        [Test]
        public void ManifestBudgetFailureDoesNotPublishPartialArtifact()
        {
            using (AuditFixture fixture = AuditFixture.Create(FixtureLayout.Fixed))
            {
                CsfProjectBaselineAuditException exception =
                    Assert.Throws<CsfProjectBaselineAuditException>(() =>
                        CsfProjectBaselineAuditService.RunForTesting(
                            fixture.Configuration,
                            fixture.Profile(maxManifestBytes: 1),
                            value => new ContentIndexer().Build(value),
                            FixedClock()));

                Assert.That(exception.Code,
                    Is.EqualTo(CsfProjectBaselineAuditFailureCode.ManifestBudgetExceeded));
                Assert.That(Directory.Exists(fixture.CachePath), Is.False);
            }
        }

        [Test]
        public void AuditRequiresExactlyOnePatchedProjectBaseline()
        {
            using (AuditFixture fixture = AuditFixture.Create(FixtureLayout.Fixed))
            {
                ExternalContentConfiguration wrong = fixture.ConfigurationWith(
                    new ExternalContentSourceDescriptor(
                        "another-source",
                        ContentSourceKind.Patched,
                        fixture.SourcePath,
                        300,
                        "synthetic",
                        true));
                CsfProjectBaselineAuditException exception =
                    Assert.Throws<CsfProjectBaselineAuditException>(() =>
                        CsfProjectBaselineAuditService.RunForTesting(
                            wrong,
                            fixture.Profile(),
                            value => new ContentIndexer().Build(value),
                            FixedClock()));

                Assert.That(exception.Code, Is.EqualTo(
                    CsfProjectBaselineAuditFailureCode.InvalidBaselineConfiguration));
            }
        }

        private static Func<DateTime> FixedClock()
        {
            int calls = 0;
            return () => calls++ == 0 ? StartedUtc : CompletedUtc;
        }

        private static string Sha256(byte[] value)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                return BitConverter.ToString(sha256.ComputeHash(value))
                    .Replace("-", string.Empty)
                    .ToLowerInvariant();
            }
        }

        private enum FixtureLayout
        {
            Fixed,
            InvalidSignature,
            MissingTarget,
            MissingRoot
        }

        private sealed class AuditFixture : IDisposable
        {
            private readonly TemporaryContentTestDirectory temporary;
            private readonly byte[] csf;

            private AuditFixture(TemporaryContentTestDirectory temporary)
            {
                this.temporary = temporary;
                RepositoryPath = temporary.CreateDirectory("repository");
                SourcePath = temporary.CreateDirectory("source");
                CachePath = temporary.GetPath("cache");
                csf = BuildCsf();
                Configuration = ConfigurationWith(CreateBaselineSource());
            }

            public ExternalContentConfiguration Configuration { get; }
            public string RepositoryPath { get; }
            public string SourcePath { get; }
            public string CachePath { get; }

            public static AuditFixture Create(FixtureLayout layout)
            {
                var fixture = new AuditFixture(new TemporaryContentTestDirectory());
                fixture.Populate(layout);
                return fixture;
            }

            public CsfProjectBaselineAuditProfile Profile(
                bool payloadHashMismatch = false,
                bool modelHashMismatch = false,
                long maxManifestBytes = 1024 * 1024)
            {
                string modelHash = TryParseCanonicalModelHash(csf) ?? new string('1', 64);
                return new CsfProjectBaselineAuditProfile(
                    new CsfGoldenSampleSpecification(
                        "ra2md.csf",
                        MixFileId.ComputeCandidateId("ra2md.csf").Value,
                        csf.Length,
                        payloadHashMismatch ? new string('0', 64) : Sha256(csf),
                        modelHashMismatch ? new string('0', 64) : modelHash),
                    maxManifestBytes,
                    MixMountLimits.Default,
                    CsfReadLimits.Default);
            }

            public ExternalContentConfiguration ConfigurationWith(
                params ExternalContentSourceDescriptor[] sources)
            {
                return new ExternalContentConfiguration(
                    ExternalContentConfigurationLoader.SupportedSchemaVersion,
                    temporary.GetPath("config/ExternalContent.local.xml"),
                    RepositoryPath,
                    CachePath,
                    sources);
            }

            public void WriteAdditionalFile()
            {
                temporary.WriteBytes("source/appeared.bin", new byte[] { 1 });
            }

            public void WriteLooseCsf()
            {
                temporary.WriteBytes("source/ra2md.csf", csf);
            }

            public void Dispose()
            {
                temporary.Dispose();
            }

            private ExternalContentSourceDescriptor CreateBaselineSource()
            {
                return new ExternalContentSourceDescriptor(
                    CsfProjectBaselineAuditService.BaselineLogicalName,
                    ContentSourceKind.Patched,
                    SourcePath,
                    300,
                    "synthetic-project-baseline",
                    true);
            }

            private void Populate(FixtureLayout layout)
            {
                if (layout == FixtureLayout.MissingRoot)
                {
                    return;
                }

                byte[] payload = (byte[])csf.Clone();
                if (layout == FixtureLayout.InvalidSignature)
                {
                    payload[0] ^= 0xff;
                    Buffer.BlockCopy(payload, 0, csf, 0, payload.Length);
                }

                MixWriteEntry[] entries = layout == FixtureLayout.MissingTarget
                    ? Array.Empty<MixWriteEntry>()
                    : new[]
                    {
                        new MixWriteEntry(
                            MixFileId.ComputeCandidateId("ra2md.csf"),
                            payload)
                    };
                temporary.WriteBytes("source/langmd.mix", BuildMix(entries));
            }

            private static string TryParseCanonicalModelHash(byte[] bytes)
            {
                CsfParseResult result = WestwoodCsfReader.Read(
                    bytes,
                    new BinarySourceContext(
                        "format.csf",
                        "synthetic-source",
                        LogicalContentPath.Parse("ra2md.csf")),
                    new CsfSourceProvenance(
                        "synthetic-source",
                        new[] { LogicalContentPath.Parse("ra2md.csf") }),
                    CsfReadLimits.Default);
                return result.IsSuccess ? result.Document.CanonicalModelSha256 : null;
            }

            private static byte[] BuildCsf()
            {
                using (var stream = new MemoryStream())
                using (var writer = new BinaryWriter(stream, Encoding.UTF8, true))
                {
                    writer.Write(0x43534620u);
                    writer.Write(3u);
                    writer.Write(2u);
                    writer.Write(3u);
                    writer.Write(0u);
                    writer.Write(9u);
                    WriteLabel(writer, "SecretLabel", new[]
                    {
                        SyntheticValue.Normal(string.Empty),
                        SyntheticValue.WithExtra("PrivateValue", "EXTRA")
                    });
                    WriteLabel(writer, "SecretLabel", new[]
                    {
                        SyntheticValue.Normal("SecondValue")
                    });
                    writer.Flush();
                    return stream.ToArray();
                }
            }

            private static void WriteLabel(
                BinaryWriter writer,
                string name,
                IReadOnlyList<SyntheticValue> values)
            {
                writer.Write(0x4c424c20u);
                writer.Write(checked((uint)values.Count));
                writer.Write(checked((uint)name.Length));
                writer.Write(Encoding.ASCII.GetBytes(name));
                foreach (SyntheticValue value in values)
                {
                    writer.Write(value.IsExtended ? 0x53545257u : 0x53545220u);
                    writer.Write(checked((uint)value.Main.Length));
                    foreach (char codeUnit in value.Main)
                    {
                        writer.Write(checked((ushort)(codeUnit ^ 0xffff)));
                    }

                    if (value.IsExtended)
                    {
                        writer.Write(checked((uint)value.Extra.Length));
                        writer.Write(Encoding.ASCII.GetBytes(value.Extra));
                    }
                }
            }

            private static byte[] BuildMix(params MixWriteEntry[] entries)
            {
                var options = new MixWriteOptions(
                    MixWriteOrder.DeterministicRebuild,
                    MixWriteHeaderKind.Classic,
                    false,
                    null,
                    32,
                    1024 * 1024);
                MixWriteResult result = MixArchiveWriter.Build(entries, options);
                if (!result.IsSuccess)
                {
                    throw new InvalidOperationException("Synthetic MIX construction failed.");
                }

                return result.GetArchiveBytes();
            }
        }

        private sealed class SyntheticValue
        {
            private SyntheticValue(string main, string extra, bool extended)
            {
                Main = main;
                Extra = extra;
                IsExtended = extended;
            }

            public string Main { get; }
            public string Extra { get; }
            public bool IsExtended { get; }

            public static SyntheticValue Normal(string main)
            {
                return new SyntheticValue(main, null, false);
            }

            public static SyntheticValue WithExtra(string main, string extra)
            {
                return new SyntheticValue(main, extra, true);
            }
        }
    }
}
