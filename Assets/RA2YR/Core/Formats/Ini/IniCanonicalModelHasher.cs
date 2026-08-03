using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace RA2YR.Core.Formats.Ini
{
    internal static class IniCanonicalModelHasher
    {
        private const string Domain = "RA2YR.INI.RAW-DOCUMENT.V1\0";

        public static string Compute(IniRawDocument document)
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
                stream.WriteByte((byte)document.ByteOrderMarkKind);
                stream.WriteByte((byte)document.PhysicalEncoding);
                stream.WriteByte((byte)document.Completeness);
                WriteInt32(stream, document.OriginalLength);
                WriteInt32(stream, document.Lines.Count);

                ReadOnlySpan<byte> original = document.OriginalSpan;
                for (int index = 0; index < original.Length; index++)
                {
                    stream.WriteByte(original[index]);
                }

                foreach (IniNode node in document.Nodes)
                {
                    IniPhysicalLine line = node.Line;
                    stream.WriteByte((byte)node.Kind);
                    WriteInt32(stream, line.Content.Offset);
                    WriteInt32(stream, line.Content.Length);
                    stream.WriteByte((byte)line.EndingKind);
                    switch (node.Kind)
                    {
                        case IniNodeKind.Section:
                            var section = (IniSectionNode)node;
                            WriteSlice(stream, section.RawName);
                            WriteSlice(stream, section.Name);
                            WriteSlice(stream, section.TrailingBytes);
                            break;
                        case IniNodeKind.KeyValue:
                            var keyValue = (IniKeyValueNode)node;
                            WriteInt32(stream, keyValue.ContainingSectionLineId);
                            WriteSlice(stream, keyValue.LeadingWhitespace);
                            WriteSlice(stream, keyValue.Key);
                            WriteSlice(stream, keyValue.WhitespaceBeforeEquals);
                            WriteSlice(stream, keyValue.WhitespaceAfterEquals);
                            WriteSlice(stream, keyValue.Value);
                            WriteInt32(stream, keyValue.EqualsByteOffset);
                            break;
                        case IniNodeKind.Comment:
                            var comment = (IniCommentNode)node;
                            WriteInt32(stream, comment.MarkerByteOffset);
                            WriteSlice(stream, comment.Body);
                            break;
                        case IniNodeKind.Opaque:
                            stream.WriteByte((byte)((IniOpaqueNode)node).Reason);
                            break;
                    }
                }

                stream.FlushFinalBlock();
                return BitConverter.ToString(sha256.Hash)
                    .Replace("-", string.Empty)
                    .ToLowerInvariant();
            }
        }

        private static void WriteSlice(Stream stream, IniRawSlice slice)
        {
            WriteInt32(stream, slice.Offset);
            WriteInt32(stream, slice.Length);
        }

        private static void WriteInt32(Stream stream, int value)
        {
            uint raw = unchecked((uint)value);
            stream.WriteByte((byte)(raw & 0xff));
            stream.WriteByte((byte)((raw >> 8) & 0xff));
            stream.WriteByte((byte)((raw >> 16) & 0xff));
            stream.WriteByte((byte)((raw >> 24) & 0xff));
        }
    }
}
