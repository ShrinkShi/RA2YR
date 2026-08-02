using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using NUnit.Framework;
using RA2YR.Core.Binary;
using RA2YR.Core.Content;
using RA2YR.Core.Formats.Mix;
using RA2YR.Core.Formats.Pal;

namespace RA2YR.Tests.EditMode.Formats.Pal
{
    [TestFixture]
    public sealed class WestwoodPaletteReaderTests
    {
        [Test]
        public void AllZeroPaletteParsesAsOneRepeatedColor()
        {
            PaletteParseResult result = Read(new byte[WestwoodPalette.FileLength]);

            WestwoodPalette palette = AssertSuccess(result);
            Assert.That(palette.Colors, Has.Count.EqualTo(WestwoodPalette.ColorCount));
            Assert.That(palette.Colors.All(color => color == new PaletteColorRaw(0, 0, 0)),
                Is.True);
            Assert.That(palette.MinimumRawChannel, Is.Zero);
            Assert.That(palette.MaximumRawChannel, Is.Zero);
            Assert.That(palette.DistinctColorCount, Is.EqualTo(1));
        }

        [Test]
        public void MaximumLegalRawValuesRemainUnexpanded()
        {
            byte[] input = Enumerable.Repeat(
                    PaletteColorRaw.MaximumChannelValue,
                    WestwoodPalette.FileLength)
                .ToArray();

            WestwoodPalette palette = AssertSuccess(Read(input));

            Assert.That(
                palette.Colors.All(color => color == new PaletteColorRaw(63, 63, 63)),
                Is.True);
            Assert.That(palette.MinimumRawChannel, Is.EqualTo(63));
            Assert.That(palette.MaximumRawChannel, Is.EqualTo(63));
        }

        [Test]
        public void ColorIndexOrderIsPreservedForAll256Entries()
        {
            byte[] input = BuildPalette((index, channel) =>
                checked((byte)((index + (int)channel * 7) % 64)));

            WestwoodPalette palette = AssertSuccess(Read(input));

            for (int index = 0; index < WestwoodPalette.ColorCount; index++)
            {
                Assert.That(palette[index], Is.EqualTo(new PaletteColorRaw(
                    checked((byte)(index % 64)),
                    checked((byte)((index + 7) % 64)),
                    checked((byte)((index + 14) % 64)))));
            }
        }

        [Test]
        public void RecordsUseRgbByteOrder()
        {
            byte[] input = BuildPalette((index, channel) =>
                index == 17 ? checked((byte)(1 + (int)channel)) : (byte)0);

            WestwoodPalette palette = AssertSuccess(Read(input));

            Assert.That(palette[17], Is.EqualTo(new PaletteColorRaw(1, 2, 3)));
        }

        [Test]
        public void ChannelGradientIsPreservedWithoutDisplayConversion()
        {
            byte[] input = BuildPalette((index, channel) =>
                checked((byte)((index / 4 + (int)channel) % 64)));

            WestwoodPalette palette = AssertSuccess(Read(input));

            Assert.That(palette[0], Is.EqualTo(new PaletteColorRaw(0, 1, 2)));
            Assert.That(palette[252], Is.EqualTo(new PaletteColorRaw(63, 0, 1)));
        }

        [Test]
        public void DuplicateColorsAreNotMergedOrReordered()
        {
            byte[] input = BuildPalette((index, channel) =>
                checked((byte)(7 + (int)channel)));

            WestwoodPalette palette = AssertSuccess(Read(input));

            Assert.That(palette.Colors, Has.Count.EqualTo(256));
            Assert.That(palette.DistinctColorCount, Is.EqualTo(1));
            Assert.That(palette[0], Is.EqualTo(palette[255]));
        }

        [Test]
        public void FirstColorRetainsItsOriginalIndex()
        {
            byte[] input = BuildPalette((index, channel) =>
                index == 0 ? checked((byte)(9 + (int)channel)) : (byte)0);

            WestwoodPalette palette = AssertSuccess(Read(input));

            Assert.That(palette[0], Is.EqualTo(new PaletteColorRaw(9, 10, 11)));
            Assert.That(palette[1], Is.EqualTo(new PaletteColorRaw(0, 0, 0)));
        }

