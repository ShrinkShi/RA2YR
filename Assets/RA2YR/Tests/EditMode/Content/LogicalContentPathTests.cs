using System;
using System.Collections.Generic;
using System.Globalization;
using NUnit.Framework;
using RA2YR.Core.Content;

namespace RA2YR.Tests.EditMode.Content
{
    public sealed class LogicalContentPathTests
    {
        [Test]
        public void ParsePreservesOriginalCaseAndUsesForwardSlashes()
        {
            LogicalContentPath path = LogicalContentPath.Parse("Data/MAPSMD03.MIX");

            Assert.That(path.Value, Is.EqualTo("Data/MAPSMD03.MIX"));
            Assert.That(path.ToString(), Is.EqualTo(path.Value));
        }

        [TestCase("")]
        [TestCase("/rooted.mix")]
        [TestCase("C:/absolute.mix")]
        [TestCase("folder\\file.mix")]
        [TestCase("folder//file.mix")]
        [TestCase("folder/./file.mix")]
        [TestCase("folder/../file.mix")]
        [TestCase("folder/")]
        [TestCase("CON")]
        [TestCase("nul.txt")]
        [TestCase("trailing. ")]
        [TestCase("unsafe?.mix")]
        public void ParseRejectsUnsafeOrNonCanonicalPaths(string value)
        {
            Assert.Throws<ArgumentException>(() => LogicalContentPath.Parse(value));
        }

        [Test]
        public void ParseRejectsNulAndInvalidUtf16()
        {
            Assert.Throws<ArgumentException>(() => LogicalContentPath.Parse("bad\0name.mix"));
            Assert.Throws<ArgumentException>(() => LogicalContentPath.Parse("bad\ud800name.mix"));
            Assert.Throws<ArgumentException>(() => LogicalContentPath.Parse("bad\ufdd0name.mix"));
            Assert.Throws<ArgumentException>(() => LogicalContentPath.Parse("bad\ud83f\udffename.mix"));
        }

        [Test]
        public void EqualityOrderingAndHashAreOrdinalIgnoreCase()
        {
            LogicalContentPath upper = LogicalContentPath.Parse("RULESMD.INI");
            LogicalContentPath lower = LogicalContentPath.Parse("rulesmd.ini");

            Assert.That(upper, Is.EqualTo(lower));
            Assert.That(upper.CompareTo(lower), Is.Zero);
            Assert.That(upper.GetHashCode(), Is.EqualTo(lower.GetHashCode()));
        }

        [Test]
        public void BehaviorDoesNotDependOnCurrentCulture()
        {
            CultureInfo previous = CultureInfo.CurrentCulture;
            CultureInfo previousUi = CultureInfo.CurrentUICulture;
            try
            {
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("tr-TR");
                CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("tr-TR");
                LogicalContentPath first = LogicalContentPath.Parse("INI/RULES.INI");
                int firstHash = first.GetHashCode();

                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
                CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en-US");
                LogicalContentPath second = LogicalContentPath.Parse("ini/rules.ini");

                Assert.That(first, Is.EqualTo(second));
                Assert.That(first.CompareTo(second), Is.Zero);
                Assert.That(firstHash, Is.EqualTo(second.GetHashCode()));
            }
            finally
            {
                CultureInfo.CurrentCulture = previous;
                CultureInfo.CurrentUICulture = previousUi;
            }
        }

        [Test]
        public void RepeatedInputHasConsistentCanonicalFormAndDeterministicCollectionHash()
        {
            LogicalContentPath first = LogicalContentPath.Parse("Taunts/taunt01.wav");
            LogicalContentPath second = LogicalContentPath.Parse("Taunts/taunt01.wav");

            Assert.That(first.Value, Is.EqualTo(second.Value));
            Assert.That(first.GetHashCode(), Is.EqualTo(second.GetHashCode()));
        }

        [Test]
        public void AsciiCollectionHashUsesDeterministicWholeStringFolding()
        {
            LogicalContentPath path = LogicalContentPath.Parse("rulesmd.ini");

            Assert.That(path.GetHashCode(), Is.EqualTo(unchecked((int)0xae87b755)));
        }

        [TestCase("A", "a")]
        [TestCase("\u00c5", "\u00e5")]
        [TestCase("\u03a3", "\u03c3")]
        [TestCase("\u03a3", "\u03c2")]
        [TestCase("\u0416", "\u0436")]
        [TestCase("I", "\u0131")]
        [TestCase("i", "\u0130")]
        [TestCase("S", "\u017f")]
        [TestCase("K", "\u212a")]
        public void OrdinalIgnoreCaseEqualityAndHashContractCoversUnicodeBmpPairs(
            string leftSegment,
            string rightSegment)
        {
            AssertEqualityHashContract(leftSegment, rightSegment);
        }

        [TestCase(0x10400, 0x10428)]
        [TestCase(0x104b0, 0x104d8)]
        [TestCase(0x10c80, 0x10cc0)]
        [TestCase(0x118a0, 0x118c0)]
        public void OrdinalIgnoreCaseEqualityAndHashContractCoversSupplementaryPairs(
            int upperScalar,
            int lowerScalar)
        {
            AssertEqualityHashContract(
                char.ConvertFromUtf32(upperScalar),
                char.ConvertFromUtf32(lowerScalar));
        }

        [TestCase("v1 at C:\\Users\\Example\\YR")]
        [TestCase("v1 at /home/example/yr")]
        [TestCase("v1 at \\\\server\\share")]
        public void PublicSourceVersionRejectsEmbeddedAbsolutePaths(string version)
        {
            using (var temporary = new TemporaryContentTestDirectory())
            {
                string root = temporary.CreateDirectory("External");
                Assert.Throws<ArgumentException>(() =>
                    new ExternalContentSourceDescriptor(
                        "source",
                        ContentSourceKind.Other,
                        root,
                        1,
                        version,
                        true));
            }
        }

        private static void AssertEqualityHashContract(string leftSegment, string rightSegment)
        {
            LogicalContentPath left = LogicalContentPath.Parse(
                "Unicode/" + leftSegment + ".mix");
            LogicalContentPath right = LogicalContentPath.Parse(
                "Unicode/" + rightSegment + ".mix");
            bool expected = StringComparer.OrdinalIgnoreCase.Equals(
                left.Value,
                right.Value);

            Assert.That(left.Equals(right), Is.EqualTo(expected));
            Assert.That(left.CompareTo(right) == 0, Is.EqualTo(expected));
            if (expected)
            {
                Assert.That(left.GetHashCode(), Is.EqualTo(right.GetHashCode()));
                Assert.That(new HashSet<LogicalContentPath> { left }.Contains(right), Is.True);
            }
        }
    }
}
