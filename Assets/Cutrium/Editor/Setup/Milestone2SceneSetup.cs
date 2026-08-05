using System;
using System.Linq;
using Cutrium.Presentation.Threats;
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
            ValidatePhase2A(scene);

            if (!EditorSceneManager.SaveScene(scene, VerticalSliceScenePath))
            {
                throw new InvalidOperationException(
                    $"Unity could not save '{VerticalSliceScenePath}'.");
            }

            AssetDatabase.SaveAssets();
            Debug.Log(
                "Milestone 2 scene setup verified through Phase 2A. " +
                "One controller and one replaceable threat presenter are serialized.");
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
