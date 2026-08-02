using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;

namespace RA2YR.Core.Formats.Mix.Writing
{
    internal enum MixWriteDiagnosticCode
    {
        InvalidOptionCombination,
        InvalidEntry,
        EntryBudgetExceeded,
        ArchiveSizeBudgetExceeded,
        DuplicateEntryId,
        ArithmeticOverflow,
        EncryptionKeyRejected,
        OutputPurposeInvalid,
        OutputPathInvalid,
        OutputDirectoryMissing,
        OutputReparsePointRejected,
        OutputAlreadyExists,
        TemporaryWriteFailed,
        WrittenFileVerificationFailed,
        AtomicCommitFailed
    }

    internal sealed class MixWriteDiagnostic
    {
        public MixWriteDiagnostic(
            MixWriteDiagnosticCode code,
            int entryIndex,
            MixFileId? entryId,
            string message)
        {
            Code = code;
            EntryIndex = entryIndex;
            EntryId = entryId;
            Message = message ?? throw new ArgumentNullException(nameof(message));
        }

        public MixWriteDiagnosticCode Code { get; }

        public int EntryIndex { get; }

        public MixFileId? EntryId { get; }

        public string Message { get; }
    }

    internal sealed class MixWriteResult
    {
        private readonly byte[] archiveBytes;

        private MixWriteResult(
            byte[] archiveBytes,
            string sha256,
            bool committedToFile,
            bool writtenFileVerified,
            IReadOnlyList<MixWriteDiagnostic> diagnostics)
        {
            this.archiveBytes = archiveBytes;
            Sha256 = sha256;
            CommittedToFile = committedToFile;
            WrittenFileVerified = writtenFileVerified;
            Diagnostics = diagnostics ?? throw new ArgumentNullException(nameof(diagnostics));
        }

        public bool IsSuccess => archiveBytes != null && Diagnostics.Count == 0;

        public long ArchiveSize => archiveBytes == null ? 0 : archiveBytes.LongLength;

        public string Sha256 { get; }

        public bool CommittedToFile { get; }

        public bool WrittenFileVerified { get; }

        public IReadOnlyList<MixWriteDiagnostic> Diagnostics { get; }

        public byte[] GetArchiveBytes()
        {
            return archiveBytes == null ? Array.Empty<byte>() : (byte[])archiveBytes.Clone();
        }

        internal static MixWriteResult Success(
            byte[] archiveBytes,
            string sha256,
            bool committedToFile,
            bool writtenFileVerified)
        {
            return new MixWriteResult(
                archiveBytes ?? throw new ArgumentNullException(nameof(archiveBytes)),
                sha256 ?? throw new ArgumentNullException(nameof(sha256)),
                committedToFile,
                writtenFileVerified,
                Array.AsReadOnly(Array.Empty<MixWriteDiagnostic>()));
        }

        internal void WriteArchiveTo(Stream destination)
        {
            if (destination == null)
            {
                throw new ArgumentNullException(nameof(destination));
            }

            if (!IsSuccess)
            {
                throw new InvalidOperationException("A failed MIX result has no archive bytes.");
            }

            destination.Write(archiveBytes, 0, archiveBytes.Length);
        }

        internal MixWriteResult AsCommittedFile()
        {
            if (!IsSuccess)
            {
                throw new InvalidOperationException("A failed MIX result cannot be committed.");
            }

            return new MixWriteResult(
                archiveBytes,
                Sha256,
                true,
                true,
                Diagnostics);
        }

        internal static MixWriteResult Failure(MixWriteDiagnostic diagnostic)
        {
            if (diagnostic == null)
            {
                throw new ArgumentNullException(nameof(diagnostic));
            }

            return new MixWriteResult(
                null,
                null,
                false,
                false,
                new ReadOnlyCollection<MixWriteDiagnostic>(
                    new List<MixWriteDiagnostic> { diagnostic }));
        }
    }
}
