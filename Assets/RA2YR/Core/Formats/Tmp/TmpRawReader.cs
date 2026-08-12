using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using RA2YR.Core.Binary;
using RA2YR.Core.Binary.Seekable;
using RA2YR.Core.Content;
using RA2YR.Core.Formats.PackedMap;
using RA2YR.Core.Formats.Ini;

namespace RA2YR.Core.Formats.Tmp
{
    internal static class TmpRawReader
    {
        private const int FileHeaderBytes = 16;
        private const int CellHeaderBytes = 52;

        internal static TmpDocument Read(ReadOnlyMemory<byte> input, BinarySourceContext source,
            IniSourceProvenance provenance, TmpReadPolicy policy = null, long absoluteStartOffset = 0)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (provenance == null) throw new ArgumentNullException(nameof(provenance));
            policy = policy ?? new TmpReadPolicy();
            if (absoluteStartOffset < 0) throw new ArgumentOutOfRangeException(nameof(absoluteStartOffset));
            var collector = new TmpDiagnosticCollector(policy.Limits.MaxDiagnostics);
            if (input.Length > policy.Limits.MaxInputBytes)
            {
                collector.Fail(Diagnostic(TmpDiagnosticCode.InputBudgetExceeded, source, provenance, absoluteStartOffset, -1, "input", "TMP input exceeds the configured byte budget."));
                return EmptyDocument(collector, source, provenance, input.Length);
            }

            byte[] bytes;
            try { bytes = input.ToArray(); }
            catch (Exception exception) when (exception is OutOfMemoryException || exception is OverflowException)
            {
                collector.Fail(Diagnostic(TmpDiagnosticCode.InputBudgetExceeded, source, provenance, absoluteStartOffset, -1, "input", "TMP input snapshot allocation failed."));
                return EmptyDocument(collector, source, provenance, input.Length);
            }
            return Parse(bytes, source, provenance, policy, absoluteStartOffset, collector);
        }

