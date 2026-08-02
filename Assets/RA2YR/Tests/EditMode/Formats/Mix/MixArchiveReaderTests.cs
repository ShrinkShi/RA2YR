using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using NUnit.Framework;
using RA2YR.Core.Binary;
using RA2YR.Core.Content;
using RA2YR.Core.Formats.Mix;
using RA2YR.Core.Formats.Mix.Crypto;

namespace RA2YR.Tests.EditMode.Formats.Mix
{
    [TestFixture]
    public sealed class MixArchiveReaderTests
    {
        [Test]
        public void SixByteZeroHeaderIsAnExplicitClassicEmptyArchive()
        {
            MixArchiveReadResult result = Read(new byte[6]);

            AssertSuccess(result);
            using (result.Archive)
            {
                Assert.That(result.Archive.HeaderKind, Is.EqualTo(MixArchiveHeaderKind.Classic));
                Assert.That(result.Archive.Flags, Is.EqualTo(MixArchiveFlags.None));
                Assert.That(result.Archive.Entries, Is.Empty);
                Assert.That(result.Archive.DeclaredDataSize, Is.Zero);
                Assert.That(result.Archive.PayloadRelativeOffset, Is.EqualTo(6));
            }
        }

        [TestCase(7)]
        [TestCase(8)]
        [TestCase(9)]
        public void ShortZeroPaddedClassicEmptyIsNotPromotedToExtended(int length)
        {
            MixArchiveReadResult result = Read(new byte[length]);

            AssertFailure(result, MixDiagnosticCode.UnexpectedTrailingData);
            Assert.That(
                result.Diagnostics.Single().FieldOrSection,
                Is.EqualTo("mix-trailing-data"));
        }

        [Test]
        public void TenByteZeroHeaderIsAnXccCompatibleExtendedEmptyArchive()
        {
            MixArchiveReadResult result = Read(new byte[10]);

            AssertSuccess(result);
            using (result.Archive)
            {
                Assert.That(result.Archive.HeaderKind, Is.EqualTo(MixArchiveHeaderKind.Extended));
                Assert.That(result.Archive.Flags, Is.EqualTo(MixArchiveFlags.None));
                Assert.That(result.Archive.Entries, Is.Empty);
                Assert.That(result.Archive.PayloadRelativeOffset, Is.EqualTo(10));
            }
        }

        [Test]
        public void ZeroFlagExtendedEntryUsesThePostFlagDirectory()
        {
            byte[] input = BuildPlainExtended(
                MixArchiveFlags.None,
                new[] { Entry(0x11223344, 0, 3) },
                new byte[] { 7, 8, 9 });

            MixArchiveReadResult result = Read(input);

            AssertSuccess(result);
            using (result.Archive)
            {
                Assert.That(result.Archive.HeaderKind, Is.EqualTo(MixArchiveHeaderKind.Extended));
                Assert.That(result.Archive.Flags, Is.EqualTo(MixArchiveFlags.None));
                Assert.That(result.Archive.Entries.Single().Id.Value, Is.EqualTo(0x11223344u));
                Assert.That(result.Archive.PayloadRelativeOffset, Is.EqualTo(22));
            }
        }

        [Test]
        public void ClassicSingleEntryExposesBoundedPayloadWithoutNameFabrication()
        {
            byte[] payload = { 1, 2, 3 };
            byte[] input = BuildClassic(
                new[] { Entry(0x11223344, 0, 3) },
                payload);

            MixArchiveReadResult result = Read(input);

            AssertSuccess(result);
            using (result.Archive)
            {
                MixArchiveEntry entry = result.Archive.Entries.Single();
                Assert.That(entry.Id, Is.EqualTo(MixFileId.FromRaw(0x11223344)));
                Assert.That(entry.RelativeOffset, Is.Zero);
                Assert.That(entry.Length, Is.EqualTo(3));
                var actual = new byte[3];
                entry.OpenPayloadWindow().ReadExactly(0, actual, 0, 3, "test-payload");
                Assert.That(actual, Is.EqualTo(payload));
            }
        }

