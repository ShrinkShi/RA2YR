using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using NUnit.Framework;
using RA2YR.Core.Binary;
using RA2YR.Core.Content;
using RA2YR.Core.Formats.Mix;
using RA2YR.Core.Formats.Mix.Crypto;
using RA2YR.Core.Formats.Mix.Writing;

namespace RA2YR.Tests.EditMode.Formats.Mix
{
    [TestFixture]
    public sealed class MixArchiveWriterTests
    {
        private static readonly byte[] SyntheticKeySource = Hex(
            "02000000000000000000000000000000000000000000000000000000000000000000000000000000" +
            "03000000000000000000000000000000000000000000000000000000000000000000000000000000");

        [Test]
        public void EmptyClassicArchiveHasExactHeader()
        {
            MixWriteResult result = MixArchiveWriter.Build(
                Array.Empty<MixWriteEntry>(),
                Classic());

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.GetArchiveBytes(), Is.EqualTo(new byte[6]));
            Assert.That(result.ArchiveSize, Is.EqualTo(6));
            Assert.That(result.Sha256, Is.EqualTo(Sha256(new byte[6])));
        }

        [Test]
        public void SingleEntryDirectoryAndPayloadAreWrittenLittleEndian()
        {
            MixWriteResult result = MixArchiveWriter.Build(
                new[] { Entry(0x11223344u, 0xaa, 0xbb, 0xcc) },
                Classic());
            byte[] archive = result.GetArchiveBytes();

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(ReadUInt16(archive, 0), Is.EqualTo(1));
            Assert.That(ReadUInt32(archive, 2), Is.EqualTo(3));
            Assert.That(ReadUInt32(archive, 6), Is.EqualTo(0x11223344u));
            Assert.That(ReadUInt32(archive, 10), Is.EqualTo(0));
            Assert.That(ReadUInt32(archive, 14), Is.EqualTo(3));
            Assert.That(archive.Skip(18).ToArray(), Is.EqualTo(Hex("AABBCC")));
        }

        [Test]
        public void ZeroLengthEntryDoesNotAdvanceFollowingOffset()
        {
            MixWriteResult result = MixArchiveWriter.Build(
                new[]
                {
                    Entry(1u),
                    Entry(2u, 0x42)
                },
                Preserve());
            byte[] archive = result.GetArchiveBytes();

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(ReadUInt32(archive, 10), Is.EqualTo(0));
            Assert.That(ReadUInt32(archive, 14), Is.EqualTo(0));
            Assert.That(ReadUInt32(archive, 22), Is.EqualTo(0));
            Assert.That(ReadUInt32(archive, 26), Is.EqualTo(1));
            Assert.That(archive[30], Is.EqualTo(0x42));
        }

        [Test]
        public void DeterministicRebuildSortsEntriesByUnsignedId()
        {
            MixWriteResult result = MixArchiveWriter.Build(
                new[]
                {
                    Entry(uint.MaxValue, 3),
                    Entry(7u, 2),
                    Entry(0u, 1)
                },
                Classic());
            byte[] archive = result.GetArchiveBytes();

            Assert.That(ReadUInt32(archive, 6), Is.EqualTo(0u));
            Assert.That(ReadUInt32(archive, 18), Is.EqualTo(7u));
            Assert.That(ReadUInt32(archive, 30), Is.EqualTo(uint.MaxValue));
            Assert.That(archive.Skip(42).ToArray(), Is.EqualTo(new byte[] { 1, 2, 3 }));
        }

        [Test]
        public void PreserveEntryOrderKeepsObservedDirectoryOrder()
        {
            MixWriteResult result = MixArchiveWriter.Build(
                new[]
                {
                    Entry(9u, 9),
                    Entry(1u, 1),
                    Entry(5u, 5)
                },
                Preserve());
            byte[] archive = result.GetArchiveBytes();

            Assert.That(ReadUInt32(archive, 6), Is.EqualTo(9u));
            Assert.That(ReadUInt32(archive, 18), Is.EqualTo(1u));
            Assert.That(ReadUInt32(archive, 30), Is.EqualTo(5u));
            Assert.That(archive.Skip(42).ToArray(), Is.EqualTo(new byte[] { 9, 1, 5 }));
        }

