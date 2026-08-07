using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using RA2YR.Core.Binary;
using RA2YR.Core.Content;
using RA2YR.Core.Formats.Mix;
using RA2YR.Core.Formats.ShpTs;

namespace RA2YR.Tests.EditMode.Formats.ShpTs
{
    [TestFixture]
    public sealed class WestwoodShpTsReaderTests
    {
        [Test]
        public void MinimalCanonicalEmptyFrameParses()
        {
            ShpTsDocument document = ShpTsTestSupport.AssertParseSuccess(
                ShpTsTestSupport.Read(ShpTsSyntheticFixtureFactory.Build(
                    1, 1, ShpTsSyntheticFixtureFactory.Empty())));

            Assert.That(document.Header.FamilyMarkerRaw, Is.Zero);
            Assert.That(document.Header.FrameCountRaw, Is.EqualTo(1));
            Assert.That(document.Frames[0].IsCanonicalEmpty, Is.True);
        }

        [Test]
        public void ZeroFrameCountFailsClosed()
        {
            byte[] bytes = ShpTsSyntheticFixtureFactory.Build(0, 1, 1, 0);
            ShpTsTestSupport.AssertParseFailure(
                ShpTsTestSupport.Read(bytes),
                ShpTsDiagnosticCode.ZeroFrameCount);
        }

        [TestCase(0u, 0)]
        [TestCase(1u, 1)]
        [TestCase(3u, 3)]
        public void ConfirmedFlagsMapWithoutMasking(
            uint flags,
            int expected)
        {
            var frame = ShpTsSyntheticFixtureFactory.Raw(1, 1, flags, 7);
            if (flags == 3)
            {
                frame.Payload = ShpTsSyntheticFixtureFactory.Rle(1, 1, new byte[] { 7 }).Payload;
            }

            ShpTsDocument document = ShpTsTestSupport.AssertParseSuccess(
                ShpTsTestSupport.Read(ShpTsSyntheticFixtureFactory.Build(1, 1, frame)));
            Assert.That(document.Frames[0].CompressionKind,
                Is.EqualTo((ShpTsCompressionKind)expected));
            Assert.That(document.Frames[0].RawFlags, Is.EqualTo(flags));
        }

        [Test]
        public void Flags2IsPreservedAsSourceConflict()
        {
            ShpTsParseResult result = ShpTsTestSupport.Read(
                ShpTsSyntheticFixtureFactory.Build(
                    1, 1, ShpTsSyntheticFixtureFactory.Raw(1, 1, 2, 1)));
            ShpTsDocument document = ShpTsTestSupport.AssertParseSuccess(result);
            Assert.That(document.Frames[0].CompressionKind,
                Is.EqualTo(ShpTsCompressionKind.SourceConflictingFlags2));
            Assert.That(result.Diagnostics.Any(item =>
                item.Code == ShpTsDiagnosticCode.SourceConflictingFlags2), Is.True);
        }

        [TestCase(4u)]
        [TestCase(0x80000000u)]
        public void UnknownFlagsArePreservedAndDiagnosed(uint flags)
        {
            ShpTsParseResult result = ShpTsTestSupport.Read(
                ShpTsSyntheticFixtureFactory.Build(
                    1, 1, ShpTsSyntheticFixtureFactory.Raw(1, 1, flags, 1)));
            ShpTsDocument document = ShpTsTestSupport.AssertParseSuccess(result);
            Assert.That(document.Frames[0].RawFlags, Is.EqualTo(flags));
            Assert.That(document.Frames[0].CompressionKind,
                Is.EqualTo(ShpTsCompressionKind.UnknownFlags));
            Assert.That(result.Diagnostics.Any(item =>
                item.Code == ShpTsDiagnosticCode.UnknownFlags), Is.True);
        }