        [Test]
        public void LastColorRetainsItsOriginalIndex()
        {
            byte[] input = BuildPalette((index, channel) =>
                index == 255 ? checked((byte)(12 + (int)channel)) : (byte)0);

            WestwoodPalette palette = AssertSuccess(Read(input));

            Assert.That(palette[254], Is.EqualTo(new PaletteColorRaw(0, 0, 0)));
            Assert.That(palette[255], Is.EqualTo(new PaletteColorRaw(12, 13, 14)));
        }

        [Test]
        public void Exactly768BytesAreFullyConsumed()
        {
            PaletteParseResult result = Read(BuildPalette((index, channel) => 1));

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Diagnostics, Is.Empty);
        }

        [Test]
        public void OneByteShortFailsAtLastBlueChannel()
        {
            byte[] input = new byte[WestwoodPalette.FileLength - 1];

            PaletteDiagnostic diagnostic = AssertFailure(
                Read(input),
                PaletteDiagnosticCode.UnexpectedEndOfInput);

            Assert.That(diagnostic.ColorIndex, Is.EqualTo(255));
            Assert.That(diagnostic.Channel, Is.EqualTo(PaletteChannel.Blue));
            Assert.That(diagnostic.AbsoluteOffset, Is.EqualTo(767));
            Assert.That(diagnostic.RequestedLength, Is.EqualTo(1));
            Assert.That(diagnostic.RemainingLength, Is.Zero);
        }

        [Test]
        public void OneCompleteColorShortFailsAtLastRedChannel()
        {
            byte[] input = new byte[WestwoodPalette.FileLength - 3];

            PaletteDiagnostic diagnostic = AssertFailure(
                Read(input),
                PaletteDiagnosticCode.UnexpectedEndOfInput);

            Assert.That(diagnostic.ColorIndex, Is.EqualTo(255));
            Assert.That(diagnostic.Channel, Is.EqualTo(PaletteChannel.Red));
            Assert.That(diagnostic.AbsoluteOffset, Is.EqualTo(765));
        }

        [Test]
        public void OneTrailingByteIsRejectedByFullConsumptionPolicy()
        {
            byte[] input = new byte[WestwoodPalette.FileLength + 1];

            PaletteDiagnostic diagnostic = AssertFailure(
                Read(input),
                PaletteDiagnosticCode.UnexpectedTrailingData);

            Assert.That(diagnostic.BinaryCode, Is.EqualTo(BinaryDiagnosticCode.TrailingData));
            Assert.That(diagnostic.AbsoluteOffset, Is.EqualTo(768));
            Assert.That(diagnostic.RequestedLength, Is.EqualTo(1));
            Assert.That(diagnostic.RemainingLength, Is.EqualTo(1));
        }

        [Test]
        public void OneTrailingColorIsRejectedByFullConsumptionPolicy()
        {
            byte[] input = new byte[WestwoodPalette.FileLength + 3];

            PaletteDiagnostic diagnostic = AssertFailure(
                Read(input),
                PaletteDiagnosticCode.UnexpectedTrailingData);

            Assert.That(diagnostic.AbsoluteOffset, Is.EqualTo(768));
            Assert.That(diagnostic.RequestedLength, Is.EqualTo(3));
            Assert.That(diagnostic.RemainingLength, Is.EqualTo(3));
        }

        [Test]
        public void OutOfRangeChannelFailsAtExactIndexChannelAndOffset()
        {
            byte[] input = new byte[WestwoodPalette.FileLength];
            int offset = 42 * WestwoodPalette.ChannelsPerColor + 1;
            input[offset] = 64;

            PaletteDiagnostic diagnostic = AssertFailure(
                Read(input),
                PaletteDiagnosticCode.InvalidChannelValue);

            Assert.That(diagnostic.ColorIndex, Is.EqualTo(42));
            Assert.That(diagnostic.Channel, Is.EqualTo(PaletteChannel.Green));
            Assert.That(diagnostic.AbsoluteOffset, Is.EqualTo(offset));
            Assert.That(diagnostic.FieldOrSection, Is.EqualTo("palette-green-channel"));
        }

