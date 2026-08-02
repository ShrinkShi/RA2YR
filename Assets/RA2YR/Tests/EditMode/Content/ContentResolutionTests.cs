using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using NUnit.Framework;
using RA2YR.Core.Content;

namespace RA2YR.Tests.EditMode.Content
{
    public sealed class ContentResolutionTests
    {
        [Test]
        public void SingleSourceResolvesEveryLogicalFile()
        {
            using (var temporary = new TemporaryContentTestDirectory())
            {
                ContentSourceIndex source = CreateSource(
                    temporary, "base", 100, "base-v1",
                    File("Rules/RULESMD.INI", "synthetic-rules"),
                    File("Maps/test.map", "synthetic-map"));

                ContentResolutionResult result = Resolve(source);

                Assert.That(result.IsComplete, Is.True);
                Assert.That(result.Entries, Has.Count.EqualTo(2));
                Assert.That(result.Entries.All(entry => entry.Selected.Source.Id == "base"), Is.True);
            }
        }

        [Test]
        public void HigherPriorityOverridesLowerPriority()
        {
            using (var temporary = new TemporaryContentTestDirectory())
            {
                ContentSourceIndex low = CreateSource(
                    temporary, "low", 100, "low-v1", File("rulesmd.ini", "low"));
                ContentSourceIndex high = CreateSource(
                    temporary, "high", 300, "high-v1", File("rulesmd.ini", "high"));

                ContentPathResolution entry = Resolve(low, high).Entries.Single();

                Assert.That(entry.Selected.Source.Id, Is.EqualTo("high"));
                Assert.That(entry.Selected.Source.Priority, Is.EqualTo(300));
                Assert.That(entry.OverriddenCandidates.Single().Source.Id, Is.EqualTo("low"));
            }
        }

        [Test]
        public void ThreeLayerOverrideRetainsCompleteProvenanceChain()
        {
            using (var temporary = new TemporaryContentTestDirectory())
            {
                ContentSourceIndex clean = CreateSource(
                    temporary, "clean", 100, "clean-v1", File("rulesmd.ini", "clean"));
                ContentSourceIndex unpacked = CreateSource(
                    temporary, "unpacked", 200, "unpacked-v1", File("RULESMD.ini", "unpacked"));
                ContentSourceIndex patched = CreateSource(
                    temporary, "patched", 300, "patched-v1", File("RulesMd.ini", "patched"));

                ContentPathResolution entry = Resolve(unpacked, clean, patched).Entries.Single();

                Assert.That(
                    entry.ProvenanceChain.Select(candidate => candidate.Source.Id),
                    Is.EqualTo(new[] { "patched", "unpacked", "clean" }));
                Assert.That(entry.LogicalPath.Value, Is.EqualTo("RulesMd.ini"));
            }
        }

        [Test]
        public void EqualHighestPriorityProducesExplicitAmbiguity()
        {
            using (var temporary = new TemporaryContentTestDirectory())
            {
                ContentResolutionResult result = Resolve(
                    CreateSource(temporary, "alpha", 300, "a", File("rulesmd.ini", "a")),
                    CreateSource(temporary, "beta", 300, "b", File("rulesmd.ini", "b")),
                    CreateSource(temporary, "low", 100, "l", File("rulesmd.ini", "l")));

                Assert.That(result.IsComplete, Is.False);
                Assert.That(result.HasErrors, Is.True);
                Assert.That(result.Entries.Single().Selected, Is.Null);
                Assert.That(result.Diagnostics.Any(diagnostic =>
                    diagnostic.Code == ContentDiagnosticCode.AmbiguousContentResolution), Is.True);
            }
        }

        [Test]
        public void SameSourceCaseCollisionIsRejected()
        {
            using (var temporary = new TemporaryContentTestDirectory())
            {
                ContentSourceIndex source = CreateSource(
                    temporary, "source", 100, "v1",
                    File("RULESMD.INI", "upper"),
                    File("rulesmd.ini", "lower"));

                ContentResolutionResult result = Resolve(source);

                Assert.That(result.IsComplete, Is.False);
                Assert.That(result.Entries.Single().Selected, Is.Null);
                Assert.That(result.Diagnostics.Any(diagnostic =>
                    diagnostic.Code == ContentDiagnosticCode.SourceLogicalPathConflict), Is.True);
            }
        }

        [Test]
        public void CrossSourceCaseVariantsResolveAsOneLogicalPath()
        {
            using (var temporary = new TemporaryContentTestDirectory())
            {
                ContentResolutionResult result = Resolve(
                    CreateSource(temporary, "base", 100, "base", File("RULESMD.INI", "base")),
                    CreateSource(temporary, "patch", 200, "patch", File("rulesmd.ini", "patch")));

                Assert.That(result.IsComplete, Is.True);
                Assert.That(result.Entries, Has.Count.EqualTo(1));
                Assert.That(result.Entries[0].LogicalPath.Value, Is.EqualTo("rulesmd.ini"));
                Assert.That(result.Entries[0].ProvenanceChain, Has.Count.EqualTo(2));
            }
        }

