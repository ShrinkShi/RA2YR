using System;
using System.Collections.Generic;
using System.Linq;
using RA2YR.Core.Binary;
using RA2YR.Core.Content;
using RA2YR.Core.Formats.Ini;

namespace RA2YR.Core.Formats.PackedMap
{
    internal sealed class IsoMapPack5PackedSectionReader
    {
        public IsoMapPack5PackedReadResult Read(
            IEnumerable<PackedIniFragmentOccurrence> occurrences,
            IsoMapPack5PackedReadPolicy policy,
            ILzoDecodeBackend lzoBackend = null)
        {
            if (occurrences == null) throw new ArgumentNullException(nameof(occurrences));
            if (policy == null) throw new ArgumentNullException(nameof(policy));
            if (policy.PackedPolicy.Codec != PackedCodecKind.RawLzo1X)
            {
                var diagnostic = new IsoMapDiagnostic(
                    BinaryDiagnosticSeverity.Error,
                    IsoMapDiagnosticCode.WrongCodec,
                    SyntheticSource(),
                    new[] { SyntheticProvenance() },
                    -1,
                    -1,
                    null,
                    "packed",
                    "IsoMapPack5 packed sections require the explicit RawLzo1X codec policy.");
                var execution = new IsoMapExecutionState();
                var diagnostics = new List<IsoMapDiagnostic>();
                Add(diagnostics, policy.Limits, execution, diagnostic);
                return new IsoMapPack5PackedReadResult(null, null, null, diagnostics, execution);
            }
            if (lzoBackend == null)
            {
                var execution = new IsoMapExecutionState();
                var diagnostics = new List<IsoMapDiagnostic>();
                Add(diagnostics, policy.Limits, execution, new IsoMapDiagnostic(
                    BinaryDiagnosticSeverity.Error,
                    IsoMapDiagnosticCode.BackendUnavailable,
                    SyntheticSource(),
                    new[] { SyntheticProvenance() },
                    -1,
                    -1,
                    null,
                    "packed",
                    "IsoMapPack5 RawLzo1X decoding requires an injected backend."));
                return new IsoMapPack5PackedReadResult(null, null, null, diagnostics, execution);
            }

            PackedSectionDecodeResult packed;
            IEnumerator<PackedIniFragmentOccurrence> occurrenceEnumerator = null;
            try
            {
                occurrenceEnumerator = occurrences.GetEnumerator();
                if (!occurrenceEnumerator.MoveNext())
                {
                    occurrenceEnumerator.Dispose();
                    occurrenceEnumerator = null;
                    return PackedFailure(
                        null,
                        policy.Limits,
                        IsoMapDiagnosticCode.EmptyPackedInput,
                        "IsoMapPack5 packed input must contain at least one fragment occurrence.");
                }

                packed = new PackedSectionDecodePipeline().Decode(
                    ReplayOccurrences(occurrenceEnumerator.Current, occurrenceEnumerator),
                    policy.PackedPolicy,
                    lzoBackend);
                occurrenceEnumerator = null;
            }
            catch (Exception exception)
            {
                if (occurrenceEnumerator != null)
                    occurrenceEnumerator.Dispose();
                var diagnostic = new IsoMapDiagnostic(
                    BinaryDiagnosticSeverity.Error,
                    IsoMapDiagnosticCode.PackedStageFailure,
                    SyntheticSource(),
                    new[] { SyntheticProvenance() },
                    -1,
                    -1,
                    null,
                    "packed",
                    "Packed-section decoding failed before record parsing: " + exception.GetType().Name);
                var execution = new IsoMapExecutionState();
                var diagnostics = new List<IsoMapDiagnostic>();
                Add(diagnostics, policy.Limits, execution, diagnostic);
                return new IsoMapPack5PackedReadResult(null, null, null, diagnostics, execution);
            }

            if (packed == null || !packed.IsSuccess || packed.DecodedBytes == null)
            {
                var diagnostics = new List<IsoMapDiagnostic>();
                var execution = new IsoMapExecutionState();
                Add(diagnostics, policy.Limits, execution, new IsoMapDiagnostic(
                    BinaryDiagnosticSeverity.Error,
                    IsoMapDiagnosticCode.PackedStageFailure,
                    SyntheticSource(),
                    new[] { SyntheticProvenance() },
                    -1,
                    -1,
                    null,
                    "packed",
                    packed == null ? "Packed-section pipeline returned no result." : "Packed-section pipeline failed; record parsing was not attempted."));
                return new IsoMapPack5PackedReadResult(packed, null, null, diagnostics, execution);
            }
            if (packed.Envelope == null || packed.Envelope.Blocks.Count == 0)
            {
                return PackedFailure(
                    packed,
                    policy.Limits,
                    IsoMapDiagnosticCode.EmptyChunkEnvelope,
                    "IsoMapPack5 packed input must contain at least one decoded chunk block.");
            }

            IReadOnlyList<IniSourceProvenance> provenance = packed.Envelope != null && packed.Envelope.Blocks.Count != 0
                ? packed.Envelope.Blocks[0].Provenance
                : packed.Fragments != null && packed.Fragments.Occurrences.Count != 0
                    ? new[] { packed.Fragments.Occurrences[0].Provenance }
                    : new[] { SyntheticProvenance() };

            BinarySourceContext source = new BinarySourceContext(
                "isomap-pack5-packed-reader",
                provenance[0].SourceId,
                provenance[0].LogicalChain[provenance[0].LogicalChain.Count - 1]);
            IsoMapPack5RecordReadResult records = new IsoMapPack5RecordReader().Read(
                packed.DecodedBytes,
                policy.TrailingPolicy,
                policy.Limits,
                0,
                source,
                provenance);
            IsoMapCoordinateAnalysis coordinates = records.IsSuccess
                ? new IsoMapCoordinateIndexer().Analyze(
                    records.Records,
                    policy.DuplicatePolicy,
                    policy.CoordinateProfile,
                    policy.Limits,
                    source)
                : null;
            var diagnosticsResult = new List<IsoMapDiagnostic>();
            var resultExecution = new IsoMapExecutionState();
            Append(diagnosticsResult, policy.Limits, resultExecution, records.Diagnostics);
            if (coordinates != null)
                Append(diagnosticsResult, policy.Limits, resultExecution, coordinates.Diagnostics);
            return new IsoMapPack5PackedReadResult(packed, records, coordinates, diagnosticsResult, resultExecution);
        }

