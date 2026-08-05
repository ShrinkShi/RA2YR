using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using NUnit.Framework;
using RA2YR.Core.Content;
using RA2YR.Core.Content.Mix;
using RA2YR.Core.Content.ShpTs.Audit;
using RA2YR.Core.Formats.Mix;
using RA2YR.Core.Formats.Mix.Writing;
using RA2YR.Core.Formats.ShpTs;
using RA2YR.Tests.EditMode.Formats.ShpTs;

namespace RA2YR.Tests.EditMode.Content.ShpTs.Audit
{
    public sealed class ShpTsProjectBaselineAuditServiceTests
    {
        private static readonly DateTime StartedUtc =
            new DateTime(2026, 8, 3, 1, 2, 3, DateTimeKind.Utc);
        private static readonly DateTime CompletedUtc =
            new DateTime(2026, 8, 3, 1, 2, 4, DateTimeKind.Utc);

        [Test]
        public void FixedProfileLocksProjectBaselineTargetsAndModelIdentities()
        {
            ShpTsProjectBaselineAuditProfile profile =
                ShpTsProjectBaselineAuditProfile.ProjectBaseline;

            Assert.That(profile.Samples.Select(value => value.SampleId), Is.EqualTo(new[]
            {
                "building-explicit-image",
                "infantry-explicit-image",
                "map-addon-catalog-survey",
                "mouse-cursor-catalog-survey",
                "techno-animation-catalog-survey",
                "ui-cameo-configuration"
            }));
            Assert.That(profile.Samples.Select(value => value.ExpectedLength), Is.EqualTo(new long[]
            {
                50184, 114032, 16016, 359800, 298016, 2912
            }));
            Assert.That(profile.Samples.All(value =>
                Sha256Utilities.IsLowerSha256(value.ExpectedDirectoryModelSha256)), Is.True);
            Assert.That(profile.Samples.All(value =>
                Sha256Utilities.IsLowerSha256(value.ExpectedDecodedModelSha256)), Is.True);
        }

        [Test]
        public void CompleteRawAuditPublishesSanitizedSummaryAndExternalFrames()
        {
            using (AuditFixture fixture = AuditFixture.Create(FixtureKind.Raw))
            {
                ShpTsProjectBaselineAuditDelivery delivery =
                    ShpTsProjectBaselineAuditService.RunForTesting(
                        fixture.Configuration,
                        fixture.Profile(),
                        value => new ContentIndexer().Build(value),
                        FixedClock());

                Assert.That(delivery.Status, Is.EqualTo(
                    ShpTsProjectBaselineAuditStatus.Complete));
                Assert.That(delivery.SampleCount, Is.EqualTo(1));
                Assert.That(delivery.UnresolvedFrameCount, Is.Zero);
                Assert.That(delivery.FailedFrameCount, Is.Zero);
                Assert.That(delivery.ExternalManifestCacheRelativePath,
                    Does.StartWith("m2-shp1/shp-ts-audits/YR1001_ProjectBaseline/"));

                string summary = delivery.SanitizedSummaryJson;
                Assert.That(summary, Does.Contain(
                    "\"manifestType\":\"RA2YR.ShpTsProjectBaselineAuditSanitized\""));
                Assert.That(summary, Does.Contain("\"selectionBasis\":\"VerifiedCatalogSurvey\""));
                Assert.That(summary, Does.Contain("\"memoryStreamMixWindowEquivalent\":true"));
                Assert.That(summary, Does.Not.Contain("\"frames\""));
                Assert.That(summary, Does.Not.Contain(fixture.LogicalName));
                Assert.That(summary, Does.Not.Contain(fixture.RepositoryPath));
                Assert.That(summary, Does.Not.Contain(fixture.SourcePath));
                Assert.That(summary, Does.Not.Contain(fixture.CachePath));

                string externalPath = fixture.ExternalPath(delivery);
                Assert.That(File.Exists(externalPath), Is.True);
                Assert.That(new FileInfo(externalPath).Length,
                    Is.EqualTo(delivery.ExternalManifestLength));
                Assert.That(Sha256(File.ReadAllBytes(externalPath)),
                    Is.EqualTo(delivery.ExternalManifestSha256));
                string external = File.ReadAllText(externalPath);
                Assert.That(external, Does.Contain(
                    "\"manifestType\":\"RA2YR.ShpTsProjectBaselineAuditExternal\""));
                Assert.That(external, Does.Contain("\"frames\":["));
                Assert.That(external, Does.Contain("\"decodeStatus\":\"decoded\""));
                Assert.That(external, Does.Not.Contain("indices"));
                Assert.That(external, Does.Not.Contain(fixture.RepositoryPath));
                Assert.That(external, Does.Not.Contain(fixture.SourcePath));
                Assert.That(external, Does.Not.Contain(fixture.CachePath));
            }
        }

