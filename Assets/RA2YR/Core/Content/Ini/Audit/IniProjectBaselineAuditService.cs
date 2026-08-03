using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Security.Cryptography;
using RA2YR.Core.Binary;
using RA2YR.Core.Content.Mix;
using RA2YR.Core.Formats.Ini;
using RA2YR.Core.Formats.Mix;

namespace RA2YR.Core.Content.Ini.Audit
{
    public static class IniProjectBaselineAuditService
    {
        public const string BaselineLogicalName = "YR1001_ProjectBaseline";

        public static IniProjectBaselineAuditDelivery Run(
            ExternalContentConfiguration configuration)
        {
            return RunCore(
                configuration,
                IniProjectBaselineAuditProfile.ProjectBaseline,
                value => new ContentIndexer().Build(value),
                () => DateTime.UtcNow,
                null);
        }

        public static IniRuntimeProjectBaselineAuditDelivery RunRuntimeResolutionAudit(
            ExternalContentConfiguration configuration)
        {
            IniProjectBaselineAuditModel observedModel = null;
            IniProjectBaselineAuditDelivery baseDelivery = RunCore(
                configuration,
                IniProjectBaselineAuditProfile.ProjectBaseline,
                value => new ContentIndexer().Build(value),
                () => DateTime.UtcNow,
                model => observedModel = model);
            if (observedModel == null)
            {
                throw new InvalidOperationException(
                    "The runtime INI audit did not receive a complete baseline model.");
            }

            string summary = IniRuntimeProjectBaselineAuditSerializer.SerializeSanitizedSummary(
                observedModel,
                baseDelivery);
            return new IniRuntimeProjectBaselineAuditDelivery(summary);
        }

        internal static IniProjectBaselineAuditDelivery RunForTesting(
            ExternalContentConfiguration configuration,
            IniProjectBaselineAuditProfile profile,
            Func<ExternalContentConfiguration, ContentIndexResult> buildIndex,
            Func<DateTime> utcNow)
        {
            return RunCore(configuration, profile, buildIndex, utcNow, null);
        }

        internal static IniRuntimeProjectBaselineAuditDelivery
            RunRuntimeResolutionAuditForTesting(
                ExternalContentConfiguration configuration,
                IniProjectBaselineAuditProfile profile,
                Func<ExternalContentConfiguration, ContentIndexResult> buildIndex,
                Func<DateTime> utcNow)
        {
            IniProjectBaselineAuditModel observedModel = null;
            IniProjectBaselineAuditDelivery baseDelivery = RunCore(
                configuration,
                profile,
                buildIndex,
                utcNow,
                model => observedModel = model);
            if (observedModel == null)
            {
                throw new InvalidOperationException(
                    "The runtime INI test audit did not receive a complete model.");
            }

            return new IniRuntimeProjectBaselineAuditDelivery(
                IniRuntimeProjectBaselineAuditSerializer.SerializeSanitizedSummary(
                    observedModel,
                    baseDelivery));
        }

        private static IniProjectBaselineAuditDelivery RunCore(
            ExternalContentConfiguration configuration,
            IniProjectBaselineAuditProfile profile,
            Func<ExternalContentConfiguration, ContentIndexResult> buildIndex,
            Func<DateTime> utcNow,
            Action<IniProjectBaselineAuditModel> modelObserver)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            if (profile == null || buildIndex == null || utcNow == null)
            {
                throw new ArgumentNullException(
                    profile == null ? nameof(profile) :
                    buildIndex == null ? nameof(buildIndex) : nameof(utcNow));
            }

            ExternalContentSourceDescriptor source = ValidateBaselineConfiguration(configuration);
            DateTime startedUtc = utcNow().ToUniversalTime();
            ContentIndexResult beforeIndex = BuildCompleteIndex(configuration, buildIndex);
            ContentSourceIndex beforeSource = GetBaselineSource(beforeIndex);
            IReadOnlyList<ContentFileRecord> rootFiles = GetExactRoots(beforeSource, profile);
            RejectLooseCandidates(beforeSource, profile);

