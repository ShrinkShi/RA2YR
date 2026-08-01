using System.Collections;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace RA2YR.Tests.PlayMode
{
    public sealed class UnityIntegrationAssemblySmokeTests
    {
        [UnityTest]
        public IEnumerator UnityIntegrationAssemblyLoadsInPlayMode()
        {
            yield return null;

            System.Reflection.Assembly assembly =
                typeof(UnityIntegration.AssemblyMarker).Assembly;

            Assert.That(assembly.GetName().Name, Is.EqualTo("RA2YR.UnityIntegration"));
            Assert.That(
                assembly.GetType("RA2YR.UnityIntegration.AssemblyMarker"),
                Is.Not.Null);
        }
    }
}
