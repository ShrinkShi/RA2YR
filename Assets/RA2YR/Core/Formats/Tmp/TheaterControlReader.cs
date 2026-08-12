using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using RA2YR.Core.Binary;
using RA2YR.Core.Configuration.Ini.Resolution;
using RA2YR.Core.Content;
using RA2YR.Core.Formats.Ini;

namespace RA2YR.Core.Formats.Tmp
{
    internal static class TheaterControlReader
    {
        internal static TheaterControlDocument Read(IniResolutionResult composed, TheaterProfileDescriptor profile,
            int maxDiagnostics = 4096, int maxValues = 100000)
        {
            if (composed == null) throw new ArgumentNullException(nameof(composed));
            if (profile == null) throw new ArgumentNullException(nameof(profile));
            if (maxDiagnostics < 0 || maxValues < 0) throw new ArgumentOutOfRangeException();
            var collector = new TmpDiagnosticCollector(maxDiagnostics);
            var values = new List<TheaterValueView>();
            if (composed.Sections.Count > maxValues)
            {
                collector.Fail(Diagnostic(TmpDiagnosticCode.DimensionBudgetExceeded, composed, "control", "Theater section budget exceeded."));
                return new TheaterControlDocument(profile, null, values, collector.Diagnostics, collector.Execution);
            }
            foreach (IniResolvedSection section in composed.Sections.OrderBy(s => s.Name, StringComparer.OrdinalIgnoreCase))
            {
                foreach (IniResolvedValue value in section.Values.OrderBy(v => v.KeyName, StringComparer.OrdinalIgnoreCase))
                {
                    IniResolvedValueCandidate winner = value.Winner;
                    string raw = Decode(winner.CopyEffectiveValueBytes());
                    int? integer = int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed) ? parsed : (int?)null;
                    values.Add(new TheaterValueView(section.Name, value.KeyName, raw, integer, winner.Document.Document.Provenance,
                        winner.SectionLineId, winner.KeyLineId, value.CandidateChain));
                }
            }
            bool hasGeneral = composed.Sections.Any(s => string.Equals(s.Name, "General", StringComparison.OrdinalIgnoreCase));
            if (!hasGeneral)
                collector.Fail(Diagnostic(TmpDiagnosticCode.MissingGeneral, composed, "General", "Theater control document has no [General] section."));
            return new TheaterControlDocument(profile, values.FirstOrDefault(v => string.Equals(v.Section, "General", StringComparison.OrdinalIgnoreCase)), values, collector.Diagnostics, collector.Execution);
        }

        private static string Decode(byte[] bytes)
        {
            return Encoding.UTF8.GetString(bytes ?? Array.Empty<byte>());
        }

        private static TmpDiagnostic Diagnostic(TmpDiagnosticCode code, IniResolutionResult result, string stage, string message)
        {
            IniSourceProvenance provenance = result.Trace.DocumentCandidates.Count == 0
                ? throw new InvalidOperationException("A composed INI result has no provenance.")
                : result.Trace.DocumentCandidates[0].Document.Provenance;
            BinarySourceContext source = result.Trace.DocumentCandidates[0].Document.Source;
            return new TmpDiagnostic(BinaryDiagnosticSeverity.Error, code, source, provenance, 0, -1, stage, message);
        }
    }
}
