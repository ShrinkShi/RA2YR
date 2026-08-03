using System;
using RA2YR.Core.Binary;

namespace RA2YR.Core.Formats.ShpTs
{
    internal sealed class ShpTsReadLimits
    {
        public ShpTsReadLimits(
            long maxInputBytes,
            long maxSingleReadBytes,
            int maxFrameCount,
            int maxCanvasDimension,
            long maxCanvasArea,
            long maxLocalFrameArea,
            long maxTotalDecodedPixels,
            int maxSingleRowBytes,
            long maxSingleFrameCompressedBytes,
            int maxCommandsPerRow,
            long maxCommandsPerFrame,
            long maxAllocatedBytes,
            long maxDescriptors,
            long maxSubwindows,
            int maxDiagnostics)
        {
            ValidateNonNegative(maxInputBytes, nameof(maxInputBytes));
            ValidateNonNegative(maxSingleReadBytes, nameof(maxSingleReadBytes));
            ValidateNonNegative(maxFrameCount, nameof(maxFrameCount));
            ValidateNonNegative(maxCanvasDimension, nameof(maxCanvasDimension));
            ValidateNonNegative(maxCanvasArea, nameof(maxCanvasArea));
            ValidateNonNegative(maxLocalFrameArea, nameof(maxLocalFrameArea));
            ValidateNonNegative(maxTotalDecodedPixels, nameof(maxTotalDecodedPixels));
            ValidateNonNegative(maxSingleRowBytes, nameof(maxSingleRowBytes));
            ValidateNonNegative(maxSingleFrameCompressedBytes, nameof(maxSingleFrameCompressedBytes));
            ValidateNonNegative(maxCommandsPerRow, nameof(maxCommandsPerRow));
            ValidateNonNegative(maxCommandsPerFrame, nameof(maxCommandsPerFrame));
            ValidateNonNegative(maxAllocatedBytes, nameof(maxAllocatedBytes));
            ValidateNonNegative(maxDescriptors, nameof(maxDescriptors));
            ValidateNonNegative(maxSubwindows, nameof(maxSubwindows));
            ValidateNonNegative(maxDiagnostics, nameof(maxDiagnostics));

            MaxInputBytes = maxInputBytes;
            MaxSingleReadBytes = maxSingleReadBytes;
            MaxFrameCount = maxFrameCount;
            MaxCanvasDimension = maxCanvasDimension;
            MaxCanvasArea = maxCanvasArea;
            MaxLocalFrameArea = maxLocalFrameArea;
            MaxTotalDecodedPixels = maxTotalDecodedPixels;
            MaxSingleRowBytes = maxSingleRowBytes;
            MaxSingleFrameCompressedBytes = maxSingleFrameCompressedBytes;
            MaxCommandsPerRow = maxCommandsPerRow;
            MaxCommandsPerFrame = maxCommandsPerFrame;
            MaxAllocatedBytes = maxAllocatedBytes;
            MaxDescriptors = maxDescriptors;
            MaxSubwindows = maxSubwindows;
            MaxDiagnostics = maxDiagnostics;
        }

        public static ShpTsReadLimits Default { get; } = new ShpTsReadLimits(
            128L * 1024 * 1024,
            1024 * 1024,
            65535,
            32768,
            256L * 1024 * 1024,
            64L * 1024 * 1024,
            512L * 1024 * 1024,
            65535,
            64L * 1024 * 1024,
            65535,
            16L * 1024 * 1024,
            768L * 1024 * 1024,
            65535,
            131072,
            4096);

        public long MaxInputBytes { get; }
        public long MaxSingleReadBytes { get; }
        public int MaxFrameCount { get; }
        public int MaxCanvasDimension { get; }
        public long MaxCanvasArea { get; }
        public long MaxLocalFrameArea { get; }
        public long MaxTotalDecodedPixels { get; }
        public int MaxSingleRowBytes { get; }
        public long MaxSingleFrameCompressedBytes { get; }
        public int MaxCommandsPerRow { get; }
        public long MaxCommandsPerFrame { get; }
        public long MaxAllocatedBytes { get; }
        public long MaxDescriptors { get; }
        public long MaxSubwindows { get; }
        public int MaxDiagnostics { get; }

        internal BinaryReadLimits ToBinaryLimits()
        {
            return new BinaryReadLimits(
                MaxInputBytes,
                MaxSingleReadBytes,
                MaxAllocatedBytes,
                MaxDescriptors,
                0,
                2,
                MaxSubwindows);
        }

        private static void ValidateNonNegative(long value, string parameterName)
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(parameterName);
            }
        }
    }
}
