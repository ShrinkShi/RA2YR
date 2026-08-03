using System;
using System.Collections.Generic;
using System.Linq;
using RA2YR.Core.Binary;
using RA2YR.Core.Content;

namespace RA2YR.Core.Formats.Ini
{
    internal enum IniByteOrderMarkKind
    {
        None,
        Utf8,
        Utf16LittleEndian,
        Utf16BigEndian
    }

    internal enum IniPhysicalEncodingKind
    {
        RawSingleByte,
        Utf8WithBom,
        Utf16LittleEndianWithBom,
        Utf16BigEndianWithBom
    }

    internal enum IniLineEnding
    {
        None,
        CarriageReturnLineFeed,
        LineFeed,
        CarriageReturn
    }

    internal enum IniNodeKind
    {
        Section,
        KeyValue,
        Comment,
        Blank,
        Opaque
    }

    internal enum IniOpaqueReason
    {
        KeyOutsideSection,
        MissingEquals,
        UnterminatedSection,
        EmptySectionName,
        SectionTrailingContent,
        UnsupportedControlCharacter,
        NonStandardLine
    }

    internal enum IniDocumentCompleteness
    {
        Structured,
        StructuredWithOpaqueLines
    }

    internal enum IniRawAsciiComparison
    {
        Ordinal
    }

    internal sealed class IniRawByteStore
    {
        private readonly byte[] bytes;

        internal IniRawByteStore(byte[] ownedBytes)
        {
            bytes = ownedBytes ?? throw new ArgumentNullException(nameof(ownedBytes));
        }

        public int Length => bytes.Length;

        internal ReadOnlySpan<byte> GetSpan(int offset, int length)
        {
            return new ReadOnlySpan<byte>(bytes, offset, length);
        }

        internal byte Read(int offset)
        {
            return bytes[offset];
        }

        internal byte[] Copy(int offset, int length)
        {
            if (length == 0)
            {
                return Array.Empty<byte>();
            }

            var copy = new byte[length];
            Buffer.BlockCopy(bytes, offset, copy, 0, length);
            return copy;
        }

        internal void CopyTo(int offset, byte[] destination, int destinationOffset, int length)
        {
            Buffer.BlockCopy(bytes, offset, destination, destinationOffset, length);
        }
    }

    internal readonly struct IniRawSlice
    {
        private readonly IniRawByteStore store;

        internal IniRawSlice(IniRawByteStore store, int offset, int length)
        {
            this.store = store ?? throw new ArgumentNullException(nameof(store));
            if (offset < 0 || length < 0 || offset > store.Length - length)
            {
                throw new ArgumentOutOfRangeException(nameof(offset));
            }

            Offset = offset;
            Length = length;
        }

        public int Offset { get; }

        public int Length { get; }

        public byte this[int index]
        {
            get
            {
                EnsureInitialized();
                if (index < 0 || index >= Length)
                {
                    throw new ArgumentOutOfRangeException(nameof(index));
                }

                return store.Read(checked(Offset + index));
            }
        }

        public byte[] ToArray()
        {
            EnsureInitialized();
            return store.Copy(Offset, Length);
        }

        public void CopyTo(byte[] destination, int destinationOffset)
        {
            EnsureInitialized();
            if (destination == null)
            {
                throw new ArgumentNullException(nameof(destination));
            }

            if (destinationOffset < 0 || destinationOffset > destination.Length - Length)
            {
                throw new ArgumentOutOfRangeException(nameof(destinationOffset));
            }

            store.CopyTo(Offset, destination, destinationOffset, Length);
        }

        internal ReadOnlySpan<byte> Span
        {
            get
            {
                EnsureInitialized();
                return store.GetSpan(Offset, Length);
            }
        }

        internal IniRawByteStore Store
        {
            get
            {
                EnsureInitialized();
                return store;
            }
        }

        private void EnsureInitialized()
        {
            if (store == null)
            {
                throw new InvalidOperationException("The raw INI slice is not initialized.");
            }
        }
    }

