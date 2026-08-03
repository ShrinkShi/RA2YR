using System;
using System.Globalization;
using System.Linq;
using System.Text;
using NUnit.Framework;
using RA2YR.Core.Binary;
using RA2YR.Core.Configuration.Ini.Resolution;
using RA2YR.Core.Configuration.Ini.Typed;
using RA2YR.Core.Content;
using RA2YR.Core.Formats.Ini;

namespace RA2YR.Tests.EditMode.Configuration.Ini.Typed
{
    public sealed class IniTypedScalarParserTests
    {
        [Test]
        public void CompleteResolutionProducesRawValueWithWinnerTrace()
        {
            IniResolvedValue value = ResolvedValue("Token");

            IniTypedParseResult result = IniTypedScalarParser.ParseRaw(value);

            Assert.That(result.Status, Is.EqualTo(IniTypedValueStatus.Present));
            Assert.That(Encoding.ASCII.GetString(result.Value.CopyRawBytes()), Is.EqualTo("Token"));
            Assert.That(result.Value.SourceTrace.Winner.CandidateId, Is.EqualTo("candidate-only"));
            Assert.That(result.Value.SourceTrace.Winner.SourceId, Is.EqualTo("source-only"));
        }

        [TestCase("Name_01", true)]
        [TestCase("NAME.SHP", true)]
        [TestCase("two words", false)]
        [TestCase("bad;value", false)]
        public void AsciiIdentifierUsesExplicitBoundedGrammar(
            string text,
            bool valid)
        {
            IniTypedParseResult result = IniTypedScalarParser.ParseAsciiIdentifier(
                ResolvedValue(text));

            Assert.That(result.Status, Is.EqualTo(valid
                ? IniTypedValueStatus.Present
                : IniTypedValueStatus.Invalid));
        }

        [Test]
        public void NonAsciiIdentifierIsInvalidWithoutCodePageGuessing()
        {
            byte[] bytes = { 0x80 };

            IniTypedParseResult result = IniTypedScalarParser.ParseAsciiIdentifier(
                ResolvedValue(bytes));

            Assert.That(result.Status, Is.EqualTo(IniTypedValueStatus.Invalid));
            Assert.That(result.Value.CopyRawBytes(), Is.EqualTo(bytes));
        }

        [TestCase("yes", false, true)]
        [TestCase("no", false, false)]
        [TestCase("YES", true, true)]
        [TestCase("No", true, false)]
        public void BooleanUsesExplicitCasePolicy(
            string text,
            bool ignoreCase,
            bool expected)
        {
            IniTypedParseResult result = IniTypedScalarParser.ParseBoolean(
                ResolvedValue(text),
                ignoreCase
                    ? IniBooleanCasePolicy.OrdinalIgnoreCaseAscii
                    : IniBooleanCasePolicy.OrdinalLowercase);

            Assert.That(result.Status, Is.EqualTo(IniTypedValueStatus.Present));
            Assert.That(result.Value.BooleanValue, Is.EqualTo(expected));
        }

        [Test]
        public void LowercaseBooleanPolicyRejectsUppercaseToken()
        {
            IniTypedParseResult result = IniTypedScalarParser.ParseBoolean(
                ResolvedValue("YES"),
                IniBooleanCasePolicy.OrdinalLowercase);

            Assert.That(result.Status, Is.EqualTo(IniTypedValueStatus.Invalid));
            Assert.That(result.Diagnostics.Single().Code,
                Is.EqualTo(IniTypedDiagnosticCode.InvalidBoolean));
        }

        [TestCase("true")]
        [TestCase("1")]
        [TestCase("")]
        public void BooleanRejectsNonYesNoTokens(string text)
        {
            Assert.That(
                IniTypedScalarParser.ParseBoolean(
                    ResolvedValue(text),
                    IniBooleanCasePolicy.OrdinalIgnoreCaseAscii).Status,
                Is.EqualTo(IniTypedValueStatus.Invalid));
        }

        [TestCase("0", 0)]
        [TestCase("2147483647", int.MaxValue)]
        public void NonNegativeIntegerAcceptsBoundedDecimal(string text, int expected)
        {
            IniTypedParseResult result = IniTypedScalarParser.ParseNonNegativeInteger(
                ResolvedValue(text));

            Assert.That(result.Status, Is.EqualTo(IniTypedValueStatus.Present));
            Assert.That(result.Value.IntegerValue, Is.EqualTo(expected));
        }

        [Test]
        public void NonNegativeIntegerRejectsNegativeValue()
        {
            IniTypedParseResult result = IniTypedScalarParser.ParseNonNegativeInteger(
                ResolvedValue("-1"));

            Assert.That(result.Status, Is.EqualTo(IniTypedValueStatus.Invalid));
            Assert.That(result.Diagnostics.Single().Code,
                Is.EqualTo(IniTypedDiagnosticCode.InvalidNonNegativeInteger));
        }

        [Test]
        public void NonNegativeIntegerReportsOverflow()
        {
            IniTypedParseResult result = IniTypedScalarParser.ParseNonNegativeInteger(
                ResolvedValue("2147483648"));

            Assert.That(result.Status, Is.EqualTo(IniTypedValueStatus.Invalid));
            Assert.That(result.Diagnostics.Single().Code,
                Is.EqualTo(IniTypedDiagnosticCode.IntegerOverflow));
        }

