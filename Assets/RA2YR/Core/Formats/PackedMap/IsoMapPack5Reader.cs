using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RA2YR.Core.Binary;
using RA2YR.Core.Binary.Seekable;
using RA2YR.Core.Content;
using RA2YR.Core.Formats.Ini;

namespace RA2YR.Core.Formats.PackedMap
{
    internal sealed class IsoMapPack5RecordReader
    {
        private const int RecordWidth = 11;

        public IsoMapPack5RecordReadResult Read(
            byte[] input,
            IsoMapPack5TrailingPolicy trailingPolicy = IsoMapPack5TrailingPolicy.RejectAnyRemainder,
            IsoMapPack5ReadLimits limits = null,
            long absoluteOffset = 0,
            BinarySourceContext source = null,
            IEnumerable<IniSourceProvenance> provenance = null)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            if (absoluteOffset < 0) throw new ArgumentOutOfRangeException(nameof(absoluteOffset));
            limits = limits ?? new IsoMapPack5ReadLimits();
            source = source ?? SyntheticSource();
            IReadOnlyList<IniSourceProvenance> chain = NormalizeProvenance(provenance, source);
            if (input.LongLength > limits.MaxInputBytes)
            {
                return Failure(limits, source, chain, IsoMapDiagnosticCode.InputBudgetExceeded,
                    absoluteOffset, -1, null, "record", "Record input exceeds the configured byte budget.");
            }

            var diagnostics = new List<IsoMapDiagnostic>();
            var execution = new IsoMapExecutionState();
            var records = new List<IsoMapPack5RecordRaw>();
            int fullRecordCount = input.Length / RecordWidth;
            int remainder = input.Length % RecordWidth;
            if (fullRecordCount > limits.MaxRecords)
            {
                Add(diagnostics, limits, execution, Error(source, chain, IsoMapDiagnosticCode.RecordBudgetExceeded,
                    absoluteOffset, -1, null, "record", "Record count exceeds the configured budget."));
                return new IsoMapPack5RecordReadResult(records, null, diagnostics, execution);
            }

            for (int ordinal = 0; ordinal < fullRecordCount; ordinal++)
            {
                long recordOffset;
                try { recordOffset = checked(absoluteOffset + checked((long)ordinal * RecordWidth)); }
                catch (OverflowException)
                {
                    Add(diagnostics, limits, execution, Error(source, chain, IsoMapDiagnosticCode.CoordinateArithmeticOverflow,
                        absoluteOffset, ordinal, null, "record", "Record offset arithmetic overflowed."));
                    return new IsoMapPack5RecordReadResult(records, null, diagnostics, execution);
                }
                byte[] raw = new byte[RecordWidth];
                Buffer.BlockCopy(input, checked(ordinal * RecordWidth), raw, 0, RecordWidth);
                records.Add(new IsoMapPack5RecordRaw(ordinal, recordOffset, raw, chain));
            }

            IsoMapPack5TrailingData trailing = null;
            if (remainder != 0)
            {
                long trailingOffset;
                try
                {
                    trailingOffset = checked(absoluteOffset + checked((long)fullRecordCount * RecordWidth));
                }
                catch (OverflowException)
                {
                    Add(diagnostics, limits, execution, Error(source, chain, IsoMapDiagnosticCode.CoordinateArithmeticOverflow,
                        absoluteOffset, -1, null, "trailing", "Trailing offset arithmetic overflowed."));
                    return new IsoMapPack5RecordReadResult(records, null, diagnostics, execution);
                }
                if (remainder > limits.MaxTrailingBytes)
                {
                    Add(diagnostics, limits, execution, Error(source, chain, IsoMapDiagnosticCode.TrailingBudgetExceeded,
                        trailingOffset, -1, null, "trailing", "Trailing bytes exceed the configured budget."));
                    return new IsoMapPack5RecordReadResult(records, null, diagnostics, execution);
                }

                byte[] trailingBytes = new byte[remainder];
                Buffer.BlockCopy(input, fullRecordCount * RecordWidth, trailingBytes, 0, remainder);
                if (trailingPolicy == IsoMapPack5TrailingPolicy.RejectAnyRemainder)
                {
                    Add(diagnostics, limits, execution, Error(source, chain, IsoMapDiagnosticCode.UnexpectedTrailingBytes,
                        trailingOffset, -1, null, "trailing", "Decoded stream is not an exact multiple of 11 bytes."));
                    trailing = new IsoMapPack5TrailingData(trailingOffset, trailingBytes, IsoMapTrailingClassification.RejectedRemainder);
                }
                else if (trailingPolicy == IsoMapPack5TrailingPolicy.AllowExactFourZeroTrailer)
                {
                    if (remainder == 4 && trailingBytes.All(item => item == 0))
                    {
                        trailing = new IsoMapPack5TrailingData(trailingOffset, trailingBytes, IsoMapTrailingClassification.ExactFourZeroTrailer);
                    }
                    else
                    {
                        Add(diagnostics, limits, execution, Error(source, chain, IsoMapDiagnosticCode.InvalidFourZeroTrailer,
                            trailingOffset, -1, null, "trailing", "Only an exact four-byte all-zero trailer is permitted."));
                        trailing = new IsoMapPack5TrailingData(trailingOffset, trailingBytes, IsoMapTrailingClassification.RejectedRemainder);
                    }
                }
                else if (trailingPolicy == IsoMapPack5TrailingPolicy.PreserveRemainderWithDiagnostic)
                {
                    Add(diagnostics, limits, execution, Warning(source, chain, IsoMapDiagnosticCode.UnexpectedTrailingBytes,
                        trailingOffset, -1, null, "trailing", "Trailing bytes were preserved under the explicit preserve policy."));
                    trailing = new IsoMapPack5TrailingData(trailingOffset, trailingBytes, IsoMapTrailingClassification.PreservedRemainder);
                }
                else
                {
                    throw new ArgumentOutOfRangeException(nameof(trailingPolicy));
                }
            }