        [Test]
        public void MultipleEntriesAndZeroByteEntryPreserveDirectoryOrder()
        {
            byte[] input = BuildClassic(
                new[]
                {
                    Entry(30, 2, 2),
                    Entry(10, 0, 0),
                    Entry(20, 0, 2)
                },
                new byte[] { 4, 5, 6, 7 });

            MixArchiveReadResult result = Read(input);

            AssertSuccess(result);
            using (result.Archive)
            {
                Assert.That(
                    result.Archive.Entries.Select(entry => entry.Id.Value),
                    Is.EqualTo(new uint[] { 30, 10, 20 }));
                Assert.That(result.Archive.Entries[1].Length, Is.Zero);
                Assert.That(result.Archive.Entries[1].OpenPayloadWindow().Length, Is.Zero);
            }
        }

        [Test]
        public void ExtendedChecksumArchiveValidatesPayloadOnly()
        {
            byte[] input = BuildPlainExtended(
                MixArchiveFlags.Checksum,
                new[] { Entry(1, 0, 4) },
                new byte[] { 8, 7, 6, 5 });

            MixArchiveReadResult result = Read(input);

            AssertSuccess(result);
            using (result.Archive)
            {
                Assert.That(result.Archive.HeaderKind, Is.EqualTo(MixArchiveHeaderKind.Extended));
                Assert.That(result.Archive.HasChecksum, Is.True);
                Assert.That(result.Archive.ChecksumVerified, Is.True);
            }
        }

        [Test]
        public void ExtendedChecksumMismatchFailsWithoutPartialArchive()
        {
            byte[] input = BuildPlainExtended(
                MixArchiveFlags.Checksum,
                new[] { Entry(1, 0, 1) },
                new byte[] { 9 });
            input[input.Length - 1] ^= 0xff;

            AssertFailure(Read(input), MixDiagnosticCode.ChecksumMismatch);
        }

        [Test]
        public void EncryptedDirectoryVectorParsesAndPreservesKeySource()
        {
            byte[] keySource = SyntheticKeySource();
            byte[] input = BuildEncrypted(
                MixArchiveFlags.EncryptedDirectory,
                keySource,
                new[] { Entry(0x11223344, 0, 3) },
                new byte[] { 0xaa, 0xbb, 0xcc });

            MixArchiveReadResult result = Read(input);

            AssertSuccess(result);
            using (result.Archive)
            {
                Assert.That(result.Archive.IsEncrypted, Is.True);
                Assert.That(result.Archive.GetKeySource(), Is.EqualTo(keySource));
                Assert.That(result.Archive.Entries.Single().Id.Value, Is.EqualTo(0x11223344));
                Assert.That(Sha256(input), Is.EqualTo(
                    "9832b7a025f0819b4fef37a876d23b5b7875bcd08b5e249316e50e018bb6cd8a"));
            }
        }

        [Test]
        public void EncryptedChecksummedVectorValidatesBothCapabilities()
        {
            byte[] input = BuildEncrypted(
                MixArchiveFlags.EncryptedDirectory | MixArchiveFlags.Checksum,
                SyntheticKeySource(),
                new[] { Entry(0x11223344, 0, 3) },
                new byte[] { 0xaa, 0xbb, 0xcc });

            MixArchiveReadResult result = Read(input);

            AssertSuccess(result);
            using (result.Archive)
            {
                Assert.That(result.Archive.ChecksumVerified, Is.True);
                Assert.That(Sha256(input), Is.EqualTo(
                    "3648b69db0d14b5e96b5ad0fc2f89d37982a2ea0fbff346940c8fa2d72d1c055"));
            }
        }

        [Test]
        public void InvalidEncryptedKeySourceFailsClosed()
        {
            byte[] input = BuildEncrypted(
                MixArchiveFlags.EncryptedDirectory,
                SyntheticKeySource(),
                new[] { Entry(1, 0, 1) },
                new byte[] { 1 });
            byte[] modulusLittleEndian = Hex(
                "157F43AA3D4FFBD1E6C1B0F86A0EDDAB4AB08266FA54AAE8" +
                "A23F7151D6605156E4FC396D08DABC51");
            Buffer.BlockCopy(modulusLittleEndian, 0, input, 4, modulusLittleEndian.Length);

            MixArchiveReadResult result = Read(input);

            AssertFailure(result, MixDiagnosticCode.DecryptionFailed);
            Assert.That(result.Diagnostics.Single().FieldOrSection, Is.EqualTo("mix-key-source"));
        }

