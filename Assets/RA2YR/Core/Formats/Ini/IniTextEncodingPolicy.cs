using System;
using System.Text;

namespace RA2YR.Core.Formats.Ini
{
    internal enum IniTextEncodingPolicyKind
    {
        StrictAscii,
        StrictUtf8,
        StrictUtf16LittleEndian,
        StrictUtf16BigEndian
    }

    internal sealed class IniTextEncodingPolicy
    {
        private static readonly Encoding Utf8 = new UTF8Encoding(false, true);
        private static readonly Encoding Utf16LittleEndian =
            new UnicodeEncoding(false, false, true);
        private static readonly Encoding Utf16BigEndian =
            new UnicodeEncoding(true, false, true);

        private IniTextEncodingPolicy(IniTextEncodingPolicyKind kind)
        {
            Kind = kind;
        }

        public static IniTextEncodingPolicy StrictAscii { get; } =
            new IniTextEncodingPolicy(IniTextEncodingPolicyKind.StrictAscii);

        public static IniTextEncodingPolicy StrictUtf8 { get; } =
            new IniTextEncodingPolicy(IniTextEncodingPolicyKind.StrictUtf8);

        public static IniTextEncodingPolicy StrictUtf16LittleEndian { get; } =
            new IniTextEncodingPolicy(IniTextEncodingPolicyKind.StrictUtf16LittleEndian);

        public static IniTextEncodingPolicy StrictUtf16BigEndian { get; } =
            new IniTextEncodingPolicy(IniTextEncodingPolicyKind.StrictUtf16BigEndian);

        public IniTextEncodingPolicyKind Kind { get; }

        public string Decode(IniRawSlice rawBytes)
        {
            byte[] bytes = rawBytes.ToArray();
            switch (Kind)
            {
                case IniTextEncodingPolicyKind.StrictAscii:
                    var characters = new char[bytes.Length];
                    for (int index = 0; index < bytes.Length; index++)
                    {
                        if (bytes[index] > 0x7f)
                        {
                            throw new DecoderFallbackException(
                                "The raw INI field is not strict ASCII.");
                        }

                        characters[index] = (char)bytes[index];
                    }

                    return new string(characters);
                case IniTextEncodingPolicyKind.StrictUtf8:
                    return Utf8.GetString(bytes);
                case IniTextEncodingPolicyKind.StrictUtf16LittleEndian:
                    return Utf16LittleEndian.GetString(bytes);
                case IniTextEncodingPolicyKind.StrictUtf16BigEndian:
                    return Utf16BigEndian.GetString(bytes);
                default:
                    throw new InvalidOperationException("The INI text policy is invalid.");
            }
        }

        internal static byte[] EncodeAsciiForPhysicalDocument(
            string asciiValue,
            IniPhysicalEncodingKind physicalEncoding)
        {
            if (asciiValue == null)
            {
                throw new ArgumentNullException(nameof(asciiValue));
            }

            foreach (char value in asciiValue)
            {
                if (value > 0x7f || value == '\0')
                {
                    throw new ArgumentException(
                        "Raw ASCII queries require non-NUL ASCII characters.",
                        nameof(asciiValue));
                }
            }

            switch (physicalEncoding)
            {
                case IniPhysicalEncodingKind.RawSingleByte:
                case IniPhysicalEncodingKind.Utf8WithBom:
                    return Encoding.ASCII.GetBytes(asciiValue);
                case IniPhysicalEncodingKind.Utf16LittleEndianWithBom:
                    return EncodeAsciiUtf16(asciiValue, false);
                case IniPhysicalEncodingKind.Utf16BigEndianWithBom:
                    return EncodeAsciiUtf16(asciiValue, true);
                default:
                    throw new ArgumentOutOfRangeException(nameof(physicalEncoding));
            }
        }

        private static byte[] EncodeAsciiUtf16(string value, bool bigEndian)
        {
            var bytes = new byte[checked(value.Length * 2)];
            for (int index = 0; index < value.Length; index++)
            {
                byte ascii = checked((byte)value[index]);
                int offset = checked(index * 2);
                if (bigEndian)
                {
                    bytes[offset] = 0;
                    bytes[offset + 1] = ascii;
                }
                else
                {
                    bytes[offset] = ascii;
                    bytes[offset + 1] = 0;
                }
            }

            return bytes;
        }
    }
}
