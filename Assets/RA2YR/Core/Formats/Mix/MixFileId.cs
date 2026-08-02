using System;

namespace RA2YR.Core.Formats.Mix
{
    internal readonly struct MixFileId : IEquatable<MixFileId>, IComparable<MixFileId>
    {
        private const uint ReflectedPolynomial = 0xedb88320u;

        private static readonly uint[] CrcTable = CreateCrcTable();

        private MixFileId(uint value)
        {
            Value = value;
        }

        public uint Value { get; }

        public static MixFileId FromRaw(uint value)
        {
            return new MixFileId(value);
        }

        public static MixFileId ComputeCandidateId(string archiveName)
        {
            byte[] normalized = NormalizeAndPad(archiveName);
            uint crc = uint.MaxValue;
            for (int index = 0; index < normalized.Length; index++)
            {
                crc = (crc >> 8) ^ CrcTable[(crc ^ normalized[index]) & 0xff];
            }

            return new MixFileId(~crc);
        }

        public bool Equals(MixFileId other)
        {
            return Value == other.Value;
        }

        public override bool Equals(object obj)
        {
            return obj is MixFileId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return unchecked((int)Value);
        }

        public int CompareTo(MixFileId other)
        {
            return Value.CompareTo(other.Value);
        }

        public override string ToString()
        {
            return "0x" + Value.ToString("X8");
        }

        public static bool operator ==(MixFileId left, MixFileId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(MixFileId left, MixFileId right)
        {
            return !left.Equals(right);
        }

        private static byte[] NormalizeAndPad(string archiveName)
        {
            if (archiveName == null)
            {
                throw new ArgumentNullException(nameof(archiveName));
            }

            if (archiveName.Length == 0)
            {
                throw new ArgumentException("A MIX archive name is required.", nameof(archiveName));
            }

            byte[] normalized = new byte[archiveName.Length];
            bool previousWasSeparator = false;
            for (int index = 0; index < archiveName.Length; index++)
            {
                char character = archiveName[index];
                if (character == '\0' || character > 0x7f || char.IsControl(character))
                {
                    throw new ArgumentException(
                        "The deterministic YR MIX name mode accepts printable ASCII only.",
                        nameof(archiveName));
                }

                bool isSeparator = character == '/' || character == '\\';
                if ((index == 0 && isSeparator) ||
                    (index == archiveName.Length - 1 && isSeparator) ||
                    (isSeparator && previousWasSeparator))
                {
                    throw new ArgumentException(
                        "A MIX archive name must be a non-empty relative path.",
                        nameof(archiveName));
                }

                if (character == ':')
                {
                    throw new ArgumentException(
                        "A host or drive path cannot be used as a MIX archive name.",
                        nameof(archiveName));
                }

                if (isSeparator)
                {
                    normalized[index] = (byte)'\\';
                }
                else if (character >= 'a' && character <= 'z')
                {
                    normalized[index] = (byte)(character - ('a' - 'A'));
                }
                else
                {
                    normalized[index] = (byte)character;
                }

                previousWasSeparator = isSeparator;
            }

            RejectTraversalSegments(normalized, nameof(archiveName));

            int remainder = normalized.Length & 3;
            if (remainder == 0)
            {
                return normalized;
            }

            int paddedLength = checked(normalized.Length + 4 - remainder);
            byte[] padded = new byte[paddedLength];
            Buffer.BlockCopy(normalized, 0, padded, 0, normalized.Length);
            padded[normalized.Length] = (byte)remainder;
            byte firstByteOfPartialGroup = normalized[normalized.Length - remainder];
            for (int index = normalized.Length + 1; index < padded.Length; index++)
            {
                padded[index] = firstByteOfPartialGroup;
            }

            return padded;
        }

        private static void RejectTraversalSegments(byte[] normalized, string parameterName)
        {
            int segmentStart = 0;
            for (int index = 0; index <= normalized.Length; index++)
            {
                if (index != normalized.Length && normalized[index] != (byte)'\\')
                {
                    continue;
                }

                int segmentLength = index - segmentStart;
                if ((segmentLength == 1 && normalized[segmentStart] == (byte)'.') ||
                    (segmentLength == 2 && normalized[segmentStart] == (byte)'.' &&
                     normalized[segmentStart + 1] == (byte)'.'))
                {
                    throw new ArgumentException(
                        "A MIX archive name cannot contain traversal segments.",
                        parameterName);
                }

                segmentStart = index + 1;
            }
        }

        private static uint[] CreateCrcTable()
        {
            var table = new uint[256];
            for (uint index = 0; index < table.Length; index++)
            {
                uint value = index;
                for (int bit = 0; bit < 8; bit++)
                {
                    value = (value & 1) != 0
                        ? (value >> 1) ^ ReflectedPolynomial
                        : value >> 1;
                }

                table[index] = value;
            }

            return table;
        }
    }
}
