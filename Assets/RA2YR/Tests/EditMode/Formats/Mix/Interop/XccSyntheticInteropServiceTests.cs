using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using NUnit.Framework;
using RA2YR.Core.Binary;
using RA2YR.Core.Content;
using RA2YR.Core.Formats.Mix;
using RA2YR.Core.Formats.Mix.Interop;
using RA2YR.Core.Formats.Mix.Writing;

namespace RA2YR.Tests.EditMode.Formats.Mix.Interop
{
    [TestFixture]
    public sealed class XccSyntheticInteropServiceTests
    {
        private static readonly string[] XccInputNames =
        {
            "xcc-alpha.synthetic.bin",
            "xcc-empty.synthetic.bin",
            "xcc-omega.synthetic.bin"
        };

        private const string LocalMixDatabaseName = "local mix database.dat";

        private static readonly byte[] EmulatedLocalMixDatabasePayload =
            System.Text.Encoding.ASCII.GetBytes(
                "RA2YR-WP02C-EMULATED-XCC-LMD-V1\r\n");

        [Test]
        public void PrepareInternalContractPublishesDeterministicSyntheticModesOutsideRepository()
        {
            using (var temporary = new TemporaryWorkspace())
            {
                ExternalContentConfiguration configuration = temporary.CreateConfiguration();
                var service = new XccSyntheticInteropService();

                XccSyntheticInteropResult first =
                    service.PrepareInternalContract(configuration, "case-a");
                XccSyntheticInteropResult second =
                    service.PrepareInternalContract(configuration, "case-b");

                Assert.That(first.IsSuccess, Is.True, FirstDiagnostic(first));
                Assert.That(second.IsSuccess, Is.True, FirstDiagnostic(second));
                Assert.That(first.Artifacts, Has.Count.EqualTo(6));
                Assert.That(first.Artifacts.All(artifact =>
                    !Path.IsPathRooted(artifact.CacheRelativePath)), Is.True);
                Assert.That(first.Artifacts.All(artifact => artifact.Sha256.Length == 64), Is.True);
                Assert.That(first.IsRealXccExecutionEvidence, Is.False);
                AssertPublishedArtifacts(configuration, first);

                string firstRoot = CaseRoot(configuration, "case-a");
                string secondRoot = CaseRoot(configuration, "case-b");
                string[] archivePaths =
                {
                    "outgoing-to-xcc/ra2yr-classic.mix",
                    "outgoing-to-xcc/ra2yr-checksum.mix",
                    "outgoing-to-xcc/ra2yr-encrypted.mix",
                    "outgoing-to-xcc/inner/local.mix",
                    "outgoing-to-xcc/ra2yr-nested.mix"
                };
                foreach (string relative in archivePaths)
                {
                    Assert.That(
                        File.ReadAllBytes(Combine(firstRoot, relative)),
                        Is.EqualTo(File.ReadAllBytes(Combine(secondRoot, relative))));
                }

                using (MixArchive classic = ReadArchive(
                           Combine(firstRoot, archivePaths[0]),
                           "classic.mix"))
                using (MixArchive checksum = ReadArchive(
                           Combine(firstRoot, archivePaths[1]),
                           "checksum.mix"))
                using (MixArchive encrypted = ReadArchive(
                           Combine(firstRoot, archivePaths[2]),
                           "encrypted.mix"))
                using (MixArchive nested = ReadArchive(
                           Combine(firstRoot, archivePaths[4]),
                           "nested.mix"))
                {
                    Assert.That(classic.HeaderKind, Is.EqualTo(MixArchiveHeaderKind.Classic));
                    Assert.That(classic.Entries.Any(entry => entry.Length == 0), Is.True);
                    Assert.That(checksum.HasChecksum && checksum.ChecksumVerified, Is.True);
                    Assert.That(encrypted.IsEncrypted, Is.True);
                    MixArchiveEntry inner = nested.Entries.Single(entry =>
                        entry.Id == MixFileId.ComputeCandidateId("inner/local.mix"));
                    var bytes = new byte[inner.Length];
                    inner.OpenPayloadWindow().ReadExactly(
                        0, bytes, 0, bytes.Length, "nested-test");
                    using (MixArchive innerArchive = ReadArchive(bytes, "inner/local.mix"))
                    {
                        Assert.That(innerArchive.Entries, Has.Count.EqualTo(1));
                    }
                }

                string manifest = File.ReadAllText(
                    Combine(firstRoot, "manifests/expected.json"));
                Assert.That(manifest, Does.Contain("\"synthetic\":true"));
                Assert.That(manifest, Does.Not.Contain(configuration.CachePath));
                Assert.That(manifest, Does.Not.Contain("RA2YR-WP02C-SYNTHETIC-ALPHA-V1"));
                Assert.That(Directory.Exists(temporary.RepositoryRoot), Is.True);
                Assert.That(Directory.GetFiles(
                    temporary.RepositoryRoot,
                    "*",
                    SearchOption.AllDirectories), Is.Empty);
            }
        }

