using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using RA2YR.Core.Binary;
using RA2YR.Core.Content;
using RA2YR.Core.Formats.Mix.Crypto;

namespace RA2YR.Core.Formats.Mix.Writing
{
    internal static class MixArchiveWriter
    {
        private const uint ChecksumFlag = 0x00010000u;
        private const uint EncryptedFlag = 0x00020000u;
        private const int ClassicHeaderLength = 6;
        private const int ExtendedFlagsLength = 4;
        private const int DirectoryEntryLength = 12;
        private const int ChecksumLength = 20;

        public static MixWriteResult Build(
            IReadOnlyList<MixWriteEntry> entries,
            MixWriteOptions options)
        {
            if (entries == null)
            {
                throw new ArgumentNullException(nameof(entries));
            }

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            MixWriteResult snapshotFailure = SnapshotEntries(
                entries,
                Math.Min(options.MaxEntryCount, ushort.MaxValue),
                out List<MixWriteEntry> snapshot);
            return snapshotFailure ?? BuildSnapshot(snapshot, options);
        }

        private static MixWriteResult BuildSnapshot(
            IReadOnlyList<MixWriteEntry> entries,
            MixWriteOptions options)
        {
            MixWriteResult optionFailure = ValidateOptions(options);
            if (optionFailure != null)
            {
                return optionFailure;
            }

            if (entries.Count > ushort.MaxValue || entries.Count > options.MaxEntryCount)
            {
                return Failure(
                    MixWriteDiagnosticCode.EntryBudgetExceeded,
                    -1,
                    null,
                    "The MIX entry count exceeds the configured or format limit.");
            }

            var ordered = new List<MixWriteEntry>(entries.Count);
            var observedIds = new HashSet<uint>();
            long dataSize = 0;
            try
            {
                for (int index = 0; index < entries.Count; index++)
                {
                    MixWriteEntry entry = entries[index];
                    if (entry == null)
                    {
                        return Failure(
                            MixWriteDiagnosticCode.InvalidEntry,
                            index,
                            null,
                            "A MIX write entry cannot be null.");
                    }

                    if (!observedIds.Add(entry.Id.Value))
                    {
                        return Failure(
                            MixWriteDiagnosticCode.DuplicateEntryId,
                            index,
                            entry.Id,
                            "A MIX archive cannot contain duplicate entry IDs.");
                    }

                    dataSize = checked(dataSize + entry.Length);
                    ordered.Add(entry);
                }
            }
            catch (OverflowException)
            {
                return Failure(
                    MixWriteDiagnosticCode.ArithmeticOverflow,
                    -1,
                    null,
                    "MIX payload size arithmetic overflowed.");
            }

            if (dataSize > uint.MaxValue)
            {
                return Failure(
                    MixWriteDiagnosticCode.ArchiveSizeBudgetExceeded,
                    -1,
                    null,
                    "The MIX data region exceeds the format limit.");
            }

            if (options.Order == MixWriteOrder.DeterministicRebuild)
            {
                ordered.Sort((left, right) => left.Id.CompareTo(right.Id));
            }

            long plainDirectoryLength;
            long storedDirectoryLength;
            long archiveLength;
            try
            {
                plainDirectoryLength = checked(
                    ClassicHeaderLength + ((long)ordered.Count * DirectoryEntryLength));
                storedDirectoryLength = options.IsEncrypted
                    ? checked((plainDirectoryLength + BlowfishCipher.BlockSize - 1) &
                              ~(long)(BlowfishCipher.BlockSize - 1))
                    : plainDirectoryLength;
                archiveLength = checked(
                    (options.HeaderKind == MixWriteHeaderKind.Extended
                        ? ExtendedFlagsLength
                        : 0L) +
                    (options.IsEncrypted
                        ? WestwoodMixKeyDeriver.KeySourceLength
                        : 0L) +
                    storedDirectoryLength +
                    dataSize +
                    (options.IncludeChecksum ? ChecksumLength : 0L));
            }
            catch (OverflowException)
            {
                return Failure(
                    MixWriteDiagnosticCode.ArithmeticOverflow,
                    -1,
                    null,
                    "MIX archive size arithmetic overflowed.");
            }

            if (archiveLength > options.MaxArchiveBytes || archiveLength > int.MaxValue)
            {
                return Failure(
                    MixWriteDiagnosticCode.ArchiveSizeBudgetExceeded,
                    -1,
                    null,
                    "The MIX archive exceeds the configured in-memory output budget.");
            }

            byte[] keySource = options.GetEncryptionKeySource();
            BlowfishCipher cipher = null;
            if (options.IsEncrypted)
            {
                WestwoodMixKeyDerivationResult keyResult =
                    WestwoodMixKeyDeriver.Derive(keySource);
                if (!keyResult.IsSuccess)
                {
                    return Failure(
                        MixWriteDiagnosticCode.EncryptionKeyRejected,
                        -1,
                        null,
                        "The supplied Westwood MIX key source was rejected.");
                }

                cipher = new BlowfishCipher(keyResult.GetKeyMaterial());
            }

            var archive = new byte[checked((int)archiveLength)];
            int directoryStart;
            if (options.HeaderKind == MixWriteHeaderKind.Extended)
            {
                uint flags = (options.IncludeChecksum ? ChecksumFlag : 0u) |
                             (options.IsEncrypted ? EncryptedFlag : 0u);
                WriteUInt32(archive, 0, flags);
                if (options.IsEncrypted)
                {
                    Buffer.BlockCopy(
                        keySource,
                        0,
                        archive,
                        ExtendedFlagsLength,
                        keySource.Length);
                    directoryStart = ExtendedFlagsLength + keySource.Length;
                }
                else
                {
                    directoryStart = ExtendedFlagsLength;
                }
            }
            else
            {
                directoryStart = 0;
            }

            byte[] plainDirectory = options.IsEncrypted
                ? new byte[checked((int)storedDirectoryLength)]
                : archive;
            int plainDirectoryStart = options.IsEncrypted ? 0 : directoryStart;
            WriteDirectory(
                plainDirectory,
                plainDirectoryStart,
                ordered,
                checked((uint)dataSize));

            if (options.IsEncrypted)
            {
                for (int offset = 0; offset < plainDirectory.Length; offset += BlowfishCipher.BlockSize)
                {
                    cipher.EncryptWestwoodLittleEndianWordBlock(
                        plainDirectory.AsSpan(offset, BlowfishCipher.BlockSize),
                        archive.AsSpan(directoryStart + offset, BlowfishCipher.BlockSize));
                }
            }

            int dataStart = checked(directoryStart + (int)storedDirectoryLength);
            int dataOffset = 0;
            foreach (MixWriteEntry entry in ordered)
            {
                entry.Payload.Span.CopyTo(archive.AsSpan(dataStart + dataOffset, entry.Length));
                dataOffset = checked(dataOffset + entry.Length);
            }

            if (options.IncludeChecksum)
            {
                byte[] checksum;
                using (SHA1 sha1 = SHA1.Create())
                {
                    checksum = sha1.ComputeHash(archive, dataStart, checked((int)dataSize));
                }

                Buffer.BlockCopy(
                    checksum,
                    0,
                    archive,
                    dataStart + checked((int)dataSize),
                    checksum.Length);
            }

            return MixWriteResult.Success(
                archive,
                ComputeSha256(archive),
                false,
                false);
        }