        [Test]
        public void StrictRleFailureStillPublishesControlledSanitizedEvidence()
        {
            using (AuditFixture fixture = AuditFixture.Create(FixtureKind.RleOverflow))
            {
                ShpTsProjectBaselineAuditDelivery delivery =
                    ShpTsProjectBaselineAuditService.RunForTesting(
                        fixture.Configuration,
                        fixture.Profile(),
                        value => new ContentIndexer().Build(value),
                        FixedClock());

                Assert.That(delivery.Status, Is.EqualTo(
                    ShpTsProjectBaselineAuditStatus.CompleteWithDecodeFailures));
                Assert.That(delivery.FailedFrameCount, Is.EqualTo(1));
                Assert.That(delivery.UnresolvedFrameCount, Is.Zero);
                Assert.That(delivery.SanitizedSummaryJson,
                    Does.Contain("\"RleOutputOverflow\":1"));

                string external = File.ReadAllText(fixture.ExternalPath(delivery));
                Assert.That(external, Does.Contain("\"decodeStatus\":\"failed\""));
                Assert.That(external, Does.Contain("\"code\":\"RleOutputOverflow\""));
                Assert.That(external, Does.Contain("\"offsetRelativeToEntry\":"));
                Assert.That(external, Does.Not.Contain("indices"));
            }
        }

        [Test]
        public void ChangedPayloadHashFailsBeforePublishingCache()
        {
            using (AuditFixture fixture = AuditFixture.Create(FixtureKind.Raw))
            {
                ShpTsProjectBaselineAuditException exception =
                    Assert.Throws<ShpTsProjectBaselineAuditException>(() =>
                        ShpTsProjectBaselineAuditService.RunForTesting(
                            fixture.Configuration,
                            fixture.Profile(expectedSha256: new string('0', 64)),
                            value => new ContentIndexer().Build(value),
                            FixedClock()));

                Assert.That(exception.Code, Is.EqualTo(
                    ShpTsProjectBaselineAuditFailureCode.TargetHashMismatch));
                Assert.That(Directory.Exists(fixture.CachePath), Is.False);
            }
        }

        [Test]
        public void ChangedDirectoryModelFailsBeforePublishingCache()
        {
            using (AuditFixture fixture = AuditFixture.Create(FixtureKind.Raw))
            {
                ShpTsProjectBaselineAuditException exception =
                    Assert.Throws<ShpTsProjectBaselineAuditException>(() =>
                        ShpTsProjectBaselineAuditService.RunForTesting(
                            fixture.Configuration,
                            fixture.Profile(expectedDirectorySha256: new string('0', 64)),
                            value => new ContentIndexer().Build(value),
                            FixedClock()));

                Assert.That(exception.Code, Is.EqualTo(
                    ShpTsProjectBaselineAuditFailureCode.DirectoryModelHashMismatch));
                Assert.That(Directory.Exists(fixture.CachePath), Is.False);
            }
        }

        [Test]
        public void ChangedDecodedModelFailsBeforePublishingCache()
        {
            using (AuditFixture fixture = AuditFixture.Create(FixtureKind.Raw))
            {
                ShpTsProjectBaselineAuditException exception =
                    Assert.Throws<ShpTsProjectBaselineAuditException>(() =>
                        ShpTsProjectBaselineAuditService.RunForTesting(
                            fixture.Configuration,
                            fixture.Profile(expectedDecodedSha256: new string('0', 64)),
                            value => new ContentIndexer().Build(value),
                            FixedClock()));

                Assert.That(exception.Code, Is.EqualTo(
                    ShpTsProjectBaselineAuditFailureCode.DecodedModelHashMismatch));
                Assert.That(Directory.Exists(fixture.CachePath), Is.False);
            }
        }

