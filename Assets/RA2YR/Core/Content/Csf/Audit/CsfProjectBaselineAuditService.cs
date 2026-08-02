using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.ExceptionServices;
using RA2YR.Core.Binary;
using RA2YR.Core.Content.Mix;
using RA2YR.Core.Formats.Csf;
using RA2YR.Core.Formats.Mix;

namespace RA2YR.Core.Content.Csf.Audit
{
    public static class CsfProjectBaselineAuditService
    {
        public const string BaselineLogicalName = "YR1001_ProjectBaseline";

        private static readonly LogicalContentPath RootArchivePath =
            LogicalContentPath.Parse("langmd.mix");
        private static readonly LogicalContentPath TargetPath =
            LogicalContentPath.Parse("ra2md.csf");

        public static CsfProjectBaselineAuditDelivery Run(
            ExternalContentConfiguration configuration)
        {
            return RunCore(
                configuration,
                CsfProjectBaselineAuditProfile.ProjectBaseline,
                value => new ContentIndexer().Build(value),
                () => DateTime.UtcNow);
        }

        internal static CsfProjectBaselineAuditDelivery RunForTesting(
            ExternalContentConfiguration configuration,
            CsfProjectBaselineAuditProfile profile,
            Func<ExternalContentConfiguration, ContentIndexResult> buildIndex,
            Func<DateTime> utcNow)
        {
            return RunCore(configuration, profile, buildIndex, utcNow);
        }

        private static CsfProjectBaselineAuditDelivery RunCore(
            ExternalContentConfiguration configuration,
            CsfProjectBaselineAuditProfile profile,
            Func<ExternalContentConfiguration, ContentIndexResult> buildIndex,
            Func<DateTime> utcNow)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            if (profile == null)
            {
                throw new ArgumentNullException(nameof(profile));
            }

            if (buildIndex == null)
            {
                throw new ArgumentNullException(nameof(buildIndex));
            }

            if (utcNow == null)
            {
                throw new ArgumentNullException(nameof(utcNow));
            }

            ExternalContentSourceDescriptor source = ValidateBaselineConfiguration(configuration);
            DateTime startedUtc = utcNow().ToUniversalTime();
            ContentIndexResult beforeIndex = BuildCompleteIndex(configuration, buildIndex);
            ContentSourceIndex beforeSource = GetBaselineSource(beforeIndex);
            ContentFileRecord rootFile = GetExactRootArchive(beforeSource);
            RejectLooseTarget(beforeSource);

            var nameCatalog = new MixNameCatalog(new[] { TargetPath });
            MixVirtualContentMountResult mount = null;
            CsfProjectBaselineAuditDelivery delivery = null;
            Exception operationFailure = null;
            try
            {
                mount = MixVirtualContentSource.MountDirectorySource(
                    beforeSource,
                    new[] { rootFile.LogicalPath },
                    nameCatalog,
                    MixArchiveCatalogAdapters.ReadWithCoreReader,
                    profile.MountLimits,
                    MixMountIndexMode.StructureOnly);
                ValidateMount(mount);
                CsfGoldenSampleRecord sample = ReadGoldenCsf(
                    mount,
                    source,
                    profile.Specification,
                    profile.ReadLimits);

                ContentIndexResult afterIndex = BuildCompleteIndex(configuration, buildIndex);
                ContentSourceIndex afterSource = GetBaselineSource(afterIndex);
                if (!string.Equals(
                        beforeSource.Fingerprint,
                        afterSource.Fingerprint,
                        StringComparison.Ordinal))
                {
                    throw Failure(
                        CsfProjectBaselineAuditFailureCode.BaselineChangedDuringAudit,
                        "The controlled baseline fingerprint changed during the CSF audit.");
                }

                DateTime completedUtc = utcNow().ToUniversalTime();
                var model = new CsfProjectBaselineAuditModel(
                    source,
                    beforeSource.Fingerprint,
                    sample,
                    startedUtc,
                    completedUtc);
                byte[] externalBytes =
                    CsfProjectBaselineAuditSerializer.SerializeExternalManifestUtf8(
                        model,
                        profile.MaxExternalManifestUtf8Bytes);
                CsfAuditExternalManifestReference externalManifest =
                    CsfAuditExternalManifestWriter.Write(
                        configuration,
                        source.Id,
                        beforeSource.Fingerprint,
                        externalBytes);
                string summary = CsfProjectBaselineAuditSerializer.SerializeSanitizedSummary(
                    model,
                    externalManifest);
                delivery = new CsfProjectBaselineAuditDelivery(
                    summary,
                    externalManifest.CacheRelativePath,
                    externalManifest.Length,
                    externalManifest.Sha256);
            }
            catch (Exception exception)
            {
                operationFailure = exception;
            }

