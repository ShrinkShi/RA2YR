using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using RA2YR.Core.Binary;

namespace RA2YR.Core.Formats.ShpTs
{
    internal enum ShpTsCompressionKind
    {
        RawOpaque,
        RawTransparent,
        SourceConflictingFlags2,
        RleZeroTransparent,
        UnknownFlags
    }

    internal sealed class ShpTsHeader
    {
        internal ShpTsHeader(
            ushort familyMarkerRaw,
            ushort canvasWidthRaw,
            ushort canvasHeightRaw,
            ushort frameCountRaw)
        {
            FamilyMarkerRaw = familyMarkerRaw;
            CanvasWidthRaw = canvasWidthRaw;
            CanvasHeightRaw = canvasHeightRaw;
            FrameCountRaw = frameCountRaw;
        }

        public ushort FamilyMarkerRaw { get; }
        public ushort CanvasWidthRaw { get; }
        public ushort CanvasHeightRaw { get; }
        public ushort FrameCountRaw { get; }
    }

    internal sealed class ShpTsFrameDescriptor
    {
        private readonly byte[] frameColorRaw;

        internal ShpTsFrameDescriptor(
            int index,
            long descriptorAbsoluteOffset,
            ushort xRaw,
            ushort yRaw,
            ushort widthRaw,
            ushort heightRaw,
            uint rawFlags,
            byte[] frameColorRaw,
            uint reservedRaw,
            uint dataOffsetRaw,
            long dataAbsoluteOffset,
            long dataUpperBoundRelative,
            ShpTsCompressionKind compressionKind,
            bool isCanonicalEmpty)
        {
            Index = index;
            DescriptorAbsoluteOffset = descriptorAbsoluteOffset;
            XRaw = xRaw;
            YRaw = yRaw;
            WidthRaw = widthRaw;
            HeightRaw = heightRaw;
            RawFlags = rawFlags;
            this.frameColorRaw = (byte[])(frameColorRaw ??
                throw new ArgumentNullException(nameof(frameColorRaw))).Clone();
            if (this.frameColorRaw.Length != 4)
            {
                throw new ArgumentException("FrameColorRaw requires four bytes.", nameof(frameColorRaw));
            }

            ReservedRaw = reservedRaw;
            DataOffsetRaw = dataOffsetRaw;
            DataAbsoluteOffset = dataAbsoluteOffset;
            DataUpperBoundRelative = dataUpperBoundRelative;
            CompressionKind = compressionKind;
            IsCanonicalEmpty = isCanonicalEmpty;
        }

        public int Index { get; }
        public long DescriptorAbsoluteOffset { get; }
        public ushort XRaw { get; }
        public ushort YRaw { get; }
        public ushort WidthRaw { get; }
        public ushort HeightRaw { get; }
        public uint RawFlags { get; }
        public uint ReservedRaw { get; }
        public uint DataOffsetRaw { get; }
        public long DataAbsoluteOffset { get; }
        public long DataUpperBoundRelative { get; }
        public ShpTsCompressionKind CompressionKind { get; }
        public bool IsCanonicalEmpty { get; }

        public byte[] GetFrameColorRawCopy()
        {
            return (byte[])frameColorRaw.Clone();
        }
    }

    internal sealed class ShpTsDocument
    {
        private const string HashDomain = "RA2YR.SHP.TS.DIRECTORY.V1\0";
        private readonly IReadOnlyList<ShpTsFrameDescriptor> frames;

        internal ShpTsDocument(
            BinarySourceContext source,
            ShpTsSourceProvenance provenance,
            long inputLength,
            long absoluteStartOffset,
            long directoryLength,
            ShpTsHeader header,
            IEnumerable<ShpTsFrameDescriptor> frames)
        {
            Source = source ?? throw new ArgumentNullException(nameof(source));
            Provenance = provenance ?? throw new ArgumentNullException(nameof(provenance));
            InputLength = inputLength;
            AbsoluteStartOffset = absoluteStartOffset;
            DirectoryLength = directoryLength;
            Header = header ?? throw new ArgumentNullException(nameof(header));
            ShpTsFrameDescriptor[] array =
                (frames ?? throw new ArgumentNullException(nameof(frames))).ToArray();
            if (array.Length != header.FrameCountRaw || array.Any(frame => frame == null))
            {
                throw new ArgumentException("The descriptor list must match the raw frame count.", nameof(frames));
            }

            this.frames = Array.AsReadOnly(array);
            CanonicalDirectoryModelSha256 = ComputeDirectoryHash(header, array);
        }

        public BinarySourceContext Source { get; }
        public ShpTsSourceProvenance Provenance { get; }
        public long InputLength { get; }
        public long AbsoluteStartOffset { get; }
        public long DirectoryLength { get; }
        public ShpTsHeader Header { get; }
        public IReadOnlyList<ShpTsFrameDescriptor> Frames => frames;
        public string CanonicalDirectoryModelSha256 { get; }

        private static string ComputeDirectoryHash(
            ShpTsHeader header,
            IEnumerable<ShpTsFrameDescriptor> descriptors)
        {
            using (var stream = new System.IO.MemoryStream())
            {
                byte[] domain = Encoding.ASCII.GetBytes(HashDomain);
                stream.Write(domain, 0, domain.Length);
                WriteUInt16(stream, header.FamilyMarkerRaw);
                WriteUInt16(stream, header.CanvasWidthRaw);
                WriteUInt16(stream, header.CanvasHeightRaw);
                WriteUInt16(stream, header.FrameCountRaw);
                foreach (ShpTsFrameDescriptor frame in descriptors)
                {
                    WriteUInt32(stream, checked((uint)frame.Index));
                    WriteUInt16(stream, frame.XRaw);
                    WriteUInt16(stream, frame.YRaw);
                    WriteUInt16(stream, frame.WidthRaw);
                    WriteUInt16(stream, frame.HeightRaw);
                    WriteUInt32(stream, frame.RawFlags);
                    byte[] color = frame.GetFrameColorRawCopy();
                    stream.Write(color, 0, color.Length);
                    WriteUInt32(stream, frame.ReservedRaw);
                    WriteUInt32(stream, frame.DataOffsetRaw);
                }

                using (SHA256 sha256 = SHA256.Create())
                {
                    return BitConverter.ToString(sha256.ComputeHash(stream.ToArray()))
                        .Replace("-", string.Empty)
                        .ToLowerInvariant();
                }
            }
        }

