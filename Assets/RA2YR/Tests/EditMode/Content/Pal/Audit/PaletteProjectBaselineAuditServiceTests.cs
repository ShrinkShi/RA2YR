using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using NUnit.Framework;
using RA2YR.Core.Content;
using RA2YR.Core.Content.Mix;
using RA2YR.Core.Content.Pal.Audit;
using RA2YR.Core.Formats.Mix;
using RA2YR.Core.Formats.Mix.Writing;
using RA2YR.Core.Formats.Pal;

namespace RA2YR.Tests.EditMode.Content.Pal.Audit
{
    public sealed class PaletteProjectBaselineAuditServiceTests
    {
        private static readonly DateTime StartedUtc =
            new DateTime(2026, 8, 3, 1, 2, 3, DateTimeKind.Utc);
        private static readonly DateTime CompletedUtc =
            new DateTime(2026, 8, 3, 1, 2, 4, DateTimeKind.Utc);

        [Test]
        public void FixedProfileLocksProjectBaselineTargetAndModelIdentities()
        {
            PaletteProjectBaselineAuditProfile profile =
                PaletteProjectBaselineAuditProfile.ProjectBaseline;

            Assert.That(profile.Samples.Select(sample => sample.LogicalName.Value), Is.EqualTo(
                new[] { "isotem.pal", "temperat.pal", "unittem.pal" }));
            Assert.That(profile.Samples.Select(sample => sample.ExpectedLength),
                Is.All.EqualTo(768));
            Assert.That(profile.Samples.Select(sample => sample.ExpectedNormalizedModelSha256),
                Is.EqualTo(new[]
                {
                    "f8650500f5d49f5fe8dd050eda345e1eb9eec82b42a8064770ed58c9c31c6524",
                    "8932af31cfa5a30098429efdc5ab61445af555b95cc827721426bf066ef1fc42",
                    "36d158b0a336d5f0ebb3749e66c79089191f4336dd970f02f7c5c24d35207717"
                }));
        }

