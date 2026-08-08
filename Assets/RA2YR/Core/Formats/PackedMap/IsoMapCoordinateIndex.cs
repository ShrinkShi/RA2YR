using System;
using System.Collections.Generic;
using System.Linq;
using RA2YR.Core.Binary;
using RA2YR.Core.Content;
using RA2YR.Core.Formats.Ini;

namespace RA2YR.Core.Formats.PackedMap
{
    internal sealed class IsoMapCoordinateIndexer
    {
        public IsoMapCoordinateAnalysis Analyze(
            IEnumerable<IsoMapPack5RecordRaw> records,
            IsoMapCoordinateDuplicatePolicy duplicatePolicy = IsoMapCoordinateDuplicatePolicy.PreserveAllAndDiagnose,
            IsoMapCoordinateValidationProfile profile = null,
            IsoMapPack5ReadLimits limits = null,
            BinarySourceContext source = null)
        {
            if (records == null) throw new ArgumentNullException(nameof(records));
            IsoMapPolicyValidation.ValidateDuplicatePolicy(duplicatePolicy, nameof(duplicatePolicy));
            limits = limits ?? new IsoMapPack5ReadLimits();
            source = source ?? new BinarySourceContext("isomap-coordinate-index", "isomap-pack5-input", LogicalContentPath.Parse("isomap-pack5-input"));
            var diagnostics = new List<IsoMapDiagnostic>();
            var execution = new IsoMapExecutionState();
            var occurrences = new List<IsoMapCoordinateOccurrence>();
            var groups = new List<IsoMapCoordinateDuplicateGroup>();
            var byKey = new Dictionary<IsoMapCoordinateKey, List<IsoMapCoordinateOccurrence>>();
            int ordinal = 0;
            foreach (IsoMapPack5RecordRaw record in records)
            {
                if (record == null)
                {
                    Add(diagnostics, limits, execution, Error(source, DefaultProvenance(source), IsoMapDiagnosticCode.NoProgress, -1, ordinal, null, "coordinate", "A null record cannot be indexed."));
                    break;
                }
                if (occurrences.Count >= limits.MaxCoordinateEntries)
                {
                    Add(diagnostics, limits, execution, Error(source, record.Provenance, IsoMapDiagnosticCode.CoordinateBudgetExceeded, record.SourceOffset, ordinal, null, "coordinate", "Coordinate occurrence budget exceeded."));
                    break;
                }
                IsoMapCoordinateKey key = new IsoMapCoordinateKey(record.XRawU16LittleEndian, record.YRawU16LittleEndian);
                bool outOfDomain = IsOutOfDomain(key, profile);
                if (outOfDomain)
                {
                    Add(diagnostics, limits, execution, Warning(source, record.Provenance, IsoMapDiagnosticCode.OutOfDomainCoordinate, record.SourceOffset, ordinal, key, "coordinate", "Coordinate is outside the explicit validation profile domain."));
                }
                List<IsoMapCoordinateOccurrence> list;
                if (!byKey.TryGetValue(key, out list))
                {
                    list = new List<IsoMapCoordinateOccurrence>();
                    byKey.Add(key, list);
                }
                var occurrence = new IsoMapCoordinateOccurrence(key, record.SourceOrdinal, list.Count == 0 ? record.SourceOrdinal : list[0].FirstOccurrenceOrdinal, outOfDomain, record);
                list.Add(occurrence);
                occurrences.Add(occurrence);
                ordinal++;
            }

            foreach (KeyValuePair<IsoMapCoordinateKey, List<IsoMapCoordinateOccurrence>> entry in byKey
                .OrderBy(item => item.Value[0].FirstOccurrenceOrdinal)
                .ThenBy(item => item.Key.XRaw)
                .ThenBy(item => item.Key.YRaw))
            {
                if (entry.Value.Count < 2) continue;
                if (groups.Count >= limits.MaxDuplicateGroups)
                {
                    Add(diagnostics, limits, execution, Error(source, entry.Value[0].Record.Provenance, IsoMapDiagnosticCode.CoordinateBudgetExceeded, entry.Value[0].Record.SourceOffset, entry.Value[0].SourceOrdinal, entry.Key, "coordinate", "Duplicate-group budget exceeded."));
                    break;
                }
                bool conflicting = entry.Value.Skip(1).Any(item => !item.Record.GetRawBytesCopy().SequenceEqual(entry.Value[0].Record.GetRawBytesCopy()));
                groups.Add(new IsoMapCoordinateDuplicateGroup(entry.Key, entry.Value, conflicting));
                Add(diagnostics, limits, execution, ErrorOrWarning(duplicatePolicy, conflicting, source, entry.Value[0].Record.Provenance, entry.Value[0].Record.SourceOffset, entry.Value[0].SourceOrdinal, entry.Key));
            }

            bool denseCandidate = profile != null && profile.ConfiguredDenseCountCandidate;
            if (denseCandidate && profile.Width.HasValue && profile.Height.HasValue)
            {
                try
                {
                    _ = checked((long)profile.Width.Value * profile.Height.Value);
                }
                catch (OverflowException)
                {
                    Add(diagnostics, limits, execution, Error(source, DefaultProvenance(source), IsoMapDiagnosticCode.CoordinateArithmeticOverflow, -1, -1, null, "coordinate", "Dense-count candidate arithmetic overflowed."));
                }
            }
            return new IsoMapCoordinateAnalysis(new IsoMapCoordinateIndex(occurrences, groups), diagnostics, denseCandidate, execution);
        }

