using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using NUnit.Framework;
using RA2YR.Core.Content;

namespace RA2YR.Tests.EditMode.Content
{
    public sealed class ContentIndexerTests
    {
        [Test]
        public void BuildUsesStableRelativePathOrderSha256AndFingerprint()
        {
            using (var temporary = new TemporaryContentTestDirectory())
            {
                temporary.CreateDirectory("Repository");
                temporary.CreateDirectory("Cache");
                temporary.CreateDirectory("External/sub");
                temporary.WriteBytes("External/z.bin", Encoding.ASCII.GetBytes("last"));
                temporary.WriteBytes("External/sub/B.bin", Encoding.ASCII.GetBytes("middle"));
                temporary.WriteBytes("External/a.bin", Encoding.ASCII.GetBytes("abc"));
                ExternalContentConfiguration configuration = CreateConfiguration(temporary);

                ContentIndexResult first = new ContentIndexer().Build(configuration);
                ContentIndexResult second = new ContentIndexer().Build(configuration);

                Assert.That(first.HasErrors, Is.False);
                Assert.That(first.IsComplete, Is.True);
                Assert.That(first.Sources, Has.Count.EqualTo(1));
                Assert.That(
                    first.Sources[0].Files.Select(file => file.RelativePath),
                    Is.EqualTo(new[] { "a.bin", "sub/B.bin", "z.bin" }));
                Assert.That(
                    first.Sources[0].Files[0].Sha256,
                    Is.EqualTo("ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad"));
                Assert.That(first.Sources[0].Files[0].Length, Is.EqualTo(3));
                Assert.That(
                    first.Sources[0].Fingerprint,
                    Is.EqualTo("f580fa149f3abdf4af5c3b2d7779376406689d6d00bb1a706cfb2a064f79cd28"));
                Assert.That(
                    first.Sources[0].Fingerprint,
                    Is.EqualTo(second.Sources[0].Fingerprint));
                Assert.That(
                    ContentManifestSerializer.ComputeCanonicalSha256(first),
                    Is.EqualTo("26e1d490e0c0a1888e93768132217c169b8324ebb211beb7c2147a19188e858d"));
            }
        }

        [Test]
        public void BuildCanReadAReadOnlySyntheticFileWithoutModifyingIt()
        {
            using (var temporary = new TemporaryContentTestDirectory())
            {
                temporary.CreateDirectory("Repository");
                temporary.CreateDirectory("Cache");
                temporary.CreateDirectory("External");
                string filePath = temporary.WriteBytes(
                    "External/read-only.bin",
                    Encoding.ASCII.GetBytes("read only"));
                File.SetAttributes(filePath, File.GetAttributes(filePath) | FileAttributes.ReadOnly);
                DateTime lastWriteTime = File.GetLastWriteTimeUtc(filePath);

                ContentIndexResult result = new ContentIndexer().Build(
                    CreateConfiguration(temporary));

                Assert.That(result.HasErrors, Is.False);
                Assert.That(result.Sources[0].Files, Has.Count.EqualTo(1));
                Assert.That(
                    (File.GetAttributes(filePath) & FileAttributes.ReadOnly) != 0,
                    Is.True);
                Assert.That(File.GetLastWriteTimeUtc(filePath), Is.EqualTo(lastWriteTime));
            }
        }

        [Test]
        public void BuildReportsFileChangedDuringHashAndOmitsUnstableRecord()
        {
            using (var temporary = new TemporaryContentTestDirectory())
            {
                temporary.CreateDirectory("Repository");
                temporary.CreateDirectory("Cache");
                temporary.CreateDirectory("External");
                temporary.WriteBytes("External/changing.bin", new byte[] { 1, 2, 3 });

                ContentIndexResult result = new ContentIndexer(
                    new MutatingDigestProvider()).Build(CreateConfiguration(temporary));

                Assert.That(result.HasErrors, Is.True);
                Assert.That(result.IsComplete, Is.False);
                Assert.That(result.Sources[0].IsComplete, Is.False);
                Assert.That(result.Sources[0].Files, Is.Empty);
                Assert.That(
                    result.Diagnostics.Any(item =>
                        item.Code == ContentDiagnosticCode.FileChangedDuringHash &&
                        item.SourceId == "synthetic-source" &&
                        item.Path == "changing.bin"),
                    Is.True);
                Assert.Throws<InvalidOperationException>(
                    () => ContentManifestSerializer.SerializeCanonicalJson(result));
            }
        }

