using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using NUnit.Framework;
using RA2YR.Core.Content;
using RA2YR.Core.Content.Ini.Audit;
using RA2YR.Core.Content.Mix;
using RA2YR.Core.Formats.Ini;
using RA2YR.Core.Formats.Mix;
using RA2YR.Core.Formats.Mix.Writing;
using RA2YR.Tests.EditMode.Content;

namespace RA2YR.Tests.EditMode.Content.Ini.Audit
{
    public sealed class IniProjectBaselineAuditServiceTests
    {
        private static readonly DateTime StartedUtc =
            new DateTime(2026, 8, 3, 3, 0, 0, DateTimeKind.Utc);
        private static readonly DateTime CompletedUtc =
            new DateTime(2026, 8, 3, 3, 0, 1, DateTimeKind.Utc);

        [Test]
        public void FourFixedCandidatesPublishSanitizedAndExternalEvidence()
        {
            using (AuditFixture fixture = AuditFixture.Create(FixtureLayout.Fixed))
            {
                IniProjectBaselineAuditDelivery delivery = fixture.Run();

                Assert.That(delivery.Status, Is.EqualTo(
                    IniProjectBaselineAuditStatus.Complete));
                Assert.That(delivery.DocumentCount, Is.EqualTo(4));
                Assert.That(delivery.LocatedSurveyCandidateCount, Is.EqualTo(1));
                Assert.That(delivery.ExternalManifestCacheRelativePath,
                    Does.StartWith("wp02f/ini-audits/YR1001_ProjectBaseline/"));
                Assert.That(delivery.ExternalManifestSha256, Has.Length.EqualTo(64));

                string summary = delivery.SanitizedSummaryJson;
                Assert.That(summary, Does.Contain(
                    "\"manifestType\":\"RA2YR.IniProjectBaselineAuditSanitized\""));
                Assert.That(summary, Does.Contain("\"byteIdentical\":true"));
                Assert.That(summary, Does.Contain("\"logicalName\":\"rulesmd.ini\""));
                Assert.That(summary, Does.Contain("\"opaque\":"));
                Assert.That(summary, Does.Not.Contain("PrivateSection"));
                Assert.That(summary, Does.Not.Contain("SecretValue"));
                Assert.That(summary, Does.Not.Contain("\"lineRecords\":["));
                Assert.That(summary, Does.Not.Contain("\"identityCacheRelativePath\":"));
                Assert.That(summary, Does.Not.Contain(fixture.RepositoryPath));
                Assert.That(summary, Does.Not.Contain(fixture.SourcePath));
                Assert.That(summary, Does.Not.Contain(fixture.CachePath));

                string external = fixture.ReadExternalManifest(delivery);
                Assert.That(external, Does.Contain(
                    "\"manifestType\":\"RA2YR.IniProjectBaselineAuditExternal\""));
                Assert.That(external, Does.Contain("\"lineRecords\":["));
                Assert.That(external, Does.Contain("\"rawLineSha256\":"));
                Assert.That(external, Does.Not.Contain("PrivateSection"));
                Assert.That(external, Does.Not.Contain("SecretValue"));
                Assert.That(external, Does.Not.Contain(fixture.RepositoryPath));
                Assert.That(external, Does.Not.Contain(fixture.SourcePath));
            }
        }

        [Test]
        public void BothRulesDocumentsRemainDistinctCompositionLayers()
        {
            using (AuditFixture fixture = AuditFixture.Create(FixtureLayout.Fixed))
            {
                string summary = fixture.Run().SanitizedSummaryJson;

                Assert.That(Count(summary, "\"logicalName\":\"rulesmd.ini\""),
                    Is.EqualTo(2));
                Assert.That(summary, Does.Contain("rulesmd-expand"));
                Assert.That(summary, Does.Contain("rulesmd-local"));
                Assert.That(summary, Does.Contain(
                    "Both rulesmd.ini documents remain distinct ordered composition layers"));
                Assert.That(summary, Does.Not.Contain("selectedCandidate"));
            }
        }

