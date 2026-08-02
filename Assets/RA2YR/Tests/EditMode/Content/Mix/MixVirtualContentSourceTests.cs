using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using NUnit.Framework;
using RA2YR.Core.Binary;
using RA2YR.Core.Binary.Seekable;
using RA2YR.Core.Content;
using RA2YR.Core.Content.Mix;
using RA2YR.Core.Formats.Mix;

namespace RA2YR.Tests.EditMode.Content.Mix
{
    public sealed class MixVirtualContentSourceTests
    {
        [Test]
        public void CoreReaderMountsNestedArchiveWithinParentWindows()
        {
            byte[] finalPayload = { 1, 2, 3, 4 };
            byte[] inner = BuildClassicMix(
                Entry("final.bin", finalPayload));
            byte[] outer = BuildClassicMix(
                Entry("inner.mix", inner));
            using (var fixture = Fixture.Create("source-a", 10, File("root.mix", outer)))
            using (MixVirtualContentMountResult mount = Mount(
                       fixture,
                       new[] { "inner.mix", "final.bin" },
                       MixMountIndexMode.StructureOnly))
            {
                Assert.That(mount.IsComplete, Is.True);
                Assert.That(mount.Archives.Count, Is.EqualTo(2));
                Assert.That(mount.Entries.Count, Is.EqualTo(2));
                Assert.That(
                    mount.Archives.All(archive =>
                        archive.HeaderKind == MixArchiveHeaderKind.Classic &&
                        archive.Flags == MixArchiveFlags.None &&
                        !archive.ChecksumVerified),
                    Is.True);

                MixVirtualEntry nestedArchive = mount.Entries.Single(
                    entry => entry.LogicalName.Value == "inner.mix");
                MixVirtualEntry final = mount.Entries.Single(
                    entry => entry.LogicalName.Value == "final.bin");
                Assert.That(nestedArchive.IsMountedArchive, Is.True);
                Assert.That(final.Provenance.Source.Id, Is.EqualTo("source-a"));
                Assert.That(final.Provenance.RootArchivePath.Value, Is.EqualTo("root.mix"));
                Assert.That(final.Provenance.Steps.Count, Is.EqualTo(2));
                Assert.That(final.Provenance.Steps[0].ArchivePath.Value, Is.EqualTo("root.mix"));
                Assert.That(
                    final.Provenance.Steps[1].ArchivePath.Value,
                    Is.EqualTo("root.mix/inner.mix"));
                Assert.That(final.HasSha256, Is.False);

                var observed = new byte[finalPayload.Length];
                final.PayloadWindow.ReadExactly(
                    0,
                    observed,
                    0,
                    observed.Length,
                    "test-payload");
                Assert.That(observed, Is.EqualTo(finalPayload));
            }
        }

        [Test]
        public void StructureOnlyMountDoesNotPrehashEntryPayloads()
        {
            byte[] payload = Enumerable.Range(0, 64).Select(value => (byte)value).ToArray();
            byte[] archive = BuildClassicMix(Entry("file.bin", payload));
            using (var fixture = Fixture.Create("structure", 1, File("root.mix", archive)))
            using (MixVirtualContentMountResult mount = Mount(
                       fixture,
                       new[] { "file.bin" },
                       MixMountIndexMode.StructureOnly))
            {
                Assert.That(mount.IsComplete, Is.True);
                Assert.That(mount.IndexMode, Is.EqualTo(MixMountIndexMode.StructureOnly));
                Assert.That(mount.Entries.Single().HasSha256, Is.False);
                Assert.Throws<InvalidOperationException>(() =>
                    MixContentManifestSerializer.SerializeMountCanonicalJson(mount));
            }
        }