        [Test]
        public void NonZeroFamilyMarkerFailsWithoutTryingAnotherFamily()
        {
            byte[] bytes = ShpTsSyntheticFixtureFactory.Build(
                1, 1, 1, 1, ShpTsSyntheticFixtureFactory.Empty());
            ShpTsTestSupport.AssertParseFailure(
                ShpTsTestSupport.Read(bytes),
                ShpTsDiagnosticCode.InvalidFamilyMarker);
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(7)]
        public void TruncatedHeaderFails(int length)
        {
            ShpTsDiagnostic diagnostic = ShpTsTestSupport.AssertParseFailure(
                ShpTsTestSupport.Read(new byte[length]),
                ShpTsDiagnosticCode.UnexpectedEndOfInput);
            Assert.That(diagnostic.AbsoluteOffset, Is.LessThanOrEqualTo(length));
        }

        [TestCase(8)]
        [TestCase(31)]
        public void TruncatedDirectoryFailsBeforeReducingFrameCount(int length)
        {
            byte[] complete = ShpTsSyntheticFixtureFactory.Build(
                1, 1, ShpTsSyntheticFixtureFactory.Empty());
            byte[] truncated = complete.Take(length).ToArray();
            ShpTsDiagnostic diagnostic = ShpTsTestSupport.AssertParseFailure(
                ShpTsTestSupport.Read(truncated),
                ShpTsDiagnosticCode.UnexpectedEndOfInput);
            Assert.That(diagnostic.FieldOrSection, Is.EqualTo("shp-frame-directory"));
        }

        [Test]
        public void FrameCountBudgetFailsBeforeDescriptorAllocation()
        {
            byte[] bytes = ShpTsSyntheticFixtureFactory.Build(
                1, 1,
                ShpTsSyntheticFixtureFactory.Empty(),
                ShpTsSyntheticFixtureFactory.Empty());
            ShpTsTestSupport.AssertParseFailure(
                ShpTsTestSupport.Read(bytes, ShpTsTestSupport.Limits(maxFrameCount: 1)),
                ShpTsDiagnosticCode.FrameCountBudgetExceeded);
        }

        [TestCase((ushort)0, (ushort)1)]
        [TestCase((ushort)1, (ushort)0)]
        [TestCase((ushort)17, (ushort)1)]
        public void CanvasDimensionBudgetIsExplicit(ushort width, ushort height)
        {
            ShpTsTestSupport.AssertParseFailure(
                ShpTsTestSupport.Read(
                    ShpTsSyntheticFixtureFactory.Build(width, height,
                        ShpTsSyntheticFixtureFactory.Empty()),
                    ShpTsTestSupport.Limits(maxCanvasDimension: 16)),
                ShpTsDiagnosticCode.CanvasDimensionBudgetExceeded);
        }

        [Test]
        public void CanvasAreaBudgetUsesCheckedProduct()
        {
            ShpTsTestSupport.AssertParseFailure(
                ShpTsTestSupport.Read(
                    ShpTsSyntheticFixtureFactory.Build(8, 8,
                        ShpTsSyntheticFixtureFactory.Empty()),
                    ShpTsTestSupport.Limits(maxCanvasArea: 63)),
                ShpTsDiagnosticCode.CanvasAreaBudgetExceeded);
        }

        [Test]
        public void LocalFrameAreaBudgetFailsBeforeDecodeAllocation()
        {
            ShpTsTestSupport.AssertParseFailure(
                ShpTsTestSupport.Read(
                    ShpTsSyntheticFixtureFactory.Build(8, 8,
                        ShpTsSyntheticFixtureFactory.Raw(8, 8, 0, new byte[64])),
                    ShpTsTestSupport.Limits(maxLocalFrameArea: 63)),
                ShpTsDiagnosticCode.LocalFrameAreaBudgetExceeded);
        }