        [Test]
        public void InputPermutationDoesNotChangeDeterministicRebuild()
        {
            var entries = new[]
            {
                Entry(0x30u, 3),
                Entry(0x10u, 1),
                Entry(0x20u, 2)
            };

            byte[] first = MixArchiveWriter.Build(entries, Classic()).GetArchiveBytes();
            byte[] second = MixArchiveWriter.Build(entries.Reverse().ToArray(), Classic()).GetArchiveBytes();

            Assert.That(second, Is.EqualTo(first));
            Assert.That(Sha256(second), Is.EqualTo(Sha256(first)));
        }

        [Test]
        public void SameInputProducesIdenticalBytesAndHash()
        {
            var entries = new[] { Entry(1u, 1, 2, 3), Entry(2u, 4, 5) };

            MixWriteResult first = MixArchiveWriter.Build(entries, Classic());
            MixWriteResult second = MixArchiveWriter.Build(entries, Classic());

            Assert.That(second.GetArchiveBytes(), Is.EqualTo(first.GetArchiveBytes()));
            Assert.That(second.Sha256, Is.EqualTo(first.Sha256));
        }

        [Test]
        public void DuplicateIdsAreRejectedWithoutOutput()
        {
            MixWriteResult result = MixArchiveWriter.Build(
                new[] { Entry(7u, 1), Entry(7u, 2) },
                Classic());

            AssertFailure(result, MixWriteDiagnosticCode.DuplicateEntryId);
            Assert.That(result.Diagnostics[0].EntryIndex, Is.EqualTo(1));
            Assert.That(result.Diagnostics[0].EntryId, Is.EqualTo(MixFileId.FromRaw(7u)));
        }

        [Test]
        public void NullEntryIsRejectedWithoutPartialOutput()
        {
            MixWriteResult result = MixArchiveWriter.Build(
                new MixWriteEntry[] { Entry(1u), null },
                Classic());

            AssertFailure(result, MixWriteDiagnosticCode.InvalidEntry);
            Assert.That(result.Diagnostics[0].EntryIndex, Is.EqualTo(1));
        }

        [Test]
        public void EntryCountBudgetIsEnforcedBeforeAllocation()
        {
            MixWriteResult result = MixArchiveWriter.Build(
                new[] { Entry(1u), Entry(2u) },
                Classic(maxEntries: 1));

            AssertFailure(result, MixWriteDiagnosticCode.EntryBudgetExceeded);
        }

        [Test]
        public void ArchiveByteBudgetIsEnforcedBeforeOutputAllocation()
        {
            MixWriteResult result = MixArchiveWriter.Build(
                new[] { Entry(1u, 1, 2, 3) },
                Classic(maxBytes: 20));

            AssertFailure(result, MixWriteDiagnosticCode.ArchiveSizeBudgetExceeded);
        }

        [TestCase(true, false)]
        [TestCase(false, true)]
        public void ClassicHeaderRejectsFlaggedFeatures(bool checksum, bool encrypted)
        {
            MixWriteResult result = MixArchiveWriter.Build(
                Array.Empty<MixWriteEntry>(),
                new MixWriteOptions(
                    MixWriteOrder.DeterministicRebuild,
                    MixWriteHeaderKind.Classic,
                    checksum,
                    encrypted ? SyntheticKeySource : null,
                    ushort.MaxValue,
                    1024 * 1024));

            AssertFailure(result, MixWriteDiagnosticCode.InvalidOptionCombination);
        }

        [Test]
        public void UnflaggedExtendedHeaderIsRejectedAsAmbiguous()
        {
            MixWriteResult result = MixArchiveWriter.Build(
                Array.Empty<MixWriteEntry>(),
                Extended(false, null));

            AssertFailure(result, MixWriteDiagnosticCode.InvalidOptionCombination);
        }

        [Test]
        public void ChecksumHeaderHashesOnlyContinuousPayloadRegion()
        {
            MixWriteResult result = MixArchiveWriter.Build(
                new[] { Entry(9u, 1, 2), Entry(3u, 3, 4, 5) },
                Extended(true, null));
            byte[] archive = result.GetArchiveBytes();
            int dataStart = 4 + 6 + (2 * 12);

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(ReadUInt32(archive, 0), Is.EqualTo(0x00010000u));
            Assert.That(
                archive.Skip(dataStart + 5).ToArray(),
                Is.EqualTo(Sha1(archive, dataStart, 5)));
        }

