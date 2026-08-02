using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using RA2YR.Core.Binary;
using RA2YR.Core.Content;
using RA2YR.Core.Formats.Mix.Writing;

namespace RA2YR.Core.Formats.Mix.Interop
{
    public sealed class XccSyntheticInteropService
    {
        private const string InteropRootRelative = "wp02c/xcc-interop";
        private const string ExpectedManifestRelative = "manifests/expected.json";
        private const string XccIncomingRelative = "incoming-from-xcc/xcc-created.mix";
        private const string XccVerificationRelative =
            "verified/xcc-created/xcc-created-preserve.mix";
        private const string XccVerificationManifestRelative =
            "verified/xcc-created/verification.json";
        private const string ExtractionVerificationManifestRelative =
            "verified/staged-extractions/verification.json";
        private const string LocalMixDatabaseName = "local mix database.dat";
        private const long MaximumInteropArchiveBytes = 16L * 1024 * 1024;
        private const int MaximumInteropEntries = 128;
        private const int MaximumExtractionEntriesPerGroup = 16;

        private static readonly byte[] SyntheticKeySource = CreateSyntheticKeySource();

        private static readonly MixFileId LocalMixDatabaseId =
            MixFileId.ComputeCandidateId(LocalMixDatabaseName);

        private static readonly SyntheticPayloadSpec[] Ra2YrPayloads =
        {
            new SyntheticPayloadSpec(
                "alpha.synthetic.bin",
                Encoding.ASCII.GetBytes("RA2YR-WP02C-SYNTHETIC-ALPHA-V1\r\n")),
            new SyntheticPayloadSpec("empty.synthetic.bin", Array.Empty<byte>()),
            new SyntheticPayloadSpec("omega.synthetic.bin", CreatePattern(257, 73, 19))
        };

        private static readonly SyntheticPayloadSpec[] XccPayloads =
        {
            new SyntheticPayloadSpec(
                "xcc-alpha.synthetic.bin",
                Encoding.ASCII.GetBytes("RA2YR-WP02C-XCC-SYNTHETIC-ALPHA-V1\r\n")),
            new SyntheticPayloadSpec("xcc-small.synthetic.bin", new byte[] { 0x5a }),
            new SyntheticPayloadSpec("xcc-omega.synthetic.bin", CreatePattern(193, 29, 7))
        };

        private static readonly SyntheticPayloadSpec[] InnerPayloads =
        {
            new SyntheticPayloadSpec(
                "inner-note.synthetic.bin",
                Encoding.ASCII.GetBytes("RA2YR-WP02C-SYNTHETIC-INNER-V1\r\n"))
        };

        private static readonly SyntheticPayloadSpec OuterNotePayload =
            new SyntheticPayloadSpec(
                "outer-note.synthetic.bin",
                Encoding.ASCII.GetBytes("RA2YR-WP02C-SYNTHETIC-OUTER-V1\r\n"));

        public XccSyntheticInteropResult PrepareInternalContract(
            ExternalContentConfiguration configuration,
            string caseId)
        {
            OperationContext context;
            XccSyntheticInteropResult validation = TryCreateContext(
                configuration,
                caseId,
                XccSyntheticInteropStage.PrepareInternalContract,
                false,
                out context);
            if (validation != null)
            {
                return validation;
            }

            string stagingPath = null;
            string publishedPath = null;
            try
            {
                EnsureDirectoryTree(context.CacheRoot, context.InteropRoot);
                if (Directory.Exists(context.CaseRoot) || File.Exists(context.CaseRoot))
                {
                    return Failure(
                        XccSyntheticInteropStage.PrepareInternalContract,
                        caseId,
                        XccSyntheticInteropDiagnosticCode.CaseAlreadyExists,
                        "The fixed synthetic interop case already exists.",
                        context.CaseRelativePath);
                }

                stagingPath = Path.Combine(
                    context.InteropRoot,
                    "." + caseId + "." + Guid.NewGuid().ToString("N") + ".ra2yr-stage");
                Directory.CreateDirectory(stagingPath);
                EnsureSafeExistingTree(stagingPath);

                PrepareLayout layout = PopulatePreparedCase(stagingPath);
                string manifest = SerializeExpectedManifest(caseId, layout);
                byte[] manifestBytes = Encoding.UTF8.GetBytes(manifest);
                string manifestPath = CombineRelative(stagingPath, ExpectedManifestRelative);
                WriteNewFileFlushed(manifestPath, manifestBytes);

                EnsureSafeExistingTree(stagingPath);
                Directory.Move(stagingPath, context.CaseRoot);
                stagingPath = null;
                publishedPath = context.CaseRoot;
                EnsureSafeExistingTree(context.CaseRoot);
                foreach (ArchiveOutput archive in layout.Archives)
                {
                    layout.Artifacts.Add(VerifyPublishedArtifact(
                        context,
                        archive.RelativePath,
                        "synthetic-mix-" + archive.Role,
                        archive.Bytes.LongLength,
                        archive.Sha256));
                }

                layout.Artifacts.Add(VerifyPublishedArtifact(
                    context,
                    ExpectedManifestRelative,
                    "synthetic-expected-manifest",
                    manifestBytes.LongLength,
                    HashBytes(manifestBytes)));
                XccSyntheticInteropResult success = XccSyntheticInteropResult.Success(
                    XccSyntheticInteropStage.PrepareInternalContract,
                    caseId,
                    layout.Artifacts);
                publishedPath = null;
                return success;
            }
            catch (InteropFailure exception)
            {
                return FailureAfterCleanup(
                    XccSyntheticInteropStage.PrepareInternalContract,
                    caseId,
                    exception.Code,
                    exception.PublicMessage,
                    exception.CacheRelativePath,
                    new CleanupTarget(stagingPath, context.InteropRoot, true),
                    new CleanupTarget(publishedPath, context.InteropRoot, false));
            }
            catch (Exception exception) when (IsExpectedFileException(exception))
            {
                return FailureAfterCleanup(
                    XccSyntheticInteropStage.PrepareInternalContract,
                    caseId,
                    XccSyntheticInteropDiagnosticCode.AtomicPublishFailed,
                    "The synthetic interop case could not be published atomically.",
                    context.CaseRelativePath,
                    new CleanupTarget(stagingPath, context.InteropRoot, true),
                    new CleanupTarget(publishedPath, context.InteropRoot, false));
            }
        }

        public XccSyntheticInteropResult ValidateStagedCreatedCandidate(
            ExternalContentConfiguration configuration,
            string caseId)
        {
            OperationContext context;
            XccSyntheticInteropResult validation = TryCreateContext(
                configuration,
                caseId,
                XccSyntheticInteropStage.ValidateStagedCreatedCandidate,
                true,
                out context);
            if (validation != null)
            {
                return validation;
            }

            string incomingPath = CombineRelative(context.CaseRoot, XccIncomingRelative);
            XccSyntheticInteropResult inputFailure = ValidateFixedInputFile(
                context,
                incomingPath,
                XccIncomingRelative,
                XccSyntheticInteropStage.ValidateStagedCreatedCandidate);
            if (inputFailure != null)
            {
                return inputFailure;
            }

            string finalVerificationRoot = Path.Combine(
                context.CaseRoot,
                "verified",
                "xcc-created");
            if (Directory.Exists(finalVerificationRoot) || File.Exists(finalVerificationRoot))
            {
                return Failure(
                    XccSyntheticInteropStage.ValidateStagedCreatedCandidate,
                    caseId,
                    XccSyntheticInteropDiagnosticCode.CaseAlreadyExists,
                    "The XCC-created verification result already exists.",
                    context.CaseRelativePath + "/verified/xcc-created");
            }

            string stagingPath = null;
            string publishedPath = null;
            try
            {
                ObservedArchive incoming = ReadObservedArchive(
                    incomingPath,
                    "incoming-from-xcc/xcc-created.mix");
                ObservedEntry localMixDatabase = ValidateStagedCreatedEntries(
                    incoming,
                    XccIncomingRelative);

                stagingPath = Path.Combine(
                    context.CaseRoot,
                    "." + Guid.NewGuid().ToString("N") + ".ra2yr-stage");
                Directory.CreateDirectory(stagingPath);
                EnsureSafeExistingTree(stagingPath);

                MixWriteOptions options = new MixWriteOptions(
                    MixWriteOrder.PreserveEntryOrder,
                    incoming.HeaderKind == MixArchiveHeaderKind.Classic
                        ? MixWriteHeaderKind.Classic
                        : MixWriteHeaderKind.Extended,
                    incoming.HasChecksum,
                    incoming.IsEncrypted ? incoming.KeySource : null,
                    MaximumInteropEntries,
                    MaximumInteropArchiveBytes);
                MixWriteResult rebuild = MixArchiveWriter.WriteToFile(
                    incoming.Entries.Select(entry =>
                            new MixWriteEntry(entry.Id, entry.Payload))
                        .ToArray(),
                    options,
                    Path.Combine(stagingPath, "xcc-created-preserve.mix"),
                    stagingPath,
                    MixOutputPurpose.Cache,
                    false);
                if (!rebuild.IsSuccess)
                {
                    throw new InteropFailure(
                        XccSyntheticInteropDiagnosticCode.ArchiveBuildFailed,
                        "The XCC-created MIX could not be rebuilt in observed entry order.",
                        context.CaseRelativePath + "/" + XccVerificationRelative);
                }

                string verification = SerializeXccCreatedVerification(
                    caseId,
                    incoming,
                    localMixDatabase,
                    rebuild);
                byte[] verificationBytes = Encoding.UTF8.GetBytes(verification);
                WriteNewFileFlushed(
                    Path.Combine(stagingPath, "verification.json"),
                    verificationBytes);
                EnsureSafeExistingTree(stagingPath);
                Directory.Move(stagingPath, finalVerificationRoot);
                stagingPath = null;
                publishedPath = finalVerificationRoot;
                EnsureSafeExistingTree(finalVerificationRoot);

                var artifacts = new[]
                {
                    VerifyPublishedArtifact(
                        context,
                        XccIncomingRelative,
                        "staged-created-candidate-input",
                        incoming.Length,
                        incoming.Sha256),
                    VerifyPublishedArtifact(
                        context,
                        XccVerificationRelative,
                        "preserve-entry-order-rebuild",
                        rebuild.ArchiveSize,
                        rebuild.Sha256.ToLowerInvariant()),
                    VerifyPublishedArtifact(
                        context,
                        XccVerificationManifestRelative,
                        "staged-created-validation-manifest",
                        verificationBytes.LongLength,
                        HashBytes(verificationBytes))
                };
                XccSyntheticInteropResult success = XccSyntheticInteropResult.Success(
                    XccSyntheticInteropStage.ValidateStagedCreatedCandidate,
                    caseId,
                    artifacts);
                publishedPath = null;
                return success;
            }
            catch (InteropFailure exception)
            {
                return FailureAfterCleanup(
                    XccSyntheticInteropStage.ValidateStagedCreatedCandidate,
                    caseId,
                    exception.Code,
                    exception.PublicMessage,
                    exception.CacheRelativePath,
                    new CleanupTarget(stagingPath, context.CaseRoot, true),
                    new CleanupTarget(publishedPath, context.CaseRoot, false));
            }
            catch (Exception exception) when (IsExpectedFileException(exception))
            {
                return FailureAfterCleanup(
                    XccSyntheticInteropStage.ValidateStagedCreatedCandidate,
                    caseId,
                    XccSyntheticInteropDiagnosticCode.AtomicPublishFailed,
                    "The XCC-created verification could not be published atomically.",
                    context.CaseRelativePath + "/verified/xcc-created",
                    new CleanupTarget(stagingPath, context.CaseRoot, true),
                    new CleanupTarget(publishedPath, context.CaseRoot, false));
            }
        }

