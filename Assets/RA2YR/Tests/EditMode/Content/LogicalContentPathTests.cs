using System;
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
        public void SameInputHasStableCanonicalFormAndHash()
        {
            LogicalContentPath first = LogicalContentPath.Parse("Taunts/taunt01.wav");
            LogicalContentPath second = LogicalContentPath.Parse("Taunts/taunt01.wav");

            Assert.That(first.Value, Is.EqualTo(second.Value));
            Assert.That(first.GetHashCode(), Is.EqualTo(second.GetHashCode()));
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
    }
}
