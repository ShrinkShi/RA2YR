using System;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace RA2YR.Core.Formats.Ini
{
    internal enum IniIdentityWriteDiagnosticCode
    {
        OutputBudgetExceeded,
        AllocationFailed,
        IntegrityVerificationFailed
    }

    internal sealed class IniIdentityWriteDiagnostic
    {
        internal IniIdentityWriteDiagnostic(
            IniIdentityWriteDiagnosticCode code,
            string message)
        {
            if (!Enum.IsDefined(typeof(IniIdentityWriteDiagnosticCode), code))
            {
                throw new ArgumentOutOfRangeException(nameof(code));
            }

            Code = code;
            Message = message ?? throw new ArgumentNullException(nameof(message));
        }

        public IniIdentityWriteDiagnosticCode Code { get; }

        public string Message { get; }
    }

    internal sealed class IniIdentityWriteResult
    {
        private readonly byte[] bytes;
        private readonly IReadOnlyList<IniIdentityWriteDiagnostic> diagnostics;

        private IniIdentityWriteResult(
            byte[] ownedBytes,
            string sha256,
            IniIdentityWriteDiagnostic diagnostic)
        {
            bool success = ownedBytes != null;
            if (success == (diagnostic != null) || success == (sha256 == null))
            {
                throw new ArgumentException(
                    "An identity write result must be either complete or failed.");
            }

            bytes = ownedBytes;
            Sha256 = sha256;
            diagnostics = diagnostic == null
                ? Array.AsReadOnly(Array.Empty<IniIdentityWriteDiagnostic>())
                : Array.AsReadOnly(new[] { diagnostic });
        }

        public bool IsSuccess => bytes != null && diagnostics.Count == 0;

        public long Length => bytes == null ? 0 : bytes.LongLength;

        public string Sha256 { get; }

        public IReadOnlyList<IniIdentityWriteDiagnostic> Diagnostics => diagnostics;

        public byte[] GetBytes()
        {
            if (!IsSuccess)
            {
                throw new InvalidOperationException(
                    "A failed INI identity write has no output bytes.");
            }

            return (byte[])bytes.Clone();
        }

        internal static IniIdentityWriteResult Success(byte[] ownedBytes, string sha256)
        {
            return new IniIdentityWriteResult(
                ownedBytes ?? throw new ArgumentNullException(nameof(ownedBytes)),
                sha256 ?? throw new ArgumentNullException(nameof(sha256)),
                null);
        }

        internal static IniIdentityWriteResult Failure(
            IniIdentityWriteDiagnosticCode code,
            string message)
        {
            return new IniIdentityWriteResult(
                null,
                null,
                new IniIdentityWriteDiagnostic(code, message));
        }
    }

    internal static class IniIdentityWriter
    {
        public static IniIdentityWriteResult WriteToBytes(
            IniRawDocument document,
            long maxOutputBytes)
        {
            if (document == null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            if (maxOutputBytes < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxOutputBytes));
            }

            if (document.OriginalLength > maxOutputBytes)
            {
                return IniIdentityWriteResult.Failure(
                    IniIdentityWriteDiagnosticCode.OutputBudgetExceeded,
                    "The immutable INI document exceeds the identity output budget.");
            }

            byte[] output;
            try
            {
                output = document.CopyOriginalBytes();
            }
            catch (OutOfMemoryException)
            {
                return IniIdentityWriteResult.Failure(
                    IniIdentityWriteDiagnosticCode.AllocationFailed,
                    "The validated INI identity output could not be allocated.");
            }

            if (output.LongLength != document.OriginalLength ||
                !output.AsSpan().SequenceEqual(document.OriginalSpan))
            {
                return IniIdentityWriteResult.Failure(
                    IniIdentityWriteDiagnosticCode.IntegrityVerificationFailed,
                    "The INI identity writer could not prove complete byte preservation.");
            }

            string sha256;
            using (SHA256 algorithm = SHA256.Create())
            {
                sha256 = BitConverter.ToString(algorithm.ComputeHash(output))
                    .Replace("-", string.Empty)
                    .ToLowerInvariant();
            }

            return IniIdentityWriteResult.Success(output, sha256);
        }
    }
}
