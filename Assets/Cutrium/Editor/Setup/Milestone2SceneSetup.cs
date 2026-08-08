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
                "Canvas/SafeAreaRoot/BoardStage/BoardViewport/BoardFrame");

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
                "Canvas/SafeAreaRoot/BoardStage/BoardViewport/BoardFrame");
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
                "BoardStage/BoardViewport/BoardFrame");
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
            RectTransform progressArea = GetOrCreateUiChild(
                topHud,
                "ProgressArea");
            Text percentage = ConfigureText(
                GetOrMoveUiChild(
                    topHud,
                    progressArea,
                    "CapturePercentage"),
                "Captured 0%",
                16,
                TextAnchor.MiddleCenter);
            Text target = ConfigureText(
                GetOrMoveUiChild(
                    topHud,
                    progressArea,
                    "CaptureTarget"),
                "Target 75%",
                14,
                TextAnchor.MiddleCenter);
            EnsureLayoutElement(percentage.rectTransform, 112f, 36f);
            EnsureLayoutElement(target.rectTransform, 88f, 36f);
            ConfigureFirstPlayableLayout(
                safeArea,
                progressArea,
                percentage.rectTransform,
                target.rectTransform);

            RectTransform overlay = GetOrCreateUiChild(
                safeArea,
                "LevelCompleteOverlay");
            StretchToParent(overlay);
            overlay.SetAsLastSibling();
            LayoutElement overlayLayout =
                GetOrAddComponent<LayoutElement>(overlay.gameObject);
            overlayLayout.ignoreLayout = true;
            CanvasGroup completionCanvasGroup =
                GetOrAddComponent<CanvasGroup>(overlay.gameObject);
            completionCanvasGroup.alpha = 0f;
            completionCanvasGroup.interactable = false;
            completionCanvasGroup.blocksRaycasts = false;
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
            overlay.gameObject.SetActive(true);

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
                completionCanvasGroup,
                completeText,
                retryButton);

            EditorUtility.SetDirty(controller);
            EditorUtility.SetDirty(boardPresenter);
            EditorUtility.SetDirty(hudPresenter);
            EditorUtility.SetDirty(overlayLayout);
            EditorUtility.SetDirty(completionCanvasGroup);
            EditorUtility.SetDirty(overlayImage);
            EditorUtility.SetDirty(retryButton);
        }

        private static void ConfigureFirstPlayableLayout(
            Transform safeArea,
            RectTransform progressArea,
            RectTransform percentage,
            RectTransform target)
        {
            VerticalLayoutGroup safeAreaLayout =
                safeArea.GetComponent<VerticalLayoutGroup>();
            safeAreaLayout.padding = new RectOffset(12, 12, 6, 6);
            safeAreaLayout.spacing = 4f;
            safeAreaLayout.childControlHeight = true;
            safeAreaLayout.childForceExpandHeight = false;

            RectTransform topHud = (RectTransform)RequireChild(
                safeArea,
                "TopHUD");
            LayoutElement topLayout = topHud.GetComponent<LayoutElement>();
            topLayout.minHeight = 52f;
            topLayout.preferredHeight = 60f;
            topLayout.flexibleHeight = 0f;
            HorizontalLayoutGroup topRow =
                topHud.GetComponent<HorizontalLayoutGroup>();
            topRow.padding = new RectOffset(8, 8, 8, 8);
            topRow.spacing = 8f;
            topRow.childAlignment = TextAnchor.MiddleCenter;
            topRow.childControlWidth = true;
            topRow.childControlHeight = true;
            topRow.childForceExpandWidth = false;
            topRow.childForceExpandHeight = false;

            Text title = RequireChild(topHud, "Title").GetComponent<Text>();
            title.text = "CUTRIUM";
            title.fontSize = 16;
            title.alignment = TextAnchor.MiddleLeft;
            LayoutElement titleLayout = title.GetComponent<LayoutElement>();
            titleLayout.minWidth = 72f;
            titleLayout.minHeight = 32f;
            titleLayout.preferredWidth = 92f;
            titleLayout.preferredHeight = 36f;
            titleLayout.flexibleWidth = 1f;
            titleLayout.flexibleHeight = 0f;

            LayoutElement progressLayout =
                GetOrAddComponent<LayoutElement>(progressArea.gameObject);
            progressLayout.minWidth = 208f;
            progressLayout.minHeight = 36f;
            progressLayout.preferredWidth = 230f;
            progressLayout.preferredHeight = 40f;
            progressLayout.flexibleWidth = 0f;
            progressLayout.flexibleHeight = 0f;
            HorizontalLayoutGroup progressRow =
                GetOrAddComponent<HorizontalLayoutGroup>(progressArea.gameObject);
            progressRow.padding = new RectOffset(0, 0, 0, 0);
            progressRow.spacing = 8f;
            progressRow.childAlignment = TextAnchor.MiddleCenter;
            progressRow.childControlWidth = true;
            progressRow.childControlHeight = true;
            progressRow.childForceExpandWidth = false;
            progressRow.childForceExpandHeight = false;
            LayoutElement percentageLayout =
                percentage.GetComponent<LayoutElement>();
            percentageLayout.minWidth = 104f;
            percentageLayout.minHeight = 32f;
            percentageLayout.preferredWidth = 112f;
            percentageLayout.preferredHeight = 36f;
            percentageLayout.flexibleWidth = 0f;
            percentageLayout.flexibleHeight = 0f;
            LayoutElement targetLayout = target.GetComponent<LayoutElement>();
            targetLayout.minWidth = 80f;
            targetLayout.minHeight = 32f;
            targetLayout.preferredWidth = 88f;
            targetLayout.preferredHeight = 36f;
            targetLayout.flexibleWidth = 0f;
            targetLayout.flexibleHeight = 0f;

            RectTransform blocker = (RectTransform)RequireChild(
                topHud,
                "HudBlockerButton");
            LayoutElement blockerLayout = blocker.GetComponent<LayoutElement>();
            blockerLayout.minWidth = 72f;
            blockerLayout.minHeight = 32f;
            blockerLayout.preferredWidth = 88f;
            blockerLayout.preferredHeight = 36f;
            blockerLayout.flexibleWidth = 0f;
            blockerLayout.flexibleHeight = 0f;
            Text blockerLabel = RequireChild(blocker, "Label").GetComponent<Text>();
            blockerLabel.text = "UI TEST";
            blockerLabel.fontSize = 12;

            title.rectTransform.SetSiblingIndex(0);
            progressArea.SetSiblingIndex(1);
            blocker.SetSiblingIndex(2);

            // BoardStage is the stable, flexible layout slot; BoardViewport
            // (inside it) is resized every frame by BoardCameraFitter to
            // exactly the fitted rect and stays ignoreLayout, so it no
            // longer takes a preferred/flexible height of its own here.
            RectTransform boardStage = (RectTransform)RequireChild(
                safeArea,
                "BoardStage");
            LayoutElement boardLayout =
                boardStage.GetComponent<LayoutElement>();
            boardLayout.minHeight = 320f;
            boardLayout.preferredHeight = 0f;
            boardLayout.flexibleHeight = 1f;
            boardLayout.flexibleWidth = 1f;
            Text boardLabel = RequireChild(
                boardStage,
                "BoardViewport/BoardFrame/BoardLabel").GetComponent<Text>();
            boardLabel.text = string.Empty;
            boardLabel.gameObject.SetActive(false);

            RectTransform bottomHud = (RectTransform)RequireChild(
                safeArea,
                "BottomHUD");
            LayoutElement bottomLayout =
                bottomHud.GetComponent<LayoutElement>();
            bottomLayout.minHeight = 28f;
            bottomLayout.preferredHeight = 32f;
            bottomLayout.flexibleHeight = 0f;
            VerticalLayoutGroup bottomColumn =
                bottomHud.GetComponent<VerticalLayoutGroup>();
            bottomColumn.padding = new RectOffset(6, 6, 2, 2);
            bottomColumn.spacing = 0f;
            bottomColumn.childControlHeight = true;
            bottomColumn.childForceExpandHeight = false;

            Text pointerStatus = RequireChild(
                bottomHud,
                "PointerStatus").GetComponent<Text>();
            pointerStatus.fontSize = 10;
            LayoutElement pointerLayout =
                pointerStatus.GetComponent<LayoutElement>();
            pointerLayout.minHeight = 14f;
            pointerLayout.preferredHeight = 14f;
            pointerLayout.flexibleHeight = 0f;
            Text mappingStatus = RequireChild(
                bottomHud,
                "MappingStatus").GetComponent<Text>();
            mappingStatus.fontSize = 10;
            LayoutElement mappingLayout =
                mappingStatus.GetComponent<LayoutElement>();
            mappingLayout.minHeight = 14f;
            mappingLayout.preferredHeight = 14f;
            mappingLayout.flexibleHeight = 0f;

            EditorUtility.SetDirty(safeAreaLayout);
            EditorUtility.SetDirty(topLayout);
            EditorUtility.SetDirty(topRow);
            EditorUtility.SetDirty(title);
            EditorUtility.SetDirty(titleLayout);
            EditorUtility.SetDirty(progressLayout);
            EditorUtility.SetDirty(progressRow);
            EditorUtility.SetDirty(percentageLayout);
            EditorUtility.SetDirty(targetLayout);
            EditorUtility.SetDirty(blockerLayout);
            EditorUtility.SetDirty(blockerLabel);
            EditorUtility.SetDirty(boardLayout);
            EditorUtility.SetDirty(boardLabel);
            EditorUtility.SetDirty(bottomLayout);
            EditorUtility.SetDirty(bottomColumn);
            EditorUtility.SetDirty(pointerStatus);
            EditorUtility.SetDirty(pointerLayout);
            EditorUtility.SetDirty(mappingStatus);
            EditorUtility.SetDirty(mappingLayout);
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
                || hud.CompletionCanvasGroup == null
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

            CanvasGroup completionGroup = hud.CompletionCanvasGroup;
            LayoutElement overlayLayout =
                hud.CompleteOverlay.GetComponent<LayoutElement>();
            if (!hud.CompleteOverlay.activeSelf
                || overlayLayout == null
                || !overlayLayout.ignoreLayout
                || hud.CompleteOverlay.transform.GetSiblingIndex()
                    != hud.CompleteOverlay.transform.parent.childCount - 1
                || !Mathf.Approximately(completionGroup.alpha, 0f)
                || completionGroup.interactable
                || completionGroup.blocksRaycasts)
            {
                throw new InvalidOperationException(
                    "The completion overlay must be active, topmost, ignored " +
                    "by layout, and CanvasGroup-hidden before completion.");
            }

            Transform safeArea = hud.CompleteOverlay.transform.parent;
            Transform topHud = RequireChild(safeArea, "TopHUD");
            Transform boardStage = RequireChild(safeArea, "BoardStage");
            Transform bottomHud = RequireChild(safeArea, "BottomHUD");
            Transform progressArea = RequireChild(topHud, "ProgressArea");
            Transform blocker = RequireChild(topHud, "HudBlockerButton");
            VerticalLayoutGroup safeLayout =
                safeArea.GetComponent<VerticalLayoutGroup>();
            HorizontalLayoutGroup topRow =
                topHud.GetComponent<HorizontalLayoutGroup>();
            VerticalLayoutGroup bottomColumn =
                bottomHud.GetComponent<VerticalLayoutGroup>();
            LayoutElement topLayout = topHud.GetComponent<LayoutElement>();
            LayoutElement boardLayout =
                boardStage.GetComponent<LayoutElement>();
            LayoutElement bottomLayout =
                bottomHud.GetComponent<LayoutElement>();
            LayoutElement blockerLayout = blocker.GetComponent<LayoutElement>();
            if (!safeLayout.childControlHeight
                || safeLayout.childForceExpandHeight
                || !topRow.childControlHeight
                || topRow.childForceExpandHeight
                || topRow.childForceExpandWidth
                || !bottomColumn.childControlHeight
                || bottomColumn.childForceExpandHeight
                || !Mathf.Approximately(topLayout.minHeight, 52f)
                || !Mathf.Approximately(topLayout.preferredHeight, 60f)
                || !Mathf.Approximately(topLayout.flexibleHeight, 0f)
                || !Mathf.Approximately(boardLayout.preferredHeight, 0f)
                || !Mathf.Approximately(boardLayout.flexibleHeight, 1f)
                || !Mathf.Approximately(bottomLayout.minHeight, 28f)
                || !Mathf.Approximately(bottomLayout.preferredHeight, 32f)
                || !Mathf.Approximately(bottomLayout.flexibleHeight, 0f)
                || blockerLayout.preferredWidth > 100f
                || blockerLayout.preferredHeight > 40f
                || blockerLayout.flexibleWidth > 0f
                || blockerLayout.flexibleHeight > 0f
                || hud.PercentageText.transform.parent != progressArea
                || hud.TargetText.transform.parent != progressArea)
            {
                throw new InvalidOperationException(
                    "The responsive layout must use fixed non-expanding HUD " +
                    "bands, a flexible board, a grouped progress area, and a " +
                    "bounded non-stretching UI blocker.");
            }

            Transform[] fixedHeightGroups =
            {
                topHud,
                progressArea,
                bottomHud,
            };
            for (int groupIndex = 0;
                 groupIndex < fixedHeightGroups.Length;
                 groupIndex++)
            {
                Transform group = fixedHeightGroups[groupIndex];
                for (int childIndex = 0;
                     childIndex < group.childCount;
                     childIndex++)
                {
                    LayoutElement childLayout = group.GetChild(childIndex)
                        .GetComponent<LayoutElement>();
                    if (childLayout == null || childLayout.flexibleHeight > 0f)
                    {
                        throw new InvalidOperationException(
                            $"'{group.name}/{group.GetChild(childIndex).name}' " +
                            "requires an explicit non-flexible LayoutElement.");
                    }
                }
            }

            Transform[] fittedObjects =
            {
                safeArea,
                topHud,
                progressArea,
                blocker,
                boardStage,
                bottomHud,
            };
            for (int index = 0; index < fittedObjects.Length; index++)
            {
                GameObject candidate = fittedObjects[index].gameObject;
                if (candidate.GetComponent<ContentSizeFitter>() != null
                    || candidate.GetComponent<AspectRatioFitter>() != null)
                {
                    throw new InvalidOperationException(
                        $"'{candidate.name}' has a fitter that conflicts with " +
                        "the authoritative parent layout.");
                }
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

        private static RectTransform GetOrMoveUiChild(
            Transform legacyParent,
            Transform desiredParent,
            string name)
        {
            Transform existing = desiredParent.Find(name);
            if (existing == null)
            {
                existing = legacyParent.Find(name);
                if (existing != null)
                {
                    existing.SetParent(desiredParent, false);
                }
            }

            if (existing == null)
            {
                return GetOrCreateUiChild(desiredParent, name);
            }

            if (existing is RectTransform existingRect)
            {
                return existingRect;
            }

            throw new InvalidOperationException(
                $"Existing UI child '{name}' is not a RectTransform.");
        }

        private static T GetOrAddComponent<T>(GameObject gameObject)
            where T : Component
        {
            T component = gameObject.GetComponent<T>();
            return component != null ? component : gameObject.AddComponent<T>();
        }
    }
}
