using System;
using System.Numerics;

namespace RA2YR.Core.Formats.Mix.Crypto
{
    internal sealed class WestwoodMixKeyDerivationResult
    {
        private readonly byte[] keyMaterial;

        private WestwoodMixKeyDerivationResult(
            byte[] keyMaterial,
            MixCryptoDiagnostic diagnostic)
        {
            this.keyMaterial = keyMaterial;
            Diagnostic = diagnostic;
        }

        public bool IsSuccess => Diagnostic == null;

        public MixCryptoDiagnostic Diagnostic { get; }

        public byte[] GetKeyMaterial()
        {
            return keyMaterial == null ? Array.Empty<byte>() : (byte[])keyMaterial.Clone();
        }

        public static WestwoodMixKeyDerivationResult Success(byte[] keyMaterial)
        {
            return new WestwoodMixKeyDerivationResult(
                (byte[])(keyMaterial ?? throw new ArgumentNullException(nameof(keyMaterial))).Clone(),
                null);
        }

        public static WestwoodMixKeyDerivationResult Failure(MixCryptoDiagnostic diagnostic)
        {
            return new WestwoodMixKeyDerivationResult(
                null,
                diagnostic ?? throw new ArgumentNullException(nameof(diagnostic)));
        }
    }

    internal static class WestwoodMixKeyDeriver
    {
        internal const int KeySourceLength = 80;
        internal const int CiphertextBlockLength = 40;
        internal const int DecodedBlockLength = 39;
        internal const int BlowfishKeyLength = 56;

        private const int PublicExponent = 65537;

        // DER INTEGER value from Westwood's public MIX key envelope.
        private const string ModulusBigEndianHex =
            "51BCDA086D39FCE4565160D651713FA2E8AA54FA6682B04AABDD0E6AF8B0C1E6D1FB4F3DAA437F15";

        private static readonly BigInteger Modulus = CreateModulus();

        public static WestwoodMixKeyDerivationResult Derive(ReadOnlySpan<byte> keySource)
        {
            if (keySource.Length != KeySourceLength)
            {
                return Failure(
                    MixCryptoDiagnosticCode.InvalidKeySourceLength,
                    "key-source",
                    -1,
                    "A Westwood MIX key source must contain exactly 80 bytes.");
            }

            var decoded = new byte[DecodedBlockLength * 2];
            for (int blockIndex = 0; blockIndex < 2; blockIndex++)
            {
                BigInteger ciphertext = CreateUnsignedLittleEndian(
                    keySource.Slice(blockIndex * CiphertextBlockLength, CiphertextBlockLength));
                if (ciphertext >= Modulus)
                {
                    return Failure(
                        MixCryptoDiagnosticCode.KeySourceValueOutOfRange,
                        "key-source-block",
                        blockIndex,
                        "A Westwood MIX key-source block must be less than the fixed modulus.");
                }

                BigInteger plaintext = BigInteger.ModPow(
                    ciphertext,
                    PublicExponent,
                    Modulus);
                byte[] littleEndian = plaintext.ToByteArray();
                int copyLength = Math.Min(DecodedBlockLength, littleEndian.Length);
                Array.Copy(
                    littleEndian,
                    0,
                    decoded,
                    blockIndex * DecodedBlockLength,
                    copyLength);
            }

            var key = new byte[BlowfishKeyLength];
            Array.Copy(decoded, key, key.Length);
            return WestwoodMixKeyDerivationResult.Success(key);
        }

        private static BigInteger CreateModulus()
        {
            int byteCount = ModulusBigEndianHex.Length / 2;
            var littleEndian = new byte[byteCount + 1];
            for (int index = 0; index < byteCount; index++)
            {
                int sourceIndex = (byteCount - index - 1) * 2;
                littleEndian[index] = Convert.ToByte(
                    ModulusBigEndianHex.Substring(sourceIndex, 2),
                    16);
            }

            return new BigInteger(littleEndian);
        }

        private static BigInteger CreateUnsignedLittleEndian(ReadOnlySpan<byte> value)
        {
            var signedLittleEndian = new byte[value.Length + 1];
            value.CopyTo(signedLittleEndian);
            return new BigInteger(signedLittleEndian);
        }

        private static WestwoodMixKeyDerivationResult Failure(
            MixCryptoDiagnosticCode code,
            string field,
            int blockIndex,
            string message)
        {
            return WestwoodMixKeyDerivationResult.Failure(
                new MixCryptoDiagnostic(code, field, blockIndex, message));
        }
    }
}