        public static MixWriteResult WriteToFile(
            IReadOnlyList<MixWriteEntry> entries,
            MixWriteOptions options,
            string outputPath,
            string approvedOutputRoot,
            MixOutputPurpose purpose,
            bool allowOverwrite)
        {
            if (entries == null)
            {
                throw new ArgumentNullException(nameof(entries));
            }

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            MixWriteResult pathFailure = ValidateOutputTarget(
                outputPath,
                approvedOutputRoot,
                purpose,
                allowOverwrite,
                out string fullPath);
            if (pathFailure != null)
            {
                return pathFailure;
            }

            MixWriteResult snapshotFailure = SnapshotEntries(
                entries,
                Math.Min(options.MaxEntryCount, ushort.MaxValue),
                out List<MixWriteEntry> snapshot);
            if (snapshotFailure != null)
            {
                return snapshotFailure;
            }

            MixWriteResult build = BuildSnapshot(snapshot, options);
            if (!build.IsSuccess)
            {
                return build;
            }

            string directory = Path.GetDirectoryName(fullPath);
            string temporaryPath = Path.Combine(
                directory,
                "." + Guid.NewGuid().ToString("N") + ".ra2yr-mix.tmp");
            try
            {
                using (var stream = new FileStream(
                    temporaryPath,
                    FileMode.CreateNew,
                    FileAccess.Write,
                    FileShare.None))
                {
                    build.WriteArchiveTo(stream);
                    stream.Flush(true);
                }
            }
            catch (Exception exception) when (IsExpectedFileException(exception))
            {
                TryDeleteTemporaryFile(temporaryPath);
                return Failure(
                    MixWriteDiagnosticCode.TemporaryWriteFailed,
                    -1,
                    null,
                    "The temporary MIX output could not be written and flushed.");
            }

            if (!VerifyWrittenFile(temporaryPath, build.ArchiveSize, build.Sha256) ||
                !VerifyArchiveSemantics(temporaryPath, snapshot, options))
            {
                TryDeleteTemporaryFile(temporaryPath);
                return Failure(
                    MixWriteDiagnosticCode.WrittenFileVerificationFailed,
                    -1,
                    null,
                    "The flushed temporary MIX output did not match the in-memory archive.");
            }

            try
            {
                if (File.Exists(fullPath))
                {
                    if (!allowOverwrite)
                    {
                        TryDeleteTemporaryFile(temporaryPath);
                        return Failure(
                            MixWriteDiagnosticCode.OutputAlreadyExists,
                            -1,
                            null,
                            "The MIX output target already exists and overwrite was not authorized.");
                    }

                    File.Replace(temporaryPath, fullPath, null);
                }
                else
                {
                    File.Move(temporaryPath, fullPath);
                }
            }
            catch (Exception exception) when (IsExpectedFileException(exception))
            {
                TryDeleteTemporaryFile(temporaryPath);
                return Failure(
                    MixWriteDiagnosticCode.AtomicCommitFailed,
                    -1,
                    null,
                    "The verified MIX output could not be atomically committed.");
            }

            return build.AsCommittedFile();
        }

