using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RA2YR.Core.Binary;
using RA2YR.Core.Binary.Seekable;

namespace RA2YR.Core.Formats.VxlHva
{
    internal static class WestwoodVxlReader
    {
        private const int HeaderLength = 802;
        private const int SectionHeaderLength = 28;
        private const int SectionTailerLength = 92;

        internal static VxlReadResult Read(ReadOnlyMemory<byte> input, VxlHvaReadLimits limits = null, long absoluteStartOffset = 0)
        {
            VxlHvaReadLimits effective = limits ?? VxlHvaReadLimits.Default;
            if (input.Length > effective.MaxInputBytes)
                return Failure<VxlReadResult>(new LegacyVisualDiagnostic(LegacyVisualDiagnosticCode.InputBudgetExceeded, LegacyVisualDiagnosticSeverity.Error, absoluteStartOffset, "vxl-input", "The VXL input exceeds its byte budget."), effective);
            return Parse(input.ToArray(), effective, absoluteStartOffset);
        }

        internal static VxlReadResult Read(Stream stream, long inputLength, VxlHvaReadLimits limits = null, bool leaveOpen = false, long absoluteStartOffset = 0)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));
            VxlHvaReadLimits effective = limits ?? VxlHvaReadLimits.Default;
            byte[] bytes;
            LegacyVisualDiagnostic diagnostic;
            if (!TryReadStream(stream, inputLength, effective.MaxInputBytes, out bytes, out diagnostic))
                return Failure<VxlReadResult>(diagnostic, effective);
            try { return Parse(bytes, effective, absoluteStartOffset); }
            finally { if (!leaveOpen) stream.Dispose(); }
        }

        internal static VxlReadResult Read(ReadOnlyDataWindow window, VxlHvaReadLimits limits = null)
        {
            if (window == null) throw new ArgumentNullException(nameof(window));
            VxlHvaReadLimits effective = limits ?? VxlHvaReadLimits.Default;
            if (window.Length > effective.MaxInputBytes || window.Length > int.MaxValue)
                return Failure<VxlReadResult>(new LegacyVisualDiagnostic(LegacyVisualDiagnosticCode.InputBudgetExceeded, LegacyVisualDiagnosticSeverity.Error, window.AbsoluteStartOffset, "vxl-window", "The bounded VXL window exceeds its byte budget."), effective);
            try
            {
                byte[] bytes = new byte[(int)window.Length];
                if (bytes.Length != 0) window.ReadExactly(0, bytes, 0, bytes.Length, "vxl-window");
                return Parse(bytes, effective, window.AbsoluteStartOffset);
            }
            catch (Exception exception) when (!(exception is OutOfMemoryException))
            {
                return Failure<VxlReadResult>(new LegacyVisualDiagnostic(LegacyVisualDiagnosticCode.TruncatedRecord, LegacyVisualDiagnosticSeverity.Error, window.AbsoluteStartOffset, "vxl-window", exception.Message), effective);
            }
        }

        private static VxlReadResult Parse(byte[] bytes, VxlHvaReadLimits limits, long absoluteStart)
        {
            var collector = new LegacyVisualDiagnosticCollector((int)Math.Min(limits.MaxDiagnostics, int.MaxValue));
            if (bytes.Length < HeaderLength)
            {
                collector.Add(new LegacyVisualDiagnostic(LegacyVisualDiagnosticCode.TruncatedHeader, LegacyVisualDiagnosticSeverity.Error, absoluteStart + bytes.Length, "vxl-header", "The VXL header is truncated."));
                return new VxlReadResult(null, collector.Diagnostics, collector.Complete());
            }
            var cursor = new Cursor(bytes, absoluteStart, collector);
            byte[] fileType = cursor.Bytes(16, "file-type");
            string fileTypeCandidate = System.Text.Encoding.ASCII.GetString(fileType).TrimEnd('\0');
            if (!fileTypeCandidate.StartsWith("Voxel Animation", StringComparison.Ordinal))
                collector.Add(new LegacyVisualDiagnostic(LegacyVisualDiagnosticCode.InvalidMagic, LegacyVisualDiagnosticSeverity.Error, absoluteStart, "vxl-header", "The VXL file-type field is not a Voxel Animation identifier."));
            uint paletteCount = cursor.UInt32("palette-count");
            uint headerCount = cursor.UInt32("section-header-count");
            uint tailerCount = cursor.UInt32("section-tailer-count");
            uint bodySize = cursor.UInt32("body-size");
            byte remapStart = cursor.Byte("remap-start");
            byte remapEnd = cursor.Byte("remap-end");
            byte[] palette = cursor.Bytes(768, "palette");
            if (!cursor.IsHealthy) return new VxlReadResult(null, collector.Diagnostics, collector.Complete());
            if (headerCount > limits.MaxSections || tailerCount > limits.MaxSections)
                collector.Add(new LegacyVisualDiagnostic(LegacyVisualDiagnosticCode.HvaCountBudgetExceeded, LegacyVisualDiagnosticSeverity.Error, cursor.AbsoluteOffset, "vxl-sections", "VXL section count exceeds its budget."));
            if (headerCount != tailerCount)
                collector.Add(new LegacyVisualDiagnostic(LegacyVisualDiagnosticCode.SectionCountMismatch, LegacyVisualDiagnosticSeverity.Error, cursor.AbsoluteOffset, "vxl-sections", "VXL section-header and tailer counts differ."));
            VxlSectionHeaderRaw[] headers = new VxlSectionHeaderRaw[(int)Math.Min(headerCount, (uint)limits.MaxSections)];
            for (int i = 0; i < headers.Length; i++)
            {
                byte[] name = cursor.Bytes(16, "section-name");
                headers[i] = new VxlSectionHeaderRaw(name, cursor.UInt32("section-number"), cursor.UInt32("unknown1"), cursor.UInt32("unknown2"), i);
            }
            for (int i = 0; i < headers.Length; i++)
                for (int j = i + 1; j < headers.Length; j++)
                    if (string.Equals(headers[i].NameCandidate, headers[j].NameCandidate, StringComparison.Ordinal))
                        collector.Add(new LegacyVisualDiagnostic(LegacyVisualDiagnosticCode.DuplicateSectionName, LegacyVisualDiagnosticSeverity.Warning, absoluteStart + HeaderLength + i * SectionHeaderLength, "vxl-section-name", "Duplicate VXL section names are retained for later binding.", j));
            if (!cursor.IsHealthy) return new VxlReadResult(null, collector.Diagnostics, collector.Complete());
            long bodyStart;
            long bodyEnd;
            long tailersEnd;
            try
            {
                bodyStart = checked(HeaderLength + (long)headerCount * SectionHeaderLength);
                bodyEnd = checked(bodyStart + bodySize);
                tailersEnd = checked(bodyEnd + (long)tailerCount * SectionTailerLength);
            }
            catch (OverflowException)
            {
                collector.Add(new LegacyVisualDiagnostic(LegacyVisualDiagnosticCode.ArithmeticOverflow, LegacyVisualDiagnosticSeverity.Error, absoluteStart + cursor.Position, "vxl-layout", "VXL layout arithmetic overflowed."));
                return new VxlReadResult(null, collector.Diagnostics, collector.Complete());
            }
            if (bodySize > limits.MaxBodyBytes || bodyEnd > bytes.Length || tailersEnd > bytes.Length)
            {
                collector.Add(new LegacyVisualDiagnostic(LegacyVisualDiagnosticCode.TruncatedRecord, LegacyVisualDiagnosticSeverity.Error, absoluteStart + bytes.Length, "vxl-layout", "VXL body or section tailers are truncated."));
                return new VxlReadResult(null, collector.Diagnostics, collector.Complete());
            }
            if (tailersEnd != bytes.Length)
                collector.Add(new LegacyVisualDiagnostic(LegacyVisualDiagnosticCode.UnexpectedTrailingData, LegacyVisualDiagnosticSeverity.Error, absoluteStart + tailersEnd, "vxl-trailing", "VXL input has bytes after the declared tailers."));
            VxlSectionTailerRaw[] tailers = new VxlSectionTailerRaw[(int)Math.Min(tailerCount, (uint)limits.MaxSections)];
            cursor.Position = (int)bodyEnd;
            for (int i = 0; i < tailers.Length; i++)
            {
                uint start = cursor.UInt32("span-start"); uint end = cursor.UInt32("span-end"); uint data = cursor.UInt32("span-data"); uint scale = cursor.UInt32("scale");
                uint[] transform = new uint[12]; for (int j = 0; j < transform.Length; j++) transform[j] = cursor.UInt32("transform");
                uint[] min = new uint[3]; for (int j = 0; j < min.Length; j++) min[j] = cursor.UInt32("min-bound");
                uint[] max = new uint[3]; for (int j = 0; j < max.Length; j++) max[j] = cursor.UInt32("max-bound");
                tailers[i] = new VxlSectionTailerRaw(start, end, data, scale, transform, min, max, cursor.Byte("size-x"), cursor.Byte("size-y"), cursor.Byte("size-z"), cursor.Byte("normal-type"), i);
                if (tailers[i].NormalTypeRaw != 2 && tailers[i].NormalTypeRaw != 4)
                    collector.Add(new LegacyVisualDiagnostic(LegacyVisualDiagnosticCode.UnknownNormalMode, LegacyVisualDiagnosticSeverity.Warning, absoluteStart + cursor.Position - 1, "vxl-normal-mode", "The normal-table selector is retained but not resolved by Core.", i));
            }
            if (!cursor.IsHealthy) return new VxlReadResult(null, collector.Diagnostics, collector.Complete());
            if (headerCount == 0) collector.Add(new LegacyVisualDiagnostic(LegacyVisualDiagnosticCode.ZeroSectionCountUnconfirmed, LegacyVisualDiagnosticSeverity.Warning, absoluteStart + 20, "vxl-sections", "A zero-section VXL is structurally readable but stock legality is unconfirmed."));
            if (headerCount != tailerCount || headers.Length != tailers.Length) return new VxlReadResult(null, collector.Diagnostics, collector.Complete());
            var sections = new List<VxlSectionRaw>(headers.Length);
            for (int i = 0; i < headers.Length; i++)
            {
                VxlSectionTailerRaw tailer = tailers[i];
                long columnCount;
                try { columnCount = checked((long)tailer.SizeXRaw * tailer.SizeYRaw); }
                catch (OverflowException) { collector.Add(new LegacyVisualDiagnostic(LegacyVisualDiagnosticCode.ArithmeticOverflow, LegacyVisualDiagnosticSeverity.Error, absoluteStart + bodyEnd, "vxl-columns", "Column count overflowed.")); continue; }
                if (columnCount > limits.MaxColumns) { collector.Add(new LegacyVisualDiagnostic(LegacyVisualDiagnosticCode.SpanVoxelBudgetExceeded, LegacyVisualDiagnosticSeverity.Error, absoluteStart + bodyEnd, "vxl-columns", "Column count exceeds its budget.", i)); continue; }
                var columns = new List<VxlColumnRaw>((int)columnCount);
                for (int index = 0; index < columnCount; index++)
                {
                    int x = tailer.SizeXRaw == 0 ? 0 : index % tailer.SizeXRaw;
                    int y = tailer.SizeXRaw == 0 ? 0 : index / tailer.SizeXRaw;
                    long startPos = bodyStart + tailer.SpanStartOffsetRaw + (long)index * 4;
                    long endPos = bodyStart + tailer.SpanEndOffsetRaw + (long)index * 4;
                    int start = ReadInt32(bytes, startPos, bodyStart, bodyEnd, collector, "span-start-entry", i);
                    int end = ReadInt32(bytes, endPos, bodyStart, bodyEnd, collector, "span-end-entry", i);
                    if (start == -1 && end == -1) { columns.Add(new VxlColumnRaw(index, x, y, start, end, null)); continue; }
                    if ((start < 0) != (end < 0)) { collector.Add(new LegacyVisualDiagnostic(LegacyVisualDiagnosticCode.InconsistentEmptyColumn, LegacyVisualDiagnosticSeverity.Error, absoluteStart + startPos, "vxl-column", "Only a paired -1/-1 span directory entry denotes an empty column.", index)); continue; }
                    if (start < 0 || end < start) { collector.Add(new LegacyVisualDiagnostic(LegacyVisualDiagnosticCode.InvalidSpanRange, LegacyVisualDiagnosticSeverity.Error, absoluteStart + startPos, "vxl-column", "The span range is negative or reversed.", index)); continue; }
                    long dataStart = bodyStart + (long)tailer.SpanDataOffsetRaw + start;
                    long dataEnd = bodyStart + (long)tailer.SpanDataOffsetRaw + end;
                    if (dataStart < bodyStart || dataEnd >= bodyEnd || dataEnd < dataStart) { collector.Add(new LegacyVisualDiagnostic(LegacyVisualDiagnosticCode.InvalidSpanRange, LegacyVisualDiagnosticSeverity.Error, absoluteStart + dataStart, "vxl-column", "The span range lies outside the VXL body.", index)); continue; }
                    var chunks = new List<VxlSpanChunkRaw>(); long position = dataStart; int z = 0;
                    while (position <= dataEnd)
                    {
                        if (dataEnd - position < 2) { collector.Add(new LegacyVisualDiagnostic(LegacyVisualDiagnosticCode.SpanDataTruncated, LegacyVisualDiagnosticSeverity.Error, absoluteStart + position, "vxl-span", "A span command is truncated.", index)); break; }
                        byte skip = bytes[positionIndex(position, bytes.Length)]; byte count = bytes[positionIndex(position + 1, bytes.Length)]; position += 2;
                        if (skip == 0 && count == 0) { collector.Add(new LegacyVisualDiagnostic(LegacyVisualDiagnosticCode.NoProgress, LegacyVisualDiagnosticSeverity.Error, absoluteStart + position - 2, "vxl-span", "A zero-length span command makes no progress.", index)); break; }
                        long required = checked((long)count * 2 + 1);
                        if (dataEnd - position + 1 < required) { collector.Add(new LegacyVisualDiagnostic(LegacyVisualDiagnosticCode.SpanDataTruncated, LegacyVisualDiagnosticSeverity.Error, absoluteStart + position, "vxl-span", "Voxel records exceed the inclusive span range.", index)); break; }
                        var voxels = new List<VxlVoxelRaw>(count);
                        for (int v = 0; v < count; v++) voxels.Add(new VxlVoxelRaw(bytes[positionIndex(position++, bytes.Length)], bytes[positionIndex(position++, bytes.Length)]));
                        byte duplicate = bytes[positionIndex(position++, bytes.Length)];
                        if (duplicate != count) collector.Add(new LegacyVisualDiagnostic(LegacyVisualDiagnosticCode.InvalidDuplicateCount, LegacyVisualDiagnosticSeverity.Error, absoluteStart + position - 1, "vxl-span", "The duplicate voxel count differs from the leading count.", index));
                        long nextZ; try { nextZ = checked((long)z + skip + count); } catch (OverflowException) { collector.Add(new LegacyVisualDiagnostic(LegacyVisualDiagnosticCode.ArithmeticOverflow, LegacyVisualDiagnosticSeverity.Error, absoluteStart + position, "vxl-span", "Column Z arithmetic overflowed.", index)); break; }
                        if (nextZ > tailer.SizeZRaw) { collector.Add(new LegacyVisualDiagnostic(LegacyVisualDiagnosticCode.SpanCommandOverflow, LegacyVisualDiagnosticSeverity.Error, absoluteStart + position, "vxl-span", "A span command exceeds the logical Z dimension.", index)); break; }
                        chunks.Add(new VxlSpanChunkRaw(skip, count, voxels, duplicate)); z = (int)nextZ;
                    }
                    if (z != tailer.SizeZRaw) collector.Add(new LegacyVisualDiagnostic(LegacyVisualDiagnosticCode.SpanCommandOverflow, LegacyVisualDiagnosticSeverity.Error, absoluteStart + dataEnd, "vxl-span", "The column does not consume its declared logical Z extent.", index));
                    columns.Add(new VxlColumnRaw(index, x, y, start, end, chunks));
                }
                sections.Add(new VxlSectionRaw(headers[i], tailer, columns));
            }
            LegacyVisualExecutionState execution = collector.Complete();
            VxlDocumentRaw document = execution.IsSuccess ? new VxlDocumentRaw(new VxlHeaderRaw(fileType, paletteCount, headerCount, tailerCount, bodySize, remapStart, remapEnd, palette), sections, LegacyVisualHash.Sha256(bytes)) : null;
            return new VxlReadResult(document, collector.Diagnostics, execution);
        }

        private static int ReadInt32(byte[] bytes, long position, long bodyStart, long bodyEnd, LegacyVisualDiagnosticCollector collector, string stage, int ordinal)
        {
            if (position < bodyStart || position + 4 > bodyEnd || position < 0 || position + 4 > bytes.Length)
            { collector.Add(new LegacyVisualDiagnostic(LegacyVisualDiagnosticCode.InvalidSpanDirectory, LegacyVisualDiagnosticSeverity.Error, position, stage, "Span directory entry lies outside the VXL body.", ordinal)); return -2; }
            int index = positionIndex(position, bytes.Length);
            return bytes[index] | (bytes[index + 1] << 8) | (bytes[index + 2] << 16) | (bytes[index + 3] << 24);
        }

        private static int positionIndex(long position, int length)
        { return checked((int)position); }

        private static VxlReadResult Failure<T>(LegacyVisualDiagnostic diagnostic, VxlHvaReadLimits limits) where T : class
        {
            var collector = new LegacyVisualDiagnosticCollector((int)Math.Min(limits.MaxDiagnostics, int.MaxValue)); collector.Add(diagnostic); return (VxlReadResult)(object)new VxlReadResult(null, collector.Diagnostics, collector.Complete());
        }

        private static bool TryReadStream(Stream stream, long length, long max, out byte[] bytes, out LegacyVisualDiagnostic diagnostic)
        {
            bytes = null; diagnostic = null;
            if (length < 0 || length > max || length > int.MaxValue) { diagnostic = new LegacyVisualDiagnostic(LegacyVisualDiagnosticCode.InputBudgetExceeded, LegacyVisualDiagnosticSeverity.Error, 0, "vxl-stream", "The bounded stream length is invalid or exceeds its budget."); return false; }
            bytes = new byte[(int)length]; int offset = 0;
            while (offset < bytes.Length)
            {
                int read = stream.Read(bytes, offset, bytes.Length - offset);
                if (read <= 0) { diagnostic = new LegacyVisualDiagnostic(LegacyVisualDiagnosticCode.TruncatedRecord, LegacyVisualDiagnosticSeverity.Error, offset, "vxl-stream", "The stream ended before the bounded window was read."); bytes = null; return false; }
                offset += read;
            }
            return true;
        }
    }

    internal static class WestwoodHvaReader
    {
        private const int HeaderLength = 24;
        internal static HvaReadResult Read(ReadOnlyMemory<byte> input, VxlHvaReadLimits limits = null, long absoluteStartOffset = 0)
        {
            VxlHvaReadLimits effective = limits ?? VxlHvaReadLimits.Default;
            if (input.Length > effective.MaxInputBytes) return Failure(new LegacyVisualDiagnostic(LegacyVisualDiagnosticCode.InputBudgetExceeded, LegacyVisualDiagnosticSeverity.Error, absoluteStartOffset, "hva-input", "The HVA input exceeds its byte budget."), effective);
            return Parse(input.ToArray(), effective, absoluteStartOffset);
        }
        internal static HvaReadResult Read(Stream stream, long inputLength, VxlHvaReadLimits limits = null, bool leaveOpen = false, long absoluteStartOffset = 0)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream)); VxlHvaReadLimits effective = limits ?? VxlHvaReadLimits.Default; byte[] bytes; LegacyVisualDiagnostic diagnostic;
            if (!TryReadStream(stream, inputLength, effective.MaxInputBytes, out bytes, out diagnostic)) return Failure(diagnostic, effective);
            try { return Parse(bytes, effective, absoluteStartOffset); } finally { if (!leaveOpen) stream.Dispose(); }
        }
        internal static HvaReadResult Read(ReadOnlyDataWindow window, VxlHvaReadLimits limits = null)
        {
            if (window == null) throw new ArgumentNullException(nameof(window)); VxlHvaReadLimits effective = limits ?? VxlHvaReadLimits.Default;
            if (window.Length > effective.MaxInputBytes || window.Length > int.MaxValue) return Failure(new LegacyVisualDiagnostic(LegacyVisualDiagnosticCode.InputBudgetExceeded, LegacyVisualDiagnosticSeverity.Error, window.AbsoluteStartOffset, "hva-window", "The bounded HVA window exceeds its budget."), effective);
            byte[] bytes = new byte[(int)window.Length]; if (bytes.Length != 0) window.ReadExactly(0, bytes, 0, bytes.Length, "hva-window"); return Parse(bytes, effective, window.AbsoluteStartOffset);
        }
        private static HvaReadResult Parse(byte[] bytes, VxlHvaReadLimits limits, long absoluteStart)
        {
            var collector = new LegacyVisualDiagnosticCollector((int)Math.Min(limits.MaxDiagnostics, int.MaxValue));
            if (bytes.Length < HeaderLength) { collector.Add(new LegacyVisualDiagnostic(LegacyVisualDiagnosticCode.TruncatedHeader, LegacyVisualDiagnosticSeverity.Error, absoluteStart + bytes.Length, "hva-header", "The HVA header is truncated.")); return new HvaReadResult(null, collector.Diagnostics, collector.Complete()); }
            var c = new Cursor(bytes, absoluteStart, collector); byte[] label = c.Bytes(16, "label"); uint frames = c.UInt32("frame-count"); uint sections = c.UInt32("section-count");
            if (frames > limits.MaxSections || sections > limits.MaxSections) collector.Add(new LegacyVisualDiagnostic(LegacyVisualDiagnosticCode.HvaCountBudgetExceeded, LegacyVisualDiagnosticSeverity.Error, c.AbsoluteOffset, "hva-counts", "HVA frame or section count exceeds its budget."));
            long nameBytes, transformBytes, expected;
            try { nameBytes = checked((long)sections * 16); transformBytes = checked((long)frames * sections * 48); expected = checked(HeaderLength + nameBytes + transformBytes); }
            catch (OverflowException) { collector.Add(new LegacyVisualDiagnostic(LegacyVisualDiagnosticCode.ArithmeticOverflow, LegacyVisualDiagnosticSeverity.Error, c.AbsoluteOffset, "hva-layout", "HVA length arithmetic overflowed.")); return new HvaReadResult(null, collector.Diagnostics, collector.Complete()); }
            if (expected > bytes.Length) { collector.Add(new LegacyVisualDiagnostic(LegacyVisualDiagnosticCode.TruncatedRecord, LegacyVisualDiagnosticSeverity.Error, absoluteStart + bytes.Length, "hva-layout", "HVA names or transforms are truncated.")); return new HvaReadResult(null, collector.Diagnostics, collector.Complete()); }
            var names = new HvaSectionNameRaw[(int)Math.Min(sections, (uint)limits.MaxSections)]; for (int i = 0; i < names.Length; i++) names[i] = new HvaSectionNameRaw(c.Bytes(16, "section-name"), i);
            for (int i = 0; i < names.Length; i++)
                for (int j = i + 1; j < names.Length; j++)
                {
                    if (string.Equals(names[i].NameCandidate, names[j].NameCandidate, StringComparison.Ordinal))
                        collector.Add(new LegacyVisualDiagnostic(LegacyVisualDiagnosticCode.DuplicateHvaSectionName, LegacyVisualDiagnosticSeverity.Warning, absoluteStart + HeaderLength + i * 16, "hva-section-name", "Duplicate HVA section names are retained for binding diagnostics.", j));
                    else if (string.Equals(names[i].NameCandidate, names[j].NameCandidate, StringComparison.OrdinalIgnoreCase))
                        collector.Add(new LegacyVisualDiagnostic(LegacyVisualDiagnosticCode.CaseOnlySectionNameConflict, LegacyVisualDiagnosticSeverity.Warning, absoluteStart + HeaderLength + i * 16, "hva-section-name", "HVA section names differ only by case.", j));
                }
            var transforms = new HvaRawTransform3x4[(int)Math.Min(frames > int.MaxValue ? 0 : frames * sections, (uint)limits.MaxSections * (uint)limits.MaxSections)];
            long transformCount = (long)frames * sections;
            if (transformCount > int.MaxValue || transformCount > (long)limits.MaxSections * limits.MaxSections) collector.Add(new LegacyVisualDiagnostic(LegacyVisualDiagnosticCode.HvaCountBudgetExceeded, LegacyVisualDiagnosticSeverity.Error, c.AbsoluteOffset, "hva-transforms", "HVA transform count exceeds its budget."));
            else
            {
                transforms = new HvaRawTransform3x4[(int)transformCount];
                for (int i = 0; i < transforms.Length; i++) { uint[] bits = new uint[12]; for (int j = 0; j < bits.Length; j++) bits[j] = c.UInt32("transform"); transforms[i] = new HvaRawTransform3x4(bits, i); }
            }
            if (expected != bytes.Length) collector.Add(new LegacyVisualDiagnostic(LegacyVisualDiagnosticCode.HvaTrailingData, LegacyVisualDiagnosticSeverity.Error, absoluteStart + expected, "hva-trailing", "HVA input has bytes after the declared transform records."));
            var execution = collector.Complete(); HvaDocumentRaw document = execution.IsSuccess ? new HvaDocumentRaw(new HvaHeaderRaw(label, frames, sections), names, transforms, LegacyVisualHash.Sha256(bytes)) : null;
            return new HvaReadResult(document, collector.Diagnostics, execution);
        }
        private static HvaReadResult Failure(LegacyVisualDiagnostic diagnostic, VxlHvaReadLimits limits) { var c = new LegacyVisualDiagnosticCollector((int)Math.Min(limits.MaxDiagnostics, int.MaxValue)); c.Add(diagnostic); return new HvaReadResult(null, c.Diagnostics, c.Complete()); }
        private static bool TryReadStream(Stream stream, long length, long max, out byte[] bytes, out LegacyVisualDiagnostic diagnostic) { return WestwoodVxlReaderExtensions.TryReadStreamForHva(stream, length, max, out bytes, out diagnostic); }
    }

    internal static class VxlHvaBinder
    {
        internal static VxlHvaBindingResult Bind(VxlDocumentRaw vxl, HvaDocumentRaw hva, long maxDiagnostics = 256)
        {
            return Bind(vxl, hva, maxDiagnostics, false);
        }

        internal static VxlHvaBindingResult Bind(VxlDocumentRaw vxl, HvaDocumentRaw hva, long maxDiagnostics, bool allowUnboundVxlSections)
        {
            var c = new LegacyVisualDiagnosticCollector((int)Math.Min(Math.Max(0, maxDiagnostics), int.MaxValue));
            if (vxl == null || hva == null) { c.Add(new LegacyVisualDiagnostic(LegacyVisualDiagnosticCode.WrongInput, LegacyVisualDiagnosticSeverity.Error, 0, "binding", "Both VXL and HVA documents are required.")); return new VxlHvaBindingResult(VxlHvaBindingStatus.NotAttempted, null, null, null, c.Diagnostics, c.Complete()); }
            var bindings = new List<VxlHvaBinding>(); var unboundVxl = new List<int>(); var usedHva = new HashSet<int>();
            for (int vi = 0; vi < vxl.Sections.Count; vi++)
            {
                string name = vxl.Sections[vi].Header.NameCandidate; var matches = hva.SectionNames.Where(n => string.Equals(n.NameCandidate, name, StringComparison.Ordinal)).ToArray();
                if (matches.Length == 1 && !usedHva.Contains(matches[0].Ordinal)) { bindings.Add(new VxlHvaBinding(vi, matches[0].Ordinal, name)); usedHva.Add(matches[0].Ordinal); }
                else if (matches.Length > 1) c.Add(new LegacyVisualDiagnostic(LegacyVisualDiagnosticCode.AmbiguousBinding, LegacyVisualDiagnosticSeverity.Error, 0, "binding", "A VXL section maps to duplicate HVA names.", vi));
                else { unboundVxl.Add(vi); c.Add(new LegacyVisualDiagnostic(LegacyVisualDiagnosticCode.MissingBinding, LegacyVisualDiagnosticSeverity.Warning, 0, "binding", "A VXL section has no unique HVA name match.", vi)); }
            }
            for (int hi = 0; hi < hva.SectionNames.Count; hi++) if (!usedHva.Contains(hi)) { c.Add(new LegacyVisualDiagnostic(LegacyVisualDiagnosticCode.UnboundSection, LegacyVisualDiagnosticSeverity.Warning, 0, "binding", "An HVA section is not bound to a VXL section.", hi)); }
            VxlHvaBindingStatus status = c.Fatal ? VxlHvaBindingStatus.Ambiguous : ((unboundVxl.Count == 0 || allowUnboundVxlSections) && usedHva.Count == hva.SectionNames.Count ? VxlHvaBindingStatus.Complete : VxlHvaBindingStatus.Incomplete);
            return new VxlHvaBindingResult(status, bindings, unboundVxl, Enumerable.Range(0, hva.SectionNames.Count).Where(i => !usedHva.Contains(i)), c.Diagnostics, c.Complete());
        }
    }

    internal sealed class Cursor
    {
        private readonly byte[] bytes; private readonly long baseOffset; private readonly LegacyVisualDiagnosticCollector collector;
        internal Cursor(byte[] bytes, long baseOffset, LegacyVisualDiagnosticCollector collector) { this.bytes = bytes; this.baseOffset = baseOffset; this.collector = collector; }
        internal int Position { get; set; }
        internal long AbsoluteOffset => baseOffset + Position;
        internal bool IsHealthy => Position <= bytes.Length;
        internal byte Byte(string stage) { if (Position >= bytes.Length) { collector.Add(new LegacyVisualDiagnostic(LegacyVisualDiagnosticCode.TruncatedRecord, LegacyVisualDiagnosticSeverity.Error, AbsoluteOffset, stage, "Input ended while reading a field.")); return 0; } return bytes[Position++]; }
        internal uint UInt32(string stage) { if (Position + 4 > bytes.Length) { collector.Add(new LegacyVisualDiagnostic(LegacyVisualDiagnosticCode.TruncatedRecord, LegacyVisualDiagnosticSeverity.Error, AbsoluteOffset, stage, "Input ended while reading a 32-bit field.")); Position = bytes.Length; return 0; } uint value = (uint)(bytes[Position] | (bytes[Position + 1] << 8) | (bytes[Position + 2] << 16) | (bytes[Position + 3] << 24)); Position += 4; return value; }
        internal byte[] Bytes(int count, string stage) { if (count < 0 || Position + count > bytes.Length) { collector.Add(new LegacyVisualDiagnostic(LegacyVisualDiagnosticCode.TruncatedRecord, LegacyVisualDiagnosticSeverity.Error, AbsoluteOffset, stage, "Input ended while reading a byte field.")); Position = bytes.Length; return new byte[count < 0 ? 0 : Math.Min(count, bytes.Length - Position)]; } byte[] result = new byte[count]; Buffer.BlockCopy(bytes, Position, result, 0, count); Position += count; return result; }
    }

    internal static class WestwoodVxlReaderExtensions
    {
        internal static bool TryReadStreamForHva(Stream stream, long length, long max, out byte[] bytes, out LegacyVisualDiagnostic diagnostic)
        {
            bytes = null; diagnostic = null; if (length < 0 || length > max || length > int.MaxValue) { diagnostic = new LegacyVisualDiagnostic(LegacyVisualDiagnosticCode.InputBudgetExceeded, LegacyVisualDiagnosticSeverity.Error, 0, "hva-stream", "The bounded stream length is invalid or exceeds its budget."); return false; }
            bytes = new byte[(int)length]; int offset = 0; while (offset < bytes.Length) { int read = stream.Read(bytes, offset, bytes.Length - offset); if (read <= 0) { diagnostic = new LegacyVisualDiagnostic(LegacyVisualDiagnosticCode.TruncatedRecord, LegacyVisualDiagnosticSeverity.Error, offset, "hva-stream", "The stream ended before the bounded window was read."); bytes = null; return false; } offset += read; } return true;
        }
    }
}
