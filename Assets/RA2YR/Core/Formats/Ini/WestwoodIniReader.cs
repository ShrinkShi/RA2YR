using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using RA2YR.Core.Binary;
using RA2YR.Core.Binary.Seekable;

namespace RA2YR.Core.Formats.Ini
{
    internal static class WestwoodIniReader
    {
        private const int WindowReadChunkSize = 81920;
        private const long FixedModelAllocationEstimate = 8192;
        private const long LineAndNodeAllocationEstimate = 320;
        private const long DiagnosticAllocationEstimate = 256;

        private static readonly Encoding StrictUtf8 = new UTF8Encoding(false, true);
        private static readonly Encoding StrictUtf16LittleEndian =
            new UnicodeEncoding(false, false, true);
        private static readonly Encoding StrictUtf16BigEndian =
            new UnicodeEncoding(true, false, true);

        public static IniParseResult Read(
            ReadOnlyMemory<byte> input,
            BinarySourceContext source,
            IniSourceProvenance provenance,
            IniReadLimits limits = null,
            long absoluteStartOffset = 0)
        {
            ValidateContext(source, provenance);
            IniReadLimits effectiveLimits = limits ?? IniReadLimits.Default;
            IniParseResult directFailure = ValidateSnapshotBounds(
                input.Length,
                absoluteStartOffset,
                source,
                provenance,
                effectiveLimits,
                true);
            if (directFailure != null)
            {
                return directFailure;
            }

            byte[] snapshot;
            try
            {
                snapshot = input.ToArray();
            }
            catch (OutOfMemoryException)
            {
                return DirectFailure(
                    IniDiagnosticCode.ReadFailure,
                    source,
                    provenance,
                    absoluteStartOffset,
                    input.Length,
                    input.Length,
                    "ini-input-snapshot",
                    -1,
                    "The validated INI memory snapshot could not be allocated.",
                    BinaryDiagnosticCode.ReadFailure);
            }

            return ReadOwnedSnapshot(
                snapshot,
                source,
                provenance,
                effectiveLimits,
                absoluteStartOffset,
                snapshot.LongLength);
        }

        public static IniParseResult Read(
            Stream stream,
            long inputLength,
            BinarySourceContext source,
            IniSourceProvenance provenance,
            IniReadLimits limits = null,
            bool leaveOpen = false,
            long absoluteStartOffset = 0)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            ValidateContext(source, provenance);
            BinaryReadSession session = null;
            try
            {
                IniReadLimits effectiveLimits = limits ?? IniReadLimits.Default;
                if (inputLength > effectiveLimits.MaxCumulativeRawBytes)
                {
                    if (!leaveOpen)
                    {
                        stream.Dispose();
                    }

                    return DirectFailure(
                        IniDiagnosticCode.CumulativeRawByteBudgetExceeded,
                        source,
                        provenance,
                        absoluteStartOffset,
                        inputLength,
                        inputLength,
                        "ini-raw-bytes",
                        -1,
                        "The declared INI input exceeds its cumulative raw-byte budget.");
                }

                session = BinaryReadSession.FromStream(
                    stream,
                    inputLength,
                    source,
                    effectiveLimits.ToBinaryLimits(),
                    leaveOpen,
                    absoluteStartOffset);
                byte[] owned = GetOwnedSessionArray(session, source, provenance);
                return ParseSession(session, owned, source, provenance, effectiveLimits);
            }
            catch (IniReadException exception)
            {
                return IniParseResult.Failure(exception.Diagnostic);
            }
            catch (BinaryReadException exception)
            {
                return IniParseResult.Failure(MapBinaryFailure(
                    exception.Diagnostic,
                    source,
                    provenance,
                    -1));
            }
            catch (OutOfMemoryException)
            {
                return DirectFailure(
                    IniDiagnosticCode.ReadFailure,
                    source,
                    provenance,
                    absoluteStartOffset,
                    inputLength,
                    inputLength,
                    "ini-input-snapshot",
                    -1,
                    "The validated INI stream snapshot could not be retained.",
                    BinaryDiagnosticCode.ReadFailure);
            }
            finally
            {
                session?.Dispose();
            }
        }