        [TestCase("")]
        [TestCase("Case")]
        [TestCase("1case")]
        [TestCase("../escape")]
        [TestCase("case/escape")]
        [TestCase("case\\escape")]
        [TestCase("case.id")]
        [TestCase("C:/escape")]
        public void UnsafeCaseIdsFailBeforeCacheCreation(string caseId)
        {
            using (var temporary = new TemporaryWorkspace())
            {
                ExternalContentConfiguration configuration = temporary.CreateConfiguration();

                XccSyntheticInteropResult result =
                    new XccSyntheticInteropService().PrepareInternalContract(
                        configuration,
                        caseId);

                AssertFailure(result, XccSyntheticInteropDiagnosticCode.InvalidCaseId);
                Assert.That(Directory.Exists(configuration.CachePath), Is.False);
            }
        }

        [Test]
        public void EmulatedXccCandidateWithLocalDatabaseValidatesPayloadSetAndPreservesObservedOrder()
        {
            using (var temporary = new TemporaryWorkspace())
            {
                ExternalContentConfiguration configuration = temporary.CreateConfiguration();
                var service = new XccSyntheticInteropService();
                Assert.That(
                    service.PrepareInternalContract(configuration, "roundtrip").IsSuccess,
                    Is.True);
                WriteEmulatedCreatedCandidate(configuration, "roundtrip", XccInputNames);

                XccSyntheticInteropResult result =
                    service.ValidateStagedCreatedCandidate(configuration, "roundtrip");

                Assert.That(result.IsSuccess, Is.True, FirstDiagnostic(result));
                string rebuildPath = Combine(
                    CaseRoot(configuration, "roundtrip"),
                    "verified/xcc-created/xcc-created-preserve.mix");
                using (MixArchive rebuild = ReadArchive(rebuildPath, "rebuild.mix"))
                {
                    var expectedById = XccInputNames.ToDictionary(
                        MixFileId.ComputeCandidateId,
                        name => File.ReadAllBytes(InputPath(
                            configuration,
                            "roundtrip",
                            name)));
                    expectedById.Add(
                        MixFileId.ComputeCandidateId(LocalMixDatabaseName),
                        EmulatedLocalMixDatabasePayload);
                    MixFileId[] expectedObservedOrder = expectedById.Keys
                        .OrderBy(id => unchecked((int)id.Value))
                        .ToArray();
                    Assert.That(rebuild.Entries, Has.Count.EqualTo(4));
                    Assert.That(
                        rebuild.Entries.Select(entry => entry.Id).ToArray(),
                        Is.EqualTo(expectedObservedOrder));
                    for (int index = 0; index < rebuild.Entries.Count; index++)
                    {
                        byte[] expected = expectedById[rebuild.Entries[index].Id];
                        var actual = new byte[expected.Length];
                        rebuild.Entries[index].OpenPayloadWindow().ReadExactly(
                            0,
                            actual,
                            0,
                            actual.Length,
                            "independent-rebuild-payload-check");
                        Assert.That(actual, Is.EqualTo(expected));
                    }
                }

                string manifest = File.ReadAllText(Combine(
                    CaseRoot(configuration, "roundtrip"),
                    "verified/xcc-created/verification.json"));
                Assert.That(manifest, Does.Contain("\"entryOrderPreserved\":true"));
                Assert.That(manifest, Does.Contain("\"payloadHashesMatched\":true"));
                Assert.That(manifest, Does.Contain("\"localMixDatabase\":{\"present\":true"));
                Assert.That(manifest, Does.Contain(
                    "\"id\":\"" +
                    MixFileId.ComputeCandidateId(LocalMixDatabaseName) + "\""));
                Assert.That(manifest, Does.Not.Contain(
                    "RA2YR-WP02C-EMULATED-XCC-LMD-V1"));
                Assert.That(manifest, Does.Not.Contain(configuration.CachePath));
            }
        }