            var mounts = new List<MixVirtualContentMountResult>();
            IniProjectBaselineAuditDelivery delivery = null;
            Exception operationFailure = null;
            try
            {
                MixNameCatalog catalog = CreateNameCatalog(profile);
                foreach (ContentFileRecord root in rootFiles)
                {
                    MixVirtualContentMountResult mount = MixVirtualContentSource.MountDirectorySource(
                        beforeSource,
                        new[] { root.LogicalPath },
                        catalog,
                        MixArchiveCatalogAdapters.ReadWithCoreReader,
                        profile.MountLimits,
                        MixMountIndexMode.StructureOnly);
                    mounts.Add(mount);
                    ValidateMount(mount, root.LogicalPath);
                }

                PendingSample[] pendingSamples = profile.Specifications
                    .Select(specification => ReadGoldenIni(
                        mounts,
                        source,
                        specification,
                        profile.ReadLimits,
                        profile.MaxIdentityOutputBytes))
                    .ToArray();
                IniSurveyCandidate[] surveyCandidates = BuildSurvey(
                    mounts,
                    profile.SurveyNames);
                LogicalContentPath[] unresolvedSurvey = profile.SurveyNames
                    .Where(name => surveyCandidates.All(candidate =>
                        !candidate.LogicalName.Equals(name)))
                    .OrderBy(name => name.Value, StringComparer.OrdinalIgnoreCase)
                    .ToArray();

                ContentIndexResult afterIndex = BuildCompleteIndex(configuration, buildIndex);
                ContentSourceIndex afterSource = GetBaselineSource(afterIndex);
                if (!string.Equals(
                        beforeSource.Fingerprint,
                        afterSource.Fingerprint,
                        StringComparison.Ordinal))
                {
                    throw Failure(
                        IniProjectBaselineAuditFailureCode.BaselineChangedDuringAudit,
                        "The controlled baseline fingerprint changed during the INI audit.");
                }

                IniGoldenSampleRecord[] samples = pendingSamples
                    .Select(pending => PublishAndVerifyIdentity(
                        configuration,
                        source,
                        beforeSource.Fingerprint,
                        pending,
                        profile))
                    .ToArray();
                DateTime completedUtc = utcNow().ToUniversalTime();
                var model = new IniProjectBaselineAuditModel(
                    source,
                    beforeSource.Fingerprint,
                    samples,
                    surveyCandidates,
                    unresolvedSurvey,
                    startedUtc,
                    completedUtc);
                modelObserver?.Invoke(model);
                byte[] externalBytes =
                    IniProjectBaselineAuditSerializer.SerializeExternalManifestUtf8(
                        model,
                        profile.MaxExternalManifestUtf8Bytes);
                IniAuditExternalManifestReference externalManifest =
                    IniAuditExternalArtifactWriter.WriteManifest(
                        configuration,
                        source.Id,
                        beforeSource.Fingerprint,
                        externalBytes);
                string summary = IniProjectBaselineAuditSerializer.SerializeSanitizedSummary(
                    model,
                    externalManifest);
                delivery = new IniProjectBaselineAuditDelivery(
                    samples.Length,
                    surveyCandidates.Length,
                    summary,
                    externalManifest.CacheRelativePath,
                    externalManifest.Length,
                    externalManifest.Sha256);
            }
            catch (Exception exception)
            {
                operationFailure = exception;
            }

            int cleanupFailureCount = DisposeAll(mounts);
            ThrowAfterCleanup(operationFailure, cleanupFailureCount);
            return delivery ?? throw new InvalidOperationException(
                "The INI baseline audit ended without a delivery or failure.");
        }