        public XccSyntheticInteropResult ValidateStagedExtractionCandidates(
            ExternalContentConfiguration configuration,
            string caseId)
        {
            OperationContext context;
            XccSyntheticInteropResult validation = TryCreateContext(
                configuration,
                caseId,
                XccSyntheticInteropStage.ValidateStagedExtractionCandidates,
                true,
                out context);
            if (validation != null)
            {
                return validation;
            }

            string rebuildPath = CombineRelative(context.CaseRoot, XccVerificationRelative);
            XccSyntheticInteropResult rebuildFailure = ValidateFixedInputFile(
                context,
                rebuildPath,
                XccVerificationRelative,
                XccSyntheticInteropStage.ValidateStagedExtractionCandidates);
            if (rebuildFailure != null)
            {
                return rebuildFailure;
            }

            string finalRoot = Path.Combine(
                context.CaseRoot,
                "verified",
                "staged-extractions");
            if (Directory.Exists(finalRoot) || File.Exists(finalRoot))
            {
                return Failure(
                    XccSyntheticInteropStage.ValidateStagedExtractionCandidates,
                    caseId,
                    XccSyntheticInteropDiagnosticCode.CaseAlreadyExists,
                    "The staged extraction validation result already exists.",
                    context.CaseRelativePath + "/verified/staged-extractions");
            }

            string stagingPath = null;
            string publishedPath = null;
            try
            {
                var artifacts = new List<XccSyntheticInteropArtifact>();
                IReadOnlyList<ExtractionContract> contracts =
                    CreateExtractionContracts(context, rebuildPath);
                foreach (ExtractionContract contract in contracts)
                {
                    VerifyExtractionDirectory(context, contract, artifacts);
                }

                stagingPath = Path.Combine(
                    context.CaseRoot,
                    "." + Guid.NewGuid().ToString("N") + ".ra2yr-stage");
                Directory.CreateDirectory(stagingPath);
                EnsureSafeExistingTree(stagingPath);
                string manifest = SerializeExtractionVerification(
                    caseId,
                    contracts);
                byte[] manifestBytes = Encoding.UTF8.GetBytes(manifest);
                WriteNewFileFlushed(
                    Path.Combine(stagingPath, "verification.json"),
                    manifestBytes);
                Directory.Move(stagingPath, finalRoot);
                stagingPath = null;
                publishedPath = finalRoot;
                EnsureSafeExistingTree(finalRoot);
                XccSyntheticInteropArtifact[] verifiedArtifacts = artifacts
                    .Select(artifact => VerifyPublishedArtifact(
                        context,
                        StripCasePrefix(context, artifact.CacheRelativePath),
                        artifact.Role,
                        artifact.Length,
                        artifact.Sha256))
                    .ToArray();
                artifacts.Clear();
                foreach (XccSyntheticInteropArtifact artifact in verifiedArtifacts)
                {
                    artifacts.Add(artifact);
                }

                artifacts.Add(VerifyPublishedArtifact(
                    context,
                    ExtractionVerificationManifestRelative,
                    "staged-extraction-validation-manifest",
                    manifestBytes.LongLength,
                    HashBytes(manifestBytes)));
                XccSyntheticInteropResult success = XccSyntheticInteropResult.Success(
                    XccSyntheticInteropStage.ValidateStagedExtractionCandidates,
                    caseId,
                    artifacts);
                publishedPath = null;
                return success;
            }
            catch (InteropFailure exception)
            {
                return FailureAfterCleanup(
                    XccSyntheticInteropStage.ValidateStagedExtractionCandidates,
                    caseId,
                    exception.Code,
                    exception.PublicMessage,
                    exception.CacheRelativePath,
                    new CleanupTarget(stagingPath, context.CaseRoot, true),
                    new CleanupTarget(publishedPath, context.CaseRoot, false));
            }
            catch (Exception exception) when (IsExpectedFileException(exception))
            {
                return FailureAfterCleanup(
                    XccSyntheticInteropStage.ValidateStagedExtractionCandidates,
                    caseId,
                    XccSyntheticInteropDiagnosticCode.AtomicPublishFailed,
                    "The staged extraction validation could not be published atomically.",
                    context.CaseRelativePath + "/verified/staged-extractions",
                    new CleanupTarget(stagingPath, context.CaseRoot, true),
                    new CleanupTarget(publishedPath, context.CaseRoot, false));
            }
        }

        private static PrepareLayout PopulatePreparedCase(string stagingRoot)
        {
            string[] directories =
            {
                "manifests",
                "synthetic-payloads/ra2yr",
                "inputs-for-xcc",
                "outgoing-to-xcc",
                "outgoing-to-xcc/inner",
                "incoming-from-xcc",
                "extracted-candidates/ra2yr-classic",
                "extracted-candidates/ra2yr-checksum",
                "extracted-candidates/ra2yr-encrypted",
                "extracted-candidates/ra2yr-inner",
                "extracted-candidates/ra2yr-nested",
                "extracted-candidates/xcc-created-rebuild",
                "verified"
            };
            foreach (string directory in directories)
            {
                Directory.CreateDirectory(CombineRelative(stagingRoot, directory));
            }

            foreach (SyntheticPayloadSpec payload in Ra2YrPayloads)
            {
                WriteNewFileFlushed(
                    CombineRelative(
                        stagingRoot,
                        "synthetic-payloads/ra2yr/" + payload.Name),
                    payload.Payload);
            }

            foreach (SyntheticPayloadSpec payload in XccPayloads)
            {
                WriteNewFileFlushed(
                    CombineRelative(stagingRoot, "inputs-for-xcc/" + payload.Name),
                    payload.Payload);
            }

            var layout = new PrepareLayout();
            ArchiveOutput classic = WriteArchive(
                stagingRoot,
                "outgoing-to-xcc/ra2yr-classic.mix",
                "classic",
                Ra2YrPayloads,
                CreateOptions(MixWriteHeaderKind.Classic, false, null));
            layout.Archives.Add(classic);

            ArchiveOutput checksum = WriteArchive(
                stagingRoot,
                "outgoing-to-xcc/ra2yr-checksum.mix",
                "checksum",
                Ra2YrPayloads,
                CreateOptions(MixWriteHeaderKind.Extended, true, null));
            layout.Archives.Add(checksum);

            ArchiveOutput encrypted = WriteArchive(
                stagingRoot,
                "outgoing-to-xcc/ra2yr-encrypted.mix",
                "encrypted-directory",
                Ra2YrPayloads,
                CreateOptions(MixWriteHeaderKind.Extended, false, SyntheticKeySource));
            layout.Archives.Add(encrypted);

            ArchiveOutput inner = WriteArchive(
                stagingRoot,
                "outgoing-to-xcc/inner/local.mix",
                "nested-inner",
                InnerPayloads,
                CreateOptions(MixWriteHeaderKind.Classic, false, null));
            layout.Archives.Add(inner);

            var outerPayloads = new[]
            {
                new SyntheticPayloadSpec("inner/local.mix", inner.Bytes),
                OuterNotePayload
            };
            ArchiveOutput outer = WriteArchive(
                stagingRoot,
                "outgoing-to-xcc/ra2yr-nested.mix",
                "nested-outer",
                outerPayloads,
                CreateOptions(MixWriteHeaderKind.Classic, false, null));
            AssertNestedArchive(outer.Bytes, inner.Bytes);
            layout.Archives.Add(outer);

            return layout;
        }