        [Test]
        public void MissingOrUnexpectedXccArchiveFailsClosedWithoutRebuild()
        {
            using (var temporary = new TemporaryWorkspace())
            {
                ExternalContentConfiguration configuration = temporary.CreateConfiguration();
                var service = new XccSyntheticInteropService();
                Assert.That(
                    service.PrepareInternalContract(configuration, "missing").IsSuccess,
                    Is.True);

                XccSyntheticInteropResult missing =
                    service.ValidateStagedCreatedCandidate(configuration, "missing");
                AssertFailure(missing, XccSyntheticInteropDiagnosticCode.RequiredInputMissing);

                Assert.That(
                    service.PrepareInternalContract(configuration, "unexpected").IsSuccess,
                    Is.True);
                WriteEmulatedCreatedCandidate(
                    configuration,
                    "unexpected",
                    XccInputNames,
                    "unexpected.synthetic.bin");
                XccSyntheticInteropResult unexpected =
                    service.ValidateStagedCreatedCandidate(configuration, "unexpected");
                AssertFailure(unexpected, XccSyntheticInteropDiagnosticCode.ArchiveMismatch);
                Assert.That(File.Exists(Combine(
                    CaseRoot(configuration, "unexpected"),
                    "verified/xcc-created/xcc-created-preserve.mix")), Is.False);
            }
        }

        [Test]
        public void ValidateStagedExtractionCandidatesRequiresEveryIndependentFixedGroup()
        {
            using (var temporary = new TemporaryWorkspace())
            {
                ExternalContentConfiguration configuration = temporary.CreateConfiguration();
                var service = new XccSyntheticInteropService();
                Assert.That(
                    service.PrepareInternalContract(configuration, "extract-ok").IsSuccess,
                    Is.True);
                WriteEmulatedCreatedCandidate(configuration, "extract-ok", XccInputNames);
                Assert.That(
                    service.ValidateStagedCreatedCandidate(configuration, "extract-ok").IsSuccess,
                    Is.True);
                PopulateEmulatedExtractionCandidates(configuration, "extract-ok");

                XccSyntheticInteropResult success =
                    service.ValidateStagedExtractionCandidates(configuration, "extract-ok");

                Assert.That(success.IsSuccess, Is.True, FirstDiagnostic(success));
                Assert.That(success.Artifacts, Has.Count.EqualTo(23));
                Assert.That(success.Artifacts.All(artifact =>
                    !Path.IsPathRooted(artifact.CacheRelativePath)), Is.True);
                Assert.That(success.IsRealXccExecutionEvidence, Is.False);
                AssertPublishedArtifacts(configuration, success);
                string verification = File.ReadAllText(Combine(
                    CaseRoot(configuration, "extract-ok"),
                    "verified/staged-extractions/verification.json"));
                foreach (string role in new[]
                         {
                             "ra2yr-classic",
                             "ra2yr-checksum",
                             "ra2yr-encrypted",
                             "ra2yr-inner",
                             "ra2yr-nested",
                             "xcc-created-rebuild"
                         })
                {
                    Assert.That(verification, Does.Contain("\"inputRole\":\"" + role + "\""));
                }

                Assert.That(verification,
                    Does.Contain("\"realXccExecutionAttested\":false"));

                Assert.That(
                    service.PrepareInternalContract(configuration, "extract-bad").IsSuccess,
                    Is.True);
                WriteEmulatedCreatedCandidate(configuration, "extract-bad", XccInputNames);
                Assert.That(
                    service.ValidateStagedCreatedCandidate(configuration, "extract-bad").IsSuccess,
                    Is.True);
                PopulateEmulatedExtractionCandidates(configuration, "extract-bad");
                File.WriteAllBytes(Combine(
                    CaseRoot(configuration, "extract-bad"),
                    "extracted-candidates/xcc-created-rebuild/xcc-alpha.synthetic.bin"),
                    new byte[] { 0xff });

                XccSyntheticInteropResult failure =
                    service.ValidateStagedExtractionCandidates(configuration, "extract-bad");
                AssertFailure(failure, XccSyntheticInteropDiagnosticCode.PayloadMismatch);
                Assert.That(Directory.Exists(Combine(
                    CaseRoot(configuration, "extract-bad"),
                    "verified/staged-extractions")), Is.False);
            }
        }

