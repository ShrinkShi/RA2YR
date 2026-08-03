using System;
using System.Collections.Generic;
using System.Linq;

namespace RA2YR.Core.Formats.Ini
{
    internal sealed class IniParseResult
    {
        private readonly IReadOnlyList<IniDiagnostic> diagnostics;

        private IniParseResult(
            IniRawDocument document,
            IEnumerable<IniDiagnostic> diagnostics)
        {
            IniDiagnostic[] diagnosticArray =
                (diagnostics ?? throw new ArgumentNullException(nameof(diagnostics))).ToArray();
            if (diagnosticArray.Any(diagnostic => diagnostic == null))
            {
                throw new ArgumentException(
                    "INI diagnostics cannot contain null.",
                    nameof(diagnostics));
            }

            bool hasError = diagnosticArray.Any(
                diagnostic => diagnostic.Severity == IniDiagnosticSeverity.Error);
            if (document == null ? !hasError : hasError)
            {
                throw new ArgumentException(
                    "An INI parse is either a complete document without errors or a failed result.",
                    nameof(document));
            }

            Document = document;
            this.diagnostics = Array.AsReadOnly(diagnosticArray);
        }

        public bool IsSuccess => Document != null && diagnostics.All(
            diagnostic => diagnostic.Severity != IniDiagnosticSeverity.Error);

        public IniRawDocument Document { get; }

        public IReadOnlyList<IniDiagnostic> Diagnostics => diagnostics;

        internal static IniParseResult Success(
            IniRawDocument document,
            IEnumerable<IniDiagnostic> diagnostics)
        {
            return new IniParseResult(
                document ?? throw new ArgumentNullException(nameof(document)),
                diagnostics ?? Array.Empty<IniDiagnostic>());
        }

        internal static IniParseResult Failure(IniDiagnostic diagnostic)
        {
            return new IniParseResult(
                null,
                new[] { diagnostic ?? throw new ArgumentNullException(nameof(diagnostic)) });
        }
    }
}