        [Test]
        public void ManifestAuditHashesPayloadsWithoutPublishingBodiesOrPhysicalPaths()
        {
            byte[] payload = System.Text.Encoding.ASCII.GetBytes("PRIVATE-SYNTHETIC-BODY");
            byte[] archive = BuildClassicMix(Entry("file.bin", payload));
            using (var fixture = Fixture.Create("audit", 3, File("root.mix", archive)))
            using (MixVirtualContentMountResult mount = Mount(
                       fixture,
                       new[] { "file.bin" },
                       MixMountIndexMode.ManifestAudit))
            {
                MixVirtualEntry entry = mount.Entries.Single();
                Assert.That(entry.HasSha256, Is.True);
                Assert.That(entry.Sha256, Is.EqualTo(Sha256(payload)));

                string first = MixContentManifestSerializer.SerializeMountCanonicalJson(mount);
                string second = MixContentManifestSerializer.SerializeMountCanonicalJson(mount);
                Assert.That(first, Is.EqualTo(second));
                Assert.That(first, Does.Contain("\"logicalName\":\"file.bin\""));
                Assert.That(first, Does.Contain(entry.Id.ToString()));
                Assert.That(first, Does.Not.Contain(fixture.DirectoryPath));
                Assert.That(first, Does.Not.Contain("PRIVATE-SYNTHETIC-BODY"));
            }
        }

        [Test]
        public void UnknownNumericIdRemainsAccessibleWithoutInventedName()
        {
            MixFileId unknown = MixFileId.FromRaw(0xdeadbeefu);
            byte[] archive = BuildClassicMix(new SyntheticEntry(
                unknown,
                new byte[] { 9, 8, 7 }));
            using (var fixture = Fixture.Create("unknown", 1, File("root.mix", archive)))
            using (MixVirtualContentMountResult mount = Mount(
                       fixture,
                       Array.Empty<string>(),
                       MixMountIndexMode.ManifestAudit))
            {
                MixVirtualEntry entry = mount.FindById(unknown).Single();
                Assert.That(entry.HasResolvedName, Is.False);
                string manifest = MixContentManifestSerializer.SerializeMountCanonicalJson(mount);
                Assert.That(manifest, Does.Contain("\"id\":\"0xDEADBEEF\""));
                Assert.That(manifest, Does.Contain("\"logicalName\":null"));
            }
        }

        [Test]
        public void UnknownIdsPreventCompleteResolvedManifestClaims()
        {
            byte[] archive = BuildClassicMix(new SyntheticEntry(
                MixFileId.FromRaw(0xdeadbeefu),
                new byte[] { 1, 2, 3 }));
            using (var fixture = Fixture.Create(
                       "unknown-resolution",
                       1,
                       File("root.mix", archive)))
            using (MixVirtualContentMountResult mount = Mount(
                       fixture,
                       Array.Empty<string>(),
                       MixMountIndexMode.ManifestAudit))
            {
                var directory = new ContentIndexResult(
                    new[] { fixture.SourceIndex },
                    Array.Empty<ContentDiagnostic>());
                MixMountedContentResolutionResult result =
                    new MixMountedContentResolver().Resolve(directory, new[] { mount });

                Assert.That(result.UnresolvedMixEntryCount, Is.EqualTo(1));
                Assert.That(result.IsComplete, Is.False);
                Assert.That(result.HasAuditedDigests, Is.False);
                Assert.That(
                    result.Diagnostics.Single(diagnostic =>
                        diagnostic.Code ==
                        MixMountedResolutionDiagnosticCode.UnresolvedMixEntryIds),
                    Is.Not.Null);
                Assert.Throws<InvalidOperationException>(() =>
                    MixContentManifestSerializer.SerializeResolvedCanonicalJson(result));
            }
        }

        [Test]
        public void CatalogCandidateCaseVariantsResolveAsOneLogicalName()
        {
            var catalog = new MixNameCatalog(new[]
            {
                LogicalContentPath.Parse("Folder/File.bin"),
                LogicalContentPath.Parse("folder/file.BIN")
            });

            LogicalContentPath resolved;
            Assert.That(
                catalog.TryResolve(
                    MixFileId.ComputeCandidateId("folder/file.bin"),
                    out resolved),
                Is.True);
            Assert.That(catalog.AmbiguousIdCount, Is.Zero);
            Assert.That(resolved.Value, Is.EqualTo("Folder/File.bin"));
        }

