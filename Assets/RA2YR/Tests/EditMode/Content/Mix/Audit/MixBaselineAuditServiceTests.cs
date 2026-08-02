using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using NUnit.Framework;
using RA2YR.Core.Content;
using RA2YR.Core.Content.Mix;
using RA2YR.Core.Content.Mix.Audit;
using RA2YR.Core.Formats.Mix;
using RA2YR.Core.Formats.Mix.Writing;

namespace RA2YR.Tests.EditMode.Content.Mix.Audit
{
    [TestFixture]
    public sealed class MixBaselineAuditServiceTests
    {
        private static readonly DateTime StartedUtc =
            new DateTime(2026, 8, 2, 1, 2, 3, DateTimeKind.Utc);
        private static readonly DateTime CompletedUtc =
            new DateTime(2026, 8, 2, 1, 7, 9, DateTimeKind.Utc);

        [Test]
        public void ControlledAuditIndexesRootsTargetsAndNestedProvenanceWithoutPublishingBodies()
        {
            using (AuditFixture fixture = AuditFixture.CreatePopulated())
            {
                int clockCalls = 0;
                MixBaselineAuditDelivery delivery = MixBaselineAuditService.RunForTesting(
                    fixture.Configuration,
                    fixture.XccDatabasePath,
                    fixture.Profile(),
                    value => new ContentIndexer().Build(value),
                    () => clockCalls++ == 0 ? StartedUtc : CompletedUtc);

                Assert.That(clockCalls, Is.EqualTo(2));
                Assert.That(delivery.Status,
                    Is.EqualTo(MixBaselineAuditStatus.CompleteWithArchiveFailures));
                Assert.That(delivery.RootArchiveCount, Is.EqualTo(3));
                Assert.That(delivery.ParsedRootArchiveCount, Is.EqualTo(2));
                Assert.That(delivery.FailedRootArchiveCount, Is.EqualTo(1));
                Assert.That(delivery.ExternalManifestCacheRelativePath,
                    Does.StartWith("wp02c/mix-audits/YR1001_ProjectBaseline/"));

                string manifestPath = Path.Combine(
                    fixture.CachePath,
                    delivery.ExternalManifestCacheRelativePath.Replace(
                        '/',
                        Path.DirectorySeparatorChar));
                Assert.That(File.Exists(manifestPath), Is.True);
                byte[] manifestBytes = File.ReadAllBytes(manifestPath);
                string manifest = Encoding.UTF8.GetString(manifestBytes);
                Assert.That(manifestBytes.LongLength, Is.EqualTo(delivery.ExternalManifestLength));
                Assert.That(Sha256(manifestBytes), Is.EqualTo(delivery.ExternalManifestSha256));
                Assert.That(Directory.GetFiles(
                    fixture.CachePath,
                    "*",
                    SearchOption.AllDirectories), Has.Length.EqualTo(1));
                Assert.That(Directory.GetFiles(
                    fixture.CachePath,
                    "*.tmp",
                    SearchOption.AllDirectories), Is.Empty);

                string summary = delivery.SanitizedSummaryJson;
                foreach (string target in AuditFixture.TargetNames)
                {
                    Assert.That(summary, Does.Contain("\"logicalName\":\"" + target + "\""));
                }

                Assert.That(summary, Does.Contain("\"unknownIdCount\":1"));
                Assert.That(summary, Does.Contain("\"nestedCount\":1"));
                Assert.That(summary, Does.Contain("\"encryptedDirectory\":1"));
                Assert.That(summary, Does.Contain("\"checksum\":1"));
                Assert.That(summary, Does.Contain("\"status\":\"ambiguous\""));
                Assert.That(summary, Does.Contain("\"encryptedChain\":true"));
                Assert.That(summary, Does.Contain("\"encryptedChain\":false"));
                Assert.That(manifest, Does.Contain("\"id\":\"0xDEADBEEF\""));
                Assert.That(manifest, Does.Contain("\"logicalName\":null"));

                string started = StartedUtc.ToString("O");
                string completed = CompletedUtc.ToString("O");
                Assert.That(summary, Does.Contain(started));
                Assert.That(summary, Does.Contain(completed));
                Assert.That(manifest, Does.Contain(started));
                Assert.That(manifest, Does.Contain(completed));

                foreach (string privateValue in new[]
                         {
                             fixture.RepositoryPath,
                             fixture.SourcePath,
                             fixture.CachePath,
                             fixture.ReferencePath,
                             AuditFixture.PrivatePayloadMarker
                         })
                {
                    Assert.That(summary, Does.Not.Contain(privateValue));
                    Assert.That(manifest, Does.Not.Contain(privateValue));
                }

                foreach (string propertyValue in typeof(MixBaselineAuditDelivery)
                             .GetProperties()
                             .Where(property => property.PropertyType == typeof(string))
                             .Select(property => (string)property.GetValue(delivery)))
                {
                    Assert.That(propertyValue, Does.Not.Contain(fixture.SourcePath));
                    Assert.That(propertyValue, Does.Not.Contain(AuditFixture.PrivatePayloadMarker));
                }
            }
        }

