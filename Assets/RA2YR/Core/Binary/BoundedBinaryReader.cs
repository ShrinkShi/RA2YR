using System;
using System.Collections.Generic;
using System.Linq;

namespace RA2YR.Core.Binary
{
    internal enum TrailingDataPolicy
    {
        RequireFullyConsumed,
        AllowWithWarning,
        PreserveOpaque,
        DeferToFormat
    }

    internal sealed class BinaryParseCompletion
    {
        internal BinaryParseCompletion(
            bool isComplete,
            bool requiresTrailingDataDecision,
            IReadOnlyList<BinaryDiagnostic> diagnostics,
            ReadOnlyMemory<byte> opaqueTrailingData)
        {
            if (diagnostics == null)
            {
                throw new ArgumentNullException(nameof(diagnostics));
            }

            if (diagnostics.Any(item => item == null))
            {
                throw new ArgumentException(
                    "Completion diagnostics cannot contain null.",
                    nameof(diagnostics));
            }

            if (isComplete && (requiresTrailingDataDecision || diagnostics.Any(
                    item => item.Severity == BinaryDiagnosticSeverity.Error)))
            {
                throw new ArgumentException(
                    "A complete parse cannot contain errors or a deferred tail decision.",
                    nameof(isComplete));
            }

            IsComplete = isComplete;
            RequiresTrailingDataDecision = requiresTrailingDataDecision;
            Diagnostics = diagnostics;
            OpaqueTrailingData = opaqueTrailingData;
        }

        public bool IsComplete { get; }

        public bool RequiresTrailingDataDecision { get; }

        public bool HasErrors => Diagnostics.Any(
            item => item.Severity == BinaryDiagnosticSeverity.Error);

        public IReadOnlyList<BinaryDiagnostic> Diagnostics { get; }

        public ReadOnlyMemory<byte> OpaqueTrailingData { get; }
    }

    internal sealed class BoundedBinaryReader
    {
        private static readonly IReadOnlyList<BinaryDiagnostic> EmptyDiagnostics =
            Array.AsReadOnly(Array.Empty<BinaryDiagnostic>());

        private readonly BinaryReadSession session;
        private readonly int memoryOffset;
        private readonly int length;
        private readonly int depth;
        private readonly BoundedBinaryReader parent;
        private long position;
        private int incompleteChildren;
        private bool completionFinalized;

        internal BoundedBinaryReader(
            BinaryReadSession session,
            int memoryOffset,
            int length,
            long absoluteStartOffset,
            int depth,
            BoundedBinaryReader parent)
        {
            this.session = session ?? throw new ArgumentNullException(nameof(session));
            this.memoryOffset = memoryOffset;
            this.length = length;
            AbsoluteStartOffset = absoluteStartOffset;
            this.depth = depth;
            this.parent = parent;
        }

        public long Position => position;

        public long AbsoluteStartOffset { get; }

        public long AbsoluteOffset => checked(AbsoluteStartOffset + position);

        public long Length => length;

        public long RemainingLength => length - position;

        public bool IsEndOfInput => RemainingLength == 0;

        public byte ReadUInt8(string fieldOrSection)
        {
            ReadOnlySpan<byte> bytes = GetSpanAndAdvance(1, fieldOrSection);
            return bytes[0];
        }

        public sbyte ReadInt8(string fieldOrSection)
        {
            return unchecked((sbyte)ReadUInt8(fieldOrSection));
        }

        public ushort ReadUInt16(string fieldOrSection)
        {
            ReadOnlySpan<byte> bytes = GetSpanAndAdvance(2, fieldOrSection);
            return (ushort)(bytes[0] | (bytes[1] << 8));
        }

        public short ReadInt16(string fieldOrSection)
        {
            return unchecked((short)ReadUInt16(fieldOrSection));
        }

        public uint ReadUInt32(string fieldOrSection)
        {
            ReadOnlySpan<byte> bytes = GetSpanAndAdvance(4, fieldOrSection);
            return (uint)bytes[0] |
                   ((uint)bytes[1] << 8) |
                   ((uint)bytes[2] << 16) |
                   ((uint)bytes[3] << 24);
        }

