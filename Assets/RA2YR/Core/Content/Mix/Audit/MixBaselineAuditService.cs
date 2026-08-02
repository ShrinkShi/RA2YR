using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Security.Cryptography;
using RA2YR.Core.Binary;
using RA2YR.Core.Binary.Seekable;
using RA2YR.Core.Formats.Mix;

namespace RA2YR.Core.Content.Mix.Audit
{
    public static class MixBaselineAuditService
    {
        public const string BaselineLogicalName = "YR1001_ProjectBaseline";

        private static readonly LogicalContentPath[] TargetPaths =
        {
            LogicalContentPath.Parse("isotem.pal"),
            LogicalContentPath.Parse("temperat.pal"),
            LogicalContentPath.Parse("unittem.pal"),
            LogicalContentPath.Parse("rulesmd.ini"),
            LogicalContentPath.Parse("artmd.ini"),
            LogicalContentPath.Parse("ai.ini"),
            LogicalContentPath.Parse("ra2md.csf")
        };

        public static MixBaselineAuditDelivery Run(
            ExternalContentConfiguration configuration,
            string xccGlobalNameDatabasePath)
        {
            return RunCore(
                configuration,
                xccGlobalNameDatabasePath,
                MixBaselineAuditProfile.ProjectBaseline,
                value => new ContentIndexer().Build(value),
                () => DateTime.UtcNow);
        }

        internal static MixBaselineAuditDelivery RunForTesting(
            ExternalContentConfiguration configuration,
            string xccGlobalNameDatabasePath,
            MixBaselineAuditProfile profile,
            Func<ExternalContentConfiguration, ContentIndexResult> buildIndex,
            Func<DateTime> utcNow)
        {
            return RunCore(
                configuration,
                xccGlobalNameDatabasePath,
                profile,
                buildIndex,
                utcNow);
        }

        private static MixBaselineAuditDelivery RunCore(
            ExternalContentConfiguration configuration,
            string xccGlobalNameDatabasePath,
            MixBaselineAuditProfile profile,
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
            XccDatabaseSnapshot xccDatabase = ReadXccDatabase(
                configuration,
                xccGlobalNameDatabasePath,
                profile);
            MixNameCatalog nameCatalog = CreateNameCatalog(xccDatabase);

            ContentIndexResult beforeIndex = BuildCompleteIndex(configuration, buildIndex);
            ContentSourceIndex beforeSource = GetBaselineSource(beforeIndex);
            ContentFileRecord[] roots = beforeSource.Files
                .Where(file => IsRootMix(file.LogicalPath))
                .OrderBy(file => file.LogicalPath, LogicalContentPathReportComparer.Instance)
                .ToArray();
            if (roots.Length == 0)
            {
                throw new MixBaselineAuditException(
                    MixBaselineAuditFailureCode.NoRootMixArchives,
                    "The controlled baseline directory index contains no root-level MIX archives.");
            }

            if (roots.Length > profile.MaxRootArchives)
            {
                throw new MixBaselineAuditException(
                    MixBaselineAuditFailureCode.RootArchiveBudgetExceeded,
                    "The root-level MIX count exceeds the explicit audit budget.");
            }

            var rootAudits = new List<MixBaselineRootAudit>(roots.Length);
            MixBaselineAuditDelivery delivery = null;
            Exception operationFailure = null;
            try
            {
                foreach (ContentFileRecord root in roots)
                {
                    MixVirtualContentMountResult mount;
                    try
                    {
                        mount = MixVirtualContentSource.MountDirectorySource(
                            beforeSource,
                            new[] { root.LogicalPath },
                            nameCatalog,
                            MixArchiveCatalogAdapters.ReadWithCoreReader,
                            profile.MountLimits,
                            MixMountIndexMode.ManifestAudit);
                    }
                    catch (Exception exception) when (IsExpectedReadException(exception))
                    {
                        mount = CreateIsolatedRootFailure(source, root.LogicalPath);
                    }

                    rootAudits.Add(new MixBaselineRootAudit(root, mount));
                }

                ContentIndexResult afterIndex = BuildCompleteIndex(configuration, buildIndex);
                ContentSourceIndex afterSource = GetBaselineSource(afterIndex);
                if (!string.Equals(
                    beforeSource.Fingerprint,
                    afterSource.Fingerprint,
                    StringComparison.Ordinal))
                {
                    throw new MixBaselineAuditException(
                        MixBaselineAuditFailureCode.BaselineChangedDuringAudit,
                        "The controlled baseline fingerprint changed during the MIX audit.");
                }

                IReadOnlyList<MixBaselineTargetAudit> targets = BuildTargets(
                    beforeSource,
                    rootAudits,
                    nameCatalog);
                DateTime completedUtc = utcNow().ToUniversalTime();
                var completedModel = new MixBaselineAuditModel(
                    source,
                    beforeSource.Fingerprint,
                    xccDatabase.Sha256,
                    xccDatabase.Length,
                    rootAudits,
                    targets,
                    startedUtc,
                    completedUtc);
                byte[] manifestBytes = MixBaselineAuditSerializer.SerializeExternalManifestUtf8(
                    completedModel,
                    profile.MaxManifestUtf8Bytes);
                MixAuditExternalManifestReference externalManifest =
                    MixAuditExternalManifestWriter.Write(
                        configuration,
                        source.Id,
                        beforeSource.Fingerprint,
                        manifestBytes);

                string summary = MixBaselineAuditSerializer.SerializeSanitizedSummary(
                    completedModel,
                    externalManifest);
                int parsedCount = rootAudits.Count(root => root.IsParsed);
                MixBaselineAuditStatus status = parsedCount == rootAudits.Count
                    ? MixBaselineAuditStatus.Complete
                    : MixBaselineAuditStatus.CompleteWithArchiveFailures;
                delivery = new MixBaselineAuditDelivery(
                    status,
                    rootAudits.Count,
                    parsedCount,
                    summary,
                    externalManifest.CacheRelativePath,
                    externalManifest.Length,
                    externalManifest.Sha256);
            }
            catch (Exception exception)
            {
                operationFailure = exception;
            }

            int cleanupFailureCount = MixBaselineAuditCleanup.DisposeAll(rootAudits);
            MixBaselineAuditCleanup.ThrowAfterCleanup(
                operationFailure,
                cleanupFailureCount);

            return delivery ?? throw new InvalidOperationException(
                "The MIX baseline audit ended without a delivery or failure.");
        }

