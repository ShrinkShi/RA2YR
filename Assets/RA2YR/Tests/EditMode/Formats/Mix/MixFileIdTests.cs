using System;
using System.Globalization;
using NUnit.Framework;
using RA2YR.Core.Formats.Mix;

namespace RA2YR.Tests.EditMode.Formats.Mix
{
    public sealed class MixFileIdTests
    {
        [TestCase("A", 0xeb978531u)]
        [TestCase("AB", 0x421faa6eu)]
        [TestCase("ABC", 0x33aff496u)]
        [TestCase("ABCD", 0xdb1720a5u)]
        [TestCase("file.bin", 0x30e620ffu)]
        [TestCase("folder/file.bin", 0xb25590bdu)]
        [TestCase("folder\\file.bin", 0xb25590bdu)]
        [TestCase("isotem.pal", 0x5f9d97b9u)]
        [TestCase("temperat.pal", 0x9c58de40u)]
        [TestCase("unittem.pal", 0x63da7359u)]
        [TestCase("rulesmd.ini", 0x8218f9f4u)]
        [TestCase("artmd.ini", 0x5b47d8d5u)]
        [TestCase("ai.ini", 0x9e11e49au)]
        [TestCase("ra2md.csf", 0xbd835079u)]
        [TestCase("local mix database.dat", 0x366e051fu)]
        public void CandidateNamesMatchFixedXccAndBaselineVectors(
            string archiveName,
            uint expected)
        {
            Assert.That(MixFileId.ComputeCandidateId(archiveName).Value, Is.EqualTo(expected));
        }

        [Test]
        public void AsciiCaseIsInvariantUnderTurkishCulture()
        {
            CultureInfo previousCulture = CultureInfo.CurrentCulture;
            CultureInfo previousUiCulture = CultureInfo.CurrentUICulture;
            try
            {
                CultureInfo turkish = CultureInfo.GetCultureInfo("tr-TR");
                CultureInfo.CurrentCulture = turkish;
                CultureInfo.CurrentUICulture = turkish;

                MixFileId lower = MixFileId.ComputeCandidateId("isotem.pal");
                MixFileId upper = MixFileId.ComputeCandidateId("ISOTEM.PAL");

                Assert.That(lower, Is.EqualTo(upper));
                Assert.That(lower.Value, Is.EqualTo(0x5f9d97b9u));
            }
            finally
            {
                CultureInfo.CurrentCulture = previousCulture;
                CultureInfo.CurrentUICulture = previousUiCulture;
            }
        }

        [Test]
        public void FullArchiveNameIsNotImplicitlyReducedToBasename()
        {
            Assert.That(
                MixFileId.ComputeCandidateId("folder/file.bin"),
                Is.Not.EqualTo(MixFileId.ComputeCandidateId("file.bin")));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("C:relative.bin")]
        [TestCase("C:\\absolute.bin")]
        [TestCase("/rooted.bin")]
        [TestCase("\\rooted.bin")]
        [TestCase("folder//file.bin")]
        [TestCase("folder/./file.bin")]
        [TestCase("folder/../file.bin")]
        [TestCase("file\0.bin")]
        [TestCase("caf\u00e9.bin")]
        public void UnsafeOrNonAsciiCandidateNamesAreRejected(string archiveName)
        {
            Assert.Catch<ArgumentException>(() => MixFileId.ComputeCandidateId(archiveName));
        }

        [Test]
        public void NumericUnknownIdDoesNotInventAName()
        {
            MixFileId id = MixFileId.FromRaw(0xdeadbeefu);

            Assert.That(id.Value, Is.EqualTo(0xdeadbeefu));
            Assert.That(id.ToString(), Is.EqualTo("0xDEADBEEF"));
        }

        [Test]
        public void ValueOrderingAndHashingAreStable()
        {
            MixFileId lower = MixFileId.FromRaw(1);
            MixFileId upper = MixFileId.FromRaw(0x80000000u);

            Assert.That(lower.CompareTo(upper), Is.LessThan(0));
            Assert.That(lower.GetHashCode(), Is.EqualTo(1));
            Assert.That(upper.GetHashCode(), Is.EqualTo(unchecked((int)0x80000000u)));
        }
    }
}