        public int ReadInt32(string fieldOrSection)
        {
            return unchecked((int)ReadUInt32(fieldOrSection));
        }

        public ulong ReadUInt64(string fieldOrSection)
        {
            ReadOnlySpan<byte> bytes = GetSpanAndAdvance(8, fieldOrSection);
            uint low = (uint)bytes[0] |
                       ((uint)bytes[1] << 8) |
                       ((uint)bytes[2] << 16) |
                       ((uint)bytes[3] << 24);
            uint high = (uint)bytes[4] |
                        ((uint)bytes[5] << 8) |
                        ((uint)bytes[6] << 16) |
                        ((uint)bytes[7] << 24);
            return low | ((ulong)high << 32);
        }

        public long ReadInt64(string fieldOrSection)
        {
            return unchecked((long)ReadUInt64(fieldOrSection));
        }

        public byte[] ReadBytes(long byteCount, string fieldOrSection)
        {
            ReadOnlySpan<byte> source = GetSpan(byteCount, fieldOrSection);
            session.ReserveAllocation(
                byteCount,
                AbsoluteOffset,
                RemainingLength,
                fieldOrSection);
            int count = ConvertValidatedLength(byteCount, fieldOrSection);
            if (count == 0)
            {
                return Array.Empty<byte>();
            }

            byte[] result;
            try
            {
                result = new byte[count];
            }
            catch (OutOfMemoryException)
            {
                session.Throw(
                    BinaryDiagnosticCode.ReadFailure,
                    AbsoluteOffset,
                    byteCount,
                    RemainingLength,
                    fieldOrSection,
                    "The validated byte result could not be allocated.");
                throw;
            }

            source.CopyTo(result);
            position += count;
            return result;
        }

        public byte[] PeekBytes(long byteCount, string fieldOrSection)
        {
            ReadOnlySpan<byte> source = GetSpan(byteCount, fieldOrSection);
            session.ReserveAllocation(
                byteCount,
                AbsoluteOffset,
                RemainingLength,
                fieldOrSection);
            int count = ConvertValidatedLength(byteCount, fieldOrSection);
            if (count == 0)
            {
                return Array.Empty<byte>();
            }

            byte[] result;
            try
            {
                result = new byte[count];
            }
            catch (OutOfMemoryException)
            {
                session.Throw(
                    BinaryDiagnosticCode.ReadFailure,
                    AbsoluteOffset,
                    byteCount,
                    RemainingLength,
                    fieldOrSection,
                    "The validated peek result could not be allocated.");
                throw;
            }

            source.CopyTo(result);
            return result;
        }

        public void Skip(long byteCount, string fieldOrSection)
        {
            EnsureAvailable(byteCount, fieldOrSection);
            position += byteCount;
        }

        public BoundedBinaryReader ReadSubrange(long byteCount, string fieldOrSection)
        {
            BoundedBinaryReader child = CreateSubrangeAt(
                position,
                byteCount,
                fieldOrSection);
            position += byteCount;
            return child;
        }

