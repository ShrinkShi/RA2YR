using System;
using System.Collections.Generic;
using System.Linq;

namespace RA2YR.Core.Content
{
    public enum ContentDiagnosticSeverity
    {
        Info,
        Warning,
        Error
    }

    public enum ContentDiagnosticCode
    {
        ConfigurationFileNotFound,
        ConfigurationReadFailed,
        ConfigurationXmlRejected,
        InvalidConfigurationRoot,
        UnsupportedSchemaVersion,
        MissingAttribute,
        DuplicateSourceId,
        InvalidSourceId,
        InvalidSourceKind,
        InvalidPriority,
        InvalidBoolean,
        UnknownConfigurationElement,
        UnknownConfigurationAttribute,
        UnknownConfigurationContent,
        NoEnabledSource,
        MissingVersion,
        InvalidVersion,
        PathInsideRepository,
        PathUsesReparsePoint,
        PathInspectionFailed,
        PathAliasUnsupported,
        PathIdentityUnavailable,
        ExternalPathsOverlap,
        CachePathNotDirectory,
        SourceDirectoryMissing,
        SourceEnumerationFailed,
        DirectoryReparsePointSkipped,
        FileReparsePointSkipped,
        FileMetadataReadFailed,
        FileHashFailed,
        FileChangedDuringHash,
        SourceTreeChangedDuringIndex
    }

    public sealed class ContentDiagnostic
    {
        public ContentDiagnostic(
            ContentDiagnosticSeverity severity,
            ContentDiagnosticCode code,
            string message,
            string sourceId = null,
            string path = null,
            int lineNumber = 0,
            int linePosition = 0)
        {
            Severity = severity;
            Code = code;
            Message = message ?? throw new ArgumentNullException(nameof(message));
            SourceId = sourceId;
            Path = path;
            LineNumber = lineNumber;
            LinePosition = linePosition;
        }

        public ContentDiagnosticSeverity Severity { get; }

        public ContentDiagnosticCode Code { get; }

        public string Message { get; }

        public string SourceId { get; }

        public string Path { get; }

        public int LineNumber { get; }

        public int LinePosition { get; }
    }

    public sealed class ContentConfigurationException : Exception
    {
        public ContentConfigurationException(
            string message,
            IEnumerable<ContentDiagnostic> diagnostics,
            Exception innerException = null)
            : base(message, innerException)
        {
            Diagnostics = Array.AsReadOnly(
                (diagnostics ?? Enumerable.Empty<ContentDiagnostic>()).ToArray());
        }

        public IReadOnlyList<ContentDiagnostic> Diagnostics { get; }
    }
}
