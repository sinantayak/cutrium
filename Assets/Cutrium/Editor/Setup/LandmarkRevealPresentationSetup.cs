using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Cutrium.Presentation.Barriers;
using Cutrium.Presentation.Capture;
using Cutrium.Presentation.Economy;
using Cutrium.Presentation.Feedback;
using Cutrium.Presentation.HUD;
using Cutrium.Presentation.Landmark;
using Cutrium.Presentation.Powers;
using Cutrium.Presentation.Theme;
using Cutrium.Presentation.Threats;
using Cutrium.Unity.Layout;
using Cutrium.Unity.Services;
using Cutrium.Unity.Simulation;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace Cutrium.Editor.Setup
{
    /// Presentation-only pass that prepares Cutrium for a landmark-reveal
    /// identity: calmer board/barrier/threat visuals, a compact power row
    /// integrated into the bottom HUD, a full-screen opaque completion
    /// reward screen with a fixed-aspect framed hero photo, and a
    /// data-driven LandmarkRevealPresenter that covers active area with
    /// sand and reveals landmark artwork as it is captured, sending a
    /// cosmetic sand stream to the target-progress bar in BottomHUD. The
    /// progress value is presentation-only and does not change gameplay.
    /// See ADR-026 and ADR-027.
    public static class LandmarkRevealPresentationSetup
    {
        // Long enough to cover the itemized stats reveal, then the reward's
        // own reveal-count-up-then-flight-then-hold sequence (reveal delay
        // + count-up + flight-delay + staggered flight + post-arrival hold
        // + settle -- see CompletionRewardRevealDelaySeconds and
        // LevelCoinRewardPresenter's own tuning) so nothing auto-hides
        // early.
        private const float CompletionSummarySeconds = 4.8f;
        private const float CompletionOverlayFadeSeconds = 0.45f;
        // The reward row stays hidden until every stats row above it has
        // finished popping in (5 rows * FeedbackPresenter's own stagger,
        // plus a short buffer) so it reads as the last item revealed in
        // that same sequence, not something popping in alongside them.
        private const float CompletionRewardRevealDelaySeconds = 1.8f;
        public const string GeneratedFolder =
            "Assets/Cutrium/Art/Generated/Landmark";
        public const string LandmarkContentFolder =
            "Assets/Cutrium/Content/Landmarks";
        public const string GalataArtworkFolder =
            LandmarkContentFolder + "/Artwork";
        public const string CleanupThemePath =
            Milestone5SceneSetup.CleanupThemePath;
        public const string ProgressBackgroundPath =
            "Assets/Cutrium/Content/Gui/ProgressBackground.png";
        public const string ProgressFillPath =
            "Assets/Cutrium/Content/Gui/ProgressFill.png";
        public const string NormalThreatVisualPath =
            "Assets/Cutrium/Content/Gui/Threat_Visual_Normal.png";
        public const string HunterThreatVisualPath =
            "Assets/Cutrium/Content/Gui/Threat_Visual_Hunter.png";
        public const string PulseThreatVisualPath =
            "Assets/Cutrium/Content/Gui/Threat_Visual_Pulse.png";
        // BigHUDBackground is now the single full-width TopHUD bar; the
        // earlier per-value SmallHUDBackground plaques are retired.
        public const string BigHudBackgroundPath =
            "Assets/Cutrium/Content/Gui/BigHUDBackground.png";
        public const string HealthIconPath =
            "Assets/Cutrium/Content/Gui/HealthIcon.png";
        public const string SpeedIconPath =
            "Assets/Cutrium/Content/Gui/SpeedIcon.png";
        // Four speedometer stages (slowest to fastest barrier growth) that
        // GameplayIdentityHudPresenter swaps between live at runtime, based
        // on where the current level's BarrierGrowthSpeed falls between the
        // catalog's own floor and ceiling -- see ConfigureIdentityHud.
        public const string SpeedIconL1Path =
            "Assets/Cutrium/Content/Gui/SpeedIconL1.png";
        public const string SpeedIconL2Path =
            "Assets/Cutrium/Content/Gui/SpeedIconL2.png";
        public const string SpeedIconL3Path =
            "Assets/Cutrium/Content/Gui/SpeedIconL3.png";
        public const string SpeedIconL4Path =
            "Assets/Cutrium/Content/Gui/SpeedIconL4.png";
        public const string SettingsButtonPath =
            "Assets/Cutrium/Content/Gui/Settings_Button.png";
        public const string CoinStackL1Path =
            "Assets/Cutrium/Content/Gui/CoinStackL1.png";
        public const string FreezeSkillPath =
            "Assets/Cutrium/Content/Gui/FreezeSkill.png";
        public const string InstantBarrierSkillPath =
            "Assets/Cutrium/Content/Gui/InstantBarrierSkill.png";
        public const string GravityWellSkillPath =
            "Assets/Cutrium/Content/Gui/GravityWellSkill.png";
        public const string GravityWellVortexPath =
            "Assets/Cutrium/Content/Gui/Vortex.png";
        public const string GeneralButtonBackgroundPath =
            "Assets/Cutrium/Content/Gui/GeneralButtonBackground.png";
        public const string TopHudFontPath =
            "Assets/Cutrium/Art/Fonts/LapsusPro-Bold SDF.asset";
        public const string CompletionFontPath =
            "Assets/Cutrium/Art/Fonts/LapsusPro-Bold.otf";

        private const float BarrierVisualLogicalThickness = 0.13f;
        private const float RevealFadeSeconds = 0.35f;
        private const float TopHudMinimumHeight = 146f;
        private const float TopHudPreferredHeight = 150f;
        private const int SafeAreaHorizontalPadding = 12;
        private const int SafeAreaVerticalPadding = 10;
        private const float SafeAreaSectionSpacing = 12f;
        private const float BottomHudMinimumHeight = 112f;
        private const float BottomHudPreferredHeight = 116f;
        private const int BottomHudPadding = 8;
        private const float TopHudBarHeight = 84f;
        private const float TopHudSettingsSize = 48f;
        private const float TopHudIconSizeMultiplier = 0.74f;
        private const float SkillCellSize = 100f;
        private const float SkillRowRightInset = 18f;
        private static readonly Color DarkBrownBackground =
            new Color(0.12f, 0.07f, 0.045f, 1f);
        private static readonly Color GrowingBarrierBrown =
            new Color(0.38f, 0.15f, 0.055f, 1f);
        private static readonly Color BarrierPreviewBrown =
            new Color(0.38f, 0.15f, 0.055f, 0.9f);
        private static readonly Color NormalThreatTrailTint =
            new Color(0.12f, 0.48f, 1f, 0.86f);
        private static readonly Color HunterThreatTrailTint =
            new Color(1f, 0.16f, 0.22f, 0.86f);
        private static readonly Color PulseThreatTrailTint =
            new Color(0.12f, 0.9f, 0.28f, 0.86f);
        private static readonly Color TopHudTextBrown =
            new Color(0.34f, 0.105f, 0.025f, 1f);
        private static readonly Color GravityTargetingHighlight =
            new Color(1f, 0.87f, 0.35f, 0.95f);
        private static readonly Color CompletionBackgroundBrown =
            new Color(0.12156863f, 0.07f, 0.043137256f, 1f);
        // Outline for a brown-filled Coin balance label (frontend/shop,
        // over lighter backgrounds) -- the default white-fill/brown-outline
        // pairing used in gameplay's dark TopHUD would disappear if simply
        // inverted, so a brown fill gets a light cream outline instead.
        private static readonly Color CoinBalanceBrownOutline =
            new Color(1f, 0.9f, 0.78f, 1f);

        [MenuItem("Cutrium/Setup/Landmark Reveal Presentation Pass")]
        public static void Apply()
        {
            VerifyBaseline();
            Milestone6SceneSetup.Apply();

            EnsureFolders();
            Dictionary<string, Sprite> sprites = GenerateSprites();
            ThemeDefinition cleanup = LoadTheme(CleanupThemePath);
            ConfigureCleanupTheme(cleanup, sprites);
            LandmarkDefinition[] landmarks = ConfigureLandmarks(sprites);

            // Sand/bowl reveal (ADR-026): EnsureSandTexture only generates
            // when the PNG is missing. An existing file is user-authored and
            // must be imported as-is; setup never rewrites its bytes.
            Sprite sandSprite = SandTextureGenerator.EnsureSandTexture();
            Sprite bowlOutlineSprite = BowlSpriteGenerator.EnsureBowlOutline();
            Sprite bowlInteriorMaskSprite =
                BowlSpriteGenerator.EnsureBowlInteriorMask();
            cleanup.ConfigureSandBowlForSetup(
                sandSprite,
                bowlOutlineSprite,
                bowlInteriorMaskSprite);
            EditorUtility.SetDirty(cleanup);
            AssetDatabase.SaveAssets();

            Scene scene = EditorSceneManager.OpenScene(
                Milestone2SceneSetup.VerticalSliceScenePath,
                OpenSceneMode.Single);
            // Opening a scene unloads unused native assets; reacquire every
            // imported asset afterward so setup and validation never depend
            // on stale UnityEngine.Object wrappers (see ADR-018).
            sprites = ReloadGeneratedSprites(sprites.Keys);
            cleanup = LoadTheme(CleanupThemePath);
            landmarks = ReloadLandmarks();
            ProgressSprites progressSprites = LoadProgressSprites();
            Configure(scene, sprites, cleanup, landmarks, progressSprites);
            Validate(scene, landmarks);
            if (!EditorSceneManager.SaveScene(
                    scene,
                    Milestone2SceneSetup.VerticalSliceScenePath))
            {
                throw new InvalidOperationException(
                    "Unity could not save the landmark reveal scene.");
            }

            AssetDatabase.SaveAssets();
            Debug.Log(
                "Landmark Reveal Presentation Pass verified. Normal play " +
                "shows the gameplay TopHUD, unchanged logical 10x16 board, " +
                "and sand-fed target-progress bar; the full-screen landmark " +
                "completion flow remains wired and ready.");
        }

        public static void ConfigureChapterTwoPresentationForSetup(
            GameObject root)
        {
            if (root == null)
            {
                throw new ArgumentNullException(nameof(root));
            }

            FirstPlayableController controller = root
                .GetComponentInChildren<FirstPlayableController>(true);
            PowerHudPresenter powerHud = root
                .GetComponentInChildren<PowerHudPresenter>(true);
            LandmarkRevealPresenter landmarkPresenter = root
                .GetComponentInChildren<LandmarkRevealPresenter>(true);
            if (controller == null
                || powerHud == null
                || landmarkPresenter == null)
            {
                throw new InvalidOperationException(
                    "Chapter 2 presentation requires the gameplay " +
                    "controller, PowerHudPresenter, and " +
                    "LandmarkRevealPresenter.");
            }

            Transform safeArea = RequireChild(
                root.transform,
                "Canvas/SafeAreaRoot");
            Transform completion = RequireChild(
                safeArea,
                "LevelCompleteOverlay");
            RectTransform boardFrame = (RectTransform)RequireChild(
                safeArea,
                "BoardStage/BoardViewport/BoardFrame");
            SkillAssets skillAssets = LoadSkillAssets();
            ConfigureBottomHudSkillRow(
                safeArea,
                controller,
                powerHud,
                skillAssets);
            ConfigureGravityWellCue(
                boardFrame,
                controller,
                LoadUiSpriteForSetup(GravityWellVortexPath));
            ConfigureCompletionRewardFlowForSetup(
                root,
                landmarkPresenter);
            ApplyCompletionReadability(completion, LoadCompletionFont());
            ConfigureGeneralActionButtonLayoutForSetup(safeArea);
            ApplyGeneralButtonStylesForSetup(root);
            ConfigureFeedbackReadabilityForSetup(safeArea);
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(
                (RectTransform)safeArea);

            if (powerHud.GravityWellButton == null
                || powerHud.GravityWellChargesText == null)
            {
                throw new InvalidOperationException(
                    "Chapter 2 Gravity Well presentation did not wire " +
                    "all required references.");
            }
            ValidateGravityWellVortex(root);

            ValidateGeneralButtonStyle(
                RequireChild(completion, "RetryButton").GetComponent<Button>());
            ValidateGeneralButtonStyle(
                RequireChild(completion, "NextButton").GetComponent<Button>());
        }

        [MenuItem("Cutrium/Setup/Apply Gravity Well Visuals Only")]
        public static void ApplyGravityWellVisualsOnly()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException(
                    "Exit Play Mode before updating the Gravity Well cue.");
            }

            Scene scene = SceneManager.GetActiveScene();
            if (scene.path != Milestone2SceneSetup.VerticalSliceScenePath)
            {
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                {
                    throw new OperationCanceledException(
                        "Gravity Well cue setup cancelled before opening " +
                        "VerticalSlice.unity.");
                }

                scene = EditorSceneManager.OpenScene(
                    Milestone2SceneSetup.VerticalSliceScenePath,
                    OpenSceneMode.Single);
            }

            GameObject root = RequireRoot(scene, "VerticalSliceRoot");
            FirstPlayableController controller = root
                .GetComponentInChildren<FirstPlayableController>(true);
            PowerHudPresenter powerHud = root
                .GetComponentInChildren<PowerHudPresenter>(true);
            Transform safeArea = RequireChild(
                root.transform,
                "Canvas/SafeAreaRoot");
            RectTransform boardFrame = (RectTransform)RequireChild(
                root.transform,
                "Canvas/SafeAreaRoot/BoardStage/BoardViewport/BoardFrame");
            if (controller == null || powerHud == null)
            {
                throw new InvalidOperationException(
                    "Gravity Well visuals require FirstPlayableController " +
                    "and PowerHudPresenter.");
            }

            ConfigureBottomHudSkillRow(
                safeArea,
                controller,
                powerHud,
                LoadSkillAssets());
            ConfigureGravityWellCue(
                boardFrame,
                controller,
                LoadUiSpriteForSetup(GravityWellVortexPath));
            ValidateBottomHudSkillRow((RectTransform)RequireChild(
                safeArea,
                "BottomHUD/BottomHudRow/SkillRow"));
            ValidateGravityWellVortex(root);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(
                    scene,
                    Milestone2SceneSetup.VerticalSliceScenePath))
            {
                throw new InvalidOperationException(
                    "Unity could not save the Gravity Well Vortex cue.");
            }

            AssetDatabase.SaveAssets();
            Debug.Log(
                "Gravity Well now uses the radius-sized Vortex effect and " +
                "a HUD-yellow targeting highlight without the legacy " +
                "center icon or range ring.");
        }

        public static void ApplyGeneralButtonStyleForSetup(Button button)
        {
            if (button == null)
            {
                return;
            }

            EnsureUiSpriteImportSettings(
                GeneralButtonBackgroundPath,
                sliced9Slice: true);
            ApplyGeneralButtonStyle(
                button,
                LoadSingleSprite(GeneralButtonBackgroundPath));
        }

        public static void ApplyGeneralButtonStylesForSetup(GameObject root)
        {
            EnsureUiSpriteImportSettings(
                GeneralButtonBackgroundPath,
                sliced9Slice: true);
            Sprite background = LoadSingleSprite(GeneralButtonBackgroundPath);
            Button[] buttons = root.GetComponentsInChildren<Button>(true);
            for (int index = 0; index < buttons.Length; index++)
            {
                Button button = buttons[index];
                if (IsIconOnlyButton(button))
                {
                    continue;
                }

                bool hasText = button.GetComponentInChildren<Text>(true) != null
                    || button.GetComponentInChildren<TMP_Text>(true) != null;
                if (hasText)
                {
                    ApplyGeneralButtonStyle(button, background);
                }
            }
        }

        [MenuItem("Cutrium/Setup/Apply Barrier Preview Only")]
        public static void ApplyBarrierPreviewOnly()
        {
            ThemeDefinition theme = LoadTheme(CleanupThemePath);
            var serializedTheme = new SerializedObject(theme);
            SerializedProperty previewColor = serializedTheme.FindProperty(
                "_barrierPreviewColor");
            if (previewColor == null)
            {
                throw new InvalidOperationException(
                    "Cleanup theme has no serialized barrier preview color.");
            }

            previewColor.colorValue = BarrierPreviewBrown;
            serializedTheme.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(theme);
            AssetDatabase.SaveAssets();
            Debug.Log(
                "Barrier preview updated without changing other theme values.");
        }

        [MenuItem("Cutrium/Setup/Apply Threat Visual Variants Only")]
        public static void ApplyThreatVisualVariantsOnly()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException(
                    "Exit Play Mode before updating threat visuals.");
            }

            ThemeDefinition theme = LoadTheme(CleanupThemePath);
            ConfigureThreatVisualVariants(theme);
            ValidateThreatVisualVariants(theme);
            EditorUtility.SetDirty(theme);
            AssetDatabase.SaveAssets();
            Debug.Log(
                "Normal, Hunter, and Pulse threat visuals and shared-trail " +
                "tints configured " +
                "without changing gameplay or scene geometry.");
        }

        [MenuItem("Cutrium/Setup/Apply Completion Popup Readability Only")]
        public static void ApplyCompletionPopupReadabilityOnly()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException(
                    "Exit Play Mode before updating the completion popup.");
            }

            Scene scene = SceneManager.GetActiveScene();
            if (scene.path != Milestone2SceneSetup.VerticalSliceScenePath)
            {
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                {
                    throw new OperationCanceledException(
                        "Completion popup setup cancelled before opening " +
                        "VerticalSlice.unity.");
                }

                scene = EditorSceneManager.OpenScene(
                    Milestone2SceneSetup.VerticalSliceScenePath,
                    OpenSceneMode.Single);
            }

            GameObject root = RequireRoot(scene, "VerticalSliceRoot");
            Transform completionOverlay = RequireChild(
                root.transform,
                "Canvas/SafeAreaRoot/LevelCompleteOverlay");
            Font font = LoadCompletionFont();
            ApplyCompletionReadability(completionOverlay, font);

            LandmarkRevealPresenter landmarkPresenter = root
                .GetComponentInChildren<LandmarkRevealPresenter>(true);
            CaptureHudPresenter hud = root
                .GetComponentInChildren<CaptureHudPresenter>(true);
            if (landmarkPresenter == null || hud == null)
            {
                throw new InvalidOperationException(
                    "Completion popup requires LandmarkRevealPresenter and " +
                    "CaptureHudPresenter.");
            }

            landmarkPresenter.ConfigureCompletionFontForSetup(font);
            hud.ConfigureCompletionRevealGateForSetup(landmarkPresenter);
            Canvas.ForceUpdateCanvases();
            landmarkPresenter.RefreshCompletionLayoutNow();
            EditorUtility.SetDirty(landmarkPresenter);
            EditorUtility.SetDirty(hud);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(
                    scene,
                    Milestone2SceneSetup.VerticalSliceScenePath))
            {
                throw new InvalidOperationException(
                    "Unity could not save the completion popup update.");
            }

            Debug.Log(
                "Completion popup readability updated without changing its " +
                "background, gameplay HUD, board, sand, or theme assets.");
        }

        [MenuItem("Cutrium/Setup/Apply Completion Reward Flow Only")]
        public static void ApplyCompletionRewardFlowOnly()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException(
                    "Exit Play Mode before wiring the completion reward flow.");
            }

            Scene scene = SceneManager.GetActiveScene();
            if (scene.path != Milestone2SceneSetup.VerticalSliceScenePath)
            {
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                {
                    throw new OperationCanceledException(
                        "Completion reward setup cancelled before " +
                        "opening VerticalSlice.unity.");
                }

                scene = EditorSceneManager.OpenScene(
                    Milestone2SceneSetup.VerticalSliceScenePath,
                    OpenSceneMode.Single);
            }

            GameObject root = RequireRoot(scene, "VerticalSliceRoot");
            LandmarkRevealPresenter presenter = root
                .GetComponentInChildren<LandmarkRevealPresenter>(true);
            if (presenter == null)
            {
                throw new InvalidOperationException(
                    "Completion reward flow requires LandmarkRevealPresenter.");
            }

            Transform safeArea = RequireChild(
                root.transform,
                "Canvas/SafeAreaRoot");
            Transform completionOverlay = RequireChild(
                safeArea,
                "LevelCompleteOverlay");
            ConfigureFeedbackReadabilityForSetup(safeArea);
            ConfigureCompletionRewardFlowForSetup(root, presenter);
            ApplyCompletionReadability(
                completionOverlay,
                LoadCompletionFont());
            presenter.RefreshCompletionLayoutNow();
            ValidateCompletionRewardFlow(root, presenter);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(
                    scene,
                    Milestone2SceneSetup.VerticalSliceScenePath))
            {
                throw new InvalidOperationException(
                    "Unity could not save the completion reward-flow wiring.");
            }

            AssetDatabase.SaveAssets();
            Debug.Log(
                "Completion reward flow wired: the configured Coin reward " +
                "is shown on the clean board, flies to the upper-left " +
                "balance HUD, then releases the landmark popup.");
        }

        [MenuItem("Cutrium/Setup/Apply Lapsus-Pro Bold Fonts Only")]
        public static void ApplyLapsusProBoldFontsOnly()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException(
                    "Exit Play Mode before updating UI fonts.");
            }

            Scene scene = SceneManager.GetActiveScene();
            if (scene.path != Milestone2SceneSetup.VerticalSliceScenePath)
            {
                if (!Application.isBatchMode
                    && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                {
                    throw new OperationCanceledException(
                        "Font setup cancelled before opening VerticalSlice.unity.");
                }

                scene = EditorSceneManager.OpenScene(
                    Milestone2SceneSetup.VerticalSliceScenePath,
                    OpenSceneMode.Single);
            }

            Font legacyFont = LoadLegacyUiFontForSetup();
            TMP_FontAsset tmpFont = LoadTmpUiFontForSetup();
            int legacyCount = 0;
            int tmpCount = 0;

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (Text text in root.GetComponentsInChildren<Text>(true))
                {
                    if (text.font == legacyFont)
                    {
                        continue;
                    }

                    Undo.RecordObject(text, "Apply Lapsus-Pro Bold Font");
                    text.font = legacyFont;
                    EditorUtility.SetDirty(text);
                    legacyCount++;
                }

                foreach (TMP_Text text in root.GetComponentsInChildren<TMP_Text>(true))
                {
                    if (text.font == tmpFont)
                    {
                        continue;
                    }

                    Undo.RecordObject(text, "Apply Lapsus-Pro Bold SDF Font");
                    text.font = tmpFont;
                    EditorUtility.SetDirty(text);
                    tmpCount++;
                }
            }

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(
                    scene,
                    Milestone2SceneSetup.VerticalSliceScenePath))
            {
                throw new InvalidOperationException(
                    "Unity could not save the Lapsus-Pro Bold font update.");
            }

            AssetDatabase.SaveAssets();
            Debug.Log(
                $"Lapsus-Pro Bold applied to {legacyCount} Legacy Text and " +
                $"{tmpCount} TMP components without changing layout, color, " +
                "background, or gameplay values.");
        }

        [MenuItem("Cutrium/Setup/Apply Gameplay Top HUD Only")]
        public static void ApplyGameplayTopHudOnly()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (scene.path != Milestone2SceneSetup.VerticalSliceScenePath)
            {
                if (!Application.isBatchMode)
                {
                    throw new InvalidOperationException(
                        "Open VerticalSlice.unity before applying the " +
                        "gameplay TopHUD.");
                }

                scene = EditorSceneManager.OpenScene(
                    Milestone2SceneSetup.VerticalSliceScenePath,
                    OpenSceneMode.Single);
            }

            GameObject root = RequireRoot(scene, "VerticalSliceRoot");
            Transform safeArea = RequireChild(
                root.transform,
                "Canvas/SafeAreaRoot");
            Transform topHud = RequireChild(safeArea, "TopHUD");
            ConfigureResponsiveGameplayBands(safeArea);
            ConfigureGameplayTopHud(topHud, LoadTopHudAssets());

            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(
                (RectTransform)safeArea);
            BoardCameraFitter boardFitter = root
                .GetComponentInChildren<BoardCameraFitter>(true)
                ?? throw new InvalidOperationException(
                    "Gameplay TopHUD setup requires BoardCameraFitter.");
            boardFitter.RefreshNow();
            Canvas.ForceUpdateCanvases();
            ValidateGameplayTopHud(topHud);
            ValidateTopHudBoardSeparation(safeArea, boardFitter);

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
            {
                throw new InvalidOperationException(
                    "Unity could not save the gameplay TopHUD.");
            }

            Debug.Log(
                "Gameplay TopHUD applied without changing theme, progress, " +
                "sand, trail, completion, or gameplay values.");
        }

        [MenuItem("Cutrium/Setup/Apply Responsive Gameplay Layout Only")]
        public static void ApplyResponsiveGameplayLayoutOnly()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException(
                    "Exit Play Mode before updating gameplay layout.");
            }

            Scene scene = SceneManager.GetActiveScene();
            if (scene.path != Milestone2SceneSetup.VerticalSliceScenePath)
            {
                if (!Application.isBatchMode)
                {
                    throw new InvalidOperationException(
                        "Open VerticalSlice.unity before applying the " +
                        "responsive gameplay layout.");
                }

                scene = EditorSceneManager.OpenScene(
                    Milestone2SceneSetup.VerticalSliceScenePath,
                    OpenSceneMode.Single);
            }

            GameObject root = RequireRoot(scene, "VerticalSliceRoot");
            Transform safeArea = RequireChild(
                root.transform,
                "Canvas/SafeAreaRoot");
            ConfigureResponsiveGameplayBands(safeArea);

            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(
                (RectTransform)safeArea);
            BoardCameraFitter boardFitter = root
                .GetComponentInChildren<BoardCameraFitter>(true)
                ?? throw new InvalidOperationException(
                    "Responsive layout requires BoardCameraFitter.");
            boardFitter.ConfigureVerticalAlignmentForSetup(0.5f);
            boardFitter.RefreshNow();
            SandProgressPresenter progress = root
                .GetComponentInChildren<SandProgressPresenter>(true)
                ?? throw new InvalidOperationException(
                    "Responsive layout requires SandProgressPresenter.");
            progress.RefreshLayoutNow();
            Canvas.ForceUpdateCanvases();
            ValidateBoardHierarchy(root);
            ValidateGameplayBandVisualSeparation(root);

            EditorUtility.SetDirty(boardFitter);
            EditorUtility.SetDirty(progress);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
            {
                throw new InvalidOperationException(
                    "Unity could not save the responsive gameplay layout.");
            }

            Debug.Log(
                "Responsive gameplay layout applied without changing the " +
                "logical 10x16 board, gameplay, theme, sand, trail, or HUD " +
                "artwork.");
        }

        private static void VerifyBaseline()
        {
            if (!string.Equals(
                    Application.unityVersion,
                    "6000.3.21f1",
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "The presentation pass requires Unity 6000.3.21f1.");
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

        private static void EnsureFolders()
        {
            EnsureFolder("Assets/Cutrium/Art");
            EnsureFolder("Assets/Cutrium/Art/Generated");
            EnsureFolder(GeneratedFolder);
            EnsureFolder(GalataArtworkFolder);
            EnsureFolder("Assets/Cutrium/Content");
            EnsureFolder(LandmarkContentFolder);
        }

        // ------------------------------------------------------------
        // Sprite generation
        // ------------------------------------------------------------

        private static Dictionary<string, Sprite> GenerateSprites()
        {
            var patterns = new Dictionary<string, GeneratedPattern>
            {
                { "frame_soft", GeneratedPattern.Frame },
                { "board_calm", GeneratedPattern.Board },
                { "barrier_body_soft", GeneratedPattern.BarrierBody },
                { "threat_gem", GeneratedPattern.ThreatGem },
                { "power_button", GeneratedPattern.PowerButton },
                { "chip_rounded", GeneratedPattern.ChipRounded },
                { "landmark_alpine", GeneratedPattern.LandmarkAlpine },
                { "landmark_coastal", GeneratedPattern.LandmarkCoastal },
                { "landmark_desert", GeneratedPattern.LandmarkDesert },
                { "completion_scrim", GeneratedPattern.CompletionScrim },
            };
            var result = new Dictionary<string, Sprite>();
            foreach (KeyValuePair<string, GeneratedPattern> pair in patterns)
            {
                bool isLandmark = pair.Value == GeneratedPattern.LandmarkAlpine
                    || pair.Value == GeneratedPattern.LandmarkCoastal
                    || pair.Value == GeneratedPattern.LandmarkDesert;
                int size = isLandmark ? 48 : 32;
                string path = $"{GeneratedFolder}/{pair.Key}.png";
                EnsureGeneratedPng(path, pair.Value, size);
                Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sprite == null)
                {
                    throw new InvalidOperationException(
                        $"Generated sprite '{path}' did not import.");
                }

                result.Add(pair.Key, sprite);
            }

            return result;
        }

        private static void EnsureGeneratedPng(
            string path,
            GeneratedPattern pattern,
            int size)
        {
            var texture = new Texture2D(
                size,
                size,
                TextureFormat.RGBA32,
                false,
                true);
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    pixels[y * size + x] = Pixel(pattern, x, y, size);
                }
            }

            texture.SetPixels(pixels);
            texture.Apply(false, false);
            byte[] png = texture.EncodeToPNG();
            UnityEngine.Object.DestroyImmediate(texture);
            string projectRoot = Directory.GetParent(Application.dataPath)
                ?.FullName
                ?? throw new InvalidOperationException(
                    "Unity project root could not be resolved.");
            string absolute = Path.Combine(
                projectRoot,
                path.Replace('/', Path.DirectorySeparatorChar));
            bool changed = !File.Exists(absolute)
                || !File.ReadAllBytes(absolute).SequenceEqual(png);
            if (changed)
            {
                File.WriteAllBytes(absolute, png);
            }

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            Vector4 border = GetSpriteBorder(pattern, size);
            bool importerChanged = importer.textureType != TextureImporterType.Sprite
                || importer.spriteImportMode != SpriteImportMode.Single
                || importer.mipmapEnabled
                || importer.wrapMode != TextureWrapMode.Clamp
                || importer.filterMode != FilterMode.Bilinear
                || !Mathf.Approximately(importer.spritePixelsPerUnit, 32f)
                || importer.spriteBorder != border;
            if (importerChanged)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.filterMode = FilterMode.Bilinear;
                importer.spritePixelsPerUnit = 32f;
                importer.spriteBorder = border;
                importer.SaveAndReimport();
            }
        }

        private static Vector4 GetSpriteBorder(GeneratedPattern pattern, int size)
        {
            if (pattern != GeneratedPattern.ChipRounded)
            {
                return Vector4.zero;
            }

            float border = size * 0.25f;
            return new Vector4(border, border, border, border);
        }

        private static Color Pixel(
            GeneratedPattern pattern,
            int x,
            int y,
            int size)
        {
            float u = (x + 0.5f) / size;
            float v = (y + 0.5f) / size;
            float dx = u - 0.5f;
            float dy = v - 0.5f;
            float radius = Mathf.Sqrt(dx * dx + dy * dy);
            switch (pattern)
            {
                case GeneratedPattern.Frame:
                {
                    bool edge = x < 3 || y < 3 || x >= size - 3 || y >= size - 3;
                    return edge
                        ? new Color(0.85f, 0.78f, 0.62f, 0.5f)
                        : new Color(0.05f, 0.06f, 0.08f, 0.05f);
                }

                case GeneratedPattern.Board:
                {
                    float vignette = Mathf.Clamp01(1f - radius * 1.3f);
                    Color soft = Color.Lerp(
                        new Color(0.035f, 0.09f, 0.1f, 1f),
                        new Color(0.06f, 0.14f, 0.15f, 1f),
                        vignette * 0.5f);
                    return new Color(soft.r, soft.g, soft.b, 0.55f);
                }

                case GeneratedPattern.BarrierBody:
                {
                    float center = Mathf.Clamp01(1f - Mathf.Abs(dy) * 8f);
                    float alpha = 0.5f + center * 0.35f;
                    return new Color(0.92f, 0.93f, 0.97f, alpha);
                }

                case GeneratedPattern.ThreatGem:
                {
                    if (radius > 0.47f)
                    {
                        return Color.clear;
                    }

                    float glow = Mathf.Clamp01(1f - radius * 1.7f);
                    Color inner = new Color(1f, 0.85f, 0.6f, 1f);
                    Color outer = new Color(0.82f, 0.42f, 0.32f, 1f);
                    Color blended = Color.Lerp(outer, inner, glow * glow);
                    float edgeAlpha = radius > 0.44f
                        ? Mathf.Clamp01((0.47f - radius) / 0.03f)
                        : 1f;
                    return new Color(blended.r, blended.g, blended.b, edgeAlpha);
                }

                case GeneratedPattern.PowerButton:
                {
                    float shade = Mathf.Clamp01(1f - radius * 1.3f);
                    return new Color(1f, 1f, 1f, 0.22f + shade * 0.18f);
                }

                case GeneratedPattern.LandmarkAlpine:
                    return LandmarkGradient(
                        u,
                        v,
                        new Color(0.06f, 0.22f, 0.32f, 1f),
                        new Color(0.55f, 0.78f, 0.86f, 1f),
                        new Color(1f, 0.92f, 0.75f, 1f),
                        0.32f,
                        0.7f);

                case GeneratedPattern.LandmarkCoastal:
                    return LandmarkGradient(
                        u,
                        v,
                        new Color(0.85f, 0.72f, 0.48f, 1f),
                        new Color(0.35f, 0.78f, 0.78f, 1f),
                        new Color(1f, 0.95f, 0.8f, 1f),
                        0.68f,
                        0.62f);

                case GeneratedPattern.LandmarkDesert:
                    return LandmarkGradient(
                        u,
                        v,
                        new Color(0.32f, 0.18f, 0.28f, 1f),
                        new Color(0.95f, 0.55f, 0.35f, 1f),
                        new Color(1f, 0.85f, 0.55f, 1f),
                        0.5f,
                        0.55f);

                case GeneratedPattern.ChipRounded:
                {
                    // A soft rounded-rect alpha mask, meant to be rendered
                    // with Image.Type.Sliced so the corner radius stays
                    // crisp regardless of how far the chip stretches.
                    float cornerRadius = size * 0.25f;
                    float px = x + 0.5f;
                    float py = y + 0.5f;
                    float dx2 = Mathf.Max(
                        0f,
                        Mathf.Max(cornerRadius - px, px - (size - cornerRadius)));
                    float dy2 = Mathf.Max(
                        0f,
                        Mathf.Max(cornerRadius - py, py - (size - cornerRadius)));
                    float cornerDistance = Mathf.Sqrt(dx2 * dx2 + dy2 * dy2);
                    float alpha = Mathf.Clamp01(
                        cornerRadius + 1f - cornerDistance);
                    return new Color(1f, 1f, 1f, alpha);
                }

                case GeneratedPattern.CompletionScrim:
                {
                    // Transparent near the top (keeps the hero artwork
                    // readable) fading to a soft dark base near the bottom
                    // (keeps title/description/buttons legible).
                    float alpha = Mathf.Lerp(0.92f, 0.02f, v);
                    return new Color(0.02f, 0.03f, 0.05f, alpha);
                }

                default:
                    return Color.magenta;
            }
        }

        private static Color LandmarkGradient(
            float u,
            float v,
            Color groundColor,
            Color skyColor,
            Color sunColor,
            float sunU,
            float sunV)
        {
            Color sky = Color.Lerp(groundColor, skyColor, v);
            float dx = u - sunU;
            float dy = v - sunV;
            float distance = Mathf.Sqrt(dx * dx + dy * dy);
            float glow = Mathf.Clamp01(1f - distance * 2f);
            Color result = Color.Lerp(sky, sunColor, glow * 0.8f);
            return new Color(result.r, result.g, result.b, 1f);
        }

        private static Dictionary<string, Sprite> ReloadGeneratedSprites(
            IEnumerable<string> names)
        {
            var result = new Dictionary<string, Sprite>();
            foreach (string name in names)
            {
                string path = $"{GeneratedFolder}/{name}.png";
                Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sprite == null)
                {
                    throw new InvalidOperationException(
                        $"Generated sprite '{path}' could not be reloaded " +
                        "after opening the scene.");
                }

                result.Add(name, sprite);
            }

            return result;
        }

        // ------------------------------------------------------------
        // User-supplied artwork
        // ------------------------------------------------------------

        private static readonly string[] GalataArtworkCandidates =
        {
            $"{GalataArtworkFolder}/GalataKulesi.png",
            $"{GalataArtworkFolder}/GalataKulesi.jpg",
            $"{GalataArtworkFolder}/GalataKulesi.jpeg",
        };

        private static Sprite LoadGalataArtworkIfPresent()
        {
            string projectRoot = Directory.GetParent(Application.dataPath)
                ?.FullName
                ?? throw new InvalidOperationException(
                    "Unity project root could not be resolved.");
            foreach (string path in GalataArtworkCandidates)
            {
                string absolute = Path.Combine(
                    projectRoot,
                    path.Replace('/', Path.DirectorySeparatorChar));
                if (!File.Exists(absolute))
                {
                    continue;
                }

                AssetDatabase.ImportAsset(
                    path,
                    ImportAssetOptions.ForceSynchronousImport);
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                if (importer == null)
                {
                    continue;
                }

                bool importerChanged =
                    importer.textureType != TextureImporterType.Sprite
                    || importer.spriteImportMode != SpriteImportMode.Single
                    || importer.mipmapEnabled
                    || importer.wrapMode != TextureWrapMode.Clamp
                    || importer.filterMode != FilterMode.Bilinear;
                if (importerChanged)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spriteImportMode = SpriteImportMode.Single;
                    importer.alphaIsTransparency = false;
                    importer.mipmapEnabled = false;
                    importer.wrapMode = TextureWrapMode.Clamp;
                    importer.filterMode = FilterMode.Bilinear;
                    importer.SaveAndReimport();
                }

                Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sprite != null)
                {
                    return sprite;
                }
            }

            return null;
        }

        // ------------------------------------------------------------
        // Theme
        // ------------------------------------------------------------

        private static ThemeDefinition LoadTheme(string path)
        {
            ThemeDefinition theme =
                AssetDatabase.LoadAssetAtPath<ThemeDefinition>(path);
            return theme ?? throw new InvalidOperationException(
                $"Theme asset '{path}' could not be loaded. Run the " +
                "Milestone 5 theme pipeline setup first.");
        }

        private static void ConfigureCleanupTheme(
            ThemeDefinition theme,
            IReadOnlyDictionary<string, Sprite> sprites)
        {
            theme.ConfigureForSetup(
                "cleanup-chamber-prototype",
                null,
                DarkBrownBackground,
                sprites["board_calm"],
                Color.white,
                sprites["frame_soft"],
                Color.white,
                LoadSingleSprite(NormalThreatVisualPath),
                Color.white,
                Vector2.one,
                Vector2.zero,
                theme.ThreatShadowSprite,
                new Color(0f, 0f, 0f, 0.26f),
                theme.ThreatTrailSprite,
                NormalThreatTrailTint,
                sprites["barrier_body_soft"],
                theme.BarrierCapSprite,
                sprites["barrier_body_soft"],
                BarrierPreviewBrown,
                GrowingBarrierBrown,
                new Color(0.95f, 0.8f, 0.4f, 1f),
                new Color(0.9f, 0.42f, 0.44f, 0.85f),
                null,
                null,
                Color.clear,
                // Fully transparent: TopHUD/BottomHUD no longer paint their
                // own panel behind their content, so they show through to
                // the same outer canvas background as everything else
                // instead of reading as separate black header/footer bands.
                new Color(0.03f, 0.045f, 0.06f, 0f),
                new Color(0.85f, 0.78f, 0.62f, 1f),
                new Color(0.96f, 0.95f, 0.92f, 1f));
            ConfigureThreatVisualVariants(theme);
            EditorUtility.SetDirty(theme);
        }

        private static void ConfigureThreatVisualVariants(ThemeDefinition theme)
        {
            theme.ConfigureThreatSpritesForSetup(
                LoadSingleSprite(NormalThreatVisualPath),
                LoadSingleSprite(HunterThreatVisualPath),
                LoadSingleSprite(PulseThreatVisualPath));
            theme.ConfigureThreatTrailColorsForSetup(
                NormalThreatTrailTint,
                HunterThreatTrailTint,
                PulseThreatTrailTint);
        }

        private static void ValidateThreatVisualVariants(ThemeDefinition theme)
        {
            if (AssetDatabase.GetAssetPath(theme.ThreatSprite)
                    != NormalThreatVisualPath
                || AssetDatabase.GetAssetPath(theme.HunterThreatSprite)
                    != HunterThreatVisualPath
                || AssetDatabase.GetAssetPath(theme.PulseThreatSprite)
                    != PulseThreatVisualPath)
            {
                throw new InvalidOperationException(
                    "The cleanup theme must reference the imported Normal, " +
                    "Hunter, and Pulse threat visuals.");
            }

            if (!theme.UseThreatBehaviorTrailColors
                || theme.ThreatTrailColor != NormalThreatTrailTint
                || theme.HunterThreatTrailColor != HunterThreatTrailTint
                || theme.PulseThreatTrailColor != PulseThreatTrailTint)
            {
                throw new InvalidOperationException(
                    "The cleanup theme must tint its shared trail blue for " +
                    "Normal, red for Hunter, and green for Pulse threats.");
            }
        }

        // ------------------------------------------------------------
        // Landmarks
        // ------------------------------------------------------------

        private const string LegacyAlpineLandmarkPath =
            LandmarkContentFolder + "/AlpineOverlook.asset";

        private static readonly string[] LandmarkAssetPaths =
        {
            $"{LandmarkContentFolder}/GalataKulesi.asset",
            $"{LandmarkContentFolder}/CoastalLagoon.asset",
            $"{LandmarkContentFolder}/DesertDunes.asset",
        };

        private static LandmarkDefinition[] ConfigureLandmarks(
            IReadOnlyDictionary<string, Sprite> sprites)
        {
            // A broad presentation rerun must not restore the old three
            // placeholder landmarks over the real first-twelve catalog.
            return MainEarthLandmarkContent.CreateOrUpdateAssets();
        }

        private static LandmarkDefinition GetOrCreateLandmark(
            string path,
            string legacyPath = null)
        {
            LandmarkDefinition landmark =
                AssetDatabase.LoadAssetAtPath<LandmarkDefinition>(path);
            if (landmark != null)
            {
                return landmark;
            }

            if (legacyPath != null)
            {
                LandmarkDefinition legacy =
                    AssetDatabase.LoadAssetAtPath<LandmarkDefinition>(legacyPath);
                if (legacy != null)
                {
                    string moveError = AssetDatabase.MoveAsset(legacyPath, path);
                    if (!string.IsNullOrEmpty(moveError))
                    {
                        throw new InvalidOperationException(
                            $"Could not migrate '{legacyPath}' to '{path}': " +
                            moveError);
                    }

                    landmark = AssetDatabase.LoadAssetAtPath<LandmarkDefinition>(path);
                    if (landmark != null)
                    {
                        return landmark;
                    }
                }
            }

            landmark = ScriptableObject.CreateInstance<LandmarkDefinition>();
            AssetDatabase.CreateAsset(landmark, path);
            return landmark;
        }

        private static LandmarkDefinition[] ReloadLandmarks()
        {
            return MainEarthLandmarkContent.CreateOrUpdateAssets();
        }

        private static ProgressSprites LoadProgressSprites()
        {
            // Sliced: the bar now fills ProgressSlot's full width (flush
            // left) instead of a fixed aspect-locked size, so width and
            // height vary independently -- same reasoning as TopHudBar.
            EnsureUiSpriteImportSettings(
                ProgressBackgroundPath,
                sliced9Slice: true);
            EnsureUiSpriteImportSettings(ProgressFillPath, sliced9Slice: true);
            return new ProgressSprites(
                LoadSingleSprite(ProgressBackgroundPath),
                LoadSingleSprite(ProgressFillPath));
        }

        private static TopHudAssets LoadTopHudAssets()
        {
            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
                TopHudFontPath);
            if (font == null)
            {
                throw new InvalidOperationException(
                    $"Gameplay TopHUD font is missing at '{TopHudFontPath}'.");
            }

            EnsureUiSpriteImportSettings(BigHudBackgroundPath, sliced9Slice: true);
            EnsureUiSpriteImportSettings(HealthIconPath);
            EnsureUiSpriteImportSettings(SpeedIconPath);
            EnsureUiSpriteImportSettings(SpeedIconL1Path);
            EnsureUiSpriteImportSettings(SpeedIconL2Path);
            EnsureUiSpriteImportSettings(SpeedIconL3Path);
            EnsureUiSpriteImportSettings(SpeedIconL4Path);
            EnsureUiSpriteImportSettings(CoinStackL1Path);

            return new TopHudAssets(
                LoadSingleSprite(BigHudBackgroundPath),
                LoadSingleSprite(HealthIconPath),
                LoadSingleSprite(SpeedIconPath),
                LoadSingleSprite(SettingsButtonPath),
                LoadSingleSprite(CoinStackL1Path),
                font);
        }

        private static SkillAssets LoadSkillAssets()
        {
            EnsureUiSpriteImportSettings(FreezeSkillPath);
            EnsureUiSpriteImportSettings(InstantBarrierSkillPath);
            EnsureUiSpriteImportSettings(GravityWellSkillPath);
            return new SkillAssets(
                LoadSingleSprite(FreezeSkillPath),
                LoadSingleSprite(InstantBarrierSkillPath),
                LoadSingleSprite(GravityWellSkillPath));
        }

        internal static Sprite LoadUiSpriteForSetup(string path)
        {
            EnsureUiSpriteImportSettings(path);
            return LoadSingleSprite(path);
        }

        private static Sprite LoadSingleSprite(string path)
        {
            UnityEngine.Object[] imported = AssetDatabase.LoadAllAssetsAtPath(path);
            Sprite[] sprites = imported.OfType<Sprite>().ToArray();
            if (sprites.Length != 1)
            {
                throw new InvalidOperationException(
                    $"UI asset '{path}' must contain exactly one " +
                    $"Sprite subasset, found {sprites.Length}.");
            }

            return sprites[0];
        }

        // User-supplied UI art (HUD plaques, icons, skill art) ships with no
        // .meta file, so its first import would otherwise default to a
        // plain Texture2D and LoadSingleSprite above would throw. This
        // forces the same Sprite (2D and UI) import settings used elsewhere
        // in this file before any such asset is loaded.
        //
        // sliced9Slice: when true, a proportional border (fraction of the
        // texture's own pixel height, since these are wide short plaques
        // whose rounded end-caps scale with height) is set on the
        // importer so the sprite can render as Image.Type.Sliced -- its
        // rounded corners/bezel stay crisp while the flat middle stretches
        // to fit a target width that doesn't match its native aspect.
        private const float HudPlaqueBorderFraction = 0.3f;

        private static void EnsureUiSpriteImportSettings(
            string path,
            bool sliced9Slice = false)
        {
            AssetDatabase.ImportAsset(
                path,
                ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            if (importer == null)
            {
                throw new InvalidOperationException(
                    $"UI asset '{path}' could not be imported.");
            }

            Vector4 border = Vector4.zero;
            if (sliced9Slice)
            {
                importer.GetSourceTextureWidthAndHeight(
                    out int textureWidth,
                    out int textureHeight);
                float edge = Mathf.Min(textureWidth, textureHeight)
                    * HudPlaqueBorderFraction;
                border = new Vector4(edge, edge, edge, edge);
            }

            bool importerChanged =
                importer.textureType != TextureImporterType.Sprite
                || importer.spriteImportMode != SpriteImportMode.Single
                || importer.mipmapEnabled
                || importer.wrapMode != TextureWrapMode.Clamp
                || importer.filterMode != FilterMode.Bilinear
                || importer.spriteBorder != border;
            if (importerChanged)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.filterMode = FilterMode.Bilinear;
                importer.spriteBorder = border;
                importer.SaveAndReimport();
            }
        }

        // ------------------------------------------------------------
        // Scene composition
        // ------------------------------------------------------------

        private static void Configure(
            Scene scene,
            IReadOnlyDictionary<string, Sprite> sprites,
            ThemeDefinition cleanup,
            LandmarkDefinition[] landmarks,
            ProgressSprites progressSprites)
        {
            GameObject root = RequireRoot(scene, "VerticalSliceRoot");
            Transform safeArea = RequireChild(root.transform, "Canvas/SafeAreaRoot");
            Transform boardStage = RequireChild(safeArea, "BoardStage");
            Transform boardViewport = RequireChild(boardStage, "BoardViewport");
            RectTransform boardFrame =
                (RectTransform)RequireChild(boardViewport, "BoardFrame");
            FirstPlayableController controller = root
                .GetComponentInChildren<FirstPlayableController>(true);
            BarrierPresenter barrierPresenter = root
                .GetComponentInChildren<BarrierPresenter>(true);
            ThreatPresenter threatPresenter = root
                .GetComponentInChildren<ThreatPresenter>(true);
            CaptureBoardPresenter capturePresenter = root
                .GetComponentInChildren<CaptureBoardPresenter>(true);
            ThemePresenter themePresenter = root
                .GetComponentInChildren<ThemePresenter>(true);
            CaptureHudPresenter hud = root
                .GetComponentInChildren<CaptureHudPresenter>(true);
            PowerHudPresenter powerHud = root
                .GetComponentInChildren<PowerHudPresenter>(true);
            BoardCameraFitter boardCameraFitter = root
                .GetComponentInChildren<BoardCameraFitter>(true);

            barrierPresenter.SetVisualLogicalThickness(
                BarrierVisualLogicalThickness);

            Transform topHud = RequireChild(safeArea, "TopHUD");
            Transform bottomHud = RequireChild(safeArea, "BottomHUD");

            ConfigureResponsiveGameplayBands(safeArea);
            ConfigureGameplayTopHud(topHud, LoadTopHudAssets());
            ConfigureMinimalBottomHud(
                root,
                safeArea,
                controller,
                progressSprites,
                out SandProgressPresenter sandProgressPresenter,
                out RectTransform progressFillStartTarget);
            ConfigureBottomHudSkillRow(
                safeArea,
                controller,
                powerHud,
                LoadSkillAssets());
            ConfigureGravityWellCue(
                boardFrame,
                controller,
                LoadUiSpriteForSetup(GravityWellVortexPath));
            ConfigureFeedbackReadabilityForSetup(safeArea);
            RectTransform grainFlightRoot = ConfigureGrainFlightRoot(safeArea);

            ConfigureLandmarkLayer(
                root,
                boardFrame,
                controller,
                RequireChild(safeArea, "LevelCompleteOverlay"),
                landmarks,
                cleanup,
                grainFlightRoot,
                progressFillStartTarget,
                sandProgressPresenter,
                out LandmarkRevealPresenter landmarkPresenter);

            HideDebugFooter(bottomHud);
            FinalizeThemeTextSync(themePresenter, topHud, bottomHud);
            // TopHUD and BottomHUD are fixed reserved bands; BoardStage owns
            // the flexible remainder. Center the unchanged 10:16 fit inside
            // that remainder so any unavoidable tall-screen surplus is
            // balanced above and below the board.
            boardCameraFitter.ConfigureVerticalAlignmentForSetup(0.5f);

            // Resolve BoardViewport to the real aspect-fitted rect before
            // saving, so the scene doesn't sit at a stale/fallback size --
            // BoardCameraFitter otherwise only refreshes via its own
            // LateUpdate() once the game is actually running.
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)safeArea);
            boardCameraFitter.RefreshNow();

            barrierPresenter.RefreshNow();
            threatPresenter.RefreshNow();
            capturePresenter.RefreshNow();
            landmarkPresenter.RefreshNow();
            sandProgressPresenter.RefreshNow();
            ApplyGeneralButtonStylesForSetup(root);

            EditorUtility.SetDirty(controller);
            EditorUtility.SetDirty(barrierPresenter);
            EditorUtility.SetDirty(landmarkPresenter);
            EditorUtility.SetDirty(sandProgressPresenter);
            EditorUtility.SetDirty(hud);
            EditorUtility.SetDirty(powerHud);
            EditorUtility.SetDirty(boardCameraFitter);
            EditorUtility.SetDirty(boardViewport.gameObject);
        }

        // A dedicated full-safe-area overlay that sand-grain particles
        // (spawned by LandmarkRevealPresenter) fly across, from the
        // captured board region to the progress bar in BottomHUD -- outside
        // _boardFrame's own hierarchy, since grains must cross into
        // BottomHUD space. Kept just before LevelCompleteOverlay in
        // sibling order (not last) so completion still visually covers
        // any leftover grain.
        private static RectTransform ConfigureGrainFlightRoot(Transform safeArea)
        {
            RectTransform grainFlightRoot = GetOrCreateUiChild(
                safeArea,
                "GrainFlightRoot");
            StretchToParent(grainFlightRoot);
            // ignoreLayout so safeArea's VerticalLayoutGroup never treats
            // this as a normal flow band (it would otherwise consume
            // vertical space and push TopHUD/BoardStage/BottomHUD down) --
            // the same pattern LevelCompleteOverlay already uses.
            LayoutElement grainFlightLayout = GetOrAddComponent<LayoutElement>(
                grainFlightRoot.gameObject);
            grainFlightLayout.ignoreLayout = true;
            EditorUtility.SetDirty(grainFlightLayout);

            grainFlightRoot.SetAsLastSibling();

            // Re-assert LevelCompleteOverlay as the final sibling
            // explicitly, rather than reading its current index and
            // moving grainFlightRoot there -- on a repeat run
            // grainFlightRoot may already sit before it, and moving to a
            // *stale* captured index would push the overlay out of last
            // place instead of keeping it there.
            Transform completionOverlay = safeArea.Find("LevelCompleteOverlay");
            if (completionOverlay != null)
            {
                completionOverlay.SetAsLastSibling();
            }

            return grainFlightRoot;
        }

        private static void ConfigureLandmarkLayer(
            GameObject root,
            RectTransform boardFrame,
            FirstPlayableController controller,
            Transform completionOverlay,
            LandmarkDefinition[] landmarks,
            ThemeDefinition sandBowlTheme,
            RectTransform grainFlightRoot,
            RectTransform progressFillStartTarget,
            SandProgressPresenter sandProgressPresenter,
            out LandmarkRevealPresenter landmarkPresenter)
        {
            RectTransform boardSurface =
                (RectTransform)RequireChild(boardFrame, "BoardSurface");

            RectTransform artworkRect = GetOrCreateUiChild(
                boardFrame,
                "LandmarkArtwork");
            StretchToParent(artworkRect);
            Image artworkImage = GetOrAddComponent<Image>(
                artworkRect.gameObject);
            artworkImage.raycastTarget = false;

            RectTransform veilRoot = GetOrCreateUiChild(
                boardFrame,
                "LandmarkVeilRoot");
            StretchToParent(veilRoot);

            artworkRect.SetSiblingIndex(boardSurface.GetSiblingIndex() + 1);
            veilRoot.SetSiblingIndex(artworkRect.GetSiblingIndex() + 1);

            // Discard legacy completion layouts from earlier presentation
            // passes (card-style, and the full-screen stretched hero photo)
            // so re-running setup converges cleanly on the new framed-photo
            // design instead of leaving stale siblings behind.
            Transform legacyCard = completionOverlay.Find("LandmarkCard");
            if (legacyCard != null)
            {
                UnityEngine.Object.DestroyImmediate(legacyCard.gameObject);
            }

            Transform legacyHero = completionOverlay.Find("HeroArtwork");
            if (legacyHero != null)
            {
                UnityEngine.Object.DestroyImmediate(legacyHero.gameObject);
            }

            Transform legacyScrim = completionOverlay.Find("ScrimOverlay");
            if (legacyScrim != null)
            {
                UnityEngine.Object.DestroyImmediate(legacyScrim.gameObject);
            }

            // The overlay itself is a single opaque color that fully hides
            // the board/HUD behind it -- never a partial alpha that lets
            // buttons or HUD text show through.
            Image overlayBackground =
                GetOrAddComponent<Image>(completionOverlay.gameObject);
            overlayBackground.sprite = null;
            overlayBackground.type = Image.Type.Simple;
            // Re-assert the owner-authored brown after the legacy milestone
            // setup chain recreates its old blue baseline.
            overlayBackground.color = CompletionBackgroundBrown;
            overlayBackground.raycastTarget = true;

            // The hero photo lives in a fixed-aspect frame instead of
            // stretching to fill the screen, so its aspect ratio is never
            // distorted. HeroFrameBounds is the available slot (positioned
            // above the text/button content); HeroArtwork fits itself into
            // a centered, deliberately wide (not square) box within it via
            // AspectRatioFitter, then the photo itself letterboxes inside
            // that box if it isn't natively that ratio (Image.preserveAspect).
            // A real frame sprite (e.g. "hung on a wall") can be layered on
            // this later without changing the layout.
            RectTransform heroBoundsRect = GetOrCreateUiChild(
                completionOverlay,
                "HeroFrameBounds");
            heroBoundsRect.anchorMin = new Vector2(0.04f, 0.30f);
            heroBoundsRect.anchorMax = new Vector2(0.96f, 0.86f);
            heroBoundsRect.pivot = new Vector2(0.5f, 0.5f);
            heroBoundsRect.offsetMin = Vector2.zero;
            heroBoundsRect.offsetMax = Vector2.zero;
            CanvasGroup scrimGroup =
                GetOrAddComponent<CanvasGroup>(heroBoundsRect.gameObject);
            heroBoundsRect.SetSiblingIndex(0);

            RectTransform heroRect = GetOrCreateUiChild(
                heroBoundsRect,
                "HeroArtwork");
            StretchToParent(heroRect);
            AspectRatioFitter heroFitter =
                GetOrAddComponent<AspectRatioFitter>(heroRect.gameObject);
            heroFitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            // Hand-tuned: 1.35 read too small once seen full-size; 1.1 is
            // the owner's chosen value.
            heroFitter.aspectRatio = 1.1f;
            Image heroImage = GetOrAddComponent<Image>(heroRect.gameObject);
            heroImage.raycastTarget = false;
            heroImage.preserveAspect = true;

            RectTransform contentRect = GetOrCreateUiChild(
                completionOverlay,
                "CompletionContent");
            contentRect.anchorMin = new Vector2(0.06f, 0.12f);
            contentRect.anchorMax = new Vector2(0.94f, 0.27f);
            contentRect.pivot = new Vector2(0.5f, 0.5f);
            contentRect.offsetMin = Vector2.zero;
            contentRect.offsetMax = Vector2.zero;
            CanvasGroup contentGroup =
                GetOrAddComponent<CanvasGroup>(contentRect.gameObject);
            contentRect.SetSiblingIndex(2);

            VerticalLayoutGroup contentColumn =
                GetOrAddComponent<VerticalLayoutGroup>(contentRect.gameObject);
            contentColumn.padding = new RectOffset(0, 0, 0, 0);
            contentColumn.spacing = 6f;
            contentColumn.childAlignment = TextAnchor.UpperCenter;
            contentColumn.childControlWidth = true;
            contentColumn.childControlHeight = true;
            contentColumn.childForceExpandWidth = true;
            contentColumn.childForceExpandHeight = false;

            RectTransform titleRect = GetOrCreateUiChild(contentRect, "Title");
            titleRect.anchorMin = new Vector2(0.5f, 1f);
            titleRect.anchorMax = new Vector2(0.5f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            Text titleText = ConfigureText(
                titleRect,
                "Landmark",
                56,
                TextAnchor.LowerCenter,
                new Color(0.99f, 0.96f, 0.9f, 1f));
            titleText.fontStyle = FontStyle.Bold;
            LayoutElement titleLayout =
                GetOrAddComponent<LayoutElement>(titleRect.gameObject);
            titleLayout.minHeight = 76f;
            titleLayout.preferredHeight = 76f;
            titleLayout.flexibleHeight = 0f;

            RectTransform sectorRect = GetOrCreateUiChild(contentRect, "Sector");
            sectorRect.anchorMin = new Vector2(0.5f, 1f);
            sectorRect.anchorMax = new Vector2(0.5f, 1f);
            sectorRect.pivot = new Vector2(0.5f, 1f);
            Text sectorText = ConfigureText(
                sectorRect,
                "Sector",
                30,
                TextAnchor.UpperCenter,
                new Color(0.85f, 0.78f, 0.62f, 0.92f));
            LayoutElement sectorLayout =
                GetOrAddComponent<LayoutElement>(sectorRect.gameObject);
            sectorLayout.minHeight = 42f;
            sectorLayout.preferredHeight = 42f;
            sectorLayout.flexibleHeight = 0f;

            RectTransform descriptionRect = GetOrCreateUiChild(
                contentRect,
                "Description");
            descriptionRect.anchorMin = new Vector2(0.5f, 1f);
            descriptionRect.anchorMax = new Vector2(0.5f, 1f);
            descriptionRect.pivot = new Vector2(0.5f, 1f);
            Text descriptionText = ConfigureText(
                descriptionRect,
                "Description",
                36,
                TextAnchor.UpperCenter,
                new Color(0.9f, 0.89f, 0.86f, 0.9f));
            descriptionText.horizontalOverflow = HorizontalWrapMode.Wrap;
            descriptionText.verticalOverflow = VerticalWrapMode.Truncate;
            LayoutElement descriptionLayout =
                GetOrAddComponent<LayoutElement>(descriptionRect.gameObject);
            descriptionLayout.minHeight = 170f;
            descriptionLayout.preferredHeight = 230f;
            descriptionLayout.flexibleHeight = 1f;

            // The stats line reuses the existing CompleteText element rather
            // than reparenting it: Milestone3CoreFunPlayModeTests looks it up
            // with the non-recursive Transform.Find("CompleteText") and would
            // break if it stopped being a direct LevelCompleteOverlay child.
            // It sits centered in the leftover space above the photo frame,
            // not below the content column.
            Transform completeTextTransform = RequireChild(
                completionOverlay,
                "CompleteText");
            var statsRect = (RectTransform)completeTextTransform;
            statsRect.anchorMin = new Vector2(0.06f, 0.88f);
            statsRect.anchorMax = new Vector2(0.94f, 0.98f);
            statsRect.pivot = new Vector2(0.5f, 0.5f);
            statsRect.offsetMin = Vector2.zero;
            statsRect.offsetMax = Vector2.zero;
            Text statsText = completeTextTransform.GetComponent<Text>();
            statsText.fontSize = 30;
            statsText.fontStyle = FontStyle.Normal;
            statsText.alignment = TextAnchor.MiddleCenter;
            statsText.color = new Color(0.82f, 0.86f, 0.88f, 0.85f);
            CanvasGroup statsGroup =
                GetOrAddComponent<CanvasGroup>(completeTextTransform.gameObject);

            // Retry/Next stay direct LevelCompleteOverlay children for the
            // same Find-by-name reason; only their rect and CanvasGroup
            // change so they read as a clean bottom action row.
            Transform retryTransform = RequireChild(
                completionOverlay,
                "RetryButton");
            var retryRect = (RectTransform)retryTransform;
            ConfigureFixedCompletionButton(retryRect, -160f);
            CanvasGroup retryGroup =
                GetOrAddComponent<CanvasGroup>(retryTransform.gameObject);

            Transform nextTransform = RequireChild(completionOverlay, "NextButton");
            var nextRect = (RectTransform)nextTransform;
            ConfigureFixedCompletionButton(nextRect, 160f);
            CanvasGroup nextGroup =
                GetOrAddComponent<CanvasGroup>(nextTransform.gameObject);

            SandBowlVisualStyle sandBowl = ThemeResolver.ResolveSandBowl(
                sandBowlTheme,
                null);
            Texture2D sandTexture = sandBowl.SandTexture != null
                ? sandBowl.SandTexture.texture
                : null;

            GameObject services = GetOrCreateChild(
                root.transform,
                "LandmarkServices");
            landmarkPresenter =
                GetOrAddComponent<LandmarkRevealPresenter>(services);
            landmarkPresenter.Configure(
                controller,
                boardFrame,
                artworkImage,
                veilRoot,
                sandTexture,
                grainFlightRoot,
                progressFillStartTarget,
                sandProgressPresenter,
                RevealFadeSeconds,
                heroImage,
                scrimGroup,
                contentGroup,
                statsGroup,
                retryGroup,
                nextGroup,
                titleText,
                sectorText,
                descriptionText,
                LandmarkCompletionTiming.Default,
                landmarks);
            ConfigureCompletionRewardFlowForSetup(
                root,
                landmarkPresenter);
            Font completionFont = LoadCompletionFont();
            ApplyCompletionReadability(
                completionOverlay,
                completionFont);
            landmarkPresenter.ConfigureCompletionFontForSetup(completionFont);

            CaptureHudPresenter hud = root
                .GetComponentInChildren<CaptureHudPresenter>(true);
            if (hud == null)
            {
                throw new InvalidOperationException(
                    "Completion reveal requires CaptureHudPresenter.");
            }

            hud.ConfigureCompletionRevealGateForSetup(landmarkPresenter);

            EditorUtility.SetDirty(artworkImage);
            EditorUtility.SetDirty(heroImage);
            EditorUtility.SetDirty(overlayBackground);
            EditorUtility.SetDirty(titleText);
            EditorUtility.SetDirty(sectorText);
            EditorUtility.SetDirty(descriptionText);
            EditorUtility.SetDirty(statsText);
            EditorUtility.SetDirty(hud);
        }

        private static void ConfigureCompletionRewardFlowForSetup(
            GameObject root,
            LandmarkRevealPresenter presenter)
        {
            ThreatPresenter threatPresenter = root
                .GetComponentInChildren<ThreatPresenter>(true);
            CaptureBoardPresenter captureBoardPresenter = root
                .GetComponentInChildren<CaptureBoardPresenter>(true);
            FeedbackPresenter feedbackPresenter = root
                .GetComponentInChildren<FeedbackPresenter>(true);
            CaptureHudPresenter captureHudPresenter = root
                .GetComponentInChildren<CaptureHudPresenter>(true);
            FirstPlayableController controller = root
                .GetComponentInChildren<FirstPlayableController>(true);
            FeedbackAudioPresenter feedbackAudioPresenter = root
                .GetComponentInChildren<FeedbackAudioPresenter>(true);
            GameObject cloudServicesObject = GetOrCreateChild(
                root.transform,
                "CloudServices");
            CloudServicesBootstrap cloudServices = GetOrAddComponent<
                CloudServicesBootstrap>(cloudServicesObject);
            if (threatPresenter == null
                || captureBoardPresenter == null
                || feedbackPresenter == null
                || captureHudPresenter == null
                || controller == null
                || feedbackAudioPresenter == null)
            {
                throw new InvalidOperationException(
                    "Completion reward flow requires ThreatPresenter, " +
                    "CaptureBoardPresenter, FeedbackPresenter, " +
                    "CaptureHudPresenter, FirstPlayableController, and " +
                    "FeedbackAudioPresenter.");
            }

            Transform safeArea = RequireChild(
                root.transform,
                "Canvas/SafeAreaRoot");
            Transform hudRow = RequireChild(
                safeArea,
                "TopHUD/GameplayHudRow");
            TopHudAssets topHudAssets = LoadTopHudAssets();
            CoinBalanceHudPresenter balanceHud = ConfigureCoinBalanceSlot(
                (RectTransform)hudRow,
                topHudAssets.Coin,
                topHudAssets.Font,
                cloudServices);
            LevelCoinRewardPresenter coinRewardPresenter =
                ConfigureLevelCoinRewardOverlay(
                    safeArea,
                    controller,
                    cloudServices,
                    feedbackAudioPresenter,
                    balanceHud,
                    topHudAssets.Coin,
                    topHudAssets.Font);

            Undo.RecordObject(presenter, "Wire Completion Reward Flow");
            presenter.ConfigureCompletionRewardFlowForSetup(
                threatPresenter,
                captureBoardPresenter,
                feedbackPresenter,
                CompletionSummarySeconds,
                coinRewardPresenter);
            Undo.RecordObject(captureHudPresenter, "Tune Completion Fade");
            captureHudPresenter.ConfigureCompletionOverlayFadeForSetup(
                CompletionOverlayFadeSeconds);
            EditorUtility.SetDirty(presenter);
            EditorUtility.SetDirty(captureHudPresenter);
            EditorUtility.SetDirty(cloudServices);
        }

        private static void ValidateCompletionRewardFlow(
            GameObject root,
            LandmarkRevealPresenter presenter)
        {
            ThreatPresenter threatPresenter = root
                .GetComponentInChildren<ThreatPresenter>(true);
            CaptureBoardPresenter captureBoardPresenter = root
                .GetComponentInChildren<CaptureBoardPresenter>(true);
            FeedbackPresenter feedbackPresenter = root
                .GetComponentInChildren<FeedbackPresenter>(true);
            CaptureHudPresenter captureHudPresenter = root
                .GetComponentInChildren<CaptureHudPresenter>(true);
            LevelCoinRewardPresenter rewardPresenter = root
                .GetComponentInChildren<LevelCoinRewardPresenter>(true);
            CoinBalanceHudPresenter balanceHud = root
                .GetComponentInChildren<CoinBalanceHudPresenter>(true);
            CloudServicesBootstrap cloudServices = root
                .GetComponentInChildren<CloudServicesBootstrap>(true);
            if (presenter.ThreatPresenter != threatPresenter
                || presenter.CaptureBoardPresenter != captureBoardPresenter
                || presenter.CompletionFeedbackPresenter != feedbackPresenter
                || captureHudPresenter == null
                || !Mathf.Approximately(
                    captureHudPresenter.CompletionOverlayFadeSeconds,
                    CompletionOverlayFadeSeconds)
                || feedbackPresenter == null
                || feedbackPresenter.CompletionSummaryBackground == null
                || rewardPresenter == null
                || presenter.LevelCoinRewardPresenter != rewardPresenter
                || rewardPresenter.BalanceHud != balanceHud
                || rewardPresenter.CloudServices != cloudServices
                || rewardPresenter.RewardIcon == null
                || rewardPresenter.RewardText == null
                || rewardPresenter.FlightCoinTemplate == null
                || balanceHud == null
                || balanceHud.CloudServices != cloudServices
                || balanceHud.CoinIcon == null
                || balanceHud.BalanceText == null
                || presenter.CompletionArtworkImage == null
                || presenter.ScrimCanvasGroup == null)
            {
                throw new InvalidOperationException(
                    "Completion reward-flow dependencies are not " +
                    "serialized correctly.");
            }
        }

        private static void FinalizeThemeTextSync(
            ThemePresenter themePresenter,
            Transform topHud,
            Transform bottomHud)
        {
            // ThemePresenter re-applies its serialized hudTexts array to a
            // single flat hudTextColor every time ApplyNow() runs, including
            // at real runtime on scene load (OnEnable). That array was
            // frozen by Milestone5SceneSetup before this pass's hero/
            // secondary text hierarchy existed, so left alone it would
            // silently overwrite every deliberately muted/hero HUD text
            // color back to one flat tone. Re-Configure with empty text/
            // accent arrays so this pass's per-element colors (hero
            // percentage, muted secondary labels, subdued blocker icon) are
            // what actually persists, while every other themed reference
            // (background/board/frame/hud panels/threat/barrier/capture/
            // feedback) stays exactly what Milestone5 already wired.
            themePresenter.Configure(
                themePresenter.SelectedTheme,
                themePresenter.FallbackTheme,
                themePresenter.Background,
                themePresenter.BoardSurface,
                themePresenter.BoardFrame,
                new[]
                {
                    topHud.GetComponent<Image>(),
                    bottomHud.GetComponent<Image>(),
                },
                Array.Empty<Graphic>(),
                Array.Empty<Text>(),
                themePresenter.ThreatPresenter,
                themePresenter.BarrierPresenter,
                themePresenter.CapturePresenter,
                themePresenter.FeedbackPresenter);
            EditorUtility.SetDirty(themePresenter);
        }

        private static void HideDebugFooter(Transform bottomHud)
        {
            DebugPointerStatusView debugView =
                bottomHud.GetComponent<DebugPointerStatusView>();
            if (debugView != null)
            {
                debugView.enabled = false;
                EditorUtility.SetDirty(debugView);
            }

            HideRow(bottomHud, "PointerStatus");
            HideRow(bottomHud, "MappingStatus");
        }

        private static void HideRow(Transform parent, string name)
        {
            Transform row = parent.Find(name);
            if (row != null && row.gameObject.activeSelf)
            {
                row.gameObject.SetActive(false);
                EditorUtility.SetDirty(row.gameObject);
            }
        }

        private static void ConfigureGameplayTopHud(
            Transform topHud,
            TopHudAssets assets)
        {
            topHud.gameObject.SetActive(true);

            LayoutElement layout = GetOrAddComponent<LayoutElement>(
                topHud.gameObject);
            layout.minHeight = TopHudMinimumHeight;
            layout.preferredHeight = TopHudPreferredHeight;
            layout.flexibleHeight = 0f;
            EditorUtility.SetDirty(layout);

            HorizontalLayoutGroup outerRow =
                GetOrAddComponent<HorizontalLayoutGroup>(topHud.gameObject);
            outerRow.padding = new RectOffset(10, 10, 6, 6);
            outerRow.spacing = 0f;
            outerRow.childAlignment = TextAnchor.MiddleCenter;
            outerRow.childControlWidth = true;
            outerRow.childControlHeight = true;
            outerRow.childForceExpandWidth = false;
            outerRow.childForceExpandHeight = false;
            EditorUtility.SetDirty(outerRow);

            for (int index = 0; index < topHud.childCount; index++)
            {
                Transform child = topHud.GetChild(index);
                if (child.name == "GameplayHudRow")
                {
                    continue;
                }

                child.gameObject.SetActive(false);
                EditorUtility.SetDirty(child.gameObject);
            }

            RectTransform hudRow = GetOrCreateUiChild(
                topHud,
                "GameplayHudRow");
            hudRow.gameObject.SetActive(true);
            LayoutElement rowLayout = GetOrAddComponent<LayoutElement>(
                hudRow.gameObject);
            rowLayout.minWidth = 0f;
            rowLayout.preferredWidth = 0f;
            rowLayout.flexibleWidth = 1f;
            rowLayout.minHeight = 133f;
            rowLayout.preferredHeight = 138f;
            rowLayout.flexibleHeight = 0f;

            HorizontalLayoutGroup row =
                GetOrAddComponent<HorizontalLayoutGroup>(hudRow.gameObject);
            row.padding = new RectOffset(0, 0, 0, 0);
            row.spacing = 0f;
            // Bottom-aligned: TopHudBar (below) is shorter than hudRow's
            // own reserved height, leaving empty space at the top of
            // hudRow for SettingsSlot (ignoreLayout, anchored top-right)
            // to sit above the bar without overlapping it.
            row.childAlignment = TextAnchor.LowerCenter;
            row.childControlWidth = true;
            row.childControlHeight = true;
            row.childForceExpandWidth = false;
            row.childForceExpandHeight = false;

            // Earlier presentation passes built separate per-value plaques
            // ("HealthColumn"/"CutColumn"/"SpeedColumn", plus the even
            // older "ScoreColumn"/"CoinColumn") each with their own
            // background image at their own width. All superseded by the
            // single full-width "TopHudBar" below -- discard them outright
            // so they don't linger as active, unused HorizontalLayoutGroup
            // children.
            DestroyLegacyChild(hudRow, "HealthColumn");
            DestroyLegacyChild(hudRow, "CutColumn");
            DestroyLegacyChild(hudRow, "SpeedColumn");
            DestroyLegacyChild(hudRow, "ScoreColumn");
            DestroyLegacyChild(hudRow, "CoinColumn");

            RectTransform bar = GetOrCreateUiChild(hudRow, "TopHudBar");
            bar.gameObject.SetActive(true);
            LayoutElement barLayout = GetOrAddComponent<LayoutElement>(
                bar.gameObject);
            barLayout.minWidth = 0f;
            barLayout.preferredWidth = 0f;
            barLayout.flexibleWidth = 1f;
            barLayout.minHeight = TopHudBarHeight;
            barLayout.preferredHeight = TopHudBarHeight;
            barLayout.flexibleHeight = 0f;

            // Sliced (not Simple): the bar spans the full row width, far
            // wider than BigHUDBackground's own native aspect at
            // TopHudBarHeight, so only the flat middle stretches to fill
            // it -- the rounded plaque border stays crisp instead of
            // visibly distorting. Requires the sprite's import border to
            // be set (see EnsureUiSpriteImportSettings' sliced9Slice
            // option); a zero border falls back to a plain uniform
            // stretch of the whole sprite.
            Image barImage = GetOrAddComponent<Image>(bar.gameObject);
            barImage.sprite = assets.Background;
            barImage.type = Image.Type.Sliced;
            barImage.color = Color.white;
            barImage.raycastTarget = false;

            // Health/Cut/Speed are regions layered directly on the one
            // shared bar (left/center/right thirds) instead of each
            // owning a separate background plaque. Health renders as a live
            // heart row (HealthHudPresenter) instead of an icon+text pair.
            RectTransform healthRegion = ConfigureHealthRegion(
                bar,
                new Vector2(0.02f, 0f),
                new Vector2(0.30f, 1f));
            RectTransform cutRegion = ConfigureTopHudRegion(
                bar,
                "CutHUD",
                new Vector2(0.32f, 0f),
                new Vector2(0.68f, 1f),
                null,
                assets.Font,
                "CUT: 0/0",
                40,
                new Vector2(0.06f, 0.08f),
                new Vector2(0.94f, 0.92f));
            RectTransform speedRegion = ConfigureTopHudRegion(
                bar,
                "SpeedHUD",
                new Vector2(0.70f, 0f),
                new Vector2(0.98f, 1f),
                assets.SpeedIcon,
                assets.Font,
                "0.0",
                44,
                new Vector2(0.34f, 0.12f),
                new Vector2(0.94f, 0.88f));
            // Hand-tuned to make room for the speedometer icon: a slightly
            // smaller, differently placed icon and a right-anchored,
            // fixed-size value label instead of the generic stretch layout
            // ConfigureTopHudRegion just applied above. Re-applied every
            // pass so it survives a re-run instead of only living in the
            // Editor's last save.
            RectTransform speedIconRect = speedRegion.Find("Icon")
                as RectTransform;
            if (speedIconRect != null)
            {
                speedIconRect.anchorMin = new Vector2(0.5f, 0.5f);
                speedIconRect.anchorMax = new Vector2(0.5f, 0.5f);
                speedIconRect.pivot = new Vector2(0.5f, 0.5f);
                speedIconRect.localScale = new Vector3(0.9f, 0.9f, 1f);
                speedIconRect.anchoredPosition = new Vector2(0f, 5f);
            }

            RectTransform speedValueRect = speedRegion.Find("ValueText")
                as RectTransform;
            if (speedValueRect != null)
            {
                speedValueRect.anchorMin = new Vector2(1f, 0.12f);
                speedValueRect.anchorMax = new Vector2(1f, 0.88f);
                speedValueRect.pivot = new Vector2(1f, 0.5f);
                speedValueRect.sizeDelta = new Vector2(159f, 10f);
                speedValueRect.anchoredPosition = new Vector2(20f, 5f);
            }

            healthRegion.SetSiblingIndex(0);
            cutRegion.SetSiblingIndex(1);
            speedRegion.SetSiblingIndex(2);

            RectTransform settingsSlot = ConfigureSettingsSlot(
                hudRow,
                assets.Settings);
            RectTransform coinSlot = ConfigureCoinBalanceSlotVisuals(
                hudRow,
                assets.Coin,
                assets.Font);
            coinSlot.SetSiblingIndex(1);
            settingsSlot.SetSiblingIndex(2);
            bar.SetSiblingIndex(0);

            EditorUtility.SetDirty(rowLayout);
            EditorUtility.SetDirty(row);
            EditorUtility.SetDirty(barLayout);
            EditorUtility.SetDirty(barImage);
            EditorUtility.SetDirty(hudRow.gameObject);
            EditorUtility.SetDirty(topHud.gameObject);
        }

        // The live heart row itself (HealthHudPresenter, wired by
        // GameplayProgressionSetup) is a runtime concern -- this only
        // prepares the named "HeartRow" container it populates, and clears
        // out the old icon+text pair a previous setup pass may have left
        // behind here.
        private static RectTransform ConfigureHealthRegion(
            RectTransform bar,
            Vector2 anchorMin,
            Vector2 anchorMax)
        {
            RectTransform region = GetOrCreateUiChild(bar, "HealthHUD");
            region.gameObject.SetActive(true);
            region.anchorMin = anchorMin;
            region.anchorMax = anchorMax;
            region.pivot = new Vector2(0.5f, 0.5f);
            region.offsetMin = Vector2.zero;
            region.offsetMax = Vector2.zero;

            string[] staleNames = { "Icon", "ValueText", "ShadowText" };
            for (int index = 0; index < staleNames.Length; index++)
            {
                Transform stale = region.Find(staleNames[index]);
                if (stale != null)
                {
                    UnityEngine.Object.DestroyImmediate(stale.gameObject);
                }
            }

            RectTransform heartRow = GetOrCreateUiChild(region, "HeartRow");
            StretchToParent(heartRow);
            HorizontalLayoutGroup layout = GetOrAddComponent<HorizontalLayoutGroup>(
                heartRow.gameObject);
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.spacing = 4f;
            // Hand-tuned left inset so the hearts don't hug the bar's left
            // edge -- re-applied every pass so it survives a re-run.
            layout.padding = new RectOffset(30, 0, 0, 0);
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            EditorUtility.SetDirty(layout);
            return region;
        }

        private static RectTransform ConfigureTopHudRegion(
            RectTransform bar,
            string name,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Sprite iconSprite,
            TMP_FontAsset font,
            string value,
            int fontSize,
            Vector2 textAnchorMinimum,
            Vector2 textAnchorMaximum)
        {
            RectTransform region = GetOrCreateUiChild(bar, name);
            region.gameObject.SetActive(true);
            region.anchorMin = anchorMin;
            region.anchorMax = anchorMax;
            region.pivot = new Vector2(0.5f, 0.5f);
            region.offsetMin = Vector2.zero;
            region.offsetMax = Vector2.zero;

            if (iconSprite != null)
            {
                RectTransform iconRect = GetOrCreateUiChild(region, "Icon");
                iconRect.gameObject.SetActive(true);
                float iconSize = TopHudBarHeight * TopHudIconSizeMultiplier;
                iconRect.anchorMin = new Vector2(0f, 0.5f);
                iconRect.anchorMax = new Vector2(0f, 0.5f);
                iconRect.pivot = new Vector2(0f, 0.5f);
                iconRect.anchoredPosition = new Vector2(
                    TopHudBarHeight * 0.06f,
                    0f);
                iconRect.sizeDelta = new Vector2(iconSize, iconSize);
                Image iconImage = GetOrAddComponent<Image>(iconRect.gameObject);
                iconImage.sprite = iconSprite;
                iconImage.type = Image.Type.Simple;
                iconImage.color = Color.white;
                iconImage.preserveAspect = true;
                iconImage.raycastTarget = false;
                iconRect.SetSiblingIndex(0);
                EditorUtility.SetDirty(iconImage);
            }
            else
            {
                Transform staleIcon = region.Find("Icon");
                if (staleIcon != null)
                {
                    staleIcon.gameObject.SetActive(false);
                    EditorUtility.SetDirty(staleIcon.gameObject);
                }
            }

            // No shadow copy anymore -- the owner removed it by hand for a
            // flatter look; keep this idempotent-clean rather than
            // recreating it if an older scene still has one.
            Transform staleShadow = region.Find("ShadowText");
            if (staleShadow != null)
            {
                UnityEngine.Object.DestroyImmediate(staleShadow.gameObject);
            }

            RectTransform textRect = GetOrCreateUiChild(region, "ValueText");
            TextMeshProUGUI text = ConfigureTopHudTextLayer(
                textRect,
                font,
                value,
                fontSize,
                TopHudTextBrown,
                textAnchorMinimum,
                textAnchorMaximum,
                Vector2.zero);
            textRect.SetSiblingIndex(iconSprite != null ? 1 : 0);

            EditorUtility.SetDirty(text);
            return region;
        }

        private static TextMeshProUGUI ConfigureTopHudTextLayer(
            RectTransform rect,
            TMP_FontAsset font,
            string value,
            int fontSize,
            Color color,
            Vector2 anchorMinimum,
            Vector2 anchorMaximum,
            Vector2 offset)
        {
            Text legacyText = rect.GetComponent<Text>();
            if (legacyText != null)
            {
                UnityEngine.Object.DestroyImmediate(legacyText);
            }

            Shadow legacyShadow = rect.GetComponent<Shadow>();
            if (legacyShadow != null)
            {
                UnityEngine.Object.DestroyImmediate(legacyShadow);
            }

            rect.anchorMin = anchorMinimum;
            rect.anchorMax = anchorMaximum;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.anchoredPosition = offset;
            TextMeshProUGUI text = GetOrAddComponent<TextMeshProUGUI>(
                rect.gameObject);
            text.font = font;
            text.text = value;
            text.fontSize = fontSize;
            text.fontStyle = FontStyles.Normal;
            text.alignment = TextAlignmentOptions.Center;
            text.color = color;
            text.raycastTarget = false;
            text.enableAutoSizing = true;
            text.fontSizeMin = 20f;
            text.fontSizeMax = fontSize;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Overflow;
            text.richText = false;
            return text;
        }

        private static RectTransform ConfigureSettingsSlot(
            RectTransform hudRow,
            Sprite settingsSprite)
        {
            RectTransform slot = GetOrCreateUiChild(hudRow, "SettingsSlot");
            slot.gameObject.SetActive(true);
            slot.anchorMin = new Vector2(1f, 1f);
            slot.anchorMax = new Vector2(1f, 1f);
            slot.pivot = new Vector2(1f, 1f);
            slot.anchoredPosition = new Vector2(-8f, 0f);
            slot.sizeDelta = new Vector2(TopHudSettingsSize, TopHudSettingsSize);
            // ignoreLayout: sits above TopHudBar in the space hudRow's own
            // bottom-aligned row leaves free, instead of competing with
            // the bar for a HorizontalLayoutGroup column of its own.
            LayoutElement slotLayout = GetOrAddComponent<LayoutElement>(
                slot.gameObject);
            slotLayout.ignoreLayout = true;

            RectTransform buttonRect = GetOrCreateUiChild(slot, "SettingsButton");
            buttonRect.gameObject.SetActive(true);
            buttonRect.anchorMin = Vector2.zero;
            buttonRect.anchorMax = Vector2.one;
            buttonRect.pivot = new Vector2(0.5f, 0.5f);
            buttonRect.offsetMin = Vector2.zero;
            buttonRect.offsetMax = Vector2.zero;

            Image image = GetOrAddComponent<Image>(buttonRect.gameObject);
            image.sprite = settingsSprite;
            image.type = Image.Type.Simple;
            image.color = Color.white;
            image.preserveAspect = true;
            image.raycastTarget = true;
            Button button = GetOrAddComponent<Button>(buttonRect.gameObject);
            button.targetGraphic = image;
            button.transition = Selectable.Transition.None;
            button.interactable = true;
            Navigation navigation = button.navigation;
            navigation.mode = Navigation.Mode.None;
            button.navigation = navigation;

            EditorUtility.SetDirty(slotLayout);
            EditorUtility.SetDirty(image);
            EditorUtility.SetDirty(button);
            return slot;
        }

        internal static RectTransform ConfigureCoinBalanceSlotVisuals(
            RectTransform hudRow,
            Sprite coinSprite,
            TMP_FontAsset font,
            Vector2? anchoredPosition = null,
            Color? textColor = null)
        {
            RectTransform slot = GetOrCreateUiChild(
                hudRow,
                "CoinBalanceSlot");
            slot.gameObject.SetActive(true);
            slot.anchorMin = new Vector2(0f, 1f);
            slot.anchorMax = new Vector2(0f, 1f);
            slot.pivot = new Vector2(0f, 1f);
            slot.anchoredPosition = anchoredPosition ?? new Vector2(8f, 0f);
            slot.sizeDelta = new Vector2(220f, 56f);
            LayoutElement slotLayout = GetOrAddComponent<LayoutElement>(
                slot.gameObject);
            slotLayout.ignoreLayout = true;

            RectTransform iconRect = GetOrCreateUiChild(slot, "CoinIcon");
            iconRect.anchorMin = new Vector2(0f, 0.5f);
            iconRect.anchorMax = new Vector2(0f, 0.5f);
            iconRect.pivot = new Vector2(0f, 0.5f);
            iconRect.anchoredPosition = Vector2.zero;
            iconRect.sizeDelta = new Vector2(56f, 56f);
            Image icon = GetOrAddComponent<Image>(iconRect.gameObject);
            icon.sprite = coinSprite;
            icon.type = Image.Type.Simple;
            icon.color = Color.white;
            icon.preserveAspect = true;
            icon.raycastTarget = false;

            RectTransform textRect = GetOrCreateUiChild(
                slot,
                "BalanceText");
            textRect.anchorMin = new Vector2(0f, 0f);
            textRect.anchorMax = new Vector2(1f, 1f);
            textRect.pivot = new Vector2(0.5f, 0.5f);
            textRect.offsetMin = new Vector2(64f, 0f);
            textRect.offsetMax = Vector2.zero;
            TextMeshProUGUI text = GetOrAddComponent<TextMeshProUGUI>(
                textRect.gameObject);
            text.font = font;
            text.text = "0";
            text.fontSize = 38f;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.MidlineLeft;
            text.color = textColor ?? Color.white;
            text.outlineColor = textColor.HasValue
                ? CoinBalanceBrownOutline
                : TopHudTextBrown;
            text.outlineWidth = 0.18f;
            text.enableAutoSizing = true;
            text.fontSizeMin = 22f;
            text.fontSizeMax = 38f;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Overflow;
            text.raycastTarget = false;

            EditorUtility.SetDirty(slotLayout);
            EditorUtility.SetDirty(icon);
            EditorUtility.SetDirty(text);
            return slot;
        }

        internal static CoinBalanceHudPresenter ConfigureCoinBalanceSlot(
            RectTransform hudRow,
            Sprite coinSprite,
            TMP_FontAsset font,
            CloudServicesBootstrap cloudServices,
            Vector2? anchoredPosition = null,
            Color? textColor = null)
        {
            RectTransform slot = ConfigureCoinBalanceSlotVisuals(
                hudRow,
                coinSprite,
                font,
                anchoredPosition,
                textColor);
            Image icon = RequireChild(slot, "CoinIcon").GetComponent<Image>();
            TextMeshProUGUI text = RequireChild(slot, "BalanceText")
                .GetComponent<TextMeshProUGUI>();
            CoinBalanceHudPresenter presenter = GetOrAddComponent<
                CoinBalanceHudPresenter>(slot.gameObject);
            Undo.RecordObject(presenter, "Wire Coin Balance HUD");
            presenter.ConfigureForSetup(cloudServices, icon, text);
            EditorUtility.SetDirty(presenter);
            return presenter;
        }

        private static LevelCoinRewardPresenter ConfigureLevelCoinRewardOverlay(
            Transform safeArea,
            FirstPlayableController controller,
            CloudServicesBootstrap cloudServices,
            FeedbackAudioPresenter feedbackAudio,
            CoinBalanceHudPresenter balanceHud,
            Sprite coinSprite,
            TMP_FontAsset font)
        {
            RectTransform overlay = GetOrCreateUiChild(
                safeArea,
                "LevelCoinRewardOverlay");
            overlay.gameObject.SetActive(true);
            StretchToParent(overlay);
            LayoutElement overlayLayout = GetOrAddComponent<LayoutElement>(
                overlay.gameObject);
            overlayLayout.ignoreLayout = true;

            // Anchored inside the same card as the clean-board stats block,
            // hanging directly off the stats list's own content-fit bottom
            // edge (see ConfigureFeedbackReadabilityForSetup ->
            // ConfigureCompletionSummaryListForSetup's CompletionSummary-
            // ContentHeight/CompletionRewardRowGapFromStats) so the reward
            // reads as that same summary's last item with no leftover gap,
            // rather than a fixed band edge unrelated to the rows' actual
            // height. No background image of its own -- the shared card
            // behind the stats text shows through.
            RectTransform container = GetOrCreateUiChild(
                overlay,
                "RewardContainer");
            container.anchorMin = new Vector2(0.04f, 0.63f);
            container.anchorMax = new Vector2(0.96f, 0.63f);
            container.pivot = new Vector2(0.5f, 1f);
            container.anchoredPosition = new Vector2(
                0f,
                -(CompletionSummaryContentHeight
                    + CompletionRewardRowGapFromStats));
            container.sizeDelta = new Vector2(0f, 108f);
            Image background = GetOrAddComponent<Image>(container.gameObject);
            background.sprite = null;
            background.type = Image.Type.Simple;
            background.color = Color.clear;
            background.raycastTarget = false;
            CanvasGroup rewardGroup = GetOrAddComponent<CanvasGroup>(
                container.gameObject);
            rewardGroup.alpha = 0f;
            rewardGroup.interactable = false;
            rewardGroup.blocksRaycasts = false;

            // One-time migration: earlier setup runs parented the icon and
            // text directly under RewardContainer. Remove them there so
            // they don't linger as a second, stale coin/amount duplicate
            // once rebuilt inside RewardContent below.
            RemoveStaleChild(container, "RewardCoinIcon");
            RemoveStaleChild(container, "RewardAmountText");

            // Icon and text sit together as one tight, centered group (a
            // HorizontalLayoutGroup content-fit to its children) rather than
            // the icon pinned to the row's left edge with the text centered
            // across the whole (wide) row -- that read as two unrelated
            // pieces instead of "[coin] +100 COINS".
            RectTransform contentRect = GetOrCreateUiChild(
                container,
                "RewardContent");
            contentRect.anchorMin = new Vector2(0.5f, 0.5f);
            contentRect.anchorMax = new Vector2(0.5f, 0.5f);
            contentRect.pivot = new Vector2(0.5f, 0.5f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.localScale = Vector3.one;
            HorizontalLayoutGroup contentLayout = GetOrAddComponent<
                HorizontalLayoutGroup>(contentRect.gameObject);
            contentLayout.spacing = 14f;
            contentLayout.childAlignment = TextAnchor.MiddleLeft;
            contentLayout.childControlWidth = true;
            contentLayout.childControlHeight = true;
            contentLayout.childForceExpandWidth = false;
            contentLayout.childForceExpandHeight = false;
            ContentSizeFitter contentFitter = GetOrAddComponent<
                ContentSizeFitter>(contentRect.gameObject);
            contentFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            RectTransform iconRect = GetOrCreateUiChild(
                contentRect,
                "RewardCoinIcon");
            iconRect.localScale = Vector3.one;
            LayoutElement iconLayout = GetOrAddComponent<LayoutElement>(
                iconRect.gameObject);
            iconLayout.preferredWidth = 80f;
            iconLayout.preferredHeight = 80f;
            Image rewardIcon = GetOrAddComponent<Image>(iconRect.gameObject);
            rewardIcon.sprite = coinSprite;
            rewardIcon.type = Image.Type.Simple;
            rewardIcon.color = Color.white;
            rewardIcon.preserveAspect = true;
            rewardIcon.raycastTarget = false;

            RectTransform amountRect = GetOrCreateUiChild(
                contentRect,
                "RewardAmountText");
            amountRect.localScale = Vector3.one;
            TextMeshProUGUI amountText = GetOrAddComponent<TextMeshProUGUI>(
                amountRect.gameObject);
            amountText.font = font;
            amountText.text = "+100 COINS";
            amountText.fontSize = 48f;
            amountText.enableAutoSizing = false;
            amountText.fontStyle = FontStyles.Bold;
            amountText.alignment = TextAlignmentOptions.MidlineLeft;
            amountText.color = Color.white;
            amountText.outlineColor = TopHudTextBrown;
            amountText.outlineWidth = 0.16f;
            amountText.textWrappingMode = TextWrappingModes.NoWrap;
            amountText.raycastTarget = false;

            RectTransform flightRoot = GetOrCreateUiChild(
                overlay,
                "CoinFlightRoot");
            StretchToParent(flightRoot);
            flightRoot.SetAsLastSibling();
            RectTransform templateRect = GetOrCreateUiChild(
                flightRoot,
                "RewardFlightCoinTemplate");
            templateRect.anchorMin = new Vector2(0.5f, 0.5f);
            templateRect.anchorMax = new Vector2(0.5f, 0.5f);
            templateRect.pivot = new Vector2(0.5f, 0.5f);
            templateRect.anchoredPosition = Vector2.zero;
            templateRect.sizeDelta = new Vector2(54f, 54f);
            Image template = GetOrAddComponent<Image>(templateRect.gameObject);
            template.sprite = coinSprite;
            template.type = Image.Type.Simple;
            template.color = Color.white;
            template.preserveAspect = true;
            template.raycastTarget = false;
            template.gameObject.SetActive(false);

            LevelCoinRewardPresenter presenter = GetOrAddComponent<
                LevelCoinRewardPresenter>(overlay.gameObject);
            Undo.RecordObject(presenter, "Wire Level Coin Reward");
            presenter.ConfigureForSetup(
                controller,
                cloudServices,
                feedbackAudio,
                balanceHud,
                rewardGroup,
                rewardIcon,
                amountText,
                flightRoot,
                template,
                flightCoinCount: 7,
                revealDelaySeconds: CompletionRewardRevealDelaySeconds);

            Transform completion = RequireChild(
                safeArea,
                "LevelCompleteOverlay");
            overlay.SetSiblingIndex(Mathf.Max(
                0,
                completion.GetSiblingIndex()));
            completion.SetAsLastSibling();

            EditorUtility.SetDirty(overlayLayout);
            EditorUtility.SetDirty(background);
            EditorUtility.SetDirty(rewardGroup);
            EditorUtility.SetDirty(rewardIcon);
            EditorUtility.SetDirty(amountText);
            EditorUtility.SetDirty(template);
            EditorUtility.SetDirty(presenter);
            return presenter;
        }

        private static void ConfigureResponsiveGameplayBands(
            Transform safeArea)
        {
            VerticalLayoutGroup safeLayout =
                GetOrAddComponent<VerticalLayoutGroup>(safeArea.gameObject);
            safeLayout.padding = new RectOffset(
                SafeAreaHorizontalPadding,
                SafeAreaHorizontalPadding,
                SafeAreaVerticalPadding,
                SafeAreaVerticalPadding);
            safeLayout.spacing = SafeAreaSectionSpacing;
            safeLayout.childAlignment = TextAnchor.UpperCenter;
            safeLayout.childControlWidth = true;
            safeLayout.childControlHeight = true;
            safeLayout.childForceExpandWidth = true;
            safeLayout.childForceExpandHeight = false;

            Transform topHud = RequireChild(safeArea, "TopHUD");
            LayoutElement topLayout = GetOrAddComponent<LayoutElement>(
                topHud.gameObject);
            topLayout.minHeight = TopHudMinimumHeight;
            topLayout.preferredHeight = TopHudPreferredHeight;
            topLayout.flexibleHeight = 0f;

            Transform boardStage = RequireChild(safeArea, "BoardStage");
            LayoutElement stageLayout = GetOrAddComponent<LayoutElement>(
                boardStage.gameObject);
            stageLayout.minHeight = 320f;
            stageLayout.preferredHeight = 0f;
            stageLayout.flexibleHeight = 1f;

            Transform bottomHud = RequireChild(safeArea, "BottomHUD");
            LayoutElement bottomLayout = GetOrAddComponent<LayoutElement>(
                bottomHud.gameObject);
            bottomLayout.minHeight = BottomHudMinimumHeight;
            bottomLayout.preferredHeight = BottomHudPreferredHeight;
            bottomLayout.flexibleHeight = 0f;

            VerticalLayoutGroup bottomColumn =
                GetOrAddComponent<VerticalLayoutGroup>(bottomHud.gameObject);
            bottomColumn.padding = new RectOffset(
                BottomHudPadding,
                BottomHudPadding,
                BottomHudPadding,
                BottomHudPadding);
            bottomColumn.spacing = 0f;
            bottomColumn.childAlignment = TextAnchor.MiddleCenter;
            bottomColumn.childControlWidth = true;
            bottomColumn.childControlHeight = true;
            bottomColumn.childForceExpandWidth = false;
            bottomColumn.childForceExpandHeight = false;

            Transform failureOverlay = safeArea.Find(
                "CutLimitFailureOverlay");
            LayoutElement failureLayout = null;
            if (failureOverlay != null)
            {
                failureLayout = GetOrAddComponent<LayoutElement>(
                    failureOverlay.gameObject);
                failureLayout.ignoreLayout = true;
                StretchToParent((RectTransform)failureOverlay);
            }

            EditorUtility.SetDirty(safeLayout);
            EditorUtility.SetDirty(topLayout);
            EditorUtility.SetDirty(stageLayout);
            EditorUtility.SetDirty(bottomLayout);
            EditorUtility.SetDirty(bottomColumn);
            if (failureLayout != null)
            {
                EditorUtility.SetDirty(failureLayout);
            }
        }

        private static void ConfigureMinimalBottomHud(
            GameObject root,
            Transform safeArea,
            FirstPlayableController controller,
            ProgressSprites sprites,
            out SandProgressPresenter presenter,
            out RectTransform fillStartTarget)
        {
            Transform bottomHud = RequireChild(safeArea, "BottomHUD");
            bottomHud.gameObject.SetActive(true);

            LayoutElement bottomLayout = bottomHud.GetComponent<LayoutElement>();
            bottomLayout.minHeight = BottomHudMinimumHeight;
            bottomLayout.preferredHeight = BottomHudPreferredHeight;
            bottomLayout.flexibleHeight = 0f;
            EditorUtility.SetDirty(bottomLayout);

            VerticalLayoutGroup bottomColumn =
                bottomHud.GetComponent<VerticalLayoutGroup>();
            if (bottomColumn != null)
            {
                bottomColumn.padding = new RectOffset(
                    BottomHudPadding,
                    BottomHudPadding,
                    BottomHudPadding,
                    BottomHudPadding);
                bottomColumn.spacing = 0f;
                bottomColumn.childAlignment = TextAnchor.MiddleCenter;
                EditorUtility.SetDirty(bottomColumn);
            }

            Transform powerControls = RequireChild(safeArea, "PowerControls");
            powerControls.gameObject.SetActive(false);
            EditorUtility.SetDirty(powerControls.gameObject);

            HideLegacyGameplayElement(bottomHud, "PowerRow");
            HideLegacyGameplayElement(bottomHud, "SandBowl");
            HideLegacyGameplayElement(bottomHud, "BowlTargetText");
            HideLegacyGameplayElement(bottomHud, "QuickRetryButton");
            HideRow(bottomHud, "PointerStatus");
            HideRow(bottomHud, "MappingStatus");

            // The Cut counter now lives inside the TopHUD Cut panel (see
            // GameplayProgressionSetup.ConfigureIdentityHud); an earlier
            // presentation pass left a "CutLimitCounter" text element as a
            // direct BottomHUD child, which Milestone2SceneSetup's baseline
            // validation rejects (every BottomHUD child must carry an
            // explicit non-flexible LayoutElement). Discard it outright
            // rather than just hiding it, matching how ConfigureLandmarkLayer
            // already discards other legacy layouts from earlier passes.
            DestroyLegacyChild(bottomHud, "CutLimitCounter");

            // An earlier presentation pass parented "ProgressBar" directly
            // under BottomHUD; it now lives under BottomHudRow/ProgressSlot
            // instead. Discard the old direct child outright so it doesn't
            // leave a second, orphaned SandProgressPresenter behind.
            DestroyLegacyChild(bottomHud, "ProgressBar");

            // BottomHudRow is the single visible row in BottomHUD's vertical
            // flow, split into a left ProgressSlot and a right SkillRow at
            // equal (50/50) width via HorizontalLayoutGroup flexible
            // weights -- not fixed pixel widths, so the split stays even
            // across phone/tablet aspect ratios.
            RectTransform bottomRow = GetOrCreateUiChild(
                bottomHud,
                "BottomHudRow");
            bottomRow.gameObject.SetActive(true);
            bottomRow.SetAsFirstSibling();
            // A fixed (non-flexible) height, mirroring GameplayHudRow in
            // TopHUD: BottomHUD's own VerticalLayoutGroup does not force-
            // expand child height, and Milestone2SceneSetup's baseline
            // validation requires every direct BottomHUD child to carry an
            // explicit non-flexible LayoutElement.
            float bottomRowHeight = Mathf.Max(
                0f,
                BottomHudPreferredHeight - (BottomHudPadding * 2f));
            LayoutElement bottomRowLayout = GetOrAddComponent<LayoutElement>(
                bottomRow.gameObject);
            bottomRowLayout.minWidth = 0f;
            bottomRowLayout.preferredWidth = 0f;
            bottomRowLayout.flexibleWidth = 1f;
            bottomRowLayout.minHeight = bottomRowHeight;
            bottomRowLayout.preferredHeight = bottomRowHeight;
            bottomRowLayout.flexibleHeight = 0f;

            HorizontalLayoutGroup bottomRowGroup =
                GetOrAddComponent<HorizontalLayoutGroup>(bottomRow.gameObject);
            bottomRowGroup.padding = new RectOffset(0, 0, 0, 0);
            bottomRowGroup.spacing = 16f;
            bottomRowGroup.childAlignment = TextAnchor.MiddleCenter;
            bottomRowGroup.childControlWidth = true;
            bottomRowGroup.childControlHeight = true;
            bottomRowGroup.childForceExpandWidth = true;
            bottomRowGroup.childForceExpandHeight = true;

            RectTransform progressSlot = GetOrCreateUiChild(
                bottomRow,
                "ProgressSlot");
            progressSlot.gameObject.SetActive(true);
            progressSlot.SetSiblingIndex(0);
            LayoutElement progressSlotLayout = GetOrAddComponent<LayoutElement>(
                progressSlot.gameObject);
            progressSlotLayout.minWidth = 0f;
            progressSlotLayout.preferredWidth = 0f;
            progressSlotLayout.flexibleWidth = 1f;
            progressSlotLayout.minHeight = 0f;
            progressSlotLayout.preferredHeight = 0f;
            progressSlotLayout.flexibleHeight = 1f;

            RectTransform skillRow = GetOrCreateUiChild(bottomRow, "SkillRow");
            skillRow.gameObject.SetActive(true);
            skillRow.SetSiblingIndex(1);
            LayoutElement skillRowLayout = GetOrAddComponent<LayoutElement>(
                skillRow.gameObject);
            skillRowLayout.minWidth = 0f;
            skillRowLayout.preferredWidth = 0f;
            skillRowLayout.flexibleWidth = 1f;
            skillRowLayout.minHeight = 0f;
            skillRowLayout.preferredHeight = 0f;
            skillRowLayout.flexibleHeight = 1f;

            RectTransform progressRect = GetOrCreateUiChild(
                progressSlot,
                "ProgressBar");
            progressRect.gameObject.SetActive(true);

            // A transparent Graphic makes the whole bar/text footprint a UI
            // raycast target. This protects the gesture layer even though the
            // visible child art itself does not consume raycasts.
            Image inputBlocker = GetOrAddComponent<Image>(progressRect.gameObject);
            inputBlocker.sprite = null;
            inputBlocker.color = new Color(0f, 0f, 0f, 0f);
            inputBlocker.raycastTarget = true;
            EditorUtility.SetDirty(inputBlocker);

            RectTransform backgroundRect = GetOrCreateUiChild(
                progressRect,
                "Background");
            Image backgroundImage = GetOrAddComponent<Image>(
                backgroundRect.gameObject);
            backgroundImage.sprite = sprites.Background;
            backgroundImage.type = Image.Type.Sliced;
            backgroundImage.color = Color.white;
            backgroundImage.raycastTarget = false;
            backgroundRect.SetSiblingIndex(0);

            RectTransform fillMaskRect = GetOrCreateUiChild(
                progressRect,
                "FillMask");
            RectMask2D fillMask = GetOrAddComponent<RectMask2D>(
                fillMaskRect.gameObject);
            fillMask.padding = Vector4.zero;
            fillMask.softness = Vector2Int.zero;
            fillMaskRect.SetSiblingIndex(1);

            RectTransform fillRect = GetOrCreateUiChild(fillMaskRect, "Fill");
            Image fillImage = GetOrAddComponent<Image>(fillRect.gameObject);
            fillImage.sprite = sprites.Fill;
            fillImage.type = Image.Type.Sliced;
            fillImage.color = Color.white;
            fillImage.raycastTarget = false;
            fillRect.SetSiblingIndex(0);

            // No visible star anymore -- FillStartTarget is still the sand
            // grains' flight destination (LandmarkRevealPresenter reads it
            // by reference), it just marks the fill's leading inner edge
            // with no artwork of its own now.
            fillStartTarget = GetOrCreateUiChild(
                fillMaskRect,
                "FillStartTarget");
            fillStartTarget.anchorMin = new Vector2(0f, 0.5f);
            fillStartTarget.anchorMax = new Vector2(0f, 0.5f);
            fillStartTarget.pivot = new Vector2(0.5f, 0.5f);
            fillStartTarget.anchoredPosition = Vector2.zero;
            fillStartTarget.sizeDelta = Vector2.zero;

            RectTransform textRect = GetOrCreateUiChild(
                progressRect,
                "ProgressText");
            Text progressText = ConfigureText(
                textRect,
                "0% / 0%",
                27,
                TextAnchor.MiddleCenter,
                new Color(0.98f, 0.94f, 0.84f, 1f));
            progressText.fontStyle = FontStyle.Bold;
            progressText.raycastTarget = false;
            textRect.SetAsLastSibling();

            presenter = GetOrAddComponent<SandProgressPresenter>(
                progressRect.gameObject);
            presenter.Configure(
                controller,
                progressRect,
                backgroundImage,
                fillMaskRect,
                fillImage,
                progressText,
                fillStartTarget);
            presenter.ConfigureAnimationForSetup(
                animationSeconds: 0.48f,
                arrivalFallbackSeconds: 0.85f);

            EditorUtility.SetDirty(bottomRowLayout);
            EditorUtility.SetDirty(bottomRowGroup);
            EditorUtility.SetDirty(progressSlotLayout);
            EditorUtility.SetDirty(skillRowLayout);
            EditorUtility.SetDirty(backgroundImage);
            EditorUtility.SetDirty(fillMask);
            EditorUtility.SetDirty(fillImage);
            EditorUtility.SetDirty(progressText);
            EditorUtility.SetDirty(presenter);
        }

        // SkillRow owns its own Freeze/Instant/Gravity GameObjects outright --
        // it does NOT reparent Milestone6SceneSetup's PowerControls buttons
        // (an earlier version did, but that made re-running this pass
        // non-idempotent: PowerControls' own GetOrCreateUiChild lookup no
        // longer found the moved-away button on the next run, so
        // Milestone6SceneSetup created a fresh duplicate pair every time,
        // and the old pair was never removed from SkillRow -- multiple
        // Apply() runs visibly multiplied the skill icons). Milestone6's
        // PowerControls buttons stay where they are (inert, inactive,
        // harmless) purely so that setup remains usable standalone; this
        // method builds/reuses its own named children of SkillRow and
        // clears any stray leftovers first so a scene that already picked
        // up duplicates from the old behavior self-heals on the next run.
        private static void ConfigureBottomHudSkillRow(
            Transform safeArea,
            FirstPlayableController controller,
            PowerHudPresenter presenter,
            SkillAssets skillAssets)
        {
            if (presenter == null)
            {
                throw new InvalidOperationException(
                    "Bottom HUD skill row requires a PowerHudPresenter " +
                    "created by Milestone 6 setup.");
            }

            Transform bottomHud = RequireChild(safeArea, "BottomHUD");
            Transform bottomRow = RequireChild(bottomHud, "BottomHudRow");
            RectTransform skillRow =
                (RectTransform)RequireChild(bottomRow, "SkillRow");

            // Discard every existing child unconditionally, not just ones
            // with an unexpected name -- a scene that already picked up
            // same-named duplicates from the old reparenting behavior would
            // otherwise keep all of them, since each individual name still
            // matches one of the three expected slots. The three slots
            // below are rebuilt fresh immediately after.
            for (int index = skillRow.childCount - 1; index >= 0; index--)
            {
                UnityEngine.Object.DestroyImmediate(
                    skillRow.GetChild(index).gameObject);
            }

            // Right-aligned with a right inset so the last skill's edge
            // lands at SkillRow's own right edge (mirroring the progress
            // bar's flush-left placement on the other BottomHudRow half)
            // instead of the three icons floating small and centered with
            // unused space on both sides.
            HorizontalLayoutGroup skillLayout =
                GetOrAddComponent<HorizontalLayoutGroup>(skillRow.gameObject);
            skillLayout.padding = new RectOffset(
                0,
                (int)SkillRowRightInset,
                0,
                0);
            skillLayout.spacing = 14f;
            skillLayout.childAlignment = TextAnchor.MiddleRight;
            skillLayout.childControlWidth = true;
            skillLayout.childControlHeight = true;
            skillLayout.childForceExpandWidth = false;
            skillLayout.childForceExpandHeight = false;

            RectTransform freezeRoot = GetOrCreateUiChild(
                skillRow,
                "FreezePulseButton");
            freezeRoot.gameObject.SetActive(true);
            Button freezeButton = GetOrAddComponent<Button>(
                freezeRoot.gameObject);
            Text freezeCharges = GetOrCreateSkillBadgeText(freezeRoot);
            ConfigureSkillSlot(
                skillRow,
                freezeRoot,
                skillAssets.Freeze,
                freezeButton,
                freezeCharges);
            freezeRoot.SetSiblingIndex(0);

            RectTransform instantRoot = GetOrCreateUiChild(
                skillRow,
                "InstantBarrierButton");
            instantRoot.gameObject.SetActive(true);
            Button instantButton = GetOrAddComponent<Button>(
                instantRoot.gameObject);
            Text instantCharges = GetOrCreateSkillBadgeText(instantRoot);
            ConfigureSkillSlot(
                skillRow,
                instantRoot,
                skillAssets.Instant,
                instantButton,
                instantCharges);
            instantRoot.SetSiblingIndex(1);

            RectTransform gravityRoot = GetOrCreateUiChild(
                skillRow,
                "GravityWellButton");
            gravityRoot.gameObject.SetActive(true);
            Button gravityButton = GetOrAddComponent<Button>(
                gravityRoot.gameObject);
            Text gravityCharges = GetOrCreateSkillBadgeText(gravityRoot);
            ConfigureSkillSlot(
                skillRow,
                gravityRoot,
                skillAssets.Gravity,
                gravityButton,
                gravityCharges);
            Outline gravityHighlight =
                GetOrAddComponent<Outline>(gravityRoot.gameObject);
            gravityHighlight.effectColor = GravityTargetingHighlight;
            gravityHighlight.effectDistance = new Vector2(4f, -4f);
            gravityHighlight.useGraphicAlpha = true;
            gravityHighlight.enabled = false;
            gravityRoot.SetSiblingIndex(2);

            presenter.Configure(
                controller,
                freezeRoot.gameObject,
                freezeButton,
                freezeCharges,
                instantRoot.gameObject,
                instantButton,
                instantCharges,
                gravityRoot.gameObject,
                gravityButton,
                gravityCharges,
                gravityHighlight);

            EditorUtility.SetDirty(skillLayout);
            EditorUtility.SetDirty(gravityButton);
            EditorUtility.SetDirty(gravityHighlight);
            EditorUtility.SetDirty(presenter);
        }

        private static void ConfigureSkillSlot(
            RectTransform skillRow,
            RectTransform slotRoot,
            Sprite sprite,
            Button button,
            Text chargesText)
        {
            slotRoot.SetParent(skillRow, false);
            LayoutElement layout = GetOrAddComponent<LayoutElement>(
                slotRoot.gameObject);
            layout.minWidth = SkillCellSize;
            layout.preferredWidth = SkillCellSize;
            layout.flexibleWidth = 0f;
            layout.minHeight = SkillCellSize;
            layout.preferredHeight = SkillCellSize;
            layout.flexibleHeight = 0f;

            Image image = GetOrAddComponent<Image>(slotRoot.gameObject);
            image.sprite = sprite;
            image.type = Image.Type.Simple;
            image.color = Color.white;
            image.preserveAspect = true;
            if (button != null)
            {
                button.targetGraphic = image;
            }

            // Just the live count -- the skill artwork itself already
            // draws a badge backing, so no separate chip Image is needed
            // here (a prior pass added one; drop it if a scene still has
            // it, matching this file's self-healing convention).
            Transform staleBackground = slotRoot.Find("LabelBackground");
            if (staleBackground != null)
            {
                UnityEngine.Object.DestroyImmediate(staleBackground.gameObject);
            }

            if (chargesText != null)
            {
                RectTransform chargesRect = chargesText.rectTransform;
                chargesRect.anchorMin = new Vector2(1f, 0f);
                chargesRect.anchorMax = new Vector2(1f, 0f);
                chargesRect.pivot = new Vector2(1f, 0f);
                chargesRect.sizeDelta = new Vector2(38f, 38f);
                chargesRect.anchoredPosition = new Vector2(-2f, 2f);
                chargesText.alignment = TextAnchor.MiddleCenter;
                chargesText.fontSize = 26;
                chargesRect.SetAsLastSibling();
                EditorUtility.SetDirty(chargesText);
            }

            EditorUtility.SetDirty(layout);
            EditorUtility.SetDirty(image);
        }

        private static void ConfigureGravityWellCue(
            RectTransform boardFrame,
            FirstPlayableController controller,
            Sprite vortexSprite)
        {
            RectTransform cueRoot = GetOrCreateUiChild(
                boardFrame,
                "GravityWellCue");
            cueRoot.anchorMin = new Vector2(0.5f, 0.5f);
            cueRoot.anchorMax = new Vector2(0.5f, 0.5f);
            cueRoot.pivot = new Vector2(0.5f, 0.5f);
            cueRoot.anchoredPosition = Vector2.zero;
            cueRoot.localScale = Vector3.one;
            cueRoot.gameObject.SetActive(true);
            cueRoot.SetAsLastSibling();

            Image staleRootImage = cueRoot.GetComponent<Image>();
            if (staleRootImage != null)
            {
                UnityEngine.Object.DestroyImmediate(staleRootImage);
            }

            Transform legacyRange = cueRoot.Find("Range");
            if (legacyRange != null)
            {
                Undo.DestroyObjectImmediate(legacyRange.gameObject);
            }

            Transform legacyIcon = cueRoot.Find("Icon");
            Transform existingVortex = cueRoot.Find("Vortex");
            if (legacyIcon != null && existingVortex == null)
            {
                Undo.RecordObject(
                    legacyIcon.gameObject,
                    "Rename Gravity Well Icon To Vortex");
                legacyIcon.name = "Vortex";
            }
            else if (legacyIcon != null)
            {
                Undo.DestroyObjectImmediate(legacyIcon.gameObject);
            }

            RectTransform vortexRoot = GetOrCreateUiChild(
                cueRoot,
                "Vortex");
            StretchToParent(vortexRoot);
            vortexRoot.localScale = Vector3.one;
            vortexRoot.SetAsLastSibling();

            Image vortexImage = GetOrAddComponent<Image>(vortexRoot.gameObject);
            vortexImage.sprite = vortexSprite;
            vortexImage.type = Image.Type.Simple;
            vortexImage.preserveAspect = true;
            vortexImage.color = new Color(1f, 1f, 1f, 0.78f);
            vortexImage.raycastTarget = false;

            LayoutElement layout = GetOrAddComponent<LayoutElement>(
                cueRoot.gameObject);
            layout.ignoreLayout = true;
            GravityWellPresenter presenter =
                GetOrAddComponent<GravityWellPresenter>(cueRoot.gameObject);
            presenter.ConfigureForSetup(
                controller,
                boardFrame,
                cueRoot,
                vortexRoot,
                vortexImage);

            EditorUtility.SetDirty(vortexImage);
            EditorUtility.SetDirty(layout);
            EditorUtility.SetDirty(presenter);
        }

        private static void ApplyGeneralButtonStyle(
            Button button,
            Sprite background)
        {
            Image image = button.GetComponent<Image>();
            if (image == null)
            {
                image = GetOrAddComponent<Image>(button.gameObject);
            }

            image.sprite = background;
            image.type = Image.Type.Sliced;
            image.preserveAspect = false;
            image.color = Color.white;
            button.targetGraphic = image;

            AspectRatioFitter aspectFitter =
                GetOrAddComponent<AspectRatioFitter>(button.gameObject);
            aspectFitter.aspectMode =
                AspectRatioFitter.AspectMode.WidthControlsHeight;
            aspectFitter.aspectRatio = background.rect.width
                / background.rect.height;

            Text[] legacyLabels = button.GetComponentsInChildren<Text>(true);
            for (int index = 0; index < legacyLabels.Length; index++)
            {
                ConfigureGeneralButtonLabel(legacyLabels[index]);
            }

            TMP_Text[] tmpLabels =
                button.GetComponentsInChildren<TMP_Text>(true);
            for (int index = 0; index < tmpLabels.Length; index++)
            {
                ConfigureGeneralButtonLabel(tmpLabels[index]);
            }

            EditorUtility.SetDirty(image);
            EditorUtility.SetDirty(aspectFitter);
            EditorUtility.SetDirty(button);
        }

        private static void ConfigureGeneralButtonLabel(Text label)
        {
            ConfigureGeneralButtonLabelRect(label.rectTransform);
            label.color = Color.white;
            label.fontStyle = FontStyle.Bold;
            label.fontSize = 40;
            label.alignment = TextAnchor.MiddleCenter;
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 20;
            label.resizeTextMaxSize = 40;
            label.raycastTarget = false;
            ConfigureButtonTextShadow(label);
            EditorUtility.SetDirty(label);
        }

        private static void ConfigureGeneralButtonLabel(TMP_Text label)
        {
            ConfigureGeneralButtonLabelRect(label.rectTransform);
            label.color = Color.white;
            label.fontStyle |= FontStyles.Bold;
            label.fontSize = 40f;
            label.alignment = TextAlignmentOptions.Center;
            label.enableAutoSizing = true;
            label.fontSizeMin = 20f;
            label.fontSizeMax = 40f;
            label.raycastTarget = false;
            ConfigureButtonTextShadow(label);
            EditorUtility.SetDirty(label);
        }

        private static void ConfigureGeneralButtonLabelRect(
            RectTransform labelRect)
        {
            // The artwork's readable inner panel is slightly right/up of the
            // full PNG bounds because the left/bottom painted shadow is part
            // of the source image. These normalized insets center the label
            // on the visible face rather than on the transparent sprite rect.
            labelRect.anchorMin = new Vector2(0.08f, 0.08f);
            labelRect.anchorMax = new Vector2(0.98f, 0.96f);
            labelRect.pivot = new Vector2(0.5f, 0.5f);
            labelRect.offsetMin = new Vector2(0f, 8f);
            labelRect.offsetMax = new Vector2(0f, 8f);
            labelRect.localScale = Vector3.one;
        }

        private static void ConfigureFeedbackReadabilityForSetup(
            Transform safeArea)
        {
            Transform feedbackOverlay = safeArea.Find("FeedbackOverlay");
            Transform cueTransform = feedbackOverlay != null
                ? feedbackOverlay.Find("CueLabel")
                : null;
            if (cueTransform != null)
            {
                RectTransform cueRect = (RectTransform)cueTransform;
                cueRect.anchorMin = new Vector2(0.04f, 0.37f);
                cueRect.anchorMax = new Vector2(0.96f, 0.63f);
                cueRect.offsetMin = Vector2.zero;
                cueRect.offsetMax = Vector2.zero;
                Text cueText = cueTransform.GetComponent<Text>();
                if (cueText != null)
                {
                    cueText.fontSize = 76;
                    cueText.resizeTextForBestFit = true;
                    cueText.resizeTextMinSize = 40;
                    cueText.resizeTextMaxSize = 76;
                    ConfigureButtonTextShadow(cueText);
                    EditorUtility.SetDirty(cueText);
                }

                FeedbackPresenter feedbackPresenter = feedbackOverlay
                    .GetComponent<FeedbackPresenter>();
                if (feedbackPresenter != null)
                {
                    ConfigureCompletionSummaryListForSetup(
                        safeArea,
                        feedbackOverlay,
                        feedbackPresenter);
                }
            }

            ConfigureCompletionFailureText(safeArea);
        }

        // Builds the itemized, one-row-at-a-time completion summary (Level
        // N Complete header + Captured/Cuts/Time/Broken) that
        // FeedbackPresenter.ShowCompletionSummary reveals in sequence.
        // Deliberately a SIBLING of FeedbackOverlay (not a child of it):
        // FeedbackOverlay's own CanvasGroup is _cueCanvasGroup, driven to
        // alpha 0 whenever no ephemeral single-line cue (LOCKED, BIG CUT,
        // ...) is showing -- which is always true during completion, since
        // ShowCompletionSummary explicitly clears any stale cue. Nesting
        // this summary inside that overlay made the whole card invisible
        // even though every row reported alpha 1.
        private static readonly Color CompletionSummaryStatRowColor =
            new Color(0.96f, 0.95f, 0.92f, 1f);
        private static readonly string[] CompletionSummaryRowNames =
        {
            "HeaderRow", "CapturedRow", "CutsRow", "TimeRow", "BrokenRow",
        };
        private const float CompletionSummaryRowHeight = 66f;
        private const float CompletionSummaryRowSpacing = 2f;
        // How far below the stats list's own (content-fit) bottom edge the
        // reward row hangs -- deliberately a bit more than the 2px between
        // stat rows so it still reads as the list's final item, not glued
        // on, without the large leftover gap a band sized for the old
        // taller rows used to leave beneath a shorter list.
        private const float CompletionRewardRowGapFromStats = 16f;
        // Total height of the 5 stat rows once laid out -- both the list
        // itself and the reward row below it are sized/positioned from
        // this so shrinking row height/spacing never reopens that gap.
        private static float CompletionSummaryContentHeight =>
            FeedbackPresenter.CompletionSummaryRowCount
                * CompletionSummaryRowHeight
            + (FeedbackPresenter.CompletionSummaryRowCount - 1)
                * CompletionSummaryRowSpacing;

        private static void ConfigureCompletionSummaryListForSetup(
            Transform safeArea,
            Transform feedbackOverlay,
            FeedbackPresenter feedbackPresenter)
        {
            // One-time migration: earlier setup runs parented these two
            // directly under FeedbackOverlay. Remove them there so they
            // don't linger as invisible, inert duplicates once rebuilt at
            // the correct location below.
            RemoveStaleChild(feedbackOverlay, "CompletionSummaryBackground");
            RemoveStaleChild(feedbackOverlay, "CompletionSummaryList");

            RectTransform summaryOverlay = GetOrCreateUiChild(
                safeArea,
                "CompletionSummaryOverlay");
            StretchToParent(summaryOverlay);
            LayoutElement summaryOverlayLayout = GetOrAddComponent<
                LayoutElement>(summaryOverlay.gameObject);
            summaryOverlayLayout.ignoreLayout = true;

            TMP_FontAsset font = LoadTopHudAssets().Font;

            RectTransform backgroundRect = GetOrCreateUiChild(
                summaryOverlay,
                "CompletionSummaryBackground");
            Image backgroundImage = GetOrAddComponent<Image>(
                backgroundRect.gameObject);

            // Anchored to a single point at the top of the old 0.37-0.63
            // band and sized to exactly fit its 5 rows (not stretched to
            // fill the whole band) so the reward row below can hang
            // directly off its real bottom edge instead of a fixed band
            // edge that left a growing gap once rows got shorter.
            RectTransform listRect = GetOrCreateUiChild(
                summaryOverlay,
                "CompletionSummaryList");
            listRect.anchorMin = new Vector2(0.04f, 0.63f);
            listRect.anchorMax = new Vector2(0.96f, 0.63f);
            listRect.pivot = new Vector2(0.5f, 1f);
            listRect.anchoredPosition = Vector2.zero;
            listRect.sizeDelta = new Vector2(0f, CompletionSummaryContentHeight);
            listRect.localScale = Vector3.one;
            CanvasGroup listGroup = GetOrAddComponent<CanvasGroup>(
                listRect.gameObject);
            listGroup.interactable = false;
            listGroup.blocksRaycasts = false;
            LayoutElement listIgnore = GetOrAddComponent<LayoutElement>(
                listRect.gameObject);
            listIgnore.ignoreLayout = true;
            VerticalLayoutGroup listLayout =
                GetOrAddComponent<VerticalLayoutGroup>(listRect.gameObject);
            listLayout.spacing = CompletionSummaryRowSpacing;
            listLayout.childAlignment = TextAnchor.UpperCenter;
            listLayout.childControlWidth = true;
            listLayout.childControlHeight = true;
            listLayout.childForceExpandWidth = true;
            listLayout.childForceExpandHeight = false;

            var rows = new FeedbackPresenter.CompletionSummaryRow[
                CompletionSummaryRowNames.Length];
            for (int index = 0; index < CompletionSummaryRowNames.Length;
                index++)
            {
                RectTransform rowRect = GetOrCreateUiChild(
                    listRect,
                    CompletionSummaryRowNames[index]);
                rowRect.localScale = Vector3.one;
                LayoutElement rowLayout = GetOrAddComponent<LayoutElement>(
                    rowRect.gameObject);
                rowLayout.preferredHeight = CompletionSummaryRowHeight;
                rowLayout.flexibleHeight = 0f;
                CanvasGroup rowGroup = GetOrAddComponent<CanvasGroup>(
                    rowRect.gameObject);
                TextMeshProUGUI rowText = GetOrAddComponent<TextMeshProUGUI>(
                    rowRect.gameObject);
                rowText.font = font;
                rowText.alignment = TextAlignmentOptions.Center;
                rowText.raycastTarget = false;
                rowText.richText = true;
                rowText.textWrappingMode = TextWrappingModes.NoWrap;
                bool isHeader = index == 0;
                rowText.fontStyle = FontStyles.Bold;
                rowText.color = isHeader
                    ? Color.white
                    : CompletionSummaryStatRowColor;
                rowText.enableAutoSizing = true;
                rowText.fontSizeMin = isHeader ? 34f : 26f;
                rowText.fontSizeMax = isHeader ? 58f : 46f;
                ConfigureButtonTextShadow(rowText);
                EditorUtility.SetDirty(rowText);
                EditorUtility.SetDirty(rowLayout);
                rows[index] = new FeedbackPresenter.CompletionSummaryRow(
                    rowText,
                    rowGroup);
            }

            Undo.RecordObject(
                feedbackPresenter,
                "Wire Completion Summary List");
            feedbackPresenter.ConfigureCompletionSummaryForSetup(
                backgroundImage,
                listGroup,
                rows);
            backgroundRect.SetSiblingIndex(
                Mathf.Max(0, listRect.GetSiblingIndex()));
            listRect.SetAsLastSibling();

            // Render just above FeedbackOverlay's board-frame effects, but
            // beneath the reward overlay and the final landmark popup
            // (matches ConfigureLevelCoinRewardOverlay's own ordering).
            summaryOverlay.SetSiblingIndex(feedbackOverlay.GetSiblingIndex() + 1);
            Transform coinOverlay = safeArea.Find("LevelCoinRewardOverlay");
            if (coinOverlay != null)
            {
                coinOverlay.SetSiblingIndex(summaryOverlay.GetSiblingIndex() + 1);
            }

            Transform completionOverlay = safeArea.Find("LevelCompleteOverlay");
            if (completionOverlay != null)
            {
                completionOverlay.SetAsLastSibling();
            }

            EditorUtility.SetDirty(summaryOverlayLayout);
            EditorUtility.SetDirty(backgroundImage);
            EditorUtility.SetDirty(listGroup);
            EditorUtility.SetDirty(feedbackPresenter);
        }

        // Destroys a direct child that an earlier setup pass left behind at
        // a location this pass no longer uses -- prevents a permanently
        // invisible (and, for reward icon/text, visually duplicated) stale
        // copy from lingering in the scene once the real one is rebuilt
        // elsewhere.
        private static void RemoveStaleChild(Transform parent, string name)
        {
            Transform stale = parent.Find(name);
            if (stale != null)
            {
                Undo.DestroyObjectImmediate(stale.gameObject);
            }
        }

        private static void ConfigureCompletionFailureText(Transform safeArea)
        {
            Transform failureTransform = safeArea.Find(
                "CutLimitFailureOverlay/GameOverPanelBounds/" +
                "GameOverPanel/FailureText");
            if (failureTransform != null)
            {
                RectTransform failureRect = (RectTransform)failureTransform;
                failureRect.anchorMin = new Vector2(0.14f, 0.5f);
                failureRect.anchorMax = new Vector2(0.86f, 0.5f);
                failureRect.pivot = new Vector2(0.5f, 0.5f);
                failureRect.anchoredPosition = new Vector2(0f, 50f);
                failureRect.sizeDelta = new Vector2(0f, 210f);
                Text failureText = failureTransform.GetComponent<Text>();
                if (failureText != null)
                {
                    failureText.fontSize = 86;
                    failureText.resizeTextForBestFit = true;
                    failureText.resizeTextMinSize = 52;
                    failureText.resizeTextMaxSize = 86;
                    ConfigureButtonTextShadow(failureText);
                    EditorUtility.SetDirty(failureText);
                }
            }
        }

        private static void ConfigureGeneralActionButtonLayoutForSetup(
            Transform safeArea)
        {
            Transform completion = RequireChild(
                safeArea,
                "LevelCompleteOverlay");
            ConfigureFixedCompletionButton(
                (RectTransform)RequireChild(completion, "RetryButton"),
                -160f);
            ConfigureFixedCompletionButton(
                (RectTransform)RequireChild(completion, "NextButton"),
                160f);

            // The failure actions use square, bespoke icon art and are
            // positioned relative to GameOverPanel by ConfigureIdentityHud.
            // Only the completion screen uses the shared wide action frame.
        }

        private static void ConfigureFixedCompletionButton(
            RectTransform buttonRect,
            float horizontalPosition)
        {
            buttonRect.anchorMin = new Vector2(0.5f, 0.065f);
            buttonRect.anchorMax = new Vector2(0.5f, 0.065f);
            buttonRect.pivot = new Vector2(0.5f, 0.5f);
            buttonRect.anchoredPosition = new Vector2(
                horizontalPosition,
                0f);
            buttonRect.sizeDelta = new Vector2(280f, 115f);
        }

        private static void ConfigureButtonTextShadow(Graphic label)
        {
            Shadow shadow = GetOrAddComponent<Shadow>(label.gameObject);
            shadow.effectColor = new Color(0f, 0f, 0f, 0.72f);
            shadow.effectDistance = new Vector2(2f, -2f);
            shadow.useGraphicAlpha = true;
            EditorUtility.SetDirty(shadow);
        }

        private static bool IsIconOnlyButton(Button button)
        {
            string name = button.gameObject.name;
            return name == "FreezePulseButton"
                || name == "InstantBarrierButton"
                || name == "GravityWellButton"
                || name == "SettingsButton"
                || name == "HudBlockerButton";
        }

        private static Text GetOrCreateSkillBadgeText(RectTransform slotRoot)
        {
            RectTransform badgeRect = GetOrCreateUiChild(slotRoot, "Label");
            Text badgeText = GetOrAddComponent<Text>(badgeRect.gameObject);
            badgeText.font = LoadLegacyUiFontForSetup();
            badgeText.fontStyle = FontStyle.Bold;
            badgeText.color = Color.white;
            badgeText.raycastTarget = false;
            badgeText.text = string.Empty;
            return badgeText;
        }

        private static void HideLegacyGameplayElement(
            Transform parent,
            string childName)
        {
            Transform child = parent.Find(childName);
            if (child == null)
            {
                return;
            }

            child.gameObject.SetActive(false);
            EditorUtility.SetDirty(child.gameObject);
        }

        // Unlike HideLegacyGameplayElement, this fully discards a direct
        // child left behind by an earlier presentation-pass layout that has
        // since been superseded -- used where merely deactivating it would
        // still leave a stray duplicate component (e.g. a second
        // SandProgressPresenter) or an unused extra layout-group child.
        private static void DestroyLegacyChild(Transform parent, string childName)
        {
            Transform child = parent.Find(childName);
            if (child != null)
            {
                UnityEngine.Object.DestroyImmediate(child.gameObject);
            }
        }

        private static void RestyleHud(
            CaptureHudPresenter hud,
            Transform topHud,
            IReadOnlyDictionary<string, Sprite> sprites)
        {
            // TopHUD's own band is widened here (Milestone2's 52/60 baseline
            // only needed room for a single text row) so its content --
            // now a progress bar row -- reads as centered in the gap
            // between the screen's top edge and the board, not hugging one
            // side of a barely-tall-enough strip.
            LayoutElement topLayout = topHud.GetComponent<LayoutElement>();
            if (topLayout != null)
            {
                topLayout.minHeight = 96f;
                topLayout.preferredHeight = 106f;
                topLayout.flexibleHeight = 0f;
                EditorUtility.SetDirty(topLayout);
            }

            // The tutorial "LEARN THE CUT" purpose line and the level
            // number are both secondary copy that only cluttered the top
            // HUD; their text content stays untouched (Milestone2CPlayModeTests
            // asserts the purpose string, Milestone3CoreFunPlayModeTests
            // asserts the level string), they're just not shown. A plain
            // Text component's .text still reads fine while inactive.
            if (hud.PurposeText != null)
            {
                hud.PurposeText.gameObject.SetActive(false);
            }

            if (hud.LevelText != null)
            {
                hud.LevelText.gameObject.SetActive(false);
            }

            HorizontalLayoutGroup topRow =
                topHud.GetComponent<HorizontalLayoutGroup>();
            if (topRow != null)
            {
                topRow.padding = new RectOffset(14, 14, 6, 6);
                topRow.spacing = 10f;
                EditorUtility.SetDirty(topRow);
            }

            // The progress bar is the only thing left in TopHUD: no
            // background chip, no level label, no spacer decoration.
            // ProgressArea stretches to fill topRow (blocker is
            // ignoreLayout, see below, so it's the only competing child),
            // and topRow's MiddleCenter alignment keeps it centered.
            Transform progressArea = RequireChild(topHud, "ProgressArea");
            LayoutElement progressLayout =
                progressArea.GetComponent<LayoutElement>();
            if (progressLayout != null)
            {
                progressLayout.minWidth = 0f;
                progressLayout.preferredWidth = 0f;
                progressLayout.flexibleWidth = 1f;
                EditorUtility.SetDirty(progressLayout);
            }

            // A previous pass relocated LevelNumber into ProgressArea and
            // gave ProgressArea its own chip Image; both are reverted here,
            // so clean up any leftovers from an earlier run instead of
            // leaving them behind orphaned.
            Transform staleLevelNumber = progressArea.Find("LevelNumber");
            if (staleLevelNumber != null)
            {
                UnityEngine.Object.DestroyImmediate(staleLevelNumber.gameObject);
            }

            Image staleProgressChip = progressArea.GetComponent<Image>();
            if (staleProgressChip != null)
            {
                UnityEngine.Object.DestroyImmediate(staleProgressChip);
            }

            Transform staleSpacer = topHud.Find("LeadingSpacer");
            if (staleSpacer != null)
            {
                UnityEngine.Object.DestroyImmediate(staleSpacer.gameObject);
            }

            HorizontalLayoutGroup progressRow =
                progressArea.GetComponent<HorizontalLayoutGroup>();
            if (progressRow != null)
            {
                progressRow.padding = new RectOffset(6, 6, 0, 0);
                EditorUtility.SetDirty(progressRow);
            }

            // This slot used to read "Captured X%" to the left of the bar;
            // that's now redundant with the sole current-percentage readout
            // at the bar's right edge (see TargetText below), so it's
            // hidden rather than shown twice. It stays wired -- Milestone2C/
            // 3 assert it's non-null, parented under ProgressArea, and its
            // text still updates while inactive, same pattern already used
            // for PurposeText/LevelText.
            if (hud.PercentageText != null)
            {
                hud.PercentageText.gameObject.SetActive(false);
                EditorUtility.SetDirty(hud.PercentageText.gameObject);
            }

            // A wide fill bar makes progress readable at a glance. It
            // spans nearly all of ProgressArea's row, filling left-to-right
            // as CapturedFraction rises, with the current percentage
            // reading immediately after its right edge (TargetText, below).
            RectTransform barTrackRect = GetOrCreateUiChild(
                progressArea,
                "ProgressBarTrack");
            barTrackRect.SetSiblingIndex(1);
            LayoutElement barTrackLayout =
                GetOrAddComponent<LayoutElement>(barTrackRect.gameObject);
            barTrackLayout.minWidth = 40f;
            barTrackLayout.preferredWidth = 40f;
            barTrackLayout.flexibleWidth = 1f;
            barTrackLayout.minHeight = 18f;
            barTrackLayout.preferredHeight = 18f;
            barTrackLayout.flexibleHeight = 0f;
            Image barTrackImage = GetOrAddComponent<Image>(barTrackRect.gameObject);
            barTrackImage.sprite = sprites["chip_rounded"];
            barTrackImage.type = Image.Type.Sliced;
            barTrackImage.color = new Color(1f, 1f, 1f, 0.18f);
            barTrackImage.raycastTarget = false;

            RectTransform barFillRect = GetOrCreateUiChild(
                barTrackRect,
                "ProgressBarFill");
            StretchToParent(barFillRect);
            Image barFillImage = GetOrAddComponent<Image>(barFillRect.gameObject);
            barFillImage.sprite = sprites["chip_rounded"];
            barFillImage.type = Image.Type.Filled;
            barFillImage.fillMethod = Image.FillMethod.Horizontal;
            barFillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
            barFillImage.color = new Color(0.32f, 0.78f, 0.62f, 1f);
            barFillImage.raycastTarget = false;

            // The target is marked directly on the bar -- a tick at the
            // target fraction's position plus a small label above it --
            // instead of as a separate number, so it reads as "cross this
            // line to win" rather than an abstract stat. CaptureHudPresenter
            // repositions both every frame from the live target fraction
            // and the track's actual width.
            RectTransform tickRect = GetOrCreateUiChild(
                barTrackRect,
                "TargetTick");
            tickRect.anchorMin = new Vector2(0f, 0f);
            tickRect.anchorMax = new Vector2(0f, 1f);
            tickRect.pivot = new Vector2(0.5f, 0.5f);
            tickRect.sizeDelta = new Vector2(3f, 10f);
            tickRect.anchoredPosition = Vector2.zero;
            Image tickImage = GetOrAddComponent<Image>(tickRect.gameObject);
            tickImage.color = new Color(1f, 0.87f, 0.35f, 0.95f);
            tickImage.raycastTarget = false;

            RectTransform tickLabelRect = GetOrCreateUiChild(
                barTrackRect,
                "TargetTickLabel");
            tickLabelRect.anchorMin = new Vector2(0f, 1f);
            tickLabelRect.anchorMax = new Vector2(0f, 1f);
            tickLabelRect.pivot = new Vector2(0.5f, 0f);
            tickLabelRect.sizeDelta = new Vector2(64f, 14f);
            tickLabelRect.anchoredPosition = new Vector2(0f, 4f);
            Text tickLabelText = ConfigureText(
                tickLabelRect,
                "TARGET",
                10,
                TextAnchor.LowerCenter,
                new Color(1f, 0.87f, 0.35f, 0.95f));
            tickLabelText.fontStyle = FontStyle.Bold;
            tickLabelText.raycastTarget = false;

            hud.ConfigureProgressBar(
                barFillImage,
                barTrackRect,
                tickRect,
                tickLabelText);
            EditorUtility.SetDirty(barTrackImage);
            EditorUtility.SetDirty(barFillImage);
            EditorUtility.SetDirty(tickImage);
            EditorUtility.SetDirty(tickLabelText);
            EditorUtility.SetDirty(hud);

            // The sole percentage readout: the current captured fraction,
            // shown right after the bar's right edge (e.g. "13%"). The
            // target is marked on the bar itself (tick + label above), not
            // as a separate number.
            if (hud.TargetText != null)
            {
                hud.TargetText.gameObject.SetActive(true);
                hud.TargetText.fontSize = 24;
                hud.TargetText.fontStyle = FontStyle.Bold;
                hud.TargetText.color = Color.white;
                EditorUtility.SetDirty(hud.TargetText);
            }

            // The blocker keeps its required function and label string (see
            // Milestone2CPlayModeTests) but is fully invisible now -- an
            // empty, reserved slot for a future settings/meta action.
            // ignoreLayout so this invisible slot doesn't consume row space
            // from topRow's HorizontalLayoutGroup -- otherwise ProgressArea
            // (the only other row child) would be skewed left of true
            // center by however much width this reserves on the right,
            // which is exactly why the progress row looked off-center.
            Transform blocker = RequireChild(topHud, "HudBlockerButton");
            blocker.SetAsLastSibling();
            var blockerRect = (RectTransform)blocker;
            LayoutElement blockerLayout = blocker.GetComponent<LayoutElement>();
            if (blockerLayout != null)
            {
                blockerLayout.ignoreLayout = true;
                blockerLayout.preferredWidth = 72f;
                blockerLayout.minWidth = 72f;
                blockerLayout.preferredHeight = 34f;
                blockerLayout.flexibleWidth = 0f;
                EditorUtility.SetDirty(blockerLayout);
            }

            blockerRect.anchorMin = new Vector2(1f, 0.5f);
            blockerRect.anchorMax = new Vector2(1f, 0.5f);
            blockerRect.pivot = new Vector2(1f, 0.5f);
            blockerRect.sizeDelta = new Vector2(72f, 34f);
            blockerRect.anchoredPosition = new Vector2(-14f, 0f);

            Image blockerImage = blocker.GetComponent<Image>();
            if (blockerImage != null)
            {
                blockerImage.color = new Color(0f, 0f, 0f, 0f);
                EditorUtility.SetDirty(blockerImage);
            }

            Transform blockerLabelTransform = blocker.Find("Label");
            if (blockerLabelTransform != null)
            {
                Text blockerLabel = blockerLabelTransform.GetComponent<Text>();
                if (blockerLabel != null)
                {
                    blockerLabel.color = new Color(0f, 0f, 0f, 0f);
                    EditorUtility.SetDirty(blockerLabel);
                }
            }
        }

        private static void ConfigureBottomHud(
            GameObject root,
            Transform safeArea,
            FirstPlayableController controller,
            IReadOnlyDictionary<string, Sprite> sprites,
            ThemeDefinition sandBowlTheme,
            out RectTransform bowlFillTargetRect)
        {
            Transform bottomHud = RequireChild(safeArea, "BottomHUD");

            // A now-obsolete "PowerRow" (from a prior pass that relocated
            // Freeze/Instant Barrier buttons into BottomHUD's own layout
            // flow) is not touched by this pass at all, so if it was left
            // behind by an earlier run it just sits there, active, still
            // showing the old blue/orange buttons behind the quick-retry
            // button. Remove it outright -- nothing should relocate power
            // buttons into BottomHUD anymore.
            Transform stalePowerRow = bottomHud.Find("PowerRow");
            if (stalePowerRow != null)
            {
                UnityEngine.Object.DestroyImmediate(stalePowerRow.gameObject);
            }

            LayoutElement bottomLayout =
                bottomHud.GetComponent<LayoutElement>();
            // Debug status rows are hidden now (HideDebugFooter) and the
            // power buttons are gone (see below), so BottomHUD only needs
            // to fit one small retry chip -- but its band is still sized
            // generously (not just chip-tight) so the button reads as
            // centered in the gap between the board and the bottom of the
            // phone, not stuck flush against the edge.
            bottomLayout.minHeight = 104f;
            bottomLayout.preferredHeight = 114f;
            bottomLayout.flexibleHeight = 0f;
            EditorUtility.SetDirty(bottomLayout);

            VerticalLayoutGroup bottomColumn =
                bottomHud.GetComponent<VerticalLayoutGroup>();
            if (bottomColumn != null)
            {
                bottomColumn.padding = new RectOffset(10, 10, 10, 10);
                bottomColumn.childAlignment = TextAnchor.MiddleCenter;
                EditorUtility.SetDirty(bottomColumn);
            }

            // The default (Milestone 3) level catalog grants zero Freeze
            // Pulse/Instant Barrier charges, so PowerHudPresenter leaves
            // both buttons permanently non-interactable (Button.interactable
            // gated on charge count) -- visible but dead in real play.
            // Hide the whole overlay instead of showing controls that do
            // nothing; PowerHudPresenter and its button references stay
            // valid so Milestone6ThreatsAndPowersPlayModeTests' reference
            // checks keep passing.
            Transform powerControls = RequireChild(safeArea, "PowerControls");
            if (powerControls.gameObject.activeSelf)
            {
                powerControls.gameObject.SetActive(false);
                EditorUtility.SetDirty(powerControls.gameObject);
            }

            // The sand bowl (+ target text) now shares this row with the
            // retry button, so both are edge-anchored -- bowl+target on
            // the left, retry on the right -- with ignoreLayout=true and
            // explicit anchors, the same reliable pattern already used
            // for the level label above, instead of relying on
            // bottomColumn's shared VerticalLayoutGroup (which produced
            // inconsistent Middle/Center alignment math for a lone
            // controlled child).
            bowlFillTargetRect = ConfigureSandBowl(
                root,
                bottomHud,
                controller,
                sandBowlTheme);

            RectTransform retryRect = GetOrCreateUiChild(bottomHud, "QuickRetryButton");
            retryRect.anchorMin = new Vector2(1f, 0.5f);
            retryRect.anchorMax = new Vector2(1f, 0.5f);
            retryRect.pivot = new Vector2(1f, 0.5f);
            retryRect.sizeDelta = new Vector2(128f, 40f);
            retryRect.anchoredPosition = new Vector2(-24f, 0f);
            LayoutElement retryLayout =
                GetOrAddComponent<LayoutElement>(retryRect.gameObject);
            retryLayout.ignoreLayout = true;
            retryLayout.minWidth = 128f;
            retryLayout.preferredWidth = 128f;
            retryLayout.minHeight = 40f;
            retryLayout.preferredHeight = 40f;
            retryLayout.flexibleWidth = 0f;
            retryLayout.flexibleHeight = 0f;
            EditorUtility.SetDirty(retryLayout);

            Image retryImage = GetOrAddComponent<Image>(retryRect.gameObject);
            retryImage.sprite = sprites["chip_rounded"];
            retryImage.type = Image.Type.Sliced;
            retryImage.color = new Color(0.84f, 0.4f, 0.36f, 1f);
            retryImage.raycastTarget = true;
            EditorUtility.SetDirty(retryImage);

            Button retryButton = GetOrAddComponent<Button>(retryRect.gameObject);
            retryButton.targetGraphic = retryImage;
            retryButton.interactable = true;

            RectTransform retryLabelRect = GetOrCreateUiChild(retryRect, "Label");
            StretchToParent(retryLabelRect);
            Text retryLabel = ConfigureText(
                retryLabelRect,
                "RETRY",
                16,
                TextAnchor.MiddleCenter,
                Color.white);
            retryLabel.fontStyle = FontStyle.Bold;

            GameObject services = GetOrCreateChild(
                root.transform,
                "QuickRetryServices");
            QuickRetryPresenter retryPresenter =
                GetOrAddComponent<QuickRetryPresenter>(services);
            retryPresenter.Configure(controller, retryButton);

            EditorUtility.SetDirty(retryButton);
            EditorUtility.SetDirty(retryPresenter);
        }

        // Builds the BottomHUD sand bowl: a decorative outline, a
        // bowl-shaped Mask that clips a rising sand-fill Image to the
        // bowl's silhouette (see BowlSpriteGenerator), and the target
        // percentage text next to it. Returns the RectTransform
        // LandmarkRevealPresenter's grain-flight burst aims at.
        private static RectTransform ConfigureSandBowl(
            GameObject root,
            Transform bottomHud,
            FirstPlayableController controller,
            ThemeDefinition sandBowlTheme)
        {
            SandBowlVisualStyle sandBowl = ThemeResolver.ResolveSandBowl(
                sandBowlTheme,
                null);

            const float bowlSize = 72f;
            RectTransform bowlRect = GetOrCreateUiChild(bottomHud, "SandBowl");
            bowlRect.anchorMin = new Vector2(0f, 0.5f);
            bowlRect.anchorMax = new Vector2(0f, 0.5f);
            bowlRect.pivot = new Vector2(0f, 0.5f);
            bowlRect.sizeDelta = new Vector2(bowlSize, bowlSize);
            bowlRect.anchoredPosition = new Vector2(20f, 0f);
            // BottomHUD's own layout validation (Milestone2SceneSetup)
            // requires every direct child to carry an explicit
            // non-flexible LayoutElement, the same pattern QuickRetryButton
            // already uses below -- ignoreLayout=true means bottomColumn's
            // VerticalLayoutGroup never actually sizes this rect, but the
            // component itself must still be present and non-flexible.
            LayoutElement bowlLayout = GetOrAddComponent<LayoutElement>(
                bowlRect.gameObject);
            bowlLayout.ignoreLayout = true;
            bowlLayout.minWidth = bowlSize;
            bowlLayout.preferredWidth = bowlSize;
            bowlLayout.minHeight = bowlSize;
            bowlLayout.preferredHeight = bowlSize;
            bowlLayout.flexibleWidth = 0f;
            bowlLayout.flexibleHeight = 0f;
            EditorUtility.SetDirty(bowlLayout);

            RectTransform maskAreaRect = GetOrCreateUiChild(bowlRect, "FillMaskArea");
            StretchToParent(maskAreaRect);
            Image maskAreaImage = GetOrAddComponent<Image>(maskAreaRect.gameObject);
            maskAreaImage.sprite = sandBowl.BowlInteriorMaskSprite;
            maskAreaImage.type = Image.Type.Simple;
            maskAreaImage.raycastTarget = false;
            Mask bowlMask = GetOrAddComponent<Mask>(maskAreaRect.gameObject);
            bowlMask.showMaskGraphic = false;
            EditorUtility.SetDirty(maskAreaImage);

            // Bottom-anchored; RefreshNow() raises anchorMax.y toward 1 as
            // CapturedFraction rises. The Mask above ensures only the
            // portion inside the bowl's actual silhouette ever shows,
            // regardless of this rect's own (rectangular) bounds.
            RectTransform sandFillRect = GetOrCreateUiChild(
                maskAreaRect,
                "SandFill");
            sandFillRect.anchorMin = new Vector2(0f, 0f);
            sandFillRect.anchorMax = new Vector2(1f, 0f);
            sandFillRect.pivot = new Vector2(0.5f, 0f);
            sandFillRect.offsetMin = Vector2.zero;
            sandFillRect.offsetMax = Vector2.zero;
            Image sandFillImage = GetOrAddComponent<Image>(sandFillRect.gameObject);
            sandFillImage.sprite = null;
            sandFillImage.color = new Color(0.82f, 0.66f, 0.42f, 1f);
            sandFillImage.raycastTarget = false;
            EditorUtility.SetDirty(sandFillImage);

            // The decorative rim, drawn on top of the fill so the bowl's
            // edge always reads clearly regardless of current fill level.
            RectTransform outlineRect = GetOrCreateUiChild(bowlRect, "BowlOutline");
            StretchToParent(outlineRect);
            outlineRect.SetAsLastSibling();
            Image outlineImage = GetOrAddComponent<Image>(outlineRect.gameObject);
            outlineImage.sprite = sandBowl.BowlOutlineSprite;
            outlineImage.type = Image.Type.Simple;
            outlineImage.raycastTarget = false;
            EditorUtility.SetDirty(outlineImage);

            // A small, non-visual reference point (no Graphic) at the
            // bowl's center -- purely the position LandmarkRevealPresenter
            // aims its cosmetic sand-grain burst at.
            RectTransform fillTargetRect = GetOrCreateUiChild(bowlRect, "FillTarget");
            fillTargetRect.anchorMin = new Vector2(0.5f, 0.5f);
            fillTargetRect.anchorMax = new Vector2(0.5f, 0.5f);
            fillTargetRect.pivot = new Vector2(0.5f, 0.5f);
            fillTargetRect.sizeDelta = Vector2.zero;
            fillTargetRect.anchoredPosition = Vector2.zero;

            RectTransform targetTextRect = GetOrCreateUiChild(
                bottomHud,
                "BowlTargetText");
            targetTextRect.anchorMin = new Vector2(0f, 0.5f);
            targetTextRect.anchorMax = new Vector2(0f, 0.5f);
            targetTextRect.pivot = new Vector2(0f, 0.5f);
            targetTextRect.sizeDelta = new Vector2(130f, 32f);
            targetTextRect.anchoredPosition = new Vector2(20f + bowlSize + 10f, 0f);
            Text targetText = ConfigureText(
                targetTextRect,
                "Target 0%",
                18,
                TextAnchor.MiddleLeft,
                new Color(0.95f, 0.9f, 0.8f, 1f));
            targetText.fontStyle = FontStyle.Bold;
            LayoutElement targetTextLayout = GetOrAddComponent<LayoutElement>(
                targetTextRect.gameObject);
            targetTextLayout.ignoreLayout = true;
            targetTextLayout.minWidth = 130f;
            targetTextLayout.preferredWidth = 130f;
            targetTextLayout.minHeight = 32f;
            targetTextLayout.preferredHeight = 32f;
            targetTextLayout.flexibleWidth = 0f;
            targetTextLayout.flexibleHeight = 0f;
            EditorUtility.SetDirty(targetTextLayout);

            GameObject sandBowlServices = GetOrCreateChild(
                root.transform,
                "SandBowlServices");
            SandBowlPresenter sandBowlPresenter =
                GetOrAddComponent<SandBowlPresenter>(sandBowlServices);
            sandBowlPresenter.Configure(
                controller,
                sandFillRect,
                targetText,
                fillTargetRect);
            EditorUtility.SetDirty(sandBowlPresenter);

            return fillTargetRect;
        }

        // ------------------------------------------------------------
        // Validation
        // ------------------------------------------------------------

        private static void Validate(Scene scene, LandmarkDefinition[] landmarks)
        {
            GameObject root = RequireRoot(scene, "VerticalSliceRoot");
            BarrierPresenter barrierPresenter = root
                .GetComponentInChildren<BarrierPresenter>(true);
            ThemePresenter themePresenter = root
                .GetComponentInChildren<ThemePresenter>(true);
            LandmarkRevealPresenter[] landmarkPresenters = root
                .GetComponentsInChildren<LandmarkRevealPresenter>(true);
            if (landmarkPresenters.Length != 1)
            {
                throw new InvalidOperationException(
                    "The presentation pass requires exactly one " +
                    "LandmarkRevealPresenter.");
            }

            LandmarkRevealPresenter landmarkPresenter = landmarkPresenters[0];
            ValidateBoardHierarchy(root);
            ValidateGameplayBandVisualSeparation(root);
            if (themePresenter == null
                || themePresenter.Current.BackgroundSprite != null
                || themePresenter.Current.BackgroundColor != DarkBrownBackground
                || AssetDatabase.GetAssetPath(
                    themePresenter.Current.Threat.Sprite)
                    != NormalThreatVisualPath
                || AssetDatabase.GetAssetPath(
                    themePresenter.Current.Threat.HunterSprite)
                    != HunterThreatVisualPath
                || AssetDatabase.GetAssetPath(
                    themePresenter.Current.Threat.PulseSprite)
                    != PulseThreatVisualPath
                || themePresenter.Current.Threat.TrailColor
                    != NormalThreatTrailTint
                || themePresenter.Current.Threat.TrailColorFor(
                    Cutrium.Gameplay.Threats.ThreatBehaviorKind.Hunter)
                    != HunterThreatTrailTint
                || themePresenter.Current.Threat.TrailColorFor(
                    Cutrium.Gameplay.Threats.ThreatBehaviorKind.Pulse)
                    != PulseThreatTrailTint
                || themePresenter.Current.Barrier.GrowingColor
                    != GrowingBarrierBrown
                || themePresenter.Current.Barrier.PreviewColor
                    != BarrierPreviewBrown
                || themePresenter.Current.Capture.Sprite != null
                || themePresenter.Current.Capture.Material != null
                || themePresenter.Current.Capture.Color != Color.clear)
            {
                throw new InvalidOperationException(
                    "The final theme must use the imported threat visual, " +
                    "the solid dark-brown background, and a fully " +
                    "transparent, sprite-free captured-region presentation.");
            }
            if (!Mathf.Approximately(
                    barrierPresenter.VisualLogicalThickness,
                    BarrierVisualLogicalThickness))
            {
                throw new InvalidOperationException(
                    "Barrier visual thickness was not tuned for the " +
                    "presentation pass.");
            }

            if (landmarkPresenter.ArtworkImage == null
                || landmarkPresenter.VeilRoot == null
                || landmarkPresenter.SandTexture == null
                || landmarkPresenter.GrainFlightRoot == null
                || landmarkPresenter.SandDestination == null
                || landmarkPresenter.SandProgressPresenter == null
                || landmarkPresenter.CompletionArtworkImage == null
                || landmarkPresenter.ScrimCanvasGroup == null
                || landmarkPresenter.ContentCanvasGroup == null
                || landmarkPresenter.StatsCanvasGroup == null
                || landmarkPresenter.RetryCanvasGroup == null
                || landmarkPresenter.NextCanvasGroup == null
                || landmarkPresenter.CompletionTitleText == null
                || landmarkPresenter.CompletionDescriptionText == null
                || landmarkPresenter.CompletionSectorText == null
                || landmarkPresenter.ThreatPresenter == null
                || landmarkPresenter.CaptureBoardPresenter == null
                || landmarkPresenter.CompletionFeedbackPresenter == null
                || landmarkPresenter.Landmarks.Count
                    != MainGameplayProgression.LevelCount)
            {
                throw new InvalidOperationException(
                    "LandmarkRevealPresenter has a missing or mismatched " +
                    "serialized reference, or is not wired to the complete " +
                    "first-24 landmark catalog.");
            }

            ValidateCompletionRewardFlow(root, landmarkPresenter);

            for (int index = 0; index < landmarks.Length; index++)
            {
                if (landmarkPresenter.Landmarks[index] != landmarks[index])
                {
                    throw new InvalidOperationException(
                        "LandmarkRevealPresenter landmark order does not " +
                        "match the configured catalog.");
                }
            }

            SandProgressPresenter[] progressPresenters = root
                .GetComponentsInChildren<SandProgressPresenter>(true);
            if (progressPresenters.Length != 1
                || progressPresenters[0].ProgressBarRect == null
                || progressPresenters[0].BackgroundImage == null
                || progressPresenters[0].FillMaskRect == null
                || progressPresenters[0].FillImage == null
                || progressPresenters[0].ProgressText == null
                || progressPresenters[0].FillStartTarget == null
                || !ReferenceEquals(
                    progressPresenters[0],
                    landmarkPresenter.SandProgressPresenter)
                || !ReferenceEquals(
                    progressPresenters[0].FillStartTarget,
                    landmarkPresenter.SandDestination))
            {
                throw new InvalidOperationException(
                    "The presentation pass requires exactly one fully " +
                    "wired SandProgressPresenter whose fill-start target " +
                    "matches the sand-flight destination.");
            }

            if (AssetDatabase.GetAssetPath(
                    progressPresenters[0].BackgroundImage.sprite)
                    != ProgressBackgroundPath
                || AssetDatabase.GetAssetPath(
                    progressPresenters[0].FillImage.sprite) != ProgressFillPath)
            {
                throw new InvalidOperationException(
                    "The target-progress bar is not wired to its imported " +
                    "progress UI assets.");
            }

            if (landmarkPresenter.Landmarks[0].LandmarkId
                != FirstTwelveLandmarkContent.Entries[0].Id)
            {
                throw new InvalidOperationException(
                    "The first landmark slot must match Chapter 1 Earth " +
                    "content.");
            }

            Transform safeArea = RequireChild(
                root.transform,
                "Canvas/SafeAreaRoot");
            Transform completion = RequireChild(
                safeArea,
                "LevelCompleteOverlay");
            if (completion.GetSiblingIndex() != safeArea.childCount - 1)
            {
                throw new InvalidOperationException(
                    "Completion overlay must remain the final safe-area sibling.");
            }

            Transform heroBounds = RequireChild(completion, "HeroFrameBounds");
            var heroArtworkRect =
                (RectTransform)RequireChild(heroBounds, "HeroArtwork");
            if (heroArtworkRect.GetComponent<AspectRatioFitter>() == null)
            {
                throw new InvalidOperationException(
                    "HeroArtwork must fit a square frame via " +
                    "AspectRatioFitter instead of stretching to fill the " +
                    "screen.");
            }

            Image heroArtworkImage = heroArtworkRect.GetComponent<Image>();
            if (heroArtworkImage == null || !heroArtworkImage.preserveAspect)
            {
                throw new InvalidOperationException(
                    "HeroArtwork must preserve its native aspect ratio " +
                    "instead of stretching.");
            }

            Image overlayBackgroundImage =
                completion.GetComponent<Image>();
            if (overlayBackgroundImage == null
                || overlayBackgroundImage.color.a < 0.999f)
            {
                throw new InvalidOperationException(
                    "LevelCompleteOverlay's background must be fully " +
                    "opaque so gameplay/HUD never shows through it.");
            }

            RequireChild(completion, "CompletionContent/Title");
            RequireChild(completion, "CompletionContent/Sector");
            RequireChild(completion, "CompletionContent/Description");
            RequireChild(completion, "CompleteText");
            RequireChild(completion, "RetryButton");
            RequireChild(completion, "NextButton");

            Transform bottomHud = RequireChild(safeArea, "BottomHUD");
            Transform topHud = RequireChild(safeArea, "TopHUD");
            ValidateGameplayTopHud(topHud);

            Transform bottomRow = RequireChild(bottomHud, "BottomHudRow");
            Transform progressBar = RequireChild(
                bottomRow,
                "ProgressSlot/ProgressBar");
            if (!progressBar.gameObject.activeSelf
                || progressBar.GetComponent<Image>() == null
                || !progressBar.GetComponent<Image>().raycastTarget)
            {
                throw new InvalidOperationException(
                    "BottomHUD/ProgressBar must be active and block pointer " +
                    "starts over its full footprint.");
            }

            RequireChild(progressBar, "Background");
            RequireChild(progressBar, "FillMask/Fill");
            RequireChild(progressBar, "FillMask/FillStartTarget");
            RequireChild(progressBar, "ProgressText");

            ValidateBottomHudSkillRow(
                (RectTransform)RequireChild(bottomRow, "SkillRow"));

            ValidateGravityWellVortex(root);

            ValidateGeneralButtonStyle(
                RequireChild(completion, "RetryButton").GetComponent<Button>());
            ValidateGeneralButtonStyle(
                RequireChild(completion, "NextButton").GetComponent<Button>());

            Transform retryButtonTransform = RequireChild(
                bottomHud,
                "QuickRetryButton");
            if (retryButtonTransform.gameObject.activeSelf
                || retryButtonTransform.GetComponent<Button>() == null)
            {
                throw new InvalidOperationException(
                    "BottomHUD's legacy quick-retry element must stay wired " +
                    "but inactive during normal gameplay.");
            }

            Transform legacyBowl = bottomHud.Find("SandBowl");
            Transform legacyBowlText = bottomHud.Find("BowlTargetText");
            if ((legacyBowl != null && legacyBowl.gameObject.activeSelf)
                || (legacyBowlText != null
                    && legacyBowlText.gameObject.activeSelf))
            {
                throw new InvalidOperationException(
                    "The legacy bowl presentation must remain hidden.");
            }

            QuickRetryPresenter[] retryPresenters = root
                .GetComponentsInChildren<QuickRetryPresenter>(true);
            if (retryPresenters.Length != 1
                || retryPresenters[0].Controller == null
                || retryPresenters[0].RetryButton == null)
            {
                throw new InvalidOperationException(
                    "The presentation pass requires exactly one fully " +
                    "wired QuickRetryPresenter.");
            }

            Transform powerControls = RequireChild(safeArea, "PowerControls");
            if (powerControls.gameObject.activeSelf)
            {
                throw new InvalidOperationException(
                    "PowerControls must stay hidden from the default " +
                    "gameplay HUD.");
            }
        }

        private static void ValidateGameplayTopHud(Transform topHud)
        {
            LayoutElement topLayout = topHud.GetComponent<LayoutElement>();
            if (!topHud.gameObject.activeSelf
                || topLayout == null
                || !Mathf.Approximately(
                    topLayout.minHeight,
                    TopHudMinimumHeight)
                || !Mathf.Approximately(
                    topLayout.preferredHeight,
                    TopHudPreferredHeight)
                || !Mathf.Approximately(topLayout.flexibleHeight, 0f))
            {
                throw new InvalidOperationException(
                    "TopHUD must remain an active, compact fixed layout " +
                    "region for the gameplay HUD.");
            }

            Transform hudRow = RequireChild(topHud, "GameplayHudRow");
            if (!hudRow.gameObject.activeSelf)
            {
                throw new InvalidOperationException(
                    "GameplayHudRow must be the active TopHUD content.");
            }

            for (int index = 0; index < topHud.childCount; index++)
            {
                Transform child = topHud.GetChild(index);
                if (child != hudRow && child.gameObject.activeSelf)
                {
                    throw new InvalidOperationException(
                        $"Legacy TopHUD child '{child.name}' must remain " +
                        "inactive during normal gameplay.");
                }
            }

            Transform bar = RequireChild(hudRow, "TopHudBar");
            Image barImage = bar.GetComponent<Image>();
            if (!bar.gameObject.activeSelf
                || barImage == null
                || AssetDatabase.GetAssetPath(barImage.sprite)
                    != BigHudBackgroundPath
                || barImage.type != Image.Type.Sliced)
            {
                throw new InvalidOperationException(
                    "TopHudBar must be the single active, correctly " +
                    "sprited full-width HUD background.");
            }

            ValidateHealthRegion(RequireChild(bar, "HealthHUD"));
            ValidateTopHudRegion(
                RequireChild(bar, "CutHUD"),
                null,
                null);
            // Speed, like Cut, is dynamically owned by
            // GameplayIdentityHudPresenter (the real per-level
            // BarrierGrowthSpeed), so no fixed placeholder string holds
            // once GameplayProgressionSetup wires it.
            ValidateTopHudRegion(
                RequireChild(bar, "SpeedHUD"),
                null,
                SpeedIconPath);

            Transform settings = RequireChild(
                hudRow,
                "SettingsSlot/SettingsButton");
            Image settingsImage = settings.GetComponent<Image>();
            Button settingsButton = settings.GetComponent<Button>();
            if (!settings.gameObject.activeSelf
                || settingsImage == null
                || AssetDatabase.GetAssetPath(settingsImage.sprite)
                    != SettingsButtonPath
                || settingsButton == null
                || settingsButton.interactable)
            {
                throw new InvalidOperationException(
                    "The settings artwork must remain a visible, inactive " +
                    "future-control placeholder above the HUD bar.");
            }

            Transform coinSlot = RequireChild(hudRow, "CoinBalanceSlot");
            Image coinIcon = RequireChild(coinSlot, "CoinIcon")
                .GetComponent<Image>();
            TextMeshProUGUI coinText = RequireChild(
                coinSlot,
                "BalanceText").GetComponent<TextMeshProUGUI>();
            LayoutElement coinLayout = coinSlot.GetComponent<LayoutElement>();
            if (!coinSlot.gameObject.activeSelf
                || coinIcon == null
                || AssetDatabase.GetAssetPath(coinIcon.sprite)
                    != CoinStackL1Path
                || coinText == null
                || coinText.color != Color.white
                || coinLayout == null
                || !coinLayout.ignoreLayout)
            {
                throw new InvalidOperationException(
                    "Coin balance must remain visible at the TopHUD's " +
                    "upper-left without affecting the gameplay bar layout.");
            }
        }

        // Health no longer has a fixed placeholder to compare against --
        // HealthHudPresenter rebuilds a variable number of hearts per level
        // at runtime -- so this only checks the row container itself is
        // wired for that (an active HorizontalLayoutGroup, no leftover
        // icon+text pair from the pre-heart-row layout).
        private static void ValidateHealthRegion(Transform region)
        {
            Transform heartRow = RequireChild(region, "HeartRow");
            HorizontalLayoutGroup layout =
                heartRow.GetComponent<HorizontalLayoutGroup>();
            if (!region.gameObject.activeSelf
                || !heartRow.gameObject.activeSelf
                || layout == null)
            {
                throw new InvalidOperationException(
                    "HealthHUD must contain an active HeartRow with a " +
                    "HorizontalLayoutGroup for HealthHudPresenter to " +
                    "populate.");
            }

            if (region.Find("Icon") != null
                || region.Find("ValueText") != null
                || region.Find("ShadowText") != null)
            {
                throw new InvalidOperationException(
                    "HealthHUD must not keep its old icon+text pair " +
                    "alongside the live heart row.");
            }
        }

        // No shadow copy anymore (removed by hand for a flatter look, see
        // ConfigureTopHudRegion) -- this only checks the value text itself.
        private static void ValidateTopHudRegion(
            Transform region,
            string expectedValue,
            string expectedIconPath)
        {
            TextMeshProUGUI text = RequireChild(region, "ValueText")
                .GetComponent<TextMeshProUGUI>();
            if (!region.gameObject.activeSelf
                || text == null
                || (expectedValue != null && text.text != expectedValue)
                || AssetDatabase.GetAssetPath(text.font) != TopHudFontPath
                || text.alignment != TextAlignmentOptions.Center
                || text.color != TopHudTextBrown)
            {
                throw new InvalidOperationException(
                    $"TopHUD region '{region.name}' is not wired to its " +
                    "exact font and centered brown text.");
            }

            if (expectedIconPath != null)
            {
                Transform icon = RequireChild(region, "Icon");
                Image iconImage = icon.GetComponent<Image>();
                if (!icon.gameObject.activeSelf
                    || iconImage == null
                    || AssetDatabase.GetAssetPath(iconImage.sprite)
                        != expectedIconPath)
                {
                    throw new InvalidOperationException(
                        $"TopHUD region '{region.name}' is missing its " +
                        "expected icon.");
                }
            }
        }

        private static void ValidateBottomHudSkillRow(RectTransform skillRow)
        {
            Transform freeze = RequireChild(skillRow, "FreezePulseButton");
            Transform instant = RequireChild(skillRow, "InstantBarrierButton");
            Transform gravity = RequireChild(skillRow, "GravityWellButton");
            if (!freeze.gameObject.activeSelf
                || !instant.gameObject.activeSelf
                || !gravity.gameObject.activeSelf)
            {
                throw new InvalidOperationException(
                    "SkillRow must show all three skill slots by default.");
            }

            if (AssetDatabase.GetAssetPath(
                    freeze.GetComponent<Image>()?.sprite) != FreezeSkillPath
                || AssetDatabase.GetAssetPath(
                    instant.GetComponent<Image>()?.sprite)
                    != InstantBarrierSkillPath
                || AssetDatabase.GetAssetPath(
                    gravity.GetComponent<Image>()?.sprite)
                    != GravityWellSkillPath)
            {
                throw new InvalidOperationException(
                    "SkillRow slots are not wired to their imported skill " +
                    "artwork.");
            }

            Button gravityButton = gravity.GetComponent<Button>();
            Outline gravityHighlight = gravity.GetComponent<Outline>();
            if (gravityButton == null
                || gravityHighlight == null
                || gravityHighlight.effectColor != GravityTargetingHighlight
                || gravityHighlight.effectDistance != new Vector2(4f, -4f)
                || !gravityHighlight.useGraphicAlpha
                || gravityHighlight.enabled)
            {
                throw new InvalidOperationException(
                    "The Gravity Well skill slot requires its Button and " +
                    "disabled HUD-yellow targeting highlight.");
            }
        }

        private static void ValidateGravityWellVortex(GameObject root)
        {
            GravityWellPresenter[] presenters = root
                .GetComponentsInChildren<GravityWellPresenter>(true);
            if (presenters.Length != 1)
            {
                throw new InvalidOperationException(
                    "Chapter 2 requires one Gravity Well presenter.");
            }

            GravityWellPresenter presenter = presenters[0];
            RectTransform vortexRoot = presenter.VortexRoot;
            Image vortexImage = presenter.VortexImage;
            if (presenter.Controller == null
                || presenter.BoardFrame == null
                || presenter.CueRoot == null
                || !presenter.CueRoot.gameObject.activeSelf
                || vortexRoot == null
                || vortexRoot.name != "Vortex"
                || vortexImage == null
                || AssetDatabase.GetAssetPath(vortexImage.sprite)
                    != GravityWellVortexPath
                || !vortexImage.preserveAspect
                || vortexImage.raycastTarget
                || vortexRoot.anchorMin != Vector2.zero
                || vortexRoot.anchorMax != Vector2.one
                || presenter.CueRoot.Find("Icon") != null
                || presenter.CueRoot.Find("Range") != null)
            {
                throw new InvalidOperationException(
                    "Gravity Well must use only the radius-sized Vortex " +
                    "effect without the legacy icon or range ring.");
            }
        }

        private static void ValidateGeneralButtonStyle(Button button)
        {
            Image image = button != null ? button.GetComponent<Image>() : null;
            Graphic label = button != null
                ? button.GetComponentInChildren<Text>(true)
                : null;
            if (label == null && button != null)
            {
                label = button.GetComponentInChildren<TMP_Text>(true);
            }

            if (button == null
                || image == null
                || image.type != Image.Type.Sliced
                || AssetDatabase.GetAssetPath(image.sprite)
                    != GeneralButtonBackgroundPath
                || label == null
                || !IsGeneralButtonLabelConfigured(label)
                || label.GetComponent<Shadow>() == null
                || button.GetComponent<AspectRatioFitter>() == null
                || button.GetComponent<AspectRatioFitter>().aspectMode
                    != AspectRatioFitter.AspectMode.WidthControlsHeight
                || !Mathf.Approximately(
                    button.GetComponent<AspectRatioFitter>().aspectRatio,
                    512f / 210f))
            {
                throw new InvalidOperationException(
                    "Text buttons must use GeneralButtonBackground with a " +
                    "centered white shadowed label.");
            }
        }

        private static bool IsGeneralButtonLabelConfigured(Graphic label)
        {
            bool centered = label is Text legacy
                ? legacy.alignment == TextAnchor.MiddleCenter
                : label is TMP_Text tmp
                    && tmp.alignment == TextAlignmentOptions.Center;
            bool readableSize = label is Text legacyText
                ? legacyText.fontSize == 40
                    && legacyText.resizeTextForBestFit
                    && legacyText.resizeTextMaxSize == 40
                : label is TMP_Text tmpText
                    && tmpText.enableAutoSizing
                    && Mathf.Approximately(tmpText.fontSizeMax, 40f);
            RectTransform rect = label.rectTransform;
            return centered
                && readableSize
                && label.color == Color.white
                && rect.anchorMin == new Vector2(0.08f, 0.08f)
                && rect.anchorMax == new Vector2(0.98f, 0.96f)
                && rect.offsetMin == new Vector2(0f, 8f)
                && rect.offsetMax == new Vector2(0f, 8f);
        }

        private static void ValidateTopHudBoardSeparation(
            Transform safeArea,
            BoardCameraFitter fitter)
        {
            RectTransform topHud = (RectTransform)RequireChild(
                safeArea,
                "TopHUD");
            RectTransform boardStage = (RectTransform)RequireChild(
                safeArea,
                "BoardStage");
            RectTransform boardViewport = (RectTransform)RequireChild(
                boardStage,
                "BoardViewport");
            if (fitter.BoardStage != boardStage
                || fitter.BoardViewport != boardViewport)
            {
                throw new InvalidOperationException(
                    "TopHUD setup must keep the existing BoardCameraFitter " +
                    "hierarchy intact.");
            }

            float aspect = boardViewport.rect.width
                / boardViewport.rect.height;
            if (!Mathf.Approximately(
                    aspect,
                    BoardViewportLayout.LogicalWidth
                        / BoardViewportLayout.LogicalHeight))
            {
                throw new InvalidOperationException(
                    "Gameplay board must remain the exact logical 10x16 " +
                    "aspect after fitting below TopHUD.");
            }

            var topCorners = new Vector3[4];
            var boardCorners = new Vector3[4];
            topHud.GetWorldCorners(topCorners);
            boardViewport.GetWorldCorners(boardCorners);
            if (topCorners[0].y < boardCorners[2].y - 0.01f)
            {
                throw new InvalidOperationException(
                    "TopHUD must remain fully outside the fitted gameplay " +
                    "board input rect.");
            }
        }

        private static void ValidateBoardHierarchy(GameObject root)
        {
            Transform safeArea = RequireChild(
                root.transform,
                "Canvas/SafeAreaRoot");
            Transform topHud = RequireChild(safeArea, "TopHUD");
            Transform boardStage = RequireChild(safeArea, "BoardStage");
            Transform boardViewport = RequireChild(boardStage, "BoardViewport");
            Transform boardFrame = RequireChild(boardViewport, "BoardFrame");
            Transform bottomHud = RequireChild(safeArea, "BottomHUD");

            VerticalLayoutGroup safeLayout =
                safeArea.GetComponent<VerticalLayoutGroup>();
            LayoutElement topLayout = topHud.GetComponent<LayoutElement>();
            LayoutElement stageLayout = boardStage.GetComponent<LayoutElement>();
            LayoutElement bottomLayout = bottomHud.GetComponent<LayoutElement>();
            if (safeLayout == null
                || !safeLayout.childControlHeight
                || safeLayout.childForceExpandHeight
                || safeLayout.padding.left != SafeAreaHorizontalPadding
                || safeLayout.padding.right != SafeAreaHorizontalPadding
                || safeLayout.padding.top != SafeAreaVerticalPadding
                || safeLayout.padding.bottom != SafeAreaVerticalPadding
                || !Mathf.Approximately(
                    safeLayout.spacing,
                    SafeAreaSectionSpacing)
                || topLayout == null
                || !Mathf.Approximately(
                    topLayout.preferredHeight,
                    TopHudPreferredHeight)
                || !Mathf.Approximately(topLayout.flexibleHeight, 0f)
                || stageLayout == null
                || !Mathf.Approximately(stageLayout.preferredHeight, 0f)
                || !Mathf.Approximately(stageLayout.flexibleHeight, 1f)
                || bottomLayout == null
                || !Mathf.Approximately(
                    bottomLayout.preferredHeight,
                    BottomHudPreferredHeight)
                || !Mathf.Approximately(bottomLayout.flexibleHeight, 0f)
                || topHud.GetSiblingIndex() >= boardStage.GetSiblingIndex()
                || boardStage.GetSiblingIndex() >= bottomHud.GetSiblingIndex())
            {
                throw new InvalidOperationException(
                    "SafeAreaRoot must keep compact fixed TopHUD/BottomHUD " +
                    "bands around one flexible BoardStage region.");
            }

            LayoutElement viewportLayout =
                boardViewport.GetComponent<LayoutElement>();
            if (viewportLayout == null || !viewportLayout.ignoreLayout)
            {
                throw new InvalidOperationException(
                    "BoardViewport must be ignoreLayout so BoardCameraFitter " +
                    "-- not the VerticalLayoutGroup -- controls its size.");
            }

            var boardViewportRect = (RectTransform)boardViewport;
            var boardFrameRect = (RectTransform)boardFrame;
            if (boardFrameRect.anchorMin != Vector2.zero
                || boardFrameRect.anchorMax != Vector2.one
                || boardFrameRect.offsetMin != Vector2.zero
                || boardFrameRect.offsetMax != Vector2.zero)
            {
                throw new InvalidOperationException(
                    "BoardFrame must stay a plain full-stretch child of " +
                    "BoardViewport so they always share the exact same " +
                    "final rect.");
            }

            BoardCameraFitter fitter = root
                .GetComponentInChildren<BoardCameraFitter>(true);
            if (fitter == null
                || fitter.BoardStage != boardStage
                || fitter.BoardViewport != boardViewportRect
                || fitter.BoardFrame != boardFrameRect
                || !Mathf.Approximately(fitter.VerticalAlignment, 0.5f))
            {
                throw new InvalidOperationException(
                    "BoardCameraFitter must be wired to BoardStage/" +
                    "BoardViewport/BoardFrame exactly as they exist in the " +
                    "scene.");
            }

            // BoardCameraFitter.RefreshNow() was already called in
            // Configure(), so BoardViewport should now sit at the real
            // fitted rect: same aspect as the logical board, and no larger
            // than BoardStage's own available area.
            float aspect = boardViewportRect.rect.width
                / boardViewportRect.rect.height;
            if (!Mathf.Approximately(
                    aspect,
                    BoardViewportLayout.LogicalWidth
                        / BoardViewportLayout.LogicalHeight))
            {
                throw new InvalidOperationException(
                    "BoardViewport must resolve to the exact 10:16 " +
                    "aspect-fitted rect, not an arbitrary container size.");
            }

            if (boardViewportRect.rect.width
                    > boardStage.GetComponent<RectTransform>().rect.width + 0.5f
                || boardViewportRect.rect.height
                    > boardStage.GetComponent<RectTransform>().rect.height + 0.5f)
            {
                throw new InvalidOperationException(
                    "BoardViewport must not be larger than BoardStage's " +
                    "available area.");
            }

            if (boardViewportRect.anchoredPosition.sqrMagnitude > 0.01f)
            {
                throw new InvalidOperationException(
                    "The fitted BoardViewport must remain centered inside " +
                    "the flexible BoardStage region.");
            }
        }

        private static void ValidateGameplayBandVisualSeparation(
            GameObject root)
        {
            Transform safeArea = RequireChild(
                root.transform,
                "Canvas/SafeAreaRoot");
            RectTransform topHud = (RectTransform)RequireChild(
                safeArea,
                "TopHUD");
            RectTransform boardViewport = (RectTransform)RequireChild(
                safeArea,
                "BoardStage/BoardViewport");
            RectTransform bottomHud = (RectTransform)RequireChild(
                safeArea,
                "BottomHUD");
            RectTransform progressSlot = (RectTransform)RequireChild(
                bottomHud,
                "BottomHudRow/ProgressSlot");
            RectTransform progress = (RectTransform)RequireChild(
                progressSlot,
                "ProgressBar");

            var topCorners = new Vector3[4];
            var boardCorners = new Vector3[4];
            var bottomCorners = new Vector3[4];
            var progressCorners = new Vector3[4];
            var progressSlotCorners = new Vector3[4];
            topHud.GetWorldCorners(topCorners);
            boardViewport.GetWorldCorners(boardCorners);
            bottomHud.GetWorldCorners(bottomCorners);
            progress.GetWorldCorners(progressCorners);
            progressSlot.GetWorldCorners(progressSlotCorners);

            // The bar now sits flush against its own left-half ProgressSlot's
            // leading (left) and trailing (right) edges, with only a small
            // left inset for breathing room (see SandProgressPresenter.
            // RefreshLayoutNow's ParentSidePadding) -- not centered -- while
            // still staying fully inside BottomHUD's bounds and vertically
            // centered within the slot.
            if (topCorners[0].y < boardCorners[2].y - 0.01f
                || boardCorners[0].y < bottomCorners[2].y - 0.01f
                || progressCorners[0].x < bottomCorners[0].x - 0.01f
                || progressCorners[2].x > bottomCorners[2].x + 0.01f
                || progressCorners[0].y < bottomCorners[0].y - 0.01f
                || progressCorners[2].y > bottomCorners[2].y + 0.01f
                || progressCorners[0].x <= progressSlotCorners[0].x + 0.01f
                || !Mathf.Approximately(
                    progressCorners[2].x,
                    progressSlotCorners[2].x)
                || !Mathf.Approximately(
                    (progressCorners[0].y + progressCorners[2].y) * 0.5f,
                    (progressSlotCorners[0].y + progressSlotCorners[2].y)
                        * 0.5f))
            {
                throw new InvalidOperationException(
                    "TopHUD, fitted 10x16 board, and the BottomHUD " +
                    "progress bar (flush against its left ProgressSlot " +
                    "half's leading and trailing edges) must remain " +
                    "separate and fully contained.");
            }
        }

        // ------------------------------------------------------------
        // Shared helpers
        // ------------------------------------------------------------

        public static Font LoadLegacyUiFontForSetup()
        {
            Font font = AssetDatabase.LoadAssetAtPath<Font>(
                CompletionFontPath);
            return font ?? throw new InvalidOperationException(
                $"Legacy UI font is missing at '{CompletionFontPath}'.");
        }

        public static TMP_FontAsset LoadTmpUiFontForSetup()
        {
            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
                TopHudFontPath);
            return font ?? throw new InvalidOperationException(
                $"TMP UI font is missing at '{TopHudFontPath}'.");
        }

        private static Font LoadCompletionFont()
        {
            return LoadLegacyUiFontForSetup();
        }

        private static void ApplyCompletionReadability(
            Transform completionOverlay,
            Font font)
        {
            Transform content = RequireChild(
                completionOverlay,
                "CompletionContent");
            VerticalLayoutGroup column =
                GetOrAddComponent<VerticalLayoutGroup>(content.gameObject);
            column.padding = new RectOffset(0, 0, 0, 0);
            column.spacing = 6f;
            column.childAlignment = TextAnchor.UpperCenter;
            column.childControlWidth = true;
            column.childControlHeight = true;
            column.childForceExpandWidth = true;
            column.childForceExpandHeight = false;

            Text title = RequireText(content, "Title");
            ConfigureCompletionText(
                title,
                font,
                56,
                32,
                TextAnchor.MiddleCenter,
                1f);
            title.fontStyle = FontStyle.Bold;
            ConfigureCompletionLayoutElement(
                title.rectTransform,
                76f,
                76f,
                0f);

            Text sector = RequireText(content, "Sector");
            ConfigureCompletionText(
                sector,
                font,
                30,
                20,
                TextAnchor.MiddleCenter,
                1f);
            ConfigureCompletionLayoutElement(
                sector.rectTransform,
                42f,
                42f,
                0f);

            Text description = RequireText(content, "Description");
            ConfigureCompletionText(
                description,
                font,
                36,
                22,
                TextAnchor.UpperCenter,
                1f);
            description.horizontalOverflow = HorizontalWrapMode.Wrap;
            description.verticalOverflow = VerticalWrapMode.Truncate;
            ConfigureCompletionLayoutElement(
                description.rectTransform,
                170f,
                230f,
                1f);

            Text summary = RequireChild(completionOverlay, "CompleteText")
                .GetComponent<Text>()
                ?? throw new InvalidOperationException(
                    "CompleteText requires a Text component.");
            ConfigureCompletionText(
                summary,
                font,
                30,
                20,
                TextAnchor.MiddleCenter,
                0.9f);

            Text retryLabel = RequireText(
                completionOverlay,
                "RetryButton/Label");
            ConfigureCompletionText(
                retryLabel,
                font,
                40,
                20,
                TextAnchor.MiddleCenter,
                1f);
            Text nextLabel = RequireText(
                completionOverlay,
                "NextButton/Label");
            ConfigureCompletionText(
                nextLabel,
                font,
                40,
                20,
                TextAnchor.MiddleCenter,
                1f);

            EditorUtility.SetDirty(column);
        }

        private static Text RequireText(Transform parent, string path)
        {
            Transform transform = RequireChild(parent, path);
            return transform.GetComponent<Text>()
                ?? throw new InvalidOperationException(
                    $"'{parent.name}/{path}' requires a Text component.");
        }

        private static void ConfigureCompletionText(
            Text text,
            Font font,
            int maximumSize,
            int minimumSize,
            TextAnchor alignment,
            float lineSpacing)
        {
            text.font = font;
            text.fontSize = maximumSize;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = minimumSize;
            text.resizeTextMaxSize = maximumSize;
            text.alignment = alignment;
            text.lineSpacing = lineSpacing;
            text.raycastTarget = false;
            EditorUtility.SetDirty(text);
        }

        private static void ConfigureCompletionLayoutElement(
            RectTransform rect,
            float minimumHeight,
            float preferredHeight,
            float flexibleHeight)
        {
            LayoutElement layout = GetOrAddComponent<LayoutElement>(
                rect.gameObject);
            layout.minHeight = minimumHeight;
            layout.preferredHeight = preferredHeight;
            layout.flexibleHeight = flexibleHeight;
            EditorUtility.SetDirty(layout);
        }

        private static Text ConfigureText(
            RectTransform rect,
            string value,
            int fontSize,
            TextAnchor alignment,
            Color color)
        {
            Text text = GetOrAddComponent<Text>(rect.gameObject);
            text.font = LoadLegacyUiFontForSetup();
            text.text = value;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = color;
            text.raycastTarget = false;
            EditorUtility.SetDirty(text);
            return text;
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

        private readonly struct ProgressSprites
        {
            public ProgressSprites(
                Sprite background,
                Sprite fill)
            {
                Background = background;
                Fill = fill;
            }

            public Sprite Background { get; }
            public Sprite Fill { get; }
        }

        private readonly struct TopHudAssets
        {
            public TopHudAssets(
                Sprite background,
                Sprite healthIcon,
                Sprite speedIcon,
                Sprite settings,
                Sprite coin,
                TMP_FontAsset font)
            {
                Background = background;
                HealthIcon = healthIcon;
                SpeedIcon = speedIcon;
                Settings = settings;
                Coin = coin;
                Font = font;
            }

            public Sprite Background { get; }
            public Sprite HealthIcon { get; }
            public Sprite SpeedIcon { get; }
            public Sprite Settings { get; }
            public Sprite Coin { get; }
            public TMP_FontAsset Font { get; }
        }

        private readonly struct SkillAssets
        {
            public SkillAssets(Sprite freeze, Sprite instant, Sprite gravity)
            {
                Freeze = freeze;
                Instant = instant;
                Gravity = gravity;
            }

            public Sprite Freeze { get; }
            public Sprite Instant { get; }
            public Sprite Gravity { get; }
        }

        private enum GeneratedPattern
        {
            Frame,
            Board,
            BarrierBody,
            ThreatGem,
            PowerButton,
            LandmarkAlpine,
            LandmarkCoastal,
            LandmarkDesert,
            CompletionScrim,
            ChipRounded,
        }
    }
}