        [Test]
        public void RuntimeResolutionAuditRecordsConfiguredCompositionWithoutWholeFileWinner()
        {
            using (AuditFixture fixture = AuditFixture.Create(FixtureLayout.Fixed))
            {
                IniRuntimeProjectBaselineAuditDelivery delivery = fixture.RunRuntime();
                string summary = delivery.SanitizedSummary;

                Assert.That(summary, Does.Contain(
                    "\"manifestType\":\"RA2YR.IniRuntimeResolutionAuditSanitized\""));
                Assert.That(summary, Does.Contain(
                    "\"logicalName\":\"rulesmd.ini\",\"documentLayerCount\":2"));
                Assert.That(summary, Does.Contain("\"compositionSets\":["));
                Assert.That(summary, Does.Contain("\"wholeFileWinner\":null"));
                Assert.That(summary, Does.Contain(
                    "\"compositionStatus\":\"ConfiguredForProjectBaseline\""));
                Assert.That(summary, Does.Contain(
                    "\"level\":\"ConfiguredForProjectBaseline\""));
                Assert.That(summary, Does.Contain(
                    "\"configuresProjectBaseline\":true"));
                Assert.That(summary, Does.Contain(
                    "\"genericExplicitLoadPlanExecutable\":true"));
                Assert.That(summary, Does.Contain(
                    "\"projectBaselineCompositionConfigured\":true"));
                Assert.That(summary, Does.Contain("\"wholeFileWinnerSelected\":false"));
                Assert.That(summary, Does.Contain(
                    "\"originalRuntimeComparisonPassed\":false"));
                Assert.That(summary.IndexOf(
                        "\"layerId\":\"projectbaseline-ra2md\"",
                        StringComparison.Ordinal),
                    Is.LessThan(summary.IndexOf(
                        "\"layerId\":\"projectbaseline-expandmd01\"",
                        StringComparison.Ordinal)));
                Assert.That(summary, Does.Contain("\"patternCounts\":{"));
                Assert.That(delivery.SummarySha256, Has.Length.EqualTo(64));
                Assert.That(delivery.SummaryUtf8Length,
                    Is.EqualTo(Encoding.UTF8.GetByteCount(summary)));
                Assert.That(summary, Does.Not.Contain("PrivateSection"));
                Assert.That(summary, Does.Not.Contain("SecretValue"));
                Assert.That(summary, Does.Not.Contain(fixture.RepositoryPath));
                Assert.That(summary, Does.Not.Contain(fixture.SourcePath));
                Assert.That(summary, Does.Not.Contain(fixture.CachePath));
            }
        }

        [Test]
        public void IdentityArtifactsAreExactSyntheticInputs()
        {
            using (AuditFixture fixture = AuditFixture.Create(FixtureLayout.Fixed))
            {
                IniProjectBaselineAuditDelivery delivery = fixture.Run();
                string external = fixture.ReadExternalManifest(delivery);

                foreach (KeyValuePair<string, byte[]> sample in fixture.Samples)
                {
                    string[] files = Directory.GetFiles(
                        Path.Combine(
                            fixture.CachePath,
                            "wp02f",
                            "ini-audits",
                            IniProjectBaselineAuditService.BaselineLogicalName),
                        sample.Key + "-*.ini",
                        SearchOption.AllDirectories);
                    Assert.That(files, Has.Length.EqualTo(1));
                    Assert.That(File.ReadAllBytes(files[0]), Is.EqualTo(sample.Value));
                    Assert.That(external, Does.Contain(
                        "\"identityOutputSha256\":\"" + Sha256(sample.Value) + "\""));
                }
            }
        }

