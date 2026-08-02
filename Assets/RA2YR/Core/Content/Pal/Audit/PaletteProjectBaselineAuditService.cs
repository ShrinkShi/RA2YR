using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.ExceptionServices;
using RA2YR.Core.Binary;
using RA2YR.Core.Content.Mix;
using RA2YR.Core.Formats.Mix;
using RA2YR.Core.Formats.Pal;

namespace RA2YR.Core.Content.Pal.Audit
{
    public static class PaletteProjectBaselineAuditService
    {
        public const string BaselineLogicalName = "YR1001_ProjectBaseline";
        public const string DisplayConversionStrategy = "XccScaleToFullRangeFloor";

        private static readonly LogicalContentPath RootArchivePath =
            LogicalContentPath.Parse("ra2.mix");
        private static readonly LogicalContentPath CacheArchiveName =
            LogicalContentPath.Parse("cache.mix");
        private static readonly LogicalContentPath NestedCacheArchivePath =
            LogicalContentPath.Parse("ra2.mix/cache.mix");
        private static readonly MixFileId CacheArchiveId =
            MixFileId.FromRaw(0x3b5a96deu);

        public static PaletteProjectBaselineAuditDelivery Run(
            ExternalContentConfiguration configuration)
        {
            return RunCore(
                configuration,
                PaletteProjectBaselineAuditProfile.ProjectBaseline,
                value => new ContentIndexer().Build(value),
                () => DateTime.UtcNow);
        }

        internal static PaletteProjectBaselineAuditDelivery RunForTesting(
            ExternalContentConfiguration configuration,
            PaletteProjectBaselineAuditProfile profile,
            Func<ExternalContentConfiguration, ContentIndexResult> buildIndex,
            Func<DateTime> utcNow)
        {
            return RunCore(configuration, profile, buildIndex, utcNow);
        }

