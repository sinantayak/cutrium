using System.Collections;
using System.Linq;
using Cutrium.Gameplay.Geometry;
using Cutrium.Presentation.HUD;
using Cutrium.Presentation.Localization;
using Cutrium.Unity.Input;
using Cutrium.Unity.Simulation;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Cutrium.PlayModeTests
{
    public sealed class GuidedTrainingPlayModeTests
    {
        private const string ScenePath =
            "Assets/Cutrium/Scenes/VerticalSlice.unity";
        private const string TrainedLevelId = "test-guided-training";
        private const string UntrainedLevelId = "no-training-level";
        private static readonly LogicalPoint Cut1Origin =
            new LogicalPoint(2f, 2f);
        private static readonly LogicalPoint Cut2Origin =
            new LogicalPoint(2f, 6f);

        [UnityTest]
        public IEnumerator FullSequence_TeachesHudThenBothCutsThenFinishesFreely()
        {
            TutorialRig rig = TutorialRig.Create(includeLocalization: false);
            yield return null;
            rig.Presenter.RefreshNow(0.2f);

            // Step 0: Info — barrier speed HUD. Frozen, tap-gated (not a
            // timer): a large dt alone must not advance it.
            Assert.That(
                rig.Presenter.Stage,
                Is.EqualTo(GuidedTrainingStage.Prompting));
            Assert.That(rig.Presenter.StepIndex, Is.EqualTo(0));
            Assert.That(
                rig.Instruction.text,
                Is.EqualTo("THIS IS YOUR CUT SPEED"),
                "The instruction text itself must not carry the tap " +
                "prompt — it lives in its own bottom-center element.");
            Assert.That(rig.TapToContinue.gameObject.activeSelf, Is.True);
            Assert.That(rig.TapToContinue.text, Is.EqualTo("TAP TO CONTINUE"));
            Assert.That(rig.FocusHighlight.Target, Is.EqualTo(rig.SpeedFocus));
            Assert.That(rig.Controller.SimulationHeld, Is.True);

            rig.Presenter.RefreshNow(0.05f);
            Assert.That(
                rig.SpeedFocus.localScale,
                Is.Not.EqualTo(Vector3.one),
                "The highlighted HUD target itself must pulse in scale.");

            rig.Presenter.RefreshNow(5f);
            Assert.That(
                rig.Presenter.StepIndex,
                Is.EqualTo(0),
                "An Info step must not auto-advance on a timer.");

            Tap(rig, new LogicalPoint(5f, 8f));
            rig.Presenter.RefreshNow(0f);

            // The previous target's scale must be restored once it is no
            // longer highlighted.
            Assert.That(rig.SpeedFocus.localScale, Is.EqualTo(Vector3.one));

            // Step 1: Info — lives HUD.
            Assert.That(rig.Presenter.StepIndex, Is.EqualTo(1));
            Assert.That(
                rig.Instruction.text,
                Is.EqualTo("THESE ARE YOUR LIVES"));
            Assert.That(rig.TapToContinue.gameObject.activeSelf, Is.True);
            Assert.That(rig.FocusHighlight.Target, Is.EqualTo(rig.LivesFocus));

            Tap(rig, new LogicalPoint(1f, 1f));
            rig.Presenter.RefreshNow(0f);

            // Step 2: Observe — unfrozen, no highlight, input fully
            // suppressed (any touch during this beat must be ignored).
            Assert.That(rig.Presenter.StepIndex, Is.EqualTo(2));
            Assert.That(rig.Instruction.text, Is.EqualTo("WATCH THE THREAT"));
            Assert.That(rig.Controller.SimulationHeld, Is.False);
            Assert.That(rig.FocusHighlight.IsVisible, Is.False);
            Assert.That(rig.TapToContinue.gameObject.activeSelf, Is.False);

            PerformSwipe(
                rig,
                new LogicalPoint(2f, 2f),
                new LogicalPoint(3.2f, 2f));
            Assert.That(
                rig.Gesture.IsTracking,
                Is.False,
                "A touch during Observe must not even start tracking.");
            Assert.That(rig.Gesture.CommittedIntentCount, Is.Zero);

            rig.Presenter.RefreshNow(1.7f);

            // Step 3: Action — horizontal cut at a fixed board position.
            Assert.That(rig.Presenter.StepIndex, Is.EqualTo(3));
            Assert.That(
                rig.Instruction.text,
                Is.EqualTo("SWIPE LEFT OR RIGHT"));
            Assert.That(rig.Controller.SimulationHeld, Is.True);
            Assert.That(rig.TapToContinue.gameObject.activeSelf, Is.False);

            // A swipe that starts far from the taught spot must be ignored
            // entirely, not merely cancelled.
            PerformSwipe(
                rig,
                new LogicalPoint(8f, 2f),
                new LogicalPoint(9.2f, 2f));
            Assert.That(rig.Gesture.CommittedIntentCount, Is.Zero);
            Assert.That(rig.Presenter.Stage, Is.EqualTo(GuidedTrainingStage.Prompting));

            // A swipe that starts within tolerance of the taught spot (but
            // not exactly on it) is accepted and its origin is snapped to
            // the exact fixed point.
            PerformSwipe(
                rig,
                new LogicalPoint(2.4f, 2.3f),
                new LogicalPoint(3.6f, 2.3f));
            Assert.That(rig.Controller.LastBarrierStartResult.Accepted, Is.True);
            rig.Presenter.RefreshNow(0f);
            Assert.That(
                rig.Presenter.Stage,
                Is.EqualTo(GuidedTrainingStage.ResolvingAction));
            Assert.That(
                AdvanceUntilStage(
                    rig,
                    GuidedTrainingStage.SuccessFeedback,
                    600),
                Is.True,
                "The horizontal barrier never locked.");
            Assert.That(rig.FocusHighlight.IsVisible, Is.True);

            rig.Presenter.RefreshNow(0.4f);

            // Step 4: Action — vertical cut, also at a fixed position.
            Assert.That(rig.Presenter.StepIndex, Is.EqualTo(4));
            Assert.That(
                rig.Instruction.text,
                Is.EqualTo("SWIPE UP OR DOWN"));
            Assert.That(rig.FocusHighlight.IsVisible, Is.False);

            PerformSwipe(
                rig,
                Cut2Origin,
                new LogicalPoint(2f, 7.2f));
            Assert.That(rig.Controller.LastBarrierStartResult.Accepted, Is.True);
            rig.Presenter.RefreshNow(0f);
            Assert.That(
                AdvanceUntilStage(
                    rig,
                    GuidedTrainingStage.SuccessFeedback,
                    600),
                Is.True,
                "The vertical barrier never locked.");

            rig.Presenter.RefreshNow(0.4f);

            // Step 5: Action — free finishing cut, unfrozen, any orientation
            // or origin.
            Assert.That(rig.Presenter.StepIndex, Is.EqualTo(5));
            Assert.That(
                rig.Instruction.text,
                Is.EqualTo("MAKE THE FINAL CUT"));
            Assert.That(rig.Controller.SimulationHeld, Is.False);
            Assert.That(rig.FocusHighlight.IsVisible, Is.True);

            // A modest cut that does not yet reach the (retuned, lower)
            // synthetic target must not complete training or re-freeze.
            PerformSwipe(
                rig,
                new LogicalPoint(3f, 10f),
                new LogicalPoint(3f, 11.5f));
            Assert.That(rig.Controller.LastBarrierStartResult.Accepted, Is.True);
            rig.Presenter.RefreshNow(0f);
            Assert.That(
                AdvanceUntilStage(
                    rig,
                    GuidedTrainingStage.Prompting,
                    600),
                Is.True,
                "The presenter never returned to prompting after a " +
                "non-finishing free cut.");
            Assert.That(rig.Presenter.StepIndex, Is.EqualTo(5));
            Assert.That(rig.Controller.SimulationHeld, Is.False);
            Assert.That(rig.Presenter.IsComplete, Is.False);

            // A larger cut that does reach the target finishes training
            // immediately (no success beat, so it does not fight the
            // game's own completion UI).
            PerformSwipe(
                rig,
                new LogicalPoint(7f, 10f),
                new LogicalPoint(7f, 11.5f));
            Assert.That(rig.Controller.LastBarrierStartResult.Accepted, Is.True);
            rig.Presenter.RefreshNow(0f);
            Assert.That(
                AdvanceUntilComplete(rig, 600),
                Is.True,
                "Training never completed after the level-finishing cut.");
            Assert.That(rig.Group.alpha, Is.Zero);
            Assert.That(
                rig.Controller.HasSimulationHold(
                    SimulationHoldReason.GuidedTraining),
                Is.False);

            rig.Controller.RetryLevel();
            rig.Presenter.RefreshNow(0f);
            Assert.That(
                rig.Presenter.Stage,
                Is.EqualTo(GuidedTrainingStage.Prompting));
            Assert.That(rig.Presenter.StepIndex, Is.EqualTo(0));

            rig.Dispose();
            yield return null;
        }

        [UnityTest]
        public IEnumerator TurkishLanguage_RefreshesTrainingCopyAndTapPrompt()
        {
            TutorialRig rig = TutorialRig.Create(includeLocalization: true);
            yield return null;
            rig.Localization.SetLanguage(
                SupportedLanguage.Turkish,
                savePreference: false);
            rig.Presenter.RefreshNow(0f);

            Assert.That(
                rig.Instruction.text,
                Is.EqualTo("BU SENİN KESİM HIZIN"));
            Assert.That(
                rig.TapToContinue.text,
                Is.EqualTo("DEVAM ETMEK İÇİN DOKUN"));

            rig.Dispose();
            yield return null;
        }

        [UnityTest]
        public IEnumerator UnconfiguredLevel_NeverAcquiresTrainingHold()
        {
            TutorialRig rig = TutorialRig.Create(includeLocalization: false);
            yield return null;
            rig.Presenter.RefreshNow(0.2f);
            Assert.That(
                rig.Presenter.Stage,
                Is.EqualTo(GuidedTrainingStage.Prompting));

            // Level 2 is beyond reached progress (CurrentLevelIndex starts
            // at 0), so TryStartLevel now correctly refuses it; jump there
            // the same way real unlocked navigation would after progress
            // advances, matching FrontEndPlayModeTests' pattern.
            Assert.That(rig.Controller.TryJumpToLevelForDevelopment(2), Is.True);
            rig.Presenter.RefreshNow(0.2f);

            Assert.That(
                rig.Presenter.Stage,
                Is.EqualTo(GuidedTrainingStage.Inactive));
            Assert.That(rig.Group.alpha, Is.Zero);
            Assert.That(
                rig.Controller.HasSimulationHold(
                    SimulationHoldReason.GuidedTraining),
                Is.False);

            rig.Dispose();
            yield return null;
        }

        [UnityTest]
        public IEnumerator Scene_WiresExactlyOneGuidedTrainingPresenter()
        {
            yield return SceneManager.LoadSceneAsync(
                ScenePath,
                LoadSceneMode.Single);
            yield return null;

            GameObject root = SceneManager.GetActiveScene()
                .GetRootGameObjects()
                .Single(candidate => candidate.name == "VerticalSliceRoot");
            GuidedTrainingPresenter[] presenters = root
                .GetComponentsInChildren<GuidedTrainingPresenter>(true);
            Assert.That(presenters, Has.Length.EqualTo(1));

            GuidedTrainingPresenter presenter = presenters[0];
            Assert.That(presenter.Controller, Is.Not.Null);
            Assert.That(presenter.Gesture, Is.Not.Null);
            Assert.That(presenter.PreLevelIntro, Is.Not.Null);
            Assert.That(presenter.Localization, Is.Not.Null);
            Assert.That(presenter.SandProgress, Is.Not.Null);
            Assert.That(presenter.CanvasGroup, Is.Not.Null);
            Assert.That(presenter.HandVisual, Is.Not.Null);
            Assert.That(presenter.InstructionText, Is.Not.Null);
            Assert.That(presenter.TapToContinueText, Is.Not.Null);
            Assert.That(presenter.FocusHighlight, Is.Not.Null);
            Assert.That(presenter.ProgressFocusTarget, Is.Not.Null);
            Assert.That(presenter.SpeedHudFocusTarget, Is.Not.Null);
            Assert.That(presenter.LivesHudFocusTarget, Is.Not.Null);
            Assert.That(presenter.CanvasGroup.blocksRaycasts, Is.False);
            Assert.That(presenter.Definitions, Has.Length.EqualTo(1));
            Assert.That(
                presenter.Definitions[0].StableLevelId,
                Is.EqualTo("learn-the-cut"));
            Assert.That(
                presenter.Definitions[0].Steps.Count,
                Is.EqualTo(6));
            Assert.That(
                presenter.Definitions[0].Steps[3].FixedOrigin,
                Is.Not.Null,
                "The horizontal cut must be taught from a fixed origin.");
            Assert.That(
                presenter.Definitions[0].Steps[4].FixedOrigin,
                Is.Not.Null,
                "The vertical cut must be taught from a fixed origin.");
        }

        private static bool AdvanceUntilStage(
            TutorialRig rig,
            GuidedTrainingStage target,
            int maxTicks)
        {
            for (int tick = 0; tick < maxTicks; tick++)
            {
                rig.Controller.AdvanceSimulation(1f / 60f);
                if (rig.Presenter.Stage == target)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool AdvanceUntilComplete(TutorialRig rig, int maxTicks)
        {
            for (int tick = 0; tick < maxTicks; tick++)
            {
                rig.Controller.AdvanceSimulation(1f / 60f);
                if (rig.Presenter.IsComplete)
                {
                    return true;
                }
            }

            return false;
        }

        private static void PerformSwipe(
            TutorialRig rig,
            LogicalPoint origin,
            LogicalPoint moved)
        {
            rig.Gesture.ProcessSample(
                Sample(PointerSamplePhase.Started, origin));
            rig.Gesture.ProcessSample(
                Sample(PointerSamplePhase.Moved, moved));
            rig.Gesture.ProcessSample(
                Sample(PointerSamplePhase.Released, moved));
        }

        private static void Tap(TutorialRig rig, LogicalPoint point)
        {
            rig.Gesture.ProcessSample(
                Sample(PointerSamplePhase.Started, point));
            rig.Gesture.ProcessSample(
                Sample(PointerSamplePhase.Released, point));
        }

        private static PointerSample Sample(
            PointerSamplePhase phase,
            LogicalPoint point) =>
            new PointerSample(
                phase,
                Vector2.zero,
                1,
                false,
                true,
                true,
                point);

        private sealed class TutorialRig
        {
            private GameObject _root;
            private LocalizationTable _table;

            public FirstPlayableController Controller { get; private set; }
            public BarrierGestureAdapter Gesture { get; private set; }
            public GuidedTrainingPresenter Presenter { get; private set; }
            public TrainingFocusHighlightPresenter FocusHighlight
            {
                get;
                private set;
            }
            public LocalizationService Localization { get; private set; }
            public CanvasGroup Group { get; private set; }
            public TMP_Text Instruction { get; private set; }
            public TMP_Text TapToContinue { get; private set; }
            public RectTransform Hand { get; private set; }
            public RectTransform SpeedFocus { get; private set; }
            public RectTransform LivesFocus { get; private set; }

            public static TutorialRig Create(bool includeLocalization)
            {
                var rig = new TutorialRig();
                rig._root = new GameObject(
                    "GuidedTrainingTestRoot",
                    typeof(RectTransform));
                rig._root.SetActive(false);

                rig.Gesture = rig._root.AddComponent<BarrierGestureAdapter>();
                rig.Gesture.Configure(null, 0.35f, 0.1f);
                rig.Controller = rig._root.AddComponent<
                    FirstPlayableController>();
                rig.Controller.ConfigureLevelsForSetup(CreateTestLevels());
                rig.Controller.ConfigureBarrierForSetup(
                    rig.Gesture,
                    40f,
                    0.08f,
                    0.3f,
                    16);

                if (includeLocalization)
                {
                    rig._table = ScriptableObject.CreateInstance<
                        LocalizationTable>();
                    rig._table.ConfigureForSetup(new[]
                    {
                        new LocalizationEntry(
                            "THIS IS YOUR CUT SPEED",
                            "BU SENİN KESİM HIZIN"),
                    });
                    rig.Localization = rig._root.AddComponent<
                        LocalizationService>();
                    rig.Localization.ConfigureForSetup(
                        rig._table,
                        persistPreference: false);
                }

                var tutorial = new GameObject(
                    "Tutorial",
                    typeof(RectTransform),
                    typeof(CanvasGroup));
                tutorial.transform.SetParent(rig._root.transform, false);
                rig.Group = tutorial.GetComponent<CanvasGroup>();
                var tutorialRect = (RectTransform)tutorial.transform;
                tutorialRect.sizeDelta = new Vector2(1000f, 1600f);

                var hand = new GameObject(
                    "Hand",
                    typeof(RectTransform),
                    typeof(Image));
                hand.transform.SetParent(tutorial.transform, false);
                rig.Hand = (RectTransform)hand.transform;

                var instruction = new GameObject(
                    "Instruction",
                    typeof(RectTransform),
                    typeof(TextMeshProUGUI));
                instruction.transform.SetParent(tutorial.transform, false);
                rig.Instruction = instruction.GetComponent<TextMeshProUGUI>();

                var tapToContinue = new GameObject(
                    "TapToContinue",
                    typeof(RectTransform),
                    typeof(TextMeshProUGUI));
                tapToContinue.transform.SetParent(tutorial.transform, false);
                rig.TapToContinue =
                    tapToContinue.GetComponent<TextMeshProUGUI>();

                var focusHolder = new GameObject(
                    "FocusHighlight",
                    typeof(RectTransform));
                focusHolder.transform.SetParent(rig._root.transform, false);
                rig.FocusHighlight = focusHolder.AddComponent<
                    TrainingFocusHighlightPresenter>();
                rig.FocusHighlight.ConfigureForSetup();

                var sandProgress = rig._root.AddComponent<
                    SandProgressPresenter>();

                var progressTarget = new GameObject(
                    "ProgressTarget",
                    typeof(RectTransform));
                progressTarget.transform.SetParent(rig._root.transform, false);

                var speedTarget = new GameObject(
                    "SpeedFocusTarget",
                    typeof(RectTransform));
                speedTarget.transform.SetParent(rig._root.transform, false);
                rig.SpeedFocus = (RectTransform)speedTarget.transform;

                var livesTarget = new GameObject(
                    "LivesFocusTarget",
                    typeof(RectTransform));
                livesTarget.transform.SetParent(rig._root.transform, false);
                rig.LivesFocus = (RectTransform)livesTarget.transform;

                GuidedTrainingDefinition definition = CreateTestDefinition();

                rig.Presenter = tutorial.AddComponent<
                    GuidedTrainingPresenter>();
                rig.Presenter.ConfigureForSetup(
                    new[] { definition },
                    rig.Controller,
                    rig.Gesture,
                    null,
                    rig.Localization,
                    sandProgress,
                    rig.Group,
                    rig.Hand,
                    rig.Instruction,
                    rig.TapToContinue,
                    rig.FocusHighlight,
                    (RectTransform)progressTarget.transform,
                    speedHudFocusTarget: rig.SpeedFocus,
                    livesHudFocusTarget: rig.LivesFocus,
                    fadeSeconds: 0f);

                rig._root.SetActive(true);
                rig.Controller.TryStartLevel(1);
                rig.Presenter.RefreshNow(0f);
                return rig;
            }

            public void Dispose()
            {
                UnityEngine.Object.Destroy(_root);
                if (_table != null)
                {
                    UnityEngine.Object.Destroy(_table);
                }

                // TestModeDetector.IsRunningTests only recognizes the
                // documented `-batchmode -runTests` CLI invocation, so a
                // test driven live (e.g. via an Editor MCP tool) writes
                // TryJumpToLevelForDevelopment's progress to the real local
                // PlayerPrefs instead of no-op'ing. Undo that here so it
                // can't leak into another test or the developer's own
                // manual Play Mode session.
                PlayerPrefs.DeleteKey("Cutrium.Progress.CurrentLevelIndex");
            }

            private static GuidedTrainingDefinition CreateTestDefinition()
            {
                var definition = ScriptableObject.CreateInstance<
                    GuidedTrainingDefinition>();
                definition.ConfigureForSetup(TrainedLevelId, new[]
                {
                    GuidedTrainingStep.Info(
                        "THIS IS YOUR CUT SPEED",
                        "BU SENİN KESİM HIZIN",
                        GuidedTrainingFocusTarget.BarrierSpeed),
                    GuidedTrainingStep.Info(
                        "THESE ARE YOUR LIVES",
                        "BUNLAR CANLARIN",
                        GuidedTrainingFocusTarget.Lives),
                    GuidedTrainingStep.Observe(
                        "WATCH THE THREAT",
                        "TEHDİDİ İZLE",
                        1.6f),
                    GuidedTrainingStep.ActionStep(
                        GuidedTrainingActionKind.HorizontalBarrier,
                        GuidedTrainingHandMotion.Horizontal,
                        Cut1Origin,
                        "SWIPE LEFT OR RIGHT",
                        "SAĞA VEYA SOLA KAYDIR",
                        "WATCH IT GROW",
                        "BÜYÜMESİNİ İZLE",
                        "NICE CUT! WATCH THE TARGET FILL",
                        "GÜZEL KESİM! HEDEFİN DOLUŞUNU İZLE",
                        GuidedTrainingFocusTarget.Progress,
                        GuidedTrainingCompletionGate.None,
                        0.2f),
                    GuidedTrainingStep.ActionStep(
                        GuidedTrainingActionKind.VerticalBarrier,
                        GuidedTrainingHandMotion.Vertical,
                        Cut2Origin,
                        "SWIPE UP OR DOWN",
                        "YUKARI VEYA AŞAĞI KAYDIR",
                        "WATCH IT GROW",
                        "BÜYÜMESİNİ İZLE",
                        "GREAT! ALMOST THERE",
                        "HARİKA! NEREDEYSE BİTTİ",
                        GuidedTrainingFocusTarget.None,
                        GuidedTrainingCompletionGate.None,
                        0.15f),
                    GuidedTrainingStep.ActionStep(
                        GuidedTrainingActionKind.FreeBarrier,
                        GuidedTrainingHandMotion.None,
                        null,
                        "MAKE THE FINAL CUT",
                        "SON KESİMİ YAP",
                        "WATCH IT GROW",
                        "BÜYÜMESİNİ İZLE",
                        successEnglish: null,
                        successTurkish: null,
                        successFocus: GuidedTrainingFocusTarget.Progress,
                        completionGate: GuidedTrainingCompletionGate.None,
                        successSeconds: 0f,
                        freeze: false,
                        requiresLevelCompletion: true,
                        promptFocus: GuidedTrainingFocusTarget.Progress),
                });
                return definition;
            }

            private static CoreFunLevelDefinition[] CreateTestLevels() =>
                new[]
                {
                    new CoreFunLevelDefinition(
                        TrainedLevelId,
                        1,
                        new Vector2(9f, 15f),
                        new Vector2(1f, 0f),
                        0.01f,
                        0.35f,
                        0.6f,
                        40f,
                        0.08f,
                        0.3f,
                        8,
                        16,
                        8,
                        "test",
                        30f),
                    new CoreFunLevelDefinition(
                        UntrainedLevelId,
                        2,
                        new Vector2(9f, 15f),
                        new Vector2(1f, 0f),
                        0.01f,
                        0.35f,
                        0.6f,
                        40f,
                        0.08f,
                        0.3f,
                        8,
                        16,
                        8,
                        "test",
                        30f),
                };
        }
    }
}