        public BoundedBinaryReader CreateSubrangeAt(
            long relativeOffset,
            long byteCount,
            string fieldOrSection)
        {
            EnsureReaderActive();
            BinaryDiagnosticLabel.Validate(fieldOrSection, nameof(fieldOrSection));
            if (relativeOffset < 0)
            {
                session.Throw(
                    BinaryDiagnosticCode.InvalidOffset,
                    AbsoluteOffset,
                    byteCount,
                    RemainingLength,
                    fieldOrSection,
                    "A subrange offset cannot be negative.");
            }

            if (byteCount < 0)
            {
                session.Throw(
                    BinaryDiagnosticCode.InvalidLength,
                    AbsoluteStartOffset + Math.Min(relativeOffset, length),
                    byteCount,
                    length,
                    fieldOrSection,
                    "A subrange length cannot be negative.");
            }

            long end;
            try
            {
                end = checked(relativeOffset + byteCount);
            }
            catch (OverflowException)
            {
                session.Throw(
                    BinaryDiagnosticCode.ArithmeticOverflow,
                    AbsoluteOffset,
                    byteCount,
                    RemainingLength,
                    fieldOrSection,
                    "The subrange offset plus length overflows Int64.");
                return null;
            }

            if (relativeOffset > length)
            {
                session.Throw(
                    BinaryDiagnosticCode.InvalidOffset,
                    AbsoluteStartOffset + length,
                    byteCount,
                    0,
                    fieldOrSection,
                    "The subrange offset lies outside its parent range.");
            }

            if (end > length)
            {
                session.Throw(
                    BinaryDiagnosticCode.InvalidSubrange,
                    AbsoluteStartOffset + relativeOffset,
                    byteCount,
                    length - relativeOffset,
                    fieldOrSection,
                    "The subrange extends beyond its parent range.");
            }

            int childDepth;
            try
            {
                childDepth = checked(depth + 1);
            }
            catch (OverflowException)
            {
                session.Throw(
                    BinaryDiagnosticCode.ArithmeticOverflow,
                    AbsoluteStartOffset + relativeOffset,
                    byteCount,
                    length - relativeOffset,
                    fieldOrSection,
                    "The child subrange depth overflowed Int32.");
                return null;
            }

            session.ReserveSubrange(
                childDepth,
                byteCount,
                AbsoluteStartOffset + relativeOffset,
                length - relativeOffset,
                fieldOrSection);
            int childOffset = checked(memoryOffset + (int)relativeOffset);
            int childLength = checked((int)byteCount);
            BoundedBinaryReader child = new BoundedBinaryReader(
                session,
                childOffset,
                childLength,
                AbsoluteStartOffset + relativeOffset,
                childDepth,
                this);
            try
            {
                incompleteChildren = checked(incompleteChildren + 1);
            }
            catch (OverflowException)
            {
                session.Throw(
                    BinaryDiagnosticCode.ArithmeticOverflow,
                    AbsoluteStartOffset + relativeOffset,
                    byteCount,
                    length - relativeOffset,
                    fieldOrSection,
                    "The number of simultaneously incomplete child ranges overflowed.");
            }

            return child;
        }

        public void ReserveRecords(long count, string fieldOrSection)
        {
            EnsureReaderActive();
            session.ReserveRecords(
                count,
                AbsoluteOffset,
                RemainingLength,
                fieldOrSection);
        }

        public void ValidateStringLength(long length, string fieldOrSection)
        {
            EnsureReaderActive();
            session.ValidateStringLength(
                length,
                AbsoluteOffset,
                RemainingLength,
                fieldOrSection);
        }

        public BinaryParseCompletion Complete(
            TrailingDataPolicy policy,
            string fieldOrSection = "trailing-data")
        {
            session.EnsureUsable();
            BinaryDiagnosticLabel.Validate(fieldOrSection, nameof(fieldOrSection));
            if (completionFinalized)
            {
                throw new InvalidOperationException("This bounded reader is already complete.");
            }

            if (incompleteChildren != 0)
            {
                session.Throw(
                    BinaryDiagnosticCode.InvalidSubrange,
                    AbsoluteOffset,
                    0,
                    RemainingLength,
                    fieldOrSection,
                    "A bounded range cannot complete while a child range is incomplete.");
            }

            if (RemainingLength == 0)
            {
                FinalizeSuccessfulCompletion();
                return CreateCompletion(true, false, ReadOnlyMemory<byte>.Empty);
            }

            switch (policy)
            {
                case TrailingDataPolicy.RequireFullyConsumed:
                    BinaryDiagnostic error = session.Record(
                        BinaryDiagnosticSeverity.Error,
                        BinaryDiagnosticCode.TrailingData,
                        AbsoluteOffset,
                        RemainingLength,
                        RemainingLength,
                        fieldOrSection,
                        "The bounded range contains unconsumed trailing data.");
                    session.MarkFaulted();
                    completionFinalized = true;
                    return CreateCompletion(
                        false,
                        false,
                        ReadOnlyMemory<byte>.Empty,
                        error);

                case TrailingDataPolicy.AllowWithWarning:
                    BinaryDiagnostic warning = session.Record(
                        BinaryDiagnosticSeverity.Warning,
                        BinaryDiagnosticCode.TrailingData,
                        AbsoluteOffset,
                        RemainingLength,
                        RemainingLength,
                        fieldOrSection,
                        "The format explicitly allowed unconsumed trailing data.");
                    position = length;
                    FinalizeSuccessfulCompletion();
                    return CreateCompletion(
                        true,
                        false,
                        ReadOnlyMemory<byte>.Empty,
                        warning);

                case TrailingDataPolicy.PreserveOpaque:
                    byte[] opaque = ReadBytes(RemainingLength, fieldOrSection);
                    FinalizeSuccessfulCompletion();
                    return CreateCompletion(true, false, opaque);

                case TrailingDataPolicy.DeferToFormat:
                    return CreateCompletion(false, true, ReadOnlyMemory<byte>.Empty);

                default:
                    throw new ArgumentOutOfRangeException(nameof(policy));
            }
        }

