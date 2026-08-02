using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using RA2YR.Core.Content;

namespace RA2YR.Tests.EditMode.Content
{
    public sealed class ExternalContentManifestWriterTests
    {
        [Test]
        public void WriteCreatesContentAddressedManifestOutsideRepository()
        {
            using (var temporary = new TemporaryContentTestDirectory())
            {
                ExternalContentConfiguration configuration = CreateConfiguration(temporary);
                temporary.WriteText("External/sample.bin", "synthetic");
                ContentResolutionResult resolution = new ContentResolver().Resolve(
                    new ContentIndexer().Build(configuration));

                ExternalManifestWriteResult result =
                    new ExternalContentManifestWriter().Write(
                        configuration,
                        resolution,
                        "baseline");

                string absoluteManifest = Path.Combine(
                    configuration.CachePath,
                    result.CacheRelativePath.Replace('/', Path.DirectorySeparatorChar));
                Assert.That(File.Exists(absoluteManifest), Is.True);
                Assert.That(result.SchemaVersion, Is.EqualTo(1));
                Assert.That(result.Sha256, Has.Length.EqualTo(64));
                Assert.That(result.CacheRelativePath, Does.Not.Contain(temporary.GetPath("Repository")));
                Assert.That(File.ReadAllText(absoluteManifest), Does.Not.Contain(configuration.RepositoryRoot));
                Assert.That(File.ReadAllText(absoluteManifest), Does.Not.Contain("synthetic"));
            }
        }

        [Test]
        public void RepeatedWriteIsIdempotentAndReturnsSameIdentity()
        {
            using (var temporary = new TemporaryContentTestDirectory())
            {
                ExternalContentConfiguration configuration = CreateConfiguration(temporary);
                temporary.WriteText("External/sample.bin", "synthetic");
                ContentResolutionResult resolution = new ContentResolver().Resolve(
                    new ContentIndexer().Build(configuration));
                var writer = new ExternalContentManifestWriter();

                ExternalManifestWriteResult first = writer.Write(
                    configuration, resolution, "baseline");
                ExternalManifestWriteResult second = writer.Write(
                    configuration, resolution, "baseline");

                Assert.That(second.Sha256, Is.EqualTo(first.Sha256));
                Assert.That(second.CacheRelativePath, Is.EqualTo(first.CacheRelativePath));
                Assert.That(second.Length, Is.EqualTo(first.Length));
            }
        }

        [Test]
        public void WriteRejectsResolutionFromDifferentSourceRootWithSameMetadata()
        {
            using (var temporary = new TemporaryContentTestDirectory())
            {
                ExternalContentConfiguration first = CreateConfiguration(temporary);
                temporary.WriteText("External/sample.bin", "synthetic");
                ContentResolutionResult resolution = new ContentResolver().Resolve(
                    new ContentIndexer().Build(first));
                string otherRoot = temporary.CreateDirectory("OtherExternal");
                var otherSource = new ExternalContentSourceDescriptor(
                    "baseline", ContentSourceKind.Patched, otherRoot, 300, "baseline-v1", true);
                var other = new ExternalContentConfiguration(
                    1,
                    temporary.GetPath("Repository/other.xml"),
                    temporary.GetPath("Repository"),
                    temporary.GetPath("OtherCache"),
                    new[] { otherSource });

                Assert.Throws<InvalidOperationException>(() =>
                    new ExternalContentManifestWriter().Write(other, resolution, "baseline"));
            }
        }

        [Test]
        public void ExistingContentAddressedFileWithDifferentBytesIsRejected()
        {
            using (var temporary = new TemporaryContentTestDirectory())
            {
                ExternalContentConfiguration configuration = CreateConfiguration(temporary);
                temporary.WriteText("External/sample.bin", "synthetic");
                ContentResolutionResult resolution = new ContentResolver().Resolve(
                    new ContentIndexer().Build(configuration));
                string hash = ContentResolutionManifestSerializer.ComputeCanonicalSha256(resolution);
                string directory = temporary.CreateDirectory("Cache/manifests/baseline");
                File.WriteAllText(Path.Combine(directory, hash + ".json"), "not-the-manifest");

                ContentManifestWriteException exception =
                    Assert.Throws<ContentManifestWriteException>(() =>
                        new ExternalContentManifestWriter().Write(
                            configuration, resolution, "baseline"));
                Assert.That(
                    exception.Diagnostic.Code,
                    Is.EqualTo(ContentDiagnosticCode.ContentManifestWriteFailed));
                Assert.That(exception.Diagnostic.Path, Is.EqualTo("manifests/baseline"));
            }
        }