        [Test]
        public void CorruptedEncryptedDirectoryCannotReturnPartialSuccess()
        {
            byte[] input = BuildEncrypted(
                MixArchiveFlags.EncryptedDirectory,
                SyntheticKeySource(),
                new[] { Entry(1, 0, 1) },
                new byte[] { 1 });
            for (int index = 84; index < 92; index++)
            {
                input[index] ^= 0xff;
            }

            MixArchiveReadResult result = Read(input);

            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Archive, Is.Null);
            Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(3)]
        public void TruncatedDiscriminatorHasSpecificDiagnostic(int length)
        {
            AssertFailure(Read(new byte[length]), MixDiagnosticCode.TruncatedHeader);
        }

        [Test]
        public void TruncatedClassicCountAndSizeHeaderIsRejected()
        {
            AssertFailure(
                Read(new byte[] { 1, 0, 0, 0, 0 }),
                MixDiagnosticCode.TruncatedHeader);
        }

        [Test]
        public void TruncatedEncryptedHeaderIsRejectedBeforeDecryption()
        {
            byte[] input = new byte[91];
            WriteUInt32(input, 0, (uint)MixArchiveFlags.EncryptedDirectory);

            AssertFailure(Read(input), MixDiagnosticCode.TruncatedHeader);
        }

        [Test]
        public void TruncatedDirectoryIsRejectedBeforeEntryAccess()
        {
            byte[] input = BuildClassic(
                new[] { Entry(1, 0, 1) },
                new byte[] { 1 });
            Array.Resize(ref input, 17);

            AssertFailure(Read(input), MixDiagnosticCode.TruncatedDirectory);
        }

        [Test]
        public void TruncatedPayloadIsRejected()
        {
            byte[] input = BuildClassic(
                new[] { Entry(1, 0, 3) },
                new byte[] { 1, 2, 3 });
            Array.Resize(ref input, input.Length - 1);

            AssertFailure(Read(input), MixDiagnosticCode.TruncatedDataRegion);
        }

        [Test]
        public void TruncatedChecksumIsNotReportedAsOrdinaryEof()
        {
            byte[] input = BuildPlainExtended(
                MixArchiveFlags.Checksum,
                Array.Empty<EntrySpec>(),
                Array.Empty<byte>());
            Array.Resize(ref input, input.Length - 1);

            AssertFailure(Read(input), MixDiagnosticCode.TruncatedChecksum);
        }

        [Test]
        public void EntryCountBudgetFailsBeforeDirectoryAllocation()
        {
            byte[] input = BuildClassic(
                new[] { Entry(1, 0, 0) },
                Array.Empty<byte>());

            AssertFailure(
                Read(input, Limits(maxEntries: 0)),
                MixDiagnosticCode.EntryCountBudgetExceeded);
        }

        [Test]
        public void DirectoryByteBudgetFailsBeforeDirectoryAllocation()
        {
            byte[] input = BuildClassic(
                new[] { Entry(1, 0, 0) },
                Array.Empty<byte>());

            AssertFailure(
                Read(input, Limits(maxDirectoryBytes: 17)),
                MixDiagnosticCode.DirectoryBudgetExceeded);
        }

        [Test]
        public void EntryOffsetPlusLengthOverflowIsDistinctFromOutOfBounds()
        {
            byte[] input = BuildClassic(
                new[] { Entry(1, uint.MaxValue, 2) },
                Array.Empty<byte>());

            MixArchiveReadResult result = Read(input);

            AssertFailure(result, MixDiagnosticCode.EntryRangeOverflow);
            Assert.That(result.Diagnostics.Single().EntryIndex, Is.Zero);
            Assert.That(result.Diagnostics.Single().EntryId.Value.Value, Is.EqualTo(1));
        }

        [Test]
        public void EntryCannotCrossDeclaredDataRegion()
        {
            byte[] input = BuildClassic(
                new[] { Entry(1, 1, 1) },
                new byte[] { 0 });

            AssertFailure(Read(input), MixDiagnosticCode.EntryOutsideDataRegion);
        }

