using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using RA2YR.Core.Binary;

namespace RA2YR.Core.Formats.PackedMap
{
    internal sealed class PackedIniFragmentCollector
    {
        public PackedIniFragmentCollection Collect(
            IEnumerable<PackedIniFragmentOccurrence> input,
            PackedIniFragmentOrderingPolicy policy,
            PackedIniFragmentCollectorLimits limits = null)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            limits = limits ?? new PackedIniFragmentCollectorLimits();
            var diagnostics = new List<PackedMapDiagnostic>();
            var accepted = new List<PackedIniFragmentOccurrence>();
            var seenPhysical = new HashSet<string>(StringComparer.Ordinal);
            long characters = 0;
            using (IEnumerator<PackedIniFragmentOccurrence> enumerator = input.GetEnumerator())
            {
                int index = 0;
                while (enumerator.MoveNext())
                {
                    if (index >= limits.MaxFragments)
                    {
                        diagnostics.Add(Error(PackedMapDiagnosticCode.FragmentBudgetExceeded, "Fragment count exceeds the configured budget."));
                        break;
                    }

                    PackedIniFragmentOccurrence occurrence = enumerator.Current;
                    if (occurrence == null)
                    {
                        diagnostics.Add(Error(PackedMapDiagnosticCode.DuplicateSourceOccurrence, "A null fragment occurrence is not valid."));
                        index++;
                        continue;
                    }

                    try
                    {
                        characters = checked(characters + occurrence.RawValue.Length);
                    }
                    catch (OverflowException)
                    {
                        diagnostics.Add(Error(PackedMapDiagnosticCode.AggregateCharacterBudgetExceeded, "Fragment character accounting overflowed.", occurrence));
                        break;
                    }
                    if (characters > limits.MaxCharacters)
                    {
                        diagnostics.Add(Error(PackedMapDiagnosticCode.AggregateCharacterBudgetExceeded, "Fragment characters exceed the configured budget."));
                        break;
                    }

                    string identity = occurrence.SourceId + "#" + occurrence.PhysicalLineId.ToString(CultureInfo.InvariantCulture);
                    if (!seenPhysical.Add(identity))
                    {
                        diagnostics.Add(Error(PackedMapDiagnosticCode.DuplicateSourceOccurrence, "The same source occurrence was supplied more than once.", occurrence));
                    }

                    if (occurrence.RawValue.Length == 0)
                        diagnostics.Add(Warning(PackedMapDiagnosticCode.EmptyFragmentValue, "An empty fragment value was preserved.", occurrence));
                    accepted.Add(occurrence);
                    index++;
                }
            }

            if (policy != PackedIniFragmentOrderingPolicy.SourceOccurrenceOrder)
            {
                var keyed = new List<Tuple<PackedIniFragmentOccurrence, int>>();
                var invalid = new List<PackedIniFragmentOccurrence>();
                foreach (PackedIniFragmentOccurrence occurrence in accepted)
                {
                    int numeric;
                    if (!TryParseNumericKey(occurrence.RawKey, out numeric, diagnostics, occurrence))
                    {
                        invalid.Add(occurrence);
                        continue;
                    }
                    keyed.Add(Tuple.Create(occurrence, numeric));
                }

                foreach (IGrouping<int, Tuple<PackedIniFragmentOccurrence, int>> group in keyed.GroupBy(item => item.Item2).Where(group => group.Count() > 1))
                {
                    diagnostics.Add(Error(PackedMapDiagnosticCode.DuplicateNumericFragmentKey, "Numeric fragment keys must be unique under this ordering policy."));
                    if (group.Select(item => item.Item1.RawKey).Distinct(StringComparer.Ordinal).Count() > 1)
                        diagnostics.Add(Error(PackedMapDiagnosticCode.FragmentKeyCollision, "Distinct raw keys normalize to the same numeric fragment key."));
                }

                int expected = 1;
                foreach (Tuple<PackedIniFragmentOccurrence, int> item in keyed.OrderBy(item => item.Item2).ThenBy(item => item.Item1.SourceOrder))
                {
                    if (item.Item2 > expected)
                        diagnostics.Add(Warning(PackedMapDiagnosticCode.FragmentKeyGap, "A numeric fragment key gap was observed."));
                    expected = Math.Max(expected, item.Item2 + 1);
                }
                if (keyed.Count == 0 || keyed.Min(item => item.Item2) != 1)
                    diagnostics.Add(Warning(PackedMapDiagnosticCode.MissingFragmentKeyOne, "Numeric ordering did not contain key 1."));
                accepted = keyed.OrderBy(item => item.Item2)
                    .ThenBy(item => item.Item1.SourceOrder)
                    .Select(item => item.Item1)
                    .Concat(invalid.OrderBy(item => item.SourceOrder))
                    .ToList();
            }

            if (policy == PackedIniFragmentOrderingPolicy.StrictSequentialFromOne)
            {
                for (int index = 0; index < accepted.Count; index++)
                {
                    int numeric;
                    if (!TryParseNumericKey(accepted[index].RawKey, out numeric, diagnostics, accepted[index]) || numeric != index + 1)
                        diagnostics.Add(Error(PackedMapDiagnosticCode.FragmentKeyGap, "Strict sequential ordering requires keys 1..N without gaps."));
                }
            }

            return new PackedIniFragmentCollection(accepted, diagnostics);
        }

        private static bool TryParseNumericKey(string raw, int occurrence, List<PackedMapDiagnostic> diagnostics)
        {
            int value;
            if (!int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out value))
            {
                diagnostics.Add(Error(PackedMapDiagnosticCode.NonnumericFragmentKey, "A fragment key is not an invariant decimal integer."));
                return false;
            }
            if (value == 0) diagnostics.Add(Error(PackedMapDiagnosticCode.FragmentKeyZero, "Fragment key zero is not valid."));
            if (value < 0) diagnostics.Add(Error(PackedMapDiagnosticCode.NegativeFragmentKey, "Negative fragment keys are not valid."));
            return value > 0;
        }

        private static bool TryParseNumericKey(string raw, out int value, List<PackedMapDiagnostic> diagnostics, PackedIniFragmentOccurrence occurrence)
        {
            value = 0;
            if (string.IsNullOrEmpty(raw) || raw.Any(character => character < '0' || character > '9'))
            {
                diagnostics.Add(Error(PackedMapDiagnosticCode.NonnumericFragmentKey, "A fragment key is not an invariant decimal integer.", occurrence));
                return false;
            }
            if (!int.TryParse(raw, NumberStyles.None, CultureInfo.InvariantCulture, out value))
            {
                diagnostics.Add(Error(PackedMapDiagnosticCode.FragmentKeyOverflow, "A fragment key overflows Int32.", occurrence));
                return false;
            }
            if (value == 0) diagnostics.Add(Error(PackedMapDiagnosticCode.FragmentKeyZero, "Fragment key zero is not valid.", occurrence));
            return value > 0;
        }

        private static PackedMapDiagnostic Error(PackedMapDiagnosticCode code, string message, PackedIniFragmentOccurrence occurrence = null)
        { return new PackedMapDiagnostic(code, BinaryDiagnosticSeverity.Error, message, occurrence == null ? null : occurrence.SourceId, occurrence == null ? -1 : occurrence.PhysicalLineId); }
        private static PackedMapDiagnostic Warning(PackedMapDiagnosticCode code, string message, PackedIniFragmentOccurrence occurrence = null)
        { return new PackedMapDiagnostic(code, BinaryDiagnosticSeverity.Warning, message, occurrence == null ? null : occurrence.SourceId, occurrence == null ? -1 : occurrence.PhysicalLineId); }
    }
}
