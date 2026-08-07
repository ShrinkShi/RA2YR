using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace RA2YR.Core.Content.ShpTs.Audit
{
    internal static class ShpTsAuditExternalManifestWriter
    {
        public static ShpTsAuditExternalManifestReference Write(
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
                    "A lowercase directory fingerprint is required.",
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
                    "m2-shp1/shp-ts-audits/" + sourceId + "/" + directoryFingerprint;
                string manifestFileName = manifestSha256.Substring(0, 16) + ".json";
                string relativePath = relativeDirectory + "/" + manifestFileName;
                string targetDirectory = Path.Combine(
                    configuration.CachePath,
                    "m2-shp1",
                    "shp-ts-audits",
                    sourceId,
                    directoryFingerprint);

                CreateVerifiedDirectory(configuration.CachePath);
                CreateVerifiedDirectory(Path.Combine(configuration.CachePath, "m2-shp1"));
                CreateVerifiedDirectory(Path.Combine(
                    configuration.CachePath,
                    "m2-shp1",
                    "shp-ts-audits"));
                CreateVerifiedDirectory(Path.Combine(
                    configuration.CachePath,
                    "m2-shp1",
                    "shp-ts-audits",
                    sourceId));
                CreateVerifiedDirectory(targetDirectory);

                string targetPath = Path.Combine(targetDirectory, manifestFileName);
                if (!RepositoryPathPolicy.IsInsideOrEqual(
                        targetPath,
                        configuration.CachePath) ||
                    string.Equals(
                        RepositoryPathPolicy.NormalizeAbsolutePath(targetPath),
                        RepositoryPathPolicy.NormalizeAbsolutePath(configuration.CachePath),
                        PathComparison))
                {
                    throw Failure("The external SHP manifest escaped its cache boundary.");
                }

                if (File.Exists(targetPath))
                {
                    VerifyExistingFile(targetPath, manifestBytes, manifestSha256);
                    return new ShpTsAuditExternalManifestReference(
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
                return new ShpTsAuditExternalManifestReference(
                    relativePath,
                    manifestBytes.LongLength,
                    manifestSha256);
            }
            catch (ShpTsProjectBaselineAuditException)
            {
                throw;
            }
            catch (Exception exception) when (IsExpectedFileException(exception))
            {
                throw Failure("The repository-external SHP manifest write failed closed.");
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
                        out failureReason) || overlaps)
                {
                    throw Failure(
                        "The external SHP cache boundary could not be verified safely.");
                }
            }

            string aliasReason;
            string reparsePoint;
            if (RepositoryPathPolicy.TryFindUnsupportedAlias(
                    configuration.CachePath,
                    out aliasReason) ||
                RepositoryPathPolicy.ContainsExistingReparsePoint(
                    configuration.CachePath,
                    out reparsePoint))
            {
                throw Failure("The external SHP cache uses an unsupported path identity.");
            }
        }

        private static void CreateVerifiedDirectory(string path)
        {
            if (File.Exists(path))
            {
                throw Failure("An external SHP cache directory is occupied by a file.");
            }

            string reparsePoint;
            if (RepositoryPathPolicy.ContainsExistingReparsePoint(path, out reparsePoint))
            {
                throw Failure("An external SHP cache directory traverses a reparse point.");
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
                throw Failure("An external SHP cache path is not a regular directory.");
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
                throw Failure("An external SHP manifest is not a regular file.");
            }

            byte[] actual = File.ReadAllBytes(path);
            if (!actual.SequenceEqual(expected) ||
                !string.Equals(
                    ComputeSha256(actual),
                    expectedSha256,
                    StringComparison.Ordinal))
            {
                throw Failure(
                    "An existing content-addressed SHP manifest does not match its expected bytes.");
            }
        }

        private static string ComputeSha256(byte[] bytes)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                return Sha256Utilities.ToLowerHex(sha256.ComputeHash(bytes));
            }
        }

        private static bool IsExpectedFileException(Exception exception)
        {
            return exception is IOException ||
                   exception is UnauthorizedAccessException ||
                   exception is NotSupportedException ||
                   exception is System.Security.SecurityException;
        }

        private static ShpTsProjectBaselineAuditException Failure(string message)
        {
            return new ShpTsProjectBaselineAuditException(
                ShpTsProjectBaselineAuditFailureCode.ExternalManifestWriteFailed,
                message);
        }

        private static StringComparison PathComparison =>
            Path.DirectorySeparatorChar == '\\'
                ? StringComparison.OrdinalIgnoreCase
                : StringComparison.Ordinal;
    }
}