        [Test]
        public void BuildReportsTruncatedDigestReadAndContinuesWithSourceResult()
        {
            using (var temporary = new TemporaryContentTestDirectory())
            {
                temporary.CreateDirectory("Repository");
                temporary.CreateDirectory("Cache");
                temporary.CreateDirectory("External");
                temporary.WriteBytes("External/truncated.bin", new byte[] { 1, 2, 3 });

                ContentIndexResult result = new ContentIndexer(
                    new TruncatedDigestProvider()).Build(CreateConfiguration(temporary));

                Assert.That(result.HasErrors, Is.True);
                Assert.That(result.IsComplete, Is.False);
                Assert.That(result.Sources[0].IsComplete, Is.False);
                Assert.That(result.Sources[0].Files, Is.Empty);
                Assert.That(
                    result.Diagnostics.Any(item =>
                        item.Code == ContentDiagnosticCode.FileHashFailed &&
                        item.SourceId == "synthetic-source" &&
                        item.Path == "truncated.bin"),
                    Is.True);
                Assert.Throws<InvalidOperationException>(
                    () => ContentManifestSerializer.SerializeCanonicalJson(result));
            }
        }

        [Test]
        public void BuildRejectsAFileAddedDuringIndexing()
        {
            using (var temporary = new TemporaryContentTestDirectory())
            {
                temporary.CreateDirectory("Repository");
                temporary.CreateDirectory("Cache");
                temporary.CreateDirectory("External");
                temporary.WriteBytes("External/seed.bin", new byte[] { 1, 2, 3 });

                ContentIndexResult result = new ContentIndexer(
                    new SiblingCreatingDigestProvider()).Build(CreateConfiguration(temporary));

                Assert.That(result.HasErrors, Is.True);
                Assert.That(result.IsComplete, Is.False);
                Assert.That(result.Sources[0].IsComplete, Is.False);
                Assert.That(result.Diagnostics.Any(item =>
                    item.Code == ContentDiagnosticCode.SourceTreeChangedDuringIndex), Is.True);
                Assert.Throws<InvalidOperationException>(
                    () => ContentManifestSerializer.SerializeCanonicalJson(result));
            }
        }

        [Test]
        public void BuildRejectsAFileThatDisappearsDuringIndexing()
        {
            using (var temporary = new TemporaryContentTestDirectory())
            {
                temporary.CreateDirectory("Repository");
                temporary.CreateDirectory("Cache");
                temporary.CreateDirectory("External");
                temporary.WriteBytes("External/disappearing.bin", new byte[] { 1, 2, 3 });

                ContentIndexResult result = new ContentIndexer(
                    new DeletingDigestProvider()).Build(CreateConfiguration(temporary));

                Assert.That(result.HasErrors, Is.True);
                Assert.That(result.IsComplete, Is.False);
                Assert.That(result.Diagnostics.Any(item =>
                    item.Code == ContentDiagnosticCode.FileChangedDuringHash), Is.True);
                Assert.Throws<InvalidOperationException>(() =>
                    ContentManifestSerializer.SerializeCanonicalJson(result));
            }
        }

        [Test]
        public void ManifestIsCanonicalAndDoesNotContainAbsolutePathsOrFileContent()
        {
            using (var temporary = new TemporaryContentTestDirectory())
            {
                temporary.CreateDirectory("Repository");
                temporary.CreateDirectory("Cache");
                temporary.CreateDirectory("External");
                const string fileBody = "SYNTHETIC_BODY_MUST_NOT_APPEAR";
                temporary.WriteBytes(
                    "External/sample.bin",
                    Encoding.ASCII.GetBytes(fileBody));
                ContentIndexResult index = new ContentIndexer().Build(
                    CreateConfiguration(temporary));

                string first = ContentManifestSerializer.SerializeCanonicalJson(index);
                string second = ContentManifestSerializer.SerializeCanonicalJson(index);

                Assert.That(first, Is.EqualTo(second));
                Assert.That(first, Does.StartWith("{\"schemaVersion\":1,\"sources\":["));
                Assert.That(first, Does.Contain("\"id\":\"synthetic-source\""));
                Assert.That(first, Does.Contain("\"path\":\"sample.bin\""));
                Assert.That(first, Does.Contain("\"sha256\":"));
                Assert.That(first, Does.Not.Contain(temporary.GetPath("External")));
                Assert.That(first, Does.Not.Contain(fileBody));
                Assert.That(ContentManifestSerializer.ComputeCanonicalSha256(index), Has.Length.EqualTo(64));
            }
        }