        [TestCase((ushort)0, (ushort)1, 0u)]
        [TestCase((ushort)1, (ushort)0, 0u)]
        [TestCase((ushort)0, (ushort)0, 32u)]
        [TestCase((ushort)1, (ushort)1, 0u)]
        public void PartialEmptyDescriptorsFail(ushort width, ushort height, uint offset)
        {
            var frame = new ShpTsSyntheticFixtureFactory.FrameSpec
            {
                Width = width,
                Height = height,
                DataOffset = offset,
                Payload = Array.Empty<byte>()
            };
            ShpTsTestSupport.AssertParseFailure(
                ShpTsTestSupport.Read(ShpTsSyntheticFixtureFactory.Build(2, 2, frame)),
                ShpTsDiagnosticCode.PartialEmptyFrame);
        }

        [Test]
        public void EmptyFrameCoordinatesArePreservedWithWarning()
        {
            ShpTsParseResult result = ShpTsTestSupport.Read(
                ShpTsSyntheticFixtureFactory.Build(
                    4, 4, ShpTsSyntheticFixtureFactory.Empty(3, 2)));
            ShpTsFrameDescriptor frame = ShpTsTestSupport.AssertParseSuccess(result).Frames[0];
            Assert.That(frame.XRaw, Is.EqualTo(3));
            Assert.That(frame.YRaw, Is.EqualTo(2));
            Assert.That(result.Diagnostics.Any(item =>
                item.Code == ShpTsDiagnosticCode.EmptyFrameCoordinatesNonZero), Is.True);
        }

        [TestCase((ushort)0x8000, (ushort)0)]
        [TestCase((ushort)0, (ushort)0x8000)]
        public void CoordinateHighBitsRemainRawAndUnresolved(ushort x, ushort y)
        {
            var frame = ShpTsSyntheticFixtureFactory.Raw(1, 1, 0, 7);
            frame.X = x;
            frame.Y = y;
            ShpTsParseResult result = ShpTsTestSupport.Read(
                ShpTsSyntheticFixtureFactory.Build(1, 1, frame));
            ShpTsFrameDescriptor descriptor = ShpTsTestSupport.AssertParseSuccess(result).Frames[0];
            Assert.That(descriptor.XRaw, Is.EqualTo(x));
            Assert.That(descriptor.YRaw, Is.EqualTo(y));
            Assert.That(result.Diagnostics.Any(item =>
                item.Code == ShpTsDiagnosticCode.CoordinateSignednessUnresolved), Is.True);
        }

        [Test]
        public void HalfOpenRectangleMayEndAtCanvasBoundary()
        {
            var frame = ShpTsSyntheticFixtureFactory.Raw(2, 2, 0, 1, 2, 3, 4);
            frame.X = 2;
            frame.Y = 2;
            ShpTsTestSupport.AssertParseSuccess(
                ShpTsTestSupport.Read(ShpTsSyntheticFixtureFactory.Build(4, 4, frame)));
        }

        [TestCase((ushort)3, (ushort)0)]
        [TestCase((ushort)0, (ushort)3)]
        public void UnsignedRectangleOutsideCanvasFails(ushort x, ushort y)
        {
            var frame = ShpTsSyntheticFixtureFactory.Raw(2, 2, 0, 1, 2, 3, 4);
            frame.X = x;
            frame.Y = y;
            ShpTsTestSupport.AssertParseFailure(
                ShpTsTestSupport.Read(ShpTsSyntheticFixtureFactory.Build(4, 4, frame)),
                ShpTsDiagnosticCode.FrameRectangleOutsideCanvas);
        }

        [Test]
        public void DataOffsetInsideDirectoryFails()
        {
            byte[] bytes = ShpTsSyntheticFixtureFactory.Build(
                1, 1, ShpTsSyntheticFixtureFactory.Raw(1, 1, 0, 7));
            WriteUInt32(bytes, 28, 16);
            ShpTsTestSupport.AssertParseFailure(
                ShpTsTestSupport.Read(bytes),
                ShpTsDiagnosticCode.DataOffsetInsideDirectory);
        }