        [Test]
        public void DistinctNamesSharingInjectedIdRemainUnnamedWithWarning()
        {
            MixFileId id = MixFileId.FromRaw(0x10203040u);
            var catalog = new MixNameCatalog(new[]
            {
                new MixNameCatalogCandidate(id, LogicalContentPath.Parse("one.bin")),
                new MixNameCatalogCandidate(id, LogicalContentPath.Parse("two.bin"))
            });
            byte[] archive = BuildClassicMix(new SyntheticEntry(id, new byte[] { 1 }));
            using (var fixture = Fixture.Create("collision", 1, File("root.mix", archive)))
            using (MixVirtualContentMountResult mount = MixVirtualContentSource.MountDirectorySource(
                       fixture.SourceIndex,
                       new[] { LogicalContentPath.Parse("root.mix") },
                       catalog,
                       MixArchiveCatalogAdapters.ReadWithCoreReader,
                       indexMode: MixMountIndexMode.StructureOnly))
            {
                Assert.That(mount.IsComplete, Is.True);
                Assert.That(mount.Entries.Single().HasResolvedName, Is.False);
                Assert.That(
                    mount.Diagnostics.Single().Code,
                    Is.EqualTo(MixMountDiagnosticCode.AmbiguousCandidateName));
                Assert.That(
                    mount.Diagnostics.Single().Severity,
                    Is.EqualTo(MixMountDiagnosticSeverity.Warning));
            }
        }

        [Test]
        public void EntryCrossingParentWindowFailsClosed()
        {
            byte[] bytes = new byte[16];
            using (var fixture = Fixture.Create("range", 1, File("root.mix", bytes)))
            using (MixVirtualContentMountResult mount = MixVirtualContentSource.MountDirectorySource(
                       fixture.SourceIndex,
                       new[] { LogicalContentPath.Parse("root.mix") },
                       new MixNameCatalog(Array.Empty<LogicalContentPath>()),
                       (window, source) => MixArchiveCatalog.Complete(new[]
                       {
                           new MixArchiveCatalogEntry(
                               MixFileId.FromRaw(1),
                               15,
                               2,
                               0)
                       }, MixArchiveHeaderKind.Classic, MixArchiveFlags.None, false)))
            {
                Assert.That(mount.IsComplete, Is.False);
                Assert.That(mount.Entries, Is.Empty);
                Assert.That(
                    mount.Diagnostics.Last().Code,
                    Is.EqualTo(MixMountDiagnosticCode.InvalidEntryRange));
            }
        }

        [Test]
        public void CoreReaderFailureRetainsStructuredFormatDiagnostic()
        {
            using (var fixture = Fixture.Create(
                       "format-error",
                       1,
                       File("root.mix", new byte[5])))
            using (MixVirtualContentMountResult mount = Mount(
                       fixture,
                       Array.Empty<string>(),
                       MixMountIndexMode.StructureOnly))
            {
                Assert.That(mount.IsComplete, Is.False);
                MixMountDiagnostic diagnostic = mount.Diagnostics.Single();
                Assert.That(diagnostic.Code, Is.EqualTo(MixMountDiagnosticCode.ArchiveIncomplete));
                Assert.That(diagnostic.FormatDiagnostic, Is.Not.Null);
                Assert.That(
                    diagnostic.FormatDiagnostic.Code,
                    Is.EqualTo(MixDiagnosticCode.TruncatedHeader));
                Assert.That(diagnostic.FormatDiagnostic.AbsoluteOffset, Is.EqualTo(0));
                Assert.That(diagnostic.FormatDiagnostic.EntryIndex, Is.EqualTo(-1));
            }
        }