        [Test]
        public void ManifestSha256ChangesWhenIndexedContentChanges()
        {
            using (var temporary = new TemporaryContentTestDirectory())
            {
                temporary.CreateDirectory("Repository");
                temporary.CreateDirectory("Cache");
                temporary.CreateDirectory("External");
                temporary.WriteBytes("External/sample.bin", Encoding.ASCII.GetBytes("first"));
                ExternalContentConfiguration configuration = CreateConfiguration(temporary);

                string firstHash = ContentManifestSerializer.ComputeCanonicalSha256(
                    new ContentIndexer().Build(configuration));
                temporary.WriteBytes("External/sample.bin", Encoding.ASCII.GetBytes("second-body"));
                string secondHash = ContentManifestSerializer.ComputeCanonicalSha256(
                    new ContentIndexer().Build(configuration));

                Assert.That(secondHash, Is.Not.EqualTo(firstHash));
            }
        }

        [TestCase("../escape.bin")]
        [TestCase("C:/absolute.bin")]
        [TestCase("folder\\file.bin")]
        public void FileRecordRejectsUnsafeManifestPaths(string path)
        {
            Assert.Throws<ArgumentException>(() => new ContentFileRecord(
                "source",
                path,
                0,
                new string('0', 64)));
        }

        [Test]
        public void FileRecordRejectsNegativeLengthAndInvalidDigest()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new ContentFileRecord(
                "source", "file.bin", -1, new string('0', 64)));
            Assert.Throws<ArgumentException>(() => new ContentFileRecord(
                "source", "file.bin", 0, "not-a-sha"));
        }

        [Test]
        public void ConfigurationModelRejectsUnsafeCacheAndDisabledOnlySources()
        {
            using (var temporary = new TemporaryContentTestDirectory())
            {
                string repository = temporary.CreateDirectory("Repository");
                string external = temporary.CreateDirectory("External");
                string cache = temporary.CreateDirectory("Cache");
                var enabledSource = new ExternalContentSourceDescriptor(
                    "enabled", ContentSourceKind.Unpacked, external, 1, "v1", true);
                var disabledSource = new ExternalContentSourceDescriptor(
                    "disabled", ContentSourceKind.Clean, external, 1, "v1", false);

                Assert.Throws<ArgumentException>(() => new ExternalContentConfiguration(
                    1,
                    temporary.GetPath("Repository/Config/ExternalContent.xml"),
                    repository,
                    temporary.GetPath("Repository/Cache"),
                    new[] { enabledSource }));
                Assert.Throws<ArgumentException>(() => new ExternalContentConfiguration(
                    1,
                    temporary.GetPath("Repository/Config/ExternalContent.xml"),
                    repository,
                    cache,
                    new[] { disabledSource }));
                Assert.Throws<ArgumentException>(() => new ExternalContentConfiguration(
                    1,
                    temporary.GetPath("Repository/Config/ExternalContent.xml"),
                    temporary.GetPath("MissingRepository"),
                    cache,
                    new[] { enabledSource }));
            }
        }

        [Test]
        public void BuildRejectsRepositoryRootRemovedAfterConfigurationCreation()
        {
            using (var temporary = new TemporaryContentTestDirectory())
            {
                string repository = temporary.CreateDirectory("Repository");
                temporary.CreateDirectory("Cache");
                temporary.CreateDirectory("External");
                ExternalContentConfiguration configuration = CreateConfiguration(temporary);
                Directory.Delete(repository, false);

                ContentIndexResult result = new ContentIndexer().Build(configuration);

                Assert.That(result.HasErrors, Is.True);
                Assert.That(result.IsComplete, Is.False);
                Assert.That(result.Sources, Is.Empty);
                Assert.That(result.Diagnostics.Any(item =>
                    item.Code == ContentDiagnosticCode.RepositoryRootNotDirectory &&
                    item.Path == repository), Is.True);
                Assert.Throws<InvalidOperationException>(
                    () => ContentManifestSerializer.SerializeCanonicalJson(result));
            }
        }

        [Test]
        public void NormalizeRejectsWindowsDriveRelativePaths()
        {
            if (Path.DirectorySeparatorChar != '\\')
            {
                Assert.Ignore("Windows drive-relative path syntax only applies on Windows.");
            }

            Assert.Throws<ArgumentException>(() =>
                RepositoryPathPolicy.NormalizeAbsolutePath(
                    "C:relative",
                    Directory.GetCurrentDirectory()));
            Assert.Throws<ArgumentException>(() =>
                RepositoryPathPolicy.NormalizeAbsolutePath(
                    "C:",
                    Directory.GetCurrentDirectory()));
        }

        [Test]
        public void ReparseInspectionPropagatesMetadataAccessFailure()
        {
            using (var temporary = new TemporaryContentTestDirectory())
            {
                string inaccessibleAncestor = temporary.CreateDirectory("External");
                string missingLeaf = temporary.GetPath("External/Missing/Leaf");

                Assert.Throws<UnauthorizedAccessException>(() =>
                    InspectWithSyntheticAccessFailure(
                        missingLeaf,
                        inaccessibleAncestor));
            }
        }

        [Test]
        public void ProductionManifestInputsCannotBeForgedThroughPublicConstructors()
        {
            Assert.That(typeof(IContentFileDigestProvider).IsPublic, Is.False);
            Assert.That(
                typeof(ContentIndexer).GetConstructors().Select(item => item.GetParameters().Length),
                Is.EqualTo(new[] { 0 }));
            Assert.That(typeof(ContentFileRecord).GetConstructors(), Is.Empty);
            Assert.That(typeof(ContentSourceIndex).GetConstructors(), Is.Empty);
            Assert.That(typeof(ContentIndexResult).GetConstructors(), Is.Empty);

            using (var temporary = new TemporaryContentTestDirectory())
            {
                string external = temporary.CreateDirectory("External");
                var source = new ExternalContentSourceDescriptor(
                    "source", ContentSourceKind.Unpacked, external, 1, "v1", true);
                var record = new ContentFileRecord(
                    "source", "file.bin", 0, new string('0', 64));

                Assert.Throws<ArgumentException>(() => new ContentSourceIndex(
                    source,
                    new[] { record },
                    new string('0', 64),
                    true));
            }
        }

        private static ExternalContentConfiguration CreateConfiguration(
            TemporaryContentTestDirectory temporary)
        {
            var source = new ExternalContentSourceDescriptor(
                "synthetic-source",
                ContentSourceKind.Unpacked,
                temporary.GetPath("External"),
                200,
                "synthetic-v1",
                true);
            return new ExternalContentConfiguration(
                1,
                temporary.GetPath("Repository/Config/ExternalContent.xml"),
                temporary.GetPath("Repository"),
                temporary.GetPath("Cache"),
                new[] { source });
        }

        private static void InspectWithSyntheticAccessFailure(
            string path,
            string inaccessiblePath)
        {
            StringComparison comparison = Path.DirectorySeparatorChar == '\\'
                ? StringComparison.OrdinalIgnoreCase
                : StringComparison.Ordinal;
            string ignored;
            RepositoryPathPolicy.ContainsExistingReparsePoint(
                path,
                currentPath =>
                {
                    if (string.Equals(currentPath, inaccessiblePath, comparison))
                    {
                        throw new UnauthorizedAccessException(
                            "Synthetic metadata access refusal.");
                    }

                    return File.GetAttributes(currentPath);
                },
                out ignored);
        }

        private sealed class MutatingDigestProvider : IContentFileDigestProvider
        {
            public string ComputeSha256(string absolutePath)
            {
                using (var stream = new FileStream(
                           absolutePath,
                           FileMode.Append,
                           FileAccess.Write,
                           FileShare.Read))
                {
                    stream.WriteByte(4);
                }

                return new string('0', 64);
            }
        }

        private sealed class TruncatedDigestProvider : IContentFileDigestProvider
        {
            public string ComputeSha256(string absolutePath)
            {
                throw new EndOfStreamException("Synthetic truncated digest stream.");
            }
        }

        private sealed class SiblingCreatingDigestProvider : IContentFileDigestProvider
        {
            public string ComputeSha256(string absolutePath)
            {
                File.WriteAllBytes(
                    Path.Combine(Path.GetDirectoryName(absolutePath), "added-during-index.bin"),
                    new byte[] { 4, 5, 6 });
                using (var stream = new FileStream(
                           absolutePath,
                           FileMode.Open,
                           FileAccess.Read,
                           FileShare.Read))
                using (SHA256 sha256 = SHA256.Create())
                {
                    return string.Concat(sha256.ComputeHash(stream).Select(value => value.ToString("x2")));
                }
            }
        }

        private sealed class DeletingDigestProvider : IContentFileDigestProvider
        {
            public string ComputeSha256(string absolutePath)
            {
                File.Delete(absolutePath);
                return new string('0', 64);
            }
        }
    }
}
