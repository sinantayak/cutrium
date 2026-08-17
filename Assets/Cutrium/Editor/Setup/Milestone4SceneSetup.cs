using System;
using System.Linq;
using Cutrium.Presentation.Capture;
using Cutrium.Presentation.Feedback;
using Cutrium.Presentation.HUD;
using Cutrium.Unity.Simulation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace Cutrium.Editor.Setup
{
    public static class Milestone4SceneSetup
    {
        public const string FeedbackTuningAssetPath =
            "Assets/Cutrium/Content/Feedback/FeedbackTuning.asset";

        [MenuItem("Cutrium/Setup/Milestone 4 Feedback Loop")]
        public static void Apply()
        {
            VerifyBaseline();
            Milestone3SceneSetup.Apply();
            FeedbackTuningDefinition tuning = GetOrCreateTuning();
            Scene scene = EditorSceneManager.OpenScene(
                Milestone2SceneSetup.VerticalSliceScenePath,
                OpenSceneMode.Single);
            Configure(scene, tuning);
            Validate(scene, tuning);
            if (!EditorSceneManager.SaveScene(
                    scene,
                    Milestone2SceneSetup.VerticalSliceScenePath))
            {
                throw new InvalidOperationException(
                    "Unity could not save the Milestone 4 scene.");
            }

            AssetDatabase.SaveAssets();
            Debug.Log(
                "Milestone 4 scene setup verified. Logical rewards, " +
                "fallback feedback, optional audio, and no-op haptics are ready.");
        }

        private static void VerifyBaseline()
        {
            if (!string.Equals(
                    Application.unityVersion,
                    "6000.3.21f1",
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "Milestone 4 requires Unity 6000.3.21f1.");
            }

            VerifyPackage(
                "Packages/com.unity.render-pipelines.universal",
                "17.3.0");
            VerifyPackage("Packages/com.unity.inputsystem", "1.20.0");
        }

        private static void VerifyPackage(string path, string version)
        {
            PackageInfo package = PackageInfo.FindForAssetPath(path);
            if (package == null
                || !string.Equals(package.version, version, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Expected '{path}' at '{version}', found " +
                    $"'{package?.version ?? "missing"}'.");
            }
        }

        private static FeedbackTuningDefinition GetOrCreateTuning()
        {
            EnsureFolder("Assets/Cutrium/Content");
            EnsureFolder("Assets/Cutrium/Content/Feedback");
            FeedbackTuningDefinition tuning =
                AssetDatabase.LoadAssetAtPath<FeedbackTuningDefinition>(
                    FeedbackTuningAssetPath);
            if (tuning == null)
            {
                tuning = ScriptableObject.CreateInstance<
                    FeedbackTuningDefinition>();
                AssetDatabase.CreateAsset(tuning, FeedbackTuningAssetPath);
            }

            tuning.ConfigureForSetup(
                0.45f,
                0.75f,
                0.2f,
                0.18f,
                0.22f,
                0.65f);
            EditorUtility.SetDirty(tuning);
            return tuning;
        }

        private static void Configure(
            Scene scene,
            FeedbackTuningDefinition tuning)
        {
            GameObject root = RequireRoot(scene, "VerticalSliceRoot");
            Transform safeArea = RequireChild(
                root.transform,
                "Canvas/SafeAreaRoot");
            FirstPlayableController controller = root
                .GetComponentInChildren<FirstPlayableController>(true);
            CaptureBoardPresenter boardPresenter = root
                .GetComponentInChildren<CaptureBoardPresenter>(true);
            CaptureHudPresenter hudPresenter = root
                .GetComponentInChildren<CaptureHudPresenter>(true);
            controller.ConfigureFeedbackForSetup(tuning);
            boardPresenter.ConfigureFeedbackRevealForSetup(
                tuning.CaptureRevealSeconds);
            hudPresenter.ConfigureFeedbackAnimationForSetup(
                tuning.PercentageAnimationSeconds);

            RectTransform overlay = GetOrCreateUiChild(
                safeArea,
                "FeedbackOverlay");
            StretchToParent(overlay);
            LayoutElement overlayLayout = GetOrAddComponent<LayoutElement>(
                overlay.gameObject);
            overlayLayout.ignoreLayout = true;
            CanvasGroup cueCanvas = GetOrAddComponent<CanvasGroup>(
                overlay.gameObject);
            cueCanvas.alpha = 0f;
            cueCanvas.interactable = false;
            cueCanvas.blocksRaycasts = false;
            // Big and dead-centered on screen -- this used to sit in a
            // small, off-center upper band (30pt) where it read as barely
            // visible; feedback cues ("LOCKED", "COMBO x2", ...) need to
            // land like a real pop, not a caption.
            RectTransform cueRect = GetOrCreateUiChild(overlay, "CueLabel");
            cueRect.anchorMin = new Vector2(0.08f, 0.4f);
            cueRect.anchorMax = new Vector2(0.92f, 0.6f);
            cueRect.offsetMin = Vector2.zero;
            cueRect.offsetMax = Vector2.zero;
            Text cueLabel = GetOrAddComponent<Text>(cueRect.gameObject);
            cueLabel.font =
                LandmarkRevealPresentationSetup.LoadLegacyUiFontForSetup();
            cueLabel.text = string.Empty;
            cueLabel.fontSize = 64;
            cueLabel.fontStyle = FontStyle.Bold;
            cueLabel.alignment = TextAnchor.MiddleCenter;
            cueLabel.color = Color.white;
            cueLabel.raycastTarget = false;
            cueLabel.resizeTextForBestFit = true;
            cueLabel.resizeTextMinSize = 30;
            cueLabel.resizeTextMaxSize = 64;
            Shadow cueShadow = GetOrAddComponent<Shadow>(cueRect.gameObject);
            cueShadow.effectColor = new Color(0f, 0f, 0f, 0.65f);
            cueShadow.effectDistance = new Vector2(3f, -3f);

            Image boardFrameGraphic = boardPresenter.BoardFrame
                .GetComponent<Image>();
            FeedbackPresenter feedbackPresenter =
                GetOrAddComponent<FeedbackPresenter>(overlay.gameObject);
            feedbackPresenter.Configure(
                controller,
                tuning,
                cueLabel,
                cueCanvas,
                boardFrameGraphic);

            Transform completeOverlay = RequireChild(
                safeArea,
                "LevelCompleteOverlay");
            overlay.SetSiblingIndex(Math.Max(
                0,
                completeOverlay.GetSiblingIndex()));
            completeOverlay.SetAsLastSibling();

            GameObject services = GetOrCreateChild(
                root.transform,
                "FeedbackServices");
            AudioSource audioSource = GetOrAddComponent<AudioSource>(services);
            audioSource.playOnAwake = false;
            audioSource.loop = false;
            audioSource.spatialBlend = 0f;
            FeedbackAudioPresenter audioPresenter =
                GetOrAddComponent<FeedbackAudioPresenter>(services);
            audioPresenter.Configure(controller, audioSource);
            FeedbackHapticPresenter hapticPresenter =
                GetOrAddComponent<FeedbackHapticPresenter>(services);
            hapticPresenter.Configure(controller);

            EditorUtility.SetDirty(controller);
            EditorUtility.SetDirty(boardPresenter);
            EditorUtility.SetDirty(hudPresenter);
            EditorUtility.SetDirty(overlayLayout);
            EditorUtility.SetDirty(cueCanvas);
            EditorUtility.SetDirty(cueLabel);
            EditorUtility.SetDirty(feedbackPresenter);
            EditorUtility.SetDirty(audioSource);
            EditorUtility.SetDirty(audioPresenter);
            EditorUtility.SetDirty(hapticPresenter);
        }

        private static void Validate(
            Scene scene,
            FeedbackTuningDefinition tuning)
        {
            GameObject root = RequireRoot(scene, "VerticalSliceRoot");
            FirstPlayableController controller = root
                .GetComponentInChildren<FirstPlayableController>(true);
            CaptureBoardPresenter boardPresenter = root
                .GetComponentInChildren<CaptureBoardPresenter>(true);
            CaptureHudPresenter hudPresenter = root
                .GetComponentInChildren<CaptureHudPresenter>(true);
            FeedbackPresenter[] feedbackPresenters = root
                .GetComponentsInChildren<FeedbackPresenter>(true);
            FeedbackAudioPresenter[] audioPresenters = root
                .GetComponentsInChildren<FeedbackAudioPresenter>(true);
            FeedbackHapticPresenter[] hapticPresenters = root
                .GetComponentsInChildren<FeedbackHapticPresenter>(true);
            Transform safeArea = RequireChild(
                root.transform,
                "Canvas/SafeAreaRoot");
            RectTransform overlay = (RectTransform)RequireChild(
                safeArea,
                "FeedbackOverlay");
            Transform completion = RequireChild(
                safeArea,
                "LevelCompleteOverlay");

            if (controller.FeedbackTuning != tuning
                || feedbackPresenters.Length != 1
                || audioPresenters.Length != 1
                || hapticPresenters.Length != 1
                || feedbackPresenters[0].Controller != controller
                || feedbackPresenters[0].Tuning != tuning
                || feedbackPresenters[0].CueLabel == null
                || feedbackPresenters[0].CueCanvasGroup == null
                || feedbackPresenters[0].CueCanvasGroup.blocksRaycasts
                || audioPresenters[0].Controller != controller
                || audioPresenters[0].AudioSource == null
                || audioPresenters[0].AudioSource.playOnAwake
                || audioPresenters[0].AudioSource.loop
                || hapticPresenters[0].Controller != controller
                || !overlay.GetComponent<LayoutElement>().ignoreLayout
                || completion.GetSiblingIndex() != safeArea.childCount - 1
                || !Mathf.Approximately(
                    boardPresenter.CaptureRevealDuration,
                    tuning.CaptureRevealSeconds)
                || !Mathf.Approximately(
                    hudPresenter.PercentageAnimationSeconds,
                    tuning.PercentageAnimationSeconds))
            {
                throw new InvalidOperationException(
                    "Milestone 4 feedback composition is incomplete.");
            }
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            int separator = path.LastIndexOf('/');
            string parent = path.Substring(0, separator);
            string name = path.Substring(separator + 1);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }

        private static GameObject RequireRoot(Scene scene, string name)
        {
            GameObject root = scene.GetRootGameObjects().SingleOrDefault(
                candidate => candidate.name == name);
            return root ?? throw new InvalidOperationException(
                $"Scene '{scene.path}' requires root '{name}'.");
        }

        private static Transform RequireChild(Transform parent, string path)
        {
            Transform child = parent.Find(path);
            return child ?? throw new InvalidOperationException(
                $"Scene requires '{parent.name}/{path}'.");
        }

        private static GameObject GetOrCreateChild(Transform parent, string name)
        {
            Transform existing = parent.Find(name);
            if (existing != null)
            {
                return existing.gameObject;
            }

            var child = new GameObject(name);
            child.transform.SetParent(parent, false);
            return child;
        }

        private static RectTransform GetOrCreateUiChild(
            Transform parent,
            string name)
        {
            Transform existing = parent.Find(name);
            if (existing is RectTransform rect)
            {
                return rect;
            }

            if (existing != null)
            {
                throw new InvalidOperationException(
                    $"Existing child '{name}' is not a RectTransform.");
            }

            var child = new GameObject(
                name,
                typeof(RectTransform),
                typeof(CanvasRenderer));
            var childRect = (RectTransform)child.transform;
            childRect.SetParent(parent, false);
            return childRect;
        }

        private static void StretchToParent(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static T GetOrAddComponent<T>(GameObject gameObject)
            where T : Component
        {
            T component = gameObject.GetComponent<T>();
            return component != null ? component : gameObject.AddComponent<T>();
        }
    }
}
