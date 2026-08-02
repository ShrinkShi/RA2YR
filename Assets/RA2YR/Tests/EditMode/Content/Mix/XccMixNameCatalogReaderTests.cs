using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using RA2YR.Core.Binary;
using RA2YR.Core.Content;
using RA2YR.Core.Content.Mix;
using RA2YR.Core.Formats.Mix;

namespace RA2YR.Tests.EditMode.Content.Mix
{
    public sealed class XccMixNameCatalogReaderTests
    {
        [Test]
        public void SelectsOnlyFourthRa2ListAndConsumesExactlyFourLists()
        {
            byte[] database = Database(
                List(Record("td.mix", "td-description")),
                List(Record("ra.mix", "ra-description")),
                List(Record("ts.mix", "ts-description")),
                List(
                    Record("folder\\rulesmd.ini", "private-description"),
                    Record("ra2md.csf", "another-description")));

            XccMixNameCatalogReadResult result = Read(database);

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(
                result.Names.Select(name => name.Value),
                Is.EqualTo(new[] { "folder/rulesmd.ini", "ra2md.csf" }));
            Assert.That(result.Catalog.ResolvedIdCount, Is.EqualTo(2));
            Assert.That(
                result.Catalog.TryResolve(
                    MixFileId.ComputeCandidateId("folder/rulesmd.ini"),
                    out LogicalContentPath resolved),
                Is.True);
            Assert.That(resolved.Value, Is.EqualTo("folder/rulesmd.ini"));
            Assert.That(
                string.Join("|", result.Names.Select(name => name.Value)),
                Does.Not.Contain("description"));
        }

        [Test]
        public void UnterminatedDescriptionFailsWithoutPartialNames()
        {
            byte[] valid = Database(
                List(),
                List(),
                List(),
                List(Record("rulesmd.ini", "description")));
            byte[] truncated = valid.Take(valid.Length - 1).ToArray();

            XccMixNameCatalogReadResult result = Read(truncated);

            AssertFailure(result, XccMixNameCatalogDiagnosticCode.UnterminatedString);
            Assert.That(result.Names, Is.Empty);
            Assert.That(result.Catalog, Is.Null);
        }

        [Test]
        public void NegativeListCountIsRejectedAtCountOffset()
        {
            byte[] input = { 0xff, 0xff, 0xff, 0xff };
            XccMixNameCatalogReadResult result = Read(input);

            AssertFailure(result, XccMixNameCatalogDiagnosticCode.InvalidListCount);
            Assert.That(result.Diagnostics.Single().AbsoluteOffset, Is.Zero);
            Assert.That(result.Diagnostics.Single().ListIndex, Is.Zero);
        }

        [Test]
        public void NonAsciiNameIsRejectedAtExactByte()
        {
            byte[] input =
            {
                1, 0, 0, 0,
                0x80, 0,
                0,
                0, 0, 0, 0,
                0, 0, 0, 0,
                0, 0, 0, 0
            };
            XccMixNameCatalogReadResult result = Read(input);

            AssertFailure(result, XccMixNameCatalogDiagnosticCode.NonAsciiString);
            Assert.That(result.Diagnostics.Single().AbsoluteOffset, Is.EqualTo(4));
            Assert.That(result.Diagnostics.Single().Field, Is.EqualTo("record-name"));
        }

        [Test]
        public void RecordBudgetCoversAllFourListsCumulatively()
        {
            byte[] input = Database(
                List(Record("a", "")),
                List(Record("b", "")),
                List(Record("c", "")),
                List(Record("d", "")));
            XccMixNameCatalogReadResult result = Read(
                input,
                Limits(maxRecords: 3));

            AssertFailure(result, XccMixNameCatalogDiagnosticCode.RecordBudgetExceeded);
            Assert.That(result.Diagnostics.Single().BinaryDiagnostic, Is.Not.Null);
        }

