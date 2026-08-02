using System;

namespace RA2YR.Core.Formats.Mix.Crypto
{
    internal enum MixCryptoDiagnosticCode
    {
        InvalidBlowfishKeyLength,
        InvalidBlockLength,
        InvalidKeySourceLength,
        KeySourceValueOutOfRange
    }

    internal sealed class MixCryptoDiagnostic
    {
        public MixCryptoDiagnostic(
            MixCryptoDiagnosticCode code,
            string field,
            int blockIndex,
            string message)
        {
            Code = code;
            Field = string.IsNullOrWhiteSpace(field)
                ? throw new ArgumentException("A field name is required.", nameof(field))
                : field;
            BlockIndex = blockIndex;
            Message = message ?? throw new ArgumentNullException(nameof(message));
        }

        public MixCryptoDiagnosticCode Code { get; }

        public string Field { get; }

        public int BlockIndex { get; }

        public string Message { get; }
    }

    internal sealed class MixCryptoException : Exception
    {
        public MixCryptoException(MixCryptoDiagnostic diagnostic)
            : base((diagnostic ?? throw new ArgumentNullException(nameof(diagnostic))).Message)
        {
            Diagnostic = diagnostic;
        }

        public MixCryptoDiagnostic Diagnostic { get; }
    }
}
