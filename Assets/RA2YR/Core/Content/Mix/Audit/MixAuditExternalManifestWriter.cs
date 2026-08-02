using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace RA2YR.Core.Content.Mix.Audit
{
    internal sealed class MixAuditExternalManifestReference
    {
        public MixAuditExternalManifestReference(
            string cacheRelativePath,
            long length,
            string sha256)
        {
            CacheRelativePath = LogicalContentPath.Parse(cacheRelativePath).Value;
            if (length < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(length));
            }

            if (!Sha256Utilities.IsLowerSha256(sha256))
            {
                throw new ArgumentException("A lowercase SHA-256 is required.", nameof(sha256));
            }

            Length = length;
            Sha256 = sha256;
        }

        public string CacheRelativePath { get; }

        public long Length { get; }

        public string Sha256 { get; }
    }

    internal static class MixAuditExternalManifestWriter
    {
        public static MixAuditExternalManifestReference Write(
            ExternalContentConfiguration configuration,
            string sourceId,
            string directoryFingerprint,
            byte[] manifestBytes)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            if (!ContentConfigurationValueRules.IsValidSourceId(sourceId))
            {
                throw new ArgumentException("A valid source id is required.", nameof(sourceId));
            }

            if (!Sha256Utilities.IsLowerSha256(directoryFingerprint))
            {
                throw new ArgumentException(
                    "A lowercase SHA-256 is required.",
                    nameof(directoryFingerprint));
            }

            if (manifestBytes == null)
            {
                throw new ArgumentNullException(nameof(manifestBytes));
            }

            try
            {
                ValidateCacheBoundary(configuration);
                string manifestSha256 = ComputeSha256(manifestBytes);
                string relativeDirectory =
                    "wp02c/mix-audits/" + sourceId + "/" + directoryFingerprint;
                string manifestFileName =
                    manifestSha256.Substring(0, 16) + ".json";
                string relativePath = relativeDirectory + "/" + manifestFileName;
                string targetDirectory = Path.Combine(
                    configuration.CachePath,
                    "wp02c",
                    "mix-audits",
                    sourceId,
                    directoryFingerprint);

                CreateVerifiedDirectory(configuration.CachePath);
                CreateVerifiedDirectory(Path.Combine(configuration.CachePath, "wp02c"));
                CreateVerifiedDirectory(Path.Combine(configuration.CachePath, "wp02c", "mix-audits"));
                CreateVerifiedDirectory(Path.Combine(
                    configuration.CachePath,
                    "wp02c",
                    "mix-audits",
                    sourceId));
                CreateVerifiedDirectory(targetDirectory);

                string targetPath = Path.Combine(targetDirectory, manifestFileName);
                if (!RepositoryPathPolicy.IsInsideOrEqual(targetPath, configuration.CachePath) ||
                    string.Equals(
                        RepositoryPathPolicy.NormalizeAbsolutePath(targetPath),
                        RepositoryPathPolicy.NormalizeAbsolutePath(configuration.CachePath),
                        PathComparison))
                {
                    throw Failure("The external MIX manifest target escaped its cache boundary.");
                }

                if (File.Exists(targetPath))
                {
                    VerifyExistingFile(targetPath, manifestBytes, manifestSha256);
                    return new MixAuditExternalManifestReference(
                        relativePath,
                        manifestBytes.LongLength,
                        manifestSha256);
                }

                string temporaryPath = Path.Combine(
                    targetDirectory,
                    "." + Guid.NewGuid().ToString("N") + ".tmp");
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
                        stream.Flush(true);
                    }

                    VerifyExistingFile(temporaryPath, manifestBytes, manifestSha256);
                    EnsureRegularDirectory(targetDirectory);
                    try
                    {
                        File.Move(temporaryPath, targetPath);
                    }
                    catch (IOException) when (File.Exists(targetPath))
                    {
                        VerifyExistingFile(targetPath, manifestBytes, manifestSha256);
                    }
                }
                finally
                {
                    if (File.Exists(temporaryPath))
                    {
                        File.Delete(temporaryPath);
                    }
                }

                VerifyExistingFile(targetPath, manifestBytes, manifestSha256);
                return new MixAuditExternalManifestReference(
                    relativePath,
                    manifestBytes.LongLength,
                    manifestSha256);
            }
            catch (MixBaselineAuditException)
            {
                throw;
            }
            catch (Exception exception) when (IsExpectedFileException(exception))
            {
                throw Failure(
                    "The repository-external MIX audit manifest write failed closed.");
            }
        }

        private static void ValidateCacheBoundary(
            ExternalContentConfiguration configuration)
        {
            var protectedPaths = new List<string> { configuration.RepositoryRoot };
            protectedPaths.AddRange(configuration.Sources.Select(source => source.RootPath));
            foreach (string protectedPath in protectedPaths)
            {
                bool overlaps;
                string failureReason;
                if (!RepositoryPathPolicy.TryDetermineOverlap(
                    configuration.CachePath,
                    protectedPath,
                    out overlaps,
                    out failureReason))
                {
                    throw Failure("The external cache storage identity could not be verified.");
                }

                if (overlaps)
                {
                    throw Failure("The external cache overlaps protected repository or source data.");
                }
            }

            string aliasReason;
            if (RepositoryPathPolicy.TryFindUnsupportedAlias(
                configuration.CachePath,
                out aliasReason))
            {
                throw Failure("The external cache uses an unsupported host path alias.");
            }

            string reparsePoint;
            if (RepositoryPathPolicy.ContainsExistingReparsePoint(
                configuration.CachePath,
                out reparsePoint))
            {
                throw Failure("The external cache traverses a reparse point.");
            }
        }

        private static void CreateVerifiedDirectory(string path)
        {
            if (File.Exists(path))
            {
                throw Failure("An external cache directory is occupied by a file.");
            }

            string reparsePoint;
            if (RepositoryPathPolicy.ContainsExistingReparsePoint(path, out reparsePoint))
            {
                throw Failure("An external cache directory traverses a reparse point.");
            }

            Directory.CreateDirectory(path);
            EnsureRegularDirectory(path);
        }

        private static void EnsureRegularDirectory(string path)
        {
            FileAttributes attributes = File.GetAttributes(path);
            if ((attributes & FileAttributes.Directory) == 0 ||
                (attributes & FileAttributes.ReparsePoint) != 0)
            {
                throw Failure("An external cache path is not a regular directory.");
            }
        }

        private static void VerifyExistingFile(
            string path,
            byte[] expected,
            string expectedSha256)
        {
            FileAttributes attributes = File.GetAttributes(path);
            if ((attributes & FileAttributes.Directory) != 0 ||
                (attributes & FileAttributes.ReparsePoint) != 0)
            {
                throw Failure("An external manifest target is not a regular file.");
            }

            using (var stream = new FileStream(
                path,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                64 * 1024,
                FileOptions.SequentialScan))
            {
                if (stream.Length != expected.LongLength)
                {
                    throw Failure("An external manifest target has an unexpected length.");
                }

                var buffer = new byte[64 * 1024];
                int expectedOffset = 0;
                using (SHA256 sha256 = SHA256.Create())
                {
                    while (true)
                    {
                        int read = stream.Read(buffer, 0, buffer.Length);
                        if (read == 0)
                        {
                            break;
                        }

                        sha256.TransformBlock(buffer, 0, read, buffer, 0);
                        for (int index = 0; index < read; index++)
                        {
                            if (buffer[index] != expected[expectedOffset + index])
                            {
                                throw Failure(
                                    "An existing content-addressed manifest does not match its expected bytes.");
                            }
                        }

                        expectedOffset = checked(expectedOffset + read);
                    }

                    sha256.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
                    string actualSha256 = ToHex(sha256.Hash);
                    if (expectedOffset != expected.Length ||
                        !string.Equals(actualSha256, expectedSha256, StringComparison.Ordinal))
                    {
                        throw Failure(
                            "An existing content-addressed manifest failed digest verification.");
                    }
                }
            }
        }

        private static string ComputeSha256(byte[] bytes)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                return ToHex(sha256.ComputeHash(bytes));
            }
        }

        private static string ToHex(byte[] bytes)
        {
            return BitConverter.ToString(bytes).Replace("-", string.Empty).ToLowerInvariant();
        }

        private static bool IsExpectedFileException(Exception exception)
        {
            return exception is IOException ||
                   exception is UnauthorizedAccessException ||
                   exception is NotSupportedException ||
                   exception is System.Security.SecurityException;
        }

        private static MixBaselineAuditException Failure(
            string message)
        {
            return new MixBaselineAuditException(
                MixBaselineAuditFailureCode.ExternalManifestWriteFailed,
                message);
        }

        private static StringComparison PathComparison =>
            Path.DirectorySeparatorChar == '\\'
                ? StringComparison.OrdinalIgnoreCase
                : StringComparison.Ordinal;
    }
}