        private static ExternalContentSourceDescriptor ValidateBaselineConfiguration(
            ExternalContentConfiguration configuration)
        {
            ExternalContentSourceDescriptor[] enabled = configuration.Sources
                .Where(source => source.Enabled)
                .ToArray();
            if (enabled.Length != 1 ||
                !string.Equals(
                    enabled[0].Id,
                    BaselineLogicalName,
                    StringComparison.Ordinal) ||
                enabled[0].Kind != ContentSourceKind.Patched)
            {
                throw new MixBaselineAuditException(
                    MixBaselineAuditFailureCode.InvalidBaselineConfiguration,
                    "The MIX audit requires exactly one enabled patched YR1001_ProjectBaseline source.");
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
                throw new MixBaselineAuditException(
                    MixBaselineAuditFailureCode.DirectoryIndexIncomplete,
                    "The controlled baseline directory index failed closed.");
            }

            if (result == null || !result.IsComplete || result.HasErrors)
            {
                throw new MixBaselineAuditException(
                    MixBaselineAuditFailureCode.DirectoryIndexIncomplete,
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
                throw new MixBaselineAuditException(
                    MixBaselineAuditFailureCode.DirectoryIndexIncomplete,
                    "The controlled baseline source index is incomplete.");
            }

            return matches[0];
        }

        private static XccDatabaseSnapshot ReadXccDatabase(
            ExternalContentConfiguration configuration,
            string path,
            MixBaselineAuditProfile profile)
        {
            string fullPath = ValidateXccDatabasePath(configuration, path);
            try
            {
                FileInfo before = new FileInfo(fullPath);
                before.Refresh();
                if (!before.Exists || before.Length < 0 ||
                    before.Length > profile.MaxXccDatabaseBytes ||
                    before.Length > int.MaxValue)
                {
                    throw new MixBaselineAuditException(
                        MixBaselineAuditFailureCode.XccNameDatabaseBudgetExceeded,
                        "The fixed XCC global name database exceeds its explicit input budget.");
                }

                long beforeLength = before.Length;
                DateTime beforeWriteTime = before.LastWriteTimeUtc;
                var bytes = new byte[checked((int)beforeLength)];
                using (var stream = new FileStream(
                    fullPath,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read,
                    64 * 1024,
                    FileOptions.SequentialScan))
                {
                    int offset = 0;
                    while (offset < bytes.Length)
                    {
                        int read = stream.Read(bytes, offset, bytes.Length - offset);
                        if (read == 0)
                        {
                            throw new MixBaselineAuditException(
                                MixBaselineAuditFailureCode.XccNameDatabaseChanged,
                                "The fixed XCC global name database ended during its bounded read.");
                        }

                        offset = checked(offset + read);
                    }

                    if (stream.ReadByte() != -1)
                    {
                        throw new MixBaselineAuditException(
                            MixBaselineAuditFailureCode.XccNameDatabaseChanged,
                            "The fixed XCC global name database grew during its bounded read.");
                    }
                }

                FileInfo after = new FileInfo(fullPath);
                after.Refresh();
                if (!after.Exists || after.Length != beforeLength ||
                    after.LastWriteTimeUtc != beforeWriteTime)
                {
                    throw new MixBaselineAuditException(
                        MixBaselineAuditFailureCode.XccNameDatabaseChanged,
                        "The fixed XCC global name database changed during its bounded read.");
                }

                string sha256 = ComputeSha256(bytes);
                if (!string.Equals(
                    sha256,
                    profile.ExpectedXccDatabaseSha256,
                    StringComparison.Ordinal))
                {
                    throw new MixBaselineAuditException(
                        MixBaselineAuditFailureCode.XccNameDatabaseHashMismatch,
                        "The XCC global name database does not match the fixed WP-02C profile.");
                }

                var source = new BinarySourceContext(
                    "XCC MIX name database",
                    "xcc-reference",
                    LogicalContentPath.Parse("reference/global-mix-database.dat"));
                XccMixNameCatalogReadResult readResult = XccMixNameCatalogReader.Read(
                    bytes,
                    source,
                    new XccMixNameCatalogLimits(
                        profile.MaxXccDatabaseBytes,
                        XccMixNameCatalogLimits.Default.MaxRecords,
                        XccMixNameCatalogLimits.Default.MaxStringLength,
                        XccMixNameCatalogLimits.Default.MaxAllocatedBytes));
                if (!readResult.IsSuccess)
                {
                    throw new MixBaselineAuditException(
                        MixBaselineAuditFailureCode.XccNameDatabaseInvalid,
                        "The fixed XCC global name database failed bounded parsing.");
                }

                return new XccDatabaseSnapshot(
                    bytes.LongLength,
                    sha256,
                    readResult.Names);
            }
            catch (MixBaselineAuditException)
            {
                throw;
            }
            catch (Exception exception) when (IsExpectedReadException(exception))
            {
                throw new MixBaselineAuditException(
                    MixBaselineAuditFailureCode.XccNameDatabaseChanged,
                    "The fixed XCC global name database could not be read safely.");
            }
        }

        private static string ValidateXccDatabasePath(
            ExternalContentConfiguration configuration,
            string path)
        {
            if (string.IsNullOrWhiteSpace(path) || !Path.IsPathRooted(path))
            {
                throw new MixBaselineAuditException(
                    MixBaselineAuditFailureCode.XccNameDatabasePathRejected,
                    "An explicit absolute XCC global name database path is required.");
            }

            try
            {
                string fullPath = RepositoryPathPolicy.NormalizeAbsolutePath(path);
                if (!File.Exists(fullPath) || Directory.Exists(fullPath))
                {
                    throw new MixBaselineAuditException(
                        MixBaselineAuditFailureCode.XccNameDatabasePathRejected,
                        "The fixed XCC global name database is not a regular file.");
                }

                FileAttributes attributes = File.GetAttributes(fullPath);
                if ((attributes & FileAttributes.ReparsePoint) != 0)
                {
                    throw new MixBaselineAuditException(
                        MixBaselineAuditFailureCode.XccNameDatabasePathRejected,
                        "The XCC global name database cannot be a reparse point.");
                }

                string aliasReason;
                string reparsePoint;
                if (RepositoryPathPolicy.TryFindUnsupportedAlias(fullPath, out aliasReason) ||
                    RepositoryPathPolicy.ContainsExistingReparsePoint(fullPath, out reparsePoint))
                {
                    throw new MixBaselineAuditException(
                        MixBaselineAuditFailureCode.XccNameDatabasePathRejected,
                        "The XCC global name database path cannot be verified safely.");
                }

                var protectedPaths = new List<string>
                {
                    configuration.RepositoryRoot,
                    configuration.CachePath
                };
                protectedPaths.AddRange(configuration.Sources.Select(source => source.RootPath));
                foreach (string protectedPath in protectedPaths)
                {
                    bool overlaps;
                    string failureReason;
                    if (!RepositoryPathPolicy.TryDetermineOverlap(
                        fullPath,
                        protectedPath,
                        out overlaps,
                        out failureReason) || overlaps)
                    {
                        throw new MixBaselineAuditException(
                            MixBaselineAuditFailureCode.XccNameDatabasePathRejected,
                            "The XCC global name database overlaps a protected storage boundary.");
                    }
                }

                return fullPath;
            }
            catch (MixBaselineAuditException)
            {
                throw;
            }
            catch (Exception exception) when (IsExpectedReadException(exception) ||
                                              exception is ArgumentException)
            {
                throw new MixBaselineAuditException(
                    MixBaselineAuditFailureCode.XccNameDatabasePathRejected,
                    "The XCC global name database path was rejected.");
            }
        }

        private static MixNameCatalog CreateNameCatalog(XccDatabaseSnapshot snapshot)
        {
            return new MixNameCatalog(snapshot.Names.Concat(TargetPaths));
        }

        private static IReadOnlyList<MixBaselineTargetAudit> BuildTargets(
            ContentSourceIndex directorySource,
            IEnumerable<MixBaselineRootAudit> roots,
            MixNameCatalog nameCatalog)
        {
            MixBaselineRootAudit[] rootArray = roots
                .OrderBy(root => root.RootFile.LogicalPath, LogicalContentPathReportComparer.Instance)
                .ToArray();
            var result = new List<MixBaselineTargetAudit>(TargetPaths.Length);
            foreach (LogicalContentPath targetPath in TargetPaths)
            {
                MixFileId id = MixFileId.ComputeCandidateId(targetPath.Value);
                var matches = new List<MixBaselineTargetMatch>();
                matches.AddRange(directorySource.Files
                    .Where(file => file.LogicalPath.Equals(targetPath))
                    .Select(file => new MixBaselineTargetMatch(
                        "Directory",
                        file.Length,
                        file.Sha256,
                        false,
                        null)));

                foreach (MixBaselineRootAudit root in rootArray.Where(item => item.IsParsed))
                {
                    foreach (MixVirtualEntry entry in root.Mount.Entries.Where(item => item.Id == id))
                    {
                        bool encryptedChain = entry.Provenance.Steps.Any(step =>
                            root.Mount.Archives.Any(archive =>
                                archive.LogicalPath.Equals(step.ArchivePath) &&
                                (archive.Flags & MixArchiveFlags.EncryptedDirectory) != 0));
                        matches.Add(new MixBaselineTargetMatch(
                            "MixArchive",
                            entry.Length,
                            entry.Sha256,
                            encryptedChain,
                            entry.Provenance));
                    }
                }

                result.Add(new MixBaselineTargetAudit(
                    targetPath,
                    id,
                    matches,
                    nameCatalog.IsAmbiguous(id)));
            }

            return Array.AsReadOnly(result.ToArray());
        }

        private static MixVirtualContentMountResult CreateIsolatedRootFailure(
            ExternalContentSourceDescriptor source,
            LogicalContentPath rootPath)
        {
            return new MixVirtualContentMountResult(
                source,
                Array.Empty<MixMountedArchive>(),
                Array.Empty<MixVirtualEntry>(),
                new[]
                {
                    new MixMountDiagnostic(
                        MixMountDiagnosticSeverity.Error,
                        MixMountDiagnosticCode.ArchiveReadFailed,
                        "The root MIX audit failed with a controlled read error.",
                        source.Id,
                        rootPath)
                },
                Array.Empty<ReadOnlyDataWindowSession>(),
                MixMountIndexMode.ManifestAudit,
                false);
        }

        private static bool IsRootMix(LogicalContentPath path)
        {
            return path.Value.IndexOf('/') < 0 &&
                   path.Value.EndsWith(".mix", StringComparison.OrdinalIgnoreCase);
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

        private static string ComputeSha256(byte[] bytes)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                return BitConverter.ToString(sha256.ComputeHash(bytes))
                    .Replace("-", string.Empty)
                    .ToLowerInvariant();
            }
        }