        private static bool IsOutOfDomain(IsoMapCoordinateKey key, IsoMapCoordinateValidationProfile profile)
        {
            if (profile == null || !profile.Width.HasValue || !profile.Height.HasValue) return false;
            long first;
            long second;
            switch (profile.AxisOrder)
            {
                case IsoMapCoordinateAxisOrder.XThenY:
                    first = key.XRaw;
                    second = key.YRaw;
                    break;
                case IsoMapCoordinateAxisOrder.YThenX:
                    first = key.YRaw;
                    second = key.XRaw;
                    break;
                default:
                    throw new InvalidOperationException("Coordinate profile contains an invalid axis order.");
            }
            if (profile.Signedness == IsoMapCoordinateSignednessCandidate.Signed16Candidate)
            {
                first = unchecked((short)first);
                second = unchecked((short)second);
            }
            else if (profile.Signedness != IsoMapCoordinateSignednessCandidate.RawUnsigned)
            {
                throw new InvalidOperationException("Coordinate profile contains an invalid signedness candidate.");
            }
            return first < 0 || second < 0 || first >= profile.Width.Value || second >= profile.Height.Value;
        }

        private static IsoMapDiagnostic ErrorOrWarning(IsoMapCoordinateDuplicatePolicy policy, bool conflicting, BinarySourceContext source, IEnumerable<IniSourceProvenance> provenance, long offset, int ordinal, IsoMapCoordinateKey key)
        {
            IsoMapDiagnosticCode code = conflicting ? IsoMapDiagnosticCode.ConflictingDuplicateCoordinate : IsoMapDiagnosticCode.DuplicateCoordinate;
            BinaryDiagnosticSeverity severity = policy == IsoMapCoordinateDuplicatePolicy.RejectAnyDuplicate ||
                (policy == IsoMapCoordinateDuplicatePolicy.AllowByteIdenticalDuplicatesButDiagnose && conflicting)
                ? BinaryDiagnosticSeverity.Error
                : BinaryDiagnosticSeverity.Warning;
            return new IsoMapDiagnostic(severity, code, source, provenance, offset, ordinal, key, "coordinate", conflicting ? "Duplicate coordinate has conflicting raw payload." : "Duplicate coordinate occurrence was preserved.");
        }

        private static void Add(IList<IsoMapDiagnostic> diagnostics, IsoMapPack5ReadLimits limits, IsoMapExecutionState execution, IsoMapDiagnostic diagnostic)
        {
            execution.Observe(diagnostic.Severity);
            if (diagnostics.Count < limits.MaxDiagnostics) diagnostics.Add(diagnostic);
            else execution.SuppressOne();
        }

        private static IsoMapDiagnostic Error(BinarySourceContext source, IEnumerable<IniSourceProvenance> provenance, IsoMapDiagnosticCode code, long offset, int ordinal, IsoMapCoordinateKey? coordinate, string stage, string message)
            => new IsoMapDiagnostic(BinaryDiagnosticSeverity.Error, code, source, provenance.Any() ? provenance : new[] { new IniSourceProvenance(source.LogicalSourceId, new[] { source.LogicalPath }) }, offset, ordinal, coordinate, stage, message);

        private static IsoMapDiagnostic Warning(BinarySourceContext source, IEnumerable<IniSourceProvenance> provenance, IsoMapDiagnosticCode code, long offset, int ordinal, IsoMapCoordinateKey coordinate, string stage, string message)
            => new IsoMapDiagnostic(BinaryDiagnosticSeverity.Warning, code, source, provenance, offset, ordinal, coordinate, stage, message);

        private static IReadOnlyList<IniSourceProvenance> DefaultProvenance(BinarySourceContext source)
            => new[] { new IniSourceProvenance(source.LogicalSourceId, new[] { source.LogicalPath }) };
    }
}
