using System;
using System.IO;
using NUnit.Framework;
using RA2YR.Core.Formats.VxlHva;

namespace RA2YR.Tests.EditMode.Formats.VxlHva
{
    public sealed class LegacyVisualImportTests
    {
        [Test] public void IndexedImagePreservesIndicesAndDefensiveCopy()
        {
            byte[] source = { 1, 2, 3, 4 }; var image = new IndexedImageDescriptor(2, 2, source, new PaletteBindingDescriptor("iso", PaletteConversionProfile.Unresolved), null, null); source[0] = 99; byte[] copy = image.GetIndicesCopy(); copy[1] = 88;
            Assert.AreEqual(1, image.Indices.Span[0]); Assert.AreEqual(2, image.Indices.Span[1]);
        }

        [Test] public void PaletteBindingKeepsExplicitConversionProfile()
        { var binding = new PaletteBindingDescriptor("unit", PaletteConversionProfile.XccScaleToFullRangeFloor); Assert.AreEqual("unit", binding.LogicalPaletteId); Assert.AreEqual(PaletteConversionProfile.XccScaleToFullRangeFloor, binding.ConversionProfile); }

        [Test] public void TeamRemapPreservesReversedRawRange()
        { var remap = new TeamRemapDescriptor(31, 16); Assert.IsTrue(remap.IsReversed); Assert.AreEqual(31, remap.StartRaw); }

        [Test] public void VxlZeroSectionDocumentIsStructurallyReadable()
        { VxlReadResult result = WestwoodVxlReader.Read(BuildVxl(0, 0, 0, null)); Assert.IsTrue(result.IsSuccess); Assert.AreEqual(0, result.Document.Sections.Count); Assert.IsTrue(result.Diagnostics.Count > 0); }

        [Test] public void VxlHeaderRetainsRawPaletteAndRemapBytes()
        { byte[] bytes = BuildVxl(0, 0, 0, null); bytes[32] = 16; bytes[33] = 31; bytes[34] = 42; VxlReadResult result = WestwoodVxlReader.Read(bytes); Assert.IsTrue(result.IsSuccess); Assert.AreEqual(16, result.Document.Header.StartPaletteRemapRaw); Assert.AreEqual(42, result.Document.Header.PaletteRaw[0]); }

        [Test] public void VxlSingleSparseColumnParsesRawVoxel()
        { byte[] input = BuildVxl(1, 1, 1, null); int body = 830; int tailer = body + 13; Assert.AreEqual(0, input[body]); Assert.AreEqual(4, input[body + 4]); Assert.AreEqual(0, input[body + 8]); Assert.AreEqual(1, input[body + 9]); Assert.AreEqual(8, Read32(input, tailer + 8)); VxlReadResult result = WestwoodVxlReader.Read(input); Assert.IsTrue(result.IsSuccess, Describe(result.Diagnostics)); Assert.AreEqual(1, result.Document.Sections[0].Columns.Count); Assert.AreEqual(1, result.Document.Sections[0].Columns[0].Chunks[0].Voxels.Count); Assert.AreEqual(7, result.Document.Sections[0].Columns[0].Chunks[0].Voxels[0].ColorIndex); }

        [Test] public void VxlAcceptsExactThreeByteTrailingEmptySpanCommand()
        { byte[] input = BuildVxl(1, 1, 1, new byte[] { 0, 0, 0, 0, 2, 0, 0, 0, 2, 0, 0 }); int tailer = 802 + 28 + 11; input[tailer + 90] = 2; VxlReadResult result = WestwoodVxlReader.Read(input); Assert.IsTrue(result.IsSuccess, Describe(result.Diagnostics)); Assert.AreEqual(1, result.Document.Sections[0].Columns[0].Chunks.Count); Assert.AreEqual(2, result.Document.Sections[0].Columns[0].Chunks[0].Skip); Assert.AreEqual(0, result.Document.Sections[0].Columns[0].Chunks[0].Voxels.Count); }

