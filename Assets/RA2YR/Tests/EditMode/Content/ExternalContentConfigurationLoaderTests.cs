using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using RA2YR.Core.Content;

namespace RA2YR.Tests.EditMode.Content
{
    public sealed class ExternalContentConfigurationLoaderTests
    {
        [Test]
        public void LoadResolvesPathsRelativeToConfigurationDirectory()
        {
            using (var temporary = new TemporaryContentTestDirectory())
            {
                string repositoryRoot = temporary.CreateDirectory("Repository");
                string expectedSourcePath = temporary.CreateDirectory("External");
                string expectedCachePath = temporary.CreateDirectory("Cache");
                string configurationPath = WriteConfiguration(
                    temporary,
                    "<Source id=\"yr1001-baseline\" kind=\"Patched\" path=\"../../External\" priority=\"300\" version=\"YR 1.001 baseline\" enabled=\"true\" />");

                ExternalContentConfigurationLoadResult result =
                    new ExternalContentConfigurationLoader().Load(
                        configurationPath,
                        repositoryRoot);

                Assert.That(result.Diagnostics, Is.Empty);
                Assert.That(result.Configuration.CachePath, Is.EqualTo(expectedCachePath));
                Assert.That(result.Configuration.Sources, Has.Count.EqualTo(1));
                Assert.That(result.Configuration.Sources[0].RootPath, Is.EqualTo(expectedSourcePath));
                Assert.That(result.Configuration.Sources[0].Kind, Is.EqualTo(ContentSourceKind.Patched));
                Assert.That(result.Configuration.Sources[0].Priority, Is.EqualTo(300));
                Assert.That(result.Configuration.Sources[0].Version, Is.EqualTo("YR 1.001 baseline"));
            }
        }

        [Test]
        public void LoadReportsMissingVersionWithoutInventingOne()
        {
            using (var temporary = new TemporaryContentTestDirectory())
            {
                string repositoryRoot = temporary.CreateDirectory("Repository");
                temporary.CreateDirectory("External");
                temporary.CreateDirectory("Cache");
                string configurationPath = WriteConfiguration(
                    temporary,
                    "<Source id=\"source-one\" kind=\"Unpacked\" path=\"../../External\" priority=\"20\" enabled=\"true\" />");

                ExternalContentConfigurationLoadResult result =
                    new ExternalContentConfigurationLoader().Load(
                        configurationPath,
                        repositoryRoot);

                Assert.That(result.Configuration.Sources[0].Version, Is.Empty);
                Assert.That(
                    result.Diagnostics.Any(item =>
                        item.Code == ContentDiagnosticCode.MissingVersion &&
                        item.Severity == ContentDiagnosticSeverity.Warning &&
                        item.SourceId == "source-one" &&
                        item.LineNumber > 0),
                    Is.True);
            }
        }

        [Test]
        public void LoadRejectsDuplicateIdsIgnoringCase()
        {
            using (var temporary = new TemporaryContentTestDirectory())
            {
                string repositoryRoot = temporary.CreateDirectory("Repository");
                temporary.CreateDirectory("External");
                temporary.CreateDirectory("ExternalTwo");
                temporary.CreateDirectory("Cache");
                string configurationPath = WriteConfiguration(
                    temporary,
                    "<Source id=\"Baseline\" kind=\"Patched\" path=\"../../External\" priority=\"20\" version=\"one\" />" +
                    "<Source id=\"baseline\" kind=\"Overlay\" path=\"../../ExternalTwo\" priority=\"30\" version=\"two\" />");

                ContentConfigurationException exception = Assert.Throws<ContentConfigurationException>(
                    () => new ExternalContentConfigurationLoader().Load(
                        configurationPath,
                        repositoryRoot));

                Assert.That(
                    exception.Diagnostics.Any(item =>
                        item.Code == ContentDiagnosticCode.DuplicateSourceId &&
                        item.SourceId == "baseline" &&
                        item.LineNumber > 0),
                    Is.True);
            }
        }