        [Test]
        public void ArchiveEntryAndDepthBudgetsFailClosed()
        {
            byte[] inner = BuildClassicMix(Entry("final.bin", new byte[] { 1 }));
            byte[] outer = BuildClassicMix(Entry("inner.mix", inner));
            using (var fixture = Fixture.Create("budget", 1, File("root.mix", outer)))
            {
                using (MixVirtualContentMountResult archives = Mount(
                           fixture,
                           new[] { "inner.mix", "final.bin" },
                           MixMountIndexMode.StructureOnly,
                           Limits(maxArchives: 1)))
                {
                    Assert.That(archives.IsComplete, Is.False);
                    Assert.That(
                        archives.Diagnostics.Last().Code,
                        Is.EqualTo(MixMountDiagnosticCode.ArchiveLimitExceeded));
                }

                using (MixVirtualContentMountResult depth = Mount(
                           fixture,
                           new[] { "inner.mix", "final.bin" },
                           MixMountIndexMode.StructureOnly,
                           Limits(maxDepth: 0)))
                {
                    Assert.That(depth.IsComplete, Is.False);
                    Assert.That(
                        depth.Diagnostics.Last().Code,
                        Is.EqualTo(MixMountDiagnosticCode.NestingDepthExceeded));
                }

                using (MixVirtualContentMountResult entries = Mount(
                           fixture,
                           new[] { "inner.mix", "final.bin" },
                           MixMountIndexMode.StructureOnly,
                           Limits(maxEntries: 1)))
                {
                    Assert.That(entries.IsComplete, Is.False);
                    Assert.That(
                        entries.Diagnostics.Last().Code,
                        Is.EqualTo(MixMountDiagnosticCode.EntryLimitExceeded));
                }
            }
        }

        [Test]
        public void RepeatedPhysicalArchiveRangeCannotRecurseForever()
        {
            byte[] bytes = new byte[16];
            MixFileId selfId = MixFileId.ComputeCandidateId("self.mix");
            using (var fixture = Fixture.Create("cycle", 1, File("root.mix", bytes)))
            using (MixVirtualContentMountResult mount = MixVirtualContentSource.MountDirectorySource(
                       fixture.SourceIndex,
                       new[] { LogicalContentPath.Parse("root.mix") },
                       Names("self.mix"),
                       (window, source) => MixArchiveCatalog.Complete(new[]
                       {
                           new MixArchiveCatalogEntry(selfId, 0, window.Length, 0)
                       }, MixArchiveHeaderKind.Classic, MixArchiveFlags.None, false)))
            {
                Assert.That(mount.IsComplete, Is.False);
                Assert.That(
                    mount.Diagnostics.Last().Code,
                    Is.EqualTo(MixMountDiagnosticCode.RepeatedArchiveRange));
            }
        }

        [Test]
        public void ExternalSourcePrioritySelectsHighAndRetainsLowProvenance()
        {
            byte[] lowPayload = { 1 };
            byte[] highPayload = { 2 };
            using (var low = Fixture.Create(
                       "low",
                       10,
                       File("root.mix", BuildClassicMix(Entry("final.bin", lowPayload)))))
            using (var high = Fixture.Create(
                       "high",
                       20,
                       File("root.mix", BuildClassicMix(Entry("final.bin", highPayload)))))
            using (MixVirtualContentMountResult lowMount = Mount(
                       low,
                       new[] { "final.bin" },
                       MixMountIndexMode.ManifestAudit))
            using (MixVirtualContentMountResult highMount = Mount(
                       high,
                       new[] { "final.bin" },
                       MixMountIndexMode.ManifestAudit))
            {
                var directory = new ContentIndexResult(
                    new[] { low.SourceIndex, high.SourceIndex },
                    Array.Empty<ContentDiagnostic>());
                MixMountedContentResolutionResult result =
                    new MixMountedContentResolver().Resolve(
                        directory,
                        new[] { lowMount, highMount });

                MixMountedPathResolution final = result.Entries.Single(
                    entry => entry.LogicalPath.Value == "final.bin");
                Assert.That(final.Selected.Source.Id, Is.EqualTo("high"));
                Assert.That(final.ProvenanceChain.Count, Is.EqualTo(2));
                Assert.That(result.IsComplete, Is.True);
                Assert.That(result.HasAuditedDigests, Is.True);

                string manifest =
                    MixContentManifestSerializer.SerializeResolvedCanonicalJson(result);
                Assert.That(manifest, Does.Contain("\"selectedSourceId\":\"high\""));
                Assert.That(manifest, Does.Contain("\"disposition\":\"overridden\""));
                Assert.That(manifest, Does.Not.Contain(low.DirectoryPath));
                Assert.That(manifest, Does.Not.Contain(high.DirectoryPath));
            }
        }

