using System;
using System.Collections.Generic;
using System.Linq;

namespace RA2YR.Tests.EditMode.Formats.ShpTs
{
    internal static class ShpTsSyntheticFixtureFactory
    {
        internal sealed class FrameSpec
        {
            public ushort X;
            public ushort Y;
            public ushort Width;
            public ushort Height;
            public uint Flags;
            public byte[] FrameColor = new byte[4];
            public uint Reserved;
            public uint? DataOffset;
            public byte[] Payload = Array.Empty<byte>();
        }

        public static byte[] Build(
            ushort canvasWidth,
            ushort canvasHeight,
            params FrameSpec[] frames)
        {
            return Build(0, canvasWidth, canvasHeight, checked((ushort)frames.Length), frames);
        }

        public static byte[] Build(
            ushort familyMarker,
            ushort canvasWidth,
            ushort canvasHeight,
            ushort declaredFrameCount,
            params FrameSpec[] frames)
        {
            if (frames == null)
            {
                throw new ArgumentNullException(nameof(frames));
            }

            int directoryLength = checked(8 + frames.Length * 24);
            uint nextOffset = AlignEight(checked((uint)directoryLength));
            var offsets = new uint[frames.Length];
            int outputLength = directoryLength;
            for (int index = 0; index < frames.Length; index++)
            {
                FrameSpec frame = frames[index] ?? throw new ArgumentException("Frame cannot be null.");
                bool canonicalEmpty = frame.Width == 0 && frame.Height == 0 &&
                    !frame.DataOffset.HasValue && frame.Payload.Length == 0;
                uint offset = canonicalEmpty ? 0 : frame.DataOffset ?? nextOffset;
                offsets[index] = offset;
                if (!canonicalEmpty)
                {
                    outputLength = Math.Max(
                        outputLength,
                        checked((int)offset + frame.Payload.Length));
                    if (!frame.DataOffset.HasValue)
                    {
                        nextOffset = AlignEight(checked(offset + (uint)frame.Payload.Length));
                    }
                }
            }

            var bytes = new byte[outputLength];
            WriteUInt16(bytes, 0, familyMarker);
            WriteUInt16(bytes, 2, canvasWidth);
            WriteUInt16(bytes, 4, canvasHeight);
            WriteUInt16(bytes, 6, declaredFrameCount);
            for (int index = 0; index < frames.Length; index++)
            {
                FrameSpec frame = frames[index];
                int descriptor = checked(8 + index * 24);
                WriteUInt16(bytes, descriptor, frame.X);
                WriteUInt16(bytes, descriptor + 2, frame.Y);
                WriteUInt16(bytes, descriptor + 4, frame.Width);
                WriteUInt16(bytes, descriptor + 6, frame.Height);
                WriteUInt32(bytes, descriptor + 8, frame.Flags);
                if (frame.FrameColor == null || frame.FrameColor.Length != 4)
                {
                    throw new ArgumentException("FrameColor requires four bytes.");
                }

                Buffer.BlockCopy(frame.FrameColor, 0, bytes, descriptor + 12, 4);
                WriteUInt32(bytes, descriptor + 16, frame.Reserved);
                WriteUInt32(bytes, descriptor + 20, offsets[index]);
                if (offsets[index] != 0 && frame.Payload.Length != 0)
                {
                    Buffer.BlockCopy(frame.Payload, 0, bytes, checked((int)offsets[index]), frame.Payload.Length);
                }
            }

            return bytes;
        }

        public static FrameSpec Empty(ushort x = 0, ushort y = 0)
        {
            return new FrameSpec { X = x, Y = y };
        }

        public static FrameSpec Raw(
            ushort width,
            ushort height,
            uint flags,
            params byte[] pixels)
        {
            return new FrameSpec
            {
                Width = width,
                Height = height,
                Flags = flags,
                Payload = pixels ?? Array.Empty<byte>()
            };
        }

        public static FrameSpec Rle(
            ushort width,
            ushort height,
            params byte[][] rowCommands)
        {
            var payload = new List<byte>();
            foreach (byte[] row in rowCommands)
            {
                byte[] commands = row ?? Array.Empty<byte>();
                WriteUInt16(payload, checked((ushort)(commands.Length + 2)));
                payload.AddRange(commands);
            }

            return new FrameSpec
            {
                Width = width,
                Height = height,
                Flags = 3,
                Payload = payload.ToArray()
            };
        }

        public static byte[] RlePayloadWithDeclaredLine(
            ushort declaredLineLength,
            params byte[] commands)
        {
            var bytes = new List<byte>();
            WriteUInt16(bytes, declaredLineLength);
            bytes.AddRange(commands ?? Array.Empty<byte>());
            return bytes.ToArray();
        }

        private static uint AlignEight(uint value)
        {
            return checked((value + 7u) & ~7u);
        }

        private static void WriteUInt16(byte[] bytes, int offset, ushort value)
        {
            bytes[offset] = (byte)value;
            bytes[offset + 1] = (byte)(value >> 8);
        }

        private static void WriteUInt32(byte[] bytes, int offset, uint value)
        {
            bytes[offset] = (byte)value;
            bytes[offset + 1] = (byte)(value >> 8);
            bytes[offset + 2] = (byte)(value >> 16);
            bytes[offset + 3] = (byte)(value >> 24);
        }

        private static void WriteUInt16(ICollection<byte> bytes, ushort value)
        {
            bytes.Add((byte)value);
            bytes.Add((byte)(value >> 8));
        }
    }
}
