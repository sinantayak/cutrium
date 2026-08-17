using System;
using System.Linq;
using Cutrium.Gameplay.Session;
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
    public static class Milestone3SceneSetup
    {
        [MenuItem("Cutrium/Setup/Milestone 3 Core-Fun Build")]
        public static void Apply()
        {
            VerifyBaseline();
            Milestone2SceneSetup.Apply();
            Scene scene = EditorSceneManager.OpenScene(
                Milestone2SceneSetup.VerticalSliceScenePath,
                OpenSceneMode.Single);
            Configure(scene);
            Validate(scene);
            if (!EditorSceneManager.SaveScene(
                    scene,
                    Milestone2SceneSetup.VerticalSliceScenePath))
            {
                throw new InvalidOperationException(
                    "Unity could not save the Milestone 3 scene.");
            }

            AssetDatabase.SaveAssets();
            Debug.Log(
                "Milestone 3 scene setup verified. Three serialized levels, " +
                "persistent Retry/Next flow, and core-fun HUD are ready.");
        }

        private static void VerifyBaseline()
        {
            if (!string.Equals(
                    Application.unityVersion,
                    "6000.3.21f1",
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "Milestone 3 requires Unity 6000.3.21f1.");
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
                || !string.Equals(
                    package.version,
                    version,
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Expected '{path}' at '{version}', found " +
                    $"'{package?.version ?? "missing"}'.");
            }
        }

        private static void Configure(Scene scene)
        {
            GameObject root = RequireRoot(scene, "VerticalSliceRoot");
            Transform safeArea = RequireChild(
                root.transform,
                "Canvas/SafeAreaRoot");
            Transform topHud = RequireChild(safeArea, "TopHUD");
            FirstPlayableController controller = root
                .GetComponentInChildren<FirstPlayableController>(true);
            controller.ConfigureLevelsForSetup(
                CoreFunLevelDefinition.CreateMilestone3Defaults());

            RectTransform levelRect = GetOrCreateUiChild(
                topHud,
                "LevelNumber");
            Text levelText = ConfigureText(
                levelRect,
                "LEVEL 1",
                14,
                TextAnchor.MiddleLeft);
            ConfigureLayout(levelRect, 68f, 36f, 76f, 36f);

            Transform title = RequireChild(topHud, "Title");
            Text purposeText = title.GetComponent<Text>();
            purposeText.text = "LEARN THE CUT";
            purposeText.fontSize = 14;
            purposeText.resizeTextForBestFit = true;
            purposeText.resizeTextMinSize = 9;
            purposeText.resizeTextMaxSize = 14;
            purposeText.horizontalOverflow = HorizontalWrapMode.Wrap;
            purposeText.verticalOverflow = VerticalWrapMode.Truncate;
            Transform progress = RequireChild(topHud, "ProgressArea");
            Transform blocker = RequireChild(topHud, "HudBlockerButton");
            title.SetSiblingIndex(0);
            levelRect.SetSiblingIndex(1);
            progress.SetSiblingIndex(2);
            blocker.SetSiblingIndex(3);

            RectTransform overlay = (RectTransform)RequireChild(
                safeArea,
                "LevelCompleteOverlay");
            overlay.SetAsLastSibling();
            RectTransform completeTextRect = (RectTransform)RequireChild(
                overlay,
                "CompleteText");
            completeTextRect.anchorMin = new Vector2(0.12f, 0.56f);
            completeTextRect.anchorMax = new Vector2(0.88f, 0.76f);
            completeTextRect.offsetMin = Vector2.zero;
            completeTextRect.offsetMax = Vector2.zero;
            Text completeText = completeTextRect.GetComponent<Text>();
            completeText.fontSize = 30;

            RectTransform retryRect = (RectTransform)RequireChild(
                overlay,
                "RetryButton");
            ConfigureOverlayButtonRect(
                retryRect,
                new Vector2(0.16f, 0.37f),
                new Vector2(0.48f, 0.49f));
            Text retryLabel = RequireChild(retryRect, "Label")
                .GetComponent<Text>();
            retryLabel.fontSize = 20;

            RectTransform nextRect = GetOrCreateUiChild(
                overlay,
                "NextButton");
            ConfigureOverlayButtonRect(
                nextRect,
                new Vector2(0.52f, 0.37f),
                new Vector2(0.84f, 0.49f));
            Image nextImage = GetOrAddComponent<Image>(nextRect.gameObject);
            nextImage.color = new Color(0.22f, 0.72f, 0.68f, 1f);
            Button nextButton = GetOrAddComponent<Button>(nextRect.gameObject);
            nextButton.targetGraphic = nextImage;
            RectTransform nextLabelRect = GetOrCreateUiChild(nextRect, "Label");
            StretchToParent(nextLabelRect);
            Text nextLabel = ConfigureText(
                nextLabelRect,
                "NEXT",
                20,
                TextAnchor.MiddleCenter);

            CaptureHudPresenter hud = root
                .GetComponentInChildren<CaptureHudPresenter>(true);
            hud.Configure(
                controller,
                levelText,
                purposeText,
                hud.PercentageText,
                hud.TargetText,
                hud.CompleteOverlay,
                hud.CompletionCanvasGroup,
                completeText,
                hud.RetryButton,
                nextButton,
                nextLabel);

            EditorUtility.SetDirty(controller);
            EditorUtility.SetDirty(levelText);
            EditorUtility.SetDirty(purposeText);
            EditorUtility.SetDirty(completeText);
            EditorUtility.SetDirty(retryLabel);
            EditorUtility.SetDirty(nextImage);
            EditorUtility.SetDirty(nextButton);
            EditorUtility.SetDirty(nextLabel);
            EditorUtility.SetDirty(hud);
        }

        private static void Validate(Scene scene)
        {
            GameObject root = RequireRoot(scene, "VerticalSliceRoot");
            FirstPlayableController[] controllers = root
                .GetComponentsInChildren<FirstPlayableController>(true);
            CaptureHudPresenter[] presenters = root
                .GetComponentsInChildren<CaptureHudPresenter>(true);
            if (controllers.Length != 1 || presenters.Length != 1)
            {
                throw new InvalidOperationException(
                    "Milestone 3 requires one controller and one HUD presenter.");
            }

            FirstPlayableController controller = controllers[0];
            CaptureHudPresenter hud = presenters[0];
            if (controller.LevelDefinitions.Count != 3)
            {
                throw new InvalidOperationException(
                    "Exactly three core-fun levels must be serialized.");
            }

            var runtime = controller.LevelDefinitions
                .Select(definition => definition.ToRuntimeConfiguration())
                .ToArray();
            var catalog = new CoreFunLevelCatalog(runtime);
            float[] targets = { 0.825f, 0.85f, 0.9f };
            float[] speeds = { 1.6f, 3.1f, 2.7f };
            float[] growthSpeeds = { 3f, 2.4f, 2.8f };
            float[] cutMargins = { 3f, 2.5f, 1.8f };
            int[] threatCounts = { 1, 1, 2 };
            string[] purposeLines =
            {
                "LEARN THE CUT",
                "WATCH THE THREAT",
                "KEEP THEM TOGETHER",
            };
            for (int index = 0; index < catalog.Count; index++)
            {
                CoreFunLevelConfiguration level = catalog[index];
                if (level.ThreatMotion.BoardBounds
                        != CoreFunLevelConfiguration.FixedBoardBounds
                    || !Mathf.Approximately(
                        level.Capture.TargetCapturedFraction,
                        targets[index])
                    || !Mathf.Approximately(
                        level.ThreatMotion.Speed,
                        speeds[index])
                    || !Mathf.Approximately(
                        level.Barrier.GrowthSpeed,
                        growthSpeeds[index])
                    || !Mathf.Approximately(
                        level.Barrier.MinimumEdgeMargin,
                        cutMargins[index])
                    || level.ThreatMotions.Count != threatCounts[index]
                    || !string.Equals(
                        level.PurposeLine,
                        purposeLines[index],
                        StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        $"Level {index + 1} tuning differs from Milestone 3.");
                }
            }

            if (!Mathf.Approximately(
                    catalog[2].ThreatMotions[1].Speed,
                    2.9f))
            {
                throw new InvalidOperationException(
                    "Level 3 second-threat tuning differs from Milestone 3.");
            }

            if (hud.Controller != controller
                || hud.LevelText == null
                || hud.PurposeText == null
                || hud.PercentageText == null
                || hud.TargetText == null
                || hud.CompleteOverlay == null
                || hud.CompletionCanvasGroup == null
                || hud.RetryButton == null
                || hud.NextButton == null
                || hud.NextButtonLabel == null
                || !hud.CompleteOverlay.activeSelf
                || !hud.CompleteOverlay.GetComponent<LayoutElement>().ignoreLayout
                || hud.CompleteOverlay.transform.GetSiblingIndex()
                    != hud.CompleteOverlay.transform.parent.childCount - 1)
            {
                throw new InvalidOperationException(
                    "Milestone 3 HUD flow has missing or invalid references.");
            }
        }

        private static void ConfigureOverlayButtonRect(
            RectTransform rect,
            Vector2 anchorMin,
            Vector2 anchorMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static Text ConfigureText(
            RectTransform rect,
            string value,
            int fontSize,
            TextAnchor alignment)
        {
            Text text = GetOrAddComponent<Text>(rect.gameObject);
            text.font =
                LandmarkRevealPresentationSetup.LoadLegacyUiFontForSetup();
            text.text = value;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = Color.white;
            text.raycastTarget = false;
            return text;
        }

        private static void ConfigureLayout(
            RectTransform rect,
            float minimumWidth,
            float minimumHeight,
            float preferredWidth,
            float preferredHeight)
        {
            LayoutElement layout = GetOrAddComponent<LayoutElement>(rect.gameObject);
            layout.minWidth = minimumWidth;
            layout.minHeight = minimumHeight;
            layout.preferredWidth = preferredWidth;
            layout.preferredHeight = preferredHeight;
            layout.flexibleWidth = 0f;
            layout.flexibleHeight = 0f;
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
            return root ?? throw new InvalidOperationException(
                $"Scene '{scene.path}' requires root '{name}'.");
        }

        private static Transform RequireChild(Transform parent, string path)
        {
            Transform child = parent.Find(path);
            return child ?? throw new InvalidOperationException(
                $"Scene requires '{parent.name}/{path}'.");
        }

        private static RectTransform GetOrCreateUiChild(
            Transform parent,
            string name)
        {
            Transform existing = parent.Find(name);
            if (existing is RectTransform existingRect)
            {
                return existingRect;
            }

            if (existing != null)
            {
                throw new InvalidOperationException(
                    $"Existing UI child '{name}' is not a RectTransform.");
            }

            var gameObject = new GameObject(
                name,
                typeof(RectTransform),
                typeof(CanvasRenderer));
            var rect = (RectTransform)gameObject.transform;
            rect.SetParent(parent, false);
            return rect;
        }

        private static T GetOrAddComponent<T>(GameObject gameObject)
            where T : Component
        {
            T component = gameObject.GetComponent<T>();
            return component != null ? component : gameObject.AddComponent<T>();
        }
    }
}