        private static PaletteProjectBaselineAuditDelivery RunCore(
            ExternalContentConfiguration configuration,
            PaletteProjectBaselineAuditProfile profile,
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
            RejectLoosePaletteCandidates(beforeSource, profile.Samples);

            var names = new List<LogicalContentPath> { CacheArchiveName };
            names.AddRange(profile.Samples.Select(sample => sample.LogicalName));
            var nameCatalog = new MixNameCatalog(names);
            MixVirtualContentMountResult mount = null;
            PaletteProjectBaselineAuditDelivery delivery = null;
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

                var records = new List<PaletteGoldenSampleRecord>(profile.Samples.Count);
                foreach (PaletteGoldenSampleSpecification specification in profile.Samples)
                {
                    records.Add(ReadGoldenPalette(
                        mount,
                        source,
                        specification,
                        profile.ReadLimits));
                }

                ContentIndexResult afterIndex = BuildCompleteIndex(configuration, buildIndex);
                ContentSourceIndex afterSource = GetBaselineSource(afterIndex);
                if (!string.Equals(
                        beforeSource.Fingerprint,
                        afterSource.Fingerprint,
                        StringComparison.Ordinal))
                {
                    throw Failure(
                        PaletteProjectBaselineAuditFailureCode.BaselineChangedDuringAudit,
                        "The controlled baseline fingerprint changed during the palette audit.");
                }

                DateTime completedUtc = utcNow().ToUniversalTime();
                var model = new PaletteProjectBaselineAuditModel(
                    source,
                    beforeSource.Fingerprint,
                    records,
                    startedUtc,
                    completedUtc);
                byte[] externalBytes =
                    PaletteProjectBaselineAuditSerializer.SerializeExternalManifestUtf8(
                        model,
                        profile.MaxExternalManifestUtf8Bytes);
                PaletteAuditExternalManifestReference externalManifest =
                    PaletteAuditExternalManifestWriter.Write(
                        configuration,
                        source.Id,
                        beforeSource.Fingerprint,
                        externalBytes);
                string summary =
                    PaletteProjectBaselineAuditSerializer.SerializeSanitizedSummary(
                        model,
                        externalManifest);
                delivery = new PaletteProjectBaselineAuditDelivery(
                    records.Count,
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
                "The palette baseline audit ended without a delivery or failure.");
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
                    PaletteProjectBaselineAuditFailureCode.InvalidBaselineConfiguration,
                    "The palette audit requires exactly one enabled patched YR1001_ProjectBaseline source.");
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
                    PaletteProjectBaselineAuditFailureCode.DirectoryIndexIncomplete,
                    "The controlled baseline directory index failed closed.");
            }

            if (result == null || !result.IsComplete || result.HasErrors)
            {
                throw Failure(
                    PaletteProjectBaselineAuditFailureCode.DirectoryIndexIncomplete,
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
                    PaletteProjectBaselineAuditFailureCode.DirectoryIndexIncomplete,
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
                    PaletteProjectBaselineAuditFailureCode.RootArchiveMissing,
                    "The controlled baseline does not contain exactly one canonical ra2.mix root archive.");
            }

            return matches[0];
        }

        private static void RejectLoosePaletteCandidates(
            ContentSourceIndex source,
            IEnumerable<PaletteGoldenSampleSpecification> specifications)
        {
            var names = new HashSet<LogicalContentPath>(
                specifications.Select(specification => specification.LogicalName));
            if (source.Files.Any(file => names.Contains(file.LogicalPath)))
            {
                throw Failure(
                    PaletteProjectBaselineAuditFailureCode.LoosePaletteCandidateFound,
                    "A golden palette exists as a loose directory file instead of only through the fixed MIX chain.");
            }
        }

        private static void ValidateMount(MixVirtualContentMountResult mount)
        {
            if (mount == null || !mount.IsComplete || mount.Diagnostics.Count != 0 ||
                mount.IndexMode != MixMountIndexMode.StructureOnly)
            {
                throw Failure(
                    PaletteProjectBaselineAuditFailureCode.MixMountFailed,
                    "The bounded ra2.mix mount did not complete without diagnostics.");
            }

            LogicalContentPath[] archives = mount.Archives
                .Select(archive => archive.LogicalPath)
                .OrderBy(path => path, LogicalContentPathReportComparer.Instance)
                .ToArray();
            if (archives.Length != 2 ||
                !archives.Any(path => path.Equals(RootArchivePath)) ||
                !archives.Any(path => path.Equals(NestedCacheArchivePath)))
            {
                throw Failure(
                    PaletteProjectBaselineAuditFailureCode.TargetProvenanceMismatch,
                    "The mounted archive set does not match the fixed ra2.mix to cache.mix chain.");
            }
        }

        private static PaletteGoldenSampleRecord ReadGoldenPalette(
            MixVirtualContentMountResult mount,
            ExternalContentSourceDescriptor source,
            PaletteGoldenSampleSpecification specification,
            PaletteReadLimits readLimits)
        {
            MixVirtualEntry[] idMatches = mount.Entries
                .Where(entry => entry.Id == specification.ExpectedMixId)
                .ToArray();
            if (idMatches.Length == 0)
            {
                throw Failure(
                    PaletteProjectBaselineAuditFailureCode.TargetMissing,
                    "A fixed ProjectBaseline palette target was not found.");
            }

            if (idMatches.Length != 1)
            {
                throw Failure(
                    PaletteProjectBaselineAuditFailureCode.TargetAmbiguous,
                    "A fixed ProjectBaseline palette target resolved to multiple MIX entries.");
            }

            MixVirtualEntry entry = idMatches[0];
            if (entry.LogicalName == null ||
                !entry.LogicalName.Equals(specification.LogicalName) ||
                entry.IsMountedArchive)
            {
                throw Failure(
                    PaletteProjectBaselineAuditFailureCode.TargetIdentityMismatch,
                    "A fixed palette target does not match its expected MIX identity.");
            }

            ValidateProvenance(entry, source, specification);
            if (entry.Length != specification.ExpectedLength)
            {
                throw Failure(
                    PaletteProjectBaselineAuditFailureCode.TargetLengthMismatch,
                    "A fixed ProjectBaseline palette length changed.");
            }

            string payloadSha256;
            try
            {
                payloadSha256 = entry.PayloadWindow.ComputeSha256(
                    "palette-golden-payload-hash");
            }
            catch (Exception exception) when (IsExpectedReadException(exception))
            {
                throw Failure(
                    PaletteProjectBaselineAuditFailureCode.TargetHashMismatch,
                    "A fixed ProjectBaseline palette could not be hashed safely.");
            }

            if (!string.Equals(
                    payloadSha256,
                    specification.ExpectedSha256,
                    StringComparison.Ordinal))
            {
                throw Failure(
                    PaletteProjectBaselineAuditFailureCode.TargetHashMismatch,
                    "A fixed ProjectBaseline palette SHA-256 changed.");
            }

            var parserProvenance = new PaletteSourceProvenance(
                source.Id,
                new[]
                {
                    RootArchivePath,
                    CacheArchiveName,
                    specification.LogicalName
                });
            var sourceContext = new BinarySourceContext(
                "format.pal",
                source.Id,
                specification.LogicalName);
            PaletteParseResult result = WestwoodPaletteReader.Read(
                entry.PayloadWindow,
                sourceContext,
                parserProvenance,
                readLimits);
            if (!result.IsSuccess || result.Diagnostics.Count != 0)
            {
                throw Failure(
                    PaletteProjectBaselineAuditFailureCode.PaletteParseFailed,
                    "A fixed ProjectBaseline palette failed strict bounded parsing.");
            }

            WestwoodPalette palette = result.Palette;
            if (!string.Equals(
                    palette.CanonicalModelSha256,
                    specification.ExpectedNormalizedModelSha256,
                    StringComparison.Ordinal))
            {
                throw Failure(
                    PaletteProjectBaselineAuditFailureCode.NormalizedModelHashMismatch,
                    "A fixed ProjectBaseline normalized palette model changed.");
            }

            byte[] raw = CopyRawTriplets(palette);
            int invalidChannels = raw.Count(value =>
                value > PaletteColorRaw.MaximumChannelValue);
            return new PaletteGoldenSampleRecord(
                specification,
                CopySanitizedProvenance(entry.Provenance),
                entry.Length,
                payloadSha256,
                palette.Colors.Count,
                palette.MinimumRawChannel,
                palette.MaximumRawChannel,
                invalidChannels,
                palette.DistinctColorCount,
                palette.CanonicalModelSha256,
                DisplayConversionStrategy,
                result.Diagnostics.Count,
                raw);
        }

        private static void ValidateProvenance(
            MixVirtualEntry entry,
            ExternalContentSourceDescriptor source,
            PaletteGoldenSampleSpecification specification)
        {
            MixEntryProvenance provenance = entry.Provenance;
            if (!string.Equals(provenance.Source.Id, source.Id, StringComparison.Ordinal) ||
                !provenance.RootArchivePath.Equals(RootArchivePath) ||
                provenance.Steps.Count != 2 ||
                !MatchesStep(
                    provenance.Steps[0],
                    RootArchivePath,
                    CacheArchiveId,
                    CacheArchiveName) ||
                !MatchesStep(
                    provenance.Steps[1],
                    NestedCacheArchivePath,
                    specification.ExpectedMixId,
                    specification.LogicalName))
            {
                throw Failure(
                    PaletteProjectBaselineAuditFailureCode.TargetProvenanceMismatch,
                    "A fixed ProjectBaseline palette provenance chain changed.");
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

        private static PaletteGoldenProvenance CopySanitizedProvenance(
            MixEntryProvenance provenance)
        {
            return new PaletteGoldenProvenance(
                provenance.Source.Id,
                provenance.RootArchivePath,
                provenance.Steps.Select(step => new PaletteGoldenProvenanceLayer(
                    step.ArchivePath,
                    step.EntryId,
                    step.ResolvedName)));
        }

        private static byte[] CopyRawTriplets(WestwoodPalette palette)
        {
            var raw = new byte[checked(palette.Colors.Count * 3)];
            int offset = 0;
            foreach (PaletteColorRaw color in palette.Colors)
            {
                raw[offset++] = color.Red;
                raw[offset++] = color.Green;
                raw[offset++] = color.Blue;
            }

            return raw;
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
                var structured = operationFailure as PaletteProjectBaselineAuditException;
                if (structured != null && cleanupFailureCount != 0)
                {
                    structured.RecordCleanupFailures(cleanupFailureCount);
                }

                ExceptionDispatchInfo.Capture(operationFailure).Throw();
            }

            if (cleanupFailureCount != 0)
            {
                throw Failure(
                    PaletteProjectBaselineAuditFailureCode.MountCleanupFailed,
                    "The controlled palette MIX mount failed cleanup.",
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

        private static PaletteProjectBaselineAuditException Failure(
            PaletteProjectBaselineAuditFailureCode code,
            string message,
            int cleanupFailureCount = 0)
        {
            return new PaletteProjectBaselineAuditException(
                code,
                message,
                cleanupFailureCount);
        }
    }
}