        [Test]
        public void ConfigurationBoundaryRejectsCacheContentOrRepositoryOverlap()
        {
            using (var temporary = new TemporaryWorkspace())
            {
                string source = temporary.ExternalRoot;
                string repository = temporary.RepositoryRoot;
                var descriptor = new ExternalContentSourceDescriptor(
                    "baseline",
                    ContentSourceKind.Patched,
                    source,
                    300,
                    "synthetic",
                    true);

                Assert.Throws<ArgumentException>(() => new ExternalContentConfiguration(
                    1,
                    Path.Combine(repository, "ExternalContent.xml"),
                    repository,
                    Path.Combine(repository, "Cache"),
                    new[] { descriptor }));
                Assert.Throws<ArgumentException>(() => new ExternalContentConfiguration(
                    1,
                    Path.Combine(repository, "ExternalContent.xml"),
                    repository,
                    Path.Combine(source, "Cache"),
                    new[] { descriptor }));
            }
        }

        [Test]
        public void ExtractionEnumerationBudgetFailsClosedWithoutPublishedResult()
        {
            using (var temporary = new TemporaryWorkspace())
            {
                ExternalContentConfiguration configuration = temporary.CreateConfiguration();
                var service = new XccSyntheticInteropService();
                Assert.That(
                    service.PrepareInternalContract(configuration, "bounded").IsSuccess,
                    Is.True);
                WriteEmulatedCreatedCandidate(configuration, "bounded", XccInputNames);
                Assert.That(
                    service.ValidateStagedCreatedCandidate(configuration, "bounded").IsSuccess,
                    Is.True);
                PopulateEmulatedExtractionCandidates(configuration, "bounded");
                string directory = Combine(
                    CaseRoot(configuration, "bounded"),
                    "extracted-candidates/ra2yr-classic");
                for (int index = 0; index < 17; index++)
                {
                    File.WriteAllBytes(
                        Path.Combine(directory, "extra-" + index + ".bin"),
                        new byte[] { (byte)index });
                }

                XccSyntheticInteropResult result =
                    service.ValidateStagedExtractionCandidates(configuration, "bounded");

                AssertFailure(
                    result,
                    XccSyntheticInteropDiagnosticCode.ExtractionBudgetExceeded);
                Assert.That(Directory.Exists(Combine(
                    CaseRoot(configuration, "bounded"),
                    "verified/staged-extractions")), Is.False);
                AssertNoStagingDirectories(configuration, "bounded");
            }
        }