            return new IsoMapPack5RecordReadResult(records, trailing, diagnostics, execution);
        }

        public IsoMapPack5RecordReadResult Read(
            Stream stream,
            long length,
            BinarySourceContext source,
            IsoMapPack5TrailingPolicy trailingPolicy = IsoMapPack5TrailingPolicy.RejectAnyRemainder,
            IsoMapPack5ReadLimits limits = null,
            IEnumerable<IniSourceProvenance> provenance = null)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));
            if (source == null) throw new ArgumentNullException(nameof(source));
            limits = limits ?? new IsoMapPack5ReadLimits();
            try
            {
                byte[] input = PackedMapBoundedInput.ReadStream(stream, length, source, limits.MaxInputBytes);
                return Read(input, trailingPolicy, limits, 0, source, provenance);
            }
            catch (ArgumentOutOfRangeException exception)
            {
                return Failure(limits, source, NormalizeProvenance(provenance, source), IsoMapDiagnosticCode.InputBudgetExceeded,
                    0, -1, null, "input", exception.Message);
            }
            catch (BinaryReadException exception)
            {
                return Failure(limits, source, NormalizeProvenance(provenance, source), IsoMapDiagnosticCode.IncompleteRecord,
                    exception.Diagnostic.AbsoluteOffset, -1, null, "input", exception.Message, exception.Diagnostic.Code);
            }
        }

        public IsoMapPack5RecordReadResult Read(
            ReadOnlyDataWindow window,
            IsoMapPack5TrailingPolicy trailingPolicy = IsoMapPack5TrailingPolicy.RejectAnyRemainder,
            IsoMapPack5ReadLimits limits = null,
            BinarySourceContext source = null,
            IEnumerable<IniSourceProvenance> provenance = null)
        {
            if (window == null) throw new ArgumentNullException(nameof(window));
            limits = limits ?? new IsoMapPack5ReadLimits();
            source = source ?? SyntheticSource();
            try
            {
                byte[] input = PackedMapBoundedInput.ReadWindow(window, "isomap-pack5", limits.MaxInputBytes);
                return Read(input, trailingPolicy, limits, window.AbsoluteStartOffset, source, provenance);
            }
            catch (ArgumentOutOfRangeException exception)
            {
                return Failure(limits, source, NormalizeProvenance(provenance, source), IsoMapDiagnosticCode.InputBudgetExceeded,
                    window.AbsoluteStartOffset, -1, null, "input", exception.Message);
            }
            catch (BinaryReadException exception)
            {
                return Failure(limits, source, NormalizeProvenance(provenance, source), IsoMapDiagnosticCode.IncompleteRecord,
                    exception.Diagnostic.AbsoluteOffset, -1, null, "input", exception.Message, exception.Diagnostic.Code);
            }
        }

        private static BinarySourceContext SyntheticSource()
        {
            return new BinarySourceContext("isomap-pack5-reader", "isomap-pack5-input", LogicalContentPath.Parse("isomap-pack5-input"));
        }

        private static IReadOnlyList<IniSourceProvenance> NormalizeProvenance(IEnumerable<IniSourceProvenance> provenance, BinarySourceContext source)
        {
            if (provenance == null)
                return new[] { new IniSourceProvenance(source.LogicalSourceId, new[] { source.LogicalPath }) };
            IniSourceProvenance[] chain = provenance.ToArray();
            if (chain.Length == 0 || chain.Any(item => item == null)) throw new ArgumentException("Provenance is required.", nameof(provenance));
            return chain;
        }

        private static IsoMapPack5RecordReadResult Failure(
            IsoMapPack5ReadLimits limits,
            BinarySourceContext source,
            IReadOnlyList<IniSourceProvenance> provenance,
            IsoMapDiagnosticCode code,
            long offset,
            int ordinal,
            IsoMapCoordinateKey? coordinate,
            string stage,
            string message,
            BinaryDiagnosticCode? binaryCode = null)
        {
            var diagnostics = new List<IsoMapDiagnostic>();
            var execution = new IsoMapExecutionState();
            Add(diagnostics, limits, execution, Error(source, provenance, code, offset, ordinal, coordinate, stage, message, binaryCode));
            return new IsoMapPack5RecordReadResult(Array.Empty<IsoMapPack5RecordRaw>(), null, diagnostics, execution);
        }

        private static void Add(IList<IsoMapDiagnostic> diagnostics, IsoMapPack5ReadLimits limits, IsoMapExecutionState execution, IsoMapDiagnostic diagnostic)
        {
            execution.Observe(diagnostic.Severity);
            if (diagnostics.Count < limits.MaxDiagnostics) diagnostics.Add(diagnostic);
            else execution.SuppressOne();
        }

        private static IsoMapDiagnostic Error(BinarySourceContext source, IEnumerable<IniSourceProvenance> provenance, IsoMapDiagnosticCode code, long offset, int ordinal, IsoMapCoordinateKey? coordinate, string stage, string message, BinaryDiagnosticCode? binaryCode = null)
            => new IsoMapDiagnostic(BinaryDiagnosticSeverity.Error, code, source, provenance, offset, ordinal, coordinate, stage, message, binaryCode);

        private static IsoMapDiagnostic Warning(BinarySourceContext source, IEnumerable<IniSourceProvenance> provenance, IsoMapDiagnosticCode code, long offset, int ordinal, IsoMapCoordinateKey? coordinate, string stage, string message)
            => new IsoMapDiagnostic(BinaryDiagnosticSeverity.Warning, code, source, provenance, offset, ordinal, coordinate, stage, message);
    }
}