        [Test]
        public void SurveyFoundAndUnresolvedNamesAreReportedWithoutParsingSemantics()
        {
            using (AuditFixture fixture = AuditFixture.Create(FixtureLayout.Fixed))
            {
                string summary = fixture.Run().SanitizedSummaryJson;

                Assert.That(summary, Does.Contain("\"logicalName\":\"survey.ini\""));
                Assert.That(summary, Does.Contain(
                    "\"notLocatedInMountedDirectoryAndMixSources\":[\"absent.ini\"]"));
                Assert.That(summary, Does.Not.Contain("SurveySecret"));
            }
        }

        [Test]
        public void ChangedPayloadHashFailsBeforePublishingArtifacts()
        {
            using (AuditFixture fixture = AuditFixture.Create(FixtureLayout.Fixed))
            {
                IniProjectBaselineAuditException exception =
                    Assert.Throws<IniProjectBaselineAuditException>(() =>
                        fixture.Run(profile: fixture.Profile(payloadHashMismatch: true)));

                Assert.That(exception.Code, Is.EqualTo(
                    IniProjectBaselineAuditFailureCode.GoldenTargetHashMismatch));
                Assert.That(Directory.Exists(fixture.CachePath), Is.False);
            }
        }

        [Test]
        public void ChangedPayloadLengthFailsClosed()
        {
            using (AuditFixture fixture = AuditFixture.Create(FixtureLayout.Fixed))
            {
                IniProjectBaselineAuditException exception =
                    Assert.Throws<IniProjectBaselineAuditException>(() =>
                        fixture.Run(profile: fixture.Profile(lengthMismatch: true)));

                Assert.That(exception.Code, Is.EqualTo(
                    IniProjectBaselineAuditFailureCode.GoldenTargetLengthMismatch));
            }
        }

        [Test]
        public void MissingFixedTargetFailsClosed()
        {
            using (AuditFixture fixture = AuditFixture.Create(FixtureLayout.MissingAi))
            {
                IniProjectBaselineAuditException exception =
                    Assert.Throws<IniProjectBaselineAuditException>(() => fixture.Run());

                Assert.That(exception.Code, Is.EqualTo(
                    IniProjectBaselineAuditFailureCode.GoldenTargetMissing));
            }
        }

        [Test]
        public void MissingRootFailsBeforeMounting()
        {
            using (AuditFixture fixture = AuditFixture.Create(FixtureLayout.MissingRoot))
            {
                IniProjectBaselineAuditException exception =
                    Assert.Throws<IniProjectBaselineAuditException>(() => fixture.Run());

                Assert.That(exception.Code, Is.EqualTo(
                    IniProjectBaselineAuditFailureCode.RootArchiveMissing));
            }
        }

        [Test]
        public void LooseCandidateIsRejected()
        {
            using (AuditFixture fixture = AuditFixture.Create(FixtureLayout.Fixed))
            {
                fixture.WriteLooseIni();
                IniProjectBaselineAuditException exception =
                    Assert.Throws<IniProjectBaselineAuditException>(() => fixture.Run());

                Assert.That(exception.Code, Is.EqualTo(
                    IniProjectBaselineAuditFailureCode.LooseIniCandidateFound));
            }
        }

        [Test]
        public void InvalidIniWithMatchingHashFailsStrictParsing()
        {
            using (AuditFixture fixture = AuditFixture.Create(FixtureLayout.NulInAi))
            {
                IniProjectBaselineAuditException exception =
                    Assert.Throws<IniProjectBaselineAuditException>(() => fixture.Run());

                Assert.That(exception.Code, Is.EqualTo(
                    IniProjectBaselineAuditFailureCode.IniParseFailed));
            }
        }

        [Test]
        public void BaselineChangeFailsBeforeIdentityPublication()
        {
            using (AuditFixture fixture = AuditFixture.Create(FixtureLayout.Fixed))
            {
                int calls = 0;
                IniProjectBaselineAuditException exception =
                    Assert.Throws<IniProjectBaselineAuditException>(() => fixture.Run(
                        buildIndex: value =>
                        {
                            ContentIndexResult result = new ContentIndexer().Build(value);
                            if (calls++ == 0)
                            {
                                fixture.WriteAdditionalFile();
                            }

                            return result;
                        }));

                Assert.That(exception.Code, Is.EqualTo(
                    IniProjectBaselineAuditFailureCode.BaselineChangedDuringAudit));
                Assert.That(Directory.Exists(fixture.CachePath), Is.False);
            }
        }

