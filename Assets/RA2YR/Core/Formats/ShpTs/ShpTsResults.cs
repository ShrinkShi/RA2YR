using System;
using System.Collections.Generic;
using System.Linq;
using RA2YR.Core.Binary;

namespace RA2YR.Core.Formats.ShpTs
{
    internal sealed class ShpTsParseResult
    {
        private readonly IReadOnlyList<ShpTsDiagnostic> diagnostics;

        private ShpTsParseResult(
            ShpTsDocument document,
            IEnumerable<ShpTsDiagnostic> diagnostics)
        {
            ShpTsDiagnostic[] array =
                (diagnostics ?? throw new ArgumentNullException(nameof(diagnostics))).ToArray();
            if (array.Any(item => item == null))
            {
                throw new ArgumentException("Diagnostics cannot contain null.", nameof(diagnostics));
            }

            bool hasErrors = array.Any(item => item.Severity == BinaryDiagnosticSeverity.Error);
            if ((document == null) != hasErrors)
            {
                throw new ArgumentException("A parse result must be complete or failed.", nameof(document));
            }

            Document = document;
            this.diagnostics = Array.AsReadOnly(array);
        }

        public bool IsSuccess => Document != null && diagnostics.All(
            item => item.Severity != BinaryDiagnosticSeverity.Error);
        public ShpTsDocument Document { get; }
        public IReadOnlyList<ShpTsDiagnostic> Diagnostics => diagnostics;

        internal static ShpTsParseResult Success(
            ShpTsDocument document,
            IEnumerable<ShpTsDiagnostic> diagnostics)
        {
            return new ShpTsParseResult(
                document ?? throw new ArgumentNullException(nameof(document)),
                diagnostics ?? Array.Empty<ShpTsDiagnostic>());
        }

        internal static ShpTsParseResult Failure(
            IEnumerable<ShpTsDiagnostic> diagnostics)
        {
            return new ShpTsParseResult(null, diagnostics);
        }

        internal static ShpTsParseResult Failure(ShpTsDiagnostic diagnostic)
        {
            return Failure(new[] { diagnostic ?? throw new ArgumentNullException(nameof(diagnostic)) });
        }
    }

    internal sealed class ShpTsDecodeResult
    {
        private readonly IReadOnlyList<ShpTsDiagnostic> diagnostics;

        private ShpTsDecodeResult(
            ShpTsIndexedLocalFrame frame,
            IEnumerable<ShpTsDiagnostic> diagnostics)
        {
            ShpTsDiagnostic[] array =
                (diagnostics ?? throw new ArgumentNullException(nameof(diagnostics))).ToArray();
            if (array.Any(item => item == null))
            {
                throw new ArgumentException("Diagnostics cannot contain null.", nameof(diagnostics));
            }

            bool hasErrors = array.Any(item => item.Severity == BinaryDiagnosticSeverity.Error);
            if ((frame == null) != hasErrors)
            {
                throw new ArgumentException("A decode result must be complete or failed.", nameof(frame));
            }

            Frame = frame;
            this.diagnostics = Array.AsReadOnly(array);
        }

        public bool IsSuccess => Frame != null && diagnostics.All(
            item => item.Severity != BinaryDiagnosticSeverity.Error);
        public ShpTsIndexedLocalFrame Frame { get; }
        public IReadOnlyList<ShpTsDiagnostic> Diagnostics => diagnostics;

        internal static ShpTsDecodeResult Success(
            ShpTsIndexedLocalFrame frame,
            IEnumerable<ShpTsDiagnostic> diagnostics = null)
        {
            return new ShpTsDecodeResult(
                frame ?? throw new ArgumentNullException(nameof(frame)),
                diagnostics ?? Array.Empty<ShpTsDiagnostic>());
        }

        internal static ShpTsDecodeResult Failure(ShpTsDiagnostic diagnostic)
        {
            return new ShpTsDecodeResult(
                null,
                new[] { diagnostic ?? throw new ArgumentNullException(nameof(diagnostic)) });
        }
    }

    internal sealed class ShpTsDecodeDocumentResult
    {
        private readonly IReadOnlyList<ShpTsDiagnostic> diagnostics;

        private ShpTsDecodeDocumentResult(
            ShpTsDecodedDocument document,
            IEnumerable<ShpTsDiagnostic> diagnostics)
        {
            ShpTsDiagnostic[] array =
                (diagnostics ?? throw new ArgumentNullException(nameof(diagnostics))).ToArray();
            bool hasErrors = array.Any(item =>
                item == null || item.Severity == BinaryDiagnosticSeverity.Error);
            if ((document == null) != hasErrors)
            {
                throw new ArgumentException("A document decode result must be complete or failed.");
            }

            Document = document;
            this.diagnostics = Array.AsReadOnly(array);
        }

        public bool IsSuccess => Document != null && diagnostics.All(
            item => item.Severity != BinaryDiagnosticSeverity.Error);
        public ShpTsDecodedDocument Document { get; }
        public IReadOnlyList<ShpTsDiagnostic> Diagnostics => diagnostics;

        internal static ShpTsDecodeDocumentResult Success(
            ShpTsDecodedDocument document,
            IEnumerable<ShpTsDiagnostic> diagnostics)
        {
            return new ShpTsDecodeDocumentResult(
                document ?? throw new ArgumentNullException(nameof(document)),
                diagnostics ?? Array.Empty<ShpTsDiagnostic>());
        }

        internal static ShpTsDecodeDocumentResult Failure(
            IEnumerable<ShpTsDiagnostic> diagnostics)
        {
            return new ShpTsDecodeDocumentResult(null, diagnostics);
        }
    }
}