        [Test]
        public void ReadOnlyMemoryInputProducesCompleteModel()
        {
            byte[] input = BuildPalette((index, channel) =>
                checked((byte)((index + (int)channel) % 64)));

            PaletteParseResult result = WestwoodPaletteReader.Read(
                new ReadOnlyMemory<byte>(input),
                Source(),
                Provenance());

            AssertSuccess(result);
        }

        [Test]
        public void SeekableStreamInputProducesCompleteModel()
        {
            byte[] input = BuildPalette((index, channel) =>
                checked((byte)((index * 3 + (int)channel) % 64)));
            using (var stream = new MemoryStream(input, false))
            {
                PaletteParseResult result = WestwoodPaletteReader.ReadSeekable(
                    stream,
                    Source(),
                    Provenance(),
                    leaveOpen: true);

                AssertSuccess(result);
                Assert.That(stream.CanRead, Is.True);
            }
        }

        [Test]
        public void SeekableStreamCompletesAcrossRepeatedShortReads()
        {
            byte[] input = BuildPalette((index, channel) =>
                checked((byte)((index + (int)channel) % 64)));
            using (var stream = new ShortReadMemoryStream(input, 1))
            {
                AssertSuccess(WestwoodPaletteReader.ReadSeekable(
                    stream,
                    Source(),
                    Provenance(),
                    leaveOpen: true));
                Assert.That(stream.ReadCalls, Is.GreaterThan(1));
            }
        }

        [Test]
        public void NonSeekableStreamUsesCallerDeclaredBound()
        {
            byte[] input = BuildPalette((index, channel) =>
                checked((byte)((index + (int)channel * 2) % 64)));
            using (var stream = new NonSeekableShortReadStream(input, 5))
            {
                PaletteParseResult result = WestwoodPaletteReader.Read(
                    stream,
                    input.Length,
                    Source(),
                    Provenance(),
                    leaveOpen: true);

                AssertSuccess(result);
                Assert.That(stream.ReadCalls, Is.GreaterThan(1));
            }
        }

        [Test]
        public void MixEntryWindowProducesCompleteModel()
        {
            byte[] paletteBytes = BuildPalette((index, channel) =>
                checked((byte)((index + (int)channel) % 64)));
            MixArchiveReadResult mixResult = ReadSyntheticMix(paletteBytes);
            Assert.That(mixResult.IsSuccess, Is.True);
            using (mixResult.Archive)
            {
                PaletteParseResult result = WestwoodPaletteReader.Read(
                    mixResult.Archive.Entries.Single().OpenPayloadWindow(),
                    Source(),
                    Provenance());

                AssertSuccess(result);
            }
        }

        [Test]
        public void MemoryStreamAndMixWindowProduceEquivalentModels()
        {
            byte[] input = BuildPalette((index, channel) =>
                checked((byte)((index * 5 + (int)channel * 11) % 64)));
            WestwoodPalette memory = AssertSuccess(Read(input));
            WestwoodPalette stream;
            using (var inputStream = new MemoryStream(input, false))
            {
                stream = AssertSuccess(WestwoodPaletteReader.ReadSeekable(
                    inputStream,
                    Source(),
                    Provenance(),
                    leaveOpen: true));
            }

            MixArchiveReadResult mixResult = ReadSyntheticMix(input);
            Assert.That(mixResult.IsSuccess, Is.True);
            using (mixResult.Archive)
            {
                WestwoodPalette window = AssertSuccess(WestwoodPaletteReader.Read(
                    mixResult.Archive.Entries.Single().OpenPayloadWindow(),
                    Source(),
                    Provenance()));

                Assert.That(stream.Colors, Is.EqualTo(memory.Colors));
                Assert.That(window.Colors, Is.EqualTo(memory.Colors));
                Assert.That(stream.CanonicalModelSha256,
                    Is.EqualTo(memory.CanonicalModelSha256));
                Assert.That(window.CanonicalModelSha256,
                    Is.EqualTo(memory.CanonicalModelSha256));
            }
        }