        [Test]
        public void SameSourceLooseAndMixCandidatesRequireExplicitLayerPriority()
        {
            byte[] archive = BuildClassicMix(Entry("final.bin", new byte[] { 2 }));
            using (var fixture = Fixture.Create(
                       "same-source",
                       10,
                       File("root.mix", archive),
                       File("final.bin", new byte[] { 1 })))
            using (MixVirtualContentMountResult mount = Mount(
                       fixture,
                       new[] { "final.bin" },
                       MixMountIndexMode.ManifestAudit))
            {
                var directory = new ContentIndexResult(
                    new[] { fixture.SourceIndex },
                    Array.Empty<ContentDiagnostic>());
                MixMountedContentResolutionResult ambiguous =
                    new MixMountedContentResolver().Resolve(directory, new[] { mount });
                MixMountedPathResolution final = ambiguous.Entries.Single(
                    entry => entry.LogicalPath.Value == "final.bin");
                Assert.That(final.IsResolved, Is.False);
                Assert.That(
                    final.Diagnostics.Single().Code,
                    Is.EqualTo(MixMountedResolutionDiagnosticCode.MissingLayerPriority));

                var policy = new MixLayerPrecedencePolicy(new[]
                {
                    new MixLayerPrecedenceRule(
                        new MixContentLayerKey(
                            "SAME-SOURCE",
                            MixContentLayerKind.Directory,
                            LogicalContentPath.Parse("_directory")),
                        1),
                    new MixLayerPrecedenceRule(
                        new MixContentLayerKey(
                            "SAME-SOURCE",
                            MixContentLayerKind.MixArchive,
                            LogicalContentPath.Parse("root.mix")),
                        2)
                });
                MixMountedContentResolutionResult resolved =
                    new MixMountedContentResolver().Resolve(
                        directory,
                        new[] { mount },
                        policy);
                final = resolved.Entries.Single(
                    entry => entry.LogicalPath.Value == "final.bin");
                Assert.That(final.IsResolved, Is.True);
                Assert.That(final.Selected.Layer.Kind, Is.EqualTo(MixContentLayerKind.MixArchive));
                Assert.That(resolved.IsComplete, Is.True);
            }
        }

        [Test]
        public void NestedArchiveLayersCanReceiveDistinctExplicitPriorities()
        {
            byte[] inner = BuildClassicMix(Entry("same.bin", new byte[] { 2 }));
            byte[] outer = BuildClassicMix(
                Entry("inner.mix", inner),
                Entry("same.bin", new byte[] { 1 }));
            using (var fixture = Fixture.Create(
                       "nested-layers",
                       10,
                       File("root.mix", outer)))
            using (MixVirtualContentMountResult mount = Mount(
                       fixture,
                       new[] { "inner.mix", "same.bin" },
                       MixMountIndexMode.ManifestAudit))
            {
                var directory = new ContentIndexResult(
                    new[] { fixture.SourceIndex },
                    Array.Empty<ContentDiagnostic>());
                MixMountedContentResolutionResult ambiguous =
                    new MixMountedContentResolver().Resolve(directory, new[] { mount });
                MixMountedPathResolution same = ambiguous.Entries.Single(
                    entry => entry.LogicalPath.Value == "same.bin");
                Assert.That(same.IsResolved, Is.False);

                var policy = new MixLayerPrecedencePolicy(new[]
                {
                    new MixLayerPrecedenceRule(
                        new MixContentLayerKey(
                            "nested-layers",
                            MixContentLayerKind.MixArchive,
                            LogicalContentPath.Parse("root.mix")),
                        1),
                    new MixLayerPrecedenceRule(
                        new MixContentLayerKey(
                            "NESTED-LAYERS",
                            MixContentLayerKind.MixArchive,
                            LogicalContentPath.Parse("root.mix/inner.mix")),
                        2)
                });
                MixMountedContentResolutionResult resolved =
                    new MixMountedContentResolver().Resolve(
                        directory,
                        new[] { mount },
                        policy);
                same = resolved.Entries.Single(
                    entry => entry.LogicalPath.Value == "same.bin");
                Assert.That(same.IsResolved, Is.True);
                Assert.That(
                    same.Selected.Layer.LayerPath.Value,
                    Is.EqualTo("root.mix/inner.mix"));
                Assert.That(same.Selected.Sha256, Is.EqualTo(Sha256(new byte[] { 2 })));
            }
        }

