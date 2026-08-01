using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

namespace RA2YR.Core.Content
{
    public sealed class ExternalContentConfigurationLoader
    {
        public const int SupportedSchemaVersion = 1;

        private const long MaximumConfigurationCharacters = 1024 * 1024;

        public ExternalContentConfigurationLoadResult Load(
            string configurationPath,
            string repositoryRoot)
        {
            string fullConfigurationPath = RepositoryPathPolicy.NormalizeAbsolutePath(
                configurationPath,
                Directory.GetCurrentDirectory());
            string fullRepositoryRoot = RepositoryPathPolicy.NormalizeAbsolutePath(
                repositoryRoot,
                Directory.GetCurrentDirectory());
            var diagnostics = new List<ContentDiagnostic>();

            ValidateRepositoryRootBeforeRead(
                fullRepositoryRoot,
                diagnostics,
                fullConfigurationPath);

            if (!File.Exists(fullConfigurationPath))
            {
                diagnostics.Add(new ContentDiagnostic(
                    ContentDiagnosticSeverity.Error,
                    ContentDiagnosticCode.ConfigurationFileNotFound,
                    "The external content configuration file does not exist.",
                    path: fullConfigurationPath));
                throw new ContentConfigurationException(
                    "External content configuration loading failed.",
                    diagnostics);
            }

            XDocument document;
            try
            {
                document = LoadSecureDocument(fullConfigurationPath);
            }
            catch (XmlException exception)
            {
                diagnostics.Add(new ContentDiagnostic(
                    ContentDiagnosticSeverity.Error,
                    ContentDiagnosticCode.ConfigurationXmlRejected,
                    "The configuration XML was rejected: " + exception.Message,
                    path: fullConfigurationPath,
                    lineNumber: exception.LineNumber,
                    linePosition: exception.LinePosition));
                throw new ContentConfigurationException(
                    "External content configuration XML was rejected.",
                    diagnostics,
                    exception);
            }
            catch (IOException exception)
            {
                diagnostics.Add(new ContentDiagnostic(
                    ContentDiagnosticSeverity.Error,
                    ContentDiagnosticCode.ConfigurationReadFailed,
                    "The configuration file could not be read: " + exception.Message,
                    path: fullConfigurationPath));
                throw new ContentConfigurationException(
                    "External content configuration loading failed.",
                    diagnostics,
                    exception);
            }
            catch (UnauthorizedAccessException exception)
            {
                diagnostics.Add(new ContentDiagnostic(
                    ContentDiagnosticSeverity.Error,
                    ContentDiagnosticCode.ConfigurationReadFailed,
                    "The configuration file could not be read: " + exception.Message,
                    path: fullConfigurationPath));
                throw new ContentConfigurationException(
                    "External content configuration loading failed.",
                    diagnostics,
                    exception);
            }

            XElement root = document.Root;
            if (root == null || root.Name != "ExternalContent")
            {
                AddNodeDiagnostic(
                    diagnostics,
                    ContentDiagnosticSeverity.Error,
                    ContentDiagnosticCode.InvalidConfigurationRoot,
                    "The root element must be ExternalContent.",
                    root,
                    fullConfigurationPath);
                throw new ContentConfigurationException(
                    "External content configuration validation failed.",
                    diagnostics);
            }

            ValidateSchemaOneShape(root, diagnostics, fullConfigurationPath);

            int schemaVersion = ParseSchemaVersion(root, diagnostics, fullConfigurationPath);
            string configurationDirectory = Path.GetDirectoryName(fullConfigurationPath);
            string cachePath = ParseExternalPath(
                root,
                "cachePath",
                null,
                configurationDirectory,
                fullRepositoryRoot,
                diagnostics,
                fullConfigurationPath);
            if (cachePath != null && File.Exists(cachePath))
            {
                AddNodeDiagnostic(
                    diagnostics,
                    ContentDiagnosticSeverity.Error,
                    ContentDiagnosticCode.CachePathNotDirectory,
                    "The cache path exists but is not a directory.",
                    root.Attribute("cachePath"),
                    cachePath);
            }

            var sources = new List<ExternalContentSourceDescriptor>();
            var sourceIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            XElement sourcesElement = root.Element("Sources");
            if (sourcesElement == null)
            {
                AddNodeDiagnostic(
                    diagnostics,
                    ContentDiagnosticSeverity.Error,
                    ContentDiagnosticCode.MissingAttribute,
                    "The Sources element is required.",
                    root,
                    fullConfigurationPath);
            }
            else
            {
                foreach (XElement sourceElement in sourcesElement.Elements("Source"))
                {
                    ParseSource(
                        sourceElement,
                        configurationDirectory,
                        fullRepositoryRoot,
                        sourceIds,
                        sources,
                        diagnostics,
                        fullConfigurationPath);
                }

                if (!sourcesElement.Elements("Source").Any())
                {
                    AddNodeDiagnostic(
                        diagnostics,
                        ContentDiagnosticSeverity.Error,
                        ContentDiagnosticCode.MissingAttribute,
                        "At least one Source element is required.",
                        sourcesElement,
                        fullConfigurationPath);
                }
            }

            if (sources.Count > 0 && !sources.Any(source => source.Enabled))
            {
                AddNodeDiagnostic(
                    diagnostics,
                    ContentDiagnosticSeverity.Error,
                    ContentDiagnosticCode.NoEnabledSource,
                    "At least one valid source must be enabled.",
                    sourcesElement ?? (XObject)root,
                    fullConfigurationPath);
            }

            ValidateExternalPathRelationships(
                cachePath,
                sources,
                diagnostics,
                fullConfigurationPath);

            if (diagnostics.Any(item => item.Severity == ContentDiagnosticSeverity.Error))
            {
                throw new ContentConfigurationException(
                    "External content configuration validation failed.",
                    diagnostics);
            }

            var configuration = new ExternalContentConfiguration(
                schemaVersion,
                fullConfigurationPath,
                fullRepositoryRoot,
                cachePath,
                sources);
            return new ExternalContentConfigurationLoadResult(configuration, diagnostics);
        }