        private static PendingSample ReadGoldenIni(
            IEnumerable<MixVirtualContentMountResult> mounts,
            ExternalContentSourceDescriptor source,
            IniGoldenSampleSpecification specification,
            IniReadLimits readLimits,
            long maxIdentityOutputBytes)
        {
            MixVirtualEntry[] idMatches = mounts
                .Where(mount => mount.Archives[0].LogicalPath.Equals(
                    specification.RootArchive))
                .SelectMany(mount => mount.Entries)
                .Where(entry => entry.Id == specification.ExpectedMixId)
                .ToArray();
            MixVirtualEntry[] matches = idMatches
                .Where(entry => EntryMatchesExpectedProvenance(entry, source, specification))
                .ToArray();
            if (matches.Length == 0)
            {
                throw Failure(
                    idMatches.Length == 0
                        ? IniProjectBaselineAuditFailureCode.GoldenTargetMissing
                        : IniProjectBaselineAuditFailureCode.GoldenTargetProvenanceMismatch,
                    "A fixed ProjectBaseline INI target was not found through its exact MIX chain.");
            }

            if (matches.Length != 1)
            {
                throw Failure(
                    IniProjectBaselineAuditFailureCode.GoldenTargetAmbiguous,
                    "A fixed ProjectBaseline INI target resolved more than once in its exact MIX chain.");
            }

            MixVirtualEntry entry = matches[0];
            if (entry.LogicalName == null ||
                !entry.LogicalName.Equals(specification.LogicalName) ||
                entry.IsMountedArchive)
            {
                throw Failure(
                    IniProjectBaselineAuditFailureCode.GoldenTargetIdentityMismatch,
                    "A fixed INI target does not match its expected MIX identity.");
            }

            if (entry.Length != specification.ExpectedLength)
            {
                throw Failure(
                    IniProjectBaselineAuditFailureCode.GoldenTargetLengthMismatch,
                    "A fixed ProjectBaseline INI length changed.");
            }

            string payloadSha256 = ComputeWindowSha256(entry);
            if (!string.Equals(
                    payloadSha256,
                    specification.ExpectedSha256,
                    StringComparison.Ordinal))
            {
                throw Failure(
                    IniProjectBaselineAuditFailureCode.GoldenTargetHashMismatch,
                    "A fixed ProjectBaseline INI SHA-256 changed.");
            }

            IniAuditProvenance provenance = CopySanitizedProvenance(entry.Provenance);
            IniSourceProvenance parserProvenance = CreateParserProvenance(provenance);
            var sourceContext = new BinarySourceContext(
                "format.ini-byte-document",
                source.Id,
                specification.LogicalName);
            IniParseResult parse = WestwoodIniReader.Read(
                entry.PayloadWindow,
                sourceContext,
                parserProvenance,
                readLimits);
            if (!parse.IsSuccess)
            {
                throw Failure(
                    IniProjectBaselineAuditFailureCode.IniParseFailed,
                    "A fixed ProjectBaseline INI failed lossless bounded parsing.");
            }

            if (specification.ExpectedCanonicalModelSha256 != null &&
                !string.Equals(
                    parse.Document.CanonicalModelSha256,
                    specification.ExpectedCanonicalModelSha256,
                    StringComparison.Ordinal))
            {
                throw Failure(
                    IniProjectBaselineAuditFailureCode.IniParseFailed,
                    "A fixed ProjectBaseline INI canonical model changed.");
            }

            IniIdentityWriteResult identity = IniIdentityWriter.WriteToBytes(
                parse.Document,
                maxIdentityOutputBytes);
            if (!identity.IsSuccess || identity.Diagnostics.Count != 0 ||
                identity.Length != specification.ExpectedLength ||
                !string.Equals(identity.Sha256, payloadSha256, StringComparison.Ordinal))
            {
                throw Failure(
                    IniProjectBaselineAuditFailureCode.IdentityWriteFailed,
                    "The unmodified INI identity writer did not reproduce the fixed payload.");
            }

            return new PendingSample(
                specification,
                provenance,
                parse.Document,
                parse.Diagnostics,
                identity.GetBytes(),
                payloadSha256);
        }

        private static IniGoldenSampleRecord PublishAndVerifyIdentity(
            ExternalContentConfiguration configuration,
            ExternalContentSourceDescriptor source,
            string directoryFingerprint,
            PendingSample pending,
            IniProjectBaselineAuditProfile profile)
        {
            IniIdentityArtifactReference artifact =
                IniAuditExternalArtifactWriter.WriteIdentity(
                    configuration,
                    source.Id,
                    directoryFingerprint,
                    pending.Specification.SampleId,
                    pending.IdentityBytes);
            byte[] readBack = IniAuditExternalArtifactWriter.ReadIdentity(
                configuration,
                artifact,
                profile.MaxIdentityOutputBytes);
            bool byteIdentical = readBack.SequenceEqual(pending.IdentityBytes) &&
                string.Equals(artifact.Sha256, pending.PayloadSha256, StringComparison.Ordinal);
            IniParseResult reparsed = WestwoodIniReader.Read(
                readBack,
                new BinarySourceContext(
                    "format.ini-byte-document.identity-verification",
                    source.Id,
                    pending.Specification.LogicalName),
                CreateParserProvenance(pending.Provenance),
                profile.ReadLimits);
            if (!byteIdentical || !reparsed.IsSuccess ||
                !string.Equals(
                    reparsed.Document.CanonicalModelSha256,
                    pending.Document.CanonicalModelSha256,
                    StringComparison.Ordinal))
            {
                throw Failure(
                    IniProjectBaselineAuditFailureCode.IdentityVerificationFailed,
                    "A published INI identity artifact failed byte or model verification.");
            }

            return BuildRecord(pending, artifact, byteIdentical);
        }