    internal sealed class IniPhysicalLine
    {
        internal IniPhysicalLine(
            int id,
            long absoluteOffset,
            IniRawSlice content,
            IniRawSlice ending,
            IniLineEnding endingKind,
            int syntaxUnitWidth)
        {
            if (id < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(id));
            }

            if (absoluteOffset < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(absoluteOffset));
            }

            if (content.Store != ending.Store ||
                ending.Offset != checked(content.Offset + content.Length))
            {
                throw new ArgumentException("An INI line must use contiguous slices from one store.");
            }

            if ((syntaxUnitWidth != 1 && syntaxUnitWidth != 2) ||
                checked(GetEndingUnitCount(endingKind) * syntaxUnitWidth) != ending.Length)
            {
                throw new ArgumentException("The line-ending kind and byte length disagree.");
            }

            Id = id;
            AbsoluteOffset = absoluteOffset;
            Content = content;
            Ending = ending;
            EndingKind = endingKind;
            FullRaw = new IniRawSlice(
                content.Store,
                content.Offset,
                checked(content.Length + ending.Length));
        }

        public int Id { get; }

        public long AbsoluteOffset { get; }

        public IniRawSlice Content { get; }

        public IniRawSlice Ending { get; }

        public IniRawSlice FullRaw { get; }

        public IniLineEnding EndingKind { get; }

        public bool HasLineEnding => EndingKind != IniLineEnding.None;

        private static int GetEndingUnitCount(IniLineEnding kind)
        {
            switch (kind)
            {
                case IniLineEnding.None:
                    return 0;
                case IniLineEnding.LineFeed:
                case IniLineEnding.CarriageReturn:
                    return 1;
                case IniLineEnding.CarriageReturnLineFeed:
                    return 2;
                default:
                    throw new ArgumentOutOfRangeException(nameof(kind));
            }
        }
    }

    internal abstract class IniNode
    {
        protected IniNode(IniNodeKind kind, IniPhysicalLine line)
        {
            if (!Enum.IsDefined(typeof(IniNodeKind), kind))
            {
                throw new ArgumentOutOfRangeException(nameof(kind));
            }

            Kind = kind;
            Line = line ?? throw new ArgumentNullException(nameof(line));
        }

        public IniNodeKind Kind { get; }

        public IniPhysicalLine Line { get; }

        public int PhysicalLineId => Line.Id;
    }

    internal sealed class IniSectionNode : IniNode
    {
        internal IniSectionNode(
            IniPhysicalLine line,
            IniRawSlice rawName,
            IniRawSlice name,
            IniRawSlice trailingBytes)
            : base(IniNodeKind.Section, line)
        {
            RawName = rawName;
            Name = name;
            TrailingBytes = trailingBytes;
        }

        public IniRawSlice RawName { get; }

        public IniRawSlice Name { get; }

        public IniRawSlice TrailingBytes { get; }
    }

    internal sealed class IniKeyValueNode : IniNode
    {
        internal IniKeyValueNode(
            IniPhysicalLine line,
            int containingSectionLineId,
            IniRawSlice leadingWhitespace,
            IniRawSlice key,
            IniRawSlice whitespaceBeforeEquals,
            IniRawSlice whitespaceAfterEquals,
            IniRawSlice value,
            int equalsByteOffset)
            : base(IniNodeKind.KeyValue, line)
        {
            if (containingSectionLineId < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(containingSectionLineId));
            }

            if (equalsByteOffset < line.Content.Offset ||
                equalsByteOffset >= checked(line.Content.Offset + line.Content.Length))
            {
                throw new ArgumentOutOfRangeException(nameof(equalsByteOffset));
            }

            ContainingSectionLineId = containingSectionLineId;
            LeadingWhitespace = leadingWhitespace;
            Key = key;
            WhitespaceBeforeEquals = whitespaceBeforeEquals;
            WhitespaceAfterEquals = whitespaceAfterEquals;
            Value = value;
            EqualsByteOffset = equalsByteOffset;
        }

        public int ContainingSectionLineId { get; }

        public IniRawSlice LeadingWhitespace { get; }

        public IniRawSlice Key { get; }

        public IniRawSlice WhitespaceBeforeEquals { get; }

        public IniRawSlice WhitespaceAfterEquals { get; }

        public IniRawSlice Value { get; }

        public int EqualsByteOffset { get; }
    }

    internal sealed class IniCommentNode : IniNode
    {
        internal IniCommentNode(
            IniPhysicalLine line,
            int markerByteOffset,
            IniRawSlice body)
            : base(IniNodeKind.Comment, line)
        {
            MarkerByteOffset = markerByteOffset;
            Body = body;
        }

        public int MarkerByteOffset { get; }

        public IniRawSlice Body { get; }
    }

    internal sealed class IniBlankNode : IniNode
    {
        internal IniBlankNode(IniPhysicalLine line)
            : base(IniNodeKind.Blank, line)
        {
        }
    }

    internal sealed class IniOpaqueNode : IniNode
    {
        internal IniOpaqueNode(IniPhysicalLine line, IniOpaqueReason reason)
            : base(IniNodeKind.Opaque, line)
        {
            if (!Enum.IsDefined(typeof(IniOpaqueReason), reason))
            {
                throw new ArgumentOutOfRangeException(nameof(reason));
            }

            Reason = reason;
        }

        public IniOpaqueReason Reason { get; }
    }

    internal sealed class IniSourceProvenance
    {
        private readonly IReadOnlyList<LogicalContentPath> logicalChain;

        public IniSourceProvenance(
            string sourceId,
            IEnumerable<LogicalContentPath> logicalChain)
        {
            SourceId = BinaryDiagnosticLabel.Validate(sourceId, nameof(sourceId));
            LogicalContentPath[] chain =
                (logicalChain ?? throw new ArgumentNullException(nameof(logicalChain))).ToArray();
            if (chain.Length == 0 || chain.Any(path => path == null))
            {
                throw new ArgumentException(
                    "INI provenance requires a nonempty logical chain.",
                    nameof(logicalChain));
            }

            this.logicalChain = Array.AsReadOnly(chain);
        }

        public string SourceId { get; }

        public IReadOnlyList<LogicalContentPath> LogicalChain => logicalChain;
    }

    internal sealed class IniRawDocument
    {
        private readonly IniRawByteStore store;
        private readonly IniPhysicalLine[] lines;
        private readonly IniNode[] nodes;
        private readonly IReadOnlyList<IniPhysicalLine> lineView;
        private readonly IReadOnlyList<IniNode> nodeView;

        internal IniRawDocument(
            IniRawByteStore ownedStore,
            BinarySourceContext source,
            IniSourceProvenance provenance,
            IniByteOrderMarkKind byteOrderMarkKind,
            IniPhysicalEncodingKind physicalEncoding,
            int byteOrderMarkLength,
            IniDocumentCompleteness completeness,
            IEnumerable<IniPhysicalLine> lines,
            IEnumerable<IniNode> nodes)
        {
            if (!Enum.IsDefined(typeof(IniByteOrderMarkKind), byteOrderMarkKind))
            {
                throw new ArgumentOutOfRangeException(nameof(byteOrderMarkKind));
            }

            if (!Enum.IsDefined(typeof(IniPhysicalEncodingKind), physicalEncoding))
            {
                throw new ArgumentOutOfRangeException(nameof(physicalEncoding));
            }

            if (!Enum.IsDefined(typeof(IniDocumentCompleteness), completeness))
            {
                throw new ArgumentOutOfRangeException(nameof(completeness));
            }

            store = ownedStore ?? throw new ArgumentNullException(nameof(ownedStore));
            if (byteOrderMarkLength < 0 || byteOrderMarkLength > store.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(byteOrderMarkLength));
            }

            Source = source ?? throw new ArgumentNullException(nameof(source));
            Provenance = provenance ?? throw new ArgumentNullException(nameof(provenance));
            ByteOrderMarkKind = byteOrderMarkKind;
            PhysicalEncoding = physicalEncoding;
            ByteOrderMark = new IniRawSlice(store, 0, byteOrderMarkLength);
            Completeness = completeness;
            this.lines = (lines ?? throw new ArgumentNullException(nameof(lines))).ToArray();
            this.nodes = (nodes ?? throw new ArgumentNullException(nameof(nodes))).ToArray();
            if (this.lines.Any(line => line == null) || this.nodes.Any(node => node == null) ||
                this.lines.Length != this.nodes.Length)
            {
                throw new ArgumentException(
                    "A lossless INI document requires one non-null node per physical line.");
            }

            for (int index = 0; index < this.lines.Length; index++)
            {
                if (this.lines[index].Id != index || this.nodes[index].Line != this.lines[index] ||
                    this.lines[index].Content.Store != store)
                {
                    throw new ArgumentException(
                        "INI lines and nodes must retain stable ordered document ownership.");
                }
            }

            lineView = Array.AsReadOnly(this.lines);
            nodeView = Array.AsReadOnly(this.nodes);
            OriginalLength = store.Length;
            CanonicalModelSha256 = IniCanonicalModelHasher.Compute(this);
        }

        public BinarySourceContext Source { get; }

        public IniSourceProvenance Provenance { get; }

        public IniByteOrderMarkKind ByteOrderMarkKind { get; }

        public IniPhysicalEncodingKind PhysicalEncoding { get; }

        public IniRawSlice ByteOrderMark { get; }

        public IniDocumentCompleteness Completeness { get; }

        public int OriginalLength { get; }

        public IReadOnlyList<IniPhysicalLine> Lines => lineView;

        public IReadOnlyList<IniNode> Nodes => nodeView;

        public string CanonicalModelSha256 { get; }

        public IReadOnlyList<IniSectionNode> FindSectionsByTrimmedRawAsciiName(
            string asciiName,
            IniRawAsciiComparison comparison)
        {
            ValidateComparison(comparison);
            byte[] expected = IniTextEncodingPolicy.EncodeAsciiForPhysicalDocument(
                asciiName,
                PhysicalEncoding);
            return Array.AsReadOnly(nodes
                .OfType<IniSectionNode>()
                .Where(node => node.Name.Span.SequenceEqual(expected))
                .ToArray());
        }

        public IReadOnlyList<IniKeyValueNode> FindKeyValuesByTrimmedRawAsciiKey(
            string asciiName,
            IniRawAsciiComparison comparison)
        {
            ValidateComparison(comparison);
            byte[] expected = IniTextEncodingPolicy.EncodeAsciiForPhysicalDocument(
                asciiName,
                PhysicalEncoding);
            return Array.AsReadOnly(nodes
                .OfType<IniKeyValueNode>()
                .Where(node => node.Key.Span.SequenceEqual(expected))
                .ToArray());
        }

        public IReadOnlyList<IniKeyValueNode> FindKeyValuesByTrimmedRawAsciiKey(
            IniSectionNode section,
            string asciiName,
            IniRawAsciiComparison comparison)
        {
            if (section == null)
            {
                throw new ArgumentNullException(nameof(section));
            }

            if (section.Line.Content.Store != store)
            {
                throw new ArgumentException(
                    "The queried section does not belong to this document.",
                    nameof(section));
            }

            ValidateComparison(comparison);
            byte[] expected = IniTextEncodingPolicy.EncodeAsciiForPhysicalDocument(
                asciiName,
                PhysicalEncoding);
            return Array.AsReadOnly(nodes
                .OfType<IniKeyValueNode>()
                .Where(node => node.ContainingSectionLineId == section.PhysicalLineId &&
                               node.Key.Span.SequenceEqual(expected))
                .ToArray());
        }

        internal byte[] CopyOriginalBytes()
        {
            return store.Copy(0, store.Length);
        }

        internal ReadOnlySpan<byte> OriginalSpan => store.GetSpan(0, store.Length);

        private static void ValidateComparison(IniRawAsciiComparison comparison)
        {
            if (comparison != IniRawAsciiComparison.Ordinal)
            {
                throw new ArgumentOutOfRangeException(nameof(comparison));
            }
        }
    }
}
