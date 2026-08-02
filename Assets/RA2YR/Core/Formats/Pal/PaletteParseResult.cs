using System;
using System.Collections.Generic;
using System.Linq;

namespace RA2YR.Core.Formats.Pal
{
    internal sealed class PaletteParseResult
    {
        private readonly IReadOnlyList<PaletteDiagnostic> diagnostics;

        private PaletteParseResult(
            WestwoodPalette palette,
            IEnumerable<PaletteDiagnostic> diagnostics)
        {
            PaletteDiagnostic[] diagnosticArray =
                (diagnostics ?? throw new ArgumentNullException(nameof(diagnostics))).ToArray();
            if (diagnosticArray.Any(diagnostic => diagnostic == null))
            {
                throw new ArgumentException(
                    "Palette diagnostics cannot contain null.",
                    nameof(diagnostics));
            }

            if ((palette == null) == (diagnosticArray.Length == 0))
            {
                throw new ArgumentException(
                    "A palette parse result must be either complete or failed.",
                    nameof(palette));
            }

            Palette = palette;
            this.diagnostics = Array.AsReadOnly(diagnosticArray);
        }

        public bool IsSuccess => Palette != null && diagnostics.Count == 0;

        public WestwoodPalette Palette { get; }

        public IReadOnlyList<PaletteDiagnostic> Diagnostics => diagnostics;

        internal static PaletteParseResult Success(WestwoodPalette palette)
        {
            return new PaletteParseResult(
                palette ?? throw new ArgumentNullException(nameof(palette)),
                Array.Empty<PaletteDiagnostic>());
        }

        internal static PaletteParseResult Failure(PaletteDiagnostic diagnostic)
        {
            return new PaletteParseResult(
                null,
                new[]
                {
                    diagnostic ?? throw new ArgumentNullException(nameof(diagnostic))
                });
        }
    }
}