        [Test]
        public void CompleteAuditUsesFixedNestedWindowsAndPublishesSanitizedDelivery()
        {
            using (AuditFixture fixture = AuditFixture.Create(FixtureLayout.FixedChain))
            {
                PaletteProjectBaselineAuditDelivery delivery =
                    PaletteProjectBaselineAuditService.RunForTesting(
                        fixture.Configuration,
                        fixture.Profile(),
                        value => new ContentIndexer().Build(value),
                        FixedClock());

                Assert.That(delivery.Status,
                    Is.EqualTo(PaletteProjectBaselineAuditStatus.Complete));
                Assert.That(delivery.PaletteCount, Is.EqualTo(3));
                Assert.That(delivery.ExternalManifestCacheRelativePath,
                    Does.StartWith("wp02d/palette-audits/YR1001_ProjectBaseline/"));
                Assert.That(delivery.ExternalManifestSha256, Has.Length.EqualTo(64));

                string summary = delivery.SanitizedSummaryJson;
                Assert.That(summary, Does.Contain(
                    "\"manifestType\":\"RA2YR.PaletteProjectBaselineAuditSanitized\""));
                Assert.That(summary, Does.Contain(
                    "\"displayConversionStrategy\":\"XccScaleToFullRangeFloor\""));
                Assert.That(summary, Does.Contain(
                    "\"rootArchive\":\"ra2.mix\""));
                Assert.That(summary, Does.Contain(
                    "\"archive\":\"ra2.mix/cache.mix\""));
                Assert.That(summary, Does.Contain(
                    "\"entryId\":\"0x3B5A96DE\""));
                Assert.That(summary, Does.Not.Contain("rawColors"));
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
                    "\"manifestType\":\"RA2YR.PaletteProjectBaselineAuditExternal\""));
                Assert.That(external, Does.Contain("\"rawColors\":["));
                Assert.That(external, Does.Not.Contain(fixture.RepositoryPath));
                Assert.That(external, Does.Not.Contain(fixture.SourcePath));
                Assert.That(external, Does.Not.Contain(fixture.CachePath));
            }
        }

        [Test]
        public void ChangedPayloadHashFailsBeforePublishingCache()
        {
            using (AuditFixture fixture = AuditFixture.Create(FixtureLayout.FixedChain))
            {
                PaletteProjectBaselineAuditException exception =
                    Assert.Throws<PaletteProjectBaselineAuditException>(() =>
                        PaletteProjectBaselineAuditService.RunForTesting(
                            fixture.Configuration,
                            fixture.Profile(payloadHashMismatch: "isotem.pal"),
                            value => new ContentIndexer().Build(value),
                            FixedClock()));

                Assert.That(exception.Code,
                    Is.EqualTo(PaletteProjectBaselineAuditFailureCode.TargetHashMismatch));
                Assert.That(Directory.Exists(fixture.CachePath), Is.False);
            }
        }

        [Test]
        public void ChangedCanonicalModelFailsBeforePublishingCache()
        {
            using (AuditFixture fixture = AuditFixture.Create(FixtureLayout.FixedChain))
            {
                PaletteProjectBaselineAuditException exception =
                    Assert.Throws<PaletteProjectBaselineAuditException>(() =>
                        PaletteProjectBaselineAuditService.RunForTesting(
                            fixture.Configuration,
                            fixture.Profile(modelHashMismatch: "temperat.pal"),
                            value => new ContentIndexer().Build(value),
                            FixedClock()));

                Assert.That(exception.Code, Is.EqualTo(
                    PaletteProjectBaselineAuditFailureCode.NormalizedModelHashMismatch));
                Assert.That(Directory.Exists(fixture.CachePath), Is.False);
            }
        }

        [Test]
        public void InvalidPaletteWithMatchingPayloadHashFailsStrictParsing()
        {
            using (AuditFixture fixture = AuditFixture.Create(FixtureLayout.InvalidChannel))
            {
                PaletteProjectBaselineAuditException exception =
                    Assert.Throws<PaletteProjectBaselineAuditException>(() =>
                        PaletteProjectBaselineAuditService.RunForTesting(
                            fixture.Configuration,
                            fixture.Profile(),
                            value => new ContentIndexer().Build(value),
                            FixedClock()));

                Assert.That(exception.Code,
                    Is.EqualTo(PaletteProjectBaselineAuditFailureCode.PaletteParseFailed));
                Assert.That(Directory.Exists(fixture.CachePath), Is.False);
            }
        }

        [Test]
        public void DirectRootEntriesFailTheFixedProvenanceChain()
        {
            using (AuditFixture fixture = AuditFixture.Create(FixtureLayout.DirectRootTargets))
            {
                PaletteProjectBaselineAuditException exception =
                    Assert.Throws<PaletteProjectBaselineAuditException>(() =>
                        PaletteProjectBaselineAuditService.RunForTesting(
                            fixture.Configuration,
                            fixture.Profile(),
                            value => new ContentIndexer().Build(value),
                            FixedClock()));

                Assert.That(exception.Code, Is.EqualTo(
                    PaletteProjectBaselineAuditFailureCode.TargetProvenanceMismatch));
            }
        }

        [Test]
        public void DuplicateIdAcrossRootAndCacheIsAmbiguous()
        {
            using (AuditFixture fixture = AuditFixture.Create(FixtureLayout.DuplicateTarget))
            {
                PaletteProjectBaselineAuditException exception =
                    Assert.Throws<PaletteProjectBaselineAuditException>(() =>
                        PaletteProjectBaselineAuditService.RunForTesting(
                            fixture.Configuration,
                            fixture.Profile(),
                            value => new ContentIndexer().Build(value),
                            FixedClock()));

                Assert.That(exception.Code,
                    Is.EqualTo(PaletteProjectBaselineAuditFailureCode.TargetAmbiguous));
            }
        }

        [Test]
        public void LoosePaletteCandidateIsRejectedBeforeMounting()
        {
            using (AuditFixture fixture = AuditFixture.Create(FixtureLayout.FixedChain))
            {
                fixture.WriteLoosePalette("isotem.pal");
                PaletteProjectBaselineAuditException exception =
                    Assert.Throws<PaletteProjectBaselineAuditException>(() =>
                        PaletteProjectBaselineAuditService.RunForTesting(
                            fixture.Configuration,
                            fixture.Profile(),
                            value => new ContentIndexer().Build(value),
                            FixedClock()));

                Assert.That(exception.Code, Is.EqualTo(
                    PaletteProjectBaselineAuditFailureCode.LoosePaletteCandidateFound));
            }
        }

        [Test]
        public void BaselineFingerprintChangeFailsBeforeManifestPublication()
        {
            using (AuditFixture fixture = AuditFixture.Create(FixtureLayout.FixedChain))
            {
                int calls = 0;
                PaletteProjectBaselineAuditException exception =
                    Assert.Throws<PaletteProjectBaselineAuditException>(() =>
                        PaletteProjectBaselineAuditService.RunForTesting(
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
                    PaletteProjectBaselineAuditFailureCode.BaselineChangedDuringAudit));
                Assert.That(Directory.Exists(fixture.CachePath), Is.False);
            }
        }

        [Test]
        public void ManifestBudgetFailureDoesNotPublishPartialArtifact()
        {
            using (AuditFixture fixture = AuditFixture.Create(FixtureLayout.FixedChain))
            {
                PaletteProjectBaselineAuditException exception =
                    Assert.Throws<PaletteProjectBaselineAuditException>(() =>
                        PaletteProjectBaselineAuditService.RunForTesting(
                            fixture.Configuration,
                            fixture.Profile(maxManifestBytes: 1),
                            value => new ContentIndexer().Build(value),
                            FixedClock()));

                Assert.That(exception.Code, Is.EqualTo(
                    PaletteProjectBaselineAuditFailureCode.ManifestBudgetExceeded));
                Assert.That(Directory.Exists(fixture.CachePath), Is.False);
            }
        }

        [Test]
        public void AuditRequiresExactlyOnePatchedProjectBaseline()
        {
            using (AuditFixture fixture = AuditFixture.Create(FixtureLayout.FixedChain))
            {
                ExternalContentConfiguration wrong = fixture.ConfigurationWith(
                    new ExternalContentSourceDescriptor(
                        "another-source",
                        ContentSourceKind.Patched,
                        fixture.SourcePath,
                        300,
                        "synthetic",
                        true));
                PaletteProjectBaselineAuditException exception =
                    Assert.Throws<PaletteProjectBaselineAuditException>(() =>
                        PaletteProjectBaselineAuditService.RunForTesting(
                            wrong,
                            fixture.Profile(),
                            value => new ContentIndexer().Build(value),
                            FixedClock()));

                Assert.That(exception.Code, Is.EqualTo(
                    PaletteProjectBaselineAuditFailureCode.InvalidBaselineConfiguration));
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

        private static string CanonicalModelSha256(byte[] raw)
        {
            byte[] domain = Encoding.ASCII.GetBytes("RA2YR.PAL.RAW.V1\0");
            byte[] canonical = new byte[checked(domain.Length + 4 + 256 * 5)];
            Buffer.BlockCopy(domain, 0, canonical, 0, domain.Length);
            canonical[domain.Length] = 0x00;
            canonical[domain.Length + 1] = 0x01;
            canonical[domain.Length + 2] = 0x00;
            canonical[domain.Length + 3] = 0x00;
            int output = domain.Length + 4;
            for (int index = 0; index < 256; index++)
            {
                canonical[output++] = checked((byte)(index & 0xff));
                canonical[output++] = checked((byte)(index >> 8));
                int input = checked(index * 3);
                canonical[output++] = raw[input];
                canonical[output++] = raw[input + 1];
                canonical[output++] = raw[input + 2];
            }

            return Sha256(canonical);
        }

        private enum FixtureLayout
        {
            FixedChain,
            InvalidChannel,
            DirectRootTargets,
            DuplicateTarget
        }

        private sealed class AuditFixture : IDisposable
        {
            private readonly TemporaryContentTestDirectory temporary;
            private readonly Dictionary<string, byte[]> palettes;

            private AuditFixture(TemporaryContentTestDirectory temporary)
            {
                this.temporary = temporary;
                RepositoryPath = temporary.CreateDirectory("repository");
                SourcePath = temporary.CreateDirectory("source");
                CachePath = temporary.GetPath("cache");
                palettes = new Dictionary<string, byte[]>(StringComparer.Ordinal)
                {
                    { "isotem.pal", BuildPalette(1) },
                    { "temperat.pal", BuildPalette(7) },
                    { "unittem.pal", BuildPalette(13) }
                };
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

            public PaletteProjectBaselineAuditProfile Profile(
                string payloadHashMismatch = null,
                string modelHashMismatch = null,
                long maxManifestBytes = 1024 * 1024)
            {
                return new PaletteProjectBaselineAuditProfile(
                    palettes.OrderBy(pair => pair.Key, StringComparer.Ordinal)
                        .Select(pair => new PaletteGoldenSampleSpecification(
                            pair.Key,
                            MixFileId.ComputeCandidateId(pair.Key).Value,
                            768,
                            string.Equals(
                                pair.Key,
                                payloadHashMismatch,
                                StringComparison.Ordinal)
                                ? new string('0', 64)
                                : Sha256(pair.Value),
                            string.Equals(
                                pair.Key,
                                modelHashMismatch,
                                StringComparison.Ordinal)
                                ? new string('0', 64)
                                : CanonicalModelSha256(pair.Value))),
                    maxManifestBytes,
                    MixMountLimits.Default,
                    PaletteReadLimits.Default);
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

            public void WriteLoosePalette(string name)
            {
                temporary.WriteBytes("source/" + name, palettes[name]);
            }

            public void Dispose()
            {
                temporary.Dispose();
            }

            private ExternalContentSourceDescriptor CreateBaselineSource()
            {
                return new ExternalContentSourceDescriptor(
                    PaletteProjectBaselineAuditService.BaselineLogicalName,
                    ContentSourceKind.Patched,
                    SourcePath,
                    300,
                    "synthetic-project-baseline",
                    true);
            }

            private void Populate(FixtureLayout layout)
            {
                if (layout == FixtureLayout.InvalidChannel)
                {
                    palettes["isotem.pal"][0] = 64;
                }

                MixWriteEntry[] targetEntries = palettes
                    .OrderBy(pair => pair.Key, StringComparer.Ordinal)
                    .Select(pair => Named(pair.Key, pair.Value))
                    .ToArray();
                byte[] cache = layout == FixtureLayout.DirectRootTargets
                    ? BuildMix(Array.Empty<MixWriteEntry>())
                    : BuildMix(targetEntries);
                var rootEntries = new List<MixWriteEntry>
                {
                    Named("cache.mix", cache)
                };
                if (layout == FixtureLayout.DirectRootTargets)
                {
                    rootEntries.AddRange(targetEntries);
                }
                else if (layout == FixtureLayout.DuplicateTarget)
                {
                    rootEntries.Add(Named("isotem.pal", palettes["isotem.pal"]));
                }

                temporary.WriteBytes("source/ra2.mix", BuildMix(rootEntries.ToArray()));
            }

            private static byte[] BuildPalette(int seed)
            {
                var bytes = new byte[768];
                for (int index = 0; index < 256; index++)
                {
                    bytes[index * 3] = checked((byte)((index + seed) & 63));
                    bytes[index * 3 + 1] = checked((byte)((index * 3 + seed) & 63));
                    bytes[index * 3 + 2] = checked((byte)((index * 5 + seed) & 63));
                }

                return bytes;
            }

            private static MixWriteEntry Named(string name, byte[] payload)
            {
                return new MixWriteEntry(MixFileId.ComputeCandidateId(name), payload);
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
