using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace RA2YR.Core.Formats.Csf
{
    internal static class CsfCanonicalModelHasher
    {
        private const string Domain = "RA2YR.CSF.RAW.V1\0";

        public static string Compute(CsfDocument document)
        {
            if (document == null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            using (SHA256 sha256 = SHA256.Create())
            using (var stream = new CryptoStream(Stream.Null, sha256, CryptoStreamMode.Write))
            {
                byte[] domain = Encoding.ASCII.GetBytes(Domain);
                stream.Write(domain, 0, domain.Length);

                CsfHeader header = document.Header;
                WriteUInt32(stream, header.Signature);
                WriteUInt32(stream, header.Version);
                WriteUInt32(stream, header.DeclaredLabelCount);
                WriteUInt32(stream, header.DeclaredValueCount);
                WriteUInt32(stream, header.Reserved);
                WriteUInt32(stream, header.Language.RawValue);

                foreach (CsfLabel label in document.Labels)
                {
                    WriteAscii(stream, label.Name);
                    WriteUInt32(stream, checked((uint)label.Values.Count));
                    foreach (CsfValue value in label.Values)
                    {
                        stream.WriteByte((byte)value.Kind);
                        WriteUInt32(stream, checked((uint)value.Text.Length));
                        foreach (char codeUnit in value.Text.CodeUnits)
                        {
                            WriteUInt16(stream, codeUnit);
                        }

                        if (value.Kind == CsfValueKind.Extended)
                        {
                            WriteAscii(stream, value.ExtraText);
                        }
                    }
                }

                stream.FlushFinalBlock();
                return BitConverter.ToString(sha256.Hash)
                    .Replace("-", string.Empty)
                    .ToLowerInvariant();
            }
        }

        private static void WriteAscii(Stream stream, string value)
        {
            WriteUInt32(stream, checked((uint)value.Length));
            foreach (char character in value)
            {
                stream.WriteByte(checked((byte)character));
            }
        }

        private static void WriteUInt16(Stream stream, ushort value)
        {
            stream.WriteByte(checked((byte)(value & 0xff)));
            stream.WriteByte(checked((byte)(value >> 8)));
        }

        private static void WriteUInt32(Stream stream, uint value)
        {
            stream.WriteByte(checked((byte)(value & 0xff)));
            stream.WriteByte(checked((byte)((value >> 8) & 0xff)));
            stream.WriteByte(checked((byte)((value >> 16) & 0xff)));
            stream.WriteByte(checked((byte)((value >> 24) & 0xff)));
        }
    }
}