        private static XDocument LoadSecureDocument(string path)
        {
            var settings = new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Prohibit,
                XmlResolver = null,
                IgnoreComments = false,
                IgnoreWhitespace = false,
                MaxCharactersFromEntities = 0,
                MaxCharactersInDocument = MaximumConfigurationCharacters,
                CloseInput = true
            };

            using (var stream = new FileStream(
                       path,
                       FileMode.Open,
                       FileAccess.Read,
                       FileShare.Read))
            using (XmlReader reader = XmlReader.Create(stream, settings, path))
            {
                return XDocument.Load(
                    reader,
                    LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);
            }
        }

        private static void ValidateRepositoryRootBeforeRead(
            string repositoryRoot,
            ICollection<ContentDiagnostic> diagnostics,
            string configurationPath)
        {
            if (!Directory.Exists(repositoryRoot))
            {
                diagnostics.Add(new ContentDiagnostic(
                    ContentDiagnosticSeverity.Error,
                    ContentDiagnosticCode.RepositoryRootNotDirectory,
                    "The formal repository root must exist and be a directory.",
                    path: repositoryRoot));
                throw new ContentConfigurationException(
                    "Repository root validation failed before configuration loading.",
                    diagnostics);
            }

            string aliasReason;
            if (RepositoryPathPolicy.TryFindUnsupportedAlias(repositoryRoot, out aliasReason))
            {
                diagnostics.Add(new ContentDiagnostic(
                    ContentDiagnosticSeverity.Error,
                    ContentDiagnosticCode.PathAliasUnsupported,
                    "The repository root cannot be verified safely: " + aliasReason,
                    path: repositoryRoot));
                throw new ContentConfigurationException(
                    "Repository root validation failed before configuration loading.",
                    diagnostics);
            }

            try
            {
                string reparsePointPath;
                if (RepositoryPathPolicy.ContainsExistingReparsePoint(
                    repositoryRoot,
                    out reparsePointPath))
                {
                    diagnostics.Add(new ContentDiagnostic(
                        ContentDiagnosticSeverity.Error,
                        ContentDiagnosticCode.PathUsesReparsePoint,
                        "The repository root traverses a reparse point.",
                        path: reparsePointPath));
                    throw new ContentConfigurationException(
                        "Repository root validation failed before configuration loading.",
                        diagnostics);
                }
            }
            catch (Exception exception) when (
                exception is IOException ||
                exception is UnauthorizedAccessException ||
                exception is System.Security.SecurityException)
            {
                diagnostics.Add(new ContentDiagnostic(
                    ContentDiagnosticSeverity.Error,
                    ContentDiagnosticCode.PathInspectionFailed,
                    "The repository root path chain could not be inspected: " + exception.Message,
                    path: repositoryRoot));
                throw new ContentConfigurationException(
                    "Repository root validation failed before configuration loading.",
                    diagnostics,
                    exception);
            }
        }