        [Test]
        public void BaselineFingerprintChangeFailsBeforePublishingManifest()
        {
            using (AuditFixture fixture = AuditFixture.CreatePopulated())
            {
                int calls = 0;
                MixBaselineAuditException exception = Assert.Throws<MixBaselineAuditException>(() =>
                    MixBaselineAuditService.RunForTesting(
                        fixture.Configuration,
                        fixture.XccDatabasePath,
                        fixture.Profile(),
                        value =>
                        {
                            ContentIndexResult result = new ContentIndexer().Build(value);
                            if (calls++ == 0)
                            {
                                File.WriteAllBytes(
                                    Path.Combine(fixture.SourcePath, "appeared.bin"),
                                    new byte[] { 1 });
                            }

                            return result;
                        },
                        FixedClock()));

                Assert.That(exception.Code,
                    Is.EqualTo(MixBaselineAuditFailureCode.BaselineChangedDuringAudit));
                Assert.That(Directory.Exists(fixture.CachePath), Is.False);
            }
        }

        [Test]
        public void XccDatabaseHashAndParseAreBothFailClosed()
        {
            using (AuditFixture fixture = AuditFixture.CreatePopulated())
            {
                MixBaselineAuditProfile wrongHash = fixture.Profile(
                    expectedHash: new string('0', 64));
                MixBaselineAuditException mismatch = Assert.Throws<MixBaselineAuditException>(() =>
                    MixBaselineAuditService.RunForTesting(
                        fixture.Configuration,
                        fixture.XccDatabasePath,
                        wrongHash,
                        value => new ContentIndexer().Build(value),
                        FixedClock()));
                Assert.That(mismatch.Code,
                    Is.EqualTo(MixBaselineAuditFailureCode.XccNameDatabaseHashMismatch));

                byte[] invalid = { 0, 0, 0, 0 };
                File.WriteAllBytes(fixture.XccDatabasePath, invalid);
                MixBaselineAuditException invalidParse = Assert.Throws<MixBaselineAuditException>(() =>
                    MixBaselineAuditService.RunForTesting(
                        fixture.Configuration,
                        fixture.XccDatabasePath,
                        fixture.Profile(expectedHash: Sha256(invalid)),
                        value => new ContentIndexer().Build(value),
                        FixedClock()));
                Assert.That(invalidParse.Code,
                    Is.EqualTo(MixBaselineAuditFailureCode.XccNameDatabaseInvalid));
            }
        }

        [Test]
        public void ManifestBudgetFailureDoesNotLeavePublishedArtifact()
        {
            using (AuditFixture fixture = AuditFixture.CreatePopulated())
            {
                MixBaselineAuditException exception = Assert.Throws<MixBaselineAuditException>(() =>
                    MixBaselineAuditService.RunForTesting(
                        fixture.Configuration,
                        fixture.XccDatabasePath,
                        fixture.Profile(maxManifestBytes: 1),
                        value => new ContentIndexer().Build(value),
                        FixedClock()));

                Assert.That(exception.Code,
                    Is.EqualTo(MixBaselineAuditFailureCode.ManifestBudgetExceeded));
                Assert.That(Directory.Exists(fixture.CachePath), Is.False);
            }
        }

