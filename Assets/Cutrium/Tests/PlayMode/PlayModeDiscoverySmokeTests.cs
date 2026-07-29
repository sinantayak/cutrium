using System.Collections;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace Cutrium.PlayModeTests
{
    public sealed class PlayModeDiscoverySmokeTests
    {
        [UnityTest]
        public IEnumerator PlayModeTestAssembly_IsDiscoveredAndRuns()
        {
            yield return null;

            Assert.Pass("Cutrium.PlayModeTests was discovered and executed.");
        }
    }
}