        [Test]
        public void EncryptedDirectoryMatchesIndependentSyntheticVector()
        {
            MixWriteResult result = MixArchiveWriter.Build(
                new[] { Entry(0x11223344u, 0xaa, 0xbb, 0xcc) },
                Extended(false, SyntheticKeySource));
            byte[] archive = result.GetArchiveBytes();

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(archive.Length, Is.EqualTo(111));
            Assert.That(ReadUInt32(archive, 0), Is.EqualTo(0x00020000u));
            Assert.That(
                archive.Skip(84).Take(24).ToArray(),
                Is.EqualTo(Hex("BE78E7D244A51CAB4B8F387E6A978CE124BA67967F4E3AD6")));
            Assert.That(archive.Skip(108).ToArray(), Is.EqualTo(Hex("AABBCC")));
            Assert.That(
                result.Sha256,
                Is.EqualTo("9832B7A025F0819B4FEF37A876D23B5B7875BCD08B5E249316E50E018BB6CD8A"));
        }

        [Test]
        public void EncryptedChecksumArchiveMatchesIndependentSyntheticVector()
        {
            MixWriteResult result = MixArchiveWriter.Build(
                new[] { Entry(0x11223344u, 0xaa, 0xbb, 0xcc) },
                Extended(true, SyntheticKeySource));
            byte[] archive = result.GetArchiveBytes();

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(ReadUInt32(archive, 0), Is.EqualTo(0x00030000u));
            Assert.That(archive.Length, Is.EqualTo(131));
            Assert.That(archive.Skip(111).ToArray(), Is.EqualTo(Sha1(archive, 108, 3)));
            Assert.That(
                result.Sha256,
                Is.EqualTo("3648B69DB0D14B5E96B5AD0FC2F89D37982A2EA0FBFF346940C8FA2D72D1C055"));
        }

        [Test]
        public void EncryptedDirectoryPaddingIsZeroBeforeEncryption()
        {
            MixWriteResult result = MixArchiveWriter.Build(
                new[] { Entry(0x11223344u, 0xaa) },
                Extended(false, SyntheticKeySource));
            byte[] archive = result.GetArchiveBytes();
            byte[] key = WestwoodMixKeyDeriver.Derive(SyntheticKeySource).GetKeyMaterial();
            var cipher = new BlowfishCipher(key);
            var plaintext = new byte[24];

            for (int offset = 0; offset < plaintext.Length; offset += 8)
            {
                cipher.DecryptWestwoodLittleEndianWordBlock(
                    archive.AsSpan(84 + offset, 8),
                    plaintext.AsSpan(offset, 8));
            }

            Assert.That(plaintext.Skip(18).ToArray(), Is.EqualTo(new byte[6]));
        }

        [Test]
        public void EverySupportedWriterModeReparsesWithTheBoundedMixReader()
        {
            var options = new[]
            {
                Classic(),
                Extended(true, null),
                Extended(false, SyntheticKeySource),
                Extended(true, SyntheticKeySource)
            };
            var entries = new[]
            {
                Entry(0x11223344u, 1, 2, 3),
                Entry(0x55667788u)
            };

            foreach (MixWriteOptions option in options)
            {
                MixWriteResult written = MixArchiveWriter.Build(entries, option);
                var source = new BinarySourceContext(
                    "MIX writer test",
                    "synthetic",
                    LogicalContentPath.Parse("synthetic.mix"));
                MixArchiveReadResult read = MixArchiveReader.Read(
                    written.GetArchiveBytes(),
                    source);

                Assert.That(read.IsSuccess, Is.True,
                    read.Diagnostics.Count == 0 ? null : read.Diagnostics[0].Message);
                using (read.Archive)
                {
                    Assert.That(read.Archive.Entries, Has.Count.EqualTo(2));
                    Assert.That(read.Archive.HasChecksum, Is.EqualTo(option.IncludeChecksum));
                    Assert.That(read.Archive.IsEncrypted, Is.EqualTo(option.IsEncrypted));
                }
            }
        }