        [Test]
        public void AuditRequiresExactlyOneEnabledPatchedProjectBaseline()
        {
            using (AuditFixture fixture = AuditFixture.CreatePopulated())
            {
                var wrongSource = new ExternalContentSourceDescriptor(
                    "DifferentBaseline",
                    ContentSourceKind.Patched,
                    fixture.SourcePath,
                    1,
                    "test",
                    true);
                ExternalContentConfiguration wrong = fixture.ConfigurationWith(wrongSource);

                MixBaselineAuditException exception = Assert.Throws<MixBaselineAuditException>(() =>
                    MixBaselineAuditService.RunForTesting(
                        wrong,
                        fixture.XccDatabasePath,
                        fixture.Profile(),
                        value => new ContentIndexer().Build(value),
                        FixedClock()));
                Assert.That(exception.Code,
                    Is.EqualTo(MixBaselineAuditFailureCode.InvalidBaselineConfiguration));
            }
        }

        [Test]
        public void CleanupAttemptsEveryRootAndPreservesStructuredPrimaryFailure()
        {
            var first = new TrackingDisposable(true);
            var second = new TrackingDisposable(false);
            var third = new TrackingDisposable(true);
            int failures = MixBaselineAuditCleanup.DisposeAll(
                new IDisposable[] { first, second, third });

            Assert.That(failures, Is.EqualTo(2));
            Assert.That(first.DisposeCalls, Is.EqualTo(1));
            Assert.That(second.DisposeCalls, Is.EqualTo(1));
            Assert.That(third.DisposeCalls, Is.EqualTo(1));

            var primary = new MixBaselineAuditException(
                MixBaselineAuditFailureCode.ManifestBudgetExceeded,
                "sanitized-primary");
            MixBaselineAuditException observed = Assert.Throws<MixBaselineAuditException>(() =>
                MixBaselineAuditCleanup.ThrowAfterCleanup(primary, failures));
            Assert.That(observed, Is.SameAs(primary));
            Assert.That(observed.Code,
                Is.EqualTo(MixBaselineAuditFailureCode.ManifestBudgetExceeded));
            Assert.That(observed.CleanupFailureCount, Is.EqualTo(2));

            MixBaselineAuditException cleanupOnly = Assert.Throws<MixBaselineAuditException>(() =>
                MixBaselineAuditCleanup.ThrowAfterCleanup(null, 2));
            Assert.That(cleanupOnly.Code,
                Is.EqualTo(MixBaselineAuditFailureCode.RootMountCleanupFailed));
            Assert.That(cleanupOnly.CleanupFailureCount, Is.EqualTo(2));
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

        private sealed class TrackingDisposable : IDisposable
        {
            private readonly bool fail;

            public TrackingDisposable(bool fail)
            {
                this.fail = fail;
            }

            public int DisposeCalls { get; private set; }

            public void Dispose()
            {
                DisposeCalls++;
                if (fail)
                {
                    throw new IOException("synthetic cleanup failure");
                }
            }
        }

        private sealed class AuditFixture : IDisposable
        {
            public const string PrivatePayloadMarker = "PRIVATE-SYNTHETIC-MIX-BODY";

            public static readonly string[] TargetNames =
            {
                "isotem.pal",
                "temperat.pal",
                "unittem.pal",
                "rulesmd.ini",
                "artmd.ini",
                "ai.ini",
                "ra2md.csf"
            };

            private readonly TemporaryContentTestDirectory temporary;
            private readonly byte[] databaseBytes;

            private AuditFixture(
                TemporaryContentTestDirectory temporary,
                byte[] databaseBytes)
            {
                this.temporary = temporary;
                this.databaseBytes = databaseBytes;
                RepositoryPath = temporary.CreateDirectory("repository");
                SourcePath = temporary.CreateDirectory("source");
                CachePath = temporary.GetPath("cache");
                ReferencePath = temporary.CreateDirectory("reference");
                XccDatabasePath = temporary.WriteBytes(
                    "reference/global mix database.dat",
                    databaseBytes);
                Configuration = ConfigurationWith(CreateBaselineSource());
            }

            public ExternalContentConfiguration Configuration { get; }

            public string RepositoryPath { get; }

            public string SourcePath { get; }

            public string CachePath { get; }

            public string ReferencePath { get; }

            public string XccDatabasePath { get; }

            public static AuditFixture CreatePopulated()
            {
                byte[] database = BuildXccDatabase("nested.mix");
                var fixture = new AuditFixture(
                    new TemporaryContentTestDirectory(),
                    database);
                fixture.PopulateSource();
                return fixture;
            }

            public MixBaselineAuditProfile Profile(
                string expectedHash = null,
                long maxManifestBytes = 4 * 1024 * 1024)
            {
                return new MixBaselineAuditProfile(
                    expectedHash ?? Sha256(File.ReadAllBytes(XccDatabasePath)),
                    1024 * 1024,
                    32,
                    maxManifestBytes,
                    MixMountLimits.Default);
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

            public void Dispose()
            {
                temporary.Dispose();
            }

            private ExternalContentSourceDescriptor CreateBaselineSource()
            {
                return new ExternalContentSourceDescriptor(
                    MixBaselineAuditService.BaselineLogicalName,
                    ContentSourceKind.Patched,
                    SourcePath,
                    300,
                    "synthetic-project-baseline",
                    true);
            }

            private void PopulateSource()
            {
                byte[] marker = Encoding.ASCII.GetBytes(PrivatePayloadMarker);
                byte[] nested = BuildMix(
                    ClassicOptions(),
                    Named("unittem.pal", new byte[] { 7, 8, 9 }));
                byte[] classic = BuildMix(
                    ClassicOptions(),
                    Named("isotem.pal", marker),
                    Named("temperat.pal", new byte[] { 1, 2 }),
                    Named("nested.mix", nested),
                    Raw(0xdeadbeefu, new byte[] { 3, 4, 5 }));
                byte[] encrypted = BuildMix(
                    EncryptedOptions(),
                    Named("isotem.pal", new byte[] { 9 }),
                    Named("rulesmd.ini", new byte[] { 10 }),
                    Named("artmd.ini", new byte[] { 11 }),
                    Named("ra2md.csf", new byte[] { 12 }));

                temporary.WriteBytes("source/a-classic.mix", classic);
                temporary.WriteBytes("source/b-encrypted.mix", encrypted);
                temporary.WriteBytes("source/z-corrupt.mix", new byte[] { 1, 2, 3 });
                temporary.WriteBytes("source/ai.ini", new byte[] { 13, 14 });
            }

            private static MixWriteEntry Named(string name, byte[] payload)
            {
                return new MixWriteEntry(MixFileId.ComputeCandidateId(name), payload);
            }

            private static MixWriteEntry Raw(uint id, byte[] payload)
            {
                return new MixWriteEntry(MixFileId.FromRaw(id), payload);
            }

            private static byte[] BuildMix(
                MixWriteOptions options,
                params MixWriteEntry[] entries)
            {
                MixWriteResult result = MixArchiveWriter.Build(entries, options);
                if (!result.IsSuccess)
                {
                    throw new InvalidOperationException("Synthetic MIX construction failed.");
                }

                return result.GetArchiveBytes();
            }

            private static MixWriteOptions ClassicOptions()
            {
                return new MixWriteOptions(
                    MixWriteOrder.DeterministicRebuild,
                    MixWriteHeaderKind.Classic,
                    false,
                    null,
                    128,
                    1024 * 1024);
            }

            private static MixWriteOptions EncryptedOptions()
            {
                var keySource = new byte[80];
                keySource[0] = 2;
                keySource[40] = 3;
                return new MixWriteOptions(
                    MixWriteOrder.DeterministicRebuild,
                    MixWriteHeaderKind.Extended,
                    true,
                    keySource,
                    128,
                    1024 * 1024);
            }

            private static byte[] BuildXccDatabase(params string[] names)
            {
                using (var stream = new MemoryStream())
                using (var writer = new BinaryWriter(stream, Encoding.ASCII, true))
                {
                    writer.Write(0);
                    writer.Write(0);
                    writer.Write(0);
                    writer.Write(names.Length);
                    foreach (string name in names)
                    {
                        writer.Write(Encoding.ASCII.GetBytes(name));
                        writer.Write((byte)0);
                        writer.Write((byte)0);
                    }

                    writer.Flush();
                    return stream.ToArray();
                }
            }
        }
    }
}