        [Test]
        public void DuplicateNumericIdsAreRejected()
        {
            byte[] input = BuildClassic(
                new[] { Entry(7, 0, 1), Entry(7, 1, 1) },
                new byte[] { 1, 2 });

            MixArchiveReadResult result = Read(input);

            AssertFailure(result, MixDiagnosticCode.DuplicateEntryId);
            Assert.That(result.Diagnostics.Single().EntryIndex, Is.EqualTo(1));
            Assert.That(result.Diagnostics.Single().AbsoluteOffset, Is.EqualTo(18));
        }

        [Test]
        public void EncryptedEntryDiagnosticPointsToContainingCiphertextBlock()
        {
            byte[] input = BuildEncrypted(
                MixArchiveFlags.EncryptedDirectory,
                SyntheticKeySource(),
                new[] { Entry(7, 0, 1), Entry(7, 1, 1) },
                new byte[] { 1, 2 });

            MixArchiveReadResult result = Read(input);

            AssertFailure(result, MixDiagnosticCode.DuplicateEntryId);
            Assert.That(result.Diagnostics.Single().EntryIndex, Is.EqualTo(1));
            Assert.That(result.Diagnostics.Single().AbsoluteOffset, Is.EqualTo(100));
        }

        [Test]
        public void NonEmptyOverlappingEntriesAreRejected()
        {
            byte[] input = BuildClassic(
                new[] { Entry(1, 0, 2), Entry(2, 1, 2) },
                new byte[] { 1, 2, 3 });

            AssertFailure(Read(input), MixDiagnosticCode.OverlappingEntries);
        }

        [Test]
        public void ZeroByteEntriesDoNotCreateFalseOverlap()
        {
            byte[] input = BuildClassic(
                new[] { Entry(1, 0, 0), Entry(2, 0, 1) },
                new byte[] { 1 });

            MixArchiveReadResult result = Read(input);

            AssertSuccess(result);
            result.Archive.Dispose();
        }

        [TestCase(0x00040000u)]
        [TestCase(0x80000000u)]
        [TestCase(0x00050000u)]
        public void UnknownExtendedFlagBitsFailClosed(uint flags)
        {
            var input = new byte[10];
            WriteUInt32(input, 0, flags);

            AssertFailure(Read(input), MixDiagnosticCode.UnsupportedFlags);
        }

        [Test]
        public void BytesAfterDeclaredArchiveEndAreRejected()
        {
            byte[] input = BuildClassic(Array.Empty<EntrySpec>(), Array.Empty<byte>());
            Array.Resize(ref input, input.Length + 1);

            AssertFailure(Read(input), MixDiagnosticCode.UnexpectedTrailingData);
        }

        [Test]
        public void ShortReadSeekableStreamMatchesMemoryResult()
        {
            byte[] input = BuildClassic(
                new[] { Entry(3, 0, 3) },
                new byte[] { 7, 8, 9 });
            MixArchiveReadResult memory = Read(input);
            var stream = new ShortReadSeekableStream(input, 1);

            MixArchiveReadResult streamed = MixArchiveReader.Read(
                stream,
                0,
                input.Length,
                Source(),
                Limits(),
                true);

            AssertSuccess(memory);
            AssertSuccess(streamed);
            using (memory.Archive)
            using (streamed.Archive)
            {
                Assert.That(streamed.Archive.Entries.Single().Id,
                    Is.EqualTo(memory.Archive.Entries.Single().Id));
                Assert.That(stream.ReadCallCount, Is.GreaterThan(3));
            }

            stream.Dispose();
        }

        [Test]
        public void NonzeroRootWindowKeepsAbsolutePayloadOffsets()
        {
            byte[] archive = BuildClassic(
                new[] { Entry(1, 0, 1) },
                new byte[] { 0x5a });
            var wrapped = new byte[archive.Length + 9];
            Buffer.BlockCopy(archive, 0, wrapped, 5, archive.Length);
            var stream = new TrackingSeekableStream(wrapped);

            MixArchiveReadResult result = MixArchiveReader.Read(
                stream,
                5,
                archive.Length,
                Source(),
                Limits(),
                true);

            AssertSuccess(result);
            using (result.Archive)
            {
                Assert.That(result.Archive.Entries.Single().PayloadAbsoluteOffset,
                    Is.EqualTo(23));
            }

            stream.Dispose();
        }

