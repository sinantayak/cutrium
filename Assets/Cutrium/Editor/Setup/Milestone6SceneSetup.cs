using System;
using System.Linq;
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
    public static class Milestone6SceneSetup
    {
        [MenuItem("Cutrium/Setup/Milestone 6 Threats And Powers")]
        public static void Apply()
        {
            VerifyBaseline();
            Milestone5SceneSetup.Apply();

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
                    "Unity could not save the Milestone 6 scene.");
            }

            AssetDatabase.SaveAssets();
            Debug.Log(
                "Milestone 6 scene setup verified. Freeze Pulse/Instant " +
                "Barrier controls are ready. The default scene keeps the " +
                "regression-tested Milestone 3 level catalog; run " +
                "'Cutrium/Setup/Load Milestone 6 Identity Levels (Manual " +
                "Playtest)' to swap in the five Hunter/Pulse/power " +
                "prototype levels for interactive review.");
        }

        [MenuItem(
            "Cutrium/Setup/Load Milestone 6 Identity Levels (Manual Playtest)")]
        public static void LoadIdentityLevelsForPlaytesting()
        {
            VerifyBaseline();
            Scene scene = EditorSceneManager.OpenScene(
                Milestone2SceneSetup.VerticalSliceScenePath,
                OpenSceneMode.Single);
            GameObject root = RequireRoot(scene, "VerticalSliceRoot");
            FirstPlayableController controller = root
                .GetComponentInChildren<FirstPlayableController>(true);
            controller.ConfigureLevelsForSetup(
                CoreFunLevelDefinition.CreateMilestone6Defaults());
            EditorUtility.SetDirty(controller);
            if (!EditorSceneManager.SaveScene(
                    scene,
                    Milestone2SceneSetup.VerticalSliceScenePath))
            {
                throw new InvalidOperationException(
                    "Unity could not save the Milestone 6 playtest scene.");
            }

            AssetDatabase.SaveAssets();
            Debug.Log(
                "Loaded the five Milestone 6 identity levels (hunter-alone, " +
                "pulse-alone, freeze-pulse-rescue, instant-barrier-finish, " +
                "identity-mix) into the scene for manual playtesting. This " +
                "intentionally replaces the Milestone 3 gate catalog in the " +
                "saved scene; re-run 'Cutrium/Setup/Milestone 3 Core-Fun " +
                "Build' (or discard the change) to restore it before " +
                "running the automated regression suites.");
        }

        private static void VerifyBaseline()
        {
            if (!string.Equals(
                    Application.unityVersion,
                    "6000.3.21f1",
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "Milestone 6 requires Unity 6000.3.21f1.");
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

        private static void Configure(Scene scene)
        {
            GameObject root = RequireRoot(scene, "VerticalSliceRoot");
            Transform safeArea = RequireChild(root.transform, "Canvas/SafeAreaRoot");
            FirstPlayableController controller = root
                .GetComponentInChildren<FirstPlayableController>(true);

            RectTransform powerControls = GetOrCreateUiChild(
                safeArea,
                "PowerControls");
            StretchToParent(powerControls);
            LayoutElement powerControlsLayout = GetOrAddComponent<LayoutElement>(
                powerControls.gameObject);
            powerControlsLayout.ignoreLayout = true;

            RectTransform freezeRoot = CreatePowerButton(
                powerControls,
                "FreezePulseButton",
                new Vector2(0.58f, 0.05f),
                new Vector2(0.79f, 0.16f),
                new Color(0.32f, 0.74f, 0.95f, 0.92f),
                out Button freezeButton,
                out Text freezeLabel);
            RectTransform instantRoot = CreatePowerButton(
                powerControls,
                "InstantBarrierButton",
                new Vector2(0.81f, 0.05f),
                new Vector2(0.98f, 0.16f),
                new Color(0.95f, 0.66f, 0.28f, 0.92f),
                out Button instantButton,
                out Text instantLabel);

            Transform completion = RequireChild(
                safeArea,
                "LevelCompleteOverlay");
            powerControls.SetSiblingIndex(Math.Max(
                0,
                completion.GetSiblingIndex()));
            completion.SetAsLastSibling();

            GameObject services = GetOrCreateChild(root.transform, "PowerServices");
            PowerHudPresenter presenter =
                GetOrAddComponent<PowerHudPresenter>(services);
            presenter.Configure(
                controller,
                freezeRoot.gameObject,
                freezeButton,
                freezeLabel,
                instantRoot.gameObject,
                instantButton,
                instantLabel);

            EditorUtility.SetDirty(controller);
            EditorUtility.SetDirty(powerControlsLayout);
            EditorUtility.SetDirty(presenter);
        }

        private static RectTransform CreatePowerButton(
            Transform parent,
            string name,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Color color,
            out Button button,
            out Text label)
        {
            RectTransform rect = GetOrCreateUiChild(parent, name);
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            Image image = GetOrAddComponent<Image>(rect.gameObject);
            image.color = color;
            button = GetOrAddComponent<Button>(rect.gameObject);
            button.targetGraphic = image;

            RectTransform labelRect = GetOrCreateUiChild(rect, "Label");
            StretchToParent(labelRect);
            label = GetOrAddComponent<Text>(labelRect.gameObject);
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = 13;
            label.fontStyle = FontStyle.Bold;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;
            label.raycastTarget = false;

            EditorUtility.SetDirty(image);
            EditorUtility.SetDirty(label);
            return rect;
        }

        private static void Validate(Scene scene)
        {
            GameObject root = RequireRoot(scene, "VerticalSliceRoot");
            FirstPlayableController controller = root
                .GetComponentInChildren<FirstPlayableController>(true);
            PowerHudPresenter[] presenters = root
                .GetComponentsInChildren<PowerHudPresenter>(true);
            if (presenters.Length != 1)
            {
                throw new InvalidOperationException(
                    "Milestone 6 requires exactly one PowerHudPresenter.");
            }

            PowerHudPresenter presenter = presenters[0];
            if (presenter.Controller != controller
                || presenter.FreezePulseRoot == null
                || presenter.FreezePulseButton == null
                || presenter.FreezePulseChargesText == null
                || presenter.InstantBarrierRoot == null
                || presenter.InstantBarrierButton == null
                || presenter.InstantBarrierChargesText == null)
            {
                throw new InvalidOperationException(
                    "Milestone 6 power HUD has a missing or mismatched " +
                    "serialized reference.");
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

        private static T GetOrAddComponent<T>(GameObject gameObject)
            where T : Component
        {
            T component = gameObject.GetComponent<T>();
            return component != null ? component : gameObject.AddComponent<T>();
        }
    }
}