        [Test]
        public void TruncatedShpWithMatchingIdentityFailsStrictParsing()
        {
            using (AuditFixture fixture = AuditFixture.Create(FixtureKind.Truncated))
            {
                ShpTsProjectBaselineAuditException exception =
                    Assert.Throws<ShpTsProjectBaselineAuditException>(() =>
                        ShpTsProjectBaselineAuditService.RunForTesting(
                            fixture.Configuration,
                            fixture.Profile(),
                            value => new ContentIndexer().Build(value),
                            FixedClock()));

                Assert.That(exception.Code, Is.EqualTo(
                    ShpTsProjectBaselineAuditFailureCode.ShpParseFailed));
                Assert.That(Directory.Exists(fixture.CachePath), Is.False);
            }
        }

        [Test]
        public void LooseCandidateIsRejectedBeforeMounting()
        {
            using (AuditFixture fixture = AuditFixture.Create(FixtureKind.Raw))
            {
                fixture.WriteLooseCandidate();
                ShpTsProjectBaselineAuditException exception =
                    Assert.Throws<ShpTsProjectBaselineAuditException>(() =>
                        ShpTsProjectBaselineAuditService.RunForTesting(
                            fixture.Configuration,
                            fixture.Profile(),
                            value => new ContentIndexer().Build(value),
                            FixedClock()));

                Assert.That(exception.Code, Is.EqualTo(
                    ShpTsProjectBaselineAuditFailureCode.LooseCandidateFound));
                Assert.That(Directory.Exists(fixture.CachePath), Is.False);
            }
        }

        [Test]
        public void ProvenanceMismatchDoesNotAcceptAChangedArchiveChain()
        {
            using (AuditFixture fixture = AuditFixture.Create(FixtureKind.Raw))
            {
                ShpTsProjectBaselineAuditException exception =
                    Assert.Throws<ShpTsProjectBaselineAuditException>(() =>
                        ShpTsProjectBaselineAuditService.RunForTesting(
                            fixture.Configuration,
                            fixture.Profile(expectedChain: new[]
                            {
                                "root.mix", "root.mix/nested.mix"
                            }),
                            value => new ContentIndexer().Build(value),
                            FixedClock()));

                Assert.That(exception.Code, Is.EqualTo(
                    ShpTsProjectBaselineAuditFailureCode.TargetProvenanceMismatch));
            }
        }

        [Test]
        public void BaselineFingerprintChangeFailsBeforeManifestPublication()
        {
            using (AuditFixture fixture = AuditFixture.Create(FixtureKind.Raw))
            {
                int calls = 0;
                ShpTsProjectBaselineAuditException exception =
                    Assert.Throws<ShpTsProjectBaselineAuditException>(() =>
                        ShpTsProjectBaselineAuditService.RunForTesting(
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
                    ShpTsProjectBaselineAuditFailureCode.BaselineChangedDuringAudit));
                Assert.That(Directory.Exists(fixture.CachePath), Is.False);
            }
        }

        [Test]
        public void ManifestBudgetFailureDoesNotPublishPartialArtifact()
        {
            using (AuditFixture fixture = AuditFixture.Create(FixtureKind.Raw))
            {
                ShpTsProjectBaselineAuditException exception =
                    Assert.Throws<ShpTsProjectBaselineAuditException>(() =>
                        ShpTsProjectBaselineAuditService.RunForTesting(
                            fixture.Configuration,
                            fixture.Profile(maxManifestBytes: 1),
                            value => new ContentIndexer().Build(value),
                            FixedClock()));

                Assert.That(exception.Code, Is.EqualTo(
                    ShpTsProjectBaselineAuditFailureCode.ManifestBudgetExceeded));
                Assert.That(Directory.Exists(fixture.CachePath), Is.False);
            }
        }

