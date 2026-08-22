using System.Collections;
using System.Linq;
using Cutrium.Gameplay.Barriers;
using Cutrium.Gameplay.Geometry;
using Cutrium.Gameplay.Session;
using Cutrium.Gameplay.Threats;
using Cutrium.Presentation.HUD;
using Cutrium.Unity.Bootstrap;
using Cutrium.Unity.Input;
using Cutrium.Unity.Simulation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Cutrium.PlayModeTests
{
    public sealed class Milestone6ThreatsAndPowersPlayModeTests
    {
        private const string ScenePath =
            "Assets/Cutrium/Scenes/VerticalSlice.unity";

        private GameObject _root;
        private SceneCompositionRoot _composition;
        private FirstPlayableController _controller;
        private BarrierGestureAdapter _gesture;
        private PowerHudPresenter _powerHud;
        private Mouse _mouse;
        private InputSettings.BackgroundBehavior _originalBackgroundBehavior;
        private InputSettings.EditorInputBehaviorInPlayMode _originalEditorBehavior;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _originalBackgroundBehavior = InputSystem.settings.backgroundBehavior;
            _originalEditorBehavior =
                InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.backgroundBehavior =
                InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode =
                InputSettings.EditorInputBehaviorInPlayMode
                    .AllDeviceInputAlwaysGoesToGameView;
            yield return SceneManager.LoadSceneAsync(
                ScenePath,
                LoadSceneMode.Single);
            yield return null;
            _root = SceneManager.GetActiveScene().GetRootGameObjects()
                .Single(candidate => candidate.name == "VerticalSliceRoot");
            _composition = _root.transform.Find("SceneCompositionRoot")
                .GetComponent<SceneCompositionRoot>();
            _controller = _root
                .GetComponentInChildren<FirstPlayableController>(true);
            _gesture = _root.GetComponentInChildren<BarrierGestureAdapter>(true);
            _powerHud = _root.GetComponentInChildren<PowerHudPresenter>(true);
            _root.GetComponentInChildren<
                    Cutrium.Presentation.Frontend.FrontEndPresenter>(true)
                ?.SkipForTesting();
            _root.GetComponentInChildren<PreLevelIntroPresenter>(true)
                ?.SkipForTesting();
            Canvas.ForceUpdateCanvases();
            _composition.BoardCameraFitter.RefreshNow();
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (_mouse != null && _mouse.added)
            {
                InputSystem.RemoveDevice(_mouse);
            }

            _mouse = null;
            InputSystem.settings.backgroundBehavior = _originalBackgroundBehavior;
            InputSystem.settings.editorInputBehaviorInPlayMode =
                _originalEditorBehavior;
            yield return null;
        }

        [Test]
        public void Scene_HasOnePowerHudPresenterWithCompleteReferences()
        {
            Assert.That(_root.GetComponentsInChildren<PowerHudPresenter>(true),
                Has.Length.EqualTo(1));
            Assert.That(_powerHud.Controller, Is.SameAs(_controller));
            Assert.That(_powerHud.FreezePulseRoot, Is.Not.Null);
            Assert.That(_powerHud.FreezePulseButton, Is.Not.Null);
            Assert.That(_powerHud.FreezePulseChargesText, Is.Not.Null);
            Assert.That(_powerHud.InstantBarrierRoot, Is.Not.Null);
            Assert.That(_powerHud.InstantBarrierButton, Is.Not.Null);
            Assert.That(_powerHud.InstantBarrierChargesText, Is.Not.Null);
        }

        [Test]
        public void Scene_KeepsTheRegressionTestedMilestone3CatalogByDefault()
        {
            // Milestone 6 setup intentionally does not overwrite the saved
            // scene's level catalog: the five Hunter/Pulse/power identity
            // levels use safe-cut heuristics (reactive steering, changing
            // speed) that the existing Milestone 2/3 flow tests were never
            // written to solve for. The identity levels are loaded through
            // 'Cutrium/Setup/Load Milestone 6 Identity Levels (Manual
            // Playtest)' instead, verified independently by
            // ThreatBehaviorAndPowerTests and the isolated-controller test
            // below.
            Assert.That(_controller.LevelDefinitions.Select(
                    definition => definition.StableId),
                Is.EqualTo(new[]
                {
                    "learn-the-cut",
                    "timing-and-failure",
                    "confident-capture",
                }));
        }

        [Test]
        public void PowerButtons_AreVisibleButNonInteractableInTheDefaultGameplayHud()
        {
            // The landmark presentation pass reparents the Freeze Pulse/
            // Instant Barrier buttons into BottomHUD's SkillRow, where they
            // are part of the default visible HUD (see
            // GameplayDefaultHud_ShowsThreeSkillSlotsInBottomHudSkillRow in
            // LandmarkRevealPlayModeTests). The default (Milestone 3) level
            // catalog grants zero charges, so both buttons stay visible but
            // non-interactable until a level configures a power.
            Assert.That(
                _powerHud.FreezePulseButton.gameObject.activeInHierarchy,
                Is.True);
            Assert.That(_powerHud.FreezePulseButton.interactable, Is.False);
            Assert.That(
                _powerHud.InstantBarrierButton.gameObject.activeInHierarchy,
                Is.True);
            Assert.That(_powerHud.InstantBarrierButton.interactable, Is.False);
        }

        [Test]
        public void FreezePulseAndInstantBarrier_WorkThroughControllerIndependentOfPresentation()
        {
            GameObject simulationObject = new GameObject(
                "PowerTestSimulation");
            simulationObject.SetActive(false);
            FirstPlayableController controller =
                simulationObject.AddComponent<FirstPlayableController>();
            var power = new CoreFunPowerDefinition(
                1,
                2f,
                0.1f,
                1,
                600f);
            var level = new CoreFunLevelDefinition(
                "power-isolation-test",
                1,
                new[]
                {
                    new CoreFunThreatDefinition(
                        new Vector2(5f, 8f),
                        new Vector2(1f, 0f),
                        2f,
                        0.35f,
                        8),
                },
                0.9f,
                2.4f,
                0.08f,
                3f,
                16,
                8,
                "Isolated power test.",
                30f,
                "POWER ISOLATION",
                power);
            controller.ConfigureLevelsForSetup(new[] { level });
            simulationObject.SetActive(true);
            try
            {
                controller.AdvanceSimulation(0f);

                Assert.That(controller.TryActivateFreezePulse(), Is.True);
                Assert.That(controller.FreezePulseChargesRemaining,
                    Is.EqualTo(0));
                controller.AdvanceSimulation(1f);
                Assert.That(
                    controller.Session.Threat.Speed,
                    Is.LessThan(1f));

                Assert.That(controller.TryArmInstantBarrier(), Is.True);
                Assert.That(controller.InstantBarrierArmed, Is.True);
                BarrierStartResult accepted = controller.SubmitBarrierIntent(
                    new BarrierIntent(
                        new LogicalPoint(8f, 8f),
                        BarrierOrientation.Vertical));
                Assert.That(accepted.Accepted, Is.True);
                Assert.That(controller.InstantBarrierArmed, Is.False);
                Assert.That(controller.InstantBarrierChargesRemaining,
                    Is.EqualTo(0));

                controller.AdvanceSimulation(
                    FirstPlayableController.SimulationStep);

                Assert.That(controller.Session.LockedBarrierCount,
                    Is.EqualTo(1));
            }
            finally
            {
                Object.DestroyImmediate(simulationObject);
            }
        }

        private static Vector2 GetScreenCenter(RectTransform rectTransform)
        {
            var corners = new Vector3[4];
            rectTransform.GetWorldCorners(corners);
            return RectTransformUtility.WorldToScreenPoint(
                null,
                (corners[0] + corners[2]) * 0.5f);
        }
    }
}