        private static IniGoldenSampleRecord BuildRecord(
            PendingSample pending,
            IniIdentityArtifactReference artifact,
            bool byteIdentical)
        {
            IniRawDocument document = pending.Document;
            int crlf = document.Lines.Count(line =>
                line.EndingKind == IniLineEnding.CarriageReturnLineFeed);
            int lf = document.Lines.Count(line =>
                line.EndingKind == IniLineEnding.LineFeed);
            int cr = document.Lines.Count(line =>
                line.EndingKind == IniLineEnding.CarriageReturn);
            int none = document.Lines.Count(line =>
                line.EndingKind == IniLineEnding.None);
            int sections = document.Nodes.Count(node => node.Kind == IniNodeKind.Section);
            int keys = document.Nodes.Count(node => node.Kind == IniNodeKind.KeyValue);
            int comments = document.Nodes.Count(node => node.Kind == IniNodeKind.Comment);
            int blanks = document.Nodes.Count(node => node.Kind == IniNodeKind.Blank);
            int opaque = document.Nodes.Count(node => node.Kind == IniNodeKind.Opaque);
            int maxLine = document.Lines.Count == 0
                ? 0
                : document.Lines.Max(line => line.Content.Length);

            int duplicateSections = CountDuplicateSections(document);
            int duplicateKeys = CountDuplicateKeys(document);
            IReadOnlyDictionary<string, int> diagnostics = pending.Diagnostics
                .GroupBy(value => value.Code.ToString(), StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);
            IniLineAuditRecord[] lineRecords = document.Lines
                .Select((line, index) =>
                {
                    IniNode node = document.Nodes[index];
                    var opaqueNode = node as IniOpaqueNode;
                    return new IniLineAuditRecord(
                        line.Id,
                        line.AbsoluteOffset,
                        line.Content.Length,
                        line.Ending.Length,
                        line.EndingKind,
                        node.Kind,
                        opaqueNode == null ? (IniOpaqueReason?)null : opaqueNode.Reason,
                        ComputeSha256(line.FullRaw.ToArray()));
                })
                .ToArray();

            return new IniGoldenSampleRecord(
                pending.Specification,
                pending.Provenance,
                document,
                pending.Specification.ExpectedLength,
                pending.PayloadSha256,
                artifact.Sha256,
                artifact.CacheRelativePath,
                byteIdentical,
                crlf,
                lf,
                cr,
                none,
                sections,
                keys,
                comments,
                blanks,
                opaque,
                duplicateSections,
                duplicateKeys,
                maxLine,
                diagnostics,
                lineRecords);
        }

        private static int CountDuplicateSections(IniRawDocument document)
        {
            var seen = new HashSet<byte[]>(ByteArrayComparer.Instance);
            int duplicates = 0;
            foreach (IniSectionNode section in document.Nodes.OfType<IniSectionNode>())
            {
                if (!seen.Add(section.RawName.ToArray()))
                {
                    duplicates = checked(duplicates + 1);
                }
            }

            return duplicates;
        }

        private static int CountDuplicateKeys(IniRawDocument document)
        {
            var bySection = new Dictionary<int, HashSet<byte[]>>();
            int duplicates = 0;
            foreach (IniKeyValueNode key in document.Nodes.OfType<IniKeyValueNode>())
            {
                HashSet<byte[]> keys;
                if (!bySection.TryGetValue(key.ContainingSectionLineId, out keys))
                {
                    keys = new HashSet<byte[]>(ByteArrayComparer.Instance);
                    bySection.Add(key.ContainingSectionLineId, keys);
                }

                byte[] rawKey = key.Line.Content.Span
                    .Slice(
                        key.LeadingWhitespace.Length,
                        checked(key.Key.Length + key.WhitespaceBeforeEquals.Length))
                    .ToArray();
                if (!keys.Add(rawKey))
                {
                    duplicates = checked(duplicates + 1);
                }
            }

            return duplicates;
        }

