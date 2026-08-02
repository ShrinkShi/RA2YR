using System;
using System.IO;

namespace RA2YR.Core.Binary.Seekable
{
    internal sealed class ReadOnlyDataWindow
    {
        private readonly ReadOnlyDataWindowSession session;

        internal ReadOnlyDataWindow(
            ReadOnlyDataWindowSession session,
            long absoluteStartOffset,
            long length,
            int depth)
        {
            this.session = session ?? throw new ArgumentNullException(nameof(session));
            AbsoluteStartOffset = absoluteStartOffset;
            Length = length;
            Depth = depth;
        }

        public long AbsoluteStartOffset { get; }

        public long Length { get; }

        public int Depth { get; }

        public ReadOnlyDataWindow CreateChild(
            long relativeStartOffset,
            long length,
            string fieldOrSection)
        {
            return session.CreateChild(
                this,
                relativeStartOffset,
                length,
                fieldOrSection);
        }

        public void ReadExactly(
            long relativeOffset,
            byte[] destination,
            int destinationOffset,
            int count,
            string fieldOrSection)
        {
            session.ReadExactly(
                this,
                relativeOffset,
                destination,
                destinationOffset,
                count,
                fieldOrSection);
        }

        public void CopyTo(
            Stream destination,
            string fieldOrSection,
            int bufferSize = ReadOnlyDataWindowSession.DefaultTransferBufferSize)
        {
            session.CopyTo(this, destination, fieldOrSection, bufferSize);
        }

        public string ComputeSha256(
            string fieldOrSection,
            int bufferSize = ReadOnlyDataWindowSession.DefaultTransferBufferSize)
        {
            return session.ComputeSha256(this, fieldOrSection, bufferSize);
        }
    }
}
