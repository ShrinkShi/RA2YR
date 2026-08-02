using System;
using System.Collections.Generic;

namespace RA2YR.Core.Formats.Mix
{
    internal sealed class MixArchiveReadResult
    {
        private readonly IReadOnlyList<MixDiagnostic> diagnostics;

        private MixArchiveReadResult(
            MixArchive archive,
            IList<MixDiagnostic> diagnostics)
        {
            Archive = archive;
            this.diagnostics = new List<MixDiagnostic>(
                diagnostics ?? throw new ArgumentNullException(nameof(diagnostics))).AsReadOnly();
        }

        public bool IsSuccess => Archive != null && diagnostics.Count == 0;

        public MixArchive Archive { get; }

        public IReadOnlyList<MixDiagnostic> Diagnostics => diagnostics;

        internal static MixArchiveReadResult Success(MixArchive archive)
        {
            return new MixArchiveReadResult(
                archive ?? throw new ArgumentNullException(nameof(archive)),
                Array.Empty<MixDiagnostic>());
        }

        internal static MixArchiveReadResult Failure(MixDiagnostic diagnostic)
        {
            return new MixArchiveReadResult(
                null,
                new[]
                {
                    diagnostic ?? throw new ArgumentNullException(nameof(diagnostic))
                });
        }
    }
}