        [Test]
        public void PublicModelsRejectUnsafePathsLengthsHashesAndFalseEvidenceShapes()
        {
            Assert.Throws<ArgumentException>(() => new XccSyntheticInteropArtifact(
                "role",
                "C:/private/file.mix",
                1,
                new string('0', 64)));
            Assert.Throws<ArgumentOutOfRangeException>(() => new XccSyntheticInteropArtifact(
                "role",
                "wp02c/file.mix",
                -1,
                new string('0', 64)));
            Assert.Throws<ArgumentException>(() => new XccSyntheticInteropArtifact(
                "role",
                "wp02c/file.mix",
                1,
                new string('A', 64)));
            Assert.Throws<ArgumentException>(() => new XccSyntheticInteropDiagnostic(
                XccSyntheticInteropDiagnosticCode.PayloadMismatch,
                "C:/private/body"));
            Assert.Throws<ArgumentException>(() => XccSyntheticInteropResult.Success(
                XccSyntheticInteropStage.PrepareInternalContract,
                "case-a",
                Array.Empty<XccSyntheticInteropArtifact>()));
        }

        [Test]
        public void ControlledCleanupDeletesEveryOwnedShapeButRejectsOutsideBoundary()
        {
            using (var temporary = new TemporaryWorkspace())
            {
                string approved = Path.Combine(temporary.Root, "approved");
                string stage = Path.Combine(approved, ".case.ra2yr-stage");
                string published = Path.Combine(approved, "published");
                string outside = Path.Combine(temporary.Root, "outside");
                Directory.CreateDirectory(stage);
                Directory.CreateDirectory(published);
                Directory.CreateDirectory(outside);
                File.WriteAllBytes(Path.Combine(stage, "partial.bin"), new byte[] { 1 });
                File.WriteAllBytes(Path.Combine(published, "result.bin"), new byte[] { 2 });

                Assert.That(
                    XccSyntheticInteropService.TryDeleteOwnedDirectory(stage, approved, true),
                    Is.True);
                Assert.That(
                    XccSyntheticInteropService.TryDeleteOwnedDirectory(
                        published,
                        approved,
                        false),
                    Is.True);
                Assert.That(
                    XccSyntheticInteropService.TryDeleteOwnedDirectory(outside, approved, false),
                    Is.False);
                Assert.That(
                    XccSyntheticInteropService.TryDeleteOwnedDirectory(
                        approved,
                        approved + Path.DirectorySeparatorChar,
                        false),
                    Is.False);
                Assert.That(Directory.Exists(stage), Is.False);
                Assert.That(Directory.Exists(published), Is.False);
                Assert.That(Directory.Exists(approved), Is.True);
                Assert.That(Directory.Exists(outside), Is.True);
            }
        }

        private static void WriteEmulatedCreatedCandidate(
            ExternalContentConfiguration configuration,
            string caseId,
            IReadOnlyList<string> payloadNames,
            string unexpectedName = null)
        {
            var entries = payloadNames.Select(name =>
                new MixWriteEntry(
                    MixFileId.ComputeCandidateId(name),
                    File.ReadAllBytes(InputPath(configuration, caseId, name))))
                .Concat(new[]
                {
                    new MixWriteEntry(
                        MixFileId.ComputeCandidateId(LocalMixDatabaseName),
                        EmulatedLocalMixDatabasePayload)
                });
            if (unexpectedName != null)
            {
                entries = entries.Concat(new[]
                {
                    new MixWriteEntry(
                        MixFileId.ComputeCandidateId(unexpectedName),
                        new byte[] { 0x51, 0x43, 0x43 })
                });
            }

            MixWriteEntry[] xccObservedEntries = entries
                .OrderBy(entry => unchecked((int)entry.Id.Value))
                .ToArray();
            string root = CaseRoot(configuration, caseId);
            MixWriteResult result = MixArchiveWriter.WriteToFile(
                xccObservedEntries,
                new MixWriteOptions(
                    MixWriteOrder.PreserveEntryOrder,
                    MixWriteHeaderKind.Classic,
                    false,
                    null,
                    128,
                    16 * 1024 * 1024),
                Combine(root, "incoming-from-xcc/xcc-created.mix"),
                root,
                MixOutputPurpose.TemporaryTestDirectory,
                false);
            Assert.That(result.IsSuccess, Is.True);
        }