        [Test]
        public void EqualHighestExternalPrioritiesStayAmbiguousRegardlessOfSourceId()
        {
            using (var first = Fixture.Create(
                       "aaa",
                       10,
                       File("root.mix", BuildClassicMix(Entry("final.bin", new byte[] { 1 })))))
            using (var second = Fixture.Create(
                       "zzz",
                       10,
                       File("root.mix", BuildClassicMix(Entry("final.bin", new byte[] { 2 })))))
            using (MixVirtualContentMountResult firstMount = Mount(
                       first,
                       new[] { "final.bin" },
                       MixMountIndexMode.StructureOnly))
            using (MixVirtualContentMountResult secondMount = Mount(
                       second,
                       new[] { "final.bin" },
                       MixMountIndexMode.StructureOnly))
            {
                var directory = new ContentIndexResult(
                    new[] { first.SourceIndex, second.SourceIndex },
                    Array.Empty<ContentDiagnostic>());
                MixMountedContentResolutionResult result =
                    new MixMountedContentResolver().Resolve(
                        directory,
                        new[] { firstMount, secondMount });
                MixMountedPathResolution final = result.Entries.Single(
                    entry => entry.LogicalPath.Value == "final.bin");
                Assert.That(final.Selected, Is.Null);
                Assert.That(
                    final.Diagnostics.Single().Code,
                    Is.EqualTo(
                        MixMountedResolutionDiagnosticCode.AmbiguousExternalSourcePriority));
            }
        }

        [Test]
        public void DisposingMountInvalidatesEntryWindows()
        {
            byte[] archive = BuildClassicMix(Entry("file.bin", new byte[] { 1 }));
            using (var fixture = Fixture.Create("dispose", 1, File("root.mix", archive)))
            {
                MixVirtualContentMountResult mount = Mount(
                    fixture,
                    new[] { "file.bin" },
                    MixMountIndexMode.StructureOnly);
                MixVirtualEntry entry = mount.Entries.Single();
                mount.Dispose();

                Assert.Throws<ObjectDisposedException>(() => entry.PayloadWindow.ReadExactly(
                    0,
                    new byte[1],
                    0,
                    1,
                    "disposed-payload"));
                Assert.Throws<ObjectDisposedException>(() => mount.FindById(entry.Id));
            }
        }

        [Test]
        public void FailedPostOpenValidationReleasesLocalFileHandleBeforeReturn()
        {
            byte[] archive = BuildClassicMix(Entry("file.bin", new byte[] { 1 }));
            using (var fixture = Fixture.Create(
                       "open-failure",
                       1,
                       File("root.mix", archive)))
            using (MixVirtualContentMountResult mount =
                   MixVirtualContentSource.MountDirectorySource(
                       fixture.SourceIndex,
                       new[] { LogicalContentPath.Parse("root.mix") },
                       Names("file.bin"),
                       MixArchiveCatalogAdapters.ReadWithCoreReader,
                       indexMode: MixMountIndexMode.StructureOnly,
                       postOpenValidationHook: () =>
                           throw new IOException("Synthetic post-open failure.")))
            {
                Assert.That(mount.IsComplete, Is.False);
                string physicalPath = Path.Combine(fixture.DirectoryPath, "root.mix");
                using (FileStream exclusive = System.IO.File.Open(
                           physicalPath,
                           FileMode.Open,
                           FileAccess.ReadWrite,
                           FileShare.None))
                {
                    Assert.That(exclusive.Length, Is.EqualTo(archive.Length));
                }
            }
        }