        private static ArchiveOutput WriteArchive(
            string stagingRoot,
            string relativePath,
            string role,
            IReadOnlyList<SyntheticPayloadSpec> payloads,
            MixWriteOptions options)
        {
            MixWriteEntry[] entries = payloads
                .Select(payload => new MixWriteEntry(payload.Id, payload.Payload))
                .ToArray();
            string path = CombineRelative(stagingRoot, relativePath);
            MixWriteResult result = MixArchiveWriter.WriteToFile(
                entries,
                options,
                path,
                stagingRoot,
                MixOutputPurpose.Cache,
                false);
            if (!result.IsSuccess)
            {
                throw new InteropFailure(
                    XccSyntheticInteropDiagnosticCode.ArchiveBuildFailed,
                    "A required autonomous synthetic MIX archive could not be built.",
                    relativePath);
            }

            SyntheticPayloadSpec[] ordered = options.Order == MixWriteOrder.DeterministicRebuild
                ? payloads.OrderBy(payload => payload.Id).ToArray()
                : payloads.ToArray();
            return new ArchiveOutput(
                role,
                relativePath,
                result.GetArchiveBytes(),
                result.Sha256.ToLowerInvariant(),
                options.HeaderKind,
                options.IncludeChecksum,
                options.IsEncrypted,
                ordered);
        }

        private static void AssertNestedArchive(byte[] outerBytes, byte[] expectedInnerBytes)
        {
            var source = new BinarySourceContext(
                "format.mix-xcc-interop-synthetic",
                "synthetic",
                LogicalContentPath.Parse("ra2yr-nested.mix"));
            MixArchiveReadResult outerResult = MixArchiveReader.Read(outerBytes, source);
            if (!outerResult.IsSuccess)
            {
                throw new InteropFailure(
                    XccSyntheticInteropDiagnosticCode.ArchiveReadFailed,
                    "The autonomous nested MIX did not pass bounded re-read.",
                    "outgoing-to-xcc/ra2yr-nested.mix");
            }

            using (outerResult.Archive)
            {
                MixFileId innerId = MixFileId.ComputeCandidateId("inner/local.mix");
                MixArchiveEntry innerEntry = outerResult.Archive.Entries
                    .SingleOrDefault(entry => entry.Id == innerId);
                if (innerEntry == null || innerEntry.Length != expectedInnerBytes.Length)
                {
                    throw new InteropFailure(
                        XccSyntheticInteropDiagnosticCode.ArchiveMismatch,
                        "The nested MIX entry was not reproduced exactly.",
                        "outgoing-to-xcc/ra2yr-nested.mix");
                }

                var actual = new byte[checked((int)innerEntry.Length)];
                innerEntry.OpenPayloadWindow().ReadExactly(
                    0,
                    actual,
                    0,
                    actual.Length,
                    "synthetic-inner-mix");
                if (!actual.SequenceEqual(expectedInnerBytes))
                {
                    throw new InteropFailure(
                        XccSyntheticInteropDiagnosticCode.PayloadMismatch,
                        "The nested MIX payload differs from the independently written inner archive.",
                        "outgoing-to-xcc/ra2yr-nested.mix");
                }

                var innerSource = new BinarySourceContext(
                    "format.mix-xcc-interop-synthetic",
                    "synthetic",
                    LogicalContentPath.Parse("inner/local.mix"));
                MixArchiveReadResult innerResult = MixArchiveReader.Read(actual, innerSource);
                if (!innerResult.IsSuccess)
                {
                    throw new InteropFailure(
                        XccSyntheticInteropDiagnosticCode.ArchiveReadFailed,
                        "The bounded nested MIX payload could not be parsed.",
                        "outgoing-to-xcc/inner/local.mix");
                }

                innerResult.Archive.Dispose();
            }
        }

        private static ObservedArchive ReadObservedArchive(
            string path,
            string logicalPath)
        {
            try
            {
                var info = new FileInfo(path);
                info.Refresh();
                if (!info.Exists || info.Length < 0 || info.Length > MaximumInteropArchiveBytes ||
                    (info.Attributes & FileAttributes.ReparsePoint) != 0)
                {
                    throw new InteropFailure(
                        XccSyntheticInteropDiagnosticCode.RequiredInputRejected,
                        "The fixed XCC-created input violates its file boundary or size budget.",
                        logicalPath);
                }

                using (var stream = new FileStream(
                    path,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read))
                {
                    long length = stream.Length;
                    if (length < 0 || length > MaximumInteropArchiveBytes ||
                        length != info.Length)
                    {
                        throw new InteropFailure(
                            XccSyntheticInteropDiagnosticCode.RequiredInputRejected,
                            "The opened XCC-created input violates its fixed size budget.",
                            logicalPath);
                    }

                    string archiveSha;
                    using (SHA256 sha256 = SHA256.Create())
                    {
                        archiveSha = ToHex(sha256.ComputeHash(stream));
                    }

                    stream.Position = 0;
                    var source = new BinarySourceContext(
                        "format.mix-xcc-interop",
                        "xcc-synthetic",
                        LogicalContentPath.Parse(logicalPath));
                    MixArchiveReadResult result = MixArchiveReader.Read(
                        stream,
                        0,
                        length,
                        source,
                        CreateReadLimits(length),
                        true);
                    if (!result.IsSuccess)
                    {
                        throw new InteropFailure(
                            XccSyntheticInteropDiagnosticCode.ArchiveReadFailed,
                            "The fixed XCC-created MIX failed bounded parsing.",
                            logicalPath);
                    }

                    using (result.Archive)
                    {
                        MixArchive archive = result.Archive;
                        var entries = new List<ObservedEntry>(archive.Entries.Count);
                        foreach (MixArchiveEntry entry in archive.Entries)
                        {
                            var payload = new byte[checked((int)entry.Length)];
                            entry.OpenPayloadWindow().ReadExactly(
                                0,
                                payload,
                                0,
                                payload.Length,
                                "xcc-synthetic-entry");
                            entries.Add(new ObservedEntry(entry.Id, payload));
                        }

                        return new ObservedArchive(
                            archive.HeaderKind,
                            archive.HasChecksum,
                            archive.IsEncrypted,
                            archive.GetKeySource(),
                            length,
                            archiveSha,
                            entries);
                    }
                }
            }
            catch (InteropFailure)
            {
                throw;
            }
            catch (Exception exception) when (
                IsExpectedFileException(exception) ||
                exception is BinaryReadException ||
                exception is OverflowException)
            {
                throw new InteropFailure(
                    XccSyntheticInteropDiagnosticCode.ArchiveReadFailed,
                    "The fixed XCC-created MIX could not be inspected safely.",
                    logicalPath);
            }
        }

        private static void AssertExactEntries(
            ObservedArchive archive,
            IReadOnlyList<SyntheticPayloadSpec> expected,
            string archiveRelativePath)
        {
            if (archive.Entries.Count != expected.Count)
            {
                throw new InteropFailure(
                    XccSyntheticInteropDiagnosticCode.ArchiveMismatch,
                    "The XCC-created MIX entry count differs from the fixed synthetic contract.",
                    archiveRelativePath);
            }

            for (int index = 0; index < expected.Count; index++)
            {
                SyntheticPayloadSpec expectedEntry = expected[index];
                ObservedEntry actual = archive.Entries[index];
                if (actual.Id != expectedEntry.Id ||
                    actual.Payload.Length != expectedEntry.Payload.Length)
                {
                    throw new InteropFailure(
                        XccSyntheticInteropDiagnosticCode.ArchiveMismatch,
                        "The XCC-created MIX ID, order, or length differs from the fixed synthetic contract.",
                        archiveRelativePath);
                }

                if (!actual.Payload.SequenceEqual(expectedEntry.Payload))
                {
                    throw new InteropFailure(
                        XccSyntheticInteropDiagnosticCode.PayloadMismatch,
                        "An XCC-created MIX payload hash differs from the autonomous synthetic input.",
                        archiveRelativePath);
                }
            }
        }

        private static ObservedEntry ValidateStagedCreatedEntries(
            ObservedArchive archive,
            string archiveRelativePath)
        {
            var expectedById = XccPayloads.ToDictionary(
                payload => payload.Id.Value,
                payload => payload);
            var observedPayloadIds = new HashSet<uint>();
            ObservedEntry localMixDatabase = null;

            foreach (ObservedEntry actual in archive.Entries)
            {
                SyntheticPayloadSpec expected;
                if (expectedById.TryGetValue(actual.Id.Value, out expected))
                {
                    if (!observedPayloadIds.Add(actual.Id.Value) ||
                        actual.Payload.Length != expected.Payload.Length)
                    {
                        throw new InteropFailure(
                            XccSyntheticInteropDiagnosticCode.ArchiveMismatch,
                            "A staged created candidate has a duplicate or length-mismatched synthetic entry.",
                            archiveRelativePath);
                    }

                    if (!actual.Payload.SequenceEqual(expected.Payload))
                    {
                        throw new InteropFailure(
                            XccSyntheticInteropDiagnosticCode.PayloadMismatch,
                            "A staged created candidate payload differs from its autonomous synthetic input.",
                            archiveRelativePath);
                    }

                    continue;
                }

                if (actual.Id == LocalMixDatabaseId && localMixDatabase == null)
                {
                    localMixDatabase = actual;
                    continue;
                }

                throw new InteropFailure(
                    XccSyntheticInteropDiagnosticCode.ArchiveMismatch,
                    "A staged created candidate contains an unexpected or duplicate metadata entry.",
                    archiveRelativePath);
            }

            if (observedPayloadIds.Count != XccPayloads.Length)
            {
                throw new InteropFailure(
                    XccSyntheticInteropDiagnosticCode.ArchiveMismatch,
                    "A staged created candidate is missing a required autonomous synthetic entry.",
                    archiveRelativePath);
            }

            int expectedCount = checked(XccPayloads.Length +
                                        (localMixDatabase == null ? 0 : 1));
            if (archive.Entries.Count != expectedCount)
            {
                throw new InteropFailure(
                    XccSyntheticInteropDiagnosticCode.ArchiveMismatch,
                    "A staged created candidate entry count violates its bounded contract.",
                    archiveRelativePath);
            }

            return localMixDatabase;
        }

