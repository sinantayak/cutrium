using System;
using System.Linq;
using Cutrium.Presentation.HUD;
using Cutrium.Presentation.Landmark;
using Cutrium.Presentation.Threats;
using Cutrium.Unity.Simulation;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Cutrium.Editor.Setup
{
    /// Focused, idempotent setup for progression data. It intentionally does
    /// not invoke any milestone or presentation setup, so user-authored HUD,
    /// color, sand, trail, and popup changes remain untouched.
    public static class GameplayProgressionSetup
    {
        public const string GameplayCatalogPath =
            "Assets/Cutrium/Content/Levels/First12GameplayCatalog.asset";

        public const string LandmarkCatalogPath =
            "Assets/Cutrium/Content/Landmarks/LandmarkCatalog.asset";

        [MenuItem("Cutrium/Setup/Fix Stale BottomHudRow LayoutElement Only")]
        public static void FixStaleBottomHudRowLayoutElementOnly()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException(
                    "Exit Play Mode before editing the scene.");
            }

            Scene scene = OpenVerticalSliceWithoutDiscardingDirtyScenes();
            GameObject root = scene.GetRootGameObjects().Single(
                candidate => candidate.name == "VerticalSliceRoot");
            RectTransform bottomHud = FindRect(root.transform, "BottomHUD");
            Transform bottomRow = bottomHud != null
                ? bottomHud.Find("BottomHudRow")
                : null;
            LayoutElement layout = bottomRow != null
                ? bottomRow.GetComponent<LayoutElement>()
                : null;
            if (layout == null || layout.flexibleHeight <= 0f)
            {
                Debug.Log(
                    "BottomHudRow's LayoutElement is already non-flexible; " +
                    "nothing to fix.");
                return;
            }

            layout.flexibleHeight = 0f;
            EditorUtility.SetDirty(layout);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(
                    scene,
                    Milestone2SceneSetup.VerticalSliceScenePath))
            {
                throw new InvalidOperationException(
                    "Unity could not save the BottomHudRow layout fix.");
            }

            AssetDatabase.SaveAssets();
            Debug.Log("Fixed BottomHudRow's stale flexible LayoutElement.");
        }

        [MenuItem("Cutrium/Setup/Remove Legacy BottomHUD Cut Counter Only")]
        public static void RemoveLegacyBottomHudCutCounterOnly()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException(
                    "Exit Play Mode before editing the scene.");
            }

            Scene scene = OpenVerticalSliceWithoutDiscardingDirtyScenes();
            GameObject root = scene.GetRootGameObjects().Single(
                candidate => candidate.name == "VerticalSliceRoot");
            RectTransform bottomHud = FindRect(root.transform, "BottomHUD");
            // The Cut counter moved into the TopHUD Cut panel (see
            // ConfigureIdentityHud); this discards the direct BottomHUD
            // child an earlier presentation pass left behind, which
            // Milestone2SceneSetup's baseline validation otherwise rejects
            // (every BottomHUD child must carry an explicit non-flexible
            // LayoutElement). Safe to run repeatedly -- a no-op once gone.
            Transform legacy = bottomHud != null
                ? bottomHud.Find("CutLimitCounter")
                : null;
            if (legacy == null)
            {
                Debug.Log(
                    "No legacy BottomHUD/CutLimitCounter element found; " +
                    "nothing to remove.");
                return;
            }

            UnityEngine.Object.DestroyImmediate(legacy.gameObject);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(
                    scene,
                    Milestone2SceneSetup.VerticalSliceScenePath))
            {
                throw new InvalidOperationException(
                    "Unity could not save the legacy cut counter removal.");
            }

            AssetDatabase.SaveAssets();
            Debug.Log("Removed the legacy BottomHUD/CutLimitCounter element.");
        }

        [MenuItem("Cutrium/Setup/Validate First 12 Gameplay Progression")]
        public static void ValidateExistingAssets()
        {
            CoreFunLevelCatalogDefinition gameplayCatalog =
                AssetDatabase.LoadAssetAtPath<CoreFunLevelCatalogDefinition>(
                    GameplayCatalogPath);
            LandmarkCatalog landmarkCatalog =
                AssetDatabase.LoadAssetAtPath<LandmarkCatalog>(
                    LandmarkCatalogPath);
            if (gameplayCatalog == null
                || gameplayCatalog.BuildRuntimeCatalog().Count
                    != FirstTwelveGameplayProgression.LevelCount)
            {
                throw new InvalidOperationException(
                    "The first-twelve gameplay catalog is invalid.");
            }

            if (landmarkCatalog == null
                || landmarkCatalog.Count
                    != FirstTwelveGameplayProgression.LevelCount)
            {
                throw new InvalidOperationException(
                    "The first-twelve landmark catalog is not materialized. " +
                    "Run First 12 Gameplay Progression setup in a licensed Editor.");
            }

            landmarkCatalog.Validate();
            Debug.Log("First 12 gameplay and landmark catalogs are valid.");
        }

        [MenuItem("Cutrium/Setup/First 12 Gameplay Progression")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException(
                    "Exit Play Mode before configuring progression assets.");
            }

            Scene scene = OpenVerticalSliceWithoutDiscardingDirtyScenes();
            CoreFunLevelCatalogDefinition gameplayCatalog =
                GetOrCreateAsset<CoreFunLevelCatalogDefinition>(
                    GameplayCatalogPath);
            gameplayCatalog.ConfigureForSetup(
                FirstTwelveGameplayProgression.CreateDefinitions());
            EditorUtility.SetDirty(gameplayCatalog);

            GameObject root = scene.GetRootGameObjects().Single(
                candidate => candidate.name == "VerticalSliceRoot");
            FirstPlayableController controller = root
                .GetComponentInChildren<FirstPlayableController>(true);
            if (controller == null)
            {
                throw new InvalidOperationException(
                    "VerticalSliceRoot is missing FirstPlayableController.");
            }

            Undo.RecordObject(controller, "Wire First 12 Gameplay Catalog");
            controller.ConfigureLevelCatalogForSetup(gameplayCatalog);
            EditorUtility.SetDirty(controller);

            LandmarkRevealPresenter landmarkPresenter = root
                .GetComponentInChildren<LandmarkRevealPresenter>(true);
            LandmarkDefinition[] landmarks =
                FirstTwelveLandmarkContent.CreateOrUpdateAssets();
            LandmarkCatalog landmarkCatalog =
                GetOrCreateAsset<LandmarkCatalog>(LandmarkCatalogPath);
            landmarkCatalog.ConfigureForSetup(landmarks);
            EditorUtility.SetDirty(landmarkCatalog);
            if (landmarkPresenter != null)
            {
                Undo.RecordObject(
                    landmarkPresenter,
                    "Wire Separate Landmark Catalog");
                landmarkPresenter.ConfigureCatalogForSetup(landmarkCatalog);
                EditorUtility.SetDirty(landmarkPresenter);
            }

            ConfigureIdentityHud(root, controller);
            ConfigureHealthHud(root, controller);
            ConfigurePreLevelIntro(root, controller);

            Validate(controller);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(
                    scene,
                    Milestone2SceneSetup.VerticalSliceScenePath))
            {
                throw new InvalidOperationException(
                    "Unity could not save the progression scene wiring.");
            }

            AssetDatabase.SaveAssets();
            Debug.Log(
                "First 12 gameplay progression configured. Gameplay and " +
                "landmark catalogs remain separate; no presentation setup " +
                "was run.");
        }

        private static Scene OpenVerticalSliceWithoutDiscardingDirtyScenes()
        {
            Scene active = SceneManager.GetActiveScene();
            if (active.path == Milestone2SceneSetup.VerticalSliceScenePath)
            {
                return active;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                throw new OperationCanceledException(
                    "Progression setup cancelled before changing scenes.");
            }

            return EditorSceneManager.OpenScene(
                Milestone2SceneSetup.VerticalSliceScenePath,
                OpenSceneMode.Single);
        }

        private static T GetOrCreateAsset<T>(string path)
            where T : ScriptableObject
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null)
            {
                return asset;
            }

            EnsureFolder(System.IO.Path.GetDirectoryName(path)
                ?.Replace('\\', '/'));
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static void EnsureFolder(string folderPath)
        {
            if (string.IsNullOrWhiteSpace(folderPath)
                || AssetDatabase.IsValidFolder(folderPath))
            {
                return;
            }

            string parent = System.IO.Path.GetDirectoryName(folderPath)
                ?.Replace('\\', '/');
            EnsureFolder(parent);
            string name = System.IO.Path.GetFileName(folderPath);
            AssetDatabase.CreateFolder(parent, name);
        }

        private static void Validate(FirstPlayableController controller)
        {
            if (controller.LevelCatalogDefinition == null
                || controller.LevelDefinitions.Count
                    != FirstTwelveGameplayProgression.LevelCount
                || controller.LevelCatalogDefinition.BuildRuntimeCatalog().Count
                    != FirstTwelveGameplayProgression.LevelCount)
            {
                throw new InvalidOperationException(
                    "The first-twelve gameplay catalog was not wired correctly.");
            }
        }

        private static void ConfigureIdentityHud(
            GameObject root,
            FirstPlayableController controller)
        {
            RectTransform safeArea = FindRect(root.transform, "SafeAreaRoot");
            RectTransform bottomHud = FindRect(root.transform, "BottomHUD");
            if (safeArea == null || bottomHud == null)
            {
                throw new InvalidOperationException(
                    "Identity HUD requires SafeAreaRoot and BottomHUD.");
            }

            GameplayIdentityHudPresenter presenter =
                GetOrAddComponent<GameplayIdentityHudPresenter>(
                    safeArea.gameObject);

            // The Cut counter's live text lives inside the TopHUD Cut panel
            // built by the Landmark presentation pass
            // (ConfigureGameplayTopHud) -- that pass must already have run.
            RectTransform cutPanel = FindRect(root.transform, "CutHUD");
            TMP_Text cutText = cutPanel != null
                ? cutPanel.Find("ValueText")?.GetComponent<TMP_Text>()
                : null;
            if (cutText == null)
            {
                throw new InvalidOperationException(
                    "Identity HUD requires the TopHUD Cut panel from the " +
                    "Landmark presentation pass to already exist.");
            }

            // Same idea for the Speed region: its placeholder text is
            // replaced every frame with the current level's real
            // growing-barrier speed (see
            // GameplayIdentityHudPresenter.RefreshNow), which varies per
            // level instead of holding one fixed value.
            RectTransform speedPanel = FindRect(root.transform, "SpeedHUD");
            TMP_Text speedText = speedPanel != null
                ? speedPanel.Find("ValueText")?.GetComponent<TMP_Text>()
                : null;
            if (speedText == null)
            {
                throw new InvalidOperationException(
                    "Identity HUD requires the TopHUD Speed region from " +
                    "the Landmark presentation pass to already exist.");
            }

            // The speedometer needle sprite: same live-per-level idea as
            // the Cut/Speed text above, just swapping an icon instead of a
            // string (see GameplayIdentityHudPresenter.RefreshNow).
            Image speedIconImage = speedPanel.Find("Icon")
                ?.GetComponent<Image>();
            Sprite[] speedTierSprites =
            {
                AssetDatabase.LoadAssetAtPath<Sprite>(
                    LandmarkRevealPresentationSetup.SpeedIconL1Path),
                AssetDatabase.LoadAssetAtPath<Sprite>(
                    LandmarkRevealPresentationSetup.SpeedIconL2Path),
                AssetDatabase.LoadAssetAtPath<Sprite>(
                    LandmarkRevealPresentationSetup.SpeedIconL3Path),
                AssetDatabase.LoadAssetAtPath<Sprite>(
                    LandmarkRevealPresentationSetup.SpeedIconL4Path),
            };
            if (speedIconImage == null || speedTierSprites.Any(s => s == null))
            {
                throw new InvalidOperationException(
                    "Identity HUD requires the Speed icon and all four " +
                    "SpeedIconL1..L4 sprites from the Landmark " +
                    "presentation pass to already exist.");
            }

            RectTransform failureRect = GetOrCreateRect(
                safeArea,
                "CutLimitFailureOverlay",
                Vector2.zero,
                Vector2.one,
                Vector2.zero,
                Vector2.zero);
            CanvasGroup failureGroup = GetOrAddComponent<CanvasGroup>(
                failureRect.gameObject);
            LayoutElement failureLayout = GetOrAddComponent<LayoutElement>(
                failureRect.gameObject);
            failureLayout.ignoreLayout = true;
            Image scrim = GetOrAddComponent<Image>(failureRect.gameObject);
            scrim.color = new Color(0.08f, 0.035f, 0.02f, 0.86f);
            scrim.raycastTarget = true;
            Text failureText = GetOrCreateText(
                failureRect,
                "FailureText",
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, 55f),
                new Vector2(520f, 120f),
                32,
                TextAnchor.MiddleCenter);
            failureText.color = Color.white;

            RectTransform retryRect = GetOrCreateRect(
                failureRect,
                "RetryButton",
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, -60f),
                new Vector2(240f, 64f));
            Image retryImage = GetOrAddComponent<Image>(
                retryRect.gameObject);
            retryImage.color = new Color(0.92f, 0.5f, 0.12f, 1f);
            Button retryButton = GetOrAddComponent<Button>(
                retryRect.gameObject);
            retryButton.targetGraphic = retryImage;
            Text retryLabel = GetOrCreateText(
                retryRect,
                "Label",
                Vector2.zero,
                Vector2.one,
                Vector2.zero,
                Vector2.zero,
                26,
                TextAnchor.MiddleCenter);
            retryLabel.text = "RETRY";
            retryLabel.color = new Color(0.2f, 0.08f, 0.02f, 1f);
            retryLabel.raycastTarget = false;
            // LevelCompleteOverlay must remain the final safe-area sibling
            // (see LandmarkRevealPresentationSetup's
            // ConfigureGrainFlightRoot/Validate) so the completion screen
            // still renders on top of a still-visible cut-limit failure.
            // SetSiblingIndex(completionOverlay.GetSiblingIndex()) does NOT
            // achieve that -- moving failureRect *to* completion's index
            // displaces completion to one slot earlier instead of landing
            // failureRect just before it. Re-asserting completion as last
            // *after* placing failureRect, the same pattern
            // ConfigureGrainFlightRoot already uses, is unambiguous.
            failureRect.SetAsLastSibling();
            Transform completionOverlay = safeArea.Find("LevelCompleteOverlay");
            if (completionOverlay != null)
            {
                completionOverlay.SetAsLastSibling();
            }

            EditorUtility.SetDirty(failureLayout);

            Undo.RecordObject(presenter, "Configure Gameplay Identity HUD");
            presenter.ConfigureForSetup(
                controller,
                cutText,
                speedText,
                speedIconImage,
                speedTierSprites,
                failureGroup,
                failureText,
                retryButton);
            EditorUtility.SetDirty(presenter);
        }

        // Wires the live heart row (HealthHudPresenter) built by
        // LandmarkRevealPresentationSetup.ConfigureHealthRegion -- kept in
        // this pass, not that one, so the burn-limit-driven heart count
        // stays alongside the other live controller-driven wiring
        // (ConfigureIdentityHud's Cut/Speed text).
        private static void ConfigureHealthHud(
            GameObject root,
            FirstPlayableController controller)
        {
            RectTransform heartRow = FindRect(root.transform, "HeartRow");
            if (heartRow == null)
            {
                throw new InvalidOperationException(
                    "Health HUD requires the HeartRow from the Landmark " +
                    "presentation pass to already exist.");
            }

            Sprite heartSprite = AssetDatabase.LoadAssetAtPath<Sprite>(
                LandmarkRevealPresentationSetup.HealthIconPath);
            if (heartSprite == null)
            {
                throw new InvalidOperationException(
                    "Health HUD requires the heart sprite at " +
                    LandmarkRevealPresentationSetup.HealthIconPath +
                    " to already be imported by the Landmark " +
                    "presentation pass.");
            }

            HealthHudPresenter presenter = GetOrAddComponent<HealthHudPresenter>(
                heartRow.gameObject);
            Undo.RecordObject(presenter, "Configure Health HUD");
            presenter.ConfigureForSetup(controller, heartRow, heartSprite);
            EditorUtility.SetDirty(presenter);
        }

        // Big, staged "LEVEL N -> TARGET X% -> intro copy" text that plays
        // before a genuinely new level starts, while only the threats stay
        // hidden (see PreLevelIntroPresenter/ThreatPresenter.SetVisible) --
        // the board, sand, and landmark art stay visible and ready. Runs
        // after ConfigureIdentityHud so the TopHUD Cut panel and the
        // sand-flight FillStartTarget it lands text on already exist.
        private static void ConfigurePreLevelIntro(
            GameObject root,
            FirstPlayableController controller)
        {
            RectTransform safeArea = FindRect(root.transform, "SafeAreaRoot");
            if (safeArea == null)
            {
                throw new InvalidOperationException(
                    "Pre-level intro requires SafeAreaRoot.");
            }

            ThreatPresenter threatPresenter =
                root.GetComponentInChildren<ThreatPresenter>(true);
            if (threatPresenter == null)
            {
                throw new InvalidOperationException(
                    "Pre-level intro requires a ThreatPresenter already " +
                    "wired into the scene.");
            }

            RectTransform progressDestination =
                FindRect(root.transform, "FillStartTarget");
            if (progressDestination == null)
            {
                throw new InvalidOperationException(
                    "Pre-level intro requires the progress bar's " +
                    "FillStartTarget from the Landmark presentation pass " +
                    "to already exist.");
            }

            RectTransform cutPanel = FindRect(root.transform, "CutHUD");
            RectTransform cutDestination = cutPanel != null
                ? cutPanel.Find("ValueText") as RectTransform
                : null;
            if (cutDestination == null)
            {
                throw new InvalidOperationException(
                    "Pre-level intro requires the TopHUD Cut panel from " +
                    "the Landmark presentation pass to already exist.");
            }

            RectTransform flightRoot = GetOrCreateRect(
                safeArea,
                "PreLevelIntroRoot",
                Vector2.zero,
                Vector2.one,
                Vector2.zero,
                Vector2.zero);
            LayoutElement flightLayout =
                GetOrAddComponent<LayoutElement>(flightRoot.gameObject);
            flightLayout.ignoreLayout = true;
            EditorUtility.SetDirty(flightLayout);
            flightRoot.SetAsLastSibling();
            // Re-assert the overlays that must render above this sequence
            // (matches ConfigureGrainFlightRoot's/ConfigureIdentityHud's
            // failureRect pattern -- re-asserting as last is unambiguous,
            // moving to a captured index is not).
            Transform failureOverlay = safeArea.Find("CutLimitFailureOverlay");
            if (failureOverlay != null)
            {
                failureOverlay.SetAsLastSibling();
            }

            Transform completionOverlay = safeArea.Find("LevelCompleteOverlay");
            if (completionOverlay != null)
            {
                completionOverlay.SetAsLastSibling();
            }

            RectTransform levelRect = GetOrCreateRect(
                flightRoot,
                "LevelGroup",
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(900f, 160f));
            CanvasGroup levelGroup =
                GetOrAddComponent<CanvasGroup>(levelRect.gameObject);
            TMP_Text levelText = GetOrCreateTmpChild(
                levelRect,
                "Text",
                PreLevelIntroTextBrown,
                72);

            RectTransform targetRect = GetOrCreateRect(
                flightRoot,
                "TargetGroup",
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(900f, 160f));
            CanvasGroup targetGroup =
                GetOrAddComponent<CanvasGroup>(targetRect.gameObject);
            TMP_Text targetText = GetOrCreateTmpChild(
                targetRect,
                "Text",
                PreLevelIntroTextBrown,
                72);

            // Title and message sit close together as one tight block (not
            // spread across a tall box) -- a small +/-6 nudge off each
            // half's own edge, not the ~45 gap this started with.
            RectTransform infoRect = GetOrCreateRect(
                flightRoot,
                "InfoGroup",
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(900f, 130f));
            CanvasGroup infoGroup =
                GetOrAddComponent<CanvasGroup>(infoRect.gameObject);
            RectTransform infoTitleRect = GetOrCreateRect(
                infoRect,
                "Title",
                new Vector2(0f, 0.5f),
                Vector2.one,
                new Vector2(0f, 6f),
                Vector2.zero);
            TMP_Text infoTitleText = ConfigureTmp(
                infoTitleRect,
                PreLevelIntroTextBrown,
                70);
            RectTransform infoMessageRect = GetOrCreateRect(
                infoRect,
                "Message",
                Vector2.zero,
                new Vector2(1f, 0.5f),
                new Vector2(0f, -6f),
                Vector2.zero);
            TMP_Text infoMessageText = ConfigureTmp(
                infoMessageRect,
                Color.white,
                38);

            PreLevelIntroPresenter presenter =
                GetOrAddComponent<PreLevelIntroPresenter>(safeArea.gameObject);
            Undo.RecordObject(presenter, "Configure Pre-Level Intro");
            presenter.ConfigureForSetup(
                controller,
                threatPresenter,
                levelGroup,
                levelText,
                targetGroup,
                targetText,
                infoGroup,
                infoTitleText,
                infoMessageText,
                flightRoot,
                progressDestination,
                cutDestination);
            EditorUtility.SetDirty(presenter);
        }

        private static RectTransform FindRect(
            Transform root,
            string name)
        {
            if (root.name == name)
            {
                return root as RectTransform;
            }

            for (int index = 0; index < root.childCount; index++)
            {
                RectTransform found = FindRect(root.GetChild(index), name);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private static RectTransform GetOrCreateRect(
            RectTransform parent,
            string name,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 anchoredPosition,
            Vector2 sizeDelta)
        {
            Transform existing = parent.Find(name);
            RectTransform rect;
            if (existing != null)
            {
                rect = existing as RectTransform;
            }
            else
            {
                var gameObject = new GameObject(name, typeof(RectTransform));
                Undo.RegisterCreatedObjectUndo(gameObject, "Create " + name);
                rect = (RectTransform)gameObject.transform;
                rect.SetParent(parent, false);
            }

            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;
            rect.localScale = Vector3.one;
            return rect;
        }

        private static Text GetOrCreateText(
            RectTransform parent,
            string name,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 anchoredPosition,
            Vector2 sizeDelta,
            int fontSize,
            TextAnchor alignment)
        {
            RectTransform rect = GetOrCreateRect(
                parent,
                name,
                anchorMin,
                anchorMax,
                anchoredPosition,
                sizeDelta);
            Text text = GetOrAddComponent<Text>(rect.gameObject);
            text.font =
                LandmarkRevealPresentationSetup.LoadLegacyUiFontForSetup();
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 10;
            text.resizeTextMaxSize = fontSize;
            return text;
        }

        private static TMP_Text GetOrCreateTmpChild(
            RectTransform parent,
            string name,
            Color color,
            int fontSize)
        {
            RectTransform rect = GetOrCreateRect(
                parent,
                name,
                Vector2.zero,
                Vector2.one,
                Vector2.zero,
                Vector2.zero);
            return ConfigureTmp(rect, color, fontSize);
        }

        // Brown against the sand/board background reads far better than
        // the pale yellow this started with (see feedback cue text for the
        // same brown+shadow treatment this now matches).
        private static readonly Color PreLevelIntroTextBrown =
            new Color(0.34f, 0.105f, 0.025f, 1f);

        private static TMP_Text ConfigureTmp(
            RectTransform rect,
            Color color,
            int fontSize)
        {
            TextMeshProUGUI text =
                GetOrAddComponent<TextMeshProUGUI>(rect.gameObject);
            text.font = LandmarkRevealPresentationSetup.LoadTmpUiFontForSetup();
            text.fontSize = fontSize;
            text.alignment = TextAlignmentOptions.Center;
            text.enableAutoSizing = false;
            text.color = color;
            text.raycastTarget = false;
            Shadow shadow = GetOrAddComponent<Shadow>(rect.gameObject);
            shadow.effectColor = new Color(0f, 0f, 0f, 0.65f);
            shadow.effectDistance = new Vector2(3f, -3f);
            return text;
        }

        private static T GetOrAddComponent<T>(GameObject gameObject)
            where T : Component
        {
            T component = gameObject.GetComponent<T>();
            if (component == null)
            {
                component = Undo.AddComponent<T>(gameObject);
            }

            if (component == null)
            {
                throw new InvalidOperationException(
                    $"Could not add {typeof(T).Name} to {gameObject.name}.");
            }

            return component;
        }
    }
}