        private sealed class XccDatabaseSnapshot
        {
            public XccDatabaseSnapshot(
                long length,
                string sha256,
                IEnumerable<LogicalContentPath> names)
            {
                Length = length;
                Sha256 = sha256;
                Names = Array.AsReadOnly(
                    (names ?? throw new ArgumentNullException(nameof(names))).ToArray());
            }

            public long Length { get; }

            public string Sha256 { get; }

            public IReadOnlyList<LogicalContentPath> Names { get; }
        }
    }

    internal static class MixBaselineAuditCleanup
    {
        public static int DisposeAll(IEnumerable<IDisposable> values)
        {
            if (values == null)
            {
                throw new ArgumentNullException(nameof(values));
            }

            int failures = 0;
            foreach (IDisposable value in values)
            {
                if (value == null)
                {
                    failures = checked(failures + 1);
                    continue;
                }

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

        public static void ThrowAfterCleanup(
            Exception operationFailure,
            int cleanupFailureCount)
        {
            if (cleanupFailureCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(cleanupFailureCount));
            }

            if (operationFailure != null)
            {
                var structuredFailure = operationFailure as MixBaselineAuditException;
                if (structuredFailure != null && cleanupFailureCount != 0)
                {
                    structuredFailure.RecordCleanupFailures(cleanupFailureCount);
                }

                ExceptionDispatchInfo.Capture(operationFailure).Throw();
            }

            if (cleanupFailureCount != 0)
            {
                throw new MixBaselineAuditException(
                    MixBaselineAuditFailureCode.RootMountCleanupFailed,
                    "One or more MIX root mounts failed controlled cleanup.",
                    cleanupFailureCount);
            }
        }
    }
}