        private static IReadOnlyList<ExtractionContract> CreateExtractionContracts(
            OperationContext context,
            string rebuildPath)
        {
            const string innerRelative = "outgoing-to-xcc/inner/local.mix";
            byte[] innerArchiveBytes = ReadBoundedFileBytes(
                CombineRelative(context.CaseRoot, innerRelative),
                innerRelative);
            var nestedPayloads = new[]
            {
                new SyntheticPayloadSpec("inner/local.mix", innerArchiveBytes),
                OuterNotePayload
            };

            return Array.AsReadOnly(new[]
            {
                ObserveExtractionContract(
                    context,
                    "ra2yr-classic",
                    "outgoing-to-xcc/ra2yr-classic.mix",
                    "extracted-candidates/ra2yr-classic",
                    DeterministicOrder(Ra2YrPayloads),
                    MixArchiveHeaderKind.Classic,
                    false,
                    false),
                ObserveExtractionContract(
                    context,
                    "ra2yr-checksum",
                    "outgoing-to-xcc/ra2yr-checksum.mix",
                    "extracted-candidates/ra2yr-checksum",
                    DeterministicOrder(Ra2YrPayloads),
                    MixArchiveHeaderKind.Extended,
                    true,
                    false),
                ObserveExtractionContract(
                    context,
                    "ra2yr-encrypted",
                    "outgoing-to-xcc/ra2yr-encrypted.mix",
                    "extracted-candidates/ra2yr-encrypted",
                    DeterministicOrder(Ra2YrPayloads),
                    MixArchiveHeaderKind.Extended,
                    false,
                    true),
                ObserveExtractionContract(
                    context,
                    "ra2yr-inner",
                    innerRelative,
                    "extracted-candidates/ra2yr-inner",
                    DeterministicOrder(InnerPayloads),
                    MixArchiveHeaderKind.Classic,
                    false,
                    false),
                ObserveExtractionContract(
                    context,
                    "ra2yr-nested",
                    "outgoing-to-xcc/ra2yr-nested.mix",
                    "extracted-candidates/ra2yr-nested",
                    DeterministicOrder(nestedPayloads),
                    MixArchiveHeaderKind.Classic,
                    false,
                    false),
                ObserveStagedCreatedExtractionContract(
                    context,
                    "xcc-created-rebuild",
                    XccVerificationRelative,
                    "extracted-candidates/xcc-created-rebuild",
                    rebuildPath)
            });
        }

        private static SyntheticPayloadSpec[] DeterministicOrder(
            IEnumerable<SyntheticPayloadSpec> payloads)
        {
            return payloads.OrderBy(payload => payload.Id).ToArray();
        }

        private static ExtractionContract ObserveExtractionContract(
            OperationContext context,
            string role,
            string archiveRelativePath,
            string extractionRelativeDirectory,
            IReadOnlyList<SyntheticPayloadSpec> expected,
            MixArchiveHeaderKind? expectedHeader,
            bool? expectedChecksum,
            bool? expectedEncryption,
            string explicitArchivePath = null)
        {
            string archivePath = explicitArchivePath ??
                                 CombineRelative(context.CaseRoot, archiveRelativePath);
            EnsureSafeExistingTree(archivePath);
            ObservedArchive archive = ReadObservedArchive(archivePath, archiveRelativePath);
            AssertExactEntries(archive, expected, archiveRelativePath);
            if ((expectedHeader.HasValue && archive.HeaderKind != expectedHeader.Value) ||
                (expectedChecksum.HasValue && archive.HasChecksum != expectedChecksum.Value) ||
                (expectedEncryption.HasValue && archive.IsEncrypted != expectedEncryption.Value))
            {
                throw new InteropFailure(
                    XccSyntheticInteropDiagnosticCode.ArchiveMismatch,
                    "A staged extraction input archive differs from its fixed mode contract.",
                    context.CaseRelativePath + "/" + archiveRelativePath);
            }

            return new ExtractionContract(
                role,
                archiveRelativePath,
                archive.Length,
                archive.Sha256,
                extractionRelativeDirectory,
                expected);
        }

        private static ExtractionContract ObserveStagedCreatedExtractionContract(
            OperationContext context,
            string role,
            string archiveRelativePath,
            string extractionRelativeDirectory,
            string archivePath)
        {
            EnsureSafeExistingTree(archivePath);
            ObservedArchive archive = ReadObservedArchive(
                archivePath,
                archiveRelativePath);
            ObservedEntry localMixDatabase = ValidateStagedCreatedEntries(
                archive,
                archiveRelativePath);
            var expected = new List<SyntheticPayloadSpec>(XccPayloads);
            if (localMixDatabase != null)
            {
                expected.Add(new SyntheticPayloadSpec(
                    LocalMixDatabaseName,
                    localMixDatabase.Payload));
            }

            return new ExtractionContract(
                role,
                archiveRelativePath,
                archive.Length,
                archive.Sha256,
                extractionRelativeDirectory,
                expected);
        }

        private static void VerifyExtractionDirectory(
            OperationContext context,
            ExtractionContract contract,
            ICollection<XccSyntheticInteropArtifact> artifacts)
        {
            artifacts.Add(new XccSyntheticInteropArtifact(
                "staged-input-" + contract.Role,
                context.CaseRelativePath + "/" + contract.InputArchiveRelativePath,
                contract.InputArchiveLength,
                contract.InputArchiveSha256));

            string relativeDirectory = contract.ExtractionRelativeDirectory;
            string directory = CombineRelative(context.CaseRoot, relativeDirectory);
            if (!Directory.Exists(directory) || File.Exists(directory))
            {
                throw new InteropFailure(
                    XccSyntheticInteropDiagnosticCode.RequiredInputMissing,
                    "A fixed staged extraction directory is missing.",
                    context.CaseRelativePath + "/" + relativeDirectory);
            }

            ExtractionDirectorySnapshot before = CaptureExtractionSnapshot(
                directory,
                MaximumExtractionEntriesPerGroup);
            string[] expectedFiles = contract.ExpectedPayloads
                .Select(payload => LogicalContentPath.Parse(payload.Name).Value)
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();
            string[] expectedDirectories = GetExpectedDirectories(expectedFiles);
            if (!before.FilePaths.SequenceEqual(expectedFiles, StringComparer.Ordinal) ||
                !before.DirectoryPaths.SequenceEqual(expectedDirectories, StringComparer.Ordinal))
            {
                throw new InteropFailure(
                    XccSyntheticInteropDiagnosticCode.PayloadMismatch,
                    "The staged extraction directory contains a missing, extra, or unexpected item.",
                    context.CaseRelativePath + "/" + relativeDirectory);
            }

            int artifactIndex = 0;
            foreach (SyntheticPayloadSpec payload in contract.ExpectedPayloads
                         .OrderBy(item => item.Name, StringComparer.Ordinal))
            {
                string relativePath = relativeDirectory + "/" + payload.Name;
                string path = CombineRelative(context.CaseRoot, relativePath);
                EnsureSafeExistingTree(path);
                var info = new FileInfo(path);
                info.Refresh();
                string actualSha256;
                if (!info.Exists || (info.Attributes & FileAttributes.ReparsePoint) != 0 ||
                    info.Length != payload.Payload.Length ||
                    !TryHashFile(path, info.Length, out actualSha256) ||
                    !string.Equals(actualSha256, payload.Sha256, StringComparison.Ordinal))
                {
                    throw new InteropFailure(
                        XccSyntheticInteropDiagnosticCode.PayloadMismatch,
                        "A staged extracted payload differs from the autonomous synthetic input.",
                        context.CaseRelativePath + "/" + relativePath);
                }

                artifacts.Add(new XccSyntheticInteropArtifact(
                    "staged-output-" + contract.Role + "-" +
                    artifactIndex++.ToString(CultureInfo.InvariantCulture),
                    context.CaseRelativePath + "/" + relativePath,
                    info.Length,
                    payload.Sha256));
            }

            ExtractionDirectorySnapshot after = CaptureExtractionSnapshot(
                directory,
                MaximumExtractionEntriesPerGroup);
            if (!string.Equals(before.Fingerprint, after.Fingerprint, StringComparison.Ordinal))
            {
                throw new InteropFailure(
                    XccSyntheticInteropDiagnosticCode.ExtractionChanged,
                    "A staged extraction directory changed during bounded validation.",
                    context.CaseRelativePath + "/" + relativeDirectory);
            }
        }