        [Test]
        public void AuditRequiresExactlyOnePatchedProjectBaseline()
        {
            using (AuditFixture fixture = AuditFixture.Create(FixtureKind.Raw))
            {
                ExternalContentConfiguration invalid = fixture.ConfigurationWith(
                    new ExternalContentSourceDescriptor(
                        "another-source",
                        ContentSourceKind.Patched,
                        fixture.SourcePath,
                        300,
                        "synthetic",
                        true));
                ShpTsProjectBaselineAuditException exception =
                    Assert.Throws<ShpTsProjectBaselineAuditException>(() =>
                        ShpTsProjectBaselineAuditService.RunForTesting(
                            invalid,
                            fixture.Profile(),
                            value => new ContentIndexer().Build(value),
                            FixedClock()));

                Assert.That(exception.Code, Is.EqualTo(
                    ShpTsProjectBaselineAuditFailureCode.InvalidBaselineConfiguration));
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

        private enum FixtureKind
        {
            Raw,
            RleOverflow,
            Truncated
        }

        private sealed class AuditFixture : IDisposable
        {
            private readonly TemporaryContentTestDirectory temporary;
            private readonly byte[] payload;

            private AuditFixture(TemporaryContentTestDirectory temporary, FixtureKind kind)
            {
                this.temporary = temporary;
                RepositoryPath = temporary.CreateDirectory("repository");
                SourcePath = temporary.CreateDirectory("source");
                CachePath = temporary.GetPath("cache");
                LogicalName = "sample.shp";
                payload = BuildPayload(kind);
                Configuration = ConfigurationWith(CreateBaselineSource());
                temporary.WriteBytes(
                    "source/root.mix",
                    BuildMix(Named(LogicalName, payload)));
            }

            public ExternalContentConfiguration Configuration { get; }
            public string RepositoryPath { get; }
            public string SourcePath { get; }
            public string CachePath { get; }
            public string LogicalName { get; }

            public static AuditFixture Create(FixtureKind kind)
            {
                return new AuditFixture(new TemporaryContentTestDirectory(), kind);
            }

            public ShpTsProjectBaselineAuditProfile Profile(
                string expectedSha256 = null,
                string expectedDirectorySha256 = null,
                string expectedDecodedSha256 = null,
                string[] expectedChain = null,
                long maxManifestBytes = 1024 * 1024)
            {
                return new ShpTsProjectBaselineAuditProfile(
                    new[]
                    {
                        new ShpTsGoldenSampleSpecification(
                            "synthetic-sample",
                            "synthetic-role",
                            ShpTsSelectionBasis.VerifiedCatalogSurvey,
                            LogicalName,
                            "root.mix",
                            expectedChain ?? new[] { "root.mix" },
                            payload.LongLength,
                            expectedSha256 ?? Sha256(payload),
                            expectedDirectorySha256,
                            expectedDecodedSha256)
                    },
                    maxManifestBytes,
                    MixMountLimits.Default,
                    ShpTsReadLimits.Default);
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

            public string ExternalPath(ShpTsProjectBaselineAuditDelivery delivery)
            {
                return Path.Combine(
                    CachePath,
                    delivery.ExternalManifestCacheRelativePath.Replace(
                        '/',
                        Path.DirectorySeparatorChar));
            }

            public void WriteLooseCandidate()
            {
                temporary.WriteBytes("source/" + LogicalName, payload);
            }

            public void WriteAdditionalFile()
            {
                temporary.WriteBytes("source/appeared.bin", new byte[] { 1 });
            }

            public void Dispose()
            {
                temporary.Dispose();
            }

            private ExternalContentSourceDescriptor CreateBaselineSource()
            {
                return new ExternalContentSourceDescriptor(
                    ShpTsProjectBaselineAuditService.BaselineLogicalName,
                    ContentSourceKind.Patched,
                    SourcePath,
                    300,
                    "synthetic-project-baseline",
                    true);
            }

            private static byte[] BuildPayload(FixtureKind kind)
            {
                switch (kind)
                {
                    case FixtureKind.Raw:
                        return ShpTsSyntheticFixtureFactory.Build(
                            2,
                            1,
                            ShpTsSyntheticFixtureFactory.Raw(2, 1, 1, 7, 9));
                    case FixtureKind.RleOverflow:
                        return ShpTsSyntheticFixtureFactory.Build(
                            2,
                            1,
                            ShpTsSyntheticFixtureFactory.Rle(2, 1, new byte[] { 0, 3 }));
                    case FixtureKind.Truncated:
                        return new byte[] { 0, 0, 1, 0, 1, 0, 1 };
                    default:
                        throw new ArgumentOutOfRangeException(nameof(kind));
                }
            }

            private static MixWriteEntry Named(string name, byte[] bytes)
            {
                return new MixWriteEntry(MixFileId.ComputeCandidateId(name), bytes);
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
    }
}