        [Test]
        public void DataOffsetAtFileEndFails()
        {
            byte[] bytes = ShpTsSyntheticFixtureFactory.Build(
                1, 1, ShpTsSyntheticFixtureFactory.Raw(1, 1, 0, 7));
            WriteUInt32(bytes, 28, checked((uint)bytes.Length));
            ShpTsTestSupport.AssertParseFailure(
                ShpTsTestSupport.Read(bytes),
                ShpTsDiagnosticCode.DataOffsetOutsideInput);
        }

        [Test]
        public void NonZeroReservedFieldIsPreservedAndDiagnosed()
        {
            var frame = ShpTsSyntheticFixtureFactory.Raw(1, 1, 0, 7);
            frame.Reserved = 0x12345678;
            ShpTsParseResult result = ShpTsTestSupport.Read(
                ShpTsSyntheticFixtureFactory.Build(1, 1, frame));
            Assert.That(ShpTsTestSupport.AssertParseSuccess(result).Frames[0].ReservedRaw,
                Is.EqualTo(0x12345678));
            Assert.That(result.Diagnostics.Any(item =>
                item.Code == ShpTsDiagnosticCode.ReservedFieldNonZero), Is.True);
        }

        [Test]
        public void MisalignedDataOffsetIsWarningNotCorruption()
        {
            var frame = ShpTsSyntheticFixtureFactory.Raw(1, 1, 0, 7);
            frame.DataOffset = 33;
            ShpTsParseResult result = ShpTsTestSupport.Read(
                ShpTsSyntheticFixtureFactory.Build(1, 1, frame));
            ShpTsTestSupport.AssertParseSuccess(result);
            Assert.That(result.Diagnostics.Any(item =>
                item.Code == ShpTsDiagnosticCode.DataOffsetNotEightByteAligned), Is.True);
        }

        [Test]
        public void DuplicateOffsetsArePreservedWithoutDependencyInference()
        {
            var first = ShpTsSyntheticFixtureFactory.Raw(1, 1, 0, 7);
            var second = ShpTsSyntheticFixtureFactory.Raw(1, 1, 0, 7);
            first.DataOffset = 64;
            second.DataOffset = 64;
            ShpTsParseResult result = ShpTsTestSupport.Read(
                ShpTsSyntheticFixtureFactory.Build(1, 1, first, second));
            ShpTsDocument document = ShpTsTestSupport.AssertParseSuccess(result);
            Assert.That(document.Frames.Select(item => item.DataOffsetRaw),
                Is.EqualTo(new uint[] { 64, 64 }));
            Assert.That(result.Diagnostics.Count(item =>
                item.Code == ShpTsDiagnosticCode.DuplicateDataOffset), Is.EqualTo(2));
        }

        [Test]
        public void DescendingOffsetsArePreservedAndDiagnosed()
        {
            var first = ShpTsSyntheticFixtureFactory.Raw(1, 1, 0, 1);
            var second = ShpTsSyntheticFixtureFactory.Raw(1, 1, 0, 2);
            first.DataOffset = 80;
            second.DataOffset = 64;
            ShpTsParseResult result = ShpTsTestSupport.Read(
                ShpTsSyntheticFixtureFactory.Build(1, 1, first, second));
            ShpTsDocument document = ShpTsTestSupport.AssertParseSuccess(result);
            Assert.That(document.Frames.Select(item => item.DataOffsetRaw),
                Is.EqualTo(new uint[] { 80, 64 }));
            Assert.That(result.Diagnostics.Any(item =>
                item.Code == ShpTsDiagnosticCode.DescendingDataOffset), Is.True);
        }