        [Test] public void VxlEmptyColumnRetainsMinusOneSentinels()
        { VxlReadResult result = WestwoodVxlReader.Read(BuildVxl(1, 1, 1, new byte[] { 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0 })); Assert.IsTrue(result.IsSuccess); Assert.IsTrue(result.Document.Sections[0].Columns[0].IsEmpty); }

        [Test] public void VxlRejectsMismatchedSectionCounts()
        { byte[] bytes = BuildVxl(0, 1, 0, null); VxlReadResult result = WestwoodVxlReader.Read(bytes); Assert.IsFalse(result.IsSuccess); Assert.IsTrue(Has(result, LegacyVisualDiagnosticCode.SectionCountMismatch)); }

        [Test] public void VxlRejectsTruncatedHeader()
        { VxlReadResult result = WestwoodVxlReader.Read(new byte[801]); Assert.IsFalse(result.IsSuccess); Assert.IsTrue(Has(result, LegacyVisualDiagnosticCode.TruncatedHeader)); }

        [Test] public void VxlRejectsTrailingBytes()
        { byte[] bytes = BuildVxl(0, 0, 0, null); Array.Resize(ref bytes, bytes.Length + 1); VxlReadResult result = WestwoodVxlReader.Read(bytes); Assert.IsFalse(result.IsSuccess); Assert.IsTrue(Has(result, LegacyVisualDiagnosticCode.UnexpectedTrailingData)); }

        [Test] public void VxlRejectsSpanCommandWithoutProgress()
        { byte[] bytes = BuildVxl(1, 1, 1, new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }); VxlReadResult result = WestwoodVxlReader.Read(bytes); Assert.IsFalse(result.IsSuccess); Assert.IsTrue(Has(result, LegacyVisualDiagnosticCode.NoProgress)); }

        [Test] public void VxlStreamPathMatchesMemoryPath()
        { byte[] bytes = BuildVxl(0, 0, 0, null); using (var stream = new MemoryStream(bytes)) { VxlReadResult memory = WestwoodVxlReader.Read(bytes); VxlReadResult streamed = WestwoodVxlReader.Read(stream, bytes.Length, leaveOpen: true); Assert.AreEqual(memory.IsSuccess, streamed.IsSuccess); Assert.AreEqual(memory.Document.CanonicalSha256, streamed.Document.CanonicalSha256); } }

        [Test] public void HvaMinimumDocumentParsesRawRecord()
        { HvaReadResult result = WestwoodHvaReader.Read(BuildHva(1, 1)); Assert.IsTrue(result.IsSuccess); Assert.AreEqual(1, result.Document.Header.FrameCountRaw); Assert.AreEqual(1, result.Document.Transforms.Count); Assert.AreEqual(0x3f800000u, result.Document.Transforms[0].RawBits[0]); }

        [Test] public void HvaAllowsArbitraryRawLabel()
        { byte[] bytes = BuildHva(1, 1); bytes[0] = (byte)'N'; bytes[1] = (byte)'O'; bytes[2] = 0; HvaReadResult result = WestwoodHvaReader.Read(bytes); Assert.IsTrue(result.IsSuccess); Assert.AreEqual((byte)'N', result.Document.Header.LabelRaw[0]); }

        [Test] public void HvaFrameMajorAndSectionMajorRemainExplicit()
        { HvaDocumentRaw document = WestwoodHvaReader.Read(BuildHva(2, 2)).Document; Assert.AreEqual(0, document.GetCandidate(0, 0, HvaTransformRecordOrder.FrameMajor).RecordOrdinal); Assert.AreEqual(1, document.GetCandidate(0, 1, HvaTransformRecordOrder.FrameMajor).RecordOrdinal); Assert.AreEqual(2, document.GetCandidate(0, 1, HvaTransformRecordOrder.SectionMajor).RecordOrdinal); }

        [Test] public void HvaRejectsMissingExplicitOrder()
        { HvaDocumentRaw document = WestwoodHvaReader.Read(BuildHva(1, 1)).Document; Assert.Throws<ArgumentException>(() => document.GetCandidate(0, 0, HvaTransformRecordOrder.Unresolved)); }