        private static ExtractionDirectorySnapshot CaptureExtractionSnapshot(
            string root,
            int maximumEntries)
        {
            try
            {
                EnsureSafeExistingTree(root);
                var files = new List<ExtractionSnapshotEntry>();
                var directories = new List<string>();
                var pending = new Stack<PendingExtractionDirectory>();
                pending.Push(new PendingExtractionDirectory(root, string.Empty, 0));
                int observedCount = 0;
                while (pending.Count != 0)
                {
                    PendingExtractionDirectory current = pending.Pop();
                    foreach (string path in Directory.EnumerateFileSystemEntries(current.Path))
                    {
                        observedCount = checked(observedCount + 1);
                        if (observedCount > maximumEntries)
                        {
                            throw new InteropFailure(
                                XccSyntheticInteropDiagnosticCode.ExtractionBudgetExceeded,
                                "A staged extraction directory exceeds its explicit entry budget.");
                        }

                        FileAttributes attributes = File.GetAttributes(path);
                        if ((attributes & FileAttributes.ReparsePoint) != 0)
                        {
                            throw new InteropFailure(
                                XccSyntheticInteropDiagnosticCode.RequiredInputRejected,
                                "A staged extraction entry is a reparse point.");
                        }

                        string name = Path.GetFileName(path);
                        string relative = current.RelativePath.Length == 0
                            ? name
                            : current.RelativePath + "/" + name;
                        relative = LogicalContentPath.Parse(relative).Value;
                        if ((attributes & FileAttributes.Directory) != 0)
                        {
                            int depth = checked(current.Depth + 1);
                            if (depth > 8)
                            {
                                throw new InteropFailure(
                                    XccSyntheticInteropDiagnosticCode.ExtractionBudgetExceeded,
                                    "A staged extraction directory exceeds its nesting budget.");
                            }

                            directories.Add(relative);
                            pending.Push(new PendingExtractionDirectory(path, relative, depth));
                        }
                        else
                        {
                            var info = new FileInfo(path);
                            info.Refresh();
                            if (!info.Exists || info.Length < 0 ||
                                info.Length > MaximumInteropArchiveBytes)
                            {
                                throw new InteropFailure(
                                    XccSyntheticInteropDiagnosticCode.RequiredInputRejected,
                                    "A staged extraction file violates its explicit size budget.");
                            }

                            string sha256;
                            if (!TryHashFile(path, info.Length, out sha256))
                            {
                                throw new InteropFailure(
                                    XccSyntheticInteropDiagnosticCode.ExtractionChanged,
                                    "A staged extraction file changed during bounded hashing.");
                            }

                            files.Add(new ExtractionSnapshotEntry(
                                relative,
                                info.Length,
                                sha256));
                        }
                    }
                }

                files.Sort((left, right) =>
                    StringComparer.Ordinal.Compare(left.RelativePath, right.RelativePath));
                directories.Sort(StringComparer.Ordinal);
                var canonical = new StringBuilder();
                foreach (string directory in directories)
                {
                    canonical.Append("D|");
                    canonical.Append(directory);
                    canonical.Append('\n');
                }

                foreach (ExtractionSnapshotEntry file in files)
                {
                    canonical.Append("F|");
                    canonical.Append(file.RelativePath);
                    canonical.Append('|');
                    canonical.Append(file.Length.ToString(CultureInfo.InvariantCulture));
                    canonical.Append('|');
                    canonical.Append(file.Sha256);
                    canonical.Append('\n');
                }

                return new ExtractionDirectorySnapshot(
                    files.Select(file => file.RelativePath),
                    directories,
                    HashBytes(Encoding.UTF8.GetBytes(canonical.ToString())));
            }
            catch (InteropFailure)
            {
                throw;
            }
            catch (Exception exception) when (
                IsExpectedFileException(exception) ||
                exception is ArgumentException ||
                exception is OverflowException)
            {
                throw new InteropFailure(
                    XccSyntheticInteropDiagnosticCode.RequiredInputRejected,
                    "A staged extraction directory could not be inspected safely.");
            }
        }

        private static string[] GetExpectedDirectories(IEnumerable<string> filePaths)
        {
            var result = new HashSet<string>(StringComparer.Ordinal);
            foreach (string filePath in filePaths)
            {
                int separator = filePath.IndexOf('/');
                while (separator >= 0)
                {
                    result.Add(filePath.Substring(0, separator));
                    separator = filePath.IndexOf('/', separator + 1);
                }
            }

            return result.OrderBy(value => value, StringComparer.Ordinal).ToArray();
        }

        private static XccSyntheticInteropResult TryCreateContext(
            ExternalContentConfiguration configuration,
            string caseId,
            XccSyntheticInteropStage stage,
            bool requireExistingCase,
            out OperationContext context)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            context = null;
            if (!IsValidCaseId(caseId))
            {
                return Failure(
                    stage,
                    null,
                    XccSyntheticInteropDiagnosticCode.InvalidCaseId,
                    "The case id must be canonical lower-case ASCII using letters, digits, '-' or '_'.");
            }

            try
            {
                string cacheRoot = RepositoryPathPolicy.NormalizeAbsolutePath(
                    configuration.CachePath);
                string root = RepositoryPathPolicy.NormalizeAbsolutePath(
                    Path.GetPathRoot(cacheRoot));
                string aliasReason;
                string reparsePoint;
                if (string.Equals(cacheRoot, root, PathComparison) ||
                    RepositoryPathPolicy.TryFindUnsupportedAlias(cacheRoot, out aliasReason) ||
                    RepositoryPathPolicy.ContainsExistingReparsePoint(
                        cacheRoot,
                        out reparsePoint) ||
                    File.Exists(cacheRoot))
                {
                    return Failure(
                        stage,
                        caseId,
                        XccSyntheticInteropDiagnosticCode.UnsafeCacheBoundary,
                        "The configured external cache boundary is unsupported.");
                }

                var protectedPaths = new List<string> { configuration.RepositoryRoot };
                protectedPaths.AddRange(configuration.Sources.Select(source => source.RootPath));
                foreach (string protectedPath in protectedPaths)
                {
                    if (RepositoryPathPolicy.ContainsExistingReparsePoint(
                            protectedPath,
                            out reparsePoint))
                    {
                        return Failure(
                            stage,
                            caseId,
                            XccSyntheticInteropDiagnosticCode.UnsafeCacheBoundary,
                            "A protected repository or content path uses an unsupported reparse point.");
                    }

                    bool overlaps;
                    string failureReason;
                    if (!RepositoryPathPolicy.TryDetermineOverlap(
                            cacheRoot,
                            protectedPath,
                            out overlaps,
                            out failureReason) ||
                        overlaps)
                    {
                        return Failure(
                            stage,
                            caseId,
                            XccSyntheticInteropDiagnosticCode.UnsafeCacheBoundary,
                            "The external cache identity cannot be separated from protected content.");
                    }
                }

                string interopRoot = CombineRelative(cacheRoot, InteropRootRelative);
                string caseRoot = Path.Combine(interopRoot, caseId);
                if (!RepositoryPathPolicy.IsInsideOrEqual(interopRoot, cacheRoot) ||
                    !RepositoryPathPolicy.IsInsideOrEqual(caseRoot, interopRoot))
                {
                    return Failure(
                        stage,
                        caseId,
                        XccSyntheticInteropDiagnosticCode.UnsafeCacheBoundary,
                        "The fixed interop case path escaped the external cache boundary.");
                }

                context = new OperationContext(
                    cacheRoot,
                    interopRoot,
                    caseRoot,
                    InteropRootRelative + "/" + caseId);
                if (requireExistingCase)
                {
                    if (!Directory.Exists(caseRoot) || File.Exists(caseRoot))
                    {
                        return Failure(
                            stage,
                            caseId,
                            XccSyntheticInteropDiagnosticCode.CaseMissing,
                            "The prepared synthetic interop case does not exist.",
                            context.CaseRelativePath);
                    }

                    EnsureSafeExistingTree(caseRoot);
                }

                return null;
            }
            catch (InteropFailure exception)
            {
                return Failure(
                    stage,
                    caseId,
                    exception.Code,
                    exception.PublicMessage,
                    exception.CacheRelativePath);
            }
            catch (Exception exception) when (
                IsExpectedFileException(exception) || exception is ArgumentException)
            {
                return Failure(
                    stage,
                    caseId,
                    XccSyntheticInteropDiagnosticCode.UnsafeCacheBoundary,
                    "The configured external cache boundary could not be validated.");
            }
        }