        private static void ValidateSchemaOneShape(
            XElement root,
            ICollection<ContentDiagnostic> diagnostics,
            string configurationPath)
        {
            ValidateAllowedAttributes(
                root,
                new[] { "schemaVersion", "cachePath" },
                diagnostics,
                configurationPath);
            ValidateTextContent(root, diagnostics, configurationPath);

            XElement[] rootElements = root.Elements().ToArray();
            foreach (XElement child in rootElements)
            {
                if (child.Name != "Sources")
                {
                    AddNodeDiagnostic(
                        diagnostics,
                        ContentDiagnosticSeverity.Error,
                        ContentDiagnosticCode.UnknownConfigurationElement,
                        "Only a Sources element is allowed under ExternalContent for schema version 1.",
                        child,
                        configurationPath);
                }
            }

            XElement[] sourcesElements = root.Elements("Sources").ToArray();
            if (sourcesElements.Length > 1)
            {
                AddNodeDiagnostic(
                    diagnostics,
                    ContentDiagnosticSeverity.Error,
                    ContentDiagnosticCode.UnknownConfigurationElement,
                    "Exactly one Sources element is allowed.",
                    sourcesElements[1],
                    configurationPath);
            }

            foreach (XElement sources in sourcesElements)
            {
                ValidateAllowedAttributes(
                    sources,
                    new string[0],
                    diagnostics,
                    configurationPath);
                ValidateTextContent(sources, diagnostics, configurationPath);
                foreach (XElement child in sources.Elements())
                {
                    if (child.Name != "Source")
                    {
                        AddNodeDiagnostic(
                            diagnostics,
                            ContentDiagnosticSeverity.Error,
                            ContentDiagnosticCode.UnknownConfigurationElement,
                            "Only Source elements are allowed under Sources for schema version 1.",
                            child,
                            configurationPath);
                        continue;
                    }

                    ValidateAllowedAttributes(
                        child,
                        new[] { "id", "kind", "path", "priority", "version", "enabled" },
                        diagnostics,
                        configurationPath);
                    ValidateTextContent(child, diagnostics, configurationPath);
                    foreach (XElement nested in child.Elements())
                    {
                        AddNodeDiagnostic(
                            diagnostics,
                            ContentDiagnosticSeverity.Error,
                            ContentDiagnosticCode.UnknownConfigurationElement,
                            "Source elements may not contain child elements for schema version 1.",
                            nested,
                            configurationPath);
                    }
                }
            }
        }

        private static void ValidateAllowedAttributes(
            XElement element,
            IEnumerable<string> allowedNames,
            ICollection<ContentDiagnostic> diagnostics,
            string configurationPath)
        {
            var allowed = new HashSet<string>(allowedNames, StringComparer.Ordinal);
            foreach (XAttribute attribute in element.Attributes())
            {
                if (attribute.IsNamespaceDeclaration ||
                    attribute.Name.Namespace != XNamespace.None ||
                    !allowed.Contains(attribute.Name.LocalName))
                {
                    AddNodeDiagnostic(
                        diagnostics,
                        ContentDiagnosticSeverity.Error,
                        ContentDiagnosticCode.UnknownConfigurationAttribute,
                        "The attribute '" + attribute.Name + "' is not allowed for schema version 1.",
                        attribute,
                        configurationPath);
                }
            }
        }

        private static void ValidateTextContent(
            XElement element,
            ICollection<ContentDiagnostic> diagnostics,
            string configurationPath)
        {
            foreach (XNode node in element.Nodes())
            {
                var text = node as XText;
                if (text != null && !string.IsNullOrWhiteSpace(text.Value))
                {
                    AddNodeDiagnostic(
                        diagnostics,
                        ContentDiagnosticSeverity.Error,
                        ContentDiagnosticCode.UnknownConfigurationContent,
                        "Non-whitespace text is not allowed here for schema version 1.",
                        text,
                        configurationPath);
                }
            }
        }