        [Test]
        public void DisposeContinuesAfterFailureAndCanRetryOnlyPendingSession()
        {
            using (var fixture = Fixture.Create(
                       "dispose-retry",
                       1,
                       File("placeholder.bin", new byte[] { 1 })))
            {
                var normalStream = new DisposalTestStream(0);
                var retryStream = new DisposalTestStream(1);
                ReadOnlyDataWindowSession normal = WindowSession(normalStream);
                ReadOnlyDataWindowSession retry = WindowSession(retryStream);
                var mount = new MixVirtualContentMountResult(
                    fixture.SourceIndex.Source,
                    Array.Empty<MixMountedArchive>(),
                    Array.Empty<MixVirtualEntry>(),
                    Array.Empty<MixMountDiagnostic>(),
                    new[] { normal, retry },
                    MixMountIndexMode.StructureOnly,
                    true);

                Assert.Throws<IOException>(() => mount.Dispose());
                Assert.That(retryStream.DisposeCallCount, Is.EqualTo(1));
                Assert.That(normalStream.DisposeCallCount, Is.EqualTo(1));
                Assert.Throws<ObjectDisposedException>(() =>
                    mount.FindById(MixFileId.FromRaw(1)));

                Assert.DoesNotThrow(() => mount.Dispose());
                Assert.That(retryStream.DisposeCallCount, Is.EqualTo(2));
                Assert.That(normalStream.DisposeCallCount, Is.EqualTo(1));
                Assert.DoesNotThrow(() => mount.Dispose());
            }
        }

        [Test]
        public void CrossThreadDisposeRejectsBeforeTouchingAnySession()
        {
            using (var fixture = Fixture.Create(
                       "dispose-thread",
                       1,
                       File("placeholder.bin", new byte[] { 1 })))
            {
                var firstStream = new DisposalTestStream(0);
                var secondStream = new DisposalTestStream(0);
                var mount = new MixVirtualContentMountResult(
                    fixture.SourceIndex.Source,
                    Array.Empty<MixMountedArchive>(),
                    Array.Empty<MixVirtualEntry>(),
                    Array.Empty<MixMountDiagnostic>(),
                    new[]
                    {
                        WindowSession(firstStream),
                        WindowSession(secondStream)
                    },
                    MixMountIndexMode.StructureOnly,
                    true);
                Exception observed = null;
                var thread = new Thread(() =>
                {
                    try
                    {
                        mount.Dispose();
                    }
                    catch (Exception exception)
                    {
                        observed = exception;
                    }
                });
                thread.Start();
                Assert.That(thread.Join(5000), Is.True);

                Assert.That(observed, Is.TypeOf<InvalidOperationException>());
                Assert.That(firstStream.DisposeCallCount, Is.Zero);
                Assert.That(secondStream.DisposeCallCount, Is.Zero);
                Assert.DoesNotThrow(() => mount.FindById(MixFileId.FromRaw(1)));

                mount.Dispose();
                Assert.That(firstStream.DisposeCallCount, Is.EqualTo(1));
                Assert.That(secondStream.DisposeCallCount, Is.EqualTo(1));
            }
        }

        private static MixVirtualContentMountResult Mount(
            Fixture fixture,
            IEnumerable<string> names,
            MixMountIndexMode mode,
            MixMountLimits limits = null)
        {
            return MixVirtualContentSource.MountDirectorySource(
                fixture.SourceIndex,
                new[] { LogicalContentPath.Parse("root.mix") },
                Names(names.ToArray()),
                MixArchiveCatalogAdapters.ReadWithCoreReader,
                limits,
                mode);
        }

        private static MixNameCatalog Names(params string[] values)
        {
            return new MixNameCatalog(values.Select(LogicalContentPath.Parse));
        }

        private static MixMountLimits Limits(
            int maxDepth = 16,
            long maxArchives = 1024,
            long maxEntries = 1000)
        {
            return new MixMountLimits(
                maxDepth,
                maxArchives,
                maxEntries,
                new ReadOnlyDataWindowLimits(
                    1024 * 1024,
                    1024 * 1024,
                    16 * 1024 * 1024,
                    1000,
                    32));
        }

        private static ReadOnlyDataWindowSession WindowSession(Stream stream)
        {
            return ReadOnlyDataWindowSession.FromSeekableStream(
                stream,
                new BinarySourceContext(
                    "format.seekable-window",
                    "synthetic-disposal",
                    LogicalContentPath.Parse("synthetic/disposal.bin")),
                0,
                stream.Length,
                new ReadOnlyDataWindowLimits(1024, 1024, 2048, 16, 4),
                false);
        }

