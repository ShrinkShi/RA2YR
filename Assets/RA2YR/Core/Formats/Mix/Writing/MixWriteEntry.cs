using System;

namespace RA2YR.Core.Formats.Mix.Writing
{
    internal sealed class MixWriteEntry
    {
        private readonly byte[] payload;

        public MixWriteEntry(MixFileId id, byte[] payload)
        {
            Id = id;
            this.payload = (byte[])(payload ?? throw new ArgumentNullException(nameof(payload))).Clone();
        }

        public MixFileId Id { get; }

        public int Length => payload.Length;

        internal ReadOnlyMemory<byte> Payload => payload;

        public byte[] GetPayloadCopy()
        {
            return (byte[])payload.Clone();
        }
    }
}