        [Test]
        public void LoadRejectsInvalidPriority()
        {
            using (var temporary = new TemporaryContentTestDirectory())
            {
                string repositoryRoot = temporary.CreateDirectory("Repository");
                temporary.CreateDirectory("External");
                temporary.CreateDirectory("Cache");
                string configurationPath = WriteConfiguration(
                    temporary,
                    "<Source id=\"source-one\" kind=\"Clean\" path=\"../../External\" priority=\"highest\" version=\"one\" />");

                ContentConfigurationException exception = Assert.Throws<ContentConfigurationException>(
                    () => new ExternalContentConfigurationLoader().Load(
                        configurationPath,
                        repositoryRoot));

                Assert.That(
                    exception.Diagnostics.Any(item =>
                        item.Code == ContentDiagnosticCode.InvalidPriority &&
                        item.SourceId == "source-one"),
                    Is.True);
            }
        }

        [TestCase("../Content", "../../Cache")]
        [TestCase("../../External", "../Cache")]
        public void LoadRejectsContentOrCacheInsideRepository(
            string sourcePath,
            string cachePath)
        {
            using (var temporary = new TemporaryContentTestDirectory())
            {
                string repositoryRoot = temporary.CreateDirectory("Repository");
                temporary.CreateDirectory("Repository/Content");
                temporary.CreateDirectory("Repository/Cache");
                temporary.CreateDirectory("External");
                temporary.CreateDirectory("Cache");
                string configurationPath = WriteConfiguration(
                    temporary,
                    "<Source id=\"source-one\" kind=\"Clean\" path=\"" + sourcePath + "\" priority=\"1\" version=\"one\" />",
                    cachePath);

                ContentConfigurationException exception = Assert.Throws<ContentConfigurationException>(
                    () => new ExternalContentConfigurationLoader().Load(
                        configurationPath,
                        repositoryRoot));

                Assert.That(
                    exception.Diagnostics.Any(item =>
                        item.Code == ContentDiagnosticCode.PathInsideRepository),
                    Is.True);
            }
        }

        [Test]
        public void LoadRejectsDtdAndDoesNotResolveExternalEntities()
        {
            using (var temporary = new TemporaryContentTestDirectory())
            {
                string repositoryRoot = temporary.CreateDirectory("Repository");
                temporary.CreateDirectory("External");
                temporary.CreateDirectory("Cache");
                string secretPath = temporary.WriteText("secret.txt", "SYNTHETIC_SECRET_MUST_NOT_BE_READ");
                string configurationPath = temporary.WriteText(
                    "Repository/Config/ExternalContent.xml",
                    "<?xml version=\"1.0\"?>" +
                    "<!DOCTYPE data [<!ENTITY xxe SYSTEM \"" + new Uri(secretPath).AbsoluteUri + "\">]>" +
                    "<ExternalContent schemaVersion=\"1\" cachePath=\"../../Cache\"><Sources>" +
                    "<Source id=\"source-one\" kind=\"Clean\" path=\"../../External\" priority=\"1\" version=\"&xxe;\" />" +
                    "</Sources></ExternalContent>");

                ContentConfigurationException exception = Assert.Throws<ContentConfigurationException>(
                    () => new ExternalContentConfigurationLoader().Load(
                        configurationPath,
                        repositoryRoot));

                Assert.That(
                    exception.Diagnostics.Any(item =>
                        item.Code == ContentDiagnosticCode.ConfigurationXmlRejected),
                    Is.True);
                Assert.That(exception.Message, Does.Not.Contain("SYNTHETIC_SECRET_MUST_NOT_BE_READ"));
            }
        }

        [Test]
        public void LoadRejectsUnknownSchemaOneAttributeIncludingEnabledTypos()
        {
            using (var temporary = new TemporaryContentTestDirectory())
            {
                string repositoryRoot = temporary.CreateDirectory("Repository");
                temporary.CreateDirectory("External");
                temporary.CreateDirectory("Cache");
                string configurationPath = WriteConfiguration(
                    temporary,
                    "<Source id=\"source-one\" kind=\"Clean\" path=\"../../External\" priority=\"1\" version=\"one\" enable=\"true\" />");

                ContentConfigurationException exception = Assert.Throws<ContentConfigurationException>(
                    () => new ExternalContentConfigurationLoader().Load(
                        configurationPath,
                        repositoryRoot));

                Assert.That(exception.Diagnostics.Any(item =>
                    item.Code == ContentDiagnosticCode.UnknownConfigurationAttribute &&
                    item.LineNumber > 0), Is.True);
            }
        }

