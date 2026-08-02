using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace RA2YR.Core.Content
{
    public sealed class ExternalManifestWriteResult
    {
        internal ExternalManifestWriteResult(
            int schemaVersion,
            string cacheRelativePath,
            long length,
            string sha256)
        {
            SchemaVersion = schemaVersion;
            CacheRelativePath = LogicalContentPath.Parse(cacheRelativePath).Value;
            Length = length;
            Sha256 = Sha256Utilities.IsLowerSha256(sha256)
                ? sha256
                : throw new ArgumentException("A lowercase SHA-256 value is required.", nameof(sha256));
        }

        public int SchemaVersion { get; }

        public string CacheRelativePath { get; }

        public long Length { get; }

        public string Sha256 { get; }
    }

    public sealed class ContentManifestWriteException : IOException
    {
        internal ContentManifestWriteException(
            string sourceId,
            Exception innerException)
            : base(
                "The repository-external content manifest could not be written safely.",
                innerException)
        {
            Diagnostic = new ContentDiagnostic(
                ContentDiagnosticSeverity.Error,
                ContentDiagnosticCode.ContentManifestWriteFailed,
                "The repository-external content manifest write failed closed.",
                sourceId,
                "manifests/" + sourceId);
        }

        public ContentDiagnostic Diagnostic { get; }
    }

    public sealed class ExternalContentManifestWriter
    {
        public ExternalManifestWriteResult Write(
            ExternalContentConfiguration configuration,
            ContentResolutionResult resolution,
            string manifestSourceId)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            if (resolution == null)
            {
                throw new ArgumentNullException(nameof(resolution));
            }

            if (!ContentConfigurationValueRules.IsValidSourceId(manifestSourceId))
            {
                throw new ArgumentException("A valid source id is required.", nameof(manifestSourceId));
            }

            try
            {
                return WriteCore(configuration, resolution, manifestSourceId);
            }
            catch (ContentManifestWriteException)
            {
                throw;
            }
            catch (Exception exception) when (
                exception is IOException ||
                exception is UnauthorizedAccessException ||
                exception is System.Security.SecurityException)
            {
                throw new ContentManifestWriteException(manifestSourceId, exception);
            }
        }

        private static ExternalManifestWriteResult WriteCore(
            ExternalContentConfiguration configuration,
            ContentResolutionResult resolution,
            string manifestSourceId)
        {

            ValidateResolutionMatchesConfiguration(configuration, resolution, manifestSourceId);
            ValidateCacheBoundary(configuration);

            byte[] manifestBytes =
                ContentResolutionManifestSerializer.SerializeCanonicalUtf8(resolution);
            string manifestSha =
                ContentResolutionManifestSerializer.ComputeCanonicalSha256(resolution);
            string relativeDirectory = "manifests/" + manifestSourceId;
            string relativePath = relativeDirectory + "/" + manifestSha + ".json";
            string targetDirectory = Path.Combine(
                configuration.CachePath,
                "manifests",
                manifestSourceId);

            CreateVerifiedDirectory(configuration.CachePath);
            CreateVerifiedDirectory(Path.Combine(configuration.CachePath, "manifests"));
            CreateVerifiedDirectory(targetDirectory);

            string targetPath = Path.Combine(targetDirectory, manifestSha + ".json");
            if (File.Exists(targetPath))
            {
                VerifyExistingManifest(targetPath, manifestBytes);
                return new ExternalManifestWriteResult(
                    ContentResolutionManifestSerializer.SchemaVersion,
                    relativePath,
                    manifestBytes.LongLength,
                    manifestSha);
            }

            string temporaryPath = Path.Combine(
                targetDirectory,
                "." + manifestSha + "." + Guid.NewGuid().ToString("N") + ".tmp");
            try
            {
                EnsureRegularDirectory(targetDirectory);
                using (var stream = new FileStream(
                           temporaryPath,
                           FileMode.CreateNew,
                           FileAccess.Write,
                           FileShare.None,
                           64 * 1024,
                           FileOptions.WriteThrough))
                {
                    stream.Write(manifestBytes, 0, manifestBytes.Length);
                    stream.Flush();
                    EnsureRegularDirectory(targetDirectory);
                }

                try
                {
                    File.Move(temporaryPath, targetPath);
                }
                catch (IOException) when (File.Exists(targetPath))
                {
                    VerifyExistingManifest(targetPath, manifestBytes);
                }
            }
            finally
            {
                if (File.Exists(temporaryPath))
                {
                    File.Delete(temporaryPath);
                }
            }

            VerifyExistingManifest(targetPath, manifestBytes);
            return new ExternalManifestWriteResult(
                ContentResolutionManifestSerializer.SchemaVersion,
                relativePath,
                manifestBytes.LongLength,
                manifestSha);
        }

        private static void ValidateResolutionMatchesConfiguration(
            ExternalContentConfiguration configuration,
            ContentResolutionResult resolution,
            string manifestSourceId)
        {
            if (!resolution.IsComplete || resolution.HasErrors)
            {
                throw new InvalidOperationException(
                    "Only a complete resolution can be written as an external manifest.");
            }

            ExternalContentSourceDescriptor[] enabled = configuration.Sources
                .Where(source => source.Enabled)
                .OrderBy(source => source.Id, StringComparer.Ordinal)
                .ToArray();
            ContentResolutionSource[] resolved = resolution.Sources
                .OrderBy(source => source.Id, StringComparer.Ordinal)
                .ToArray();
            if (enabled.Length != resolved.Length)
            {
                throw new InvalidOperationException(
                    "The resolution source set does not match the enabled configuration.");
            }

            for (int index = 0; index < enabled.Length; index++)
            {
                ExternalContentSourceDescriptor descriptor = enabled[index];
                ContentResolutionSource source = resolved[index];
                if (!string.Equals(descriptor.Id, source.Id, StringComparison.Ordinal) ||
                    descriptor.Kind != source.Kind ||
                    descriptor.Priority != source.Priority ||
                    !string.Equals(descriptor.Version, source.Version, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        "The resolution source metadata does not match the configuration.");
                }

                bool sameRootIdentity;
                string identityFailure;
                if (!RepositoryPathPolicy.TryDetermineSameIdentity(
                        descriptor.RootPath,
                        source.RootPath,
                        out sameRootIdentity,
                        out identityFailure))
                {
                    throw new IOException(
                        "The resolution source storage identity could not be verified: " +
                        identityFailure);
                }

                if (!sameRootIdentity)
                {
                    throw new InvalidOperationException(
                        "The resolution was produced from a different source storage identity.");
                }
            }

            if (!resolved.Any(source =>
                    string.Equals(source.Id, manifestSourceId, StringComparison.Ordinal)))
            {
                throw new InvalidOperationException(
                    "The manifest source id is not an enabled resolved source.");
            }
        }

        private static void ValidateCacheBoundary(ExternalContentConfiguration configuration)
        {
            var paths = new List<string> { configuration.RepositoryRoot };
            paths.AddRange(configuration.Sources.Select(source => source.RootPath));
            foreach (string path in paths)
            {
                bool overlaps;
                string failure;
                if (!RepositoryPathPolicy.TryDetermineOverlap(
                        configuration.CachePath,
                        path,
                        out overlaps,
                        out failure))
                {
                    throw new IOException(
                        "The external manifest cache boundary could not be verified: " + failure);
                }

                if (overlaps)
                {
                    throw new IOException(
                        "The external manifest cache overlaps protected repository or source data.");
                }
            }

            string reparsePoint;
            if (RepositoryPathPolicy.ContainsExistingReparsePoint(
                    configuration.CachePath,
                    out reparsePoint))
            {
                throw new IOException(
                    "The external manifest cache traverses a reparse point.");
            }
        }

        private static void CreateVerifiedDirectory(string path)
        {
            if (File.Exists(path))
            {
                throw new IOException("A manifest cache directory path is occupied by a file.");
            }

            string reparsePoint;
            if (RepositoryPathPolicy.ContainsExistingReparsePoint(path, out reparsePoint))
            {
                throw new IOException("A manifest cache directory traverses a reparse point.");
            }

            Directory.CreateDirectory(path);
            if (RepositoryPathPolicy.ContainsExistingReparsePoint(path, out reparsePoint))
            {
                throw new IOException("A created manifest cache directory is a reparse point.");
            }
        }

        private static void VerifyExistingManifest(string path, byte[] expected)
        {
            FileAttributes attributes = File.GetAttributes(path);
            if ((attributes & FileAttributes.ReparsePoint) != 0)
            {
                throw new IOException("A manifest target is a reparse point.");
            }

            byte[] actual = File.ReadAllBytes(path);
            if (!actual.SequenceEqual(expected))
            {
                throw new IOException(
                    "An existing content-addressed manifest does not match its expected bytes.");
            }
        }

        private static void EnsureRegularDirectory(string path)
        {
            FileAttributes attributes = File.GetAttributes(path);
            if ((attributes & FileAttributes.Directory) == 0 ||
                (attributes & FileAttributes.ReparsePoint) != 0)
            {
                throw new IOException(
                    "A manifest cache path is not a regular directory.");
            }
        }
    }
}