        private static void WriteUInt16(System.IO.Stream stream, ushort value)
        {
            stream.WriteByte((byte)value);
            stream.WriteByte((byte)(value >> 8));
        }

        private static void WriteUInt32(System.IO.Stream stream, uint value)
        {
            stream.WriteByte((byte)value);
            stream.WriteByte((byte)(value >> 8));
            stream.WriteByte((byte)(value >> 16));
            stream.WriteByte((byte)(value >> 24));
        }
    }

    internal sealed class ShpTsIndexedLocalFrame
    {
        private readonly byte[] indices;

        internal ShpTsIndexedLocalFrame(
            int frameIndex,
            ushort width,
            ushort height,
            ShpTsCompressionKind compressionKind,
            long bytesConsumed,
            long paddingBytes,
            byte[] indices)
        {
            this.indices = (byte[])(indices ?? throw new ArgumentNullException(nameof(indices))).Clone();
            if (this.indices.LongLength != checked((long)width * height))
            {
                throw new ArgumentException("Decoded indices must match the local rectangle.", nameof(indices));
            }

            FrameIndex = frameIndex;
            Width = width;
            Height = height;
            CompressionKind = compressionKind;
            BytesConsumed = bytesConsumed;
            PaddingBytes = paddingBytes;
            byte minimum = byte.MaxValue;
            byte maximum = 0;
            foreach (byte value in this.indices)
            {
                minimum = Math.Min(minimum, value);
                maximum = Math.Max(maximum, value);
            }

            MinimumIndex = this.indices.Length == 0 ? (byte)0 : minimum;
            MaximumIndex = this.indices.Length == 0 ? (byte)0 : maximum;
        }

        public int FrameIndex { get; }
        public ushort Width { get; }
        public ushort Height { get; }
        public ShpTsCompressionKind CompressionKind { get; }
        public long BytesConsumed { get; }
        public long PaddingBytes { get; }
        public byte MinimumIndex { get; }
        public byte MaximumIndex { get; }
        public int PixelCount => indices.Length;

        public byte[] GetIndicesCopy()
        {
            return (byte[])indices.Clone();
        }
    }

    internal sealed class ShpTsDecodedDocument
    {
        private const string HashDomain = "RA2YR.SHP.TS.DECODED.V1\0";
        private readonly IReadOnlyList<ShpTsIndexedLocalFrame> frames;

        internal ShpTsDecodedDocument(IEnumerable<ShpTsIndexedLocalFrame> frames)
        {
            ShpTsIndexedLocalFrame[] array =
                (frames ?? throw new ArgumentNullException(nameof(frames))).ToArray();
            if (array.Any(frame => frame == null) ||
                array.Select(frame => frame.FrameIndex).Distinct().Count() != array.Length)
            {
                throw new ArgumentException("Decoded frames must be non-null and unique.", nameof(frames));
            }

            this.frames = Array.AsReadOnly(array.OrderBy(frame => frame.FrameIndex).ToArray());
            CanonicalDecodedModelSha256 = ComputeHash(this.frames);
        }

        public IReadOnlyList<ShpTsIndexedLocalFrame> Frames => frames;

        public string CanonicalDecodedModelSha256 { get; }

        private static string ComputeHash(IEnumerable<ShpTsIndexedLocalFrame> decodedFrames)
        {
            using (var stream = new System.IO.MemoryStream())
            {
                byte[] domain = Encoding.ASCII.GetBytes(HashDomain);
                stream.Write(domain, 0, domain.Length);
                foreach (ShpTsIndexedLocalFrame frame in decodedFrames)
                {
                    WriteUInt32(stream, checked((uint)frame.FrameIndex));
                    WriteUInt16(stream, frame.Width);
                    WriteUInt16(stream, frame.Height);
                    WriteUInt32(stream, checked((uint)frame.CompressionKind));
                    WriteUInt64(stream, checked((ulong)frame.BytesConsumed));
                    byte[] indices = frame.GetIndicesCopy();
                    WriteUInt64(stream, checked((ulong)indices.LongLength));
                    stream.Write(indices, 0, indices.Length);
                }

                using (SHA256 sha256 = SHA256.Create())
                {
                    return BitConverter.ToString(sha256.ComputeHash(stream.ToArray()))
                        .Replace("-", string.Empty)
                        .ToLowerInvariant();
                }
            }
        }

        private static void WriteUInt16(System.IO.Stream stream, ushort value)
        {
            stream.WriteByte((byte)value);
            stream.WriteByte((byte)(value >> 8));
        }

        private static void WriteUInt32(System.IO.Stream stream, uint value)
        {
            stream.WriteByte((byte)value);
            stream.WriteByte((byte)(value >> 8));
            stream.WriteByte((byte)(value >> 16));
            stream.WriteByte((byte)(value >> 24));
        }

        private static void WriteUInt64(System.IO.Stream stream, ulong value)
        {
            WriteUInt32(stream, (uint)value);
            WriteUInt32(stream, (uint)(value >> 32));
        }
    }
}