        internal static TmpDocument Read(Stream stream, long length, BinarySourceContext source,
            IniSourceProvenance provenance, TmpReadPolicy policy = null, long absoluteStartOffset = 0)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));
            policy = policy ?? new TmpReadPolicy();
            var collector = new TmpDiagnosticCollector(policy.Limits.MaxDiagnostics);
            try
            {
                byte[] bytes = PackedMapBoundedInput.ReadStream(stream, length, source, policy.Limits.MaxInputBytes);
                return Parse(bytes, source, provenance, policy, absoluteStartOffset, collector);
            }
            catch (Exception exception) when (exception is IOException || exception is InvalidOperationException || exception is ArgumentOutOfRangeException)
            {
                collector.Fail(Diagnostic(TmpDiagnosticCode.SourceFailure, source, provenance, absoluteStartOffset, -1, "stream", "TMP stream could not be read within its bound: " + exception.Message));
                return EmptyDocument(collector, source, provenance, length);
            }
        }

        internal static TmpDocument Read(ReadOnlyDataWindow window, BinarySourceContext source,
            IniSourceProvenance provenance, TmpReadPolicy policy = null)
        {
            if (window == null) throw new ArgumentNullException(nameof(window));
            policy = policy ?? new TmpReadPolicy();
            var collector = new TmpDiagnosticCollector(policy.Limits.MaxDiagnostics);
            try
            {
                byte[] bytes = PackedMapBoundedInput.ReadWindow(window, "tmp-window-input", policy.Limits.MaxInputBytes);
                return Parse(bytes, source, provenance, policy, window.AbsoluteStartOffset, collector);
            }
            catch (Exception exception) when (exception is IOException || exception is InvalidOperationException || exception is ArgumentOutOfRangeException)
            {
                collector.Fail(Diagnostic(TmpDiagnosticCode.SourceFailure, source, provenance, window.AbsoluteStartOffset, -1, "window", "TMP bounded window could not be read: " + exception.Message));
                return EmptyDocument(collector, source, provenance, window.Length);
            }
        }

        private static TmpDocument Parse(byte[] bytes, BinarySourceContext source, IniSourceProvenance provenance,
            TmpReadPolicy policy, long absoluteStartOffset, TmpDiagnosticCollector collector)
        {
            var offsets = new List<TmpCellOffsetEntry>();
            var cells = new List<TmpCellRaw>();
            TmpFileHeaderRaw header = null;
            if (bytes.Length < FileHeaderBytes)
            {
                collector.Fail(Diagnostic(TmpDiagnosticCode.InvalidFileHeader, source, provenance, absoluteStartOffset, -1, "file-header", "TMP file header is truncated."));
                return new TmpDocument(new TmpFileHeaderRaw(0, 0, 0, 0), offsets, cells, collector.Diagnostics, collector.Execution, bytes.Length, Hash(bytes));
            }

            header = new TmpFileHeaderRaw(ReadU32(bytes, 0), ReadU32(bytes, 4), ReadU32(bytes, 8), ReadU32(bytes, 12));
            long slotsLong;
            try { slotsLong = checked((long)header.BlocksXRaw * header.BlocksYRaw); }
            catch (OverflowException)
            {
                collector.Fail(Diagnostic(TmpDiagnosticCode.ArithmeticOverflow, source, provenance, absoluteStartOffset, -1, "grid", "TMP cell-slot multiplication overflowed."));
                return new TmpDocument(header, offsets, cells, collector.Diagnostics, collector.Execution, bytes.Length, Hash(bytes));
            }
            if (header.BlocksXRaw == 0 || header.BlocksYRaw == 0 || header.BlocksXRaw > policy.Limits.MaxTemplateWidth || header.BlocksYRaw > policy.Limits.MaxTemplateHeight)
            {
                collector.Fail(Diagnostic(TmpDiagnosticCode.InvalidGridDimensions, source, provenance, absoluteStartOffset, -1, "grid", "TMP grid dimensions are missing or exceed the configured limits."));
            }
            if (slotsLong > policy.Limits.MaxCellSlots || slotsLong > int.MaxValue)
            {
                collector.Fail(Diagnostic(TmpDiagnosticCode.DimensionBudgetExceeded, source, provenance, absoluteStartOffset, -1, "grid", "TMP cell-slot budget exceeded."));
                return new TmpDocument(header, offsets, cells, collector.Diagnostics, collector.Execution, bytes.Length, Hash(bytes));
            }
            int slots = (int)slotsLong;
            long tableBytes;
            try { tableBytes = checked((long)slots * 4L); }
            catch (OverflowException)
            {
                collector.Fail(Diagnostic(TmpDiagnosticCode.OffsetArithmeticOverflow, source, provenance, absoluteStartOffset, -1, "offset-table", "TMP offset-table size overflowed."));
                return new TmpDocument(header, offsets, cells, collector.Diagnostics, collector.Execution, bytes.Length, Hash(bytes));
            }
            long tableEnd = FileHeaderBytes + tableBytes;
            if (tableEnd > bytes.Length)
            {
                collector.Fail(Diagnostic(TmpDiagnosticCode.OffsetTableTruncated, source, provenance, absoluteStartOffset + FileHeaderBytes, -1, "offset-table", "TMP cell offset table is truncated."));
                return new TmpDocument(header, offsets, cells, collector.Diagnostics, collector.Execution, bytes.Length, Hash(bytes));
            }

            var offsetsByValue = new Dictionary<int, int>();
            for (int slot = 0; slot < slots; slot++)
            {
                int raw = unchecked((int)ReadU32(bytes, checked(FileHeaderBytes + slot * 4)));
                bool empty = raw == 0;
                offsets.Add(new TmpCellOffsetEntry(slot, raw, empty));
                if (empty) continue;
                if (raw < 0)
                {
                    collector.Fail(Diagnostic(TmpDiagnosticCode.NegativeOffset, source, provenance, absoluteStartOffset + FileHeaderBytes + slot * 4L, slot, "offset-table", "TMP cell offset is negative."));
                    continue;
                }
                if (raw < FileHeaderBytes)
                {
                    collector.Fail(Diagnostic(TmpDiagnosticCode.OffsetInsideHeader, source, provenance, absoluteStartOffset + raw, slot, "offset-table", "TMP cell offset points into the file header."));
                    continue;
                }
                if (raw < tableEnd)
                {
                    collector.Fail(Diagnostic(TmpDiagnosticCode.OffsetInsideOffsetTable, source, provenance, absoluteStartOffset + raw, slot, "offset-table", "TMP cell offset points into the offset table."));
                    continue;
                }
                if (raw > bytes.Length - CellHeaderBytes)
                {
                    collector.Fail(Diagnostic(TmpDiagnosticCode.OffsetOutsideWindow, source, provenance, absoluteStartOffset + raw, slot, "offset-table", "TMP cell header offset is outside the bounded window."));
                    continue;
                }
                if (offsetsByValue.ContainsKey(raw))
                    collector.Add(Diagnostic(BinaryDiagnosticSeverity.Warning, TmpDiagnosticCode.DuplicateCellOffset, source, provenance, absoluteStartOffset + raw, slot, "offset-table", "Multiple slots reference the same physical cell header."), false);
                else offsetsByValue.Add(raw, slot);

                byte[] headerBytes = new byte[CellHeaderBytes];
                Buffer.BlockCopy(bytes, raw, headerBytes, 0, CellHeaderBytes);
                TmpCellHeaderRaw cellHeader = new TmpCellHeaderRaw(absoluteStartOffset + raw, headerBytes);
                TmpCellPlaneDirectory directory = BuildDirectory(bytes, raw, slot, cellHeader, source, provenance, policy, collector, absoluteStartOffset);
                cells.Add(new TmpCellRaw(slot, absoluteStartOffset + raw, cellHeader, directory));
            }
            return new TmpDocument(header, offsets, cells, collector.Diagnostics, collector.Execution, bytes.Length, Hash(bytes));
        }

        private static TmpCellPlaneDirectory BuildDirectory(byte[] bytes, int cellOffset, int slot, TmpCellHeaderRaw header,
            BinarySourceContext source, IniSourceProvenance provenance, TmpReadPolicy policy, TmpDiagnosticCollector collector, long absoluteStartOffset)
        {
            long diamondLength;
            try
            {
                if (policy.TileWidth != checked(policy.TileHeight * 2))
                {
                    collector.Fail(Diagnostic(TmpDiagnosticCode.DiamondProfileMismatch, source, provenance, absoluteStartOffset + cellOffset, slot, "diamond", "The selected canonical diamond profile requires width == 2 * height."));
                }
                diamondLength = checked((long)policy.TileWidth * policy.TileHeight / 2L);
            }
            catch (OverflowException)
            {
                collector.Fail(Diagnostic(TmpDiagnosticCode.DiamondLengthOverflow, source, provenance, absoluteStartOffset + cellOffset, slot, "diamond", "Diamond plane length arithmetic overflowed."));
                diamondLength = 0;
            }
            if (diamondLength > policy.Limits.MaxDiamondPlaneBytes)
                collector.Fail(Diagnostic(TmpDiagnosticCode.DimensionBudgetExceeded, source, provenance, absoluteStartOffset + cellOffset, slot, "diamond", "Diamond plane exceeds the configured byte budget."));

            long extraLength = 0;
            bool extraCandidate = header.HasExtraDataCandidate || header.ExtraColorOffsetRawU32 != 0 || header.ExtraDepthOffsetRawU32 != 0;
            if (extraCandidate)
            {
                if (header.ExtraWidthRawU32 == 0 || header.ExtraHeightRawU32 == 0 || header.ExtraWidthRawU32 > policy.Limits.MaxExtraWidth || header.ExtraHeightRawU32 > policy.Limits.MaxExtraHeight)
                {
                    collector.Fail(Diagnostic(TmpDiagnosticCode.InvalidDimensions, source, provenance, absoluteStartOffset + cellOffset + 28, slot, "extra", "Extra plane dimensions are invalid."));
                }
                try { extraLength = checked((long)header.ExtraWidthRawU32 * header.ExtraHeightRawU32); }
                catch (OverflowException)
                {
                    collector.Fail(Diagnostic(TmpDiagnosticCode.ExtraAreaOverflow, source, provenance, absoluteStartOffset + cellOffset + 28, slot, "extra", "Extra plane area arithmetic overflowed."));
                }
                if (extraLength > policy.Limits.MaxExtraArea)
                    collector.Fail(Diagnostic(TmpDiagnosticCode.DimensionBudgetExceeded, source, provenance, absoluteStartOffset + cellOffset, slot, "extra", "Extra plane exceeds the configured byte budget."));
            }
            if (header.UnknownFlagsRaw != 0)
                collector.Add(Diagnostic(BinaryDiagnosticSeverity.Warning, TmpDiagnosticCode.UnknownFlags, source, provenance, absoluteStartOffset + cellOffset + 36, slot, "flags", "Unknown TMP flag bits were preserved."), false);
            if (header.HasDamagedDataCandidate)
                collector.Add(Diagnostic(BinaryDiagnosticSeverity.Warning, TmpDiagnosticCode.DamagedDataUnresolved, source, provenance, absoluteStartOffset + cellOffset + 36, slot, "flags", "Damaged-data body layout remains unresolved."), false);

            var windows = new List<TmpPlaneWindow>();
            long sequential = checked((long)cellOffset + CellHeaderBytes);
            AddWindow(windows, bytes, cellOffset, slot, "DiamondColor", sequential, diamondLength, source, provenance, policy, collector, absoluteStartOffset);
            sequential = checked(sequential + diamondLength);
            bool includeZ = policy.PlaneLayout == TmpPlaneLayoutPolicy.SequentialWithZ || (policy.PlaneLayout == TmpPlaneLayoutPolicy.DeclaredOffsets && header.HasZDataCandidate);
            if (includeZ)
            {
                long offset = policy.PlaneLayout == TmpPlaneLayoutPolicy.DeclaredOffsets ? header.DiamondDepthOffsetRawU32 == 0 ? -1 : checked((long)cellOffset + header.DiamondDepthOffsetRawU32) : sequential;
                AddWindow(windows, bytes, cellOffset, slot, "DiamondDepth", offset, diamondLength, source, provenance, policy, collector, absoluteStartOffset);
                if (policy.PlaneLayout != TmpPlaneLayoutPolicy.DeclaredOffsets) sequential = checked(sequential + diamondLength);
            }
            bool includeExtra = extraCandidate;
            if (includeExtra)
            {
                long offset = policy.PlaneLayout == TmpPlaneLayoutPolicy.DeclaredOffsets ? header.ExtraColorOffsetRawU32 == 0 ? -1 : checked((long)cellOffset + header.ExtraColorOffsetRawU32) : sequential;
                AddWindow(windows, bytes, cellOffset, slot, "ExtraColor", offset, extraLength, source, provenance, policy, collector, absoluteStartOffset);
                if (policy.PlaneLayout != TmpPlaneLayoutPolicy.DeclaredOffsets) sequential = checked(sequential + extraLength);
                if (header.HasZDataCandidate || header.ExtraDepthOffsetRawU32 != 0)
                {
                    offset = policy.PlaneLayout == TmpPlaneLayoutPolicy.DeclaredOffsets ? header.ExtraDepthOffsetRawU32 == 0 ? -1 : checked((long)cellOffset + header.ExtraDepthOffsetRawU32) : sequential;
                    AddWindow(windows, bytes, cellOffset, slot, "ExtraDepth", offset, extraLength, source, provenance, policy, collector, absoluteStartOffset);
                    if (policy.PlaneLayout != TmpPlaneLayoutPolicy.DeclaredOffsets) sequential = checked(sequential + extraLength);
                }
            }
            ValidateOverlaps(windows, slot, source, provenance, collector, absoluteStartOffset);
            if (sequential < bytes.Length && policy.PlaneLayout != TmpPlaneLayoutPolicy.DeclaredOffsets)
                collector.Add(Diagnostic(BinaryDiagnosticSeverity.Warning, TmpDiagnosticCode.TrailingBytes, source, provenance, absoluteStartOffset + sequential, slot, "planes", "Bytes after the selected plane directory were preserved as trailing data."), false);
            return new TmpCellPlaneDirectory(policy.PlaneLayout, windows, collector.Diagnostics.Where(d => d.CellOrdinal == slot));
        }

        private static void AddWindow(List<TmpPlaneWindow> windows, byte[] bytes, int cellOffset, int slot, string kind, long offset, long length,
            BinarySourceContext source, IniSourceProvenance provenance, TmpReadPolicy policy, TmpDiagnosticCollector collector, long absoluteStartOffset)
        {
            if (length <= 0 || offset < 0)
            {
                if (length > 0 && offset < 0)
                    collector.Fail(Diagnostic(TmpDiagnosticCode.PlaneOutsideWindow, source, provenance, absoluteStartOffset + cellOffset, slot, kind, "Plane offset is absent or invalid."));
                windows.Add(new TmpPlaneWindow(kind, offset, length, Array.Empty<byte>(), false, "Unavailable"));
                return;
            }
            long end;
            try { end = checked(offset + length); }
            catch (OverflowException)
            {
                collector.Fail(Diagnostic(TmpDiagnosticCode.OffsetArithmeticOverflow, source, provenance, absoluteStartOffset + cellOffset, slot, kind, "Plane range arithmetic overflowed."));
                windows.Add(new TmpPlaneWindow(kind, offset, length, Array.Empty<byte>(), false, "Overflow"));
                return;
            }
            if (offset < cellOffset + CellHeaderBytes || end > bytes.Length)
            {
                collector.Fail(Diagnostic(TmpDiagnosticCode.PlaneOutsideWindow, source, provenance, absoluteStartOffset + Math.Max(0, offset), slot, kind, "Plane range is outside the bounded TMP window or overlaps the cell header."));
                windows.Add(new TmpPlaneWindow(kind, offset, length, Array.Empty<byte>(), false, "OutOfRange"));
                return;
            }
            if (length > int.MaxValue)
            {
                collector.Fail(Diagnostic(TmpDiagnosticCode.DimensionBudgetExceeded, source, provenance, absoluteStartOffset + offset, slot, kind, "Plane length exceeds the supported array size."));
                windows.Add(new TmpPlaneWindow(kind, offset, length, Array.Empty<byte>(), false, "Budget"));
                return;
            }
            byte[] data = new byte[(int)length];
            Buffer.BlockCopy(bytes, (int)offset, data, 0, data.Length);
            windows.Add(new TmpPlaneWindow(kind, offset, length, data, true, "Exact"));
        }

        private static void ValidateOverlaps(List<TmpPlaneWindow> windows, int slot, BinarySourceContext source, IniSourceProvenance provenance,
            TmpDiagnosticCollector collector, long absoluteStartOffset)
        {
            foreach (var pair in windows.SelectMany((a, i) => windows.Skip(i + 1).Select(b => new { A = a, B = b })))
            {
                if (!pair.A.Present || !pair.B.Present) continue;
                long aEnd = checked(pair.A.RelativeOffset + pair.A.Length);
                long bEnd = checked(pair.B.RelativeOffset + pair.B.Length);
                if (pair.A.RelativeOffset < bEnd && pair.B.RelativeOffset < aEnd)
                    collector.Fail(Diagnostic(TmpDiagnosticCode.PlaneOverlap, source, provenance, absoluteStartOffset + Math.Max(pair.A.RelativeOffset, pair.B.RelativeOffset), slot, "planes", "Selected plane windows overlap."));
            }
        }

        private static TmpDocument EmptyDocument(TmpDiagnosticCollector collector, BinarySourceContext source, IniSourceProvenance provenance, long consumed)
            => new TmpDocument(new TmpFileHeaderRaw(0, 0, 0, 0), Array.Empty<TmpCellOffsetEntry>(), Array.Empty<TmpCellRaw>(), collector.Diagnostics, collector.Execution, consumed, Hash(Array.Empty<byte>()));

        private static TmpDiagnostic Diagnostic(TmpDiagnosticCode code, BinarySourceContext source, IniSourceProvenance provenance, long offset, int cell, string stage, string message)
            => new TmpDiagnostic(BinaryDiagnosticSeverity.Error, code, source, provenance, offset, cell, stage, message);

        private static TmpDiagnostic Diagnostic(BinaryDiagnosticSeverity severity, TmpDiagnosticCode code, BinarySourceContext source, IniSourceProvenance provenance, long offset, int cell, string stage, string message)
            => new TmpDiagnostic(severity, code, source, provenance, offset, cell, stage, message);

        private static uint ReadU32(byte[] bytes, int offset) => (uint)(bytes[offset] | (bytes[offset + 1] << 8) | (bytes[offset + 2] << 16) | (bytes[offset + 3] << 24));
        private static string Hash(byte[] bytes)
        {
            using (SHA256 sha = SHA256.Create())
                return BitConverter.ToString(sha.ComputeHash(bytes ?? Array.Empty<byte>())).Replace("-", string.Empty).ToLowerInvariant();
        }
    }
}
