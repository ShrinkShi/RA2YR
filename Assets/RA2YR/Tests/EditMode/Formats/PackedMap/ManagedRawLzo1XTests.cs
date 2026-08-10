using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using NUnit.Framework;
using RA2YR.Core.Binary;
using RA2YR.Core.Content;
using RA2YR.Core.Formats.Ini;
using RA2YR.Core.Formats.PackedMap;

namespace RA2YR.Tests.EditMode.Formats.PackedMap
{
    public sealed class ManagedRawLzo1XTests
    {
        [Test]
        public void MinimalTerminalProducesEmptyOutput()
        {
            LzoDecodeResult result = Decode(new byte[] { 0x11, 0x00, 0x00 }, 0);
            AssertSuccess(result, 3, 0);
        }

        [Test]
        public void InitialLiteralRunIsDecoded()
        {
            byte[] raw = Encoding.ASCII.GetBytes("hello");
            LzoDecodeResult result = Decode(InitialLiteral(raw), raw.Length);
            AssertSuccess(result, raw.Length + 4, raw.Length);
            Assert.That(result.Bytes, Is.EqualTo(raw));
        }

        [Test]
        public void ShortDistanceMatchSupportsOverlap()
        {
            byte[] compressed = { 0x14, (byte)'A', (byte)'B', (byte)'C', 0x08, 0x00, 0x11, 0x00, 0x00 };
            LzoDecodeResult result = Decode(compressed, 5);
            AssertSuccess(result, compressed.Length, 5);
            Assert.That(result.Bytes, Is.EqualTo(Encoding.ASCII.GetBytes("ABCAB")));
        }

        [Test]
        public void MediumDistanceMatchIsDecoded()
        {
            byte[] compressed = { 0x15, (byte)'A', (byte)'B', (byte)'C', (byte)'D', 0x6C, 0x00, 0x11, 0x00, 0x00 };
            LzoDecodeResult result = Decode(compressed, 8);
            AssertSuccess(result, compressed.Length, 8);
            Assert.That(result.Bytes, Is.EqualTo(Encoding.ASCII.GetBytes("ABCDABCD")));
        }

        [Test]
        public void MediumDistanceMatchUsesBytewiseOverlap()
        {
            byte[] compressed = { 0x14, (byte)'A', (byte)'B', (byte)'C', 0x68, 0x00, 0x11, 0x00, 0x00 };
            LzoDecodeResult result = Decode(compressed, 7);
            AssertSuccess(result, compressed.Length, 7);
            Assert.That(result.Bytes, Is.EqualTo(Encoding.ASCII.GetBytes("ABCABCA")));
        }

        [Test]
        public void MediumFamilyLengthFiveIsDecoded()
        {
            byte[] compressed = { 0x15, (byte)'A', (byte)'B', (byte)'C', (byte)'D', 0x8C, 0x00, 0x11, 0x00, 0x00 };
            LzoDecodeResult result = Decode(compressed, 9);
            AssertSuccess(result, compressed.Length, 9);
            Assert.That(result.Bytes, Is.EqualTo(Encoding.ASCII.GetBytes("ABCDABCDA")));
        }

        [Test]
        public void M3DistanceFamilyIsDecoded()
        {
            byte[] compressed = { 0x15, (byte)'A', (byte)'B', (byte)'C', (byte)'D', 0x21, 0x0C, 0x00, 0x11, 0x00, 0x00 };
            LzoDecodeResult result = Decode(compressed, 7);
            AssertSuccess(result, compressed.Length, 7);
            Assert.That(result.Bytes, Is.EqualTo(Encoding.ASCII.GetBytes("ABCDABC")));
        }