        private static void PopulateEmulatedExtractionCandidates(
            ExternalContentConfiguration configuration,
            string caseId)
        {
            string root = CaseRoot(configuration, caseId);
            ExtractArchiveEmulated(
                Combine(root, "outgoing-to-xcc/ra2yr-classic.mix"),
                Combine(root, "extracted-candidates/ra2yr-classic"),
                new[] { "alpha.synthetic.bin", "empty.synthetic.bin", "omega.synthetic.bin" });
            ExtractArchiveEmulated(
                Combine(root, "outgoing-to-xcc/ra2yr-checksum.mix"),
                Combine(root, "extracted-candidates/ra2yr-checksum"),
                new[] { "alpha.synthetic.bin", "empty.synthetic.bin", "omega.synthetic.bin" });
            ExtractArchiveEmulated(
                Combine(root, "outgoing-to-xcc/ra2yr-encrypted.mix"),
                Combine(root, "extracted-candidates/ra2yr-encrypted"),
                new[] { "alpha.synthetic.bin", "empty.synthetic.bin", "omega.synthetic.bin" });
            ExtractArchiveEmulated(
                Combine(root, "outgoing-to-xcc/inner/local.mix"),
                Combine(root, "extracted-candidates/ra2yr-inner"),
                new[] { "inner-note.synthetic.bin" });
            ExtractArchiveEmulated(
                Combine(root, "outgoing-to-xcc/ra2yr-nested.mix"),
                Combine(root, "extracted-candidates/ra2yr-nested"),
                new[] { "inner/local.mix", "outer-note.synthetic.bin" });
            ExtractArchiveEmulated(
                Combine(root, "verified/xcc-created/xcc-created-preserve.mix"),
                Combine(root, "extracted-candidates/xcc-created-rebuild"),
                XccInputNames.Concat(new[] { LocalMixDatabaseName }).ToArray());
        }

        private static void ExtractArchiveEmulated(
            string archivePath,
            string destinationRoot,
            IReadOnlyList<string> names)
        {
            using (MixArchive archive = ReadArchive(archivePath, "emulated-extraction.mix"))
            {
                Assert.That(archive.Entries, Has.Count.EqualTo(names.Count));
                foreach (string name in names)
                {
                    MixArchiveEntry entry = archive.Entries.Single(item =>
                        item.Id == MixFileId.ComputeCandidateId(name));
                    var bytes = new byte[checked((int)entry.Length)];
                    entry.OpenPayloadWindow().ReadExactly(
                        0,
                        bytes,
                        0,
                        bytes.Length,
                        "emulated-extraction");
                    string path = Combine(destinationRoot, name);
                    Directory.CreateDirectory(Path.GetDirectoryName(path));
                    File.WriteAllBytes(path, bytes);
                }
            }
        }

        private static MixArchive ReadArchive(string path, string logicalPath)
        {
            return ReadArchive(File.ReadAllBytes(path), logicalPath);
        }

        private static MixArchive ReadArchive(byte[] bytes, string logicalPath)
        {
            var source = new BinarySourceContext(
                "xcc-interop-test",
                "synthetic",
                LogicalContentPath.Parse(logicalPath));
            MixArchiveReadResult result = MixArchiveReader.Read(bytes, source);
            Assert.That(result.IsSuccess, Is.True,
                result.Diagnostics.Count == 0 ? null : result.Diagnostics[0].Message);
            return result.Archive;
        }

