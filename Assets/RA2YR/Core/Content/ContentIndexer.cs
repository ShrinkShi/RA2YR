using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace RA2YR.Core.Content
{
    public sealed class ContentIndexer
    {
        private readonly IContentFileDigestProvider digestProvider;

        public ContentIndexer()
            : this(new Sha256ContentFileDigestProvider())
        {
        }

        internal ContentIndexer(IContentFileDigestProvider digestProvider)
        {
            this.digestProvider = digestProvider ??
                throw new ArgumentNullException(nameof(digestProvider));
        }

        public ContentIndexResult Build(ExternalContentConfiguration configuration)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            var diagnostics = new List<ContentDiagnostic>();
            var sourceIndexes = new List<ContentSourceIndex>();
            if (!ValidateConfigurationPaths(configuration, diagnostics))
            {
                return new ContentIndexResult(sourceIndexes, diagnostics);
            }

            IEnumerable<ExternalContentSourceDescriptor> enabledSources = configuration.Sources
                .Where(source => source.Enabled)
                .OrderByDescending(source => source.Priority)
                .ThenBy(source => source.Id, StringComparer.Ordinal);

            foreach (ExternalContentSourceDescriptor source in enabledSources)
            {
                IContentSource contentSource = new DirectoryContentSource(
                    source,
                    IndexDirectorySource);
                ContentSourceIndex sourceIndex = contentSource.BuildIndex(
                    configuration.RepositoryRoot,
                    diagnostics);
                if (sourceIndex != null)
                {
                    sourceIndexes.Add(sourceIndex);
                }
            }

            return new ContentIndexResult(sourceIndexes, diagnostics);
        }

        private static bool ValidateConfigurationPaths(
            ExternalContentConfiguration configuration,
            ICollection<ContentDiagnostic> diagnostics)
        {
            if (!Directory.Exists(configuration.RepositoryRoot))
            {
                diagnostics.Add(new ContentDiagnostic(
                    ContentDiagnosticSeverity.Error,
                    ContentDiagnosticCode.RepositoryRootNotDirectory,
                    "The formal repository root no longer exists as a directory.",
                    path: configuration.RepositoryRoot));
                return false;
            }

            string aliasReason;
            if (RepositoryPathPolicy.TryFindUnsupportedAlias(
                configuration.RepositoryRoot,
                out aliasReason))
            {
                diagnostics.Add(new ContentDiagnostic(
                    ContentDiagnosticSeverity.Error,
                    ContentDiagnosticCode.PathAliasUnsupported,
                    "The repository root cannot be verified safely: " + aliasReason,
                    path: configuration.RepositoryRoot));
                return false;
            }

            try
            {
                string reparsePointPath;
                if (RepositoryPathPolicy.ContainsExistingReparsePoint(
                    configuration.RepositoryRoot,
                    out reparsePointPath))
                {
                    diagnostics.Add(new ContentDiagnostic(
                        ContentDiagnosticSeverity.Error,
                        ContentDiagnosticCode.PathUsesReparsePoint,
                        "The repository root traverses a reparse point.",
                        path: reparsePointPath));
                    return false;
                }
            }
            catch (Exception exception) when (
                exception is IOException ||
                exception is UnauthorizedAccessException ||
                exception is System.Security.SecurityException)
            {
                diagnostics.Add(new ContentDiagnostic(
                    ContentDiagnosticSeverity.Error,
                    ContentDiagnosticCode.PathInspectionFailed,
                    "The repository root path chain could not be inspected: " + exception.Message,
                    path: configuration.RepositoryRoot));
                return false;
            }

            if (File.Exists(configuration.CachePath))
            {
                diagnostics.Add(new ContentDiagnostic(
                    ContentDiagnosticSeverity.Error,
                    ContentDiagnosticCode.CachePathNotDirectory,
                    "The cache path exists but is not a directory.",
                    path: configuration.CachePath));
                return false;
            }

            try
            {
                string cacheReparsePoint;
                if (RepositoryPathPolicy.ContainsExistingReparsePoint(
                    configuration.CachePath,
                    out cacheReparsePoint))
                {
                    diagnostics.Add(new ContentDiagnostic(
                        ContentDiagnosticSeverity.Error,
                        ContentDiagnosticCode.PathUsesReparsePoint,
                        "The cache path traverses a reparse point.",
                        path: cacheReparsePoint));
                    return false;
                }
            }
            catch (Exception exception) when (
                exception is IOException ||
                exception is UnauthorizedAccessException ||
                exception is System.Security.SecurityException)
            {
                diagnostics.Add(new ContentDiagnostic(
                    ContentDiagnosticSeverity.Error,
                    ContentDiagnosticCode.PathInspectionFailed,
                    "The cache path chain could not be inspected: " + exception.Message,
                    path: configuration.CachePath));
                return false;
            }

            bool cacheOverlapsRepository;
            string cacheIdentityFailure;
            if (!RepositoryPathPolicy.TryDetermineOverlap(
                configuration.CachePath,
                configuration.RepositoryRoot,
                out cacheOverlapsRepository,
                out cacheIdentityFailure))
            {
                diagnostics.Add(new ContentDiagnostic(
                    ContentDiagnosticSeverity.Error,
                    ContentDiagnosticCode.PathIdentityUnavailable,
                    "The cache identity could not be compared with the repository: " + cacheIdentityFailure,
                    path: configuration.CachePath));
                return false;
            }

            if (cacheOverlapsRepository)
            {
                diagnostics.Add(new ContentDiagnostic(
                    ContentDiagnosticSeverity.Error,
                    ContentDiagnosticCode.PathInsideRepository,
                    "The cache storage identity overlaps the formal repository.",
                    path: configuration.CachePath));
                return false;
            }

            for (int index = 0; index < configuration.Sources.Count; index++)
            {
                ExternalContentSourceDescriptor source = configuration.Sources[index];
                bool overlaps;
                string failureReason;
                if (!RepositoryPathPolicy.TryDetermineOverlap(
                    configuration.CachePath,
                    source.RootPath,
                    out overlaps,
                    out failureReason))
                {
                    diagnostics.Add(new ContentDiagnostic(
                        ContentDiagnosticSeverity.Error,
                        ContentDiagnosticCode.PathIdentityUnavailable,
                        "Cache/source identity comparison failed: " + failureReason,
                        source.Id,
                        source.RootPath));
                    return false;
                }

                if (overlaps)
                {
                    diagnostics.Add(new ContentDiagnostic(
                        ContentDiagnosticSeverity.Error,
                        ContentDiagnosticCode.ExternalPathsOverlap,
                        "The cache path must not overlap a source path.",
                        source.Id,
                        source.RootPath));
                    return false;
                }

                for (int otherIndex = index + 1;
                     otherIndex < configuration.Sources.Count;
                     otherIndex++)
                {
                    ExternalContentSourceDescriptor otherSource =
                        configuration.Sources[otherIndex];
                    if (!RepositoryPathPolicy.TryDetermineOverlap(
                            source.RootPath,
                            otherSource.RootPath,
                            out overlaps,
                            out failureReason))
                    {
                        diagnostics.Add(new ContentDiagnostic(
                            ContentDiagnosticSeverity.Error,
                            ContentDiagnosticCode.PathIdentityUnavailable,
                            "Source identity comparison failed: " + failureReason,
                            otherSource.Id,
                            otherSource.RootPath));
                        return false;
                    }

                    if (overlaps)
                    {
                        diagnostics.Add(new ContentDiagnostic(
                            ContentDiagnosticSeverity.Error,
                            ContentDiagnosticCode.ExternalPathsOverlap,
                            "Source storage identities must not contain one another.",
                            otherSource.Id,
                            otherSource.RootPath));
                        return false;
                    }
                }
            }

            return true;
        }

        private ContentSourceIndex IndexDirectorySource(
            ExternalContentSourceDescriptor source,
            string repositoryRoot,
            ICollection<ContentDiagnostic> diagnostics)
        {
            string aliasReason;
            if (RepositoryPathPolicy.TryFindUnsupportedAlias(source.RootPath, out aliasReason))
            {
                diagnostics.Add(new ContentDiagnostic(
                    ContentDiagnosticSeverity.Error,
                    ContentDiagnosticCode.PathAliasUnsupported,
                    "The source path cannot be verified safely: " + aliasReason,
                    source.Id,
                    source.RootPath));
                return null;
            }

            if (RepositoryPathPolicy.OverlapsRepository(source.RootPath, repositoryRoot))
            {
                diagnostics.Add(new ContentDiagnostic(
                    ContentDiagnosticSeverity.Error,
                    ContentDiagnosticCode.PathInsideRepository,
                    "The source path overlaps the formal repository and was not indexed.",
                    source.Id,
                    source.RootPath));
                return null;
            }

            bool identityOverlap;
            string identityFailure;
            if (!RepositoryPathPolicy.TryDetermineOverlap(
                source.RootPath,
                repositoryRoot,
                out identityOverlap,
                out identityFailure))
            {
                diagnostics.Add(new ContentDiagnostic(
                    ContentDiagnosticSeverity.Error,
                    ContentDiagnosticCode.PathIdentityUnavailable,
                    "The source identity could not be compared with the repository: " + identityFailure,
                    source.Id,
                    source.RootPath));
                return null;
            }

            if (identityOverlap)
            {
                diagnostics.Add(new ContentDiagnostic(
                    ContentDiagnosticSeverity.Error,
                    ContentDiagnosticCode.PathInsideRepository,
                    "The source storage identity overlaps the formal repository.",
                    source.Id,
                    source.RootPath));
                return null;
            }

            if (!Directory.Exists(source.RootPath))
            {
                diagnostics.Add(new ContentDiagnostic(
                    ContentDiagnosticSeverity.Error,
                    ContentDiagnosticCode.SourceDirectoryMissing,
                    "The enabled source directory does not exist.",
                    source.Id,
                    source.RootPath));
                return null;
            }

            try
            {
                string reparsePointPath;
                if (RepositoryPathPolicy.ContainsExistingReparsePoint(
                    source.RootPath,
                    out reparsePointPath))
                {
                    diagnostics.Add(new ContentDiagnostic(
                        ContentDiagnosticSeverity.Error,
                        ContentDiagnosticCode.PathUsesReparsePoint,
                        "The source path traverses a reparse point and was not indexed.",
                        source.Id,
                        reparsePointPath));
                    return null;
                }
            }
            catch (Exception exception) when (
                exception is IOException ||
                exception is UnauthorizedAccessException ||
                exception is System.Security.SecurityException)
            {
                diagnostics.Add(new ContentDiagnostic(
                    ContentDiagnosticSeverity.Error,
                    ContentDiagnosticCode.PathInspectionFailed,
                    "The source path chain could not be inspected: " + exception.Message,
                    source.Id,
                    source.RootPath));
                return null;
            }

            var files = new List<ContentFileRecord>();
            var observations = new List<IndexedFileObservation>();
            bool isComplete = true;
            var pendingDirectories = new Stack<DirectoryInfo>();
            pendingDirectories.Push(new DirectoryInfo(source.RootPath));

            while (pendingDirectories.Count > 0)
            {
                DirectoryInfo directory = pendingDirectories.Pop();
                FileSystemInfo[] entries;
                try
                {
                    directory.Refresh();
                    if (!directory.Exists)
                    {
                        diagnostics.Add(new ContentDiagnostic(
                            ContentDiagnosticSeverity.Error,
                            ContentDiagnosticCode.SourceEnumerationFailed,
                            "A directory disappeared while the source was being indexed.",
                            source.Id,
                            ToRelativePath(source.RootPath, directory.FullName)));
                        isComplete = false;
                        continue;
                    }

                    if ((directory.Attributes & FileAttributes.ReparsePoint) != 0)
                    {
                        diagnostics.Add(new ContentDiagnostic(
                            ContentDiagnosticSeverity.Warning,
                            ContentDiagnosticCode.DirectoryReparsePointSkipped,
                            "A reparse-point directory was skipped.",
                            source.Id,
                            ToRelativePath(source.RootPath, directory.FullName)));
                        isComplete = false;
                        continue;
                    }

                    entries = directory.GetFileSystemInfos();
                }
                catch (Exception exception) when (
                    exception is IOException ||
                    exception is UnauthorizedAccessException ||
                    exception is System.Security.SecurityException)
                {
                    diagnostics.Add(new ContentDiagnostic(
                        ContentDiagnosticSeverity.Error,
                        ContentDiagnosticCode.SourceEnumerationFailed,
                        "A source directory could not be enumerated: " + exception.Message,
                        source.Id,
                        ToRelativePath(source.RootPath, directory.FullName)));
                    isComplete = false;
                    continue;
                }

                Array.Sort(
                    entries,
                    (left, right) => StringComparer.Ordinal.Compare(
                        left.Name,
                        right.Name));

                var childDirectories = new List<DirectoryInfo>();
                foreach (FileSystemInfo entry in entries)
                {
                    string relativePath = ToRelativePath(source.RootPath, entry.FullName);
                    FileAttributes attributes;
                    try
                    {
                        entry.Refresh();
                        attributes = entry.Attributes;
                    }
                    catch (Exception exception) when (
                        exception is IOException ||
                        exception is UnauthorizedAccessException ||
                        exception is System.Security.SecurityException)
                    {
                        diagnostics.Add(new ContentDiagnostic(
                            ContentDiagnosticSeverity.Error,
                            ContentDiagnosticCode.FileMetadataReadFailed,
                            "File-system metadata could not be read: " + exception.Message,
                            source.Id,
                            relativePath));
                        isComplete = false;
                        continue;
                    }

                    if ((attributes & FileAttributes.ReparsePoint) != 0)
                    {
                        diagnostics.Add(new ContentDiagnostic(
                            ContentDiagnosticSeverity.Warning,
                            entry is DirectoryInfo
                                ? ContentDiagnosticCode.DirectoryReparsePointSkipped
                                : ContentDiagnosticCode.FileReparsePointSkipped,
                            "A reparse-point entry was skipped.",
                            source.Id,
                            relativePath));
                        isComplete = false;
                        continue;
                    }

                    if (entry is DirectoryInfo childDirectory)
                    {
                        childDirectories.Add(childDirectory);
                    }
                    else if (entry is FileInfo file)
                    {
                        IndexedFileObservation observation = IndexFile(
                            source,
                            file,
                            relativePath,
                            diagnostics);
                        if (observation != null)
                        {
                            observations.Add(observation);
                            files.Add(observation.Record);
                        }
                        else
                        {
                            isComplete = false;
                        }
                    }
                }

                for (int index = childDirectories.Count - 1; index >= 0; index--)
                {
                    pendingDirectories.Push(childDirectories[index]);
                }
            }

            foreach (IGrouping<LogicalContentPath, ContentFileRecord> collision in files
                         .GroupBy(file => file.LogicalPath)
                         .Where(group => group.Count() > 1)
                         .OrderBy(group => group.Key, LogicalContentPathReportComparer.Instance))
            {
                string[] actualPaths = collision
                    .Select(file => file.RelativePath)
                    .OrderBy(path => path, StringComparer.Ordinal)
                    .ToArray();
                diagnostics.Add(new ContentDiagnostic(
                    ContentDiagnosticSeverity.Error,
                    ContentDiagnosticCode.SourceLogicalPathConflict,
                    "The directory source contains multiple case variants for one logical " +
                    "path and no candidate was selected: " + string.Join(", ", actualPaths),
                    source.Id,
                    collision.Key.Value));
                isComplete = false;
            }

            if (isComplete && !VerifySourceTreeSnapshot(
                    source,
                    observations,
                    diagnostics))
            {
                isComplete = false;
            }

            files.Sort((left, right) => StringComparer.Ordinal.Compare(
                left.RelativePath,
                right.RelativePath));
            string fingerprint = ContentSourceFingerprint.Compute(source, files);
            return new ContentSourceIndex(source, files, fingerprint, isComplete);
        }

        private IndexedFileObservation IndexFile(
            ExternalContentSourceDescriptor source,
            FileInfo file,
            string relativePath,
            ICollection<ContentDiagnostic> diagnostics)
        {
            LogicalContentPath logicalPath;
            string pathFailure;
            if (!LogicalContentPath.TryParse(relativePath, out logicalPath, out pathFailure))
            {
                diagnostics.Add(new ContentDiagnostic(
                    ContentDiagnosticSeverity.Error,
                    ContentDiagnosticCode.InvalidLogicalPath,
                    "The directory entry cannot be represented as a logical content path: " +
                    pathFailure,
                    source.Id,
                    relativePath));
                return null;
            }

            long lengthBefore;
            DateTime lastWriteBefore;
            try
            {
                file.Refresh();
                lengthBefore = file.Length;
                lastWriteBefore = file.LastWriteTimeUtc;
            }
            catch (Exception exception) when (
                exception is IOException ||
                exception is UnauthorizedAccessException ||
                exception is System.Security.SecurityException)
            {
                diagnostics.Add(new ContentDiagnostic(
                    ContentDiagnosticSeverity.Error,
                    ContentDiagnosticCode.FileMetadataReadFailed,
                    "File metadata could not be read before hashing: " + exception.Message,
                    source.Id,
                    relativePath));
                return null;
            }

            string digest;
            try
            {
                digest = digestProvider.ComputeSha256(file.FullName);
                if (!Sha256Utilities.IsLowerSha256(digest))
                {
                    throw new InvalidDataException(
                        "The digest provider did not return a lowercase SHA-256 value.");
                }
            }
            catch (Exception exception) when (
                exception is IOException ||
                exception is UnauthorizedAccessException ||
                exception is InvalidDataException ||
                exception is CryptographicException ||
                exception is System.Security.SecurityException)
            {
                diagnostics.Add(new ContentDiagnostic(
                    ContentDiagnosticSeverity.Error,
                    ContentDiagnosticCode.FileHashFailed,
                    "The file could not be hashed: " + exception.Message,
                    source.Id,
                    relativePath));
                return null;
            }

            try
            {
                file.Refresh();
                if (!file.Exists ||
                    file.Length != lengthBefore ||
                    file.LastWriteTimeUtc != lastWriteBefore)
                {
                    diagnostics.Add(new ContentDiagnostic(
                        ContentDiagnosticSeverity.Error,
                        ContentDiagnosticCode.FileChangedDuringHash,
                        "The file length or last-write time changed while it was being hashed.",
                        source.Id,
                        relativePath));
                    return null;
                }
            }
            catch (Exception exception) when (
                exception is IOException ||
                exception is UnauthorizedAccessException ||
                exception is System.Security.SecurityException)
            {
                diagnostics.Add(new ContentDiagnostic(
                    ContentDiagnosticSeverity.Error,
                    ContentDiagnosticCode.FileMetadataReadFailed,
                    "File metadata could not be read after hashing: " + exception.Message,
                    source.Id,
                    relativePath));
                return null;
            }

            return new IndexedFileObservation(
                new ContentFileRecord(
                    source.Id,
                    logicalPath,
                    lengthBefore,
                    digest),
                lastWriteBefore);
        }

        private static bool VerifySourceTreeSnapshot(
            ExternalContentSourceDescriptor source,
            IEnumerable<IndexedFileObservation> observations,
            ICollection<ContentDiagnostic> diagnostics)
        {
            Dictionary<string, IndexedFileObservation> expected = observations.ToDictionary(
                item => item.Record.RelativePath,
                StringComparer.Ordinal);
            var observedPaths = new HashSet<string>(StringComparer.Ordinal);
            var pendingDirectories = new Stack<DirectoryInfo>();
            pendingDirectories.Push(new DirectoryInfo(source.RootPath));
            bool changed = false;

            while (pendingDirectories.Count > 0)
            {
                DirectoryInfo directory = pendingDirectories.Pop();
                FileSystemInfo[] entries;
                try
                {
                    directory.Refresh();
                    if (!directory.Exists ||
                        (directory.Attributes & FileAttributes.ReparsePoint) != 0)
                    {
                        changed = true;
                        continue;
                    }

                    entries = directory.GetFileSystemInfos();
                }
                catch (Exception exception) when (
                    exception is IOException ||
                    exception is UnauthorizedAccessException ||
                    exception is System.Security.SecurityException)
                {
                    diagnostics.Add(new ContentDiagnostic(
                        ContentDiagnosticSeverity.Error,
                        ContentDiagnosticCode.SourceEnumerationFailed,
                        "The source tree could not be verified after hashing: " + exception.Message,
                        source.Id,
                        ToRelativePath(source.RootPath, directory.FullName)));
                    return false;
                }

                foreach (FileSystemInfo entry in entries)
                {
                    string relativePath = ToRelativePath(source.RootPath, entry.FullName);
                    try
                    {
                        entry.Refresh();
                        if ((entry.Attributes & FileAttributes.ReparsePoint) != 0)
                        {
                            changed = true;
                            continue;
                        }

                        if (entry is DirectoryInfo childDirectory)
                        {
                            pendingDirectories.Push(childDirectory);
                            continue;
                        }

                        var file = entry as FileInfo;
                        IndexedFileObservation expectedFile;
                        if (file == null ||
                            !file.Exists ||
                            !expected.TryGetValue(relativePath, out expectedFile) ||
                            file.Length != expectedFile.Record.Length ||
                            file.LastWriteTimeUtc != expectedFile.LastWriteTimeUtc ||
                            !observedPaths.Add(relativePath))
                        {
                            changed = true;
                        }
                    }
                    catch (Exception exception) when (
                        exception is IOException ||
                        exception is UnauthorizedAccessException ||
                        exception is System.Security.SecurityException)
                    {
                        diagnostics.Add(new ContentDiagnostic(
                            ContentDiagnosticSeverity.Error,
                            ContentDiagnosticCode.FileMetadataReadFailed,
                            "File metadata could not be verified after hashing: " + exception.Message,
                            source.Id,
                            relativePath));
                        return false;
                    }
                }
            }

            changed |= observedPaths.Count != expected.Count;
            if (changed)
            {
                diagnostics.Add(new ContentDiagnostic(
                    ContentDiagnosticSeverity.Error,
                    ContentDiagnosticCode.SourceTreeChangedDuringIndex,
                    "The source tree changed while it was being indexed; no manifest may be produced.",
                    source.Id,
                    source.RootPath));
            }

            return !changed;
        }

        private static string ToRelativePath(string rootPath, string fullPath)
        {
            string relativePath = Path.GetRelativePath(rootPath, fullPath)
                .Replace(Path.DirectorySeparatorChar, '/')
                .Replace(Path.AltDirectorySeparatorChar, '/');
            return relativePath == "." ? string.Empty : relativePath;
        }

        private sealed class IndexedFileObservation
        {
            public IndexedFileObservation(ContentFileRecord record, DateTime lastWriteTimeUtc)
            {
                Record = record ?? throw new ArgumentNullException(nameof(record));
                LastWriteTimeUtc = lastWriteTimeUtc;
            }

            public ContentFileRecord Record { get; }

            public DateTime LastWriteTimeUtc { get; }
        }

    }
}