        [TestCase(0)]
        [TestCase(79)]
        [TestCase(81)]
        public void InvalidEncryptionKeySourceHasControlledDiagnostic(int length)
        {
            MixWriteResult result = MixArchiveWriter.Build(
                Array.Empty<MixWriteEntry>(),
                Extended(false, new byte[length]));

            AssertFailure(result, MixWriteDiagnosticCode.EncryptionKeyRejected);
        }

        [Test]
        public void EntryAndResultDoNotExposeMutableBuffers()
        {
            byte[] source = { 1, 2, 3 };
            var entry = new MixWriteEntry(MixFileId.FromRaw(1u), source);
            source[0] = 99;
            MixWriteResult result = MixArchiveWriter.Build(new[] { entry }, Classic());
            byte[] first = result.GetArchiveBytes();
            first[first.Length - 1] = 77;

            Assert.That(entry.GetPayloadCopy(), Is.EqualTo(new byte[] { 1, 2, 3 }));
            Assert.That(result.GetArchiveBytes().Last(), Is.EqualTo(3));
        }

        [Test]
        public void OptionsDoNotExposeMutableKeySource()
        {
            byte[] source = (byte[])SyntheticKeySource.Clone();
            MixWriteOptions options = Extended(false, source);
            source[0] = 99;

            MixWriteResult result = MixArchiveWriter.Build(
                new[] { Entry(0x11223344u, 0xaa, 0xbb, 0xcc) },
                options);

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(
                result.Sha256,
                Is.EqualTo("9832B7A025F0819B4FEF37A876D23B5B7875BCD08B5E249316E50E018BB6CD8A"));
        }

        [Test]
        public void ExplicitTemporaryTargetIsFlushedVerifiedAndCommitted()
        {
            using (var temporary = new TemporaryDirectory())
            {
                string output = Path.Combine(temporary.Path, "synthetic.mix");

                MixWriteResult result = MixArchiveWriter.WriteToFile(
                    new[] { Entry(1u, 1, 2, 3) },
                    Classic(),
                    output,
                    temporary.Path,
                    MixOutputPurpose.TemporaryTestDirectory,
                    false);

                Assert.That(result.IsSuccess, Is.True);
                Assert.That(result.CommittedToFile, Is.True);
                Assert.That(result.WrittenFileVerified, Is.True);
                Assert.That(File.ReadAllBytes(output), Is.EqualTo(result.GetArchiveBytes()));
                Assert.That(result.Sha256, Is.EqualTo(Sha256(File.ReadAllBytes(output))));
                Assert.That(
                    result.Diagnostics.Any(value =>
                        value.Message.Contains(temporary.Path)),
                    Is.False);
            }
        }

        [Test]
        public void ExistingTargetRequiresExplicitOverwriteAndLeavesNoTemporaryFile()
        {
            using (var temporary = new TemporaryDirectory())
            {
                string output = Path.Combine(temporary.Path, "existing.mix");
                File.WriteAllBytes(output, new byte[] { 9 });

                MixWriteResult result = MixArchiveWriter.WriteToFile(
                    new[] { Entry(1u, 1) },
                    Classic(),
                    output,
                    temporary.Path,
                    MixOutputPurpose.TestResults,
                    false);

                AssertFailure(result, MixWriteDiagnosticCode.OutputAlreadyExists);
                Assert.That(File.ReadAllBytes(output), Is.EqualTo(new byte[] { 9 }));
                Assert.That(
                    Directory.GetFiles(temporary.Path, "*.ra2yr-mix.tmp"),
                    Is.Empty);
            }
        }

        [Test]
        public void ExplicitOverwriteAtomicallyReplacesExistingTestOutput()
        {
            using (var temporary = new TemporaryDirectory())
            {
                string output = Path.Combine(temporary.Path, "replace.mix");
                File.WriteAllBytes(output, new byte[] { 9 });

                MixWriteResult result = MixArchiveWriter.WriteToFile(
                    new[] { Entry(1u, 7) },
                    Classic(),
                    output,
                    temporary.Path,
                    MixOutputPurpose.Cache,
                    true);

                Assert.That(result.IsSuccess, Is.True);
                Assert.That(File.ReadAllBytes(output), Is.EqualTo(result.GetArchiveBytes()));
            }
        }

