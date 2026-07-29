using NUnit.Framework;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace Cutrium.Gameplay.EditModeTests
{
    public sealed class ProjectConfigurationTests
    {
        [Test]
        public void AcceptedUnityAndIdentitySettings_AreEffective()
        {
            Assert.That(Application.unityVersion, Is.EqualTo("6000.3.21f1"));
            Assert.That(PlayerSettings.companyName, Is.EqualTo("Tayack Games"));
            Assert.That(PlayerSettings.productName, Is.EqualTo("Cutrium"));
            Assert.That(EditorSettings.projectGenerationRootNamespace, Is.EqualTo("Cutrium"));
            Assert.That(
                PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android),
                Is.EqualTo("com.tayackgames.cutrium"));
            Assert.That(
                PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.iOS),
                Is.EqualTo("com.tayackgames.cutrium"));
        }

        [Test]
        public void AcceptedOrientation_IsFixedUprightPortrait()
        {
            Assert.That(PlayerSettings.defaultInterfaceOrientation, Is.EqualTo(UIOrientation.Portrait));
        }
    }
}
