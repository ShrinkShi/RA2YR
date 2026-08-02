using System;
using RA2YR.Core.Binary.Seekable;

namespace RA2YR.Core.Formats.Mix
{
    internal sealed class MixArchiveEntry
    {
        private readonly ReadOnlyDataWindow payloadWindow;

        internal MixArchiveEntry(
            int index,
            MixFileId id,
            uint relativeOffset,
            uint length,
            ReadOnlyDataWindow payloadWindow)
        {
            if (index < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            Index = index;
            Id = id;
            RelativeOffset = relativeOffset;
            Length = length;
            this.payloadWindow = payloadWindow ??
                throw new ArgumentNullException(nameof(payloadWindow));
        }

        public int Index { get; }

        public MixFileId Id { get; }

        public uint RelativeOffset { get; }

        public uint Length { get; }

        public long PayloadAbsoluteOffset => payloadWindow.AbsoluteStartOffset;

        public ReadOnlyDataWindow OpenPayloadWindow()
        {
            return payloadWindow;
        }
    }
}
