using System;
using System.Collections.Generic;
using System.Linq;

namespace RA2YR.Core.Formats.Csf
{
    internal sealed class CsfParseResult
    {
        private readonly IReadOnlyList<CsfDiagnostic> diagnostics;

        private CsfParseResult(
            CsfDocument document,
            IEnumerable<CsfDiagnostic> diagnostics)
        {
            CsfDiagnostic[] diagnosticArray =
                (diagnostics ?? throw new ArgumentNullException(nameof(diagnostics))).ToArray();
            if (diagnosticArray.Any(diagnostic => diagnostic == null))
            {
                throw new ArgumentException(
                    "CSF diagnostics cannot contain null.",
                    nameof(diagnostics));
            }

            if ((document == null) == (diagnosticArray.Length == 0))
            {
                throw new ArgumentException(
                    "A CSF parse result must be either complete or failed.",
                    nameof(document));
            }

            Document = document;
            this.diagnostics = Array.AsReadOnly(diagnosticArray);
        }

        public bool IsSuccess => Document != null && diagnostics.Count == 0;

        public CsfDocument Document { get; }

        public IReadOnlyList<CsfDiagnostic> Diagnostics => diagnostics;

        internal static CsfParseResult Success(CsfDocument document)
        {
            return new CsfParseResult(
                document ?? throw new ArgumentNullException(nameof(document)),
                Array.Empty<CsfDiagnostic>());
        }

        internal static CsfParseResult Failure(CsfDiagnostic diagnostic)
        {
            return new CsfParseResult(
                null,
                new[] { diagnostic ?? throw new ArgumentNullException(nameof(diagnostic)) });
        }
    }
}