        private static XccSyntheticInteropResult ValidateFixedInputFile(
            OperationContext context,
            string path,
            string relativePath,
            XccSyntheticInteropStage stage)
        {
            try
            {
                if (!RepositoryPathPolicy.IsInsideOrEqual(path, context.CaseRoot))
                {
                    return Failure(
                        stage,
                        context.CaseId,
                        XccSyntheticInteropDiagnosticCode.RequiredInputRejected,
                        "A fixed interop input escaped its prepared case boundary.",
                        context.CaseRelativePath + "/" + relativePath);
                }

                if (!File.Exists(path) || Directory.Exists(path))
                {
                    return Failure(
                        stage,
                        context.CaseId,
                        XccSyntheticInteropDiagnosticCode.RequiredInputMissing,
                        "A fixed interop input file is missing.",
                        context.CaseRelativePath + "/" + relativePath);
                }

                EnsureSafeExistingTree(path);
                if ((File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0)
                {
                    return Failure(
                        stage,
                        context.CaseId,
                        XccSyntheticInteropDiagnosticCode.RequiredInputRejected,
                        "A fixed interop input is a reparse point.",
                        context.CaseRelativePath + "/" + relativePath);
                }

                return null;
            }
            catch (Exception exception) when (IsExpectedFileException(exception))
            {
                return Failure(
                    stage,
                    context.CaseId,
                    XccSyntheticInteropDiagnosticCode.RequiredInputRejected,
                    "A fixed interop input could not be validated safely.",
                    context.CaseRelativePath + "/" + relativePath);
            }
        }

        private static void EnsureDirectoryTree(string cacheRoot, string finalDirectory)
        {
            if (File.Exists(cacheRoot))
            {
                throw new InteropFailure(
                    XccSyntheticInteropDiagnosticCode.UnsafeCacheBoundary,
                    "The configured cache path identifies a file.");
            }

            Directory.CreateDirectory(cacheRoot);
            EnsureSafeExistingTree(cacheRoot);
            string relative = finalDirectory.Substring(cacheRoot.Length)
                .TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            string current = cacheRoot;
            foreach (string segment in relative.Split(
                         new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar },
                         StringSplitOptions.RemoveEmptyEntries))
            {
                current = Path.Combine(current, segment);
                if (File.Exists(current))
                {
                    throw new InteropFailure(
                        XccSyntheticInteropDiagnosticCode.UnsafeCacheBoundary,
                        "A fixed cache directory is occupied by a file.");
                }

                Directory.CreateDirectory(current);
                EnsureSafeExistingTree(current);
            }
        }

        private static void EnsureSafeExistingTree(string path)
        {
            string reparsePoint;
            if (RepositoryPathPolicy.ContainsExistingReparsePoint(path, out reparsePoint))
            {
                throw new InteropFailure(
                    XccSyntheticInteropDiagnosticCode.UnsafeCacheBoundary,
                    "The fixed external cache path traverses a reparse point.");
            }
        }