        private static IniSurveyCandidate[] BuildSurvey(
            IEnumerable<MixVirtualContentMountResult> mounts,
            IEnumerable<LogicalContentPath> surveyNames)
        {
            LogicalContentPath[] names = surveyNames.ToArray();
            return mounts
                .SelectMany(mount => mount.Entries)
                .Where(entry => entry.LogicalName != null &&
                                names.Any(name => name.Equals(entry.LogicalName)) &&
                                !entry.IsMountedArchive)
                .Select(entry => new IniSurveyCandidate(
                    entry.LogicalName,
                    entry.Id,
                    CopySanitizedProvenance(entry.Provenance),
                    entry.Length,
                    ComputeWindowSha256(entry)))
                .OrderBy(candidate => candidate.LogicalName.Value, StringComparer.OrdinalIgnoreCase)
                .ThenBy(candidate => candidate.Provenance.RootArchive.Value,
                    StringComparer.OrdinalIgnoreCase)
                .ThenBy(candidate => string.Join(
                    "/",
                    candidate.Provenance.Layers.Select(layer => layer.ResolvedName.Value)),
                    StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        private static bool EntryMatchesExpectedProvenance(
            MixVirtualEntry entry,
            ExternalContentSourceDescriptor source,
            IniGoldenSampleSpecification specification)
        {
            MixEntryProvenance provenance = entry.Provenance;
            if (!string.Equals(provenance.Source.Id, source.Id, StringComparison.Ordinal) ||
                !provenance.RootArchivePath.Equals(specification.RootArchive))
            {
                return false;
            }

            if (specification.NestedArchive == null)
            {
                return provenance.Steps.Count == 1 &&
                       MatchesStep(
                           provenance.Steps[0],
                           specification.RootArchive,
                           specification.ExpectedMixId,
                           specification.LogicalName);
            }

            LogicalContentPath nestedPath = LogicalContentPath.Parse(
                specification.RootArchive.Value + "/" + specification.NestedArchive.Value);
            return provenance.Steps.Count == 2 &&
                   provenance.Steps[0].ArchivePath.Equals(specification.RootArchive) &&
                   provenance.Steps[0].ResolvedName != null &&
                   provenance.Steps[0].ResolvedName.Equals(specification.NestedArchive) &&
                   MatchesStep(
                       provenance.Steps[1],
                       nestedPath,
                       specification.ExpectedMixId,
                       specification.LogicalName);
        }

        private static bool MatchesStep(
            MixArchiveProvenanceStep step,
            LogicalContentPath archive,
            MixFileId id,
            LogicalContentPath name)
        {
            return step.ArchivePath.Equals(archive) &&
                   step.EntryId == id &&
                   step.ResolvedName != null &&
                   step.ResolvedName.Equals(name);
        }

        private static IniAuditProvenance CopySanitizedProvenance(
            MixEntryProvenance provenance)
        {
            return new IniAuditProvenance(
                provenance.Source.Id,
                provenance.RootArchivePath,
                provenance.Steps.Select(step => new IniAuditProvenanceLayer(
                    step.ArchivePath,
                    step.EntryId,
                    step.ResolvedName)));
        }

        private static IniSourceProvenance CreateParserProvenance(
            IniAuditProvenance provenance)
        {
            return new IniSourceProvenance(
                provenance.SourceId,
                new[] { provenance.RootArchive }.Concat(
                    provenance.Layers.Select(layer => layer.ResolvedName)));
        }

        private static string ComputeWindowSha256(MixVirtualEntry entry)
        {
            try
            {
                return entry.PayloadWindow.ComputeSha256("ini-audit-payload-hash");
            }
            catch (Exception exception) when (IsExpectedReadException(exception))
            {
                throw Failure(
                    IniProjectBaselineAuditFailureCode.GoldenTargetHashMismatch,
                    "An INI MIX entry could not be hashed safely.");
            }
        }

        private static ExternalContentSourceDescriptor ValidateBaselineConfiguration(
            ExternalContentConfiguration configuration)
        {
            ExternalContentSourceDescriptor[] enabled = configuration.Sources
                .Where(source => source.Enabled)
                .ToArray();
            if (enabled.Length != 1 ||
                !string.Equals(enabled[0].Id, BaselineLogicalName, StringComparison.Ordinal) ||
                enabled[0].Kind != ContentSourceKind.Patched)
            {
                throw Failure(
                    IniProjectBaselineAuditFailureCode.InvalidBaselineConfiguration,
                    "The INI audit requires exactly one enabled patched YR1001_ProjectBaseline source.");
            }

            return enabled[0];
        }

        private static ContentIndexResult BuildCompleteIndex(
            ExternalContentConfiguration configuration,
            Func<ExternalContentConfiguration, ContentIndexResult> buildIndex)
        {
            ContentIndexResult result;
            try
            {
                result = buildIndex(configuration);
            }
            catch (Exception exception) when (IsExpectedReadException(exception))
            {
                throw Failure(
                    IniProjectBaselineAuditFailureCode.DirectoryIndexIncomplete,
                    "The controlled baseline directory index failed closed.");
            }

            if (result == null || !result.IsComplete || result.HasErrors)
            {
                throw Failure(
                    IniProjectBaselineAuditFailureCode.DirectoryIndexIncomplete,
                    "The controlled baseline directory index is incomplete.");
            }

            return result;
        }

        private static ContentSourceIndex GetBaselineSource(ContentIndexResult index)
        {
            ContentSourceIndex[] matches = index.Sources
                .Where(source => string.Equals(
                    source.Source.Id,
                    BaselineLogicalName,
                    StringComparison.Ordinal))
                .ToArray();
            if (matches.Length != 1 || !matches[0].IsComplete)
            {
                throw Failure(
                    IniProjectBaselineAuditFailureCode.DirectoryIndexIncomplete,
                    "The controlled baseline source index is incomplete.");
            }

            return matches[0];
        }

        private static IReadOnlyList<ContentFileRecord> GetExactRoots(
            ContentSourceIndex source,
            IniProjectBaselineAuditProfile profile)
        {
            var result = new List<ContentFileRecord>();
            foreach (LogicalContentPath root in profile.RootArchives)
            {
                ContentFileRecord[] matches = source.Files
                    .Where(file => file.LogicalPath.Equals(root))
                    .ToArray();
                if (matches.Length != 1 ||
                    !string.Equals(matches[0].RelativePath, root.Value, StringComparison.Ordinal))
                {
                    throw Failure(
                        IniProjectBaselineAuditFailureCode.RootArchiveMissing,
                        "The controlled baseline does not contain an exact required root MIX archive.");
                }

                result.Add(matches[0]);
            }

            return result.AsReadOnly();
        }

        private static void RejectLooseCandidates(
            ContentSourceIndex source,
            IniProjectBaselineAuditProfile profile)
        {
            HashSet<LogicalContentPath> names = new HashSet<LogicalContentPath>(
                profile.Specifications.Select(specification => specification.LogicalName)
                    .Concat(profile.SurveyNames));
            if (source.Files.Any(file => names.Contains(file.LogicalPath)))
            {
                throw Failure(
                    IniProjectBaselineAuditFailureCode.LooseIniCandidateFound,
                    "The controlled INI audit requires its candidates to resolve only through MIX sources.");
            }
        }

        private static MixNameCatalog CreateNameCatalog(IniProjectBaselineAuditProfile profile)
        {
            return new MixNameCatalog(
                profile.Specifications.Select(specification => specification.LogicalName)
                    .Concat(profile.Specifications
                        .Where(specification => specification.NestedArchive != null)
                        .Select(specification => specification.NestedArchive))
                    .Concat(profile.NestedArchiveNames)
                    .Concat(profile.SurveyNames));
        }

        private static void ValidateMount(
            MixVirtualContentMountResult mount,
            LogicalContentPath root)
        {
            if (mount == null || !mount.IsComplete || mount.Diagnostics.Count != 0 ||
                mount.IndexMode != MixMountIndexMode.StructureOnly ||
                mount.Archives.Count == 0 ||
                !mount.Archives[0].LogicalPath.Equals(root))
            {
                throw Failure(
                    IniProjectBaselineAuditFailureCode.MixMountFailed,
                    "A bounded INI MIX mount did not complete with its fixed root archive.");
            }
        }

        private static int DisposeAll(IEnumerable<IDisposable> values)
        {
            int failures = 0;
            foreach (IDisposable value in values)
            {
                try
                {
                    value.Dispose();
                }
                catch (Exception)
                {
                    failures = checked(failures + 1);
                }
            }

            return failures;
        }

        private static void ThrowAfterCleanup(Exception operationFailure, int cleanupFailureCount)
        {
            if (operationFailure != null)
            {
                var structured = operationFailure as IniProjectBaselineAuditException;
                if (structured != null && cleanupFailureCount != 0)
                {
                    structured.RecordCleanupFailures(cleanupFailureCount);
                }

                ExceptionDispatchInfo.Capture(operationFailure).Throw();
            }

            if (cleanupFailureCount != 0)
            {
                throw Failure(
                    IniProjectBaselineAuditFailureCode.MountCleanupFailed,
                    "One or more controlled INI MIX mounts failed cleanup.",
                    cleanupFailureCount);
            }
        }

        private static bool IsExpectedReadException(Exception exception)
        {
            return exception is IOException ||
                   exception is UnauthorizedAccessException ||
                   exception is NotSupportedException ||
                   exception is InvalidDataException ||
                   exception is OverflowException ||
                   exception is System.Security.SecurityException ||
                   exception is BinaryReadException ||
                   exception is IniReadException;
        }

        private static string ComputeSha256(byte[] bytes)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                return Sha256Utilities.ToLowerHex(sha256.ComputeHash(bytes));
            }
        }

