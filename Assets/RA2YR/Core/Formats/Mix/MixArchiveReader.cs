using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using RA2YR.Core.Binary;
using RA2YR.Core.Binary.Seekable;
using RA2YR.Core.Formats.Mix.Crypto;

namespace RA2YR.Core.Formats.Mix
{
    internal static class MixArchiveReader
    {
        private const int ClassicHeaderLength = 6;
        private const int ExtendedFlagsLength = 4;
        private const int DirectoryEntryLength = 12;
        private const int ChecksumLength = 20;
        private const int HashBufferSize = 81920;
        private const int HashWorkingAllocationEstimate = 4096;
        private const int RawEntryAllocationEstimate = 96;
        private const int EncryptedWorkingAllocationEstimate = 8192;

        private const uint KnownFlags =
            (uint)(MixArchiveFlags.Checksum | MixArchiveFlags.EncryptedDirectory);

        public static MixArchiveReadResult Read(
            ReadOnlyMemory<byte> input,
            BinarySourceContext source,
            MixReadLimits limits = null)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            MixReadLimits effectiveLimits = limits ?? MixReadLimits.Default;
            if (input.Length > effectiveLimits.MaxArchiveBytes)
            {
                return MixArchiveReadResult.Failure(new MixDiagnostic(
                    MixDiagnosticCode.BinaryReadFailure,
                    source,
                    0,
                    input.Length,
                    input.Length,
                    "mix-input",
                    -1,
                    null,
                    "The MIX input exceeds its explicit archive-size budget.",
                    BinaryDiagnosticCode.InputBudgetExceeded));
            }

            if (input.Length > effectiveLimits.MaxAllocatedBytes)
            {
                return MixArchiveReadResult.Failure(new MixDiagnostic(
                    MixDiagnosticCode.AllocationBudgetExceeded,
                    source,
                    0,
                    input.Length,
                    input.Length,
                    "mix-input-snapshot",
                    -1,
                    null,
                    "The in-memory MIX snapshot exceeds its cumulative allocation budget.",
                    BinaryDiagnosticCode.AllocationBudgetExceeded));
            }

            byte[] snapshot;
            try
            {
                snapshot = input.ToArray();
            }
            catch (OutOfMemoryException)
            {
                return MixArchiveReadResult.Failure(new MixDiagnostic(
                    MixDiagnosticCode.AllocationBudgetExceeded,
                    source,
                    0,
                    input.Length,
                    effectiveLimits.MaxAllocatedBytes,
                    "mix-input-snapshot",
                    -1,
                    null,
                    "The bounded in-memory MIX snapshot could not be allocated.",
                    BinaryDiagnosticCode.AllocationBudgetExceeded));
            }

            var stream = new MemoryStream(snapshot, false);
            return ReadStream(
                stream,
                0,
                input.Length,
                source,
                effectiveLimits,
                false,
                input.Length);
        }