        [Test] public void HvaRejectsTruncatedTransform()
        { byte[] bytes = BuildHva(1, 1); Array.Resize(ref bytes, bytes.Length - 1); HvaReadResult result = WestwoodHvaReader.Read(bytes); Assert.IsFalse(result.IsSuccess); Assert.IsTrue(Has(result, LegacyVisualDiagnosticCode.TruncatedRecord)); }

        [Test] public void HvaRejectsTrailingData()
        { byte[] bytes = BuildHva(1, 1); Array.Resize(ref bytes, bytes.Length + 2); HvaReadResult result = WestwoodHvaReader.Read(bytes); Assert.IsFalse(result.IsSuccess); Assert.IsTrue(Has(result, LegacyVisualDiagnosticCode.HvaTrailingData)); }

        [Test] public void HvaZeroCountsAreSafeAndExact()
        { HvaReadResult result = WestwoodHvaReader.Read(BuildHva(0, 0)); Assert.IsTrue(result.IsSuccess); Assert.AreEqual(0, result.Document.Transforms.Count); }

        [Test] public void BinderProducesUniqueBinding()
        { VxlDocumentRaw vxl = WestwoodVxlReader.Read(BuildNamedVxl("BODY")).Document; HvaDocumentRaw hva = WestwoodHvaReader.Read(BuildNamedHva("BODY")).Document; VxlHvaBindingResult result = VxlHvaBinder.Bind(vxl, hva); Assert.IsTrue(result.IsSuccess); Assert.AreEqual(1, result.Bindings.Count); }

        [Test] public void BinderRetainsUnboundSections()
        { VxlDocumentRaw vxl = WestwoodVxlReader.Read(BuildNamedVxl("BODY")).Document; HvaDocumentRaw hva = WestwoodHvaReader.Read(BuildNamedHva("OTHER")).Document; VxlHvaBindingResult result = VxlHvaBinder.Bind(vxl, hva); Assert.IsFalse(result.IsSuccess); Assert.AreEqual(VxlHvaBindingStatus.Incomplete, result.Status); Assert.AreEqual(1, result.UnboundVxlSections.Count); }

        [Test] public void BinderRejectsDuplicateHvaNamesAsAmbiguous()
        { VxlDocumentRaw vxl = WestwoodVxlReader.Read(BuildNamedVxl("BODY")).Document; HvaDocumentRaw hva = WestwoodHvaReader.Read(BuildNamedHva("BODY", "BODY")).Document; VxlHvaBindingResult result = VxlHvaBinder.Bind(vxl, hva); Assert.IsFalse(result.IsSuccess); Assert.AreEqual(VxlHvaBindingStatus.Ambiguous, result.Status); Assert.IsTrue(Has(result, LegacyVisualDiagnosticCode.AmbiguousBinding)); }

        [Test] public void DiagnosticBudgetZeroStillFailsClosed()
        { VxlReadResult result = WestwoodVxlReader.Read(new byte[1], new VxlHvaReadLimits(maxDiagnostics: 0)); Assert.IsFalse(result.IsSuccess); Assert.IsTrue(result.Execution.HasFatalError); Assert.Greater(result.Execution.SuppressedDiagnosticCount, 0); }

        private static bool Has(VxlReadResult result, LegacyVisualDiagnosticCode code) { for (int i = 0; i < result.Diagnostics.Count; i++) if (result.Diagnostics[i].Code == code) return true; return result.Execution.HasFatalError; }
        private static bool Has(HvaReadResult result, LegacyVisualDiagnosticCode code) { for (int i = 0; i < result.Diagnostics.Count; i++) if (result.Diagnostics[i].Code == code) return true; return result.Execution.HasFatalError; }
        private static bool Has(VxlHvaBindingResult result, LegacyVisualDiagnosticCode code) { for (int i = 0; i < result.Diagnostics.Count; i++) if (result.Diagnostics[i].Code == code) return true; return result.Execution.HasFatalError; }
        private static string Describe(System.Collections.Generic.IReadOnlyList<LegacyVisualDiagnostic> diagnostics) { var text = string.Empty; for (int i = 0; i < diagnostics.Count; i++) text += diagnostics[i].Code + ":" + diagnostics[i].Message + ";"; return text; }

