using System;
using System.Collections.Generic;

namespace RA2YR.Core.Content
{
    /// <summary>
    /// Internal trust boundary for indexed content providers. Future archive,
    /// embedded-map, mod, and synthetic providers implement this contract.
    /// </summary>
    internal interface IContentSource
    {
        ExternalContentSourceDescriptor Descriptor { get; }

        ContentSourceIndex BuildIndex(
            string repositoryRoot,
            ICollection<ContentDiagnostic> diagnostics);
    }

    internal delegate ContentSourceIndex DirectoryContentSourceIndexBuilder(
        ExternalContentSourceDescriptor descriptor,
        string repositoryRoot,
        ICollection<ContentDiagnostic> diagnostics);

    internal sealed class DirectoryContentSource : IContentSource
    {
        private readonly DirectoryContentSourceIndexBuilder indexBuilder;

        public DirectoryContentSource(
            ExternalContentSourceDescriptor descriptor,
            DirectoryContentSourceIndexBuilder indexBuilder)
        {
            Descriptor = descriptor ?? throw new ArgumentNullException(nameof(descriptor));
            this.indexBuilder = indexBuilder ??
                throw new ArgumentNullException(nameof(indexBuilder));
        }

        public ExternalContentSourceDescriptor Descriptor { get; }

        public ContentSourceIndex BuildIndex(
            string repositoryRoot,
            ICollection<ContentDiagnostic> diagnostics)
        {
            return indexBuilder(Descriptor, repositoryRoot, diagnostics);
        }
    }
}