        private static MixWriteResult ValidateOptions(MixWriteOptions options)
        {
            if (options.HeaderKind == MixWriteHeaderKind.Classic &&
                (options.IncludeChecksum || options.IsEncrypted))
            {
                return Failure(
                    MixWriteDiagnosticCode.InvalidOptionCombination,
                    -1,
                    null,
                    "Checksum and encrypted directories require the extended MIX header.");
            }

            if (options.HeaderKind == MixWriteHeaderKind.Extended &&
                !options.IncludeChecksum && !options.IsEncrypted)
            {
                return Failure(
                    MixWriteDiagnosticCode.InvalidOptionCombination,
                    -1,
                    null,
                    "An extended MIX header requires at least one supported flag.");
            }

            return null;
        }

        private static MixWriteResult SnapshotEntries(
            IReadOnlyList<MixWriteEntry> entries,
            int maximumCount,
            out List<MixWriteEntry> snapshot)
        {
            snapshot = null;
            try
            {
                int count = entries.Count;
                if (count < 0)
                {
                    return Failure(
                        MixWriteDiagnosticCode.InvalidEntry,
                        -1,
                        null,
                        "The MIX entry collection reported an invalid count.");
                }

                if (count > maximumCount)
                {
                    return Failure(
                        MixWriteDiagnosticCode.EntryBudgetExceeded,
                        -1,
                        null,
                        "The MIX entry count exceeds the configured or format limit.");
                }

                var observed = new List<MixWriteEntry>(count);
                for (int index = 0; index < count; index++)
                {
                    observed.Add(entries[index]);
                }

                if (entries.Count != count)
                {
                    return Failure(
                        MixWriteDiagnosticCode.InvalidEntry,
                        -1,
                        null,
                        "The MIX entry collection changed while it was being captured.");
                }

                snapshot = observed;
                return null;
            }
            catch (Exception exception) when (
                exception is ArgumentOutOfRangeException ||
                exception is IndexOutOfRangeException ||
                exception is InvalidOperationException)
            {
                return Failure(
                    MixWriteDiagnosticCode.InvalidEntry,
                    -1,
                    null,
                    "The MIX entry collection could not be captured consistently.");
            }
        }

