using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using RA2YR.Core.Binary;
using RA2YR.Core.Content;
using RA2YR.Core.Formats.Mix;
using RA2YR.Core.Formats.ShpTs;

namespace RA2YR.Tests.EditMode.Formats.ShpTs
{
    internal static class ShpTsTestSupport
    {
        public static BinarySourceContext Source(long marker = 0)
        {
            return new BinarySourceContext(
                "format.shp-ts-directory",
                "synthetic-source",
                LogicalContentPath.Parse(marker == 0
                    ? "synthetic/sample.shp"
                    : "synthetic/sample-" + marker + ".shp"));
        }

        public static ShpTsSourceProvenance Provenance()
        {
            return new ShpTsSourceProvenance(
                "synthetic-source",
                new[]
                {
                    LogicalContentPath.Parse("synthetic.mix"),
                    LogicalContentPath.Parse("sample.shp")
                });
        }

        public static ShpTsReadLimits Limits(
            long maxInputBytes = 4 * 1024 * 1024,
            long maxSingleReadBytes = 1024 * 1024,
            int maxFrameCount = 1024,
            int maxCanvasDimension = 4096,
            long maxCanvasArea = 16 * 1024 * 1024,
            long maxLocalFrameArea = 8 * 1024 * 1024,
            long maxTotalDecodedPixels = 32 * 1024 * 1024,
            int maxSingleRowBytes = 65535,
            long maxSingleFrameCompressedBytes = 8 * 1024 * 1024,
            int maxCommandsPerRow = 65535,
            long maxCommandsPerFrame = 1024 * 1024,
            long maxAllocatedBytes = 64 * 1024 * 1024,
            long maxDescriptors = 1024,
            long maxSubwindows = 4096,
            int maxDiagnostics = 1024)
        {
            return new ShpTsReadLimits(
                maxInputBytes,
                maxSingleReadBytes,
                maxFrameCount,
                maxCanvasDimension,
                maxCanvasArea,
                maxLocalFrameArea,
                maxTotalDecodedPixels,
                maxSingleRowBytes,
                maxSingleFrameCompressedBytes,
                maxCommandsPerRow,
                maxCommandsPerFrame,
                maxAllocatedBytes,
                maxDescriptors,
                maxSubwindows,
                maxDiagnostics);
        }

        public static ShpTsDocument AssertParseSuccess(ShpTsParseResult result)
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result.IsSuccess, Is.True,
                result.Diagnostics.Count == 0 ? null : result.Diagnostics[0].Code.ToString());
            Assert.That(result.Document, Is.Not.Null);
            return result.Document;
        }

        public static ShpTsDiagnostic AssertParseFailure(
            ShpTsParseResult result,
            ShpTsDiagnosticCode code)
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Document, Is.Null);
            Assert.That(result.Diagnostics.Any(item => item.Code == code), Is.True,
                string.Join(",", result.Diagnostics.Select(item => item.Code.ToString())));
            return result.Diagnostics.First(item => item.Code == code);
        }

        public static ShpTsIndexedLocalFrame AssertDecodeSuccess(ShpTsDecodeResult result)
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result.IsSuccess, Is.True,
                result.Diagnostics.Count == 0 ? null : result.Diagnostics[0].Code.ToString());
            Assert.That(result.Frame, Is.Not.Null);
            return result.Frame;
        }

        public static ShpTsDiagnostic AssertDecodeFailure(
            ShpTsDecodeResult result,
            ShpTsDiagnosticCode code)
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Frame, Is.Null);
            Assert.That(result.Diagnostics.Any(item => item.Code == code), Is.True,
                string.Join(",", result.Diagnostics.Select(item => item.Code.ToString())));
            return result.Diagnostics.First(item => item.Code == code);
        }

        public static ShpTsParseResult Read(byte[] bytes, ShpTsReadLimits limits = null)
        {
            return WestwoodShpTsReader.Read(bytes, Source(), Provenance(), limits);
        }

        public static Tuple<ShpTsDocument, byte[]> Parse(byte[] bytes, ShpTsReadLimits limits = null)
        {
            return Tuple.Create(AssertParseSuccess(Read(bytes, limits)), bytes);
        }

        public static MixArchiveReadResult ReadSyntheticMix(byte[] payload)
        {
            byte[] archive = new byte[checked(18 + payload.Length)];
            WriteUInt16(archive, 0, 1);
            WriteUInt32(archive, 2, checked((uint)payload.Length));
            WriteUInt32(archive, 6, MixFileId.ComputeCandidateId("sample.shp").Value);
            WriteUInt32(archive, 10, 0);
            WriteUInt32(archive, 14, checked((uint)payload.Length));
            Buffer.BlockCopy(payload, 0, archive, 18, payload.Length);
            return MixArchiveReader.Read(
                archive,
                new BinarySourceContext(
                    "format.mix-container-read",
                    "synthetic-source",
                    LogicalContentPath.Parse("synthetic.mix")),
                new MixReadLimits(
                    8 * 1024 * 1024,
                    16,
                    1024,
                    8 * 1024 * 1024,
                    8 * 1024 * 1024,
                    64,
                    16));
        }

        public static byte[] Extend(byte[] bytes, int count, byte value = 0xcc)
        {
            byte[] result = new byte[checked(bytes.Length + count)];
            Buffer.BlockCopy(bytes, 0, result, 0, bytes.Length);
            for (int index = bytes.Length; index < result.Length; index++)
            {
                result[index] = value;
            }

            return result;
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
    }

    internal sealed class ShpTsShortReadMemoryStream : MemoryStream
    {
        private readonly int maximumChunk;

        public ShpTsShortReadMemoryStream(byte[] bytes, int maximumChunk)
            : base(bytes, false)
        {
            this.maximumChunk = maximumChunk;
        }

        public int ReadCalls { get; private set; }

        public override int Read(byte[] buffer, int offset, int count)
        {
            ReadCalls++;
            return base.Read(buffer, offset, Math.Min(count, maximumChunk));
        }
    }
}
