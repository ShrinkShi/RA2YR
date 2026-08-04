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
                return new IsoMapPack5PackedReadResult(null, null, null, new[] { diagnostic });
            }

            PackedSectionDecodeResult packed;
            try
            {
                packed = new PackedSectionDecodePipeline().Decode(occurrences, policy.PackedPolicy, lzoBackend);
            }
            catch (Exception exception)
            {
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
                return new IsoMapPack5PackedReadResult(null, null, null, new[] { diagnostic });
            }

            if (packed == null || !packed.IsSuccess || packed.DecodedBytes == null)
            {
                var diagnostics = new List<IsoMapDiagnostic>
                {
                    new IsoMapDiagnostic(
                        BinaryDiagnosticSeverity.Error,
                        packed == null ? IsoMapDiagnosticCode.PackedStageFailure : IsoMapDiagnosticCode.PackedStageFailure,
                        SyntheticSource(),
                        new[] { SyntheticProvenance() },
                        -1,
                        -1,
                        null,
                        "packed",
                        packed == null ? "Packed-section pipeline returned no result." : "Packed-section pipeline failed; record parsing was not attempted.")
                };
                return new IsoMapPack5PackedReadResult(packed, null, null, diagnostics);
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
            diagnosticsResult.AddRange(records.Diagnostics);
            if (coordinates != null)
                diagnosticsResult.AddRange(coordinates.Diagnostics);
            return new IsoMapPack5PackedReadResult(packed, records, coordinates, diagnosticsResult);
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
