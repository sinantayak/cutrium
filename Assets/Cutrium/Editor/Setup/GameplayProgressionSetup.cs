using System;
using System.Linq;
using Cutrium.Presentation.Landmark;
using Cutrium.Unity.Simulation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

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
            if (landmarkPresenter != null
                && landmarkPresenter.Landmarks != null
                && landmarkPresenter.Landmarks.Count > 0)
            {
                LandmarkCatalog landmarkCatalog =
                    GetOrCreateAsset<LandmarkCatalog>(LandmarkCatalogPath);
                landmarkCatalog.ConfigureForSetup(landmarkPresenter.Landmarks);
                EditorUtility.SetDirty(landmarkCatalog);
                Undo.RecordObject(
                    landmarkPresenter,
                    "Wire Separate Landmark Catalog");
                landmarkPresenter.ConfigureCatalogForSetup(landmarkCatalog);
                EditorUtility.SetDirty(landmarkPresenter);
            }

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
    }
}