        [Test]
        public void M4LongDistanceFamilyIsDecoded()
        {
            const int literalLength = 238;
            const int repeatedRuns = 490;
            var compressed = new List<byte> { 0xFF };
            compressed.AddRange(Enumerable.Repeat((byte)'A', literalLength));
            for (int index = 0; index < repeatedRuns; index++)
                compressed.AddRange(new byte[] { 0x3F, 0x00, 0x00 });
            compressed.AddRange(new byte[] { 0x11, 0x04, 0x00, 0x11, 0x00, 0x00 });
            int expectedLength = literalLength + repeatedRuns * 33 + 3;
            LzoDecodeResult result = Decode(compressed.ToArray(), expectedLength);
            AssertSuccess(result, compressed.Count, expectedLength);
            Assert.That(result.Bytes.All(item => item == (byte)'A'), Is.True);
        }

        [Test]
        public void ExtendedLiteralLengthIsDecoded()
        {
            byte[] compressed = Concat(new byte[] { 0x00, 0x11 }, Enumerable.Repeat((byte)'D', 35).ToArray(), new byte[] { 0x11, 0x00, 0x00 });
            LzoDecodeResult result = Decode(compressed, 35);
            AssertSuccess(result, compressed.Length, 35);
            Assert.That(result.Bytes.All(item => item == (byte)'D'), Is.True);
        }

        [Test]
        public void EmptyInputFailsClosed()
        {
            LzoDecodeResult result = Decode(Array.Empty<byte>(), 0);
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Diagnostics.Any(item => item.Code == PackedMapDiagnosticCode.BackendInputTruncated), Is.True);
        }