        private BinaryParseCompletion CreateCompletion(
            bool isComplete,
            bool requiresTrailingDataDecision,
            ReadOnlyMemory<byte> opaqueTrailingData,
            BinaryDiagnostic completionDiagnostic = null)
        {
            IReadOnlyList<BinaryDiagnostic> completionDiagnostics;
            if (parent == null && completionFinalized)
            {
                completionDiagnostics = session.Diagnostics;
            }
            else if (completionDiagnostic == null)
            {
                completionDiagnostics = EmptyDiagnostics;
            }
            else
            {
                completionDiagnostics = Array.AsReadOnly(
                    new[] { completionDiagnostic });
            }

            bool containsErrors = completionDiagnostics.Any(
                item => item.Severity == BinaryDiagnosticSeverity.Error);
            return new BinaryParseCompletion(
                isComplete && !containsErrors,
                requiresTrailingDataDecision,
                completionDiagnostics,
                opaqueTrailingData);
        }

        private void FinalizeSuccessfulCompletion()
        {
            completionFinalized = true;
            if (parent != null)
            {
                parent.OnChildCompleted();
            }
            else
            {
                session.MarkCompleted();
            }
        }

        private void OnChildCompleted()
        {
            if (incompleteChildren <= 0)
            {
                throw new InvalidOperationException("No incomplete child range is registered.");
            }

            incompleteChildren--;
        }

        private ReadOnlySpan<byte> GetSpanAndAdvance(
            int byteCount,
            string fieldOrSection)
        {
            ReadOnlySpan<byte> result = GetSpan(byteCount, fieldOrSection);
            position += byteCount;
            return result;
        }

        private ReadOnlySpan<byte> GetSpan(long byteCount, string fieldOrSection)
        {
            EnsureAvailable(byteCount, fieldOrSection);
            int count = ConvertValidatedLength(byteCount, fieldOrSection);
            int offset = checked(memoryOffset + (int)position);
            return session.Memory.Span.Slice(offset, count);
        }

        private void EnsureAvailable(long byteCount, string fieldOrSection)
        {
            EnsureReaderActive();
            session.ValidateReadLength(
                byteCount,
                AbsoluteOffset,
                RemainingLength,
                fieldOrSection);
            long end;
            try
            {
                end = checked(position + byteCount);
            }
            catch (OverflowException)
            {
                session.Throw(
                    BinaryDiagnosticCode.ArithmeticOverflow,
                    AbsoluteOffset,
                    byteCount,
                    RemainingLength,
                    fieldOrSection,
                    "The current position plus read length overflows Int64.");
                return;
            }

            if (end > length)
            {
                session.Throw(
                    BinaryDiagnosticCode.UnexpectedEndOfInput,
                    AbsoluteOffset,
                    byteCount,
                    RemainingLength,
                    fieldOrSection,
                    "The requested read extends beyond the bounded range.");
            }
        }

        private void EnsureReaderActive()
        {
            session.EnsureUsable();
            if (completionFinalized)
            {
                throw new InvalidOperationException(
                    "This bounded reader is complete and cannot continue.");
            }
        }

        private int ConvertValidatedLength(long byteCount, string fieldOrSection)
        {
            try
            {
                return checked((int)byteCount);
            }
            catch (OverflowException)
            {
                session.Throw(
                    BinaryDiagnosticCode.ArithmeticOverflow,
                    AbsoluteOffset,
                    byteCount,
                    RemainingLength,
                    fieldOrSection,
                    "The validated read length cannot be represented by the input buffer.");
                return 0;
            }
        }
    }
}
