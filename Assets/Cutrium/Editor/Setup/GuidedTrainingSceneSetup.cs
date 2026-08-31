using System;
using System.Linq;
using Cutrium.Gameplay.Geometry;
using Cutrium.Presentation.HUD;
using Cutrium.Presentation.Localization;
using Cutrium.Unity.Input;
using Cutrium.Unity.Simulation;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Cutrium.Editor.Setup
{
    /// <summary>
    /// Idempotently wires the Level 1 <see cref="GuidedTrainingPresenter"/>
    /// overlay (definition asset, focus highlight, and serialized
    /// references) into the active gameplay scene. Replaces the retired
    /// same-hold-axis-switch tutorial object.
    /// </summary>
    public static class GuidedTrainingSceneSetup
    {
        private const string DefinitionFolder = "Assets/Cutrium/Content/Training";
        private const string Level1DefinitionPath =
            DefinitionFolder + "/Level1GuidedTraining.asset";
        private const string Level1StableId = "learn-the-cut";
        private const string TutorialObjectName = "GuidedTraining";
        private const string LegacyTutorialObjectName =
            "FirstLevelGestureTutorial";

        [MenuItem("Cutrium/Setup/Guided Training Scene Setup")]
        public static void Apply()
        {
            GuidedTrainingDefinition level1 = BuildLevel1Definition();
            AssetDatabase.SaveAssets();

            Scene scene = EditorSceneManager.OpenScene(
                Milestone2SceneSetup.VerticalSliceScenePath,
                OpenSceneMode.Single);

            GameObject root = scene.GetRootGameObjects()
                .FirstOrDefault(go => go.name == "VerticalSliceRoot");
            if (root == null)
            {
                throw new InvalidOperationException(
                    "VerticalSliceRoot was not found in the active scene.");
            }

            var controller = root.GetComponentInChildren<
                FirstPlayableController>(true);
            var gesture = root.GetComponentInChildren<
                BarrierGestureAdapter>(true);
            var preLevelIntro = root.GetComponentInChildren<
                PreLevelIntroPresenter>(true);
            var localization = root.GetComponentInChildren<
                LocalizationService>(true);
            var sandProgress = root.GetComponentInChildren<
                SandProgressPresenter>(true);
            RequireDependency(controller, nameof(FirstPlayableController));
            RequireDependency(gesture, nameof(BarrierGestureAdapter));
            RequireDependency(preLevelIntro, nameof(PreLevelIntroPresenter));
            RequireDependency(localization, nameof(LocalizationService));
            RequireDependency(sandProgress, nameof(SandProgressPresenter));

            GameObject tutorialGo =
                FindDescendant(root.transform, TutorialObjectName)
                ?? FindDescendant(root.transform, LegacyTutorialObjectName);
            if (tutorialGo == null)
            {
                throw new InvalidOperationException(
                    "The tutorial overlay object was not found under " +
                    "VerticalSliceRoot.");
            }

            tutorialGo.name = TutorialObjectName;
            GameObjectUtility.RemoveMonoBehavioursWithMissingScript(
                tutorialGo);

            CanvasGroup canvasGroup = tutorialGo.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = tutorialGo.AddComponent<CanvasGroup>();
            }

            GameObject handGo = FindDescendant(
                tutorialGo.transform,
                "HandSwipe");
            GameObject instructionGo = FindDescendant(
                tutorialGo.transform,
                "InstructionText");
            if (handGo == null || instructionGo == null)
            {
                throw new InvalidOperationException(
                    "The tutorial overlay is missing its hand or " +
                    "instruction child object.");
            }

            RectTransform handRect = (RectTransform)handGo.transform;
            TMP_Text instructionText =
                instructionGo.GetComponent<TMP_Text>();

            TMP_Text tapToContinueText = EnsureTapToContinueText(
                tutorialGo.transform,
                instructionText);

            TrainingFocusHighlightPresenter focusHighlight =
                EnsureFocusHighlight(tutorialGo.transform);

            GameObject speedHudGo = FindDescendant(root.transform, "SpeedHUD");
            GameObject heartRowGo = FindDescendant(root.transform, "HeartRow");
            if (speedHudGo == null || heartRowGo == null)
            {
                throw new InvalidOperationException(
                    "SpeedHUD or HeartRow was not found under " +
                    "VerticalSliceRoot.");
            }

            GuidedTrainingPresenter presenter =
                tutorialGo.GetComponent<GuidedTrainingPresenter>();
            if (presenter == null)
            {
                presenter = tutorialGo.AddComponent<GuidedTrainingPresenter>();
            }

            presenter.ConfigureForSetup(
                new[] { level1 },
                controller,
                gesture,
                preLevelIntro,
                localization,
                sandProgress,
                canvasGroup,
                handRect,
                instructionText,
                tapToContinueText,
                focusHighlight,
                sandProgress.ProgressBarRect,
                speedHudFocusTarget: (RectTransform)speedHudGo.transform,
                livesHudFocusTarget: (RectTransform)heartRowGo.transform,
                travelDistance: 52f,
                cycleSeconds: 1.15f,
                fadeSeconds: 0.16f);

            EditorUtility.SetDirty(tutorialGo);
            EditorUtility.SetDirty(presenter);
            EditorSceneManager.MarkSceneDirty(scene);

            if (!EditorSceneManager.SaveScene(
                    scene,
                    Milestone2SceneSetup.VerticalSliceScenePath))
            {
                throw new InvalidOperationException(
                    "Unity could not save the guided training scene setup.");
            }

            AssetDatabase.SaveAssets();
            Debug.Log(
                "Guided training scene setup complete: Level 1 uses " +
                "GuidedTrainingPresenter with a six-step definition (HUD " +
                "intro, watch, two guided cuts, one free finishing cut).");
        }

        private static GuidedTrainingDefinition BuildLevel1Definition()
        {
            if (!AssetDatabase.IsValidFolder(DefinitionFolder))
            {
                AssetDatabase.CreateFolder(
                    "Assets/Cutrium/Content",
                    "Training");
            }

            var definition = AssetDatabase.LoadAssetAtPath<
                GuidedTrainingDefinition>(Level1DefinitionPath);
            if (definition == null)
            {
                definition = ScriptableObject.CreateInstance<
                    GuidedTrainingDefinition>();
                AssetDatabase.CreateAsset(definition, Level1DefinitionPath);
            }

            // Fixed points below are not arbitrary: verified against the
            // real Level 1 threat physics (initial position (5, 8),
            // direction (0.8, 0.6), speed 1.6) via a deterministic
            // simulation probe. With a 1.5s Observe beat (threat frozen
            // through both preceding Info steps, so this is the same for
            // every player), the threat is at (~6.9, ~9.4) when the
            // horizontal prompt begins. Cut 1 at (5, 7.5) is safely below
            // it and captures 46.875% of the board; cut 2 at (5, 11) is
            // the horizontal center of the remaining room (the threat has
            // drifted to its right by then) and brings the cumulative
            // total to 73.4375% — just under the 75% target, leaving a
            // small, real final cut for the player to make unassisted.
            var cut1Origin = new LogicalPoint(5f, 7.5f);
            var cut2Origin = new LogicalPoint(5f, 11f);

            definition.ConfigureForSetup(Level1StableId, new[]
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
                    1.5f),
                GuidedTrainingStep.ActionStep(
                    GuidedTrainingActionKind.HorizontalBarrier,
                    GuidedTrainingHandMotion.Horizontal,
                    cut1Origin,
                    "SWIPE LEFT OR RIGHT",
                    "SAĞA VEYA SOLA KAYDIR",
                    "WATCH IT GROW",
                    "BÜYÜMESİNİ İZLE",
                    "NICE CUT! WATCH THE TARGET FILL",
                    "GÜZEL KESİM! HEDEFİN DOLUŞUNU İZLE",
                    GuidedTrainingFocusTarget.Progress,
                    GuidedTrainingCompletionGate.ProgressSettled,
                    1.5f),
                GuidedTrainingStep.ActionStep(
                    GuidedTrainingActionKind.VerticalBarrier,
                    GuidedTrainingHandMotion.Vertical,
                    cut2Origin,
                    "SWIPE UP OR DOWN",
                    "YUKARI VEYA AŞAĞI KAYDIR",
                    "WATCH IT GROW",
                    "BÜYÜMESİNİ İZLE",
                    "GREAT! ALMOST THERE",
                    "HARİKA! NEREDEYSE BİTTİ",
                    GuidedTrainingFocusTarget.None,
                    GuidedTrainingCompletionGate.None,
                    1.25f),
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

            EditorUtility.SetDirty(definition);
            return definition;
        }

        private static TMP_Text EnsureTapToContinueText(
            Transform parent,
            TMP_Text styleSource)
        {
            Transform existing = parent.Find("TapToContinueText");
            GameObject go = existing != null
                ? existing.gameObject
                : new GameObject(
                    "TapToContinueText",
                    typeof(RectTransform),
                    typeof(TextMeshProUGUI));
            if (existing == null)
            {
                go.transform.SetParent(parent, false);
            }

            var rect = (RectTransform)go.transform;
            rect.anchorMin = new Vector2(0.1f, 0f);
            rect.anchorMax = new Vector2(0.9f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, 48f);
            rect.sizeDelta = new Vector2(0f, 60f);

            TMP_Text text = go.GetComponent<TMP_Text>();
            if (styleSource != null)
            {
                text.font = styleSource.font;
                text.fontSharedMaterial = styleSource.fontSharedMaterial;
                text.color = styleSource.color;
            }

            text.fontSize = 32f;
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;
            text.text = string.Empty;
            go.SetActive(false);

            return text;
        }

        private static TrainingFocusHighlightPresenter EnsureFocusHighlight(
            Transform parent)
        {
            Transform existing = parent.Find("TrainingFocusHighlight");
            GameObject holder = existing != null
                ? existing.gameObject
                : new GameObject(
                    "TrainingFocusHighlight",
                    typeof(RectTransform));
            if (existing == null)
            {
                holder.transform.SetParent(parent, false);
            }

            // Clean up the old frame-overlay hierarchy/CanvasGroup this
            // setup used to create before the presenter pulsed the target
            // itself instead of drawing a frame over it.
            Transform staleFrame = holder.transform.Find("Frame");
            if (staleFrame != null)
            {
                UnityEngine.Object.DestroyImmediate(staleFrame.gameObject);
            }

            CanvasGroup staleGroup = holder.GetComponent<CanvasGroup>();
            if (staleGroup != null)
            {
                UnityEngine.Object.DestroyImmediate(staleGroup);
            }

            TrainingFocusHighlightPresenter presenter =
                holder.GetComponent<TrainingFocusHighlightPresenter>();
            if (presenter == null)
            {
                presenter = holder.AddComponent<
                    TrainingFocusHighlightPresenter>();
            }

            presenter.ConfigureForSetup();
            return presenter;
        }

        private static GameObject FindDescendant(
            Transform root,
            string name)
        {
            foreach (Transform child in root)
            {
                if (child.name == name)
                {
                    return child.gameObject;
                }

                GameObject found = FindDescendant(child, name);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private static void RequireDependency(
            UnityEngine.Object dependency,
            string name)
        {
            if (dependency == null)
            {
                throw new InvalidOperationException(
                    $"{name} was not found under VerticalSliceRoot.");
            }
        }
    }
}