            int cleanupFailureCount = DisposeMount(mount);
            ThrowAfterCleanup(operationFailure, cleanupFailureCount);
            return delivery ?? throw new InvalidOperationException(
                "The CSF baseline audit ended without a delivery or failure.");
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
                    CsfProjectBaselineAuditFailureCode.InvalidBaselineConfiguration,
                    "The CSF audit requires exactly one enabled patched YR1001_ProjectBaseline source.");
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
                    CsfProjectBaselineAuditFailureCode.DirectoryIndexIncomplete,
                    "The controlled baseline directory index failed closed.");
            }

            if (result == null || !result.IsComplete || result.HasErrors)
            {
                throw Failure(
                    CsfProjectBaselineAuditFailureCode.DirectoryIndexIncomplete,
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
                    CsfProjectBaselineAuditFailureCode.DirectoryIndexIncomplete,
                    "The controlled baseline source index is incomplete.");
            }

            return matches[0];
        }

        private static ContentFileRecord GetExactRootArchive(ContentSourceIndex source)
        {
            ContentFileRecord[] matches = source.Files
                .Where(file => file.LogicalPath.Equals(RootArchivePath))
                .ToArray();
            if (matches.Length != 1 ||
                !string.Equals(
                    matches[0].RelativePath,
                    RootArchivePath.Value,
                    StringComparison.Ordinal))
            {
                throw Failure(
                    CsfProjectBaselineAuditFailureCode.RootArchiveMissing,
                    "The controlled baseline does not contain exactly one canonical langmd.mix root archive.");
            }

            return matches[0];
        }

        private static void RejectLooseTarget(ContentSourceIndex source)
        {
            if (source.Files.Any(file => file.LogicalPath.Equals(TargetPath)))
            {
                throw Failure(
                    CsfProjectBaselineAuditFailureCode.LooseCsfCandidateFound,
                    "The golden CSF exists as a loose directory file instead of only through the fixed MIX chain.");
            }
        }

        private static void ValidateMount(MixVirtualContentMountResult mount)
        {
            if (mount == null || !mount.IsComplete || mount.Diagnostics.Count != 0 ||
                mount.IndexMode != MixMountIndexMode.StructureOnly ||
                mount.Archives.Count != 1 ||
                !mount.Archives[0].LogicalPath.Equals(RootArchivePath))
            {
                throw Failure(
                    CsfProjectBaselineAuditFailureCode.MixMountFailed,
                    "The bounded langmd.mix mount did not complete with the fixed archive set.");
            }
        }

        private static CsfGoldenSampleRecord ReadGoldenCsf(
            MixVirtualContentMountResult mount,
            ExternalContentSourceDescriptor source,
            CsfGoldenSampleSpecification specification,
            CsfReadLimits readLimits)
        {
            MixVirtualEntry[] matches = mount.Entries
                .Where(entry => entry.Id == specification.ExpectedMixId)
                .ToArray();
            if (matches.Length == 0)
            {
                throw Failure(
                    CsfProjectBaselineAuditFailureCode.TargetMissing,
                    "The fixed ProjectBaseline CSF target was not found.");
            }

            if (matches.Length != 1)
            {
                throw Failure(
                    CsfProjectBaselineAuditFailureCode.TargetAmbiguous,
                    "The fixed ProjectBaseline CSF target resolved to multiple MIX entries.");
            }

            MixVirtualEntry entry = matches[0];
            if (entry.LogicalName == null ||
                !entry.LogicalName.Equals(specification.LogicalName) ||
                entry.IsMountedArchive)
            {
                throw Failure(
                    CsfProjectBaselineAuditFailureCode.TargetIdentityMismatch,
                    "The fixed CSF target does not match its expected MIX identity.");
            }

            ValidateProvenance(entry, source, specification);
            if (entry.Length != specification.ExpectedLength)
            {
                throw Failure(
                    CsfProjectBaselineAuditFailureCode.TargetLengthMismatch,
                    "The fixed ProjectBaseline CSF length changed.");
            }

            string payloadSha256;
            try
            {
                payloadSha256 = entry.PayloadWindow.ComputeSha256(
                    "csf-golden-payload-hash");
            }
            catch (Exception exception) when (IsExpectedReadException(exception))
            {
                throw Failure(
                    CsfProjectBaselineAuditFailureCode.TargetHashMismatch,
                    "The fixed ProjectBaseline CSF could not be hashed safely.");
            }

            if (!string.Equals(
                    payloadSha256,
                    specification.ExpectedSha256,
                    StringComparison.Ordinal))
            {
                throw Failure(
                    CsfProjectBaselineAuditFailureCode.TargetHashMismatch,
                    "The fixed ProjectBaseline CSF SHA-256 changed.");
            }

            var parserProvenance = new CsfSourceProvenance(
                source.Id,
                new[] { RootArchivePath, specification.LogicalName });
            var sourceContext = new BinarySourceContext(
                "format.csf",
                source.Id,
                specification.LogicalName);
            CsfParseResult result = WestwoodCsfReader.Read(
                entry.PayloadWindow,
                sourceContext,
                parserProvenance,
                readLimits);
            if (!result.IsSuccess || result.Diagnostics.Count != 0)
            {
                throw Failure(
                    CsfProjectBaselineAuditFailureCode.CsfParseFailed,
                    "The fixed ProjectBaseline CSF failed strict bounded parsing.");
            }

            CsfDocument document = result.Document;
            if (!string.Equals(
                    document.CanonicalModelSha256,
                    specification.ExpectedNormalizedModelSha256,
                    StringComparison.Ordinal))
            {
                throw Failure(
                    CsfProjectBaselineAuditFailureCode.NormalizedModelHashMismatch,
                    "The fixed ProjectBaseline normalized CSF model changed.");
            }

            return BuildRecord(
                specification,
                CopySanitizedProvenance(entry.Provenance),
                entry.Length,
                payloadSha256,
                document,
                result.Diagnostics.Count);
        }

        private static CsfGoldenSampleRecord BuildRecord(
            CsfGoldenSampleSpecification specification,
            CsfGoldenProvenance provenance,
            long length,
            string sha256,
            CsfDocument document,
            int diagnosticCount)
        {
            int totalValues = 0;
            int normalValues = 0;
            int extendedValues = 0;
            int emptyValues = 0;
            int maximumValuesPerLabel = 0;
            int minimumLabelLength = document.Labels.Count == 0 ? 0 : int.MaxValue;
            int maximumLabelLength = 0;
            int minimumMainLength = 0;
            int maximumMainLength = 0;
            int minimumExtendedLength = 0;
            int maximumExtendedLength = 0;
            bool sawMain = false;
            bool sawExtended = false;
            var names = new HashSet<string>(StringComparer.Ordinal);
            int duplicateLabels = 0;

            foreach (CsfLabel label in document.Labels)
            {
                if (!names.Add(label.Name))
                {
                    duplicateLabels = checked(duplicateLabels + 1);
                }

                minimumLabelLength = Math.Min(minimumLabelLength, label.Name.Length);
                maximumLabelLength = Math.Max(maximumLabelLength, label.Name.Length);
                maximumValuesPerLabel = Math.Max(
                    maximumValuesPerLabel,
                    label.Values.Count);
                foreach (CsfValue value in label.Values)
                {
                    totalValues = checked(totalValues + 1);
                    if (!sawMain)
                    {
                        minimumMainLength = value.Text.Length;
                        sawMain = true;
                    }
                    else
                    {
                        minimumMainLength = Math.Min(
                            minimumMainLength,
                            value.Text.Length);
                    }

                    maximumMainLength = Math.Max(maximumMainLength, value.Text.Length);
                    if (value.Text.Length == 0)
                    {
                        emptyValues = checked(emptyValues + 1);
                    }

                    if (value.Kind == CsfValueKind.Normal)
                    {
                        normalValues = checked(normalValues + 1);
                    }
                    else
                    {
                        extendedValues = checked(extendedValues + 1);
                        int extraLength = value.ExtraText.Length;
                        if (!sawExtended)
                        {
                            minimumExtendedLength = extraLength;
                            sawExtended = true;
                        }
                        else
                        {
                            minimumExtendedLength = Math.Min(
                                minimumExtendedLength,
                                extraLength);
                        }

                        maximumExtendedLength = Math.Max(
                            maximumExtendedLength,
                            extraLength);
                    }
                }
            }

            return new CsfGoldenSampleRecord(
                specification,
                provenance,
                length,
                sha256,
                document,
                totalValues,
                normalValues,
                extendedValues,
                emptyValues,
                duplicateLabels,
                maximumValuesPerLabel,
                minimumLabelLength,
                maximumLabelLength,
                minimumMainLength,
                maximumMainLength,
                minimumExtendedLength,
                maximumExtendedLength,
                diagnosticCount);
        }

        private static void ValidateProvenance(
            MixVirtualEntry entry,
            ExternalContentSourceDescriptor source,
            CsfGoldenSampleSpecification specification)
        {
            MixEntryProvenance provenance = entry.Provenance;
            if (!string.Equals(provenance.Source.Id, source.Id, StringComparison.Ordinal) ||
                !provenance.RootArchivePath.Equals(RootArchivePath) ||
                provenance.Steps.Count != 1 ||
                !MatchesStep(
                    provenance.Steps[0],
                    RootArchivePath,
                    specification.ExpectedMixId,
                    specification.LogicalName))
            {
                throw Failure(
                    CsfProjectBaselineAuditFailureCode.TargetProvenanceMismatch,
                    "The fixed ProjectBaseline CSF provenance chain changed.");
            }
        }

        private static bool MatchesStep(
            MixArchiveProvenanceStep step,
            LogicalContentPath archive,
            MixFileId entryId,
            LogicalContentPath resolvedName)
        {
            return step.ArchivePath.Equals(archive) &&
                   step.EntryId == entryId &&
                   step.ResolvedName != null &&
                   step.ResolvedName.Equals(resolvedName);
        }

        private static CsfGoldenProvenance CopySanitizedProvenance(
            MixEntryProvenance provenance)
        {
            return new CsfGoldenProvenance(
                provenance.Source.Id,
                provenance.RootArchivePath,
                provenance.Steps.Select(step => new CsfGoldenProvenanceLayer(
                    step.ArchivePath,
                    step.EntryId,
                    step.ResolvedName)));
        }

        private static int DisposeMount(IDisposable mount)
        {
            if (mount == null)
            {
                return 0;
            }

            try
            {
                mount.Dispose();
                return 0;
            }
            catch (Exception)
            {
                return 1;
            }
        }

        private static void ThrowAfterCleanup(
            Exception operationFailure,
            int cleanupFailureCount)
        {
            if (operationFailure != null)
            {
                var structured = operationFailure as CsfProjectBaselineAuditException;
                if (structured != null && cleanupFailureCount != 0)
                {
                    structured.RecordCleanupFailures(cleanupFailureCount);
                }

                ExceptionDispatchInfo.Capture(operationFailure).Throw();
            }

            if (cleanupFailureCount != 0)
            {
                throw Failure(
                    CsfProjectBaselineAuditFailureCode.MountCleanupFailed,
                    "The controlled CSF MIX mount failed cleanup.",
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
                   exception is BinaryReadException;
        }

        private static CsfProjectBaselineAuditException Failure(
            CsfProjectBaselineAuditFailureCode code,
            string message,
            int cleanupFailureCount = 0)
        {
            return new CsfProjectBaselineAuditException(
                code,
                message,
                cleanupFailureCount);
        }
    }

    internal sealed class CsfProjectBaselineAuditProfile
    {
        public CsfProjectBaselineAuditProfile(
            CsfGoldenSampleSpecification specification,
            long maxExternalManifestUtf8Bytes,
            MixMountLimits mountLimits,
            CsfReadLimits readLimits)
        {
            Specification = specification ??
                throw new ArgumentNullException(nameof(specification));
            if (maxExternalManifestUtf8Bytes <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxExternalManifestUtf8Bytes));
            }

            MaxExternalManifestUtf8Bytes = maxExternalManifestUtf8Bytes;
            MountLimits = mountLimits ?? throw new ArgumentNullException(nameof(mountLimits));
            ReadLimits = readLimits ?? throw new ArgumentNullException(nameof(readLimits));
        }

        public static CsfProjectBaselineAuditProfile ProjectBaseline { get; } =
            new CsfProjectBaselineAuditProfile(
                new CsfGoldenSampleSpecification(
                    "ra2md.csf",
                    0xbd835079u,
                    332973,
                    "1b90bb0756137f46ff529af043fe798d7f1f9fa1713a4110f17e1d674de81f1c",
                    "f9018758f35a351f2316a78db99f40141641050c9253d2f6ab7961c24c19201e"),
                8L * 1024 * 1024,
                MixMountLimits.Default,
                CsfReadLimits.Default);

        public CsfGoldenSampleSpecification Specification { get; }
        public long MaxExternalManifestUtf8Bytes { get; }
        public MixMountLimits MountLimits { get; }
        public CsfReadLimits ReadLimits { get; }
    }
}