        [Test]
        public void ManifestBudgetFailureDoesNotPublishJsonManifest()
        {
            using (AuditFixture fixture = AuditFixture.Create(FixtureLayout.Fixed))
            {
                IniProjectBaselineAuditException exception =
                    Assert.Throws<IniProjectBaselineAuditException>(() => fixture.Run(
                        profile: fixture.Profile(maxManifestBytes: 1)));

                Assert.That(exception.Code, Is.EqualTo(
                    IniProjectBaselineAuditFailureCode.ManifestBudgetExceeded));
                Assert.That(Directory.GetFiles(
                    fixture.CachePath,
                    "*.json",
                    SearchOption.AllDirectories),
                    Is.Empty);
            }
        }

        [Test]
        public void AuditRequiresExactlyOnePatchedNamedBaseline()
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
                IniProjectBaselineAuditException exception =
                    Assert.Throws<IniProjectBaselineAuditException>(() => fixture.Run(
                        configuration: wrong));

                Assert.That(exception.Code, Is.EqualTo(
                    IniProjectBaselineAuditFailureCode.InvalidBaselineConfiguration));
            }
        }

        [Test]
        public void ExternalCacheReparsePointIsRejectedBeforePublication()
        {
            if (Path.DirectorySeparatorChar != '\\')
            {
                Assert.Ignore("Windows junction behavior is validated on the primary platform.");
            }

            using (AuditFixture fixture = AuditFixture.Create(FixtureLayout.Fixed))
            {
                if (!fixture.TryCreateCacheJunction())
                {
                    Assert.Ignore("A bounded test junction could not be created on this host.");
                }

                IniProjectBaselineAuditException exception =
                    Assert.Throws<IniProjectBaselineAuditException>(() => fixture.Run());

                Assert.That(exception.Code, Is.EqualTo(
                    IniProjectBaselineAuditFailureCode.DirectoryIndexIncomplete));
            }
        }

        [Test]
        public void ExistingChangedIdentityArtifactFailsWithoutOverwrite()
        {
            using (AuditFixture fixture = AuditFixture.Create(FixtureLayout.Fixed))
            {
                fixture.Run();
                string identity = Directory.GetFiles(
                        fixture.CachePath,
                        "*.ini",
                        SearchOption.AllDirectories)
                    .OrderBy(path => path, StringComparer.Ordinal)
                    .First();
                byte[] changed = { 0x52, 0x41, 0x32, 0x59, 0x52 };
                File.WriteAllBytes(identity, changed);

                IniProjectBaselineAuditException exception =
                    Assert.Throws<IniProjectBaselineAuditException>(() => fixture.Run());

                Assert.That(exception.Code, Is.EqualTo(
                    IniProjectBaselineAuditFailureCode.ExternalArtifactWriteFailed));
                Assert.That(File.ReadAllBytes(identity), Is.EqualTo(changed));
            }
        }

        [Test]
        public void IdentityReferenceRejectsPathTraversal()
        {
            Assert.Throws<ArgumentException>(() => new IniIdentityArtifactReference(
                "../outside.ini",
                1,
                new string('0', 64)));
        }

        private static int Count(string value, string token)
        {
            int count = 0;
            int offset = 0;
            while ((offset = value.IndexOf(token, offset, StringComparison.Ordinal)) >= 0)
            {
                count++;
                offset += token.Length;
            }

            return count;
        }

        private static string Sha256(byte[] bytes)
        {
            using (SHA256 algorithm = SHA256.Create())
            {
                return BitConverter.ToString(algorithm.ComputeHash(bytes))
                    .Replace("-", string.Empty)
                    .ToLowerInvariant();
            }
        }

        private enum FixtureLayout
        {
            Fixed,
            MissingAi,
            MissingRoot,
            NulInAi
        }

        private sealed class AuditFixture : IDisposable
        {
            private readonly TemporaryContentTestDirectory temporary;
            private readonly Dictionary<string, byte[]> sampleBytes;

            private AuditFixture(TemporaryContentTestDirectory temporary, FixtureLayout layout)
            {
                this.temporary = temporary;
                RepositoryPath = temporary.CreateDirectory("repository");
                SourcePath = temporary.CreateDirectory("source");
                CachePath = temporary.GetPath("cache");
                sampleBytes = new Dictionary<string, byte[]>(StringComparer.Ordinal)
                {
                    ["artmd-local"] = Ascii(
                        "; comment\r\n[PrivateSection]\r\nArt = SecretValue\r\n"),
                    ["ai-local"] = layout == FixtureLayout.NulInAi
                        ? new byte[] { 0 }
                        : Ascii("[AI]\nTask=One\nTask=Two\n"),
                    ["rulesmd-expand"] = Ascii(
                        "[Rules]\rKey = Value\rOpaque directive\r"),
                    ["rulesmd-local"] = Ascii(
                        "[Rules]\r\nKey=Other\n; tail\r\n")
                };
                Configuration = ConfigurationWith(CreateBaselineSource());
                Populate(layout);
            }

            public ExternalContentConfiguration Configuration { get; }
            public string RepositoryPath { get; }
            public string SourcePath { get; }
            public string CachePath { get; }
            public IReadOnlyDictionary<string, byte[]> Samples => sampleBytes;

            public static AuditFixture Create(FixtureLayout layout)
            {
                return new AuditFixture(new TemporaryContentTestDirectory(), layout);
            }

            public IniProjectBaselineAuditDelivery Run(
                IniProjectBaselineAuditProfile profile = null,
                ExternalContentConfiguration configuration = null,
                Func<ExternalContentConfiguration, ContentIndexResult> buildIndex = null)
            {
                int clock = 0;
                return IniProjectBaselineAuditService.RunForTesting(
                    configuration ?? Configuration,
                    profile ?? Profile(),
                    buildIndex ?? (value => new ContentIndexer().Build(value)),
                    () => clock++ == 0 ? StartedUtc : CompletedUtc);
            }

            public IniRuntimeProjectBaselineAuditDelivery RunRuntime()
            {
                int clock = 0;
                return IniProjectBaselineAuditService.RunRuntimeResolutionAuditForTesting(
                    Configuration,
                    Profile(),
                    value => new ContentIndexer().Build(value),
                    () => clock++ == 0 ? StartedUtc : CompletedUtc);
            }

            public IniProjectBaselineAuditProfile Profile(
                bool payloadHashMismatch = false,
                bool lengthMismatch = false,
                long maxManifestBytes = 1024 * 1024)
            {
                IniGoldenSampleSpecification[] specifications =
                {
                    Specification("artmd-local", "ra2md.mix", "localmd.mix", "artmd.ini"),
                    Specification(
                        "ai-local",
                        "ra2.mix",
                        "local.mix",
                        "ai.ini",
                        payloadHashMismatch,
                        lengthMismatch),
                    Specification(
                        "rulesmd-expand",
                        "expandmd01.mix",
                        null,
                        "rulesmd.ini"),
                    Specification(
                        "rulesmd-local",
                        "ra2md.mix",
                        "localmd.mix",
                        "rulesmd.ini")
                };
                return new IniProjectBaselineAuditProfile(
                    specifications,
                    new[] { "survey.ini", "absent.ini" },
                    new[] { "local.mix", "localmd.mix" },
                    maxManifestBytes,
                    1024 * 1024,
                    MixMountLimits.Default,
                    IniReadLimits.Default);
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

            public string ReadExternalManifest(IniProjectBaselineAuditDelivery delivery)
            {
                string path = Path.Combine(
                    CachePath,
                    delivery.ExternalManifestCacheRelativePath.Replace(
                        '/',
                        Path.DirectorySeparatorChar));
                Assert.That(new FileInfo(path).Length,
                    Is.EqualTo(delivery.ExternalManifestLength));
                Assert.That(Sha256(File.ReadAllBytes(path)),
                    Is.EqualTo(delivery.ExternalManifestSha256));
                return File.ReadAllText(path);
            }

            public void WriteLooseIni()
            {
                temporary.WriteBytes("source/ai.ini", sampleBytes["ai-local"]);
            }

            public void WriteAdditionalFile()
            {
                temporary.WriteBytes("source/appeared.bin", new byte[] { 1 });
            }

            public bool TryCreateCacheJunction()
            {
                string target = temporary.CreateDirectory("cache-target");
                var startInfo = new ProcessStartInfo
                {
                    FileName = Environment.GetEnvironmentVariable("ComSpec"),
                    Arguments = "/d /c mklink /J \"" + CachePath + "\" \"" + target + "\"",
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };
                using (Process process = Process.Start(startInfo))
                {
                    if (process == null)
                    {
                        return false;
                    }

                    if (!process.WaitForExit(10000))
                    {
                        process.Kill();
                        return false;
                    }

                    return process.ExitCode == 0;
                }
            }

            public void Dispose()
            {
                temporary.Dispose();
            }

            private IniGoldenSampleSpecification Specification(
                string sampleId,
                string root,
                string nested,
                string logicalName,
                bool hashMismatch = false,
                bool lengthMismatch = false)
            {
                byte[] bytes = sampleBytes[sampleId];
                return new IniGoldenSampleSpecification(
                    sampleId,
                    root,
                    nested,
                    logicalName,
                    lengthMismatch ? bytes.Length + 1 : bytes.Length,
                    hashMismatch ? new string('0', 64) : Sha256(bytes));
            }

            private ExternalContentSourceDescriptor CreateBaselineSource()
            {
                return new ExternalContentSourceDescriptor(
                    IniProjectBaselineAuditService.BaselineLogicalName,
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

                var localEntries = new List<MixWriteEntry>();
                if (layout != FixtureLayout.MissingAi)
                {
                    localEntries.Add(Entry("ai.ini", sampleBytes["ai-local"]));
                }

                localEntries.Add(Entry("survey.ini", Ascii("[Survey]\nK=SurveySecret\n")));
                byte[] localMix = BuildMix(localEntries.ToArray());
                temporary.WriteBytes(
                    "source/ra2.mix",
                    BuildMix(Entry("local.mix", localMix)));

                byte[] localMdMix = BuildMix(
                    Entry("artmd.ini", sampleBytes["artmd-local"]),
                    Entry("rulesmd.ini", sampleBytes["rulesmd-local"]));
                temporary.WriteBytes(
                    "source/ra2md.mix",
                    BuildMix(Entry("localmd.mix", localMdMix)));
                temporary.WriteBytes(
                    "source/expandmd01.mix",
                    BuildMix(Entry("rulesmd.ini", sampleBytes["rulesmd-expand"])));
            }

            private static MixWriteEntry Entry(string name, byte[] bytes)
            {
                return new MixWriteEntry(MixFileId.ComputeCandidateId(name), bytes);
            }

            private static byte[] BuildMix(params MixWriteEntry[] entries)
            {
                var options = new MixWriteOptions(
                    MixWriteOrder.PreserveEntryOrder,
                    MixWriteHeaderKind.Classic,
                    false,
                    null,
                    32,
                    4 * 1024 * 1024);
                MixWriteResult result = MixArchiveWriter.Build(entries, options);
                if (!result.IsSuccess)
                {
                    throw new InvalidOperationException("Synthetic MIX construction failed.");
                }

                return result.GetArchiveBytes();
            }

            private static byte[] Ascii(string value)
            {
                return Encoding.ASCII.GetBytes(value);
            }
        }
    }
}