        [Test]
        public void SourceAndFileEnumerationOrderDoNotChangeManifest()
        {
            using (var temporary = new TemporaryContentTestDirectory())
            {
                SyntheticFile[] files =
                {
                    File("z/file.bin", "z"),
                    File("A/file.bin", "a"),
                    File("middle.bin", "m")
                };
                ContentSourceIndex firstA = CreateSource(
                    temporary, "base", 100, "base", files);
                ContentSourceIndex firstB = CreateSource(
                    temporary, "patch", 200, "patch", File("A/FILE.BIN", "patched"));
                ContentSourceIndex secondA = CreateSource(
                    temporary, "base", 100, "base", files.Reverse().ToArray());
                ContentSourceIndex secondB = CreateSource(
                    temporary, "patch", 200, "patch", File("A/FILE.BIN", "patched"));

                string first = ContentResolutionManifestSerializer.SerializeCanonicalJson(
                    Resolve(firstA, firstB));
                string second = ContentResolutionManifestSerializer.SerializeCanonicalJson(
                    Resolve(secondB, secondA));

                Assert.That(second, Is.EqualTo(first));
            }
        }

        [Test]
        public void DisabledSourceDoesNotParticipateInResolution()
        {
            using (var temporary = new TemporaryContentTestDirectory())
            {
                string repository = temporary.CreateDirectory("Repository");
                string cache = temporary.CreateDirectory("Cache");
                string enabledRoot = temporary.CreateDirectory("Enabled");
                string disabledRoot = temporary.CreateDirectory("Disabled");
                temporary.WriteText("Enabled/file.bin", "enabled");
                temporary.WriteText("Disabled/file.bin", "disabled");
                var enabled = new ExternalContentSourceDescriptor(
                    "enabled", ContentSourceKind.Unpacked, enabledRoot, 100, "v1", true);
                var disabled = new ExternalContentSourceDescriptor(
                    "disabled", ContentSourceKind.Overlay, disabledRoot, 999, "v1", false);
                var configuration = new ExternalContentConfiguration(
                    1,
                    temporary.GetPath("Repository/ExternalContent.xml"),
                    repository,
                    cache,
                    new[] { disabled, enabled });

                ContentResolutionResult result = new ContentResolver().Resolve(
                    new ContentIndexer().Build(configuration));

                Assert.That(result.IsComplete, Is.True);
                Assert.That(result.Sources.Select(source => source.Id), Is.EqualTo(new[] { "enabled" }));
                Assert.That(result.Entries.Single().Selected.Source.Id, Is.EqualTo("enabled"));
            }
        }

        [Test]
        public void PublicManifestContainsNoAbsolutePathOrFileBody()
        {
            using (var temporary = new TemporaryContentTestDirectory())
            {
                const string body = "SYNTHETIC_BODY_MUST_NOT_APPEAR";
                ContentResolutionResult result = Resolve(CreateSource(
                    temporary, "source", 100, "v1", File("folder/file.bin", body)));

                string manifest = ContentResolutionManifestSerializer.SerializeCanonicalJson(result);

                Assert.That(manifest, Does.Not.Contain(temporary.RootPath));
                Assert.That(manifest, Does.Not.Contain(body));
                Assert.That(manifest, Does.Contain("\"sourceRelativePath\":\"folder/file.bin\""));
                Assert.That(manifest, Does.Contain("\"provenance\":["));
            }
        }

        [Test]
        public void ProvenanceOrderIsStableIncludingLowerEqualPriorities()
        {
            using (var temporary = new TemporaryContentTestDirectory())
            {
                ContentPathResolution entry = Resolve(
                    CreateSource(temporary, "z-low", 100, "z", File("file.bin", "z")),
                    CreateSource(temporary, "winner", 300, "w", File("file.bin", "w")),
                    CreateSource(temporary, "a-low", 100, "a", File("file.bin", "a")))
                    .Entries.Single();

                Assert.That(
                    entry.ProvenanceChain.Select(candidate => candidate.Source.Id),
                    Is.EqualTo(new[] { "winner", "a-low", "z-low" }));
            }
        }

        [Test]
        public void SourceIdDoesNotBreakEqualPriorityTie()
        {
            using (var temporary = new TemporaryContentTestDirectory())
            {
                ContentResolutionResult first = Resolve(
                    CreateSource(temporary, "aaa", 50, "a", File("file.bin", "same")),
                    CreateSource(temporary, "zzz", 50, "z", File("file.bin", "same")));
                ContentResolutionResult second = Resolve(
                    CreateSource(temporary, "bbb", 50, "b", File("file.bin", "same")),
                    CreateSource(temporary, "ccc", 50, "c", File("file.bin", "same")));

                Assert.That(first.Entries.Single().Selected, Is.Null);
                Assert.That(second.Entries.Single().Selected, Is.Null);
                Assert.That(first.Diagnostics.Count(diagnostic =>
                    diagnostic.Code == ContentDiagnosticCode.AmbiguousContentResolution), Is.EqualTo(1));
                Assert.That(second.Diagnostics.Count(diagnostic =>
                    diagnostic.Code == ContentDiagnosticCode.AmbiguousContentResolution), Is.EqualTo(1));
            }
        }