        private static IniProjectBaselineAuditException Failure(
            IniProjectBaselineAuditFailureCode code,
            string message,
            int cleanupFailures = 0)
        {
            return new IniProjectBaselineAuditException(code, message, cleanupFailures);
        }

        private sealed class PendingSample
        {
            public PendingSample(
                IniGoldenSampleSpecification specification,
                IniAuditProvenance provenance,
                IniRawDocument document,
                IReadOnlyList<IniDiagnostic> diagnostics,
                byte[] identityBytes,
                string payloadSha256)
            {
                Specification = specification;
                Provenance = provenance;
                Document = document;
                Diagnostics = diagnostics;
                IdentityBytes = identityBytes;
                PayloadSha256 = payloadSha256;
            }

            public IniGoldenSampleSpecification Specification { get; }
            public IniAuditProvenance Provenance { get; }
            public IniRawDocument Document { get; }
            public IReadOnlyList<IniDiagnostic> Diagnostics { get; }
            public byte[] IdentityBytes { get; }
            public string PayloadSha256 { get; }
        }

        private sealed class ByteArrayComparer : IEqualityComparer<byte[]>
        {
            public static ByteArrayComparer Instance { get; } = new ByteArrayComparer();

            public bool Equals(byte[] left, byte[] right)
            {
                if (ReferenceEquals(left, right))
                {
                    return true;
                }

                return left != null && right != null && left.SequenceEqual(right);
            }

