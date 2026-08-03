using System.Linq;
using System.Text;
using NUnit.Framework;
using RA2YR.Core.Binary;
using RA2YR.Core.Configuration.Ini.Resolution;
using RA2YR.Core.Content;
using RA2YR.Core.Formats.Ini;

namespace RA2YR.Tests.EditMode.Configuration.Ini
{
    public sealed class IniRuntimeSyntaxAuditTests
    {
        [Test]
        public void OpaqueLinesAreClassifiedBeforeInsideAndAfterSections()
        {
            IniRuntimeSyntaxAudit audit = Analyze(
                "before\n[S]\ninside\n[Broken] tail\nafter");

            Assert.That(audit.OpaqueBeforeSectionCount, Is.EqualTo(1));
            Assert.That(audit.OpaqueInsideSectionCount, Is.EqualTo(2));
            Assert.That(audit.OpaqueAfterSectionCount, Is.EqualTo(1));
        }

        [Test]
        public void OpaqueReasonCountsRemainAggregatedWithoutRawLines()
        {
            IniRuntimeSyntaxAudit audit = Analyze("missing\n[S]\nother");

            Assert.That(audit.OpaqueReasonCounts[IniOpaqueReason.MissingEquals],
                Is.EqualTo(2));
            Assert.That(audit.OpaquePatternCounts.Keys.All(value =>
                value.Contains("MissingEquals") && !value.Contains("missing") &&
                !value.Contains("other")), Is.True);
        }

        [Test]
        public void OpaqueEqualsAndKnownPunctuationAreCountedSeparately()
        {
            IniRuntimeSyntaxAudit audit = Analyze("=value\n#directive\nword");

            Assert.That(audit.OpaqueContainsEqualsCount, Is.EqualTo(1));
            Assert.That(audit.OpaqueKnownPunctuationCount, Is.EqualTo(2));
        }

        [Test]
        public void PotentialRuntimeImpactIncludesInsideEqualsAndStructuralPunctuation()
        {
            IniRuntimeSyntaxAudit audit = Analyze("plain\n[S]\ninside");

            Assert.That(audit.OpaquePotentialRuntimeImpactCount, Is.EqualTo(1));
            Assert.That(audit.MayAffectMinimalTypedView, Is.True);
        }

        [Test]
        public void SemicolonPositionsAreAggregatedWithoutValueText()
        {
            IniRuntimeSyntaxAudit audit = Analyze(
                "[S]\nA=;start\nB=middle;comment\nC=end;");

            Assert.That(audit.InlineSemicolonLineCount, Is.EqualTo(3));
            Assert.That(audit.SemicolonAtValueStartCount, Is.EqualTo(1));
            Assert.That(audit.SemicolonInValueMiddleCount, Is.EqualTo(1));
            Assert.That(audit.SemicolonAtValueEndCount, Is.EqualTo(1));
        }

        [Test]
        public void WhitespaceAroundSemicolonDoesNotChangePositionClass()
        {
            IniRuntimeSyntaxAudit audit = Analyze("[S]\nA=  ; comment\nB=value;  ");

            Assert.That(audit.SemicolonAtValueStartCount, Is.EqualTo(1));
            Assert.That(audit.SemicolonAtValueEndCount, Is.EqualTo(1));
        }

        [Test]
        public void StructuredDocumentProducesZeroOpaqueAuditCounts()
        {
            IniRuntimeSyntaxAudit audit = Analyze("[S]\nK=V");

            Assert.That(audit.OpaqueLineCount, Is.Zero);
            Assert.That(audit.OpaquePatternCounts, Is.Empty);
            Assert.That(audit.MayAffectMinimalTypedView, Is.False);
        }

        [Test]
        public void AuditDoesNotModifyTheLosslessDocument()
        {
            IniRawDocument document = Parse("[S]\nK=value;comment");
            string before = document.CanonicalModelSha256;

            IniRuntimeSyntaxAuditor.Analyze(document);

            Assert.That(document.CanonicalModelSha256, Is.EqualTo(before));
        }

        private static IniRuntimeSyntaxAudit Analyze(string text)
        {
            return IniRuntimeSyntaxAuditor.Analyze(Parse(text));
        }

        private static IniRawDocument Parse(string text)
        {
            LogicalContentPath path = LogicalContentPath.Parse("synthetic/audit.ini");
            IniParseResult result = WestwoodIniReader.Read(
                Encoding.ASCII.GetBytes(text),
                new BinarySourceContext("ini-runtime-syntax-audit", "synthetic-source", path),
                new IniSourceProvenance(
                    "synthetic-source",
                    new[] { LogicalContentPath.Parse("synthetic.mix"), path }));
            Assert.That(result.IsSuccess, Is.True);
            return result.Document;
        }
    }
}
