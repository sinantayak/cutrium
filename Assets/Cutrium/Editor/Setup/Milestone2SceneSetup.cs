using System;
using System.Linq;
using Cutrium.Presentation.Barriers;
using Cutrium.Presentation.Capture;
using Cutrium.Presentation.HUD;
using Cutrium.Presentation.Threats;
using Cutrium.Unity.Bootstrap;
using Cutrium.Unity.Input;
using Cutrium.Unity.Simulation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace Cutrium.Editor.Setup
{
    public static class Milestone2SceneSetup
    {
        public const string VerticalSliceScenePath =
            "Assets/Cutrium/Scenes/VerticalSlice.unity";

        private static readonly Color ThreatColor =
            new Color(1f, 0.38f, 0.42f, 1f);

        [MenuItem("Cutrium/Setup/Milestone 2 First Playable")]
        public static void Apply()
        {
            VerifyBaseline();
            Scene scene = EditorSceneManager.OpenScene(
                VerticalSliceScenePath,
                OpenSceneMode.Single);
            ConfigurePhase2A(scene);
            ConfigurePhase2B(scene);
            ConfigurePhase2C(scene);
            ValidatePhase2A(scene);
            ValidatePhase2B(scene);
            ValidatePhase2C(scene);

            if (!EditorSceneManager.SaveScene(scene, VerticalSliceScenePath))
            {
                throw new InvalidOperationException(
                    $"Unity could not save '{VerticalSliceScenePath}'.");
            }

            AssetDatabase.SaveAssets();
            Debug.Log(
                "Milestone 2 scene setup verified through Phase 2C. " +
                "First-playable capture and retry references are serialized.");
        }

        private static void VerifyBaseline()
        {
            if (!string.Equals(
                    Application.unityVersion,
                    "6000.3.21f1",
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "Milestone 2 requires Unity 6000.3.21f1, but " +
                    $"'{Application.unityVersion}' is running.");
            }

            VerifyPackageVersion(
                "Packages/com.unity.inputsystem",
                "1.20.0");
            VerifyPackageVersion(
                "Packages/com.unity.render-pipelines.universal",
                "17.3.0");
        }

        private static void VerifyPackageVersion(
            string assetPath,
            string expectedVersion)
        {
            PackageInfo packageInfo = PackageInfo.FindForAssetPath(assetPath);
            if (packageInfo == null
                || !string.Equals(
                    packageInfo.version,
                    expectedVersion,
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Expected '{assetPath}' at '{expectedVersion}', found " +
                    $"'{packageInfo?.version ?? "missing"}'.");
            }
        }

        private static void ConfigurePhase2A(Scene scene)
        {
            GameObject verticalSliceRoot = RequireRoot(scene, "VerticalSliceRoot");
            Transform root = verticalSliceRoot.transform;
            Transform boardFrame = RequireChild(
                root,
                "Canvas/SafeAreaRoot/BoardViewport/BoardFrame");

            GameObject gameplayRoot = GetOrCreateChild(root, "GameplayRoot");
            FirstPlayableController controller =
                GetOrAddComponent<FirstPlayableController>(gameplayRoot);
            controller.ConfigureForSetup(
                new Vector2(5f, 8f),
                new Vector2(0.8f, 0.6f),
                3f,
                0.35f,
                8,
                8);

            GameObject presenterObject = GetOrCreateChild(
                gameplayRoot.transform,
                "ThreatPresenter");
            ThreatPresenter presenter =
                GetOrAddComponent<ThreatPresenter>(presenterObject);

            RectTransform threatVisual = GetOrCreateUiChild(
                boardFrame,
                "ThreatVisual");
            Image threatImage = GetOrAddComponent<Image>(threatVisual.gameObject);
            threatImage.color = ThreatColor;
            threatImage.raycastTarget = false;
            if (threatImage.sprite == null)
            {
                threatImage.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>(
                    "UI/Skin/Knob.psd");
            }

            presenter.Configure(
                controller,
                (RectTransform)boardFrame,
                threatVisual,
                threatImage,
                presenter.OptionalSprite,
                presenter.VisualLogicalDiameter > 0f
                    ? presenter.VisualLogicalDiameter
                    : 0.9f);

            EditorUtility.SetDirty(controller);
            EditorUtility.SetDirty(presenter);
            EditorUtility.SetDirty(threatImage);
        }

        private static void ValidatePhase2A(Scene scene)
        {
            GameObject root = RequireRoot(scene, "VerticalSliceRoot");
            FirstPlayableController[] controllers = root
                .GetComponentsInChildren<FirstPlayableController>(true);
            ThreatPresenter[] presenters = root
                .GetComponentsInChildren<ThreatPresenter>(true);

            if (controllers.Length != 1)
            {
                throw new InvalidOperationException(
                    $"Expected one FirstPlayableController, found {controllers.Length}.");
            }

            if (presenters.Length != 1)
            {
                throw new InvalidOperationException(
                    $"Expected one ThreatPresenter, found {presenters.Length}.");
            }

            FirstPlayableController controller = controllers[0];
            ThreatPresenter presenter = presenters[0];
            if (presenter.Controller != controller
                || presenter.BoardFrame == null
                || presenter.Visual == null
                || presenter.Image == null)
            {
                throw new InvalidOperationException(
                    "ThreatPresenter has missing or mismatched serialized references.");
            }

            if (!Mathf.Approximately(controller.ThreatRadius, 0.35f)
                || !Mathf.Approximately(controller.ThreatSpeed, 3f)
                || controller.MaximumCatchUpTicks != 8)
            {
                throw new InvalidOperationException(
                    "The serialized Phase 2A tuning does not match the reviewed defaults.");
            }
        }

        private static void ConfigurePhase2B(Scene scene)
        {
            GameObject root = RequireRoot(scene, "VerticalSliceRoot");
            Transform boardFrame = RequireChild(
                root.transform,
                "Canvas/SafeAreaRoot/BoardViewport/BoardFrame");
            SceneCompositionRoot composition = RequireChild(
                    root.transform,
                    "SceneCompositionRoot")
                .GetComponent<SceneCompositionRoot>();
            Transform gameplayRoot = RequireChild(root.transform, "GameplayRoot");
            FirstPlayableController controller =
                gameplayRoot.GetComponent<FirstPlayableController>();

            GameObject gestureObject = GetOrCreateChild(
                gameplayRoot,
                "BarrierGesture");
            BarrierGestureAdapter gesture =
                GetOrAddComponent<BarrierGestureAdapter>(gestureObject);
            gesture.Configure(composition.PointerInput, 0.35f, 0.1f);
            controller.ConfigureBarrierForSetup(
                gesture,
                8f,
                0.08f,
                0.6f,
                16);

            GameObject presenterObject = GetOrCreateChild(
                gameplayRoot,
                "BarrierPresenter");
            BarrierPresenter presenter =
                GetOrAddComponent<BarrierPresenter>(presenterObject);
            RectTransform preview = CreateBarrierVisual(boardFrame, "BarrierPreview");
            RectTransform negative = CreateBarrierVisual(
                boardFrame,
                "BarrierNegativeHalf");
            RectTransform positive = CreateBarrierVisual(
                boardFrame,
                "BarrierPositiveHalf");
            RectTransform failure = CreateBarrierVisual(
                boardFrame,
                "BarrierBreakFeedback");
            presenter.Configure(
                controller,
                gesture,
                (RectTransform)boardFrame,
                preview,
                preview.GetComponent<Image>(),
                negative,
                negative.GetComponent<Image>(),
                positive,
                positive.GetComponent<Image>(),
                failure,
                failure.GetComponent<Image>(),
                presenter.VisualLogicalThickness > 0f
                    ? presenter.VisualLogicalThickness
                    : 0.22f,
                0.16f);

            EditorUtility.SetDirty(gesture);
            EditorUtility.SetDirty(controller);
            EditorUtility.SetDirty(presenter);
        }

        private static void ValidatePhase2B(Scene scene)
        {
            GameObject root = RequireRoot(scene, "VerticalSliceRoot");
            BarrierGestureAdapter[] gestures = root
                .GetComponentsInChildren<BarrierGestureAdapter>(true);
            BarrierPresenter[] presenters = root
                .GetComponentsInChildren<BarrierPresenter>(true);
            FirstPlayableController controller = root
                .GetComponentInChildren<FirstPlayableController>(true);
            if (gestures.Length != 1 || presenters.Length != 1)
            {
                throw new InvalidOperationException(
                    $"Expected one barrier gesture/presenter, found " +
                    $"{gestures.Length}/{presenters.Length}.");
            }

            BarrierGestureAdapter gesture = gestures[0];
            BarrierPresenter presenter = presenters[0];
            if (controller.BarrierGesture != gesture
                || gesture.PointerInput == null
                || presenter.Controller != controller
                || presenter.Gesture != gesture
                || presenter.BoardFrame == null
                || presenter.Preview == null
                || presenter.NegativeHalf == null
                || presenter.PositiveHalf == null
                || presenter.FailureFeedback == null)
            {
                throw new InvalidOperationException(
                    "Phase 2B has a missing or mismatched serialized reference.");
            }

            if (!Mathf.Approximately(controller.BarrierGrowthSpeed, 8f)
                || !Mathf.Approximately(
                    controller.BarrierCollisionHalfWidth,
                    0.08f)
                || !Mathf.Approximately(
                    controller.BarrierMinimumEdgeMargin,
                    0.6f))
            {
                throw new InvalidOperationException(
                    "Phase 2B barrier tuning differs from the reviewed defaults.");
            }
        }

        private static void ConfigurePhase2C(Scene scene)
        {
            GameObject root = RequireRoot(scene, "VerticalSliceRoot");
            Transform safeArea = RequireChild(
                root.transform,
                "Canvas/SafeAreaRoot");
            RectTransform boardFrame = (RectTransform)RequireChild(
                safeArea,
                "BoardViewport/BoardFrame");
            Transform gameplayRoot = RequireChild(root.transform, "GameplayRoot");
            FirstPlayableController controller =
                gameplayRoot.GetComponent<FirstPlayableController>();
            controller.ConfigureCaptureForSetup(0.75f);

            RectTransform capturedRoot = GetOrCreateUiChild(
                boardFrame,
                "CapturedRegions");
            StretchToParent(capturedRoot);
            capturedRoot.SetAsFirstSibling();
            RectTransform completedRoot = GetOrCreateUiChild(
                boardFrame,
                "CompletedBarriers");
            StretchToParent(completedRoot);

            GameObject boardPresenterObject = GetOrCreateChild(
                gameplayRoot,
                "CaptureBoardPresenter");
            CaptureBoardPresenter boardPresenter =
                GetOrAddComponent<CaptureBoardPresenter>(boardPresenterObject);
            boardPresenter.Configure(
                controller,
                boardFrame,
                capturedRoot,
                completedRoot,
                0.22f);

            RectTransform topHud = (RectTransform)RequireChild(
                safeArea,
                "TopHUD");
            Text percentage = ConfigureText(
                GetOrCreateUiChild(topHud, "CapturePercentage"),
                "Captured 0%",
                26,
                TextAnchor.MiddleCenter);
            Text target = ConfigureText(
                GetOrCreateUiChild(topHud, "CaptureTarget"),
                "Target 75%",
                22,
                TextAnchor.MiddleCenter);
            EnsureLayoutElement(percentage.rectTransform, 170f, 48f);
            EnsureLayoutElement(target.rectTransform, 140f, 48f);

            RectTransform overlay = GetOrCreateUiChild(
                safeArea,
                "LevelCompleteOverlay");
            StretchToParent(overlay);
            Image overlayImage = GetOrAddComponent<Image>(overlay.gameObject);
            overlayImage.color = new Color(0.04f, 0.07f, 0.12f, 0.86f);
            overlayImage.raycastTarget = true;

            RectTransform completeTextRect = GetOrCreateUiChild(
                overlay,
                "CompleteText");
            completeTextRect.anchorMin = new Vector2(0.15f, 0.55f);
            completeTextRect.anchorMax = new Vector2(0.85f, 0.7f);
            completeTextRect.offsetMin = Vector2.zero;
            completeTextRect.offsetMax = Vector2.zero;
            Text completeText = ConfigureText(
                completeTextRect,
                "LEVEL COMPLETE",
                42,
                TextAnchor.MiddleCenter);

            RectTransform retryRect = GetOrCreateUiChild(overlay, "RetryButton");
            retryRect.anchorMin = new Vector2(0.3f, 0.38f);
            retryRect.anchorMax = new Vector2(0.7f, 0.5f);
            retryRect.offsetMin = Vector2.zero;
            retryRect.offsetMax = Vector2.zero;
            Image retryImage = GetOrAddComponent<Image>(retryRect.gameObject);
            retryImage.color = new Color(0.22f, 0.72f, 0.68f, 1f);
            Button retryButton = GetOrAddComponent<Button>(retryRect.gameObject);
            retryButton.targetGraphic = retryImage;
            RectTransform retryLabelRect = GetOrCreateUiChild(
                retryRect,
                "Label");
            StretchToParent(retryLabelRect);
            ConfigureText(
                retryLabelRect,
                "RETRY",
                30,
                TextAnchor.MiddleCenter).raycastTarget = false;
            overlay.gameObject.SetActive(false);

            GameObject hudPresenterObject = GetOrCreateChild(
                gameplayRoot,
                "CaptureHudPresenter");
            CaptureHudPresenter hudPresenter =
                GetOrAddComponent<CaptureHudPresenter>(hudPresenterObject);
            hudPresenter.Configure(
                controller,
                percentage,
                target,
                overlay.gameObject,
                completeText,
                retryButton);

            EditorUtility.SetDirty(controller);
            EditorUtility.SetDirty(boardPresenter);
            EditorUtility.SetDirty(hudPresenter);
            EditorUtility.SetDirty(overlayImage);
            EditorUtility.SetDirty(retryButton);
        }

        private static void ValidatePhase2C(Scene scene)
        {
            GameObject root = RequireRoot(scene, "VerticalSliceRoot");
            FirstPlayableController controller = root
                .GetComponentInChildren<FirstPlayableController>(true);
            CaptureBoardPresenter[] boardPresenters = root
                .GetComponentsInChildren<CaptureBoardPresenter>(true);
            CaptureHudPresenter[] hudPresenters = root
                .GetComponentsInChildren<CaptureHudPresenter>(true);
            if (boardPresenters.Length != 1 || hudPresenters.Length != 1)
            {
                throw new InvalidOperationException(
                    $"Expected one capture board/HUD presenter, found " +
                    $"{boardPresenters.Length}/{hudPresenters.Length}.");
            }

            CaptureBoardPresenter board = boardPresenters[0];
            CaptureHudPresenter hud = hudPresenters[0];
            if (board.Controller != controller
                || board.BoardFrame == null
                || board.CapturedRegionRoot == null
                || board.CompletedBarrierRoot == null
                || hud.Controller != controller
                || hud.PercentageText == null
                || hud.TargetText == null
                || hud.CompleteOverlay == null
                || hud.RetryButton == null)
            {
                throw new InvalidOperationException(
                    "Phase 2C has a missing or mismatched serialized reference.");
            }

            if (!Mathf.Approximately(controller.TargetCapturedFraction, 0.75f))
            {
                throw new InvalidOperationException(
                    "The first-playable capture target must serialize as 75%.");
            }
        }

        private static RectTransform CreateBarrierVisual(
            Transform parent,
            string name)
        {
            RectTransform rect = GetOrCreateUiChild(parent, name);
            Image image = GetOrAddComponent<Image>(rect.gameObject);
            image.raycastTarget = false;
            if (image.sprite == null)
            {
                image.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>(
                    "UI/Skin/UISprite.psd");
            }

            rect.gameObject.SetActive(false);
            EditorUtility.SetDirty(image);
            return rect;
        }

        private static Text ConfigureText(
            RectTransform rect,
            string value,
            int fontSize,
            TextAnchor alignment)
        {
            Text text = GetOrAddComponent<Text>(rect.gameObject);
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = value;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = Color.white;
            text.raycastTarget = false;
            EditorUtility.SetDirty(text);
            return text;
        }

        private static void EnsureLayoutElement(
            RectTransform rect,
            float preferredWidth,
            float preferredHeight)
        {
            LayoutElement layout = GetOrAddComponent<LayoutElement>(rect.gameObject);
            layout.preferredWidth = preferredWidth;
            layout.preferredHeight = preferredHeight;
            EditorUtility.SetDirty(layout);
        }

        private static void StretchToParent(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static GameObject RequireRoot(Scene scene, string name)
        {
            GameObject root = scene.GetRootGameObjects().SingleOrDefault(
                candidate => candidate.name == name);
            if (root == null)
            {
                throw new InvalidOperationException(
                    $"Scene '{scene.path}' requires root '{name}'.");
            }

            return root;
        }

        private static Transform RequireChild(Transform parent, string path)
        {
            Transform child = parent.Find(path);
            if (child == null)
            {
                throw new InvalidOperationException(
                    $"Scene requires '{parent.name}/{path}'.");
            }

            return child;
        }

        private static GameObject GetOrCreateChild(Transform parent, string name)
        {
            Transform existing = parent.Find(name);
            if (existing != null)
            {
                return existing.gameObject;
            }

            var gameObject = new GameObject(name);
            gameObject.transform.SetParent(parent, false);
            return gameObject;
        }

        private static RectTransform GetOrCreateUiChild(
            Transform parent,
            string name)
        {
            Transform existing = parent.Find(name);
            if (existing != null)
            {
                if (existing is RectTransform existingRect)
                {
                    return existingRect;
                }

                throw new InvalidOperationException(
                    $"Existing UI child '{name}' is not a RectTransform.");
            }

            var gameObject = new GameObject(
                name,
                typeof(RectTransform),
                typeof(CanvasRenderer));
            var rectTransform = (RectTransform)gameObject.transform;
            rectTransform.SetParent(parent, false);
            return rectTransform;
        }

        private static T GetOrAddComponent<T>(GameObject gameObject)
            where T : Component
        {
            T component = gameObject.GetComponent<T>();
            return component != null ? component : gameObject.AddComponent<T>();
        }
    }
}
