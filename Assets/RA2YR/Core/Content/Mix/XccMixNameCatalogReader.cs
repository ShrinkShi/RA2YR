using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RA2YR.Core.Binary;
using RA2YR.Core.Formats.Mix;

namespace RA2YR.Core.Content.Mix
{
    internal sealed class XccMixNameCatalogLimits
    {
        public XccMixNameCatalogLimits(
            long maxInputBytes,
            long maxRecords,
            int maxStringLength,
            long maxAllocatedBytes)
        {
            if (maxInputBytes < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxInputBytes));
            }

            if (maxRecords < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxRecords));
            }

            if (maxStringLength < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxStringLength));
            }

            if (maxAllocatedBytes < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxAllocatedBytes));
            }

            MaxInputBytes = maxInputBytes;
            MaxRecords = maxRecords;
            MaxStringLength = maxStringLength;
            MaxAllocatedBytes = maxAllocatedBytes;
        }

        public static XccMixNameCatalogLimits Default { get; } =
            new XccMixNameCatalogLimits(
                16L * 1024 * 1024,
                100_000,
                4096,
                64L * 1024 * 1024);

        public long MaxInputBytes { get; }

        public long MaxRecords { get; }

        public int MaxStringLength { get; }

        public long MaxAllocatedBytes { get; }
    }

    internal enum XccMixNameCatalogDiagnosticCode
    {
        InputBudgetExceeded,
        TruncatedInput,
        InvalidListCount,
        RecordBudgetExceeded,
        StringBudgetExceeded,
        AllocationBudgetExceeded,
        UnterminatedString,
        NonAsciiString,
        UnsafeCandidateName,
        TrailingData,
        BinaryReadFailure
    }

    internal sealed class XccMixNameCatalogDiagnostic
    {
        public XccMixNameCatalogDiagnostic(
            XccMixNameCatalogDiagnosticCode code,
            long absoluteOffset,
            int listIndex,
            int recordIndex,
            string field,
            BinaryDiagnostic binaryDiagnostic = null)
        {
            Code = code;
            AbsoluteOffset = absoluteOffset;
            ListIndex = listIndex;
            RecordIndex = recordIndex;
            Field = BinaryDiagnosticLabel.Validate(field, nameof(field));
            BinaryDiagnostic = binaryDiagnostic;
        }

        public XccMixNameCatalogDiagnosticCode Code { get; }

        public long AbsoluteOffset { get; }

        public int ListIndex { get; }

        public int RecordIndex { get; }

        public string Field { get; }

        public BinaryDiagnostic BinaryDiagnostic { get; }
    }

    internal sealed class XccMixNameCatalogReadResult
    {
        private XccMixNameCatalogReadResult(
            MixNameCatalog catalog,
            IEnumerable<LogicalContentPath> names,
            IEnumerable<XccMixNameCatalogDiagnostic> diagnostics)
        {
            Catalog = catalog;
            Names = Array.AsReadOnly(
                (names ?? Enumerable.Empty<LogicalContentPath>()).ToArray());
            Diagnostics = Array.AsReadOnly(
                (diagnostics ?? Enumerable.Empty<XccMixNameCatalogDiagnostic>()).ToArray());
            IsSuccess = Catalog != null && Diagnostics.Count == 0;
            if (!IsSuccess && (Catalog != null || Names.Count != 0))
            {
                throw new ArgumentException(
                    "A failed name-catalog parse cannot expose partial names.");
            }
        }

        public bool IsSuccess { get; }

        public MixNameCatalog Catalog { get; }

        public IReadOnlyList<LogicalContentPath> Names { get; }

        public IReadOnlyList<XccMixNameCatalogDiagnostic> Diagnostics { get; }

        public static XccMixNameCatalogReadResult Success(
            MixNameCatalog catalog,
            IEnumerable<LogicalContentPath> names)
        {
            return new XccMixNameCatalogReadResult(
                catalog ?? throw new ArgumentNullException(nameof(catalog)),
                names,
                Array.Empty<XccMixNameCatalogDiagnostic>());
        }

        public static XccMixNameCatalogReadResult Failure(
            XccMixNameCatalogDiagnostic diagnostic)
        {
            return new XccMixNameCatalogReadResult(
                null,
                Array.Empty<LogicalContentPath>(),
                new[]
                {
                    diagnostic ?? throw new ArgumentNullException(nameof(diagnostic))
                });
        }
    }

    internal static class XccMixNameCatalogReader
    {
        private const int ListCount = 4;
        private const int Ra2ListIndex = 3;
        private const int CandidateAllocationEstimate = 160;

        public static XccMixNameCatalogReadResult Read(
            ReadOnlyMemory<byte> input,
            BinarySourceContext source,
            XccMixNameCatalogLimits limits = null)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            XccMixNameCatalogLimits effectiveLimits =
                limits ?? XccMixNameCatalogLimits.Default;
            var binaryLimits = new BinaryReadLimits(
                effectiveLimits.MaxInputBytes,
                8,
                effectiveLimits.MaxAllocatedBytes,
                effectiveLimits.MaxRecords,
                effectiveLimits.MaxStringLength,
                0,
                0);

            BinaryReadSession session;
            try
            {
                session = BinaryReadSession.FromMemory(input, source, binaryLimits);
            }
            catch (BinaryReadException exception)
            {
                return XccMixNameCatalogReadResult.Failure(
                    new XccMixNameCatalogDiagnostic(
                        MapBinaryCode(exception.Diagnostic.Code),
                        exception.Diagnostic.AbsoluteOffset,
                        -1,
                        -1,
                        "input",
                        exception.Diagnostic));
            }

            using (session)
            {
                BoundedBinaryReader reader = session.Root;
                int listIndex = -1;
                int recordIndex = -1;
                string field = "list-count";
                try
                {
                    session.ReserveAllocation(
                        effectiveLimits.MaxStringLength,
                        reader.AbsoluteOffset,
                        reader.RemainingLength,
                        "string-scratch");
                    byte[] scratch = new byte[effectiveLimits.MaxStringLength];
                    var names = new List<LogicalContentPath>();
                    for (listIndex = 0; listIndex < ListCount; listIndex++)
                    {
                        recordIndex = -1;
                        field = "list-count";
                        long countOffset = reader.AbsoluteOffset;
                        int count = reader.ReadInt32(field);
                        if (count < 0)
                        {
                            return Failure(
                                XccMixNameCatalogDiagnosticCode.InvalidListCount,
                                countOffset,
                                listIndex,
                                recordIndex,
                                field);
                        }

                        reader.ReserveRecords(count, "list-records");
                        for (recordIndex = 0; recordIndex < count; recordIndex++)
                        {
                            field = "record-name";
                            string name = ReadNullTerminatedAscii(
                                session,
                                reader,
                                scratch,
                                listIndex == Ra2ListIndex,
                                listIndex,
                                recordIndex,
                                field);
                            field = "record-description";
                            ReadNullTerminatedAscii(
                                session,
                                reader,
                                scratch,
                                false,
                                listIndex,
                                recordIndex,
                                field);

                            if (listIndex != Ra2ListIndex)
                            {
                                continue;
                            }

                            session.ReserveAllocation(
                                CandidateAllocationEstimate,
                                reader.AbsoluteOffset,
                                reader.RemainingLength,
                                "name-candidate");
                            string normalized = name.Replace('\\', '/');
                            LogicalContentPath logicalName;
                            string failureReason;
                            if (!LogicalContentPath.TryParse(
                                    normalized,
                                    out logicalName,
                                    out failureReason))
                            {
                                return Failure(
                                    XccMixNameCatalogDiagnosticCode.UnsafeCandidateName,
                                    reader.AbsoluteOffset,
                                    listIndex,
                                    recordIndex,
                                    "record-name");
                            }

                            try
                            {
                                names.Add(logicalName);
                                MixFileId.ComputeCandidateId(name);
                            }
                            catch (ArgumentException)
                            {
                                return Failure(
                                    XccMixNameCatalogDiagnosticCode.UnsafeCandidateName,
                                    reader.AbsoluteOffset,
                                    listIndex,
                                    recordIndex,
                                    "record-name");
                            }
                        }
                    }

                    field = "trailing-data";
                    BinaryParseCompletion completion = reader.Complete(
                        TrailingDataPolicy.RequireFullyConsumed,
                        field);
                    if (!completion.IsComplete)
                    {
                        BinaryDiagnostic trailing = completion.Diagnostics.Single();
                        return XccMixNameCatalogReadResult.Failure(
                            new XccMixNameCatalogDiagnostic(
                                XccMixNameCatalogDiagnosticCode.TrailingData,
                                trailing.AbsoluteOffset,
                                listIndex,
                                recordIndex,
                                field,
                                trailing));
                    }

                    return XccMixNameCatalogReadResult.Success(
                        new MixNameCatalog(names),
                        names);
                }
                catch (XccCatalogParseException exception)
                {
                    return XccMixNameCatalogReadResult.Failure(exception.Diagnostic);
                }
                catch (BinaryReadException exception)
                {
                    return XccMixNameCatalogReadResult.Failure(
                        new XccMixNameCatalogDiagnostic(
                            MapBinaryCode(exception.Diagnostic.Code),
                            exception.Diagnostic.AbsoluteOffset,
                            listIndex,
                            recordIndex,
                            field,
                            exception.Diagnostic));
                }
                catch (OutOfMemoryException)
                {
                    return Failure(
                        XccMixNameCatalogDiagnosticCode.AllocationBudgetExceeded,
                        reader.AbsoluteOffset,
                        listIndex,
                        recordIndex,
                        field);
                }
            }
        }

        private static string ReadNullTerminatedAscii(
            BinaryReadSession session,
            BoundedBinaryReader reader,
            byte[] scratch,
            bool capture,
            int listIndex,
            int recordIndex,
            string field)
        {
            long startOffset = reader.AbsoluteOffset;
            int length = 0;
            while (true)
            {
                if (reader.IsEndOfInput)
                {
                    throw new XccCatalogParseException(
                        new XccMixNameCatalogDiagnostic(
                            XccMixNameCatalogDiagnosticCode.UnterminatedString,
                            startOffset,
                            listIndex,
                            recordIndex,
                            field));
                }

                long characterOffset = reader.AbsoluteOffset;
                byte value = reader.ReadUInt8(field);
                if (value == 0)
                {
                    break;
                }

                if (value > 0x7f)
                {
                    throw new XccCatalogParseException(
                        new XccMixNameCatalogDiagnostic(
                            XccMixNameCatalogDiagnosticCode.NonAsciiString,
                            characterOffset,
                            listIndex,
                            recordIndex,
                            field));
                }

                length = checked(length + 1);
                reader.ValidateStringLength(length, field);
                if (capture)
                {
                    scratch[length - 1] = value;
                }
            }

            if (!capture)
            {
                return null;
            }

            long stringAllocation = checked((long)length * sizeof(char));
            session.ReserveAllocation(
                stringAllocation,
                startOffset,
                reader.RemainingLength,
                field);
            return Encoding.ASCII.GetString(scratch, 0, length);
        }

        private static XccMixNameCatalogReadResult Failure(
            XccMixNameCatalogDiagnosticCode code,
            long offset,
            int listIndex,
            int recordIndex,
            string field)
        {
            return XccMixNameCatalogReadResult.Failure(
                new XccMixNameCatalogDiagnostic(
                    code,
                    offset,
                    listIndex,
                    recordIndex,
                    field));
        }

        private static XccMixNameCatalogDiagnosticCode MapBinaryCode(
            BinaryDiagnosticCode code)
        {
            switch (code)
            {
                case BinaryDiagnosticCode.InputBudgetExceeded:
                    return XccMixNameCatalogDiagnosticCode.InputBudgetExceeded;
                case BinaryDiagnosticCode.UnexpectedEndOfInput:
                    return XccMixNameCatalogDiagnosticCode.TruncatedInput;
                case BinaryDiagnosticCode.RecordBudgetExceeded:
                    return XccMixNameCatalogDiagnosticCode.RecordBudgetExceeded;
                case BinaryDiagnosticCode.StringBudgetExceeded:
                    return XccMixNameCatalogDiagnosticCode.StringBudgetExceeded;
                case BinaryDiagnosticCode.AllocationBudgetExceeded:
                    return XccMixNameCatalogDiagnosticCode.AllocationBudgetExceeded;
                case BinaryDiagnosticCode.TrailingData:
                    return XccMixNameCatalogDiagnosticCode.TrailingData;
                default:
                    return XccMixNameCatalogDiagnosticCode.BinaryReadFailure;
            }
        }

        private sealed class XccCatalogParseException : Exception
        {
            public XccCatalogParseException(XccMixNameCatalogDiagnostic diagnostic)
            {
                Diagnostic = diagnostic ?? throw new ArgumentNullException(nameof(diagnostic));
            }

            public XccMixNameCatalogDiagnostic Diagnostic { get; }
        }
    }
}