        [Test]
        public void LoadRejectsCacheAndSourceContainmentInEitherDirection()
        {
            using (var temporary = new TemporaryContentTestDirectory())
            {
                string repositoryRoot = temporary.CreateDirectory("Repository");
                temporary.CreateDirectory("External/Cache");
                string configurationPath = WriteConfiguration(
                    temporary,
                    "<Source id=\"source-one\" kind=\"Clean\" path=\"../../External\" priority=\"1\" version=\"one\" />",
                    "../../External/Cache");

                ContentConfigurationException exception = Assert.Throws<ContentConfigurationException>(
                    () => new ExternalContentConfigurationLoader().Load(
                        configurationPath,
                        repositoryRoot));

                Assert.That(exception.Diagnostics.Any(item =>
                    item.Code == ContentDiagnosticCode.ExternalPathsOverlap), Is.True);
            }
        }

        [Test]
        public void LoadRejectsExistingCacheFile()
        {
            using (var temporary = new TemporaryContentTestDirectory())
            {
                string repositoryRoot = temporary.CreateDirectory("Repository");
                temporary.CreateDirectory("External");
                temporary.WriteText("CacheFile", "synthetic");
                string configurationPath = WriteConfiguration(
                    temporary,
                    "<Source id=\"source-one\" kind=\"Clean\" path=\"../../External\" priority=\"1\" version=\"one\" />",
                    "../../CacheFile");

                ContentConfigurationException exception = Assert.Throws<ContentConfigurationException>(
                    () => new ExternalContentConfigurationLoader().Load(
                        configurationPath,
                        repositoryRoot));

                Assert.That(exception.Diagnostics.Any(item =>
                    item.Code == ContentDiagnosticCode.CachePathNotDirectory), Is.True);
            }
        }

        [Test]
        public void LoadRejectsMissingOrFileValuedRepositoryRootBeforeReadingConfiguration()
        {
            using (var temporary = new TemporaryContentTestDirectory())
            {
                temporary.CreateDirectory("Repository");
                temporary.CreateDirectory("External");
                temporary.CreateDirectory("Cache");
                string configurationPath = WriteConfiguration(
                    temporary,
                    "<Source id=\"source-one\" kind=\"Clean\" path=\"../../External\" priority=\"1\" version=\"one\" />");
                string missingRepositoryRoot = temporary.GetPath("MissingRepository");
                string fileValuedRepositoryRoot = temporary.WriteText(
                    "RepositoryRootFile",
                    "synthetic");

                foreach (string invalidRepositoryRoot in new[]
                         {
                             missingRepositoryRoot,
                             fileValuedRepositoryRoot
                         })
                {
                    ContentConfigurationException exception =
                        Assert.Throws<ContentConfigurationException>(
                            () => new ExternalContentConfigurationLoader().Load(
                                configurationPath,
                                invalidRepositoryRoot));

                    Assert.That(exception.Diagnostics.Any(item =>
                        item.Code == ContentDiagnosticCode.RepositoryRootNotDirectory &&
                        item.Path == invalidRepositoryRoot), Is.True);
                }
            }
        }

        [Test]
        public void LoadRejectsConfigurationWithoutAnEnabledSource()
        {
            using (var temporary = new TemporaryContentTestDirectory())
            {
                string repositoryRoot = temporary.CreateDirectory("Repository");
                temporary.CreateDirectory("External");
                temporary.CreateDirectory("Cache");
                string configurationPath = WriteConfiguration(
                    temporary,
                    "<Source id=\"source-one\" kind=\"Clean\" path=\"../../External\" priority=\"1\" version=\"one\" enabled=\"false\" />");

                ContentConfigurationException exception = Assert.Throws<ContentConfigurationException>(
                    () => new ExternalContentConfigurationLoader().Load(
                        configurationPath,
                        repositoryRoot));

                Assert.That(exception.Diagnostics.Any(item =>
                    item.Code == ContentDiagnosticCode.NoEnabledSource), Is.True);
            }
        }

        private static string WriteConfiguration(
            TemporaryContentTestDirectory temporary,
            string sources,
            string cachePath = "../../Cache")
        {
            return temporary.WriteText(
                "Repository/Config/ExternalContent.xml",
                "<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
                "<ExternalContent schemaVersion=\"1\" cachePath=\"" + cachePath + "\">" +
                "<Sources>" + sources + "</Sources>" +
                "</ExternalContent>");
        }
    }
}