        private static int ParseSchemaVersion(
            XElement root,
            ICollection<ContentDiagnostic> diagnostics,
            string configurationPath)
        {
            XAttribute attribute = root.Attribute("schemaVersion");
            int schemaVersion;
            if (attribute == null ||
                !int.TryParse(
                    attribute.Value,
                    NumberStyles.None,
                    CultureInfo.InvariantCulture,
                    out schemaVersion))
            {
                AddNodeDiagnostic(
                    diagnostics,
                    ContentDiagnosticSeverity.Error,
                    ContentDiagnosticCode.UnsupportedSchemaVersion,
                    "schemaVersion must be the integer value 1.",
                    attribute ?? (XObject)root,
                    configurationPath);
                return 0;
            }

            if (schemaVersion != SupportedSchemaVersion)
            {
                AddNodeDiagnostic(
                    diagnostics,
                    ContentDiagnosticSeverity.Error,
                    ContentDiagnosticCode.UnsupportedSchemaVersion,
                    "Only external content schema version 1 is supported.",
                    attribute,
                    configurationPath);
            }

            return schemaVersion;
        }

        private static void ValidateExternalPathRelationships(
            string cachePath,
            IReadOnlyList<ExternalContentSourceDescriptor> sources,
            ICollection<ContentDiagnostic> diagnostics,
            string configurationPath)
        {
            if (cachePath == null)
            {
                return;
            }

            for (int index = 0; index < sources.Count; index++)
            {
                ValidatePathsDoNotOverlap(
                    cachePath,
                    sources[index].RootPath,
                    "The cache path must not contain or be contained by a source path.",
                    sources[index].Id,
                    diagnostics,
                    configurationPath);

                for (int otherIndex = index + 1; otherIndex < sources.Count; otherIndex++)
                {
                    ValidatePathsDoNotOverlap(
                        sources[index].RootPath,
                        sources[otherIndex].RootPath,
                        "Source paths must not contain one another.",
                        sources[otherIndex].Id,
                        diagnostics,
                        configurationPath);
                }
            }
        }

        private static void ValidatePathsDoNotOverlap(
            string firstPath,
            string secondPath,
            string message,
            string sourceId,
            ICollection<ContentDiagnostic> diagnostics,
            string configurationPath)
        {
            bool overlaps;
            string failureReason;
            if (!RepositoryPathPolicy.TryDetermineOverlap(
                firstPath,
                secondPath,
                out overlaps,
                out failureReason))
            {
                diagnostics.Add(new ContentDiagnostic(
                    ContentDiagnosticSeverity.Error,
                    ContentDiagnosticCode.PathIdentityUnavailable,
                    "Path identity comparison failed: " + failureReason,
                    sourceId,
                    configurationPath));
            }
            else if (overlaps)
            {
                diagnostics.Add(new ContentDiagnostic(
                    ContentDiagnosticSeverity.Error,
                    ContentDiagnosticCode.ExternalPathsOverlap,
                    message,
                    sourceId,
                    secondPath));
            }
        }