            public int GetHashCode(byte[] value)
            {
                if (value == null)
                {
                    return 0;
                }

                unchecked
                {
                    int hash = 17;
                    foreach (byte item in value)
                    {
                        hash = (hash * 31) + item;
                    }

                    return hash;
                }
            }
        }
    }

    internal sealed class IniProjectBaselineAuditProfile
    {
        public IniProjectBaselineAuditProfile(
            IEnumerable<IniGoldenSampleSpecification> specifications,
            IEnumerable<string> surveyNames,
            IEnumerable<string> nestedArchiveNames,
            long maxExternalManifestUtf8Bytes,
            long maxIdentityOutputBytes,
            MixMountLimits mountLimits,
            IniReadLimits readLimits)
        {
            IniGoldenSampleSpecification[] specificationArray =
                (specifications ?? throw new ArgumentNullException(nameof(specifications))).ToArray();
            if (specificationArray.Length == 0 || specificationArray.Any(value => value == null) ||
                specificationArray.Select(value => value.SampleId).Distinct(StringComparer.Ordinal).Count() !=
                    specificationArray.Length ||
                maxExternalManifestUtf8Bytes <= 0 || maxIdentityOutputBytes <= 0)
            {
                throw new ArgumentException("The INI audit profile is invalid.");
            }

            Specifications = Array.AsReadOnly(specificationArray);
            SurveyNames = Array.AsReadOnly((surveyNames ?? Array.Empty<string>())
                .Select(LogicalContentPath.Parse)
                .Distinct()
                .ToArray());
            NestedArchiveNames = Array.AsReadOnly((nestedArchiveNames ?? Array.Empty<string>())
                .Select(LogicalContentPath.Parse)
                .Distinct()
                .ToArray());
            RootArchives = Array.AsReadOnly(specificationArray
                .Select(specification => specification.RootArchive)
                .Distinct()
                .OrderBy(path => path.Value, StringComparer.OrdinalIgnoreCase)
                .ToArray());
            MaxExternalManifestUtf8Bytes = maxExternalManifestUtf8Bytes;
            MaxIdentityOutputBytes = maxIdentityOutputBytes;
            MountLimits = mountLimits ?? throw new ArgumentNullException(nameof(mountLimits));
            ReadLimits = readLimits ?? throw new ArgumentNullException(nameof(readLimits));
        }

        public static IniProjectBaselineAuditProfile ProjectBaseline { get; } =
            new IniProjectBaselineAuditProfile(
                new[]
                {
                    new IniGoldenSampleSpecification(
                        "artmd-localmd",
                        "ra2md.mix",
                        "localmd.mix",
                        "artmd.ini",
                        336535,
                        "e1f0378394313c04ebbd5073f47785ee3e46f1b3c62d65724e8f3c310ee7ba31",
                        "d138e1443bb1797b95c23857de0fffc9900ffae6838b9cd79c42707af519a64d"),
                    new IniGoldenSampleSpecification(
                        "ai-local",
                        "ra2.mix",
                        "local.mix",
                        "ai.ini",
                        84972,
                        "1feac6ddea6886b177ddf7e5f8580b7a99a63f12684f2cbb42831671bb7a8a79",
                        "b41fec9d9331349126b32929abbf2d1d8e77ce3959a4cf2461c034324c72a361"),
                    new IniGoldenSampleSpecification(
                        "rulesmd-expandmd01",
                        "expandmd01.mix",
                        null,
                        "rulesmd.ini",
                        743215,
                        "3d341ef8a13a4b5ab24af2eef48ac94931ac2bb87d950fe3330a07e2d25672ef",
                        "86fa33b1c844101ce6facb8df50e254ceb784bafb45880e0ce2f55fc3738d287"),
                    new IniGoldenSampleSpecification(
                        "rulesmd-localmd",
                        "ra2md.mix",
                        "localmd.mix",
                        "rulesmd.ini",
                        742958,
                        "06761dd7f714e7d9400216ec3c06109ec5c1461f6a0727be7401eb9d8b0f6d05",
                        "b5f97e861fa620bf2af96060c8216965f682c5ae24ca50cdd6bde3219ab224e1")
                },
                new[]
                {
                    "aimd.ini", "rules.ini", "art.ini", "soundmd.ini", "evamd.ini",
                    "uimd.ini", "missionmd.ini", "temperat.ini", "snow.ini", "urban.ini",
                    "urbann.ini", "desert.ini", "lunar.ini"
                },
                new[] { "local.mix", "localmd.mix", "audio.mix", "isotemp.mix" },
                64L * 1024 * 1024,
                2L * 1024 * 1024,
                MixMountLimits.Default,
                IniReadLimits.Default);

        public IReadOnlyList<IniGoldenSampleSpecification> Specifications { get; }
        public IReadOnlyList<LogicalContentPath> SurveyNames { get; }
        public IReadOnlyList<LogicalContentPath> NestedArchiveNames { get; }
        public IReadOnlyList<LogicalContentPath> RootArchives { get; }
        public long MaxExternalManifestUtf8Bytes { get; }
        public long MaxIdentityOutputBytes { get; }
        public MixMountLimits MountLimits { get; }
        public IniReadLimits ReadLimits { get; }
    }
}