        private static SyntheticEntry Entry(string name, byte[] payload)
        {
            return new SyntheticEntry(MixFileId.ComputeCandidateId(name), payload);
        }

        private static SyntheticFile File(string logicalPath, byte[] bytes)
        {
            return new SyntheticFile(logicalPath, bytes);
        }

        private static byte[] BuildClassicMix(params SyntheticEntry[] entries)
        {
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream))
            {
                writer.Write(checked((ushort)entries.Length));
                uint totalLength = 0;
                foreach (SyntheticEntry entry in entries)
                {
                    totalLength = checked(totalLength + (uint)entry.Payload.Length);
                }

                writer.Write(totalLength);
                uint offset = 0;
                foreach (SyntheticEntry entry in entries)
                {
                    writer.Write(entry.Id.Value);
                    writer.Write(offset);
                    writer.Write(checked((uint)entry.Payload.Length));
                    offset = checked(offset + (uint)entry.Payload.Length);
                }

                foreach (SyntheticEntry entry in entries)
                {
                    writer.Write(entry.Payload);
                }

                return stream.ToArray();
            }
        }

        private static string Sha256(byte[] bytes)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                return string.Concat(sha256.ComputeHash(bytes).Select(
                    value => value.ToString("x2")));
            }
        }

        private sealed class Fixture : IDisposable
        {
            private Fixture(
                string directoryPath,
                ContentSourceIndex sourceIndex)
            {
                DirectoryPath = directoryPath;
                SourceIndex = sourceIndex;
            }

            public string DirectoryPath { get; }

            public ContentSourceIndex SourceIndex { get; }

            public static Fixture Create(
                string sourceId,
                int priority,
                params SyntheticFile[] files)
            {
                string directory = Path.Combine(
                    Path.GetTempPath(),
                    "RA2YR-MixMount-" + Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(directory);
                var descriptor = new ExternalContentSourceDescriptor(
                    sourceId,
                    ContentSourceKind.Patched,
                    directory,
                    priority,
                    "synthetic-1",
                    true);
                var records = new List<ContentFileRecord>();
                foreach (SyntheticFile file in files)
                {
                    string physical = Path.Combine(
                        directory,
                        file.LogicalPath.Replace('/', Path.DirectorySeparatorChar));
                    string parent = Path.GetDirectoryName(physical);
                    if (!string.IsNullOrEmpty(parent))
                    {
                        Directory.CreateDirectory(parent);
                    }

                    System.IO.File.WriteAllBytes(physical, file.Bytes);
                    records.Add(new ContentFileRecord(
                        sourceId,
                        LogicalContentPath.Parse(file.LogicalPath),
                        file.Bytes.Length,
                        Sha256(file.Bytes)));
                }

                string fingerprint = ContentSourceFingerprint.Compute(descriptor, records);
                return new Fixture(
                    directory,
                    new ContentSourceIndex(descriptor, records, fingerprint, true));
            }

            public void Dispose()
            {
                if (Directory.Exists(DirectoryPath))
                {
                    Directory.Delete(DirectoryPath, true);
                }
            }
        }

        private sealed class SyntheticEntry
        {
            public SyntheticEntry(MixFileId id, byte[] payload)
            {
                Id = id;
                Payload = payload;
            }

            public MixFileId Id { get; }

            public byte[] Payload { get; }
        }

        private sealed class SyntheticFile
        {
            public SyntheticFile(string logicalPath, byte[] bytes)
            {
                LogicalPath = logicalPath;
                Bytes = bytes;
            }

            public string LogicalPath { get; }

            public byte[] Bytes { get; }
        }

        private sealed class DisposalTestStream : MemoryStream
        {
            private int failuresRemaining;

            public DisposalTestStream(int failuresRemaining)
                : base(new byte[] { 1 }, false)
            {
                this.failuresRemaining = failuresRemaining;
            }

            public int DisposeCallCount { get; private set; }

            protected override void Dispose(bool disposing)
            {
                DisposeCallCount++;
                if (failuresRemaining > 0)
                {
                    failuresRemaining--;
                    throw new IOException("Synthetic disposal failure.");
                }

                base.Dispose(disposing);
            }
        }
    }
}