        [Test]
        public void EmptyEnabledSourceResolvesToCompleteEmptyResult()
        {
            using (var temporary = new TemporaryContentTestDirectory())
            {
                ContentSourceIndex empty = CreateSource(
                    temporary, "empty", 100, "empty-v1");

                ContentResolutionResult result = Resolve(empty);

                Assert.That(result.IsComplete, Is.True);
                Assert.That(result.Entries, Is.Empty);
                Assert.DoesNotThrow(() =>
                    ContentResolutionManifestSerializer.SerializeCanonicalJson(result));
            }
        }

        [Test]
        public void NoIndexedSourceIsIncompleteAndCannotBeSerialized()
        {
            var index = new ContentIndexResult(
                Array.Empty<ContentSourceIndex>(),
                Array.Empty<ContentDiagnostic>());

            ContentResolutionResult result = new ContentResolver().Resolve(index);

            Assert.That(result.IsComplete, Is.False);
            Assert.That(result.Diagnostics.Any(diagnostic =>
                diagnostic.Code == ContentDiagnosticCode.ResolutionInputIncomplete), Is.True);
            Assert.Throws<InvalidOperationException>(() =>
                ContentResolutionManifestSerializer.SerializeCanonicalJson(result));
        }

        [Test]
        public void AllDisabledConfigurationIsRejectedBeforeIndexing()
        {
            using (var temporary = new TemporaryContentTestDirectory())
            {
                string repository = temporary.CreateDirectory("Repository");
                string cache = temporary.CreateDirectory("Cache");
                string external = temporary.CreateDirectory("External");
                var disabled = new ExternalContentSourceDescriptor(
                    "disabled", ContentSourceKind.Other, external, 100, "v1", false);

                Assert.Throws<ArgumentException>(() => new ExternalContentConfiguration(
                    1,
                    temporary.GetPath("Repository/config.xml"),
                    repository,
                    cache,
                    new[] { disabled }));
            }
        }

        [Test]
        public void AmbiguousAndSourceConflictResultsCannotBeSerialized()
        {
            using (var temporary = new TemporaryContentTestDirectory())
            {
                ContentResolutionResult ambiguous = Resolve(
                    CreateSource(temporary, "a", 1, "a", File("same.bin", "a")),
                    CreateSource(temporary, "b", 1, "b", File("same.bin", "b")));
                ContentResolutionResult conflicted = Resolve(CreateSource(
                    temporary, "c", 1, "c",
                    File("SAME.BIN", "a"), File("same.bin", "b")));

                Assert.Throws<InvalidOperationException>(() =>
                    ContentResolutionManifestSerializer.SerializeCanonicalJson(ambiguous));
                Assert.Throws<InvalidOperationException>(() =>
                    ContentResolutionManifestSerializer.SerializeCanonicalJson(conflicted));
            }
        }

        [Test]
        public void ProductionResolutionResultsCannotBeForgedWithPublicConstructors()
        {
            Assert.That(typeof(IContentSource).IsPublic, Is.False);
            Assert.That(typeof(DirectoryContentSource).IsPublic, Is.False);
            Assert.That(typeof(ContentResolutionSource).GetConstructors(), Is.Empty);
            Assert.That(typeof(ContentProvenanceCandidate).GetConstructors(), Is.Empty);
            Assert.That(typeof(ContentPathResolution).GetConstructors(), Is.Empty);
            Assert.That(typeof(ContentResolutionResult).GetConstructors(), Is.Empty);
        }

        private static ContentResolutionResult Resolve(params ContentSourceIndex[] sources)
        {
            return new ContentResolver().Resolve(new ContentIndexResult(
                sources,
                Array.Empty<ContentDiagnostic>()));
        }

        private static ContentSourceIndex CreateSource(
            TemporaryContentTestDirectory temporary,
            string id,
            int priority,
            string version,
            params SyntheticFile[] files)
        {
            string root = temporary.CreateDirectory("Sources/" + id);
            var descriptor = new ExternalContentSourceDescriptor(
                id,
                ContentSourceKind.Unpacked,
                root,
                priority,
                version,
                true);
            ContentFileRecord[] records = files.Select(file => new ContentFileRecord(
                id,
                file.Path,
                Encoding.UTF8.GetByteCount(file.Body),
                Sha256(file.Body))).ToArray();
            return new ContentSourceIndex(
                descriptor,
                records,
                ContentSourceFingerprint.Compute(descriptor, records),
                true);
        }

        private static SyntheticFile File(string path, string body)
        {
            return new SyntheticFile(path, body);
        }

        private static string Sha256(string value)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                return string.Concat(sha256
                    .ComputeHash(Encoding.UTF8.GetBytes(value))
                    .Select(item => item.ToString("x2")));
            }
        }

        private sealed class SyntheticFile
        {
            public SyntheticFile(string path, string body)
            {
                Path = path;
                Body = body;
            }

            public string Path { get; }

            public string Body { get; }
        }
    }
}
