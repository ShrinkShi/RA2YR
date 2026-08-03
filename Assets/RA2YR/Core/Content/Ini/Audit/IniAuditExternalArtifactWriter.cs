using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace RA2YR.Core.Content.Ini.Audit
{
    internal sealed class IniIdentityArtifactReference
    {
        public IniIdentityArtifactReference(string cacheRelativePath, long length, string sha256)
        {
            CacheRelativePath = LogicalContentPath.Parse(cacheRelativePath).Value;
            if (length < 0 || !Sha256Utilities.IsLowerSha256(sha256))
            {
                throw new ArgumentException("A valid identity artifact reference is required.");
            }

            Length = length;
            Sha256 = sha256;
        }

        public string CacheRelativePath { get; }
        public long Length { get; }
        public string Sha256 { get; }
    }

    internal static class IniAuditExternalArtifactWriter
    {
        public static IniIdentityArtifactReference WriteIdentity(
            ExternalContentConfiguration configuration,
            string sourceId,
            string directoryFingerprint,
            string sampleId,
            byte[] bytes)
        {
            ValidateCommon(configuration, sourceId, directoryFingerprint);
            if (!ContentConfigurationValueRules.IsValidSourceId(sampleId))
            {
                throw new ArgumentException("A stable sample id is required.", nameof(sampleId));
            }

            if (bytes == null)
            {
                throw new ArgumentNullException(nameof(bytes));
            }

            try
            {
                string sha256 = ComputeSha256(bytes);
                string relativeDirectory =
                    "wp02f/ini-audits/" + sourceId + "/" + directoryFingerprint +
                    "/identity";
                string fileName = sampleId + "-" + sha256.Substring(0, 16) + ".ini";
                string relativePath = relativeDirectory + "/" + fileName;
                string targetDirectory = EnsureAuditDirectory(
                    configuration,
                    sourceId,
                    directoryFingerprint,
                    "identity");
                string targetPath = GetBoundedTargetPath(
                    configuration,
                    targetDirectory,
                    fileName);
                WriteContentAddressedFile(targetPath, targetDirectory, bytes, sha256);
                return new IniIdentityArtifactReference(
                    relativePath,
                    bytes.LongLength,
                    sha256);
            }
            catch (IniProjectBaselineAuditException)
            {
                throw;
            }
            catch (Exception exception) when (IsExpectedFileException(exception))
            {
                throw Failure("The repository-external INI identity write failed closed.");
            }
        }

        public static IniAuditExternalManifestReference WriteManifest(
            ExternalContentConfiguration configuration,
            string sourceId,
            string directoryFingerprint,
            byte[] bytes)
        {
            ValidateCommon(configuration, sourceId, directoryFingerprint);
            if (bytes == null)
            {
                throw new ArgumentNullException(nameof(bytes));
            }

            try
            {
                string sha256 = ComputeSha256(bytes);
                string fileName = sha256.Substring(0, 16) + ".json";
                string relativePath =
                    "wp02f/ini-audits/" + sourceId + "/" + directoryFingerprint +
                    "/" + fileName;
                string targetDirectory = EnsureAuditDirectory(
                    configuration,
                    sourceId,
                    directoryFingerprint,
                    null);
                string targetPath = GetBoundedTargetPath(
                    configuration,
                    targetDirectory,
                    fileName);
                WriteContentAddressedFile(targetPath, targetDirectory, bytes, sha256);
                return new IniAuditExternalManifestReference(
                    relativePath,
                    bytes.LongLength,
                    sha256);
            }
            catch (IniProjectBaselineAuditException)
            {
                throw;
            }
            catch (Exception exception) when (IsExpectedFileException(exception))
            {
                throw Failure("The repository-external INI manifest write failed closed.");
            }
        }

        public static byte[] ReadIdentity(
            ExternalContentConfiguration configuration,
            IniIdentityArtifactReference reference,
            long maximumBytes)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            if (reference == null || maximumBytes < 0 || reference.Length > maximumBytes ||
                reference.Length > int.MaxValue)
            {
                throw Failure("The external identity read exceeds its explicit budget.");
            }

            string path = Path.Combine(
                configuration.CachePath,
                reference.CacheRelativePath.Replace('/', Path.DirectorySeparatorChar));
            if (!RepositoryPathPolicy.IsInsideOrEqual(path, configuration.CachePath))
            {
                throw Failure("The external identity read escaped its cache boundary.");
            }

            try
            {
                EnsurePathChainHasNoReparsePoint(path);
                EnsureRegularFile(path);
                byte[] bytes = ReadExactRegularFile(path, checked((int)reference.Length));
                if (bytes.LongLength != reference.Length ||
                    !string.Equals(
                        ComputeSha256(bytes),
                        reference.Sha256,
                        StringComparison.Ordinal))
                {
                    throw Failure("The external INI identity artifact changed after publication.");
                }

                return bytes;
            }
            catch (IniProjectBaselineAuditException)
            {
                throw;
            }
            catch (Exception exception) when (IsExpectedFileException(exception))
            {
                throw Failure("The external INI identity artifact could not be read safely.");
            }
        }

        private static void ValidateCommon(
            ExternalContentConfiguration configuration,
            string sourceId,
            string directoryFingerprint)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            if (!ContentConfigurationValueRules.IsValidSourceId(sourceId) ||
                !Sha256Utilities.IsLowerSha256(directoryFingerprint))
            {
                throw new ArgumentException("A valid source identity is required.");
            }

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
                    throw Failure("The external INI cache boundary could not be verified safely.");
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
                throw Failure("The external INI cache uses an unsupported path identity.");
            }
        }

        private static string EnsureAuditDirectory(
            ExternalContentConfiguration configuration,
            string sourceId,
            string fingerprint,
            string leaf)
        {
            var parts = new List<string>
            {
                configuration.CachePath,
                "wp02f",
                "ini-audits",
                sourceId,
                fingerprint
            };
            if (leaf != null)
            {
                parts.Add(leaf);
            }

            string current = parts[0];
            CreateVerifiedDirectory(current);
            for (int index = 1; index < parts.Count; index++)
            {
                current = Path.Combine(current, parts[index]);
                CreateVerifiedDirectory(current);
            }

            return current;
        }

        private static string GetBoundedTargetPath(
            ExternalContentConfiguration configuration,
            string directory,
            string fileName)
        {
            string path = Path.Combine(directory, fileName);
            if (!RepositoryPathPolicy.IsInsideOrEqual(path, configuration.CachePath) ||
                string.Equals(
                    RepositoryPathPolicy.NormalizeAbsolutePath(path),
                    RepositoryPathPolicy.NormalizeAbsolutePath(configuration.CachePath),
                    PathComparison))
            {
                throw Failure("An external INI artifact escaped its cache boundary.");
            }

            return path;
        }

        private static void CreateVerifiedDirectory(string path)
        {
            if (File.Exists(path))
            {
                throw Failure("An external INI cache directory is occupied by a file.");
            }

            string reparsePoint;
            if (RepositoryPathPolicy.ContainsExistingReparsePoint(path, out reparsePoint))
            {
                throw Failure("An external INI cache directory traverses a reparse point.");
            }

            Directory.CreateDirectory(path);
            FileAttributes attributes = File.GetAttributes(path);
            if ((attributes & FileAttributes.Directory) == 0 ||
                (attributes & FileAttributes.ReparsePoint) != 0)
            {
                throw Failure("An external INI cache path is not a regular directory.");
            }
        }

        private static void WriteContentAddressedFile(
            string targetPath,
            string targetDirectory,
            byte[] bytes,
            string sha256)
        {
            EnsureRegularDirectory(targetDirectory);
            EnsurePathChainHasNoReparsePoint(targetPath);
            if (File.Exists(targetPath))
            {
                VerifyFile(targetPath, bytes, sha256);
                return;
            }

            string temporaryPath = Path.Combine(
                targetDirectory,
                "." + Guid.NewGuid().ToString("N") + ".tmp");
            try
            {
                using (var stream = new FileStream(
                    temporaryPath,
                    FileMode.CreateNew,
                    FileAccess.Write,
                    FileShare.None,
                    64 * 1024,
                    FileOptions.WriteThrough))
                {
                    stream.Write(bytes, 0, bytes.Length);
                    stream.Flush(true);
                }

                VerifyFile(temporaryPath, bytes, sha256);
                EnsureRegularDirectory(targetDirectory);
                EnsurePathChainHasNoReparsePoint(targetPath);
                try
                {
                    File.Move(temporaryPath, targetPath);
                }
                catch (IOException) when (File.Exists(targetPath))
                {
                    VerifyFile(targetPath, bytes, sha256);
                }
            }
            finally
            {
                if (File.Exists(temporaryPath))
                {
                    File.Delete(temporaryPath);
                }
            }

            VerifyFile(targetPath, bytes, sha256);
        }

        private static void VerifyFile(string path, byte[] expected, string expectedSha256)
        {
            EnsurePathChainHasNoReparsePoint(path);
            EnsureRegularFile(path);
            byte[] actual = ReadExactRegularFile(path, expected.Length);
            if (!actual.SequenceEqual(expected) ||
                !string.Equals(ComputeSha256(actual), expectedSha256, StringComparison.Ordinal))
            {
                throw Failure("An existing content-addressed INI artifact does not match.");
            }
        }

        private static void EnsureRegularFile(string path)
        {
            FileAttributes attributes = File.GetAttributes(path);
            if ((attributes & FileAttributes.Directory) != 0 ||
                (attributes & FileAttributes.ReparsePoint) != 0)
            {
                throw Failure("An external INI artifact is not a regular file.");
            }
        }

        private static void EnsureRegularDirectory(string path)
        {
            EnsurePathChainHasNoReparsePoint(path);
            FileAttributes attributes = File.GetAttributes(path);
            if ((attributes & FileAttributes.Directory) == 0 ||
                (attributes & FileAttributes.ReparsePoint) != 0)
            {
                throw Failure("An external INI artifact directory is not regular.");
            }
        }

        private static void EnsurePathChainHasNoReparsePoint(string path)
        {
            string reparsePoint;
            if (RepositoryPathPolicy.ContainsExistingReparsePoint(path, out reparsePoint))
            {
                throw Failure("An external INI artifact path traverses a reparse point.");
            }
        }

        private static byte[] ReadExactRegularFile(string path, int expectedLength)
        {
            if (expectedLength < 0)
            {
                throw Failure("An external INI artifact length is invalid.");
            }

            var bytes = new byte[expectedLength];
            using (var stream = new FileStream(
                path,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                64 * 1024,
                FileOptions.SequentialScan))
            {
                if (!stream.CanSeek || stream.Length != expectedLength)
                {
                    throw Failure("An external INI artifact length changed.");
                }

                int offset = 0;
                while (offset < bytes.Length)
                {
                    int read = stream.Read(bytes, offset, bytes.Length - offset);
                    if (read == 0)
                    {
                        throw Failure("An external INI artifact ended before its declared length.");
                    }

                    offset = checked(offset + read);
                }

                if (stream.ReadByte() != -1)
                {
                    throw Failure("An external INI artifact contains unexpected trailing bytes.");
                }
            }

            return bytes;
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

        private static IniProjectBaselineAuditException Failure(string message)
        {
            return new IniProjectBaselineAuditException(
                IniProjectBaselineAuditFailureCode.ExternalArtifactWriteFailed,
                message);
        }

        private static StringComparison PathComparison =>
            Path.DirectorySeparatorChar == '\\'
                ? StringComparison.OrdinalIgnoreCase
                : StringComparison.Ordinal;
    }
}