        [Test]
        public void CacheDirectoryOccupiedByFileProducesStructuredDiagnostic()
        {
            using (var temporary = new TemporaryContentTestDirectory())
            {
                ExternalContentConfiguration configuration = CreateConfiguration(temporary);
                temporary.WriteText("External/sample.bin", "synthetic");
                temporary.CreateDirectory("Cache");
                temporary.WriteText("Cache/manifests", "occupied");
                ContentResolutionResult resolution = new ContentResolver().Resolve(
                    new ContentIndexer().Build(configuration));

                ContentManifestWriteException exception =
                    Assert.Throws<ContentManifestWriteException>(() =>
                        new ExternalContentManifestWriter().Write(
                            configuration, resolution, "baseline"));

                Assert.That(
                    exception.Diagnostic.Code,
                    Is.EqualTo(ContentDiagnosticCode.ContentManifestWriteFailed));
                Assert.That(exception.Diagnostic.SourceId, Is.EqualTo("baseline"));
                Assert.That(exception.Diagnostic.Path, Is.EqualTo("manifests/baseline"));
                Assert.That(exception.Diagnostic.Path, Does.Not.Contain(temporary.RootPath));
            }
        }

        [Test]
        public void PublicSummaryContainsOnlyAggregatesAndApprovedRepresentatives()
        {
            using (var temporary = new TemporaryContentTestDirectory())
            {
                ExternalContentConfiguration configuration = CreateConfiguration(temporary);
                const string body = "SYNTHETIC_BODY_MUST_NOT_APPEAR";
                temporary.WriteText("External/Sample.MIX", body);
                temporary.WriteText("External/other.txt", "other synthetic body");
                ContentResolutionResult resolution = new ContentResolver().Resolve(
                    new ContentIndexer().Build(configuration));
                ExternalManifestWriteResult manifest =
                    new ExternalContentManifestWriter().Write(
                        configuration, resolution, "baseline");
                DateTime started = new DateTime(2026, 8, 2, 1, 2, 3, DateTimeKind.Utc);
                DateTime completed = started.AddSeconds(1);

                ContentBaselineSummary summary = ContentBaselineSummaryBuilder.Build(
                    "baseline",
                    resolution,
                    manifest,
                    started,
                    completed,
                    new[] { "Sample.MIX" },
                    new[] { "Synthetic test source; no proprietary content." },
                    "Directory source only; archive contents were not parsed.");
                string json = ContentBaselineSummarySerializer.SerializeJson(summary);

                Assert.That(json, Does.Not.Contain(temporary.RootPath));
                Assert.That(json, Does.Not.Contain(body));
                Assert.That(json, Does.Contain("\"extension\":\".mix\""));
                Assert.That(json, Does.Contain("\"logicalPath\":\"Sample.MIX\""));
                Assert.That(json, Does.Not.Contain("other.txt\""));
                Assert.That(summary.TotalFileCount, Is.EqualTo(2));
                Assert.That(summary.ChangesDetected, Is.False);
            }
        }

        [Test]
        public void SummaryModelsCannotBeForgedWithPublicConstructors()
        {
            Assert.That(typeof(ContentBaselineSummary).GetConstructors(), Is.Empty);
            Assert.That(typeof(ContentRepresentativeFile).GetConstructors(), Is.Empty);
            Assert.That(typeof(ContentExtensionAggregate).GetConstructors(), Is.Empty);
            Assert.That(typeof(ExternalManifestWriteResult).GetConstructors(), Is.Empty);
        }

        private static ExternalContentConfiguration CreateConfiguration(
            TemporaryContentTestDirectory temporary)
        {
            string repository = temporary.CreateDirectory("Repository");
            string external = temporary.CreateDirectory("External");
            var source = new ExternalContentSourceDescriptor(
                "baseline", ContentSourceKind.Patched, external, 300, "baseline-v1", true);
            return new ExternalContentConfiguration(
                1,
                temporary.GetPath("Repository/ExternalContent.xml"),
                repository,
                temporary.GetPath("Cache"),
                new[] { source });
        }
    }
}