        private static string InputPath(
            ExternalContentConfiguration configuration,
            string caseId,
            string name)
        {
            return Combine(CaseRoot(configuration, caseId), "inputs-for-xcc/" + name);
        }

        private static string CaseRoot(
            ExternalContentConfiguration configuration,
            string caseId)
        {
            return Path.Combine(configuration.CachePath, "wp02c", "xcc-interop", caseId);
        }

        private static string Combine(string root, string relative)
        {
            return Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar));
        }

        private static void AssertFailure(
            XccSyntheticInteropResult result,
            XccSyntheticInteropDiagnosticCode code)
        {
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Artifacts, Is.Empty);
            Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(result.Diagnostics[0].Code, Is.EqualTo(code));
            Assert.That(result.Diagnostics[0].Message, Does.Not.Contain(":\\"));
        }

        private static void AssertPublishedArtifacts(
            ExternalContentConfiguration configuration,
            XccSyntheticInteropResult result)
        {
            Assert.That(result.Artifacts.Select(item => item.CacheRelativePath).Distinct().Count(),
                Is.EqualTo(result.Artifacts.Count));
            Assert.That(result.Artifacts.Select(item => item.Role).Distinct().Count(),
                Is.EqualTo(result.Artifacts.Count));
            foreach (XccSyntheticInteropArtifact artifact in result.Artifacts)
            {
                Assert.That(artifact.Sha256, Does.Match("^[0-9a-f]{64}$"));
                string path = Combine(configuration.CachePath, artifact.CacheRelativePath);
                Assert.That(File.Exists(path), Is.True, artifact.CacheRelativePath);
                Assert.That(new FileInfo(path).Length, Is.EqualTo(artifact.Length));
                Assert.That(HashFile(path), Is.EqualTo(artifact.Sha256));
            }
        }

        private static void AssertNoStagingDirectories(
            ExternalContentConfiguration configuration,
            string caseId)
        {
            string caseRoot = CaseRoot(configuration, caseId);
            Assert.That(Directory.GetDirectories(
                    caseRoot,
                    "*.ra2yr-stage",
                    SearchOption.TopDirectoryOnly),
                Is.Empty);
        }

        private static string HashFile(string path)
        {
            using (var stream = File.OpenRead(path))
            using (SHA256 sha256 = SHA256.Create())
            {
                return BitConverter.ToString(sha256.ComputeHash(stream))
                    .Replace("-", string.Empty)
                    .ToLowerInvariant();
            }
        }

        private static string FirstDiagnostic(XccSyntheticInteropResult result)
        {
            return result.Diagnostics.Count == 0 ? null : result.Diagnostics[0].Message;
        }

        private sealed class TemporaryWorkspace : IDisposable
        {
            public TemporaryWorkspace()
            {
                Root = Path.Combine(
                    Path.GetTempPath(),
                    "RA2YR.XccInterop.Tests",
                    Guid.NewGuid().ToString("N"));
                RepositoryRoot = Path.Combine(Root, "Repository");
                ExternalRoot = Path.Combine(Root, "External");
                Directory.CreateDirectory(RepositoryRoot);
                Directory.CreateDirectory(ExternalRoot);
            }

            public string Root { get; }

            public string RepositoryRoot { get; }

            public string ExternalRoot { get; }

            public ExternalContentConfiguration CreateConfiguration()
            {
                return new ExternalContentConfiguration(
                    1,
                    Path.Combine(RepositoryRoot, "ExternalContent.xml"),
                    RepositoryRoot,
                    Path.Combine(Root, "Cache"),
                    new[]
                    {
                        new ExternalContentSourceDescriptor(
                            "baseline",
                            ContentSourceKind.Patched,
                            ExternalRoot,
                            300,
                            "synthetic",
                            true)
                    });
            }

            public void Dispose()
            {
                if (Directory.Exists(Root))
                {
                    Directory.Delete(Root, true);
                }
            }
        }
    }
}