        public static MixArchiveReadResult Read(
            Stream stream,
            long startOffset,
            long length,
            BinarySourceContext source,
            MixReadLimits limits = null,
            bool leaveOpen = false)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            return ReadStream(
                stream,
                startOffset,
                length,
                source,
                limits ?? MixReadLimits.Default,
                leaveOpen,
                0);
        }

        private static MixArchiveReadResult ReadStream(
            Stream stream,
            long startOffset,
            long length,
            BinarySourceContext source,
            MixReadLimits effectiveLimits,
            bool leaveOpen,
            long initialAllocatedBytes)
        {
            ReadOnlyDataWindowSession session = null;
            try
            {
                session = ReadOnlyDataWindowSession.FromSeekableStream(
                    stream,
                    source,
                    startOffset,
                    length,
                    CreateWindowLimits(effectiveLimits),
                    leaveOpen);
                return ReadCore(
                    session.Root,
                    source,
                    effectiveLimits,
                    session,
                    initialAllocatedBytes);
            }
            catch (BinaryReadException exception)
            {
                session?.Dispose();
                return BinaryFailure(exception.Diagnostic);
            }
            catch
            {
                session?.Dispose();
                throw;
            }
        }

        public static MixArchiveReadResult Read(
            ReadOnlyDataWindow window,
            BinarySourceContext source,
            MixReadLimits limits = null)
        {
            if (window == null)
            {
                throw new ArgumentNullException(nameof(window));
            }

            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            return ReadCore(window, source, limits ?? MixReadLimits.Default, null, 0);
        }

        private static MixArchiveReadResult ReadCore(
            ReadOnlyDataWindow root,
            BinarySourceContext source,
            MixReadLimits limits,
            IDisposable ownedInput,
            long initialAllocatedBytes)
        {
            try
            {
                var allocations = new MixAllocationBudget(
                    limits.MaxAllocatedBytes,
                    initialAllocatedBytes);
                MixArchive archive = Parse(
                    root,
                    source,
                    limits,
                    allocations,
                    ownedInput);
                return MixArchiveReadResult.Success(archive);
            }
            catch (MixReadException exception)
            {
                ownedInput?.Dispose();
                return MixArchiveReadResult.Failure(exception.Diagnostic);
            }
            catch (BinaryReadException exception)
            {
                ownedInput?.Dispose();
                return BinaryFailure(exception.Diagnostic);
            }
            catch (MixCryptoException exception)
            {
                ownedInput?.Dispose();
                return MixArchiveReadResult.Failure(new MixDiagnostic(
                    MixDiagnosticCode.DecryptionFailed,
                    source,
                    root.AbsoluteStartOffset,
                    0,
                    root.Length,
                    "mix-encrypted-directory",
                    exception.Diagnostic.BlockIndex,
                    null,
                    "The encrypted MIX directory could not be decrypted."));
            }
            catch (OverflowException)
            {
                ownedInput?.Dispose();
                return MixArchiveReadResult.Failure(new MixDiagnostic(
                    MixDiagnosticCode.ArithmeticOverflow,
                    source,
                    root.AbsoluteStartOffset,
                    root.Length,
                    root.Length,
                    "mix-layout",
                    -1,
                    null,
                    "The MIX layout overflowed checked arithmetic."));
            }
            catch (OutOfMemoryException)
            {
                ownedInput?.Dispose();
                return MixArchiveReadResult.Failure(new MixDiagnostic(
                    MixDiagnosticCode.AllocationBudgetExceeded,
                    source,
                    root.AbsoluteStartOffset,
                    0,
                    limits.MaxAllocatedBytes,
                    "mix-allocation",
                    -1,
                    null,
                    "A validated bounded MIX parser allocation could not be completed.",
                    BinaryDiagnosticCode.AllocationBudgetExceeded));
            }
        }

        private static MixArchive Parse(
            ReadOnlyDataWindow root,
            BinarySourceContext source,
            MixReadLimits limits,
            MixAllocationBudget allocations,
            IDisposable ownedInput)
        {
            if (root.Length > limits.MaxArchiveBytes)
            {
                Fail(
                    MixDiagnosticCode.BinaryReadFailure,
                    source,
                    root.AbsoluteStartOffset,
                    root.Length,
                    root.Length,
                    "mix-input",
                    "The MIX input exceeds its explicit archive-size budget.",
                    binaryCode: BinaryDiagnosticCode.InputBudgetExceeded);
            }

            EnsureAvailable(
                root,
                0,
                ExtendedFlagsLength,
                MixDiagnosticCode.TruncatedHeader,
                source,
                "mix-header",
                "The MIX input is shorter than its header discriminator.");
            byte[] discriminator = ReadBytes(
                root,
                0,
                ExtendedFlagsLength,
                "mix-header",
                source,
                allocations);
            uint firstWord = ReadUInt32(discriminator, 0);
            // XCC writes the extended RA2 header even when no flag bits are set.
            // Six zero bytes remain the canonical classic empty archive; ten or
            // more bytes provide the otherwise ambiguous extended header body.
            bool isExtended = (firstWord & 0xffffu) == 0 &&
                              (firstWord != 0 || root.Length >= 10);

            MixArchiveHeaderKind headerKind = isExtended
                ? MixArchiveHeaderKind.Extended
                : MixArchiveHeaderKind.Classic;
            MixArchiveFlags flags = isExtended
                ? (MixArchiveFlags)firstWord
                : MixArchiveFlags.None;
            if (isExtended && (firstWord & ~KnownFlags) != 0)
            {
                Fail(
                    MixDiagnosticCode.UnsupportedFlags,
                    source,
                    root.AbsoluteStartOffset,
                    ExtendedFlagsLength,
                    root.Length,
                    "mix-flags",
                    "The extended MIX header contains unsupported flag bits.");
            }

            ParsedDirectory directory = (flags & MixArchiveFlags.EncryptedDirectory) != 0
                ? ReadEncryptedDirectory(root, source, limits, allocations)
                : ReadPlainDirectory(root, source, headerKind, limits, allocations);

            long payloadEnd = CheckedAdd(
                directory.PayloadRelativeOffset,
                directory.DataSize,
                source,
                root,
                "mix-data-region");
            if (payloadEnd > root.Length)
            {
                Fail(
                    MixDiagnosticCode.TruncatedDataRegion,
                    source,
                    root.AbsoluteStartOffset + directory.PayloadRelativeOffset,
                    directory.DataSize,
                    Math.Max(0, root.Length - directory.PayloadRelativeOffset),
                    "mix-data-region",
                    "The MIX payload region is shorter than its declared data size.");
            }

            bool hasChecksum = (flags & MixArchiveFlags.Checksum) != 0;
            long expectedEnd = payloadEnd;
            if (hasChecksum)
            {
                expectedEnd = CheckedAdd(
                    payloadEnd,
                    ChecksumLength,
                    source,
                    root,
                    "mix-checksum");
                if (expectedEnd > root.Length)
                {
                    Fail(
                        MixDiagnosticCode.TruncatedChecksum,
                        source,
                        root.AbsoluteStartOffset + payloadEnd,
                        ChecksumLength,
                        Math.Max(0, root.Length - payloadEnd),
                        "mix-checksum",
                        "The checksummed MIX input is missing part of its SHA-1 trailer.");
                }
            }

            if (root.Length > expectedEnd)
            {
                Fail(
                    MixDiagnosticCode.UnexpectedTrailingData,
                    source,
                    root.AbsoluteStartOffset + expectedEnd,
                    root.Length - expectedEnd,
                    root.Length - expectedEnd,
                    "mix-trailing-data",
                    "The MIX input contains bytes after its declared archive end.");
            }

            ValidateRawEntries(directory, source, root);
            ReadOnlyDataWindow payloadWindow = root.CreateChild(
                directory.PayloadRelativeOffset,
                directory.DataSize,
                "mix-data-region");
            bool checksumVerified = false;
            if (hasChecksum)
            {
                byte[] expectedChecksum = ReadBytes(
                    root,
                    payloadEnd,
                    ChecksumLength,
                    "mix-checksum",
                    source,
                    allocations);
                allocations.Reserve(
                    ChecksumLength + HashWorkingAllocationEstimate,
                    source,
                    root,
                    "mix-checksum-digest");
                byte[] actualChecksum = ComputeSha1(payloadWindow);
                if (!AreEqual(expectedChecksum, actualChecksum))
                {
                    Fail(
                        MixDiagnosticCode.ChecksumMismatch,
                        source,
                        root.AbsoluteStartOffset + payloadEnd,
                        ChecksumLength,
                        ChecksumLength,
                        "mix-checksum",
                        "The MIX payload SHA-1 does not match its checksum trailer.");
                }

                checksumVerified = true;
            }

            var entries = new List<MixArchiveEntry>(directory.Entries.Count);
            foreach (RawEntry rawEntry in directory.Entries)
            {
                ReadOnlyDataWindow entryWindow = payloadWindow.CreateChild(
                    rawEntry.RelativeOffset,
                    rawEntry.Length,
                    "mix-entry-payload");
                entries.Add(new MixArchiveEntry(
                    rawEntry.Index,
                    rawEntry.Id,
                    rawEntry.RelativeOffset,
                    rawEntry.Length,
                    entryWindow));
            }

            return new MixArchive(
                source,
                headerKind,
                flags,
                directory.DataSize,
                directory.PayloadRelativeOffset,
                payloadWindow,
                entries,
                directory.KeySource,
                checksumVerified,
                ownedInput);
        }

        private static ParsedDirectory ReadPlainDirectory(
            ReadOnlyDataWindow root,
            BinarySourceContext source,
            MixArchiveHeaderKind headerKind,
            MixReadLimits limits,
            MixAllocationBudget allocations)
        {
            long headerOffset = headerKind == MixArchiveHeaderKind.Classic
                ? 0
                : ExtendedFlagsLength;
            EnsureAvailable(
                root,
                headerOffset,
                ClassicHeaderLength,
                MixDiagnosticCode.TruncatedHeader,
                source,
                "mix-header",
                "The MIX count and data-size header is truncated.");
            byte[] header = ReadBytes(
                root,
                headerOffset,
                ClassicHeaderLength,
                "mix-header",
                source,
                allocations);
            ushort count = ReadUInt16(header, 0);
            uint dataSize = ReadUInt32(header, 2);
            long directorySize = ValidateDirectorySize(count, limits, source, root);
            long entryBytes = directorySize - ClassicHeaderLength;
            ReserveEntryModels(count, allocations, source, root);
            long directoryOffset = CheckedAdd(
                headerOffset,
                ClassicHeaderLength,
                source,
                root,
                "mix-directory");
            EnsureAvailable(
                root,
                directoryOffset,
                entryBytes,
                MixDiagnosticCode.TruncatedDirectory,
                source,
                "mix-directory",
                "The MIX directory is shorter than its declared entry count.");
            byte[] entryData = ReadBytes(
                root,
                directoryOffset,
                entryBytes,
                "mix-directory",
                source,
                allocations);
            long payloadOffset = CheckedAdd(
                directoryOffset,
                entryBytes,
                source,
                root,
                "mix-data-region");
            return new ParsedDirectory(
                dataSize,
                payloadOffset,
                ParseEntries(entryData, 0, count, directoryOffset, false),
                null,
                directoryOffset);
        }

        private static ParsedDirectory ReadEncryptedDirectory(
            ReadOnlyDataWindow root,
            BinarySourceContext source,
            MixReadLimits limits,
            MixAllocationBudget allocations)
        {
            long keySourceOffset = ExtendedFlagsLength;
            long encryptedDirectoryOffset = CheckedAdd(
                keySourceOffset,
                WestwoodMixKeyDeriver.KeySourceLength,
                source,
                root,
                "mix-encrypted-directory");
            EnsureAvailable(
                root,
                keySourceOffset,
                WestwoodMixKeyDeriver.KeySourceLength + BlowfishCipher.BlockSize,
                MixDiagnosticCode.TruncatedHeader,
                source,
                "mix-encrypted-directory",
                "The encrypted MIX key source or first directory block is truncated.");
            byte[] keySource = ReadBytes(
                root,
                keySourceOffset,
                WestwoodMixKeyDeriver.KeySourceLength,
                "mix-key-source",
                source,
                allocations);
            allocations.Reserve(
                EncryptedWorkingAllocationEstimate,
                source,
                root,
                "mix-crypto-state");
            WestwoodMixKeyDerivationResult keyResult =
                WestwoodMixKeyDeriver.Derive(keySource);
            if (!keyResult.IsSuccess)
            {
                Fail(
                    MixDiagnosticCode.DecryptionFailed,
                    source,
                    root.AbsoluteStartOffset + keySourceOffset,
                    WestwoodMixKeyDeriver.KeySourceLength,
                    Math.Max(0, root.Length - keySourceOffset),
                    "mix-key-source",
                    "The encrypted MIX key source is invalid.");
            }

            var cipher = new BlowfishCipher(keyResult.GetKeyMaterial());
            byte[] firstBlock = ReadBytes(
                root,
                encryptedDirectoryOffset,
                BlowfishCipher.BlockSize,
                "mix-encrypted-directory",
                source,
                allocations);
            allocations.Reserve(
                BlowfishCipher.BlockSize,
                source,
                root,
                "mix-decrypted-header");
            var firstPlainBlock = new byte[BlowfishCipher.BlockSize];
            cipher.DecryptWestwoodLittleEndianWordBlock(firstBlock, firstPlainBlock);
            ushort count = ReadUInt16(firstPlainBlock, 0);
            uint dataSize = ReadUInt32(firstPlainBlock, 2);
            long plainDirectorySize = ValidateDirectorySize(count, limits, source, root);
            ReserveEntryModels(count, allocations, source, root);
            long encryptedDirectorySize = RoundUpToBlock(
                plainDirectorySize,
                BlowfishCipher.BlockSize,
                source,
                root);
            EnsureAvailable(
                root,
                encryptedDirectoryOffset,
                encryptedDirectorySize,
                MixDiagnosticCode.TruncatedDirectory,
                source,
                "mix-encrypted-directory",
                "The encrypted MIX directory is shorter than its declared entry count.");
            byte[] encrypted = ReadBytes(
                root,
                encryptedDirectoryOffset,
                encryptedDirectorySize,
                "mix-encrypted-directory",
                source,
                allocations);
            for (int offset = 0; offset < encrypted.Length; offset += BlowfishCipher.BlockSize)
            {
                cipher.DecryptWestwoodLittleEndianWordBlock(
                    encrypted.AsSpan(offset, BlowfishCipher.BlockSize),
                    encrypted.AsSpan(offset, BlowfishCipher.BlockSize));
            }

            long payloadOffset = CheckedAdd(
                encryptedDirectoryOffset,
                encryptedDirectorySize,
                source,
                root,
                "mix-data-region");
            return new ParsedDirectory(
                dataSize,
                payloadOffset,
                ParseEntries(
                    encrypted,
                    ClassicHeaderLength,
                    count,
                    encryptedDirectoryOffset,
                    true),
                keySource,
                encryptedDirectoryOffset);
        }

        private static long ValidateDirectorySize(
            ushort count,
            MixReadLimits limits,
            BinarySourceContext source,
            ReadOnlyDataWindow root)
        {
            if (count > limits.MaxEntries)
            {
                Fail(
                    MixDiagnosticCode.EntryCountBudgetExceeded,
                    source,
                    root.AbsoluteStartOffset,
                    count,
                    root.Length,
                    "mix-file-count",
                    "The MIX entry count exceeds its explicit record budget.");
            }

            long entryBytes;
            long directoryBytes;
            try
            {
                entryBytes = checked((long)count * DirectoryEntryLength);
                directoryBytes = checked(ClassicHeaderLength + entryBytes);
            }
            catch (OverflowException)
            {
                Fail(
                    MixDiagnosticCode.DirectorySizeOverflow,
                    source,
                    root.AbsoluteStartOffset,
                    count,
                    root.Length,
                    "mix-directory",
                    "The MIX directory size overflows checked arithmetic.");
                return 0;
            }

            if (directoryBytes > limits.MaxDirectoryBytes)
            {
                Fail(
                    MixDiagnosticCode.DirectoryBudgetExceeded,
                    source,
                    root.AbsoluteStartOffset,
                    directoryBytes,
                    root.Length,
                    "mix-directory",
                    "The MIX directory exceeds its explicit byte budget.");
            }

            return directoryBytes;
        }

        private static List<RawEntry> ParseEntries(
            byte[] entryData,
            int dataOffset,
            ushort count,
            long directoryRelativeOffset,
            bool encrypted)
        {
            var entries = new List<RawEntry>(count);
            for (int index = 0; index < count; index++)
            {
                int relativeEntryOffset = checked(index * DirectoryEntryLength);
                int offset = checked(dataOffset + relativeEntryOffset);
                long physicalDiagnosticOffset = encrypted
                    ? checked(
                        directoryRelativeOffset +
                        (offset / BlowfishCipher.BlockSize) * BlowfishCipher.BlockSize)
                    : checked(directoryRelativeOffset + relativeEntryOffset);
                entries.Add(new RawEntry(
                    index,
                    MixFileId.FromRaw(ReadUInt32(entryData, offset)),
                    ReadUInt32(entryData, offset + 4),
                    ReadUInt32(entryData, offset + 8),
                    physicalDiagnosticOffset));
            }

            return entries;
        }

        private static void ValidateRawEntries(
            ParsedDirectory directory,
            BinarySourceContext source,
            ReadOnlyDataWindow root)
        {
            var ids = new HashSet<uint>();
            foreach (RawEntry entry in directory.Entries)
            {
                if (!ids.Add(entry.Id.Value))
                {
                    FailEntry(
                        MixDiagnosticCode.DuplicateEntryId,
                        source,
                        root,
                        entry,
                        "mix-entry-id",
                        "The MIX directory contains a duplicate numeric file ID.");
                }

                ulong end = (ulong)entry.RelativeOffset + entry.Length;
                if (end > uint.MaxValue)
                {
                    FailEntry(
                        MixDiagnosticCode.EntryRangeOverflow,
                        source,
                        root,
                        entry,
                        "mix-entry-range",
                        "The MIX entry offset plus length exceeds the 32-bit format range.");
                }

                if (end > directory.DataSize)
                {
                    FailEntry(
                        MixDiagnosticCode.EntryOutsideDataRegion,
                        source,
                        root,
                        entry,
                        "mix-entry-range",
                        "The MIX entry crosses the declared payload data region.");
                }
            }

            var ordered = new List<RawEntry>(directory.Entries);
            ordered.Sort((left, right) =>
            {
                int offset = left.RelativeOffset.CompareTo(right.RelativeOffset);
                return offset != 0 ? offset : left.Index.CompareTo(right.Index);
            });
            ulong occupiedEnd = 0;
            bool hasOccupiedRange = false;
            foreach (RawEntry entry in ordered)
            {
                if (entry.Length == 0)
                {
                    continue;
                }

                if (hasOccupiedRange && entry.RelativeOffset < occupiedEnd)
                {
                    FailEntry(
                        MixDiagnosticCode.OverlappingEntries,
                        source,
                        root,
                        entry,
                        "mix-entry-range",
                        "Two non-empty MIX entries overlap in the payload region.");
                }

                occupiedEnd = (ulong)entry.RelativeOffset + entry.Length;
                hasOccupiedRange = true;
            }
        }

        private static byte[] ComputeSha1(ReadOnlyDataWindow payload)
        {
            using (SHA1 sha1 = SHA1.Create())
            using (var hashingStream = new CryptoStream(
                       Stream.Null,
                       sha1,
                       CryptoStreamMode.Write))
            {
                payload.CopyTo(hashingStream, "mix-checksum-payload", HashBufferSize);
                hashingStream.FlushFinalBlock();
                return (byte[])sha1.Hash.Clone();
            }
        }

        private static bool AreEqual(byte[] left, byte[] right)
        {
            if (left.Length != right.Length)
            {
                return false;
            }

            int difference = 0;
            for (int index = 0; index < left.Length; index++)
            {
                difference |= left[index] ^ right[index];
            }

            return difference == 0;
        }

        private static byte[] ReadBytes(
            ReadOnlyDataWindow window,
            long relativeOffset,
            long count,
            string field,
            BinarySourceContext source,
            MixAllocationBudget allocations)
        {
            int length;
            try
            {
                length = checked((int)count);
            }
            catch (OverflowException)
            {
                throw new OverflowException("A bounded MIX snapshot length exceeds Int32.");
            }

            allocations.Reserve(count, source, window, field);
            var bytes = new byte[length];
            window.ReadExactly(relativeOffset, bytes, 0, bytes.Length, field);
            return bytes;
        }

        private static ushort ReadUInt16(byte[] bytes, int offset)
        {
            return (ushort)(bytes[offset] | (bytes[offset + 1] << 8));
        }

        private static uint ReadUInt32(byte[] bytes, int offset)
        {
            return bytes[offset] |
                ((uint)bytes[offset + 1] << 8) |
                ((uint)bytes[offset + 2] << 16) |
                ((uint)bytes[offset + 3] << 24);
        }

        private static long RoundUpToBlock(
            long value,
            int blockSize,
            BinarySourceContext source,
            ReadOnlyDataWindow root)
        {
            try
            {
                return checked((value + blockSize - 1) / blockSize * blockSize);
            }
            catch (OverflowException)
            {
                Fail(
                    MixDiagnosticCode.DirectorySizeOverflow,
                    source,
                    root.AbsoluteStartOffset,
                    value,
                    root.Length,
                    "mix-encrypted-directory",
                    "The padded encrypted directory size overflows checked arithmetic.");
                return 0;
            }
        }

        private static long CheckedAdd(
            long left,
            long right,
            BinarySourceContext source,
            ReadOnlyDataWindow root,
            string field)
        {
            try
            {
                return checked(left + right);
            }
            catch (OverflowException)
            {
                Fail(
                    MixDiagnosticCode.ArithmeticOverflow,
                    source,
                    root.AbsoluteStartOffset,
                    right,
                    root.Length,
                    field,
                    "The MIX byte range overflows checked arithmetic.");
                return 0;
            }
        }

        private static void EnsureAvailable(
            ReadOnlyDataWindow root,
            long relativeOffset,
            long requestedLength,
            MixDiagnosticCode code,
            BinarySourceContext source,
            string field,
            string message)
        {
            long end;
            try
            {
                end = checked(relativeOffset + requestedLength);
            }
            catch (OverflowException)
            {
                Fail(
                    MixDiagnosticCode.ArithmeticOverflow,
                    source,
                    root.AbsoluteStartOffset,
                    requestedLength,
                    root.Length,
                    field,
                    "The required MIX range overflows checked arithmetic.");
                return;
            }

            if (relativeOffset < 0 || requestedLength < 0 || end > root.Length)
            {
                Fail(
                    code,
                    source,
                    root.AbsoluteStartOffset + Math.Max(0, relativeOffset),
                    requestedLength,
                    Math.Max(0, root.Length - Math.Max(0, relativeOffset)),
                    field,
                    message);
            }
        }

        private static void FailEntry(
            MixDiagnosticCode code,
            BinarySourceContext source,
            ReadOnlyDataWindow root,
            RawEntry entry,
            string field,
            string message)
        {
            Fail(
                code,
                source,
                root.AbsoluteStartOffset + entry.DirectoryRelativeOffset,
                DirectoryEntryLength,
                Math.Max(0, root.Length - entry.DirectoryRelativeOffset),
                field,
                message,
                entry.Index,
                entry.Id);
        }

        private static void Fail(
            MixDiagnosticCode code,
            BinarySourceContext source,
            long absoluteOffset,
            long requestedLength,
            long remainingLength,
            string field,
            string message,
            int entryIndex = -1,
            MixFileId? entryId = null,
            BinaryDiagnosticCode? binaryCode = null)
        {
            throw new MixReadException(new MixDiagnostic(
                code,
                source,
                absoluteOffset,
                requestedLength,
                remainingLength,
                field,
                entryIndex,
                entryId,
                message,
                binaryCode));
        }

        private static MixArchiveReadResult BinaryFailure(BinaryDiagnostic diagnostic)
        {
            return MixArchiveReadResult.Failure(new MixDiagnostic(
                MixDiagnosticCode.BinaryReadFailure,
                diagnostic.Source,
                diagnostic.AbsoluteOffset,
                diagnostic.RequestedLength,
                diagnostic.RemainingLength,
                diagnostic.FieldOrSection,
                -1,
                null,
                diagnostic.Message,
                diagnostic.Code));
        }

        private static void ReserveEntryModels(
            ushort count,
            MixAllocationBudget allocations,
            BinarySourceContext source,
            ReadOnlyDataWindow root)
        {
            long bytes;
            try
            {
                bytes = checked((long)count * RawEntryAllocationEstimate);
            }
            catch (OverflowException)
            {
                Fail(
                    MixDiagnosticCode.ArithmeticOverflow,
                    source,
                    root.AbsoluteStartOffset,
                    count,
                    root.Length,
                    "mix-entry-models",
                    "The MIX entry model allocation accounting overflowed.");
                return;
            }

            allocations.Reserve(bytes, source, root, "mix-entry-models");
        }

        private static ReadOnlyDataWindowLimits CreateWindowLimits(MixReadLimits limits)
        {
            long maximumSingleRead = Math.Max(
                HashBufferSize,
                limits.MaxDirectoryBytes);
            return new ReadOnlyDataWindowLimits(
                limits.MaxArchiveBytes,
                maximumSingleRead,
                limits.MaxTotalReadBytes,
                limits.MaxWindows,
                limits.MaxWindowDepth);
        }

        private sealed class ParsedDirectory
        {
            public ParsedDirectory(
                uint dataSize,
                long payloadRelativeOffset,
                List<RawEntry> entries,
                byte[] keySource,
                long directoryRelativeOffset)
            {
                DataSize = dataSize;
                PayloadRelativeOffset = payloadRelativeOffset;
                Entries = entries;
                KeySource = keySource;
                DirectoryRelativeOffset = directoryRelativeOffset;
            }

            public uint DataSize { get; }

            public long PayloadRelativeOffset { get; }

            public List<RawEntry> Entries { get; }

            public byte[] KeySource { get; }

            public long DirectoryRelativeOffset { get; }
        }

        private sealed class MixAllocationBudget
        {
            private readonly long maximum;
            private long allocated;

            public MixAllocationBudget(long maximum, long initialAllocation)
            {
                if (maximum < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(maximum));
                }

                if (initialAllocation < 0 || initialAllocation > maximum)
                {
                    throw new ArgumentOutOfRangeException(nameof(initialAllocation));
                }

                this.maximum = maximum;
                allocated = initialAllocation;
            }

            public void Reserve(
                long count,
                BinarySourceContext source,
                ReadOnlyDataWindow window,
                string field)
            {
                if (count < 0)
                {
                    Fail(
                        MixDiagnosticCode.ArithmeticOverflow,
                        source,
                        window.AbsoluteStartOffset,
                        count,
                        window.Length,
                        field,
                        "A MIX allocation reservation cannot be negative.");
                }

                long updated;
                try
                {
                    updated = checked(allocated + count);
                }
                catch (OverflowException)
                {
                    Fail(
                        MixDiagnosticCode.ArithmeticOverflow,
                        source,
                        window.AbsoluteStartOffset,
                        count,
                        window.Length,
                        field,
                        "Cumulative MIX allocation accounting overflowed.");
                    return;
                }

                if (updated > maximum)
                {
                    Fail(
                        MixDiagnosticCode.AllocationBudgetExceeded,
                        source,
                        window.AbsoluteStartOffset,
                        count,
                        Math.Max(0, maximum - allocated),
                        field,
                        "The cumulative MIX allocation budget would be exceeded.",
                        binaryCode: BinaryDiagnosticCode.AllocationBudgetExceeded);
                }

                allocated = updated;
            }
        }

        private sealed class RawEntry
        {
            public RawEntry(
                int index,
                MixFileId id,
                uint relativeOffset,
                uint length,
                long directoryRelativeOffset)
            {
                Index = index;
                Id = id;
                RelativeOffset = relativeOffset;
                Length = length;
                DirectoryRelativeOffset = directoryRelativeOffset;
            }

            public int Index { get; }

            public MixFileId Id { get; }

            public uint RelativeOffset { get; }

            public uint Length { get; }

            public long DirectoryRelativeOffset { get; }
        }
    }
}
