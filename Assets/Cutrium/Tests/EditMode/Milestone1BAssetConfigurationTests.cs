using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine.InputSystem;

namespace Cutrium.Gameplay.EditModeTests
{
    public sealed class Milestone1BAssetConfigurationTests
    {
        private const string InputAssetPath =
            "Assets/Cutrium/Input/CutriumInput.inputactions";
        private const string VerticalSliceScenePath =
            "Assets/Cutrium/Scenes/VerticalSlice.unity";
        private const string SampleScenePath =
            "Assets/Scenes/SampleScene.unity";

        [Test]
        public void DedicatedGameplayActions_HaveRequiredTypesAndBindings()
        {
            InputActionAsset asset =
                AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputAssetPath);

            Assert.That(asset, Is.Not.Null);
            InputActionMap gameplay = asset.FindActionMap("Gameplay", true);
            InputAction point = gameplay.FindAction("Point", true);
            InputAction press = gameplay.FindAction("Press", true);
            InputAction cancel = gameplay.FindAction("Cancel", true);

            Assert.That(point.type, Is.EqualTo(InputActionType.PassThrough));
            Assert.That(point.expectedControlType, Is.EqualTo("Vector2"));
            Assert.That(point.bindings.Select(binding => binding.path),
                Does.Contain("<Pointer>/position"));

            Assert.That(press.type, Is.EqualTo(InputActionType.Button));
            Assert.That(press.bindings.Select(binding => binding.path),
                Does.Contain("<Pointer>/press"));

            Assert.That(cancel.type, Is.EqualTo(InputActionType.Button));
            string[] cancelBindings =
                cancel.bindings.Select(binding => binding.path).ToArray();
            Assert.That(cancelBindings, Does.Contain("<Keyboard>/escape"));
            Assert.That(cancelBindings, Does.Contain("<Mouse>/rightButton"));
        }

        [Test]
        public void DedicatedUiActions_AreSuitableForInputSystemUiModule()
        {
            InputActionAsset asset =
                AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputAssetPath);

            Assert.That(asset, Is.Not.Null);
            InputActionMap ui = asset.FindActionMap("UI", true);

            Assert.That(ui.FindAction("Point", true).type,
                Is.EqualTo(InputActionType.PassThrough));
            Assert.That(ui.FindAction("LeftClick", true).type,
                Is.EqualTo(InputActionType.PassThrough));
            Assert.That(ui.FindAction("Navigate", true).type,
                Is.EqualTo(InputActionType.PassThrough));
            Assert.That(ui.FindAction("Submit", true).type,
                Is.EqualTo(InputActionType.Button));
            Assert.That(ui.FindAction("Cancel", true).type,
                Is.EqualTo(InputActionType.Button));
        }

        [Test]
        public void VerticalSliceScene_IsEnabledAndSampleSceneIsDisabled()
        {
            EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;

            Assert.That(scenes.Length, Is.EqualTo(2));
            Assert.That(scenes[0].path, Is.EqualTo(VerticalSliceScenePath));
            Assert.That(scenes[0].enabled, Is.True);
            Assert.That(scenes[1].path, Is.EqualTo(SampleScenePath));
            Assert.That(scenes[1].enabled, Is.False);
        }

        [Test]
        public void VerticalSliceSceneAsset_Exists()
        {
            SceneAsset scene =
                AssetDatabase.LoadAssetAtPath<SceneAsset>(VerticalSliceScenePath);

            Assert.That(scene, Is.Not.Null);
        }
    }
}
