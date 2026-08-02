using System;

namespace RA2YR.Core.Formats.Mix.Crypto
{
    internal sealed class BlowfishCipher
    {
        internal const int BlockSize = 8;
        internal const int MinimumKeySize = 4;
        internal const int MaximumKeySize = 56;

        private readonly uint[] p;
        private readonly uint[] s;

        public BlowfishCipher(ReadOnlySpan<byte> key)
        {
            if (key.Length < MinimumKeySize || key.Length > MaximumKeySize)
            {
                throw Error(
                    MixCryptoDiagnosticCode.InvalidBlowfishKeyLength,
                    "key",
                    -1,
                    "A Blowfish key must contain between 4 and 56 bytes.");
            }

            p = BlowfishInitialState.CreatePArray();
            s = BlowfishInitialState.CreateSBoxes();
            ExpandKey(key);
        }

        public void EncryptBigEndianBlock(
            ReadOnlySpan<byte> input,
            Span<byte> output)
        {
            ValidateBlock(input, output);
            uint left = ReadBigEndian(input, 0);
            uint right = ReadBigEndian(input, 4);
            EncryptWords(ref left, ref right);
            WriteBigEndian(output, 0, left);
            WriteBigEndian(output, 4, right);
        }

        public void DecryptBigEndianBlock(
            ReadOnlySpan<byte> input,
            Span<byte> output)
        {
            ValidateBlock(input, output);
            uint left = ReadBigEndian(input, 0);
            uint right = ReadBigEndian(input, 4);
            DecryptWords(ref left, ref right);
            WriteBigEndian(output, 0, left);
            WriteBigEndian(output, 4, right);
        }

        // Westwood's x86 implementation loaded little-endian host words, then
        // byte-swapped them before the standard Blowfish word operation.
        public void EncryptWestwoodLittleEndianWordBlock(
            ReadOnlySpan<byte> input,
            Span<byte> output)
        {
            ValidateBlock(input, output);
            uint left = ReverseBytes(ReadLittleEndian(input, 0));
            uint right = ReverseBytes(ReadLittleEndian(input, 4));
            EncryptWords(ref left, ref right);
            WriteLittleEndian(output, 0, ReverseBytes(left));
            WriteLittleEndian(output, 4, ReverseBytes(right));
        }

        public void DecryptWestwoodLittleEndianWordBlock(
            ReadOnlySpan<byte> input,
            Span<byte> output)
        {
            ValidateBlock(input, output);
            uint left = ReverseBytes(ReadLittleEndian(input, 0));
            uint right = ReverseBytes(ReadLittleEndian(input, 4));
            DecryptWords(ref left, ref right);
            WriteLittleEndian(output, 0, ReverseBytes(left));
            WriteLittleEndian(output, 4, ReverseBytes(right));
        }

        private void ExpandKey(ReadOnlySpan<byte> key)
        {
            int keyIndex = 0;
            for (int index = 0; index < p.Length; index++)
            {
                uint word = 0;
                for (int byteIndex = 0; byteIndex < 4; byteIndex++)
                {
                    word = (word << 8) | key[keyIndex];
                    keyIndex = (keyIndex + 1) % key.Length;
                }

                p[index] ^= word;
            }

            uint left = 0;
            uint right = 0;
            for (int index = 0; index < p.Length; index += 2)
            {
                EncryptWords(ref left, ref right);
                p[index] = left;
                p[index + 1] = right;
            }

            for (int index = 0; index < s.Length; index += 2)
            {
                EncryptWords(ref left, ref right);
                s[index] = left;
                s[index + 1] = right;
            }
        }

        private void EncryptWords(ref uint left, ref uint right)
        {
            unchecked
            {
                for (int round = 0; round < 16; round++)
                {
                    left ^= p[round];
                    right ^= F(left);
                    uint swap = left;
                    left = right;
                    right = swap;
                }

                uint finalSwap = left;
                left = right;
                right = finalSwap;
                right ^= p[16];
                left ^= p[17];
            }
        }

        private void DecryptWords(ref uint left, ref uint right)
        {
            unchecked
            {
                for (int round = 17; round > 1; round--)
                {
                    left ^= p[round];
                    right ^= F(left);
                    uint swap = left;
                    left = right;
                    right = swap;
                }

                uint finalSwap = left;
                left = right;
                right = finalSwap;
                right ^= p[1];
                left ^= p[0];
            }
        }

        private uint F(uint value)
        {
            unchecked
            {
                uint result = s[(value >> 24) & 0xff];
                result += s[256 + ((value >> 16) & 0xff)];
                result ^= s[512 + ((value >> 8) & 0xff)];
                result += s[768 + (value & 0xff)];
                return result;
            }
        }

        private static void ValidateBlock(
            ReadOnlySpan<byte> input,
            Span<byte> output)
        {
            if (input.Length != BlockSize || output.Length != BlockSize)
            {
                throw Error(
                    MixCryptoDiagnosticCode.InvalidBlockLength,
                    "block",
                    -1,
                    "Blowfish block input and output must each contain exactly 8 bytes.");
            }
        }

        private static uint ReadBigEndian(ReadOnlySpan<byte> value, int offset)
        {
            return ((uint)value[offset] << 24) |
                ((uint)value[offset + 1] << 16) |
                ((uint)value[offset + 2] << 8) |
                value[offset + 3];
        }

        private static uint ReadLittleEndian(ReadOnlySpan<byte> value, int offset)
        {
            return value[offset] |
                ((uint)value[offset + 1] << 8) |
                ((uint)value[offset + 2] << 16) |
                ((uint)value[offset + 3] << 24);
        }

        private static void WriteBigEndian(Span<byte> value, int offset, uint word)
        {
            value[offset] = (byte)(word >> 24);
            value[offset + 1] = (byte)(word >> 16);
            value[offset + 2] = (byte)(word >> 8);
            value[offset + 3] = (byte)word;
        }

        private static void WriteLittleEndian(Span<byte> value, int offset, uint word)
        {
            value[offset] = (byte)word;
            value[offset + 1] = (byte)(word >> 8);
            value[offset + 2] = (byte)(word >> 16);
            value[offset + 3] = (byte)(word >> 24);
        }

        private static uint ReverseBytes(uint value)
        {
            return (value >> 24) |
                ((value >> 8) & 0x0000ff00u) |
                ((value << 8) & 0x00ff0000u) |
                (value << 24);
        }

        private static MixCryptoException Error(
            MixCryptoDiagnosticCode code,
            string field,
            int blockIndex,
            string message)
        {
            return new MixCryptoException(
                new MixCryptoDiagnostic(code, field, blockIndex, message));
        }
    }
}