        private static IsoMapPack5PackedReadResult PackedFailure(
            PackedSectionDecodeResult packed,
            IsoMapPack5ReadLimits limits,
            IsoMapDiagnosticCode code,
            string message)
        {
            var diagnostics = new List<IsoMapDiagnostic>();
            var execution = new IsoMapExecutionState();
            Add(diagnostics, limits, execution, new IsoMapDiagnostic(
                BinaryDiagnosticSeverity.Error,
                code,
                SyntheticSource(),
                new[] { SyntheticProvenance() },
                -1,
                -1,
                null,
                "packed",
                message));
            return new IsoMapPack5PackedReadResult(packed, null, null, diagnostics, execution);
        }

        private static IEnumerable<PackedIniFragmentOccurrence> ReplayOccurrences(
            PackedIniFragmentOccurrence first,
            IEnumerator<PackedIniFragmentOccurrence> remainder)
        {
            try
            {
                yield return first;
                while (remainder.MoveNext())
                    yield return remainder.Current;
            }
            finally
            {
                remainder.Dispose();
            }
        }

        private static void Append(IList<IsoMapDiagnostic> target, IsoMapPack5ReadLimits limits, IsoMapExecutionState execution, IEnumerable<IsoMapDiagnostic> diagnostics)
        {
            foreach (IsoMapDiagnostic diagnostic in diagnostics)
                Add(target, limits, execution, diagnostic);
        }

        private static void Add(IList<IsoMapDiagnostic> diagnostics, IsoMapPack5ReadLimits limits, IsoMapExecutionState execution, IsoMapDiagnostic diagnostic)
        {
            execution.Observe(diagnostic.Severity);
            if (diagnostics.Count < limits.MaxDiagnostics)
                diagnostics.Add(diagnostic);
            else
                execution.SuppressOne();
        }

        private static BinarySourceContext SyntheticSource()
        {
            return new BinarySourceContext("isomap-pack5-packed-reader", "isomap-pack5-input", LogicalContentPath.Parse("isomap-pack5-input"));
        }

        private static IniSourceProvenance SyntheticProvenance()
        {
            return new IniSourceProvenance("isomap-pack5-input", new[] { LogicalContentPath.Parse("isomap-pack5-input") });
        }
    }
}