        [Test]
        public void ParsedModelDoesNotAliasInputOrExposeMutableColorStorage()
        {
            byte[] input = BuildPalette((index, channel) =>
                index == 0 ? checked((byte)(1 + (int)channel)) : (byte)0);
            WestwoodPalette palette = AssertSuccess(Read(input));

            input[0] = 63;
            input[1] = 63;
            input[2] = 63;

            Assert.That(palette[0], Is.EqualTo(new PaletteColorRaw(1, 2, 3)));
            var list = (IList<PaletteColorRaw>)palette.Colors;
            Assert.Throws<NotSupportedException>(() =>
                list[0] = new PaletteColorRaw(4, 5, 6));
        }

        [Test]
        public void DisplayConversionNeverOverwritesParsedRawValues()
        {
            byte[] input = BuildPalette((index, channel) =>
                index == 8 ? checked((byte)(11 + (int)channel)) : (byte)0);
            WestwoodPalette palette = AssertSuccess(Read(input));
            PaletteColorRaw before = palette[8];

            PaletteDisplayConversion.ConvertColor(
                before,
                PaletteDisplayConversionStrategy.ScaleToFullRangeRounded);

            Assert.That(palette[8], Is.EqualTo(before));
        }

        [Test]
        public void InputBudgetFailsBeforeAnyStreamRead()
        {
            byte[] input = new byte[WestwoodPalette.FileLength];
            using (var stream = new CountingMemoryStream(input))
            {
                PaletteParseResult result = WestwoodPaletteReader.Read(
                    stream,
                    input.Length,
                    Source(),
                    Provenance(),
                    Limits(maxInputBytes: WestwoodPalette.FileLength - 1),
                    leaveOpen: true);

                AssertFailure(result, PaletteDiagnosticCode.InputBudgetExceeded);
                Assert.That(stream.ReadCalls, Is.Zero);
            }
        }

        [Test]
        public void PerReadBudgetFailsIndependentlyFromEndOfInput()
        {
            PaletteParseResult result = Read(
                new byte[WestwoodPalette.FileLength],
                Limits(maxSingleReadBytes: 0));

            PaletteDiagnostic diagnostic = AssertFailure(
                result,
                PaletteDiagnosticCode.ReadBudgetExceeded);
            Assert.That(diagnostic.ColorIndex, Is.Zero);
            Assert.That(diagnostic.Channel, Is.EqualTo(PaletteChannel.Red));
        }

        [Test]
        public void RecordBudgetFailsBeforeReadingColorData()
        {
            PaletteParseResult result = Read(
                new byte[WestwoodPalette.FileLength],
                Limits(maxRecords: WestwoodPalette.ColorCount - 1));

            PaletteDiagnostic diagnostic = AssertFailure(
                result,
                PaletteDiagnosticCode.RecordBudgetExceeded);
            Assert.That(diagnostic.ColorIndex, Is.EqualTo(-1));
            Assert.That(diagnostic.AbsoluteOffset, Is.Zero);
        }

        [Test]
        public void ModelAllocationBudgetFailsBeforeModelAllocation()
        {
            PaletteParseResult result = Read(
                new byte[WestwoodPalette.FileLength],
                Limits(maxAllocatedBytes: 4095));

            AssertFailure(result, PaletteDiagnosticCode.AllocationBudgetExceeded);
        }

        [Test]
        public void StreamSnapshotAndModelShareOneAllocationBudget()
        {
            byte[] input = new byte[WestwoodPalette.FileLength];
            using (var stream = new MemoryStream(input, false))
            {
                PaletteParseResult result = WestwoodPaletteReader.ReadSeekable(
                    stream,
                    Source(),
                    Provenance(),
                    Limits(maxAllocatedBytes: 4096),
                    leaveOpen: true);

                AssertFailure(result, PaletteDiagnosticCode.AllocationBudgetExceeded);
            }
        }