        public static IniParseResult ReadSeekable(
            Stream stream,
            BinarySourceContext source,
            IniSourceProvenance provenance,
            IniReadLimits limits = null,
            bool leaveOpen = false)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            ValidateContext(source, provenance);
            BinaryReadSession session = null;
            try
            {
                IniReadLimits effectiveLimits = limits ?? IniReadLimits.Default;
                long position;
                long length;
                try
                {
                    if (!stream.CanSeek)
                    {
                        if (!leaveOpen)
                        {
                            TryDispose(stream);
                        }

                        return DirectFailure(
                            IniDiagnosticCode.UnsupportedSeekOperation,
                            source,
                            provenance,
                            0,
                            0,
                            0,
                            "ini-input",
                            -1,
                            "A seekable stream is required when the INI input length is inferred.",
                            BinaryDiagnosticCode.UnsupportedSeekOperation);
                    }

                    position = stream.Position;
                    length = stream.Length;
                }
                catch (Exception exception) when (
                    exception is IOException ||
                    exception is NotSupportedException ||
                    exception is ObjectDisposedException)
                {
                    if (!leaveOpen)
                    {
                        TryDispose(stream);
                    }

                    return DirectFailure(
                        exception is NotSupportedException
                            ? IniDiagnosticCode.UnsupportedSeekOperation
                            : IniDiagnosticCode.ReadFailure,
                        source,
                        provenance,
                        0,
                        0,
                        0,
                        "ini-input",
                        -1,
                        "The seekable INI stream bounds could not be inspected.",
                        exception is NotSupportedException
                            ? BinaryDiagnosticCode.UnsupportedSeekOperation
                            : BinaryDiagnosticCode.ReadFailure);
                }

                if (position < 0 || length < position)
                {
                    if (!leaveOpen)
                    {
                        TryDispose(stream);
                    }

                    return DirectFailure(
                        IniDiagnosticCode.BinaryReadFailure,
                        source,
                        provenance,
                        position,
                        length,
                        0,
                        "ini-input",
                        -1,
                        "The seekable INI stream reported invalid bounds.",
                        BinaryDiagnosticCode.InvalidLength);
                }

                IniParseResult boundsFailure = ValidateSnapshotBounds(
                    length - position,
                    position,
                    source,
                    provenance,
                    effectiveLimits,
                    true);
                if (boundsFailure != null)
                {
                    if (!leaveOpen)
                    {
                        TryDispose(stream);
                    }

                    return boundsFailure;
                }

                session = BinaryReadSession.FromSeekableStream(
                    stream,
                    source,
                    effectiveLimits.ToBinaryLimits(),
                    leaveOpen);
                byte[] owned = GetOwnedSessionArray(session, source, provenance);
                return ParseSession(session, owned, source, provenance, effectiveLimits);
            }
            catch (IniReadException exception)
            {
                return IniParseResult.Failure(exception.Diagnostic);
            }
            catch (BinaryReadException exception)
            {
                return IniParseResult.Failure(MapBinaryFailure(
                    exception.Diagnostic,
                    source,
                    provenance,
                    -1));
            }
            catch (OutOfMemoryException)
            {
                return DirectFailure(
                    IniDiagnosticCode.ReadFailure,
                    source,
                    provenance,
                    0,
                    0,
                    0,
                    "ini-input-snapshot",
                    -1,
                    "The validated seekable INI snapshot could not be retained.",
                    BinaryDiagnosticCode.ReadFailure);
            }
            finally
            {
                session?.Dispose();
            }
        }

        public static IniParseResult Read(
            ReadOnlyDataWindow window,
            BinarySourceContext source,
            IniSourceProvenance provenance,
            IniReadLimits limits = null)
        {
            if (window == null)
            {
                throw new ArgumentNullException(nameof(window));
            }

            ValidateContext(source, provenance);
            IniReadLimits effectiveLimits = limits ?? IniReadLimits.Default;
            IniParseResult directFailure = ValidateSnapshotBounds(
                window.Length,
                window.AbsoluteStartOffset,
                source,
                provenance,
                effectiveLimits,
                true);
            if (directFailure != null)
            {
                return directFailure;
            }

            byte[] snapshot;
            try
            {
                snapshot = new byte[checked((int)window.Length)];
                int offset = 0;
                int maximumChunk = checked((int)Math.Min(
                    WindowReadChunkSize,
                    effectiveLimits.MaxSingleReadBytes));
                while (offset < snapshot.Length)
                {
                    int count = Math.Min(maximumChunk, snapshot.Length - offset);
                    window.ReadExactly(
                        offset,
                        snapshot,
                        offset,
                        count,
                        "ini-window-input");
                    offset = checked(offset + count);
                }
            }
            catch (BinaryReadException exception)
            {
                return IniParseResult.Failure(MapBinaryFailure(
                    exception.Diagnostic,
                    source,
                    provenance,
                    -1));
            }
            catch (OutOfMemoryException)
            {
                return DirectFailure(
                    IniDiagnosticCode.ReadFailure,
                    source,
                    provenance,
                    window.AbsoluteStartOffset,
                    window.Length,
                    window.Length,
                    "ini-window-snapshot",
                    -1,
                    "The validated bounded INI window could not be allocated.",
                    BinaryDiagnosticCode.ReadFailure);
            }

            return ReadOwnedSnapshot(
                snapshot,
                source,
                provenance,
                effectiveLimits,
                window.AbsoluteStartOffset,
                snapshot.LongLength);
        }

        private static IniParseResult ReadOwnedSnapshot(
            byte[] ownedBytes,
            BinarySourceContext source,
            IniSourceProvenance provenance,
            IniReadLimits limits,
            long absoluteStartOffset,
            long initialAllocation)
        {
            BinaryReadSession session = null;
            try
            {
                session = BinaryReadSession.FromMemory(
                    ownedBytes,
                    source,
                    limits.ToBinaryLimits(),
                    absoluteStartOffset);
                if (initialAllocation != 0)
                {
                    session.ReserveAllocation(
                        initialAllocation,
                        absoluteStartOffset,
                        ownedBytes.LongLength,
                        "ini-input-snapshot");
                }

                return ParseSession(session, ownedBytes, source, provenance, limits);
            }
            catch (IniReadException exception)
            {
                return IniParseResult.Failure(exception.Diagnostic);
            }
            catch (BinaryReadException exception)
            {
                return IniParseResult.Failure(MapBinaryFailure(
                    exception.Diagnostic,
                    source,
                    provenance,
                    -1));
            }
            catch (OutOfMemoryException)
            {
                return DirectFailure(
                    IniDiagnosticCode.ReadFailure,
                    source,
                    provenance,
                    absoluteStartOffset,
                    ownedBytes.LongLength,
                    ownedBytes.LongLength,
                    "ini-document-model",
                    -1,
                    "The bounded INI document model could not be allocated.",
                    BinaryDiagnosticCode.ReadFailure);
            }
            finally
            {
                session?.Dispose();
            }
        }

        private static IniParseResult ParseSession(
            BinaryReadSession session,
            byte[] ownedBytes,
            BinarySourceContext source,
            IniSourceProvenance provenance,
            IniReadLimits limits)
        {
            BoundedBinaryReader reader = session.Root;
            ReserveAllocation(
                session,
                reader,
                FixedModelAllocationEstimate,
                source,
                provenance,
                -1,
                "ini-fixed-model");

            if (ownedBytes.LongLength > limits.MaxCumulativeRawBytes)
            {
                throw Failure(
                    IniDiagnosticCode.CumulativeRawByteBudgetExceeded,
                    source,
                    provenance,
                    reader.AbsoluteStartOffset,
                    ownedBytes.LongLength,
                    reader.RemainingLength,
                    "ini-raw-bytes",
                    -1,
                    "The preserved INI input exceeds its cumulative raw-byte budget.");
            }

            EncodingObservation encoding = InspectEncoding(
                ownedBytes,
                reader.AbsoluteStartOffset,
                source,
                provenance);
            var store = new IniRawByteStore(ownedBytes);
            var lines = new List<IniPhysicalLine>();
            var nodes = new List<IniNode>();
            var diagnostics = new List<IniDiagnostic>();
            long sectionCount = 0;
            long keyValueCount = 0;
            long commentCount = 0;
            long opaqueCount = 0;
            int currentSectionLineId = -1;
            int position = encoding.BomLength;

            while (position < ownedBytes.Length)
            {
                int lineIndex = lines.Count;
                if (lineIndex >= limits.MaxLineCount)
                {
                    throw Failure(
                        IniDiagnosticCode.LineCountBudgetExceeded,
                        source,
                        provenance,
                        CheckedAbsoluteOffset(
                            reader.AbsoluteStartOffset,
                            position,
                            source,
                            provenance,
                            lineIndex),
                        1,
                        ownedBytes.Length - position,
                        "ini-line-count",
                        lineIndex,
                        "The INI physical-line count exceeds its explicit budget.");
                }

                if (lineIndex >= limits.MaxTotalNodes)
                {
                    throw Failure(
                        IniDiagnosticCode.TotalNodeBudgetExceeded,
                        source,
                        provenance,
                        CheckedAbsoluteOffset(
                            reader.AbsoluteStartOffset,
                            position,
                            source,
                            provenance,
                            lineIndex),
                        1,
                        ownedBytes.Length - position,
                        "ini-node-count",
                        lineIndex,
                        "The INI node count exceeds its explicit budget.");
                }

                int contentStart = position;
                while (position < ownedBytes.Length)
                {
                    int unit = ReadSyntaxUnit(ownedBytes, position, encoding);
                    if (unit == '\r' || unit == '\n')
                    {
                        break;
                    }

                    position = checked(position + encoding.UnitWidth);
                }

                int contentLength = checked(position - contentStart);
                if (contentLength > limits.MaxLineBytes)
                {
                    throw Failure(
                        IniDiagnosticCode.LineLengthBudgetExceeded,
                        source,
                        provenance,
                        CheckedAbsoluteOffset(
                            reader.AbsoluteStartOffset,
                            contentStart,
                            source,
                            provenance,
                            lineIndex),
                        contentLength,
                        ownedBytes.Length - contentStart,
                        "ini-line-bytes",
                        lineIndex,
                        "The INI physical line exceeds its explicit byte budget.");
                }

                IniLineEnding endingKind = IniLineEnding.None;
                int endingLength = 0;
                if (position < ownedBytes.Length)
                {
                    int first = ReadSyntaxUnit(ownedBytes, position, encoding);
                    if (first == '\r')
                    {
                        if (position + encoding.UnitWidth < ownedBytes.Length &&
                            ReadSyntaxUnit(
                                ownedBytes,
                                position + encoding.UnitWidth,
                                encoding) == '\n')
                        {
                            endingKind = IniLineEnding.CarriageReturnLineFeed;
                            endingLength = checked(2 * encoding.UnitWidth);
                        }
                        else
                        {
                            endingKind = IniLineEnding.CarriageReturn;
                            endingLength = encoding.UnitWidth;
                        }
                    }
                    else
                    {
                        endingKind = IniLineEnding.LineFeed;
                        endingLength = encoding.UnitWidth;
                    }
                }

                long absoluteLineOffset = CheckedAbsoluteOffset(
                    reader.AbsoluteStartOffset,
                    contentStart,
                    source,
                    provenance,
                    lineIndex);
                ReserveAllocation(
                    session,
                    reader,
                    LineAndNodeAllocationEstimate,
                    source,
                    provenance,
                    lineIndex,
                    "ini-line-node-model");
                ReserveRecord(
                    reader,
                    source,
                    provenance,
                    lineIndex,
                    "ini-node-record");

                var line = new IniPhysicalLine(
                    lineIndex,
                    absoluteLineOffset,
                    new IniRawSlice(store, contentStart, contentLength),
                    new IniRawSlice(store, position, endingLength),
                    endingKind,
                    encoding.UnitWidth);
                bool resetsSection;
                int nextSectionLineId;
                IniNode node = ClassifyLine(
                    ownedBytes,
                    store,
                    line,
                    encoding,
                    currentSectionLineId,
                    source,
                    provenance,
                    diagnostics,
                    session,
                    reader,
                    out nextSectionLineId,
                    out resetsSection);

                ValidateCategoryBudget(
                    node.Kind,
                    ref sectionCount,
                    ref keyValueCount,
                    ref commentCount,
                    ref opaqueCount,
                    limits,
                    source,
                    provenance,
                    line);
                lines.Add(line);
                nodes.Add(node);
                if (resetsSection)
                {
                    currentSectionLineId = nextSectionLineId;
                }

                position = checked(position + endingLength);
            }

            ConsumeAndComplete(reader, limits, source, provenance);
            IniDocumentCompleteness completeness = opaqueCount == 0
                ? IniDocumentCompleteness.Structured
                : IniDocumentCompleteness.StructuredWithOpaqueLines;
            var document = new IniRawDocument(
                store,
                source,
                provenance,
                encoding.BomKind,
                encoding.PhysicalEncoding,
                encoding.BomLength,
                completeness,
                lines,
                nodes);
            return IniParseResult.Success(document, diagnostics);
        }

        private static IniNode ClassifyLine(
            byte[] bytes,
            IniRawByteStore store,
            IniPhysicalLine line,
            EncodingObservation encoding,
            int currentSectionLineId,
            BinarySourceContext source,
            IniSourceProvenance provenance,
            List<IniDiagnostic> diagnostics,
            BinaryReadSession session,
            BoundedBinaryReader reader,
            out int nextSectionLineId,
            out bool resetsSection)
        {
            int start = line.Content.Offset;
            int end = checked(start + line.Content.Length);
            int trimmedStart = SkipWhitespace(bytes, start, end, encoding);
            int trimmedEnd = TrimWhitespaceEnd(bytes, trimmedStart, end, encoding);
            nextSectionLineId = currentSectionLineId;
            resetsSection = false;

            if (trimmedStart == trimmedEnd)
            {
                return new IniBlankNode(line);
            }

            if (ContainsUnsupportedControl(bytes, start, end, encoding))
            {
                return CreateOpaque(
                    line,
                    IniOpaqueReason.UnsupportedControlCharacter,
                    source,
                    provenance,
                    diagnostics,
                    session,
                    reader);
            }

            int first = ReadSyntaxUnit(bytes, trimmedStart, encoding);
            if (first == ';')
            {
                int bodyStart = checked(trimmedStart + encoding.UnitWidth);
                return new IniCommentNode(
                    line,
                    trimmedStart,
                    new IniRawSlice(store, bodyStart, end - bodyStart));
            }

            if (first == '[')
            {
                resetsSection = true;
                nextSectionLineId = -1;
                int close = FindUnit(
                    bytes,
                    trimmedStart + encoding.UnitWidth,
                    end,
                    ']',
                    encoding);
                if (close < 0)
                {
                    return CreateOpaque(
                        line,
                        IniOpaqueReason.UnterminatedSection,
                        source,
                        provenance,
                        diagnostics,
                        session,
                        reader);
                }

                int rawNameStart = checked(trimmedStart + encoding.UnitWidth);
                int rawNameEnd = close;
                int nameStart = SkipWhitespace(bytes, rawNameStart, rawNameEnd, encoding);
                int nameEnd = TrimWhitespaceEnd(bytes, nameStart, rawNameEnd, encoding);
                if (nameStart == nameEnd)
                {
                    return CreateOpaque(
                        line,
                        IniOpaqueReason.EmptySectionName,
                        source,
                        provenance,
                        diagnostics,
                        session,
                        reader);
                }

                int trailingStart = checked(close + encoding.UnitWidth);
                int trailingMeaning = SkipWhitespace(bytes, trailingStart, end, encoding);
                if (trailingMeaning < end)
                {
                    return CreateOpaque(
                        line,
                        IniOpaqueReason.SectionTrailingContent,
                        source,
                        provenance,
                        diagnostics,
                        session,
                        reader);
                }

                nextSectionLineId = line.Id;
                return new IniSectionNode(
                    line,
                    new IniRawSlice(store, rawNameStart, rawNameEnd - rawNameStart),
                    new IniRawSlice(store, nameStart, nameEnd - nameStart),
                    new IniRawSlice(store, trailingStart, end - trailingStart));
            }

            int equals = FindUnit(bytes, trimmedStart, end, '=', encoding);
            if (equals < 0)
            {
                return CreateOpaque(
                    line,
                    IniOpaqueReason.MissingEquals,
                    source,
                    provenance,
                    diagnostics,
                    session,
                    reader);
            }

            if (currentSectionLineId < 0)
            {
                return CreateOpaque(
                    line,
                    IniOpaqueReason.KeyOutsideSection,
                    source,
                    provenance,
                    diagnostics,
                    session,
                    reader);
            }

            int keyEnd = TrimWhitespaceEnd(bytes, trimmedStart, equals, encoding);
            if (keyEnd == trimmedStart)
            {
                return CreateOpaque(
                    line,
                    IniOpaqueReason.NonStandardLine,
                    source,
                    provenance,
                    diagnostics,
                    session,
                    reader);
            }

            int afterEquals = checked(equals + encoding.UnitWidth);
            int valueStart = SkipWhitespace(bytes, afterEquals, end, encoding);
            int semicolon = FindUnit(bytes, afterEquals, end, ';', encoding);
            if (semicolon >= 0)
            {
                AddAmbiguousSemicolonDiagnostic(
                    line,
                    semicolon,
                    encoding.UnitWidth,
                    source,
                    provenance,
                    diagnostics,
                    session,
                    reader);
            }

            return new IniKeyValueNode(
                line,
                currentSectionLineId,
                new IniRawSlice(store, start, trimmedStart - start),
                new IniRawSlice(store, trimmedStart, keyEnd - trimmedStart),
                new IniRawSlice(store, keyEnd, equals - keyEnd),
                new IniRawSlice(store, afterEquals, valueStart - afterEquals),
                new IniRawSlice(store, valueStart, end - valueStart),
                equals);
        }

        private static IniOpaqueNode CreateOpaque(
            IniPhysicalLine line,
            IniOpaqueReason reason,
            BinarySourceContext source,
            IniSourceProvenance provenance,
            List<IniDiagnostic> diagnostics,
            BinaryReadSession session,
            BoundedBinaryReader reader)
        {
            ReserveAllocation(
                session,
                reader,
                DiagnosticAllocationEstimate,
                source,
                provenance,
                line.Id,
                "ini-diagnostic");
            diagnostics.Add(new IniDiagnostic(
                IniDiagnosticSeverity.Warning,
                IniDiagnosticCode.OpaqueLinePreserved,
                source,
                provenance,
                line.AbsoluteOffset,
                line.FullRaw.Length,
                reader.Length - line.Content.Offset,
                "ini-opaque-line",
                line.Id,
                "The physical INI line was preserved without assigning unconfirmed semantics."));
            return new IniOpaqueNode(line, reason);
        }

        private static void AddAmbiguousSemicolonDiagnostic(
            IniPhysicalLine line,
            int semicolonByteOffset,
            int unitWidth,
            BinarySourceContext source,
            IniSourceProvenance provenance,
            List<IniDiagnostic> diagnostics,
            BinaryReadSession session,
            BoundedBinaryReader reader)
        {
            ReserveAllocation(
                session,
                reader,
                DiagnosticAllocationEstimate,
                source,
                provenance,
                line.Id,
                "ini-diagnostic");
            long absoluteOffset = checked(
                line.AbsoluteOffset + semicolonByteOffset - line.Content.Offset);
            diagnostics.Add(new IniDiagnostic(
                IniDiagnosticSeverity.Warning,
                IniDiagnosticCode.AmbiguousInlineSemicolon,
                source,
                provenance,
                absoluteOffset,
                unitWidth,
                reader.Length - semicolonByteOffset,
                "ini-inline-semicolon",
                line.Id,
                "An inline semicolon was retained without applying unconfirmed comment semantics."));
        }

        private static void ValidateCategoryBudget(
            IniNodeKind kind,
            ref long sectionCount,
            ref long keyValueCount,
            ref long commentCount,
            ref long opaqueCount,
            IniReadLimits limits,
            BinarySourceContext source,
            IniSourceProvenance provenance,
            IniPhysicalLine line)
        {
            long updated;
            IniDiagnosticCode code;
            long maximum;
            string field;
            switch (kind)
            {
                case IniNodeKind.Section:
                    updated = CheckedIncrement(sectionCount, source, provenance, line);
                    maximum = limits.MaxSectionNodes;
                    code = IniDiagnosticCode.SectionBudgetExceeded;
                    field = "ini-section-count";
                    if (updated <= maximum)
                    {
                        sectionCount = updated;
                        return;
                    }

                    break;
                case IniNodeKind.KeyValue:
                    updated = CheckedIncrement(keyValueCount, source, provenance, line);
                    maximum = limits.MaxKeyValueNodes;
                    code = IniDiagnosticCode.KeyValueBudgetExceeded;
                    field = "ini-key-value-count";
                    if (updated <= maximum)
                    {
                        keyValueCount = updated;
                        return;
                    }

                    break;
                case IniNodeKind.Comment:
                    updated = CheckedIncrement(commentCount, source, provenance, line);
                    maximum = limits.MaxCommentNodes;
                    code = IniDiagnosticCode.CommentBudgetExceeded;
                    field = "ini-comment-count";
                    if (updated <= maximum)
                    {
                        commentCount = updated;
                        return;
                    }

                    break;
                case IniNodeKind.Opaque:
                    updated = CheckedIncrement(opaqueCount, source, provenance, line);
                    maximum = limits.MaxOpaqueNodes;
                    code = IniDiagnosticCode.OpaqueBudgetExceeded;
                    field = "ini-opaque-count";
                    if (updated <= maximum)
                    {
                        opaqueCount = updated;
                        return;
                    }

                    break;
                case IniNodeKind.Blank:
                    return;
                default:
                    throw new InvalidOperationException("The INI node kind is invalid.");
            }

            throw Failure(
                code,
                source,
                provenance,
                line.AbsoluteOffset,
                updated,
                line.FullRaw.Length,
                field,
                line.Id,
                "An INI node category exceeds its explicit budget.");
        }

        private static long CheckedIncrement(
            long value,
            BinarySourceContext source,
            IniSourceProvenance provenance,
            IniPhysicalLine line)
        {
            try
            {
                return checked(value + 1);
            }
            catch (OverflowException)
            {
                throw Failure(
                    IniDiagnosticCode.ArithmeticOverflow,
                    source,
                    provenance,
                    line.AbsoluteOffset,
                    1,
                    line.FullRaw.Length,
                    "ini-node-count",
                    line.Id,
                    "INI node accounting overflowed Int64.",
                    BinaryDiagnosticCode.ArithmeticOverflow);
            }
        }

        private static EncodingObservation InspectEncoding(
            byte[] bytes,
            long absoluteStartOffset,
            BinarySourceContext source,
            IniSourceProvenance provenance)
        {
            IniByteOrderMarkKind bomKind = IniByteOrderMarkKind.None;
            IniPhysicalEncodingKind physicalEncoding = IniPhysicalEncodingKind.RawSingleByte;
            int bomLength = 0;
            int unitWidth = 1;
            bool bigEndian = false;
            Encoding strictEncoding = null;

            if (bytes.Length >= 3 && bytes[0] == 0xef && bytes[1] == 0xbb && bytes[2] == 0xbf)
            {
                bomKind = IniByteOrderMarkKind.Utf8;
                physicalEncoding = IniPhysicalEncodingKind.Utf8WithBom;
                bomLength = 3;
                strictEncoding = StrictUtf8;
            }
            else if (bytes.Length >= 2 && bytes[0] == 0xff && bytes[1] == 0xfe)
            {
                bomKind = IniByteOrderMarkKind.Utf16LittleEndian;
                physicalEncoding = IniPhysicalEncodingKind.Utf16LittleEndianWithBom;
                bomLength = 2;
                unitWidth = 2;
                strictEncoding = StrictUtf16LittleEndian;
            }
            else if (bytes.Length >= 2 && bytes[0] == 0xfe && bytes[1] == 0xff)
            {
                bomKind = IniByteOrderMarkKind.Utf16BigEndian;
                physicalEncoding = IniPhysicalEncodingKind.Utf16BigEndianWithBom;
                bomLength = 2;
                unitWidth = 2;
                bigEndian = true;
                strictEncoding = StrictUtf16BigEndian;
            }

            if (unitWidth == 2 && ((bytes.Length - bomLength) & 1) != 0)
            {
                throw Failure(
                    IniDiagnosticCode.ByteOrderMarkLengthConflict,
                    source,
                    provenance,
                    checked(absoluteStartOffset + bytes.Length - 1),
                    1,
                    1,
                    "ini-byte-order-mark",
                    -1,
                    "The UTF-16 BOM is incompatible with the remaining byte length.");
            }

            if (strictEncoding != null)
            {
                try
                {
                    strictEncoding.GetCharCount(bytes, bomLength, bytes.Length - bomLength);
                }
                catch (DecoderFallbackException)
                {
                    throw Failure(
                        IniDiagnosticCode.InvalidEncoding,
                        source,
                        provenance,
                        absoluteStartOffset + bomLength,
                        bytes.Length - bomLength,
                        bytes.Length - bomLength,
                        "ini-encoded-input",
                        -1,
                        "The BOM-declared INI byte sequence is not valid in its declared encoding.");
                }
            }

            var observation = new EncodingObservation(
                bomKind,
                physicalEncoding,
                bomLength,
                unitWidth,
                bigEndian);
            for (int offset = bomLength; offset < bytes.Length; offset += unitWidth)
            {
                if (ReadSyntaxUnit(bytes, offset, observation) == 0)
                {
                    throw Failure(
                        IniDiagnosticCode.NulCharacter,
                        source,
                        provenance,
                        checked(absoluteStartOffset + offset),
                        unitWidth,
                        bytes.Length - offset,
                        "ini-nul-character",
                        -1,
                        "NUL is not accepted in a lossless Westwood INI document.");
                }
            }

            return observation;
        }

        private static int ReadSyntaxUnit(
            byte[] bytes,
            int offset,
            EncodingObservation encoding)
        {
            if (encoding.UnitWidth == 1)
            {
                return bytes[offset];
            }

            return encoding.BigEndian
                ? (bytes[offset] << 8) | bytes[offset + 1]
                : bytes[offset] | (bytes[offset + 1] << 8);
        }

        private static int SkipWhitespace(
            byte[] bytes,
            int start,
            int end,
            EncodingObservation encoding)
        {
            int position = start;
            while (position < end && IsStructuralWhitespace(
                       ReadSyntaxUnit(bytes, position, encoding)))
            {
                position += encoding.UnitWidth;
            }

            return position;
        }

        private static int TrimWhitespaceEnd(
            byte[] bytes,
            int start,
            int end,
            EncodingObservation encoding)
        {
            int position = end;
            while (position > start && IsStructuralWhitespace(
                       ReadSyntaxUnit(bytes, position - encoding.UnitWidth, encoding)))
            {
                position -= encoding.UnitWidth;
            }

            return position;
        }

        private static bool IsStructuralWhitespace(int value)
        {
            return value == 0x20 || value == 0x09;
        }

        private static bool ContainsUnsupportedControl(
            byte[] bytes,
            int start,
            int end,
            EncodingObservation encoding)
        {
            for (int position = start; position < end; position += encoding.UnitWidth)
            {
                int value = ReadSyntaxUnit(bytes, position, encoding);
                if (value < 0x20 && value != 0x09)
                {
                    return true;
                }
            }

            return false;
        }

        private static int FindUnit(
            byte[] bytes,
            int start,
            int end,
            int target,
            EncodingObservation encoding)
        {
            for (int position = start; position < end; position += encoding.UnitWidth)
            {
                if (ReadSyntaxUnit(bytes, position, encoding) == target)
                {
                    return position;
                }
            }

            return -1;
        }

        private static void ConsumeAndComplete(
            BoundedBinaryReader reader,
            IniReadLimits limits,
            BinarySourceContext source,
            IniSourceProvenance provenance)
        {
            while (reader.RemainingLength > 0)
            {
                if (limits.MaxSingleReadBytes == 0)
                {
                    throw Failure(
                        IniDiagnosticCode.ReadBudgetExceeded,
                        source,
                        provenance,
                        reader.AbsoluteOffset,
                        1,
                        reader.RemainingLength,
                        "ini-input",
                        -1,
                        "The INI input cannot be consumed within a zero-byte read budget.",
                        BinaryDiagnosticCode.ReadBudgetExceeded);
                }

                long count = Math.Min(reader.RemainingLength, limits.MaxSingleReadBytes);
                reader.Skip(count, "ini-input");
            }

            BinaryParseCompletion completion = reader.Complete(
                TrailingDataPolicy.RequireFullyConsumed,
                "ini-input");
            if (!completion.IsComplete || completion.HasErrors)
            {
                BinaryDiagnostic diagnostic = completion.Diagnostics.Count > 0
                    ? completion.Diagnostics[0]
                    : null;
                if (diagnostic != null)
                {
                    throw new IniReadException(MapBinaryFailure(
                        diagnostic,
                        source,
                        provenance,
                        -1));
                }

                throw Failure(
                    IniDiagnosticCode.BinaryReadFailure,
                    source,
                    provenance,
                    reader.AbsoluteOffset,
                    0,
                    reader.RemainingLength,
                    "ini-input",
                    -1,
                    "The bounded INI reader could not prove full input consumption.");
            }
        }

        private static byte[] GetOwnedSessionArray(
            BinaryReadSession session,
            BinarySourceContext source,
            IniSourceProvenance provenance)
        {
            ArraySegment<byte> segment;
            if (MemoryMarshal.TryGetArray(session.Memory, out segment) &&
                segment.Array != null && segment.Offset == 0 &&
                segment.Count == segment.Array.Length)
            {
                return segment.Array;
            }

            session.ReserveAllocation(
                session.Memory.Length,
                session.AbsoluteStartOffset,
                session.Memory.Length,
                "ini-owned-snapshot");
            try
            {
                return session.Memory.ToArray();
            }
            catch (OutOfMemoryException)
            {
                throw Failure(
                    IniDiagnosticCode.ReadFailure,
                    source,
                    provenance,
                    session.AbsoluteStartOffset,
                    session.Memory.Length,
                    session.Memory.Length,
                    "ini-owned-snapshot",
                    -1,
                    "The bounded INI snapshot could not be retained.",
                    BinaryDiagnosticCode.ReadFailure);
            }
        }

        private static IniParseResult ValidateSnapshotBounds(
            long inputLength,
            long absoluteStartOffset,
            BinarySourceContext source,
            IniSourceProvenance provenance,
            IniReadLimits limits,
            bool allocationRequired)
        {
            if (inputLength < 0)
            {
                return DirectFailure(
                    IniDiagnosticCode.BinaryReadFailure,
                    source,
                    provenance,
                    absoluteStartOffset,
                    inputLength,
                    0,
                    "ini-input",
                    -1,
                    "An INI input length cannot be negative.",
                    BinaryDiagnosticCode.InvalidLength);
            }

            if (inputLength > limits.MaxInputBytes)
            {
                return DirectFailure(
                    IniDiagnosticCode.InputBudgetExceeded,
                    source,
                    provenance,
                    absoluteStartOffset,
                    inputLength,
                    inputLength,
                    "ini-input",
                    -1,
                    "The INI input exceeds its explicit input budget.",
                    BinaryDiagnosticCode.InputBudgetExceeded);
            }

            if (inputLength > limits.MaxCumulativeRawBytes)
            {
                return DirectFailure(
                    IniDiagnosticCode.CumulativeRawByteBudgetExceeded,
                    source,
                    provenance,
                    absoluteStartOffset,
                    inputLength,
                    inputLength,
                    "ini-raw-bytes",
                    -1,
                    "The INI input exceeds its cumulative raw-byte budget.");
            }

            if (inputLength > int.MaxValue)
            {
                return DirectFailure(
                    IniDiagnosticCode.BinaryReadFailure,
                    source,
                    provenance,
                    absoluteStartOffset,
                    inputLength,
                    inputLength,
                    "ini-input",
                    -1,
                    "The INI input cannot be represented by the immutable byte model.",
                    BinaryDiagnosticCode.InvalidLength);
            }

            if (allocationRequired && inputLength > limits.MaxAllocatedBytes)
            {
                return DirectFailure(
                    IniDiagnosticCode.AllocationBudgetExceeded,
                    source,
                    provenance,
                    absoluteStartOffset,
                    inputLength,
                    limits.MaxAllocatedBytes,
                    "ini-input-snapshot",
                    -1,
                    "The INI snapshot exceeds its cumulative allocation budget.",
                    BinaryDiagnosticCode.AllocationBudgetExceeded);
            }

            if (inputLength > 0 && limits.MaxSingleReadBytes == 0)
            {
                return DirectFailure(
                    IniDiagnosticCode.ReadBudgetExceeded,
                    source,
                    provenance,
                    absoluteStartOffset,
                    1,
                    inputLength,
                    "ini-input",
                    -1,
                    "The INI input cannot be read within a zero-byte operation budget.",
                    BinaryDiagnosticCode.ReadBudgetExceeded);
            }

            try
            {
                checked
                {
                    _ = absoluteStartOffset + inputLength;
                }
            }
            catch (OverflowException)
            {
                return DirectFailure(
                    IniDiagnosticCode.ArithmeticOverflow,
                    source,
                    provenance,
                    absoluteStartOffset,
                    inputLength,
                    inputLength,
                    "ini-input",
                    -1,
                    "The INI absolute start plus length overflows Int64.",
                    BinaryDiagnosticCode.ArithmeticOverflow);
            }

            return null;
        }

        private static long CheckedAbsoluteOffset(
            long absoluteStartOffset,
            int relativeOffset,
            BinarySourceContext source,
            IniSourceProvenance provenance,
            int lineIndex)
        {
            try
            {
                return checked(absoluteStartOffset + relativeOffset);
            }
            catch (OverflowException)
            {
                throw Failure(
                    IniDiagnosticCode.ArithmeticOverflow,
                    source,
                    provenance,
                    absoluteStartOffset,
                    relativeOffset,
                    0,
                    "ini-byte-offset",
                    lineIndex,
                    "An INI absolute byte offset overflowed Int64.",
                    BinaryDiagnosticCode.ArithmeticOverflow);
            }
        }

        private static void ReserveAllocation(
            BinaryReadSession session,
            BoundedBinaryReader reader,
            long bytes,
            BinarySourceContext source,
            IniSourceProvenance provenance,
            int lineIndex,
            string field)
        {
            try
            {
                session.ReserveAllocation(
                    bytes,
                    reader.AbsoluteOffset,
                    reader.RemainingLength,
                    field);
            }
            catch (BinaryReadException exception)
            {
                throw new IniReadException(MapBinaryFailure(
                    exception.Diagnostic,
                    source,
                    provenance,
                    lineIndex));
            }
        }

        private static void ReserveRecord(
            BoundedBinaryReader reader,
            BinarySourceContext source,
            IniSourceProvenance provenance,
            int lineIndex,
            string field)
        {
            try
            {
                reader.ReserveRecords(1, field);
            }
            catch (BinaryReadException exception)
            {
                throw new IniReadException(MapBinaryFailure(
                    exception.Diagnostic,
                    source,
                    provenance,
                    lineIndex));
            }
        }

        private static void ValidateContext(
            BinarySourceContext source,
            IniSourceProvenance provenance)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (provenance == null)
            {
                throw new ArgumentNullException(nameof(provenance));
            }

            if (!string.Equals(
                    source.LogicalSourceId,
                    provenance.SourceId,
                    StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    "INI provenance must identify the binary source.",
                    nameof(provenance));
            }
        }

        private static IniDiagnostic MapBinaryFailure(
            BinaryDiagnostic diagnostic,
            BinarySourceContext source,
            IniSourceProvenance provenance,
            int lineIndex)
        {
            IniDiagnosticCode code;
            switch (diagnostic.Code)
            {
                case BinaryDiagnosticCode.InputBudgetExceeded:
                    code = IniDiagnosticCode.InputBudgetExceeded;
                    break;
                case BinaryDiagnosticCode.ReadBudgetExceeded:
                    code = IniDiagnosticCode.ReadBudgetExceeded;
                    break;
                case BinaryDiagnosticCode.AllocationBudgetExceeded:
                    code = IniDiagnosticCode.AllocationBudgetExceeded;
                    break;
                case BinaryDiagnosticCode.RecordBudgetExceeded:
                    code = IniDiagnosticCode.RecordBudgetExceeded;
                    break;
                case BinaryDiagnosticCode.ArithmeticOverflow:
                    code = IniDiagnosticCode.ArithmeticOverflow;
                    break;
                case BinaryDiagnosticCode.UnexpectedEndOfInput:
                    code = IniDiagnosticCode.UnexpectedEndOfInput;
                    break;
                case BinaryDiagnosticCode.UnsupportedSeekOperation:
                    code = IniDiagnosticCode.UnsupportedSeekOperation;
                    break;
                case BinaryDiagnosticCode.ReadFailure:
                    code = IniDiagnosticCode.ReadFailure;
                    break;
                default:
                    code = IniDiagnosticCode.BinaryReadFailure;
                    break;
            }

            return new IniDiagnostic(
                IniDiagnosticSeverity.Error,
                code,
                source,
                provenance,
                diagnostic.AbsoluteOffset,
                diagnostic.RequestedLength,
                diagnostic.RemainingLength,
                diagnostic.FieldOrSection,
                lineIndex,
                "The bounded binary layer rejected the INI input.",
                diagnostic.Code);
        }

        private static IniReadException Failure(
            IniDiagnosticCode code,
            BinarySourceContext source,
            IniSourceProvenance provenance,
            long absoluteOffset,
            long requestedLength,
            long remainingLength,
            string fieldOrSection,
            int lineIndex,
            string message,
            BinaryDiagnosticCode? binaryCode = null)
        {
            return new IniReadException(new IniDiagnostic(
                IniDiagnosticSeverity.Error,
                code,
                source,
                provenance,
                absoluteOffset,
                requestedLength,
                remainingLength,
                fieldOrSection,
                lineIndex,
                message,
                binaryCode));
        }

        private static IniParseResult DirectFailure(
            IniDiagnosticCode code,
            BinarySourceContext source,
            IniSourceProvenance provenance,
            long absoluteOffset,
            long requestedLength,
            long remainingLength,
            string fieldOrSection,
            int lineIndex,
            string message,
            BinaryDiagnosticCode? binaryCode = null)
        {
            return IniParseResult.Failure(new IniDiagnostic(
                IniDiagnosticSeverity.Error,
                code,
                source,
                provenance,
                absoluteOffset,
                requestedLength,
                remainingLength,
                fieldOrSection,
                lineIndex,
                message,
                binaryCode));
        }

        private static void TryDispose(Stream stream)
        {
            try
            {
                stream.Dispose();
            }
            catch (Exception exception) when (
                exception is IOException ||
                exception is ObjectDisposedException)
            {
                // Failure paths retain their original structured diagnostic.
            }
        }

        private readonly struct EncodingObservation
        {
            public EncodingObservation(
                IniByteOrderMarkKind bomKind,
                IniPhysicalEncodingKind physicalEncoding,
                int bomLength,
                int unitWidth,
                bool bigEndian)
            {
                BomKind = bomKind;
                PhysicalEncoding = physicalEncoding;
                BomLength = bomLength;
                UnitWidth = unitWidth;
                BigEndian = bigEndian;
            }

            public IniByteOrderMarkKind BomKind { get; }

            public IniPhysicalEncodingKind PhysicalEncoding { get; }

            public int BomLength { get; }

            public int UnitWidth { get; }

            public bool BigEndian { get; }
        }
    }
}