        private static MixWriteResult ValidateOutputTarget(
            string outputPath,
            string approvedOutputRoot,
            MixOutputPurpose purpose,
            bool allowOverwrite,
            out string fullPath)
        {
            fullPath = null;
            if (!Enum.IsDefined(typeof(MixOutputPurpose), purpose))
            {
                return Failure(
                    MixWriteDiagnosticCode.OutputPurposeInvalid,
                    -1,
                    null,
                    "A controlled MIX output purpose is required.");
            }

            if (string.IsNullOrWhiteSpace(outputPath) || !Path.IsPathRooted(outputPath) ||
                string.IsNullOrWhiteSpace(approvedOutputRoot) ||
                !Path.IsPathRooted(approvedOutputRoot))
            {
                return Failure(
                    MixWriteDiagnosticCode.OutputPathInvalid,
                    -1,
                    null,
                    "The MIX output target must be an explicit absolute file path.");
            }

            try
            {
                fullPath = Path.GetFullPath(outputPath);
                string fullRoot = Path.GetFullPath(approvedOutputRoot);
                string directory = Path.GetDirectoryName(fullPath);
                string fileSystemRoot = Path.GetPathRoot(fullRoot);
                if (string.Equals(
                    TrimTrailingSeparators(fullRoot),
                    TrimTrailingSeparators(fileSystemRoot),
                    PathComparison))
                {
                    return Failure(
                        MixWriteDiagnosticCode.OutputPathInvalid,
                        -1,
                        null,
                        "A filesystem root is too broad to approve as a MIX output root.");
                }

                if (!Directory.Exists(fullRoot))
                {
                    return Failure(
                        MixWriteDiagnosticCode.OutputDirectoryMissing,
                        -1,
                        null,
                        "The approved MIX output root does not exist.");
                }

                string aliasReason;
                if (RepositoryPathPolicy.TryFindUnsupportedAlias(fullRoot, out aliasReason) ||
                    RepositoryPathPolicy.TryFindUnsupportedAlias(fullPath, out aliasReason) ||
                    ContainsWindowsAliasSegment(fullRoot) ||
                    ContainsWindowsAliasSegment(fullPath))
                {
                    return Failure(
                        MixWriteDiagnosticCode.OutputPathInvalid,
                        -1,
                        null,
                        "The MIX output boundary contains an unsupported host path alias.");
                }

                string rootPrefix = EndsWithDirectorySeparator(fullRoot)
                    ? fullRoot
                    : fullRoot + Path.DirectorySeparatorChar;
                if (!fullPath.StartsWith(rootPrefix, PathComparison))
                {
                    return Failure(
                        MixWriteDiagnosticCode.OutputPathInvalid,
                        -1,
                        null,
                        "The MIX output target is outside the approved output root.");
                }

                if (string.IsNullOrWhiteSpace(Path.GetFileName(fullPath)))
                {
                    return Failure(
                        MixWriteDiagnosticCode.OutputPathInvalid,
                        -1,
                        null,
                        "The MIX output target must identify a file.");
                }

                if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory))
                {
                    return Failure(
                        MixWriteDiagnosticCode.OutputDirectoryMissing,
                        -1,
                        null,
                        "The explicitly selected MIX output directory does not exist.");
                }

                string reparsePoint;
                if (RepositoryPathPolicy.ContainsExistingReparsePoint(fullRoot, out reparsePoint) ||
                    RepositoryPathPolicy.ContainsExistingReparsePoint(fullPath, out reparsePoint))
                {
                    return Failure(
                        MixWriteDiagnosticCode.OutputReparsePointRejected,
                        -1,
                        null,
                        "The MIX output target traverses an unsupported reparse point.");
                }

                if (Directory.Exists(fullPath))
                {
                    return Failure(
                        MixWriteDiagnosticCode.OutputPathInvalid,
                        -1,
                        null,
                        "The MIX output target identifies a directory.");
                }