        [Test]
        public void StreamIOExceptionProducesStructuredSanitizedFailure()
        {
            using (var stream = new ThrowingReadStream())
            {
                PaletteDiagnostic diagnostic = AssertFailure(
                    WestwoodPaletteReader.Read(
                        stream,
                        WestwoodPalette.FileLength,
                        Source(),
                        Provenance(),
                        leaveOpen: true),
                    PaletteDiagnosticCode.ReadFailure);

                Assert.That(diagnostic.BinaryCode, Is.EqualTo(BinaryDiagnosticCode.ReadFailure));
                Assert.That(diagnostic.Message, Does.Not.Contain("private"));
                Assert.That(diagnostic.Message, Does.Not.Contain(":\\"));
            }
        }

        [Test]
        public void EveryTruncatedLengthFailsWithoutPartialPalette()
        {
            for (int length = 0; length < WestwoodPalette.FileLength; length++)
            {
                PaletteParseResult result = Read(new byte[length]);
                Assert.That(result.IsSuccess, Is.False, "length=" + length);
                Assert.That(result.Palette, Is.Null, "length=" + length);
                Assert.That(result.Diagnostics, Has.Count.EqualTo(1), "length=" + length);
            }
        }

        [Test]
        public void InputAboveBoundFailsWithoutAttemptingUnboundedAllocation()
        {
            byte[] input = new byte[4097];

            PaletteDiagnostic diagnostic = AssertFailure(
                Read(input),
                PaletteDiagnosticCode.InputBudgetExceeded);

            Assert.That(diagnostic.RequestedLength, Is.EqualTo(4097));
        }

        [Test]
        public void MixWindowDiagnosticUsesPayloadAbsoluteOffset()
        {
            byte[] input = new byte[WestwoodPalette.FileLength];
            input[4] = 64;
            MixArchiveReadResult mixResult = ReadSyntheticMix(input);
            Assert.That(mixResult.IsSuccess, Is.True);
            using (mixResult.Archive)
            {
                long payloadStart = mixResult.Archive.Entries.Single().PayloadAbsoluteOffset;
                PaletteDiagnostic diagnostic = AssertFailure(
                    WestwoodPaletteReader.Read(
                        mixResult.Archive.Entries.Single().OpenPayloadWindow(),
                        Source(),
                        Provenance()),
                    PaletteDiagnosticCode.InvalidChannelValue);

                Assert.That(diagnostic.AbsoluteOffset, Is.EqualTo(payloadStart + 4));
                Assert.That(diagnostic.ColorIndex, Is.EqualTo(1));
                Assert.That(diagnostic.Channel, Is.EqualTo(PaletteChannel.Green));
            }
        }

        [Test]
        public void DiagnosticsRetainOnlyLogicalProvenance()
        {
            byte[] input = new byte[WestwoodPalette.FileLength - 1];

            PaletteDiagnostic diagnostic = AssertFailure(
                Read(input),
                PaletteDiagnosticCode.UnexpectedEndOfInput);

            Assert.That(diagnostic.Source.LogicalPath.Value,
                Is.EqualTo("synthetic/palette.pal"));
            Assert.That(
                diagnostic.Provenance.LogicalChain.Select(path => path.Value),
                Is.EqualTo(new[] { "synthetic.mix", "palette.pal" }));
            Assert.That(diagnostic.Provenance.SourceId, Is.EqualTo("synthetic-source"));
        }

        [Test]
        public void ProvenanceCannotClaimAnotherLogicalSource()
        {
            var mismatched = new PaletteSourceProvenance(
                "another-source",
                new[] { LogicalContentPath.Parse("palette.pal") });

            Assert.Throws<ArgumentException>(() => WestwoodPaletteReader.Read(
                new byte[WestwoodPalette.FileLength],
                Source(),
                mismatched));
        }

        [Test]
        public void ParsedPaletteRetainsLogicalSourceAndProvenance()
        {
            BinarySourceContext source = Source();
            PaletteSourceProvenance provenance = Provenance();

            WestwoodPalette palette = AssertSuccess(WestwoodPaletteReader.Read(
                new byte[WestwoodPalette.FileLength],
                source,
                provenance));

            Assert.That(palette.Source, Is.SameAs(source));
            Assert.That(palette.Provenance, Is.SameAs(provenance));
        }