        private static void WriteNewFileFlushed(string path, byte[] bytes)
        {
            string directory = Path.GetDirectoryName(path);
            if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory))
            {
                throw new InteropFailure(
                    XccSyntheticInteropDiagnosticCode.ManifestWriteFailed,
                    "A required external cache directory is missing.");
            }

            using (var stream = new FileStream(
                path,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None))
            {
                stream.Write(bytes, 0, bytes.Length);
                stream.Flush(true);
            }
        }

        private static XccSyntheticInteropArtifact VerifyPublishedArtifact(
            OperationContext context,
            string caseRelativePath,
            string role,
            long expectedLength,
            string expectedSha256)
        {
            try
            {
                LogicalContentPath logicalPath = LogicalContentPath.Parse(caseRelativePath);
                string path = CombineRelative(context.CaseRoot, logicalPath.Value);
                if (!RepositoryPathPolicy.IsInsideOrEqual(path, context.CaseRoot))
                {
                    throw new InteropFailure(
                        XccSyntheticInteropDiagnosticCode.PublishedArtifactMismatch,
                        "A published synthetic artifact escaped its fixed case boundary.");
                }

                EnsureSafeExistingTree(path);
                var before = new FileInfo(path);
                before.Refresh();
                if (!before.Exists || (before.Attributes & FileAttributes.ReparsePoint) != 0 ||
                    before.Length != expectedLength)
                {
                    throw new InteropFailure(
                        XccSyntheticInteropDiagnosticCode.PublishedArtifactMismatch,
                        "A published synthetic artifact has an unexpected file identity.",
                        context.CaseRelativePath + "/" + logicalPath.Value);
                }

                DateTime writeTime = before.LastWriteTimeUtc;
                string actualSha256;
                if (!TryHashFile(path, before.Length, out actualSha256))
                {
                    throw new InteropFailure(
                        XccSyntheticInteropDiagnosticCode.PublishedArtifactMismatch,
                        "A published synthetic artifact changed during bounded hashing.",
                        context.CaseRelativePath + "/" + logicalPath.Value);
                }

                var after = new FileInfo(path);
                after.Refresh();
                if (!after.Exists || after.Length != before.Length ||
                    after.LastWriteTimeUtc != writeTime ||
                    !string.Equals(actualSha256, expectedSha256, StringComparison.Ordinal))
                {
                    throw new InteropFailure(
                        XccSyntheticInteropDiagnosticCode.PublishedArtifactMismatch,
                        "A published synthetic artifact failed its length or digest verification.",
                        context.CaseRelativePath + "/" + logicalPath.Value);
                }

                return new XccSyntheticInteropArtifact(
                    role,
                    context.CaseRelativePath + "/" + logicalPath.Value,
                    after.Length,
                    actualSha256);
            }
            catch (InteropFailure)
            {
                throw;
            }
            catch (Exception exception) when (
                IsExpectedFileException(exception) || exception is ArgumentException)
            {
                throw new InteropFailure(
                    XccSyntheticInteropDiagnosticCode.PublishedArtifactMismatch,
                    "A published synthetic artifact could not be verified safely.");
            }
        }

        private static string StripCasePrefix(
            OperationContext context,
            string cacheRelativePath)
        {
            string prefix = context.CaseRelativePath + "/";
            if (!cacheRelativePath.StartsWith(prefix, StringComparison.Ordinal))
            {
                throw new InteropFailure(
                    XccSyntheticInteropDiagnosticCode.PublishedArtifactMismatch,
                    "A reported synthetic artifact does not belong to its fixed case.");
            }

            return LogicalContentPath.Parse(
                cacheRelativePath.Substring(prefix.Length)).Value;
        }

        private static MixWriteOptions CreateOptions(
            MixWriteHeaderKind headerKind,
            bool checksum,
            byte[] keySource)
        {
            return new MixWriteOptions(
                MixWriteOrder.DeterministicRebuild,
                headerKind,
                checksum,
                keySource,
                MaximumInteropEntries,
                MaximumInteropArchiveBytes);
        }

        private static MixReadLimits CreateReadLimits(long length)
        {
            long directoryBudget = Math.Min(length, 2L * 1024 * 1024);
            return new MixReadLimits(
                MaximumInteropArchiveBytes,
                MaximumInteropEntries,
                directoryBudget,
                4L * 1024 * 1024,
                checked(MaximumInteropArchiveBytes * 3),
                MaximumInteropEntries + 8L,
                8);
        }

        private static string SerializeExpectedManifest(string caseId, PrepareLayout layout)
        {
            var builder = new StringBuilder();
            builder.Append("{\"schemaVersion\":1,\"synthetic\":true,\"caseId\":");
            AppendJson(builder, caseId);
            builder.Append(",\"contractKind\":\"internal-emulated-contract\"");
            builder.Append(",\"realXccExecutionAttested\":false");
            builder.Append(",\"keySourceId\":\"ra2yr-autonomous-synthetic-key-v1\"");
            builder.Append(",\"xccCreateExpectedEntries\":");
            AppendPayloads(builder, XccPayloads);
            builder.Append(",\"archives\":[");
            for (int index = 0; index < layout.Archives.Count; index++)
            {
                if (index != 0)
                {
                    builder.Append(',');
                }

                ArchiveOutput archive = layout.Archives[index];
                builder.Append("{\"role\":");
                AppendJson(builder, archive.Role);
                builder.Append(",\"relativePath\":");
                AppendJson(builder, archive.RelativePath);
                builder.Append(",\"length\":");
                builder.Append(archive.Bytes.Length.ToString(CultureInfo.InvariantCulture));
                builder.Append(",\"sha256\":");
                AppendJson(builder, archive.Sha256);
                builder.Append(",\"header\":");
                AppendJson(builder, archive.HeaderKind.ToString());
                builder.Append(",\"checksum\":");
                builder.Append(archive.Checksum ? "true" : "false");
                builder.Append(",\"encryptedDirectory\":");
                builder.Append(archive.Encrypted ? "true" : "false");
                builder.Append(",\"entries\":");
                AppendPayloads(builder, archive.Entries);
                builder.Append('}');
            }

            builder.Append("]}");
            return builder.ToString();
        }

        private static string SerializeXccCreatedVerification(
            string caseId,
            ObservedArchive incoming,
            ObservedEntry localMixDatabase,
            MixWriteResult rebuild)
        {
            var builder = new StringBuilder();
            builder.Append("{\"schemaVersion\":1,\"synthetic\":true,\"caseId\":");
            AppendJson(builder, caseId);
            builder.Append(",\"validationKind\":\"staged-candidate-semantics\"");
            builder.Append(",\"realXccExecutionAttested\":false");
            builder.Append(",\"incomingSha256\":");
            AppendJson(builder, incoming.Sha256);
            builder.Append(",\"rebuildSha256\":");
            AppendJson(builder, rebuild.Sha256.ToLowerInvariant());
            builder.Append(",\"entryOrderPreserved\":true,\"payloadHashesMatched\":true");
            builder.Append(",\"localMixDatabase\":{\"present\":");
            builder.Append(localMixDatabase == null ? "false" : "true");
            if (localMixDatabase != null)
            {
                builder.Append(",\"id\":");
                AppendJson(builder, localMixDatabase.Id.ToString());
                builder.Append(",\"length\":");
                builder.Append(localMixDatabase.Payload.Length.ToString(
                    CultureInfo.InvariantCulture));
                builder.Append(",\"sha256\":");
                AppendJson(builder, HashBytes(localMixDatabase.Payload));
            }

            builder.Append('}');
            builder.Append(",\"byteIdentical\":");
            builder.Append(string.Equals(
                    incoming.Sha256,
                    rebuild.Sha256.ToLowerInvariant(),
                    StringComparison.Ordinal)
                ? "true"
                : "false");
            builder.Append(",\"entries\":");
            AppendObservedEntries(builder, incoming.Entries);
            builder.Append('}');
            return builder.ToString();
        }

        private static string SerializeExtractionVerification(
            string caseId,
            IEnumerable<ExtractionContract> contracts)
        {
            var builder = new StringBuilder();
            builder.Append("{\"schemaVersion\":1,\"synthetic\":true,\"caseId\":");
            AppendJson(builder, caseId);
            builder.Append(",\"validationKind\":\"staged-candidate-semantics\"");
            builder.Append(",\"realXccExecutionAttested\":false");
            builder.Append(",\"payloadHashesMatched\":true,\"groups\":[");
            int index = 0;
            foreach (ExtractionContract contract in contracts
                         .OrderBy(item => item.Role, StringComparer.Ordinal))
            {
                if (index++ != 0)
                {
                    builder.Append(',');
                }

                builder.Append("{\"inputRole\":");
                AppendJson(builder, contract.Role);
                builder.Append(",\"inputArchivePath\":");
                AppendJson(builder, contract.InputArchiveRelativePath);
                builder.Append(",\"inputArchiveLength\":");
                builder.Append(contract.InputArchiveLength.ToString(CultureInfo.InvariantCulture));
                builder.Append(",\"inputArchiveSha256\":");
                AppendJson(builder, contract.InputArchiveSha256);
                builder.Append(",\"extractionDirectory\":");
                AppendJson(builder, contract.ExtractionRelativeDirectory);
                builder.Append(",\"files\":");
                AppendPayloads(builder, contract.ExpectedPayloads);
                builder.Append('}');
            }

            builder.Append("]}");
            return builder.ToString();
        }

        private static void AppendPayloads(
            StringBuilder builder,
            IEnumerable<SyntheticPayloadSpec> payloads)
        {
            builder.Append('[');
            int index = 0;
            foreach (SyntheticPayloadSpec payload in payloads)
            {
                if (index++ != 0)
                {
                    builder.Append(',');
                }

                builder.Append("{\"name\":");
                AppendJson(builder, payload.Name);
                builder.Append(",\"id\":");
                AppendJson(builder, payload.Id.ToString());
                builder.Append(",\"length\":");
                builder.Append(payload.Payload.Length.ToString(CultureInfo.InvariantCulture));
                builder.Append(",\"sha256\":");
                AppendJson(builder, payload.Sha256);
                builder.Append('}');
            }

            builder.Append(']');
        }

        private static void AppendObservedEntries(
            StringBuilder builder,
            IEnumerable<ObservedEntry> entries)
        {
            builder.Append('[');
            int index = 0;
            foreach (ObservedEntry entry in entries)
            {
                if (index++ != 0)
                {
                    builder.Append(',');
                }

                builder.Append("{\"id\":");
                AppendJson(builder, entry.Id.ToString());
                builder.Append(",\"length\":");
                builder.Append(entry.Payload.Length.ToString(CultureInfo.InvariantCulture));
                builder.Append(",\"sha256\":");
                AppendJson(builder, HashBytes(entry.Payload));
                builder.Append('}');
            }

            builder.Append(']');
        }

        private static void AppendJson(StringBuilder builder, string value)
        {
            builder.Append('"');
            foreach (char character in value)
            {
                switch (character)
                {
                    case '"':
                        builder.Append("\\\"");
                        break;
                    case '\\':
                        builder.Append("\\\\");
                        break;
                    default:
                        if (character < 0x20)
                        {
                            builder.Append("\\u");
                            builder.Append(((int)character).ToString("x4", CultureInfo.InvariantCulture));
                        }
                        else
                        {
                            builder.Append(character);
                        }

                        break;
                }
            }

            builder.Append('"');
        }

        private static bool IsValidCaseId(string value)
        {
            if (string.IsNullOrEmpty(value) || value.Length > 64 ||
                value[0] < 'a' || value[0] > 'z')
            {
                return false;
            }

            for (int index = 1; index < value.Length; index++)
            {
                char character = value[index];
                if ((character < 'a' || character > 'z') &&
                    (character < '0' || character > '9') &&
                    character != '-' && character != '_')
                {
                    return false;
                }
            }

            return true;
        }

        private static string CombineRelative(string root, string logicalRelativePath)
        {
            return Path.GetFullPath(Path.Combine(
                root,
                logicalRelativePath.Replace('/', Path.DirectorySeparatorChar)));
        }

        private static byte[] CreateSyntheticKeySource()
        {
            var source = new byte[80];
            source[0] = 2;
            source[40] = 3;
            return source;
        }

        private static byte[] CreatePattern(int length, int multiplier, int increment)
        {
            var result = new byte[length];
            for (int index = 0; index < result.Length; index++)
            {
                result[index] = (byte)((index * multiplier + increment) & 0xff);
            }

            return result;
        }

        private static string HashBytes(byte[] value)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                return ToHex(sha256.ComputeHash(value));
            }
        }

        private static bool TryHashFile(
            string path,
            long expectedLength,
            out string sha256Value)
        {
            sha256Value = null;
            if (expectedLength < 0 || expectedLength > MaximumInteropArchiveBytes)
            {
                return false;
            }

            try
            {
                using (var stream = new FileStream(
                    path,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read))
                using (SHA256 sha256 = SHA256.Create())
                {
                    if (stream.Length != expectedLength ||
                        stream.Length > MaximumInteropArchiveBytes)
                    {
                        return false;
                    }

                    sha256Value = ToHex(sha256.ComputeHash(stream));
                    return stream.Position == expectedLength &&
                           stream.Length == expectedLength;
                }
            }
            catch (Exception exception) when (IsExpectedFileException(exception))
            {
                sha256Value = null;
                return false;
            }
        }

        private static byte[] ReadBoundedFileBytes(string path, string logicalPath)
        {
            try
            {
                EnsureSafeExistingTree(path);
                var before = new FileInfo(path);
                before.Refresh();
                if (!before.Exists || before.Length < 0 ||
                    before.Length > MaximumInteropArchiveBytes ||
                    before.Length > int.MaxValue ||
                    (before.Attributes & FileAttributes.ReparsePoint) != 0)
                {
                    throw new InteropFailure(
                        XccSyntheticInteropDiagnosticCode.RequiredInputRejected,
                        "A fixed staged archive violates its explicit input budget.",
                        logicalPath);
                }

                long length = before.Length;
                DateTime writeTime = before.LastWriteTimeUtc;
                var bytes = new byte[checked((int)length)];
                using (var stream = new FileStream(
                    path,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read))
                {
                    int offset = 0;
                    while (offset < bytes.Length)
                    {
                        int read = stream.Read(bytes, offset, bytes.Length - offset);
                        if (read == 0)
                        {
                            throw new InteropFailure(
                                XccSyntheticInteropDiagnosticCode.ExtractionChanged,
                                "A fixed staged archive ended during its bounded read.",
                                logicalPath);
                        }

                        offset = checked(offset + read);
                    }

                    if (stream.ReadByte() != -1)
                    {
                        throw new InteropFailure(
                            XccSyntheticInteropDiagnosticCode.ExtractionChanged,
                            "A fixed staged archive grew during its bounded read.",
                            logicalPath);
                    }
                }

                var after = new FileInfo(path);
                after.Refresh();
                if (!after.Exists || after.Length != length ||
                    after.LastWriteTimeUtc != writeTime)
                {
                    throw new InteropFailure(
                        XccSyntheticInteropDiagnosticCode.ExtractionChanged,
                        "A fixed staged archive changed during its bounded read.",
                        logicalPath);
                }

                return bytes;
            }
            catch (InteropFailure)
            {
                throw;
            }
            catch (Exception exception) when (
                IsExpectedFileException(exception) || exception is OverflowException)
            {
                throw new InteropFailure(
                    XccSyntheticInteropDiagnosticCode.ArchiveReadFailed,
                    "A fixed staged archive could not be read safely.",
                    logicalPath);
            }
        }

        private static string ToHex(byte[] value)
        {
            return BitConverter.ToString(value)
                .Replace("-", string.Empty)
                .ToLowerInvariant();
        }

        internal static bool TryDeleteOwnedDirectory(
            string path,
            string approvedParent,
            bool requireStagingSuffix)
        {
            if (string.IsNullOrEmpty(path))
            {
                return true;
            }

            try
            {
                string fullPath = RepositoryPathPolicy.NormalizeAbsolutePath(path);
                string fullApprovedParent =
                    RepositoryPathPolicy.NormalizeAbsolutePath(approvedParent);
                if (!RepositoryPathPolicy.IsInsideOrEqual(fullPath, fullApprovedParent) ||
                    string.Equals(fullPath, fullApprovedParent, PathComparison) ||
                    (requireStagingSuffix &&
                     !Path.GetFileName(fullPath).EndsWith(
                         ".ra2yr-stage",
                         StringComparison.Ordinal)))
                {
                    return false;
                }

                string reparsePoint;
                if (RepositoryPathPolicy.ContainsExistingReparsePoint(
                    fullPath,
                    out reparsePoint))
                {
                    return false;
                }

                if (File.Exists(fullPath))
                {
                    return false;
                }

                if (Directory.Exists(fullPath))
                {
                    if (ContainsDescendantReparsePoint(fullPath))
                    {
                        return false;
                    }

                    Directory.Delete(fullPath, true);
                }

                return !Directory.Exists(fullPath) && !File.Exists(fullPath);
            }
            catch (Exception exception) when (
                IsExpectedFileException(exception) || exception is ArgumentException)
            {
                return false;
            }
        }

        private static bool ContainsDescendantReparsePoint(string root)
        {
            const int maximumCleanupEntries = 4096;
            var pending = new Stack<string>();
            pending.Push(root);
            int observedEntries = 0;
            while (pending.Count != 0)
            {
                string directory = pending.Pop();
                foreach (string path in Directory.EnumerateFileSystemEntries(directory))
                {
                    if (observedEntries >= maximumCleanupEntries)
                    {
                        return true;
                    }

                    observedEntries++;
                    FileAttributes attributes = File.GetAttributes(path);
                    if ((attributes & FileAttributes.ReparsePoint) != 0)
                    {
                        return true;
                    }

                    if ((attributes & FileAttributes.Directory) != 0)
                    {
                        pending.Push(path);
                    }
                }
            }

            return false;
        }

        private static XccSyntheticInteropResult FailureAfterCleanup(
            XccSyntheticInteropStage stage,
            string caseId,
            XccSyntheticInteropDiagnosticCode code,
            string message,
            string cacheRelativePath,
            params CleanupTarget[] cleanupTargets)
        {
            var diagnostics = new List<XccSyntheticInteropDiagnostic>
            {
                new XccSyntheticInteropDiagnostic(code, message, cacheRelativePath)
            };
            int cleanupFailures = 0;
            foreach (CleanupTarget target in cleanupTargets ?? Array.Empty<CleanupTarget>())
            {
                if (target != null &&
                    !TryDeleteOwnedDirectory(
                        target.Path,
                        target.ApprovedParent,
                        target.RequireStagingSuffix))
                {
                    cleanupFailures = checked(cleanupFailures + 1);
                }
            }

            if (cleanupFailures != 0)
            {
                diagnostics.Add(new XccSyntheticInteropDiagnostic(
                    XccSyntheticInteropDiagnosticCode.CleanupFailed,
                    "One or more synthetic interop directories failed controlled cleanup."));
            }

            return XccSyntheticInteropResult.Failure(stage, caseId, diagnostics);
        }

        private static XccSyntheticInteropResult Failure(
            XccSyntheticInteropStage stage,
            string caseId,
            XccSyntheticInteropDiagnosticCode code,
            string message,
            string cacheRelativePath = null)
        {
            return XccSyntheticInteropResult.Failure(
                stage,
                caseId,
                new XccSyntheticInteropDiagnostic(code, message, cacheRelativePath));
        }

        private static bool IsExpectedFileException(Exception exception)
        {
            return exception is IOException ||
                   exception is UnauthorizedAccessException ||
                   exception is SecurityException ||
                   exception is NotSupportedException ||
                   exception is PathTooLongException;
        }

        private static StringComparison PathComparison =>
            Path.DirectorySeparatorChar == '\\'
                ? StringComparison.OrdinalIgnoreCase
                : StringComparison.Ordinal;

        private sealed class OperationContext
        {
            public OperationContext(
                string cacheRoot,
                string interopRoot,
                string caseRoot,
                string caseRelativePath)
            {
                CacheRoot = cacheRoot;
                InteropRoot = interopRoot;
                CaseRoot = caseRoot;
                CaseRelativePath = caseRelativePath;
                CaseId = Path.GetFileName(caseRoot);
            }

            public string CacheRoot { get; }

            public string InteropRoot { get; }

            public string CaseRoot { get; }

            public string CaseRelativePath { get; }

            public string CaseId { get; }
        }

        private sealed class SyntheticPayloadSpec
        {
            public SyntheticPayloadSpec(string name, byte[] payload)
            {
                Name = name;
                Payload = (byte[])(payload ?? throw new ArgumentNullException(nameof(payload))).Clone();
                Id = MixFileId.ComputeCandidateId(name);
                Sha256 = HashBytes(Payload);
            }

            public string Name { get; }

            public byte[] Payload { get; }

            public MixFileId Id { get; }

            public string Sha256 { get; }
        }

        private sealed class ArchiveOutput
        {
            public ArchiveOutput(
                string role,
                string relativePath,
                byte[] bytes,
                string sha256,
                MixWriteHeaderKind headerKind,
                bool checksum,
                bool encrypted,
                IReadOnlyList<SyntheticPayloadSpec> entries)
            {
                Role = role;
                RelativePath = relativePath;
                Bytes = bytes;
                Sha256 = sha256;
                HeaderKind = headerKind;
                Checksum = checksum;
                Encrypted = encrypted;
                Entries = entries;
            }

            public string Role { get; }

            public string RelativePath { get; }

            public byte[] Bytes { get; }

            public string Sha256 { get; }

            public MixWriteHeaderKind HeaderKind { get; }

            public bool Checksum { get; }

            public bool Encrypted { get; }

            public IReadOnlyList<SyntheticPayloadSpec> Entries { get; }
        }

        private sealed class PrepareLayout
        {
            public List<ArchiveOutput> Archives { get; } = new List<ArchiveOutput>();

            public List<XccSyntheticInteropArtifact> Artifacts { get; } =
                new List<XccSyntheticInteropArtifact>();
        }

        private sealed class ObservedEntry
        {
            public ObservedEntry(MixFileId id, byte[] payload)
            {
                Id = id;
                Payload = payload;
            }

            public MixFileId Id { get; }

            public byte[] Payload { get; }
        }

        private sealed class ObservedArchive
        {
            public ObservedArchive(
                MixArchiveHeaderKind headerKind,
                bool hasChecksum,
                bool isEncrypted,
                byte[] keySource,
                long length,
                string sha256,
                IReadOnlyList<ObservedEntry> entries)
            {
                HeaderKind = headerKind;
                HasChecksum = hasChecksum;
                IsEncrypted = isEncrypted;
                KeySource = keySource;
                Length = length;
                Sha256 = sha256;
                Entries = entries;
            }

            public MixArchiveHeaderKind HeaderKind { get; }

            public bool HasChecksum { get; }

            public bool IsEncrypted { get; }

            public byte[] KeySource { get; }

            public long Length { get; }

            public string Sha256 { get; }

            public IReadOnlyList<ObservedEntry> Entries { get; }
        }

        private sealed class ExtractionContract
        {
            public ExtractionContract(
                string role,
                string inputArchiveRelativePath,
                long inputArchiveLength,
                string inputArchiveSha256,
                string extractionRelativeDirectory,
                IReadOnlyList<SyntheticPayloadSpec> expectedPayloads)
            {
                Role = role;
                InputArchiveRelativePath = inputArchiveRelativePath;
                InputArchiveLength = inputArchiveLength;
                InputArchiveSha256 = inputArchiveSha256;
                ExtractionRelativeDirectory = extractionRelativeDirectory;
                ExpectedPayloads = expectedPayloads;
            }

            public string Role { get; }

            public string InputArchiveRelativePath { get; }

            public long InputArchiveLength { get; }

            public string InputArchiveSha256 { get; }

            public string ExtractionRelativeDirectory { get; }

            public IReadOnlyList<SyntheticPayloadSpec> ExpectedPayloads { get; }
        }

        private sealed class PendingExtractionDirectory
        {
            public PendingExtractionDirectory(string path, string relativePath, int depth)
            {
                Path = path;
                RelativePath = relativePath;
                Depth = depth;
            }

            public string Path { get; }

            public string RelativePath { get; }

            public int Depth { get; }
        }

        private sealed class ExtractionSnapshotEntry
        {
            public ExtractionSnapshotEntry(string relativePath, long length, string sha256)
            {
                RelativePath = relativePath;
                Length = length;
                Sha256 = sha256;
            }

            public string RelativePath { get; }

            public long Length { get; }

            public string Sha256 { get; }
        }

        private sealed class ExtractionDirectorySnapshot
        {
            public ExtractionDirectorySnapshot(
                IEnumerable<string> filePaths,
                IEnumerable<string> directoryPaths,
                string fingerprint)
            {
                FilePaths = Array.AsReadOnly(filePaths.ToArray());
                DirectoryPaths = Array.AsReadOnly(directoryPaths.ToArray());
                Fingerprint = fingerprint;
            }

            public IReadOnlyList<string> FilePaths { get; }

            public IReadOnlyList<string> DirectoryPaths { get; }

            public string Fingerprint { get; }
        }

        private sealed class CleanupTarget
        {
            public CleanupTarget(
                string path,
                string approvedParent,
                bool requireStagingSuffix)
            {
                Path = path;
                ApprovedParent = approvedParent;
                RequireStagingSuffix = requireStagingSuffix;
            }

            public string Path { get; }

            public string ApprovedParent { get; }

            public bool RequireStagingSuffix { get; }
        }

        private sealed class InteropFailure : Exception
        {
            public InteropFailure(
                XccSyntheticInteropDiagnosticCode code,
                string publicMessage,
                string cacheRelativePath = null)
            {
                Code = code;
                PublicMessage = publicMessage;
                CacheRelativePath = cacheRelativePath;
            }

            public XccSyntheticInteropDiagnosticCode Code { get; }

            public string PublicMessage { get; }

            public string CacheRelativePath { get; }
        }
    }
}
