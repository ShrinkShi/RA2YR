using System;
using System.Linq;
using NUnit.Framework;

namespace RA2YR.Tests.EditMode
{
    public sealed class CoreAssemblyBoundaryTests
    {
        [Test]
        public void CoreAssemblyDoesNotReferenceUnityAssemblies()
        {
            string[] referencedAssemblyNames = typeof(Core.AssemblyMarker).Assembly
                .GetReferencedAssemblies()
                .Select(assemblyName => assemblyName.Name)
                .Where(assemblyName => assemblyName != null)
                .ToArray();

            bool referencesUnity = referencedAssemblyNames.Any(assemblyName =>
                assemblyName.StartsWith("UnityEngine", StringComparison.Ordinal) ||
                assemblyName.StartsWith("UnityEditor", StringComparison.Ordinal));

            Assert.That(
                referencesUnity,
                Is.False,
                "RA2YR.Core must not reference UnityEngine or UnityEditor assemblies.");
        }
    }
}