        [Test]
        public void CanonicalModelHashUsesLockedDomainCountIndexAndRgbSchema()
        {
            WestwoodPalette palette = AssertSuccess(
                Read(new byte[WestwoodPalette.FileLength]));

            Assert.That(
                palette.CanonicalModelSha256,
                Is.EqualTo("8e2ae4257e4ff70c69cd1f6c6bd2324e527808f80ef09d96dc115c0c27d5c548"));
            Assert.That(
                palette.CanonicalModelSha256,
                Is.Not.EqualTo(Sha256(new byte[WestwoodPalette.FileLength])));
        }

        [Test]
        public void CanonicalModelHashChangesWhenIndexedColorsChange()
        {
            byte[] first = BuildPalette((index, channel) =>
                index == 1 ? checked((byte)(1 + (int)channel)) : (byte)0);
            byte[] second = BuildPalette((index, channel) =>
                index == 2 ? checked((byte)(1 + (int)channel)) : (byte)0);

            Assert.That(
                AssertSuccess(Read(first)).CanonicalModelSha256,
                Is.Not.EqualTo(AssertSuccess(Read(second)).CanonicalModelSha256));
        }

        [Test]
        public void ParseResultCannotBeConstructedAsPublicSuccess()
        {
            Assert.That(typeof(PaletteParseResult).GetConstructors(), Is.Empty);
            Assert.That(typeof(WestwoodPalette).GetConstructors(), Is.Empty);
        }

        [Test]
        public void StreamOwnershipFollowsLeaveOpenPolicy()
        {
            byte[] input = new byte[WestwoodPalette.FileLength];
            var owned = new TrackingMemoryStream(input);
            AssertSuccess(WestwoodPaletteReader.ReadSeekable(
                owned,
                Source(),
                Provenance(),
                leaveOpen: false));
            Assert.That(owned.WasDisposed, Is.True);

            var borrowed = new TrackingMemoryStream(input);
            AssertSuccess(WestwoodPaletteReader.ReadSeekable(
                borrowed,
                Source(),
                Provenance(),
                leaveOpen: true));
            Assert.That(borrowed.WasDisposed, Is.False);
            borrowed.Dispose();
        }

        private static PaletteParseResult Read(
            byte[] input,
            PaletteReadLimits limits = null)
        {
            return WestwoodPaletteReader.Read(
                input,
                Source(),
                Provenance(),
                limits);
        }