        [Test]
        public void RelativeAndMissingDirectoryTargetsAreRejectedWithoutWriting()
        {
            MixWriteResult relative = MixArchiveWriter.WriteToFile(
                Array.Empty<MixWriteEntry>(),
                Classic(),
                "relative.mix",
                Path.GetTempPath(),
                MixOutputPurpose.TestResults,
                false);
            string missing = Path.Combine(
                Path.GetTempPath(),
                "RA2YR.Missing." + Guid.NewGuid().ToString("N"),
                "output.mix");
            MixWriteResult absent = MixArchiveWriter.WriteToFile(
                Array.Empty<MixWriteEntry>(),
                Classic(),
                missing,
                Path.GetTempPath(),
                MixOutputPurpose.TestResults,
                false);

            AssertFailure(relative, MixWriteDiagnosticCode.OutputPathInvalid);
            AssertFailure(absent, MixWriteDiagnosticCode.OutputDirectoryMissing);
            Assert.That(File.Exists(missing), Is.False);
        }

        [Test]
        public void InvalidOutputPurposeFailsClosed()
        {
            using (var temporary = new TemporaryDirectory())
            {
                MixWriteResult result = MixArchiveWriter.WriteToFile(
                    Array.Empty<MixWriteEntry>(),
                    Classic(),
                    Path.Combine(temporary.Path, "output.mix"),
                    temporary.Path,
                    (MixOutputPurpose)999,
                    false);

                AssertFailure(result, MixWriteDiagnosticCode.OutputPurposeInvalid);
                Assert.That(Directory.GetFiles(temporary.Path), Is.Empty);
            }
        }

        [Test]
        public void ApprovedRootRejectsTraversalAndSamePrefixSibling()
        {
            using (var temporary = new TemporaryDirectory())
            {
                string sibling = temporary.Path + "-sibling";
                Directory.CreateDirectory(sibling);
                try
                {
                    string traversal = Path.Combine(temporary.Path, "..", "escaped.mix");
                    string samePrefix = Path.Combine(sibling, "escaped.mix");

                    MixWriteResult traversed = MixArchiveWriter.WriteToFile(
                        Array.Empty<MixWriteEntry>(),
                        Classic(),
                        traversal,
                        temporary.Path,
                        MixOutputPurpose.TestResults,
                        false);
                    MixWriteResult prefixed = MixArchiveWriter.WriteToFile(
                        Array.Empty<MixWriteEntry>(),
                        Classic(),
                        samePrefix,
                        temporary.Path,
                        MixOutputPurpose.TestResults,
                        false);

                    AssertFailure(traversed, MixWriteDiagnosticCode.OutputPathInvalid);
                    AssertFailure(prefixed, MixWriteDiagnosticCode.OutputPathInvalid);
                    Assert.That(File.Exists(traversal), Is.False);
                    Assert.That(File.Exists(samePrefix), Is.False);
                }
                finally
                {
                    Directory.Delete(sibling, false);
                }
            }
        }

        [Test]
        public void ApprovedRootItselfCannotBeUsedAsOutputFile()
        {
            using (var temporary = new TemporaryDirectory())
            {
                MixWriteResult result = MixArchiveWriter.WriteToFile(
                    Array.Empty<MixWriteEntry>(),
                    Classic(),
                    temporary.Path,
                    temporary.Path,
                    MixOutputPurpose.TestResults,
                    false);

                AssertFailure(result, MixWriteDiagnosticCode.OutputPathInvalid);
            }
        }

