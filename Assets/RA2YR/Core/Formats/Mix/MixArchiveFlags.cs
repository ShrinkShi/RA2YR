using System;

namespace RA2YR.Core.Formats.Mix
{
    [Flags]
    internal enum MixArchiveFlags : uint
    {
        None = 0,
        Checksum = 0x00010000,
        EncryptedDirectory = 0x00020000
    }

    internal enum MixArchiveHeaderKind
    {
        Classic,
        Extended
    }
}