        private static byte[] BuildVxl(int sections, int headerCount, int tailerCount, byte[] bodyOverride)
        {
            byte[] body = bodyOverride ?? new byte[0]; if (sections == 1 && bodyOverride == null) body = new byte[13];
            if (bodyOverride == null && tailerCount > 0) { body = new byte[] { 0, 0, 0, 0, 4, 0, 0, 0, 0, 1, 7, 2, 1 }; }
            int bodySize = body.Length; byte[] bytes = new byte[802 + headerCount * 28 + bodySize + tailerCount * 92]; WriteAscii(bytes, 0, "Voxel Animation"); Write32(bytes, 16, 1); Write32(bytes, 20, (uint)headerCount); Write32(bytes, 24, (uint)tailerCount); Write32(bytes, 28, (uint)bodySize); if (headerCount > 0) WriteAscii(bytes, 802, "BODY"); if (tailerCount > 0) { int t = 802 + headerCount * 28 + bodySize; Write32(bytes, t, 0); Write32(bytes, t + 4, 4); Write32(bytes, t + 8, 8); Write32(bytes, t + 12, 0x3f800000); bytes[t + 88] = 1; bytes[t + 89] = 1; bytes[t + 90] = 1; bytes[t + 91] = 2; }
            if (sections == 1 && bodyOverride != null) { int bodyStart = 802 + headerCount * 28; Buffer.BlockCopy(bodyOverride, 0, bytes, bodyStart, bodyOverride.Length); }
            if (sections == 1 && bodyOverride == null && tailerCount > 0) { int bodyStart = 802 + headerCount * 28; bytes[bodyStart] = 0; bytes[bodyStart + 4] = 4; bytes[bodyStart + 8] = 0; bytes[bodyStart + 9] = 1; bytes[bodyStart + 10] = 7; bytes[bodyStart + 11] = 2; bytes[bodyStart + 12] = 1; }
            return bytes;
        }

        private static byte[] BuildNamedVxl(params string[] names)
        { byte[] bytes = BuildVxl(names.Length, names.Length, names.Length, names.Length == 1 ? null : new byte[names.Length * 13]); for (int i = 0; i < names.Length; i++) WriteAscii(bytes, 802 + i * 28, names[i]); return bytes; }
        private static byte[] BuildHva(uint frames, uint sections) { byte[] bytes = new byte[checked(24 + (int)sections * 16 + checked((int)(frames * sections * 48)))]; Write32(bytes, 16, frames); Write32(bytes, 20, sections); for (int i = 0; i < frames * sections; i++) Write32(bytes, 24 + (int)(sections * 16) + i * 48, 0x3f800000u + (uint)i); return bytes; }
        private static byte[] BuildNamedHva(params string[] names) { byte[] bytes = BuildHva(1, (uint)names.Length); for (int i = 0; i < names.Length; i++) WriteAscii(bytes, 24 + i * 16, names[i]); return bytes; }
        private static void Write32(byte[] bytes, int offset, uint value) { bytes[offset] = (byte)value; bytes[offset + 1] = (byte)(value >> 8); bytes[offset + 2] = (byte)(value >> 16); bytes[offset + 3] = (byte)(value >> 24); }
        private static uint Read32(byte[] bytes, int offset) { return (uint)(bytes[offset] | bytes[offset + 1] << 8 | bytes[offset + 2] << 16 | bytes[offset + 3] << 24); }
        private static void WriteAscii(byte[] bytes, int offset, string value) { for (int i = 0; i < value.Length && i < 16; i++) bytes[offset + i] = (byte)value[i]; }
    }
}