        [Test]
        public void ReparsePointInsideApprovedRootIsRejectedOnWindows()
        {
            if (Path.DirectorySeparatorChar != '\\')
            {
                Assert.Ignore("Windows junction behavior is validated on the primary platform.");
            }

            using (var temporary = new TemporaryDirectory())
            {
                string target = Path.Combine(temporary.Path, "target");
                string junction = Path.Combine(temporary.Path, "junction");
                Directory.CreateDirectory(target);
                try
                {
                    var startInfo = new ProcessStartInfo
                    {
                        FileName = Environment.GetEnvironmentVariable("ComSpec"),
                        Arguments = "/d /c mklink /J \"" + junction + "\" \"" + target + "\"",
                        CreateNoWindow = true,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    };
                    using (Process process = Process.Start(startInfo))
                    {
                        if (process == null)
                        {
                            Assert.Ignore("The Windows command processor is unavailable.");
                        }

                        if (!process.WaitForExit(10000))
                        {
                            process.Kill();
                            Assert.Fail("Creating the bounded test junction timed out.");
                        }

                        if (process.ExitCode != 0)
                        {
                            Assert.Ignore("A test junction could not be created on this host.");
                        }
                    }

                    MixWriteResult result = MixArchiveWriter.WriteToFile(
                        Array.Empty<MixWriteEntry>(),
                        Classic(),
                        Path.Combine(junction, "output.mix"),
                        temporary.Path,
                        MixOutputPurpose.TemporaryTestDirectory,
                        false);

                    AssertFailure(result, MixWriteDiagnosticCode.OutputReparsePointRejected);
                    Assert.That(File.Exists(Path.Combine(target, "output.mix")), Is.False);
                }
                finally
                {
                    if (Directory.Exists(junction))
                    {
                        Directory.Delete(junction, false);
                    }

                    if (Directory.Exists(target))
                    {
                        Directory.Delete(target, false);
                    }
                }
            }
        }

        private static MixWriteEntry Entry(uint id, params byte[] payload)
        {
            return new MixWriteEntry(MixFileId.FromRaw(id), payload);
        }

        private static MixWriteOptions Classic(
            int maxEntries = ushort.MaxValue,
            long maxBytes = 1024 * 1024)
        {
            return new MixWriteOptions(
                MixWriteOrder.DeterministicRebuild,
                MixWriteHeaderKind.Classic,
                false,
                null,
                maxEntries,
                maxBytes);
        }

        private static MixWriteOptions Preserve()
        {
            return new MixWriteOptions(
                MixWriteOrder.PreserveEntryOrder,
                MixWriteHeaderKind.Classic,
                false,
                null,
                ushort.MaxValue,
                1024 * 1024);
        }

        private static MixWriteOptions Extended(bool checksum, byte[] keySource)
        {
            return new MixWriteOptions(
                MixWriteOrder.DeterministicRebuild,
                MixWriteHeaderKind.Extended,
                checksum,
                keySource,
                ushort.MaxValue,
                1024 * 1024);
        }

        private static void AssertFailure(
            MixWriteResult result,
            MixWriteDiagnosticCode code)
        {
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.GetArchiveBytes(), Is.Empty);
            Assert.That(result.Sha256, Is.Null);
            Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(result.Diagnostics[0].Code, Is.EqualTo(code));
        }

        private static ushort ReadUInt16(byte[] value, int offset)
        {
            return (ushort)(value[offset] | (value[offset + 1] << 8));
        }

        private static uint ReadUInt32(byte[] value, int offset)
        {
            return value[offset] |
                ((uint)value[offset + 1] << 8) |
                ((uint)value[offset + 2] << 16) |
                ((uint)value[offset + 3] << 24);
        }

        private static byte[] Sha1(byte[] value, int offset, int length)
        {
            using (SHA1 sha1 = SHA1.Create())
            {
                return sha1.ComputeHash(value, offset, length);
            }
        }

        private static string Sha256(byte[] value)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                return BitConverter.ToString(sha256.ComputeHash(value))
                    .Replace("-", string.Empty);
            }
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

        private sealed class TemporaryDirectory : IDisposable
        {
            public TemporaryDirectory()
            {
                Path = System.IO.Path.Combine(
                    System.IO.Path.GetTempPath(),
                    "RA2YR.MixWriter.Tests",
                    Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(Path);
            }

            public string Path { get; }

            public void Dispose()
            {
                if (!Directory.Exists(Path))
                {
                    return;
                }

                foreach (string file in Directory.GetFiles(Path))
                {
                    File.SetAttributes(file, FileAttributes.Normal);
                    File.Delete(file);
                }

                Directory.Delete(Path, false);
            }
        }
    }
}