        private static WestwoodPalette AssertSuccess(PaletteParseResult result)
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Diagnostics, Is.Empty);
            Assert.That(result.Palette, Is.Not.Null);
            return result.Palette;
        }

        private static PaletteDiagnostic AssertFailure(
            PaletteParseResult result,
            PaletteDiagnosticCode expectedCode)
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Palette, Is.Null);
            Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(result.Diagnostics[0].Code, Is.EqualTo(expectedCode));
            return result.Diagnostics[0];
        }

        private static BinarySourceContext Source()
        {
            return new BinarySourceContext(
                "format.pal",
                "synthetic-source",
                LogicalContentPath.Parse("synthetic/palette.pal"));
        }

        private static PaletteSourceProvenance Provenance()
        {
            return new PaletteSourceProvenance(
                "synthetic-source",
                new[]
                {
                    LogicalContentPath.Parse("synthetic.mix"),
                    LogicalContentPath.Parse("palette.pal")
                });
        }

        private static PaletteReadLimits Limits(
            long maxInputBytes = 4096,
            long maxSingleReadBytes = 4096,
            long maxAllocatedBytes = 16 * 1024,
            long maxRecords = WestwoodPalette.ColorCount)
        {
            return new PaletteReadLimits(
                maxInputBytes,
                maxSingleReadBytes,
                maxAllocatedBytes,
                maxRecords);
        }

        private static byte[] BuildPalette(Func<int, PaletteChannel, byte> valueFactory)
        {
            var bytes = new byte[WestwoodPalette.FileLength];
            for (int index = 0; index < WestwoodPalette.ColorCount; index++)
            {
                int offset = index * WestwoodPalette.ChannelsPerColor;
                bytes[offset] = valueFactory(index, PaletteChannel.Red);
                bytes[offset + 1] = valueFactory(index, PaletteChannel.Green);
                bytes[offset + 2] = valueFactory(index, PaletteChannel.Blue);
            }

            return bytes;
        }

        private static MixArchiveReadResult ReadSyntheticMix(byte[] payload)
        {
            byte[] archive = new byte[checked(18 + payload.Length)];
            WriteUInt16(archive, 0, 1);
            WriteUInt32(archive, 2, checked((uint)payload.Length));
            WriteUInt32(archive, 6, MixFileId.ComputeCandidateId("palette.pal").Value);
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
                    1024 * 1024,
                    16,
                    1024,
                    1024 * 1024,
                    1024 * 1024,
                    32,
                    8));
        }

        private static void WriteUInt16(byte[] bytes, int offset, ushort value)
        {
            bytes[offset] = checked((byte)(value & 0xff));
            bytes[offset + 1] = checked((byte)((value >> 8) & 0xff));
        }

        private static void WriteUInt32(byte[] bytes, int offset, uint value)
        {
            bytes[offset] = checked((byte)(value & 0xff));
            bytes[offset + 1] = checked((byte)((value >> 8) & 0xff));
            bytes[offset + 2] = checked((byte)((value >> 16) & 0xff));
            bytes[offset + 3] = checked((byte)((value >> 24) & 0xff));
        }

        private static string Sha256(byte[] bytes)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                return BitConverter.ToString(sha256.ComputeHash(bytes))
                    .Replace("-", string.Empty)
                    .ToLowerInvariant();
            }
        }

        private class CountingMemoryStream : MemoryStream
        {
            public CountingMemoryStream(byte[] bytes)
                : base(bytes, false)
            {
            }

            public int ReadCalls { get; protected set; }

            public override int Read(byte[] buffer, int offset, int count)
            {
                ReadCalls++;
                return base.Read(buffer, offset, count);
            }
        }

        private sealed class ShortReadMemoryStream : CountingMemoryStream
        {
            private readonly int maximumChunk;

            public ShortReadMemoryStream(byte[] bytes, int maximumChunk)
                : base(bytes)
            {
                this.maximumChunk = maximumChunk;
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                return base.Read(buffer, offset, Math.Min(count, maximumChunk));
            }
        }

        private sealed class NonSeekableShortReadStream : Stream
        {
            private readonly byte[] bytes;
            private readonly int maximumChunk;
            private int position;

            public NonSeekableShortReadStream(byte[] bytes, int maximumChunk)
            {
                this.bytes = bytes;
                this.maximumChunk = maximumChunk;
            }

            public int ReadCalls { get; private set; }

            public override bool CanRead => true;

            public override bool CanSeek => false;

            public override bool CanWrite => false;

            public override long Length => throw new NotSupportedException();

            public override long Position
            {
                get => throw new NotSupportedException();
                set => throw new NotSupportedException();
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                ReadCalls++;
                int available = bytes.Length - position;
                int actual = Math.Min(Math.Min(count, maximumChunk), available);
                if (actual == 0)
                {
                    return 0;
                }

                Buffer.BlockCopy(bytes, position, buffer, offset, actual);
                position += actual;
                return actual;
            }

            public override void Flush()
            {
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                throw new NotSupportedException();
            }

            public override void SetLength(long value)
            {
                throw new NotSupportedException();
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                throw new NotSupportedException();
            }
        }

        private sealed class ThrowingReadStream : Stream
        {
            public override bool CanRead => true;

            public override bool CanSeek => false;

            public override bool CanWrite => false;

            public override long Length => throw new NotSupportedException();

            public override long Position
            {
                get => throw new NotSupportedException();
                set => throw new NotSupportedException();
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                throw new IOException("private host path must remain hidden");
            }

            public override void Flush()
            {
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                throw new NotSupportedException();
            }

            public override void SetLength(long value)
            {
                throw new NotSupportedException();
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                throw new NotSupportedException();
            }
        }

        private sealed class TrackingMemoryStream : MemoryStream
        {
            public TrackingMemoryStream(byte[] bytes)
                : base(bytes, false)
            {
            }

            public bool WasDisposed { get; private set; }

            protected override void Dispose(bool disposing)
            {
                WasDisposed = true;
                base.Dispose(disposing);
            }
        }
    }
}