        [Test]
        public void FrameColorRawAndDescriptorOrderAreImmutable()
        {
            var first = ShpTsSyntheticFixtureFactory.Raw(1, 1, 0, 1);
            first.FrameColor = new byte[] { 1, 2, 3, 4 };
            var second = ShpTsSyntheticFixtureFactory.Raw(1, 1, 1, 2);
            ShpTsDocument document = ShpTsTestSupport.AssertParseSuccess(
                ShpTsTestSupport.Read(ShpTsSyntheticFixtureFactory.Build(1, 1, first, second)));
            byte[] color = document.Frames[0].GetFrameColorRawCopy();
            color[0] = 99;
            Assert.That(document.Frames[0].GetFrameColorRawCopy(),
                Is.EqualTo(new byte[] { 1, 2, 3, 4 }));
            Assert.That(document.Frames.Select(item => item.RawFlags),
                Is.EqualTo(new uint[] { 0, 1 }));
            Assert.Throws<NotSupportedException>(() =>
                ((IList<ShpTsFrameDescriptor>)document.Frames).Clear());
        }

        [Test]
        public void AbsoluteOffsetsIncludeCallerWindowStart()
        {
            byte[] bytes = ShpTsSyntheticFixtureFactory.Build(
                1, 1, ShpTsSyntheticFixtureFactory.Raw(1, 1, 0, 7));
            ShpTsDocument document = ShpTsTestSupport.AssertParseSuccess(
                WestwoodShpTsReader.Read(
                    bytes,
                    ShpTsTestSupport.Source(),
                    ShpTsTestSupport.Provenance(),
                    absoluteStartOffset: 100));
            Assert.That(document.AbsoluteStartOffset, Is.EqualTo(100));
            Assert.That(document.Frames[0].DescriptorAbsoluteOffset, Is.EqualTo(108));
            Assert.That(document.Frames[0].DataAbsoluteOffset,
                Is.EqualTo(100 + document.Frames[0].DataOffsetRaw));
        }

        [Test]
        public void SourceIdComparisonIsOrdinal()
        {
            var source = new BinarySourceContext(
                "format.shp-ts-directory",
                "CaseSource",
                LogicalContentPath.Parse("sample.shp"));
            var provenance = new ShpTsSourceProvenance(
                "casesource",
                new[] { LogicalContentPath.Parse("sample.shp") });
            Assert.Throws<ArgumentException>(() => WestwoodShpTsReader.Read(
                ShpTsSyntheticFixtureFactory.Build(1, 1,
                    ShpTsSyntheticFixtureFactory.Empty()),
                source,
                provenance));
        }

        [Test]
        public void SeekableAndShortReadStreamsProduceSameDirectoryHash()
        {
            byte[] bytes = ShpTsSyntheticFixtureFactory.Build(
                2, 1, ShpTsSyntheticFixtureFactory.Raw(2, 1, 0, 1, 2));
            string memoryHash = ShpTsTestSupport.AssertParseSuccess(
                ShpTsTestSupport.Read(bytes)).CanonicalDirectoryModelSha256;
            using (var stream = new ShpTsShortReadMemoryStream(bytes, 1))
            {
                ShpTsDocument streamed = ShpTsTestSupport.AssertParseSuccess(
                    WestwoodShpTsReader.ReadSeekable(
                        stream,
                        ShpTsTestSupport.Source(),
                        ShpTsTestSupport.Provenance(),
                        leaveOpen: true));
                Assert.That(streamed.CanonicalDirectoryModelSha256, Is.EqualTo(memoryHash));
                Assert.That(stream.ReadCalls, Is.GreaterThan(1));
            }
        }

        [Test]
        public void MixEntryWindowProducesSameDirectoryHash()
        {
            byte[] bytes = ShpTsSyntheticFixtureFactory.Build(
                2, 1, ShpTsSyntheticFixtureFactory.Raw(2, 1, 0, 1, 2));
            string memoryHash = ShpTsTestSupport.AssertParseSuccess(
                ShpTsTestSupport.Read(bytes)).CanonicalDirectoryModelSha256;
            MixArchiveReadResult mix = ShpTsTestSupport.ReadSyntheticMix(bytes);
            Assert.That(mix.IsSuccess, Is.True);
            using (mix.Archive)
            {
                ShpTsDocument window = ShpTsTestSupport.AssertParseSuccess(
                    WestwoodShpTsReader.Read(
                        mix.Archive.Entries.Single().OpenPayloadWindow(),
                        ShpTsTestSupport.Source(),
                        ShpTsTestSupport.Provenance()));
                Assert.That(window.CanonicalDirectoryModelSha256, Is.EqualTo(memoryHash));
            }
        }

