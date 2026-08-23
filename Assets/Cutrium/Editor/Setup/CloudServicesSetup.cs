using System;
using Cutrium.Unity.Services;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Cutrium.Editor.Setup
{
    /// Idempotent wiring for Unity Gaming Services: creates (or reuses) a
    /// single CloudServicesBootstrap object under VerticalSliceRoot. Safe
    /// to run more than once -- never duplicates the object.
    public static class CloudServicesSetup
    {
        private const string ScenePath =
            "Assets/Cutrium/Scenes/VerticalSlice.unity";

        [MenuItem("Cutrium/Setup/Apply Cloud Services")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException(
                    "Exit Play Mode before applying cloud services setup.");
            }

            Scene scene = OpenVerticalSliceScene();
            GameObject root = RequireRoot(scene, "VerticalSliceRoot");

            GameObject cloudServicesObject = GetOrCreateChild(
                root.transform,
                "CloudServices");
            CloudServicesBootstrap bootstrap = GetOrAddComponent<
                CloudServicesBootstrap>(cloudServicesObject);

            EditorUtility.SetDirty(bootstrap);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                throw new InvalidOperationException(
                    "Unity could not save the cloud services setup.");
            }

            AssetDatabase.SaveAssets();
            Debug.Log(
                "Cloud services wired: CloudServicesBootstrap present "
                + "under VerticalSliceRoot/CloudServices. It signs in "
                + "anonymously on Play; this stays silent/offline until "
                + "Authentication is enabled for this project on the "
                + "Unity Dashboard.");
        }

        private static GameObject GetOrCreateChild(
            Transform parent,
            string name)
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

        private static T GetOrAddComponent<T>(GameObject gameObject)
            where T : Component
        {
            T component = gameObject.GetComponent<T>();
            return component != null
                ? component
                : gameObject.AddComponent<T>();
        }

        private static Scene OpenVerticalSliceScene()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (scene.path == ScenePath)
            {
                return scene;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                throw new OperationCanceledException(
                    "Cloud services setup cancelled before opening the "
                    + "scene.");
            }

            return EditorSceneManager.OpenScene(
                ScenePath,
                OpenSceneMode.Single);
        }

        private static GameObject RequireRoot(Scene scene, string name)
        {
            foreach (GameObject rootObject in scene.GetRootGameObjects())
            {
                if (rootObject.name == name)
                {
                    return rootObject;
                }
            }

            throw new InvalidOperationException(
                $"Scene does not contain required root '{name}'.");
        }
    }
}