        [Test]
        public void StringBudgetAppliesBeforeScratchWriteCanCrossBoundary()
        {
            byte[] input = Database(
                List(),
                List(),
                List(),
                List(Record("abcd", "")));
            XccMixNameCatalogReadResult result = Read(
                input,
                Limits(maxStringLength: 3));

            AssertFailure(result, XccMixNameCatalogDiagnosticCode.StringBudgetExceeded);
        }

        [Test]
        public void AllocationBudgetIncludesScratchStringsAndCandidateModels()
        {
            byte[] input = Database(
                List(),
                List(),
                List(),
                List(Record("a", "")));
            XccMixNameCatalogReadResult result = Read(
                input,
                Limits(maxStringLength: 8, maxAllocatedBytes: 16));

            AssertFailure(result, XccMixNameCatalogDiagnosticCode.AllocationBudgetExceeded);
        }

        [Test]
        public void TrailingDataIsNotSilentlyIgnored()
        {
            byte[] valid = Database(List(), List(), List(), List());
            byte[] input = valid.Concat(new byte[] { 1 }).ToArray();
            XccMixNameCatalogReadResult result = Read(input);

            AssertFailure(result, XccMixNameCatalogDiagnosticCode.TrailingData);
            Assert.That(result.Diagnostics.Single().BinaryDiagnostic.Code,
                Is.EqualTo(BinaryDiagnosticCode.TrailingData));
        }

        [Test]
        public void InputBudgetFailsBeforeParsingOrPartialExposure()
        {
            byte[] input = Database(List(), List(), List(), List());
            XccMixNameCatalogReadResult result = Read(
                input,
                Limits(maxInputBytes: input.Length - 1));

            AssertFailure(result, XccMixNameCatalogDiagnosticCode.InputBudgetExceeded);
            Assert.That(result.Names, Is.Empty);
        }

        private static XccMixNameCatalogReadResult Read(
            byte[] input,
            XccMixNameCatalogLimits limits = null)
        {
            return XccMixNameCatalogReader.Read(
                input,
                new BinarySourceContext(
                    "content.xcc-name-catalog",
                    "synthetic-xcc",
                    LogicalContentPath.Parse("global-mix-database.dat")),
                limits ?? Limits());
        }

        private static XccMixNameCatalogLimits Limits(
            long maxInputBytes = 1024 * 1024,
            long maxRecords = 1000,
            int maxStringLength = 1024,
            long maxAllocatedBytes = 1024 * 1024)
        {
            return new XccMixNameCatalogLimits(
                maxInputBytes,
                maxRecords,
                maxStringLength,
                maxAllocatedBytes);
        }

        private static byte[] Database(params List<NameRecord>[] lists)
        {
            Assert.That(lists.Length, Is.EqualTo(4));
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream, Encoding.ASCII, true))
            {
                foreach (List<NameRecord> list in lists)
                {
                    writer.Write(list.Count);
                    foreach (NameRecord record in list)
                    {
                        WriteAscii(writer, record.Name);
                        WriteAscii(writer, record.Description);
                    }
                }

                return stream.ToArray();
            }
        }

        private static void WriteAscii(BinaryWriter writer, string value)
        {
            writer.Write(Encoding.ASCII.GetBytes(value));
            writer.Write((byte)0);
        }

        private static List<NameRecord> List(params NameRecord[] records)
        {
            return records.ToList();
        }

        private static NameRecord Record(string name, string description)
        {
            return new NameRecord(name, description);
        }

        private static void AssertFailure(
            XccMixNameCatalogReadResult result,
            XccMixNameCatalogDiagnosticCode expected)
        {
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Diagnostics.Single().Code, Is.EqualTo(expected));
        }

        private sealed class NameRecord
        {
            public NameRecord(string name, string description)
            {
                Name = name;
                Description = description;
            }

            public string Name { get; }

            public string Description { get; }
        }
    }
}