        private static void ParseSource(
            XElement element,
            string configurationDirectory,
            string repositoryRoot,
            ISet<string> sourceIds,
            ICollection<ExternalContentSourceDescriptor> sources,
            ICollection<ContentDiagnostic> diagnostics,
            string configurationPath)
        {
            string id = GetRequiredAttribute(
                element,
                "id",
                diagnostics,
                configurationPath);
            bool valid = id != null;

            if (id != null && !IsValidSourceId(id))
            {
                AddNodeDiagnostic(
                    diagnostics,
                    ContentDiagnosticSeverity.Error,
                    ContentDiagnosticCode.InvalidSourceId,
                    "Source id must be 1-64 ASCII letters, digits, dots, underscores, or hyphens and must start with a letter or digit.",
                    element.Attribute("id"),
                    configurationPath,
                    id);
                valid = false;
            }
            else if (id != null && !sourceIds.Add(id))
            {
                AddNodeDiagnostic(
                    diagnostics,
                    ContentDiagnosticSeverity.Error,
                    ContentDiagnosticCode.DuplicateSourceId,
                    "Source ids must be unique using ordinal case-insensitive comparison.",
                    element.Attribute("id"),
                    configurationPath,
                    id);
                valid = false;
            }

            ContentSourceKind kind = ContentSourceKind.Other;
            string kindValue = GetRequiredAttribute(
                element,
                "kind",
                diagnostics,
                configurationPath,
                id);
            if (kindValue == null ||
                !Enum.TryParse(kindValue, true, out kind) ||
                !Enum.IsDefined(typeof(ContentSourceKind), kind))
            {
                if (kindValue != null)
                {
                    AddNodeDiagnostic(
                        diagnostics,
                        ContentDiagnosticSeverity.Error,
                        ContentDiagnosticCode.InvalidSourceKind,
                        "Source kind must be Clean, Unpacked, Patched, Overlay, or Other.",
                        element.Attribute("kind"),
                        configurationPath,
                        id);
                }

                valid = false;
            }

            int priority = 0;
            string priorityValue = GetRequiredAttribute(
                element,
                "priority",
                diagnostics,
                configurationPath,
                id);
            if (priorityValue == null ||
                !int.TryParse(
                    priorityValue,
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out priority))
            {
                if (priorityValue != null)
                {
                    AddNodeDiagnostic(
                        diagnostics,
                        ContentDiagnosticSeverity.Error,
                        ContentDiagnosticCode.InvalidPriority,
                        "Source priority must be a 32-bit integer.",
                        element.Attribute("priority"),
                        configurationPath,
                        id);
                }

                valid = false;
            }

            bool enabled = true;
            XAttribute enabledAttribute = element.Attribute("enabled");
            if (enabledAttribute != null &&
                !bool.TryParse(enabledAttribute.Value, out enabled))
            {
                AddNodeDiagnostic(
                    diagnostics,
                    ContentDiagnosticSeverity.Error,
                    ContentDiagnosticCode.InvalidBoolean,
                    "Source enabled must be true or false.",
                    enabledAttribute,
                    configurationPath,
                    id);
                valid = false;
            }

            string version = element.Attribute("version")?.Value;
            if (string.IsNullOrWhiteSpace(version))
            {
                version = string.Empty;
                AddNodeDiagnostic(
                    diagnostics,
                    ContentDiagnosticSeverity.Warning,
                    ContentDiagnosticCode.MissingVersion,
                    "Source version is not set; compatibility reports will identify this source by id and fingerprint only.",
                    element,
                    configurationPath,
                    id);
            }
            else if (version.Length > 256 || version.Any(char.IsControl) || Path.IsPathRooted(version))
            {
                AddNodeDiagnostic(
                    diagnostics,
                    ContentDiagnosticSeverity.Error,
                    ContentDiagnosticCode.InvalidVersion,
                    "Source version must be at most 256 characters, contain no control characters, and not be an absolute path.",
                    element.Attribute("version"),
                    configurationPath,
                    id);
                valid = false;
            }

            string rootPath = ParseExternalPath(
                element,
                "path",
                id,
                configurationDirectory,
                repositoryRoot,
                diagnostics,
                configurationPath);
            if (rootPath == null)
            {
                valid = false;
            }
            else if (enabled && !Directory.Exists(rootPath))
            {
                AddNodeDiagnostic(
                    diagnostics,
                    ContentDiagnosticSeverity.Warning,
                    ContentDiagnosticCode.SourceDirectoryMissing,
                    "The enabled source directory does not currently exist.",
                    element.Attribute("path"),
                    rootPath,
                    id);
            }

            if (valid)
            {
                sources.Add(new ExternalContentSourceDescriptor(
                    id,
                    kind,
                    rootPath,
                    priority,
                    version,
                    enabled));
            }
        }

