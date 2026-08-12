using System;
using System.Linq;
using Cutrium.Presentation.HUD;
using Cutrium.Presentation.Landmark;
using Cutrium.Unity.Simulation;
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
            RectTransform boardStage = FindRect(root.transform, "BoardStage")
                ?? FindRect(root.transform, "BoardViewport");
            if (safeArea == null || bottomHud == null || boardStage == null)
            {
                throw new InvalidOperationException(
                    "Identity HUD requires SafeAreaRoot, BoardStage, and BottomHUD.");
            }

            GameplayIdentityHudPresenter presenter =
                GetOrAddComponent<GameplayIdentityHudPresenter>(
                    safeArea.gameObject);

            Text cutText = GetOrCreateText(
                bottomHud,
                "CutLimitCounter",
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0f, -4f),
                new Vector2(260f, 30f),
                20,
                TextAnchor.MiddleCenter);
            cutText.color = new Color(0.22f, 0.1f, 0.035f, 1f);
            cutText.raycastTarget = false;

            RectTransform introRect = GetOrCreateRect(
                boardStage,
                "MechanicIntro",
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0f, -36f),
                new Vector2(520f, 94f));
            CanvasGroup introGroup = GetOrAddComponent<CanvasGroup>(
                introRect.gameObject);
            introGroup.blocksRaycasts = false;
            introGroup.interactable = false;
            Text introTitle = GetOrCreateText(
                introRect,
                "Title",
                new Vector2(0f, 0.48f),
                Vector2.one,
                Vector2.zero,
                Vector2.zero,
                30,
                TextAnchor.LowerCenter);
            Text introMessage = GetOrCreateText(
                introRect,
                "Message",
                Vector2.zero,
                new Vector2(1f, 0.5f),
                Vector2.zero,
                Vector2.zero,
                18,
                TextAnchor.UpperCenter);
            introTitle.color = new Color(1f, 0.82f, 0.3f, 1f);
            introMessage.color = Color.white;
            introTitle.raycastTarget = false;
            introMessage.raycastTarget = false;

            RectTransform failureRect = GetOrCreateRect(
                safeArea,
                "CutLimitFailureOverlay",
                Vector2.zero,
                Vector2.one,
                Vector2.zero,
                Vector2.zero);
            CanvasGroup failureGroup = GetOrAddComponent<CanvasGroup>(
                failureRect.gameObject);
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
            failureRect.SetAsLastSibling();

            Undo.RecordObject(presenter, "Configure Gameplay Identity HUD");
            presenter.ConfigureForSetup(
                controller,
                cutText,
                introGroup,
                introTitle,
                introMessage,
                failureGroup,
                failureText,
                retryButton);
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
            text.font = AssetDatabase.LoadAssetAtPath<Font>(
                "Assets/Cutrium/Art/Fonts/gomarice_rocks.ttf")
                ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 10;
            text.resizeTextMaxSize = fontSize;
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