        [Test]
        public void SeekableStreamOwnershipFollowsLeaveOpenPolicy()
        {
            byte[] input = BuildClassic(Array.Empty<EntrySpec>(), Array.Empty<byte>());
            var owned = new TrackingSeekableStream(input);
            MixArchiveReadResult ownedResult = MixArchiveReader.Read(
                owned,
                0,
                input.Length,
                Source(),
                Limits(),
                false);
            AssertSuccess(ownedResult);
            Assert.That(owned.WasDisposed, Is.False);
            ownedResult.Archive.Dispose();
            Assert.That(owned.WasDisposed, Is.True);

            var borrowed = new TrackingSeekableStream(input);
            MixArchiveReadResult borrowedResult = MixArchiveReader.Read(
                borrowed,
                0,
                input.Length,
                Source(),
                Limits(),
                true);
            AssertSuccess(borrowedResult);
            borrowedResult.Archive.Dispose();
            Assert.That(borrowed.WasDisposed, Is.False);
            borrowed.Dispose();
        }

        [Test]
        public void UnchecksummedOpenDoesNotReadEntryPayload()
        {
            byte[] input = BuildClassic(
                new[] { Entry(1, 0, 64) },
                Enumerable.Range(0, 64).Select(value => (byte)value).ToArray());
            var stream = new TrackingSeekableStream(input);

            MixArchiveReadResult result = MixArchiveReader.Read(
                stream,
                0,
                input.Length,
                Source(),
                Limits(),
                true);

            AssertSuccess(result);
            using (result.Archive)
            {
                Assert.That(stream.HighestReadEnd, Is.LessThanOrEqualTo(18));
                var oneByte = new byte[1];
                result.Archive.Entries[0].OpenPayloadWindow().ReadExactly(
                    63,
                    oneByte,
                    0,
                    1,
                    "test-payload");
                Assert.That(oneByte[0], Is.EqualTo(63));
                Assert.That(stream.HighestReadEnd, Is.EqualTo(input.Length));
            }

            stream.Dispose();
        }

        [Test]
        public void BackingReadFailureIsStructuredAndSanitized()
        {
            byte[] input = BuildClassic(Array.Empty<EntrySpec>(), Array.Empty<byte>());
            var stream = new ThrowingSeekableStream(input);

            MixArchiveReadResult result = MixArchiveReader.Read(
                stream,
                0,
                input.Length,
                Source(),
                Limits(),
                true);

            AssertFailure(result, MixDiagnosticCode.BinaryReadFailure);
            Assert.That(result.Diagnostics.Single().BinaryCode,
                Is.EqualTo(BinaryDiagnosticCode.ReadFailure));
            Assert.That(result.Diagnostics.Single().Message, Does.Not.Contain("private"));
            Assert.That(result.Diagnostics.Single().Message, Does.Not.Contain(":"));
            stream.Dispose();
        }