        private static string ParseExternalPath(
            XElement element,
            string attributeName,
            string sourceId,
            string configurationDirectory,
            string repositoryRoot,
            ICollection<ContentDiagnostic> diagnostics,
            string configurationPath)
        {
            string path = GetRequiredAttribute(
                element,
                attributeName,
                diagnostics,
                configurationPath,
                sourceId);
            if (path == null)
            {
                return null;
            }

            string resolvedPath;
            try
            {
                resolvedPath = RepositoryPathPolicy.NormalizeAbsolutePath(
                    path,
                    configurationDirectory);
            }
            catch (Exception exception) when (
                exception is ArgumentException ||
                exception is NotSupportedException ||
                exception is PathTooLongException)
            {
                AddNodeDiagnostic(
                    diagnostics,
                    ContentDiagnosticSeverity.Error,
                    ContentDiagnosticCode.MissingAttribute,
                    "The configured path is invalid: " + exception.Message,
                    element.Attribute(attributeName),
                    configurationPath,
                    sourceId);
                return null;
            }

            if (RepositoryPathPolicy.OverlapsRepository(resolvedPath, repositoryRoot))
            {
                AddNodeDiagnostic(
                    diagnostics,
                    ContentDiagnosticSeverity.Error,
                    ContentDiagnosticCode.PathInsideRepository,
                    "External content and cache paths must not contain or be contained by the formal repository.",
                    element.Attribute(attributeName),
                    resolvedPath,
                    sourceId);
                return null;
            }

            string aliasReason;
            if (RepositoryPathPolicy.TryFindUnsupportedAlias(resolvedPath, out aliasReason))
            {
                AddNodeDiagnostic(
                    diagnostics,
                    ContentDiagnosticSeverity.Error,
                    ContentDiagnosticCode.PathAliasUnsupported,
                    "The configured path cannot be verified safely: " + aliasReason,
                    element.Attribute(attributeName),
                    resolvedPath,
                    sourceId);
                return null;
            }

            try
            {
                string reparsePointPath;
                if (RepositoryPathPolicy.ContainsExistingReparsePoint(
                    resolvedPath,
                    out reparsePointPath))
                {
                    AddNodeDiagnostic(
                        diagnostics,
                        ContentDiagnosticSeverity.Error,
                        ContentDiagnosticCode.PathUsesReparsePoint,
                        "External content and cache paths may not traverse an existing reparse point.",
                        element.Attribute(attributeName),
                        reparsePointPath,
                        sourceId);
                    return null;
                }
            }
            catch (Exception exception) when (
                exception is IOException ||
                exception is UnauthorizedAccessException ||
                exception is System.Security.SecurityException)
            {
                AddNodeDiagnostic(
                    diagnostics,
                    ContentDiagnosticSeverity.Error,
                    ContentDiagnosticCode.PathInspectionFailed,
                    "The configured path chain could not be inspected: " + exception.Message,
                    element.Attribute(attributeName),
                    resolvedPath,
                    sourceId);
                return null;
            }


            bool identityOverlap;
            string identityFailure;
            if (!RepositoryPathPolicy.TryDetermineOverlap(
                resolvedPath,
                repositoryRoot,
                out identityOverlap,
                out identityFailure))
            {
                AddNodeDiagnostic(
                    diagnostics,
                    ContentDiagnosticSeverity.Error,
                    ContentDiagnosticCode.PathIdentityUnavailable,
                    "The path identity could not be compared with the repository: " + identityFailure,
                    element.Attribute(attributeName),
                    resolvedPath,
                    sourceId);
                return null;
            }

            if (identityOverlap)
            {
                AddNodeDiagnostic(
                    diagnostics,
                    ContentDiagnosticSeverity.Error,
                    ContentDiagnosticCode.PathInsideRepository,
                    "The resolved storage identity overlaps the formal repository.",
                    element.Attribute(attributeName),
                    resolvedPath,
                    sourceId);
                return null;
            }

            return resolvedPath;
        }

        private static string GetRequiredAttribute(
            XElement element,
            string attributeName,
            ICollection<ContentDiagnostic> diagnostics,
            string configurationPath,
            string sourceId = null)
        {
            XAttribute attribute = element.Attribute(attributeName);
            if (attribute != null && !string.IsNullOrWhiteSpace(attribute.Value))
            {
                return attribute.Value.Trim();
            }

            AddNodeDiagnostic(
                diagnostics,
                ContentDiagnosticSeverity.Error,
                ContentDiagnosticCode.MissingAttribute,
                "The " + attributeName + " attribute is required.",
                element,
                configurationPath,
                sourceId);
            return null;
        }

        private static bool IsValidSourceId(string value)
        {
            return ContentConfigurationValueRules.IsValidSourceId(value);
        }

        private static void AddNodeDiagnostic(
            ICollection<ContentDiagnostic> diagnostics,
            ContentDiagnosticSeverity severity,
            ContentDiagnosticCode code,
            string message,
            XObject node,
            string path,
            string sourceId = null)
        {
            var lineInfo = node as IXmlLineInfo;
            diagnostics.Add(new ContentDiagnostic(
                severity,
                code,
                message,
                sourceId,
                path,
                lineInfo != null && lineInfo.HasLineInfo() ? lineInfo.LineNumber : 0,
                lineInfo != null && lineInfo.HasLineInfo() ? lineInfo.LinePosition : 0));
        }
    }
}
