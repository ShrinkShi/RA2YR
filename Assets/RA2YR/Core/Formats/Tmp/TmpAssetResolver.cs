using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using RA2YR.Core.Binary;
using RA2YR.Core.Formats.Ini;

namespace RA2YR.Core.Formats.Tmp
{
    internal static class TmpAssetResolver
    {
        internal static TmpAssetResolutionTrace Resolve(TheaterTileRegistry registry, TheaterProfileDescriptor profile,
            int tileSetIndex, int localOrdinal, ITmpAssetProvider provider, TmpAssetResolutionPolicy policy = null, int maxCandidates = 128)
        {
            if (registry == null) throw new ArgumentNullException(nameof(registry));
            if (profile == null) throw new ArgumentNullException(nameof(profile));
            if (provider == null) throw new ArgumentNullException(nameof(provider));
            policy = policy ?? new TmpAssetResolutionPolicy();
            if (localOrdinal < 0 || maxCandidates < 0) throw new ArgumentOutOfRangeException(nameof(localOrdinal));
            TheaterTileIdRange range = registry.IdRanges.FirstOrDefault(r => r.TileSetIndex == tileSetIndex);
            var execution = new TmpExecutionState();
            var diagnostics = new List<TmpDiagnostic>();
            if (range == null || localOrdinal >= range.Count)
            {
                execution.Fail();
                return new TmpAssetResolutionTrace(profile, tileSetIndex, localOrdinal, -1, Array.Empty<TmpAssetCandidate>(), null, Array.Empty<TmpAssetCandidate>(), diagnostics, execution);
            }
            long globalId = checked(range.StartInclusive + localOrdinal);
            TheaterTileSetDescriptor set = range.Descriptor;
            if (string.IsNullOrWhiteSpace(set.FileNameRaw))
            {
                execution.Fail();
                return new TmpAssetResolutionTrace(profile, tileSetIndex, localOrdinal, globalId, Array.Empty<TmpAssetCandidate>(), null, Array.Empty<TmpAssetCandidate>(), diagnostics, execution);
            }
            var candidates = new List<TmpAssetCandidate>();
            string stem = set.FileNameRaw + (localOrdinal + 1).ToString("D2", CultureInfo.InvariantCulture);
            var variations = new List<string> { string.Empty };
            if (policy.Variation == TmpVariationPolicy.BaseAndAThroughF)
                variations.AddRange(new[] { "a", "b", "c", "d", "e", "f" });
            foreach (string variation in variations)
            {
                AddCandidate(candidates, stem + variation + profile.PrimaryTmpExtension, profile.PrimaryTmpExtension, variation, tileSetIndex, localOrdinal, globalId, provider, diagnostics, execution);
                if (policy.Fallback == TmpFallbackExtensionPolicy.ExplicitNewUrbanEditorCandidate && profile.Kind == TheaterKind.NewUrban)
                {
                    foreach (string extension in profile.OptionalFallbackTmpExtensions)
                        AddCandidate(candidates, stem + variation + extension, extension, variation, tileSetIndex, localOrdinal, globalId, provider, diagnostics, execution);
                }
                if (candidates.Count > maxCandidates)
                {
                    execution.Fail();
                    break;
                }
            }
            TmpAssetCandidate selected = candidates.FirstOrDefault(c => c.IsPresent);
            if (selected == null) execution.Fail();
            var suppressed = candidates.Where(c => !ReferenceEquals(c, selected)).ToArray();
            return new TmpAssetResolutionTrace(profile, tileSetIndex, localOrdinal, globalId, candidates, selected, suppressed, diagnostics, execution);
        }

        private static void AddCandidate(List<TmpAssetCandidate> list, string logicalName, string extension, string variation,
            int tileSetIndex, int localOrdinal, long globalId, ITmpAssetProvider provider, List<TmpDiagnostic> diagnostics, TmpExecutionState execution)
        {
            IReadOnlyList<TmpAssetProviderCandidate> providers = provider.ResolveCandidates(logicalName) ?? Array.Empty<TmpAssetProviderCandidate>();
            if (providers.Count == 0)
            {
                list.Add(new TmpAssetCandidate(logicalName, extension, variation, tileSetIndex, localOrdinal, globalId, null));
                return;
            }
            foreach (TmpAssetProviderCandidate candidate in providers.OrderBy(c => c.ProviderId, StringComparer.Ordinal))
                list.Add(new TmpAssetCandidate(logicalName, extension, variation, tileSetIndex, localOrdinal, globalId, candidate));
            if (providers.Count > 1)
            {
                execution.Fail();
                diagnostics.Add(new TmpDiagnostic(BinaryDiagnosticSeverity.Error, TmpDiagnosticCode.AmbiguousAssetCandidate,
                    new BinarySourceContext("tmp-asset-resolver", providers[0].Provenance.SourceId, providers[0].Provenance.LogicalChain[0]),
                    providers[0].Provenance, 0, -1, "assets", "Multiple provider candidates share one logical TMP name."));
            }
            if (extension != string.Empty && extension != "." + extension.TrimStart('.'))
                execution.Observe(BinaryDiagnosticSeverity.Warning);
        }
    }
}