        [Test]
        public void InputBudgetFailsBeforeParsingHeader()
        {
            byte[] bytes = ShpTsSyntheticFixtureFactory.Build(
                1, 1, ShpTsSyntheticFixtureFactory.Empty());
            ShpTsTestSupport.AssertParseFailure(
                ShpTsTestSupport.Read(bytes,
                    ShpTsTestSupport.Limits(maxInputBytes: bytes.Length - 1)),
                ShpTsDiagnosticCode.InputBudgetExceeded);
        }

        [Test]
        public void DescriptorBudgetIsIndependentFromFrameCountBudget()
        {
            byte[] bytes = ShpTsSyntheticFixtureFactory.Build(
                1, 1,
                ShpTsSyntheticFixtureFactory.Empty(),
                ShpTsSyntheticFixtureFactory.Empty());
            ShpTsTestSupport.AssertParseFailure(
                ShpTsTestSupport.Read(bytes,
                    ShpTsTestSupport.Limits(maxFrameCount: 2, maxDescriptors: 1)),
                ShpTsDiagnosticCode.DescriptorBudgetExceeded);
        }

        [Test]
        public void AllocationBudgetFailsBeforeDescriptorModelCreation()
        {
            byte[] bytes = ShpTsSyntheticFixtureFactory.Build(
                1, 1, ShpTsSyntheticFixtureFactory.Empty());
            ShpTsTestSupport.AssertParseFailure(
                ShpTsTestSupport.Read(bytes,
                    ShpTsTestSupport.Limits(maxAllocatedBytes: 64)),
                ShpTsDiagnosticCode.AllocationBudgetExceeded);
        }

        [Test]
        public void DiagnosticBudgetFailsClosed()
        {
            var frame = ShpTsSyntheticFixtureFactory.Raw(1, 1, 2, 7);
            frame.Reserved = 1;
            frame.DataOffset = 33;
            ShpTsTestSupport.AssertParseFailure(
                ShpTsTestSupport.Read(
                    ShpTsSyntheticFixtureFactory.Build(1, 1, frame),
                    ShpTsTestSupport.Limits(maxDiagnostics: 1)),
                ShpTsDiagnosticCode.DiagnosticBudgetExceeded);
        }

        [Test]
        public void CoreModelDoesNotExposeDependencyFields()
        {
            string[] names = typeof(ShpTsFrameDescriptor).GetProperties()
                .Select(property => property.Name)
                .ToArray();
            Assert.That(names.Any(name => name.IndexOf(
                "Dependency", StringComparison.OrdinalIgnoreCase) >= 0), Is.False);
            Assert.That(names.Any(name => name.IndexOf(
                "Reference", StringComparison.OrdinalIgnoreCase) >= 0), Is.False);
        }

        [Test]
        public void CoreAssemblyHasNoUnityEngineOrSystemDrawingReference()
        {
            string[] names = typeof(ShpTsDocument).Assembly.GetReferencedAssemblies()
                .Select(assembly => assembly.Name)
                .ToArray();
            Assert.That(names, Does.Not.Contain("UnityEngine"));
            Assert.That(names, Does.Not.Contain("System.Drawing"));
        }

        private static void WriteUInt32(byte[] bytes, int offset, uint value)
        {
            bytes[offset] = (byte)value;
            bytes[offset + 1] = (byte)(value >> 8);
            bytes[offset + 2] = (byte)(value >> 16);
            bytes[offset + 3] = (byte)(value >> 24);
        }
    }
}