        [Test]
        public void TruncatedLiteralFailsClosed()
        {
            LzoDecodeResult result = Decode(new byte[] { 22, (byte)'A' }, 5);
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Bytes, Is.Null);
            Assert.That(result.Diagnostics.Any(item => item.Code == PackedMapDiagnosticCode.BackendInputTruncated), Is.True);
        }

        [Test]
        public void InvalidLookbehindFailsClosed()
        {
            LzoDecodeResult result = Decode(new byte[] { 0x14, (byte)'A', (byte)'B', (byte)'C', 0x08, 0x7F, 0x11, 0x00, 0x00 }, 5);
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Diagnostics.Any(item => item.Code == PackedMapDiagnosticCode.BackendLookbehindOverrun), Is.True);
        }

        [Test]
        public void DeclaredOutputOverflowFailsClosed()
        {
            LzoDecodeResult result = Decode(InitialLiteral(Encoding.ASCII.GetBytes("hello")), 4);
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Diagnostics.Any(item => item.Code == PackedMapDiagnosticCode.BackendOutputOverflow), Is.True);
        }

        [Test]
        public void DeclaredOutputUnderflowFailsClosed()
        {
            LzoDecodeResult result = Decode(InitialLiteral(Encoding.ASCII.GetBytes("hello")), 6);
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Diagnostics.Any(item => item.Code == PackedMapDiagnosticCode.BackendOutputUnderflow), Is.True);
        }

        [Test]
        public void MissingTerminalFailsClosed()
        {
            byte[] raw = Encoding.ASCII.GetBytes("hello");
            byte[] stream = InitialLiteral(raw).Take(raw.Length + 1).ToArray();
            LzoDecodeResult result = Decode(stream, raw.Length);
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Diagnostics.Any(item => item.Code == PackedMapDiagnosticCode.BackendMissingTerminator), Is.True);
        }

        [Test]
        public void TrailingCompressedBytesAreRejected()
        {
            byte[] raw = Encoding.ASCII.GetBytes("hello");
            LzoDecodeResult result = Decode(Concat(InitialLiteral(raw), new byte[] { 0x7F }), raw.Length);
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Diagnostics.Any(item => item.Code == PackedMapDiagnosticCode.BackendTrailingInput), Is.True);
        }

        [Test]
        public void CancellationIsStructured()
        {
            using (var source = new CancellationTokenSource())
            {
                source.Cancel();
                LzoDecodeResult result = Decode(new byte[] { 0x11, 0x00, 0x00 }, 0, source.Token);
                Assert.That(result.IsSuccess, Is.False);
                Assert.That(result.Diagnostics.Any(item => item.Code == PackedMapDiagnosticCode.BackendCancelled), Is.True);
            }
        }

        [Test]
        public void BackendIdentityAndProvenanceAreStable()
        {
            var provenance = new[] { new IniSourceProvenance("synthetic-lzo", new[] { LogicalContentPath.Parse("synthetic/packed") }) };
            LzoDecodeResult result = Decode(InitialLiteral(new byte[] { 1, 2, 3 }), 3, CancellationToken.None, provenance);
            AssertSuccess(result, 7, 3);
            Assert.That(result.BackendIdentity, Is.EqualTo(ManagedRawLzo1XDecodeBackend.Identity));
            Assert.That(result.ProducedOutput, Is.EqualTo(3));
            Assert.That(result.TerminatorSeen, Is.True);
            Assert.That(result.Provenance[0].SourceId, Is.EqualTo("synthetic-lzo"));
        }

        [Test]
        public void RepeatedDecodeIsDeterministic()
        {
            byte[] compressed = InitialLiteral(Encoding.ASCII.GetBytes("deterministic"));
            LzoDecodeResult first = Decode(compressed, 13);
            LzoDecodeResult second = Decode(compressed, 13);
            Assert.That(second.IsSuccess, Is.EqualTo(first.IsSuccess));
            Assert.That(second.ConsumedInput, Is.EqualTo(first.ConsumedInput));
            Assert.That(second.Bytes, Is.EqualTo(first.Bytes));
            Assert.That(second.Diagnostics.Select(item => item.Code), Is.EqualTo(first.Diagnostics.Select(item => item.Code)));
        }

        [Test]
        public void ExternalLzokayRepetitiveVectorDecodesExactly()
        {
            byte[] expected = Enumerable.Repeat((byte)'A', 256).ToArray();
            byte[] compressed =
            {
                0x12, 0x41, 0x20, 0xDE, 0x00, 0x00, 0x11, 0x00, 0x00
            };
            LzoDecodeResult result = Decode(compressed, expected.Length);
            AssertSuccess(result, compressed.Length, expected.Length);
            Assert.That(result.Bytes, Is.EqualTo(expected));
        }

        [Test]
        public void ExternalLzokayMixedVectorDecodesExactly()
        {
            byte[] expected = MixedOraclePayload();
            byte[] compressed =
            {
                0x19, 0x5A, 0x62, 0x63, 0x64, 0x65, 0x66, 0x67, 0x61, 0x3B,
                0x19, 0x00, 0x5A, 0x20, 0x01, 0x8B, 0x00, 0x63, 0x64, 0x5A,
                0x20, 0x01, 0x8B, 0x00, 0x65, 0x66, 0x5A, 0x20, 0x01, 0x8B,
                0x00, 0x67, 0x61, 0x5A, 0x20, 0x02, 0x4A, 0x02, 0x63, 0x5A,
                0x20, 0x02, 0x4A, 0x02, 0x65, 0x5A, 0x20, 0x02, 0x49, 0x02,
                0x67, 0x20, 0xDC, 0x08, 0x04, 0x11, 0x00, 0x00
            };
            LzoDecodeResult result = Decode(compressed, expected.Length);
            AssertSuccess(result, compressed.Length, expected.Length);
            Assert.That(result.Bytes, Is.EqualTo(expected));
        }

        [Test]
        public void PackedSectionPipelineUsesManagedBackend()
        {
            byte[] raw = Encoding.ASCII.GetBytes("pipeline");
            PackedSectionDecodeResult result = DecodePipeline(InitialLiteral(raw), raw.Length);
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.DecodedBytes, Is.EqualTo(raw));
            Assert.That(result.BlockOutputs, Has.Count.EqualTo(1));
        }

        [Test]
        public void IsoMapPack5AdapterPreservesRecordsThroughManagedBackend()
        {
            byte[] records = Record(12, 34, 0x12345678u, 7, 8, 9);
            byte[] compressed = InitialLiteral(records);
            var policy = new IsoMapPack5PackedReadPolicy(
                new PackedSectionDecodePolicy(
                    PackedIniFragmentOrderingPolicy.SourceOccurrenceOrder,
                    StrictBase64Policy.StandardAlphabetNoWhitespace,
                    ChunkSentinelPolicy.RejectAllZero,
                    PackedCodecKind.RawLzo1X));
            IsoMapPack5PackedReadResult result = new IsoMapPack5PackedSectionReader().Read(
                Occurrence(Convert.ToBase64String(Envelope(compressed, records.Length))), policy, new ManagedRawLzo1XDecodeBackend());
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Records.Records, Has.Count.EqualTo(1));
            Assert.That(result.Records.Records[0].GetRawBytesCopy(), Is.EqualTo(records));
        }

        private static LzoDecodeResult Decode(byte[] compressed, int expectedLength, CancellationToken cancellationToken = default(CancellationToken), IEnumerable<IniSourceProvenance> provenance = null)
        {
            IniSourceProvenance[] chain = (provenance ?? new[] { new IniSourceProvenance("synthetic", new[] { LogicalContentPath.Parse("synthetic/lzo") }) }).ToArray();
            var request = new LzoDecodeRequest(PackedCodecKind.RawLzo1X, compressed, expectedLength, 1_000_000, "synthetic-lzo", chain, cancellationToken);
            return new ManagedRawLzo1XDecodeBackend().Decode(request);
        }

        private static void AssertSuccess(LzoDecodeResult result, int consumed, int produced)
        {
            Assert.That(result.IsSuccess, Is.True, string.Join("; ", result.Diagnostics.Select(item => item.Message)));
            Assert.That(result.ConsumedInput, Is.EqualTo(consumed));
            Assert.That(result.ProducedOutput, Is.EqualTo(produced));
        }

        private static byte[] InitialLiteral(byte[] raw)
        {
            if (raw.Length < 1 || raw.Length > 238) throw new ArgumentOutOfRangeException(nameof(raw));
            return Concat(new[] { checked((byte)(raw.Length + 17)) }, raw, new byte[] { 0x11, 0x00, 0x00 });
        }

        private static byte[] Envelope(byte[] compressed, int outputLength)
        {
            return Concat(new byte[] { (byte)compressed.Length, (byte)(compressed.Length >> 8), (byte)outputLength, (byte)(outputLength >> 8) }, compressed);
        }

        private static PackedIniFragmentOccurrence[] Occurrence(string value)
        {
            return new[]
            {
                new PackedIniFragmentOccurrence(
                    "IsoMapPack5",
                    "1",
                    value,
                    0,
                    "synthetic",
                    1,
                    new IniSourceProvenance("synthetic", new[] { LogicalContentPath.Parse("synthetic/map.ini") }))
            };
        }

        private static PackedSectionDecodeResult DecodePipeline(byte[] compressed, int outputLength)
        {
            var policy = new PackedSectionDecodePolicy(
                PackedIniFragmentOrderingPolicy.SourceOccurrenceOrder,
                StrictBase64Policy.StandardAlphabetNoWhitespace,
                ChunkSentinelPolicy.RejectAllZero,
                PackedCodecKind.RawLzo1X);
            return new PackedSectionDecodePipeline().Decode(
                Occurrence(Convert.ToBase64String(Envelope(compressed, outputLength))),
                policy,
                new ManagedRawLzo1XDecodeBackend());
        }

        private static byte[] Record(ushort x, ushort y, uint tile, byte subTile, byte level, byte tail)
        {
            return new byte[] { (byte)x, (byte)(x >> 8), (byte)y, (byte)(y >> 8), (byte)tile, (byte)(tile >> 8), (byte)(tile >> 16), (byte)(tile >> 24), subTile, level, tail };
        }

        private static byte[] MixedOraclePayload()
        {
            var bytes = new byte[512];
            for (int index = 0; index < bytes.Length; index++)
                bytes[index] = index % 37 == 0 ? (byte)'Z' : (byte)('a' + index % 7);
            return bytes;
        }

        private static byte[] Concat(params byte[][] arrays)
        {
            int length = arrays.Sum(item => item.Length);
            byte[] result = new byte[length];
            int offset = 0;
            foreach (byte[] array in arrays)
            {
                Buffer.BlockCopy(array, 0, result, offset, array.Length);
                offset += array.Length;
            }
            return result;
        }
    }
}