        [Test]
        public void FailureResultCannotExposePartialArchive()
        {
            byte[] input = BuildClassic(
                new[] { Entry(1, 0, 2) },
                new byte[] { 1 });

            MixArchiveReadResult result = Read(input);

            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Archive, Is.Null);
            Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
        }

        [Test]
        public void ArchiveSizeAndWindowBudgetsAreEnforced()
        {
            byte[] input = BuildClassic(Array.Empty<EntrySpec>(), Array.Empty<byte>());
            AssertFailure(
                Read(input, Limits(maxArchiveBytes: 5)),
                MixDiagnosticCode.BinaryReadFailure);

            byte[] oneEntry = BuildClassic(
                new[] { Entry(1, 0, 0) },
                Array.Empty<byte>());
            AssertFailure(
                Read(oneEntry, Limits(maxWindows: 1)),
                MixDiagnosticCode.BinaryReadFailure);
        }

        [Test]
        public void CumulativeAllocationBudgetIncludesMemorySnapshotAndDirectoryBuffers()
        {
            byte[] input = BuildClassic(
                new[] { Entry(1, 0, 0) },
                Array.Empty<byte>());

            AssertFailure(
                Read(input, Limits(maxAllocatedBytes: input.Length)),
                MixDiagnosticCode.AllocationBudgetExceeded);
        }

        private static MixArchiveReadResult Read(
            byte[] input,
            MixReadLimits limits = null)
        {
            return MixArchiveReader.Read(input, Source(), limits ?? Limits());
        }

        private static BinarySourceContext Source()
        {
            return new BinarySourceContext(
                "format.mix-container-read",
                "synthetic-source",
                LogicalContentPath.Parse("Synthetic/archive.mix"));
        }

        private static MixReadLimits Limits(
            long maxArchiveBytes = 1024 * 1024,
            int maxEntries = 1024,
            long maxDirectoryBytes = 1024 * 1024,
            long maxAllocatedBytes = 4 * 1024 * 1024,
            long maxTotalReadBytes = 4 * 1024 * 1024,
            long maxWindows = 2048,
            int maxWindowDepth = 16)
        {
            return new MixReadLimits(
                maxArchiveBytes,
                maxEntries,
                maxDirectoryBytes,
                maxAllocatedBytes,
                maxTotalReadBytes,
                maxWindows,
                maxWindowDepth);
        }

        private static EntrySpec Entry(uint id, uint offset, uint length)
        {
            return new EntrySpec(id, offset, length);
        }

        private static byte[] BuildClassic(
            IReadOnlyList<EntrySpec> entries,
            byte[] payload)
        {
            return BuildBody(entries, payload, 0);
        }

        private static byte[] BuildPlainExtended(
            MixArchiveFlags flags,
            IReadOnlyList<EntrySpec> entries,
            byte[] payload)
        {
            byte[] body = BuildBody(entries, payload, 4);
            WriteUInt32(body, 0, (uint)flags);
            return AppendChecksumIfRequested(body, payload, flags);
        }

        private static byte[] BuildBody(
            IReadOnlyList<EntrySpec> entries,
            byte[] payload,
            int prefixLength)
        {
            int headerOffset = prefixLength;
            int directoryOffset = headerOffset + 6;
            int payloadOffset = checked(directoryOffset + entries.Count * 12);
            var output = new byte[checked(payloadOffset + payload.Length)];
            WriteUInt16(output, headerOffset, checked((ushort)entries.Count));
            WriteUInt32(output, headerOffset + 2, checked((uint)payload.Length));
            for (int index = 0; index < entries.Count; index++)
            {
                int offset = directoryOffset + index * 12;
                WriteUInt32(output, offset, entries[index].Id);
                WriteUInt32(output, offset + 4, entries[index].Offset);
                WriteUInt32(output, offset + 8, entries[index].Length);
            }

            Buffer.BlockCopy(payload, 0, output, payloadOffset, payload.Length);
            return output;
        }

        private static byte[] BuildEncrypted(
            MixArchiveFlags flags,
            byte[] keySource,
            IReadOnlyList<EntrySpec> entries,
            byte[] payload)
        {
            int plainLength = checked(6 + entries.Count * 12);
            int encryptedLength = checked((plainLength + 7) / 8 * 8);
            var plain = new byte[encryptedLength];
            WriteUInt16(plain, 0, checked((ushort)entries.Count));
            WriteUInt32(plain, 2, checked((uint)payload.Length));
            for (int index = 0; index < entries.Count; index++)
            {
                int offset = 6 + index * 12;
                WriteUInt32(plain, offset, entries[index].Id);
                WriteUInt32(plain, offset + 4, entries[index].Offset);
                WriteUInt32(plain, offset + 8, entries[index].Length);
            }

            byte[] key = WestwoodMixKeyDeriver.Derive(keySource).GetKeyMaterial();
            var cipher = new BlowfishCipher(key);
            var encrypted = new byte[encryptedLength];
            for (int offset = 0; offset < encryptedLength; offset += 8)
            {
                cipher.EncryptWestwoodLittleEndianWordBlock(
                    plain.AsSpan(offset, 8),
                    encrypted.AsSpan(offset, 8));
            }

            var output = new byte[checked(4 + keySource.Length + encrypted.Length + payload.Length)];
            WriteUInt32(output, 0, (uint)flags);
            Buffer.BlockCopy(keySource, 0, output, 4, keySource.Length);
            Buffer.BlockCopy(encrypted, 0, output, 4 + keySource.Length, encrypted.Length);
            Buffer.BlockCopy(
                payload,
                0,
                output,
                4 + keySource.Length + encrypted.Length,
                payload.Length);
            return AppendChecksumIfRequested(output, payload, flags);
        }

        private static byte[] AppendChecksumIfRequested(
            byte[] archive,
            byte[] payload,
            MixArchiveFlags flags)
        {
            if ((flags & MixArchiveFlags.Checksum) == 0)
            {
                return archive;
            }

            byte[] checksum;
            using (SHA1 sha1 = SHA1.Create())
            {
                checksum = sha1.ComputeHash(payload);
            }

            int originalLength = archive.Length;
            Array.Resize(ref archive, checked(originalLength + checksum.Length));
            Buffer.BlockCopy(checksum, 0, archive, originalLength, checksum.Length);
            return archive;
        }

        private static byte[] SyntheticKeySource()
        {
            var result = new byte[WestwoodMixKeyDeriver.KeySourceLength];
            result[0] = 2;
            result[WestwoodMixKeyDeriver.CiphertextBlockLength] = 3;
            return result;
        }

        private static void WriteUInt16(byte[] target, int offset, ushort value)
        {
            target[offset] = (byte)value;
            target[offset + 1] = (byte)(value >> 8);
        }

        private static void WriteUInt32(byte[] target, int offset, uint value)
        {
            target[offset] = (byte)value;
            target[offset + 1] = (byte)(value >> 8);
            target[offset + 2] = (byte)(value >> 16);
            target[offset + 3] = (byte)(value >> 24);
        }

        private static byte[] Hex(string value)
        {
            var result = new byte[value.Length / 2];
            for (int index = 0; index < result.Length; index++)
            {
                result[index] = Convert.ToByte(value.Substring(index * 2, 2), 16);
            }

            return result;
        }

        private static string Sha256(byte[] bytes)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                return string.Concat(
                    sha256.ComputeHash(bytes).Select(value => value.ToString("x2")));
            }
        }

        private static void AssertSuccess(MixArchiveReadResult result)
        {
            Assert.That(result.IsSuccess, Is.True,
                result.Diagnostics.Count == 0 ? null : result.Diagnostics[0].Message);
            Assert.That(result.Archive, Is.Not.Null);
            Assert.That(result.Diagnostics, Is.Empty);
        }

        private static void AssertFailure(
            MixArchiveReadResult result,
            MixDiagnosticCode code)
        {
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Archive, Is.Null);
            Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(result.Diagnostics[0].Code, Is.EqualTo(code));
            Assert.That(result.Diagnostics[0].Source.LogicalPath.Value,
                Is.EqualTo("Synthetic/archive.mix"));
        }

        private readonly struct EntrySpec
        {
            public EntrySpec(uint id, uint offset, uint length)
            {
                Id = id;
                Offset = offset;
                Length = length;
            }

            public uint Id { get; }

            public uint Offset { get; }

            public uint Length { get; }
        }

        private class TrackingSeekableStream : MemoryStream
        {
            public TrackingSeekableStream(byte[] bytes)
                : base(bytes, false)
            {
            }

            public long HighestReadEnd { get; private set; }

            public int ReadCallCount { get; private set; }

            public bool WasDisposed { get; private set; }

            public override int Read(byte[] buffer, int offset, int count)
            {
                ReadCallCount++;
                int result = base.Read(buffer, offset, count);
                HighestReadEnd = Math.Max(HighestReadEnd, Position);
                return result;
            }

            protected override void Dispose(bool disposing)
            {
                WasDisposed = true;
                base.Dispose(disposing);
            }
        }

        private sealed class ShortReadSeekableStream : TrackingSeekableStream
        {
            private readonly int maxChunk;

            public ShortReadSeekableStream(byte[] bytes, int maxChunk)
                : base(bytes)
            {
                this.maxChunk = maxChunk;
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                return base.Read(buffer, offset, Math.Min(count, maxChunk));
            }
        }

        private sealed class ThrowingSeekableStream : TrackingSeekableStream
        {
            public ThrowingSeekableStream(byte[] bytes)
                : base(bytes)
            {
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                throw new IOException("Synthetic C:\\private\\archive.mix read failure.");
            }
        }
    }
}