                if (File.Exists(fullPath) && !allowOverwrite)
                {
                    return Failure(
                        MixWriteDiagnosticCode.OutputAlreadyExists,
                        -1,
                        null,
                        "The MIX output target already exists and overwrite was not authorized.");
                }
            }
            catch (Exception exception) when (IsExpectedFileException(exception) || exception is ArgumentException)
            {
                fullPath = null;
                return Failure(
                    MixWriteDiagnosticCode.OutputPathInvalid,
                    -1,
                    null,
                    "The MIX output target could not be validated.");
            }

            return null;
        }

        private static bool VerifyArchiveSemantics(
            string path,
            IReadOnlyList<MixWriteEntry> entries,
            MixWriteOptions options)
        {
            try
            {
                var expectedEntries = new List<MixWriteEntry>(entries);
                if (options.Order == MixWriteOrder.DeterministicRebuild)
                {
                    expectedEntries.Sort((left, right) => left.Id.CompareTo(right.Id));
                }

                long fileLength = new FileInfo(path).Length;
                long directoryBytes = checked(
                    ClassicHeaderLength +
                    ((long)expectedEntries.Count * DirectoryEntryLength));
                long totalReadBudget = checked(fileLength * 3L);
                var limits = new MixReadLimits(
                    fileLength,
                    expectedEntries.Count,
                    Math.Max(directoryBytes, BlowfishCipher.BlockSize),
                    Math.Max(1024L * 1024, checked(directoryBytes * 4L)),
                    totalReadBudget,
                    expectedEntries.Count + 4L,
                    4);
                var source = new BinarySourceContext(
                    "MIX writer verification",
                    "generated-output",
                    LogicalContentPath.Parse("generated-output.mix"));

                MixArchiveReadResult readResult;
                using (var stream = new FileStream(
                    path,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read))
                {
                    readResult = MixArchiveReader.Read(
                        stream,
                        0,
                        fileLength,
                        source,
                        limits,
                        true);
                    if (!readResult.IsSuccess)
                    {
                        return false;
                    }

                    using (readResult.Archive)
                    {
                        MixArchive archive = readResult.Archive;
                        if (archive.Entries.Count != expectedEntries.Count ||
                            archive.HeaderKind !=
                                (options.HeaderKind == MixWriteHeaderKind.Classic
                                    ? MixArchiveHeaderKind.Classic
                                    : MixArchiveHeaderKind.Extended) ||
                            archive.HasChecksum != options.IncludeChecksum ||
                            archive.IsEncrypted != options.IsEncrypted ||
                            (options.IncludeChecksum && !archive.ChecksumVerified))
                        {
                            return false;
                        }

                        var buffer = new byte[64 * 1024];
                        for (int index = 0; index < expectedEntries.Count; index++)
                        {
                            MixWriteEntry expected = expectedEntries[index];
                            MixArchiveEntry actual = archive.Entries[index];
                            if (actual.Id != expected.Id || actual.Length != expected.Length ||
                                !PayloadMatches(actual, expected.Payload.Span, buffer))
                            {
                                return false;
                            }
                        }
                    }
                }

                return true;
            }
            catch (Exception exception) when (
                IsExpectedFileException(exception) ||
                exception is OverflowException ||
                exception is BinaryReadException)
            {
                return false;
            }
        }

        private static bool PayloadMatches(
            MixArchiveEntry actual,
            ReadOnlySpan<byte> expected,
            byte[] buffer)
        {
            long offset = 0;
            while (offset < expected.Length)
            {
                int count = Math.Min(buffer.Length, expected.Length - checked((int)offset));
                actual.OpenPayloadWindow().ReadExactly(
                    offset,
                    buffer,
                    0,
                    count,
                    "mix-write-verification");
                if (!buffer.AsSpan(0, count).SequenceEqual(
                    expected.Slice(checked((int)offset), count)))
                {
                    return false;
                }

                offset = checked(offset + count);
            }

            return true;
        }

        private static void WriteDirectory(
            byte[] destination,
            int start,
            IReadOnlyList<MixWriteEntry> entries,
            uint dataSize)
        {
            WriteUInt16(destination, start, checked((ushort)entries.Count));
            WriteUInt32(destination, start + 2, dataSize);

            uint relativeOffset = 0;
            for (int index = 0; index < entries.Count; index++)
            {
                MixWriteEntry entry = entries[index];
                int offset = checked(start + ClassicHeaderLength +
                                     (index * DirectoryEntryLength));
                WriteUInt32(destination, offset, entry.Id.Value);
                WriteUInt32(destination, offset + 4, relativeOffset);
                WriteUInt32(destination, offset + 8, checked((uint)entry.Length));
                relativeOffset = checked(relativeOffset + (uint)entry.Length);
            }
        }

        private static bool VerifyWrittenFile(
            string path,
            long expectedLength,
            string expectedSha256)
        {
            try
            {
                using (var stream = new FileStream(
                    path,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read))
                {
                    if (stream.Length != expectedLength)
                    {
                        return false;
                    }

                    using (SHA256 sha256 = SHA256.Create())
                    {
                        return string.Equals(
                            ToHex(sha256.ComputeHash(stream)),
                            expectedSha256,
                            StringComparison.Ordinal);
                    }
                }
            }
            catch (Exception exception) when (IsExpectedFileException(exception))
            {
                return false;
            }
        }

        private static string ComputeSha256(byte[] value)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                return ToHex(sha256.ComputeHash(value));
            }
        }

        private static string ToHex(byte[] value)
        {
            return BitConverter.ToString(value).Replace("-", string.Empty);
        }

        private static bool IsExpectedFileException(Exception exception)
        {
            return exception is IOException ||
                   exception is UnauthorizedAccessException ||
                   exception is NotSupportedException ||
                   exception is PathTooLongException ||
                   exception is System.Security.SecurityException;
        }

        private static StringComparison PathComparison =>
            Path.DirectorySeparatorChar == '\\'
                ? StringComparison.OrdinalIgnoreCase
                : StringComparison.Ordinal;

        private static bool EndsWithDirectorySeparator(string path)
        {
            return path.Length > 0 &&
                   (path[path.Length - 1] == Path.DirectorySeparatorChar ||
                    path[path.Length - 1] == Path.AltDirectorySeparatorChar);
        }

        private static string TrimTrailingSeparators(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return path;
            }

            int length = path.Length;
            while (length > 0 &&
                   (path[length - 1] == Path.DirectorySeparatorChar ||
                    path[length - 1] == Path.AltDirectorySeparatorChar))
            {
                length--;
            }

            return path.Substring(0, length);
        }

        private static bool ContainsWindowsAliasSegment(string path)
        {
            if (Path.DirectorySeparatorChar != '\\')
            {
                return false;
            }

            string root = Path.GetPathRoot(path);
            string remainder = path.Substring(root.Length);
            string[] segments = remainder.Split(Path.DirectorySeparatorChar);
            foreach (string segment in segments)
            {
                if (segment.Length > 0 &&
                    (segment[segment.Length - 1] == '.' ||
                     segment[segment.Length - 1] == ' '))
                {
                    return true;
                }
            }

            return false;
        }

        private static void TryDeleteTemporaryFile(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch (Exception exception) when (IsExpectedFileException(exception))
            {
                // The target archive was never committed; cleanup is best effort only.
            }
        }

        private static MixWriteResult Failure(
            MixWriteDiagnosticCode code,
            int entryIndex,
            MixFileId? entryId,
            string message)
        {
            return MixWriteResult.Failure(
                new MixWriteDiagnostic(code, entryIndex, entryId, message));
        }

        private static void WriteUInt16(byte[] destination, int offset, ushort value)
        {
            destination[offset] = (byte)value;
            destination[offset + 1] = (byte)(value >> 8);
        }

        private static void WriteUInt32(byte[] destination, int offset, uint value)
        {
            destination[offset] = (byte)value;
            destination[offset + 1] = (byte)(value >> 8);
            destination[offset + 2] = (byte)(value >> 16);
            destination[offset + 3] = (byte)(value >> 24);
        }
    }
}