        [Test]
        public void IdentifierListPreservesOrder()
        {
            IniTypedParseResult result = IniTypedScalarParser.ParseIdentifierList(
                ResolvedValue("One,TWO,three"));

            Assert.That(result.Status, Is.EqualTo(IniTypedValueStatus.Present));
            Assert.That(result.Value.Identifiers, Is.EqualTo(new[] { "One", "TWO", "three" }));
        }

        [TestCase(",One")]
        [TestCase("One,,Two")]
        [TestCase("One,")]
        public void IdentifierListRejectsEmptyItems(string text)
        {
            IniTypedParseResult result = IniTypedScalarParser.ParseIdentifierList(
                ResolvedValue(text));

            Assert.That(result.Status, Is.EqualTo(IniTypedValueStatus.Invalid));
            Assert.That(result.Diagnostics.Single().Code,
                Is.EqualTo(IniTypedDiagnosticCode.EmptyIdentifierListItem));
        }

        [Test]
        public void ScalarBudgetFailsWithoutAValue()
        {
            var limits = new IniTypedViewLimits(3, 10, 10, 10, 10, 10);

            IniTypedParseResult result = IniTypedScalarParser.ParseRaw(
                ResolvedValue("four"),
                limits);

            Assert.That(result.Status, Is.EqualTo(IniTypedValueStatus.Failed));
            Assert.That(result.Value, Is.Null);
            Assert.That(result.Diagnostics.Single().Code,
                Is.EqualTo(IniTypedDiagnosticCode.ScalarBudgetExceeded));
        }

        [Test]
        public void ListBudgetFailsWithoutEnumeratingAnUnboundedResult()
        {
            var limits = new IniTypedViewLimits(100, 2, 10, 10, 10, 10);

            IniTypedParseResult result = IniTypedScalarParser.ParseIdentifierList(
                ResolvedValue("One,Two,Three"),
                limits);

            Assert.That(result.Status, Is.EqualTo(IniTypedValueStatus.Failed));
            Assert.That(result.Diagnostics.Single().Code,
                Is.EqualTo(IniTypedDiagnosticCode.ListItemBudgetExceeded));
        }

        [Test]
        public void CurrentCultureDoesNotChangeTypedParsing()
        {
            CultureInfo original = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = new CultureInfo("tr-TR");
                IniTypedParseResult identifier = IniTypedScalarParser.ParseAsciiIdentifier(
                    ResolvedValue("INFANTRY"));
                IniTypedParseResult boolean = IniTypedScalarParser.ParseBoolean(
                    ResolvedValue("YES"),
                    IniBooleanCasePolicy.OrdinalIgnoreCaseAscii);

                Assert.That(identifier.Status, Is.EqualTo(IniTypedValueStatus.Present));
                Assert.That(boolean.Value.BooleanValue, Is.True);
            }
            finally
            {
                CultureInfo.CurrentCulture = original;
            }
        }

        [Test]
        public void TypedValuesReturnRawByteCopies()
        {
            IniTypedValue value = IniTypedScalarParser.ParseRaw(
                ResolvedValue("abc")).Value;
            byte[] first = value.CopyRawBytes();

            first[0] = 0;

            Assert.That(Encoding.ASCII.GetString(value.CopyRawBytes()), Is.EqualTo("abc"));
        }

        private static IniResolvedValue ResolvedValue(string value)
        {
            return ResolvedValue(Encoding.ASCII.GetBytes(value));
        }

        private static IniResolvedValue ResolvedValue(byte[] value)
        {
            byte[] prefix = Encoding.ASCII.GetBytes("[S]\nK=");
            byte[] content = prefix.Concat(value).ToArray();
            LogicalContentPath logicalName = LogicalContentPath.Parse("rulesmd.ini");
            IniParseResult parsed = WestwoodIniReader.Read(
                content,
                new BinarySourceContext("wp02g2-tests", "source-only", logicalName),
                new IniSourceProvenance("source-only", new[] { logicalName }));
            Assert.That(parsed.IsSuccess, Is.True);
            var layer = new IniLoadLayer(
                "layer-only",
                "source-only",
                IniLoadLayerKind.TestSource,
                new[] { logicalName },
                10,
                Evidence());
            var candidate = new IniCandidateDocument(
                "candidate-only",
                "layer-only",
                logicalName,
                parsed.Document);
            IniResolutionResult result = new IniRuntimeResolver().Resolve(
                new IniLoadPlan("wp02g2-tests", new[] { layer }),
                new[] { candidate },
                Policy());
            Assert.That(result.Status, Is.EqualTo(IniResolutionStatus.Complete),
                string.Join(",", result.Diagnostics.Select(item => item.Code)));
            return result.Sections.Single().Values.Single();
        }

        private static IniResolutionPolicy Policy()
        {
            IniResolutionEvidence evidence = Evidence();
            return new IniResolutionPolicy(
                IniFileCompositionPolicy.SelectHighestPriorityDocument, evidence,
                IniNameComparisonPolicy.OrdinalIgnoreCaseAscii, evidence,
                IniDuplicateSectionPolicy.MergeSectionsInFileOrder, evidence,
                IniDuplicateKeyPolicy.LastKeyWins, evidence,
                IniInlineCommentPolicy.PreserveSemicolonInValue, evidence,
                IniWhitespaceReadPolicy.Preserve, evidence,
                IniEmptyValuePolicy.OverridesEarlierValue, evidence);
        }

        private static IniResolutionEvidence Evidence()
        {
            return new IniResolutionEvidence(
                IniResolutionEvidenceLevel.ConfiguredForTesting,
                "wp02g2-synthetic-policy");
        }
    }
}
