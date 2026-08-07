using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Cutrium.Presentation.Barriers;
using Cutrium.Presentation.Capture;
using Cutrium.Presentation.Feedback;
using Cutrium.Presentation.Theme;
using Cutrium.Presentation.Threats;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace Cutrium.Editor.Setup
{
    public static class Milestone5SceneSetup
    {
        public const string CleanupThemePath =
            "Assets/Cutrium/Content/Themes/CleanupPrototype.asset";
        public const string FallbackThemePath =
            "Assets/Cutrium/Content/Themes/MinimalFallback.asset";
        public const string GeneratedFolder =
            "Assets/Cutrium/Art/Generated/Cleanup";

        [MenuItem("Cutrium/Setup/Milestone 5 Theme Pipeline")]
        public static void Apply()
        {
            VerifyBaseline();
            Milestone4SceneSetup.Apply();
            EnsureFolders();
            Dictionary<string, Sprite> sprites = GenerateSprites();
            ThemeDefinition cleanup = GetOrCreateTheme(CleanupThemePath);
            ThemeDefinition fallback = GetOrCreateTheme(FallbackThemePath);
            ConfigureCleanupTheme(cleanup, sprites);
            ConfigureFallbackTheme(fallback);
            AssetDatabase.SaveAssets();

            Scene scene = EditorSceneManager.OpenScene(
                Milestone2SceneSetup.VerticalSliceScenePath,
                OpenSceneMode.Single);
            // Opening a scene unloads unused native assets. Reacquire the
            // imported assets so setup and validation never depend on stale
            // UnityEngine.Object wrappers from before the scene load.
            sprites = ReloadGeneratedSprites(sprites.Keys);
            cleanup = LoadTheme(CleanupThemePath);
            fallback = LoadTheme(FallbackThemePath);
            ConfigureScene(scene, cleanup, fallback);
            Validate(scene, cleanup, fallback, sprites);
            if (!EditorSceneManager.SaveScene(
                    scene,
                    Milestone2SceneSetup.VerticalSliceScenePath))
            {
                throw new InvalidOperationException(
                    "Unity could not save the Milestone 5 scene.");
            }

            AssetDatabase.SaveAssets();
            Debug.Log(
                "Milestone 5 scene setup verified. Cleanup prototype and " +
                "minimal fallback themes are replaceable and presentation-only.");
        }

        private static void VerifyBaseline()
        {
            if (!string.Equals(
                    Application.unityVersion,
                    "6000.3.21f1",
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "Milestone 5 requires Unity 6000.3.21f1.");
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
            EnsureFolder("Assets/Cutrium/Content/Themes");
        }

        private static Dictionary<string, Sprite> GenerateSprites()
        {
            var patterns = new Dictionary<string, GeneratedPattern>
            {
                { "chamber_background", GeneratedPattern.Background },
                { "board_grid", GeneratedPattern.Board },
                { "frame", GeneratedPattern.Frame },
                { "normal_threat", GeneratedPattern.Threat },
                { "threat_shadow", GeneratedPattern.Shadow },
                { "threat_trail", GeneratedPattern.Trail },
                { "barrier_body", GeneratedPattern.Barrier },
                { "barrier_cap", GeneratedPattern.Cap },
                { "capture_fill", GeneratedPattern.Capture },
            };
            var result = new Dictionary<string, Sprite>();
            foreach (KeyValuePair<string, GeneratedPattern> pair in patterns)
            {
                string path = $"{GeneratedFolder}/{pair.Key}.png";
                EnsureGeneratedPng(path, pair.Value);
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
            GeneratedPattern pattern)
        {
            const int size = 32;
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
            bool importerChanged = importer.textureType != TextureImporterType.Sprite
                || importer.spriteImportMode != SpriteImportMode.Single
                || importer.mipmapEnabled
                || importer.wrapMode != TextureWrapMode.Clamp
                || importer.filterMode != FilterMode.Bilinear
                || !Mathf.Approximately(importer.spritePixelsPerUnit, 32f);
            if (importerChanged)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.filterMode = FilterMode.Bilinear;
                importer.spritePixelsPerUnit = 32f;
                importer.SaveAndReimport();
            }
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
                case GeneratedPattern.Background:
                    return Color.Lerp(
                        new Color(0.015f, 0.04f, 0.06f, 1f),
                        new Color(0.045f, 0.14f, 0.15f, 1f),
                        v * 0.8f + u * 0.2f);
                case GeneratedPattern.Board:
                    bool grid = x % 8 == 0 || y % 8 == 0;
                    return grid
                        ? new Color(0.18f, 0.62f, 0.58f, 0.22f)
                        : new Color(0.035f, 0.13f, 0.15f, 1f);
                case GeneratedPattern.Frame:
                    bool edge = x < 3 || y < 3 || x >= size - 3 || y >= size - 3;
                    return edge
                        ? new Color(0.28f, 1f, 0.82f, 1f)
                        : new Color(0.02f, 0.07f, 0.08f, 0.15f);
                case GeneratedPattern.Threat:
                    if (radius > 0.48f)
                    {
                        return Color.clear;
                    }

                    float threatGlow = Mathf.Clamp01(1f - radius * 1.9f);
                    return Color.Lerp(
                        new Color(0.62f, 0.03f, 0.12f, 1f),
                        new Color(1f, 0.5f, 0.44f, 1f),
                        threatGlow);
                case GeneratedPattern.Shadow:
                    return new Color(0f, 0f, 0f,
                        Mathf.Clamp01(1f - radius * 2f) * 0.65f);
                case GeneratedPattern.Trail:
                    float vertical = Mathf.Clamp01(1f - Mathf.Abs(dy) * 4f);
                    return new Color(1f, 0.2f, 0.28f,
                        vertical * Mathf.Clamp01(1f - u) * 0.65f);
                case GeneratedPattern.Barrier:
                    float center = Mathf.Clamp01(1f - Mathf.Abs(dy) * 6f);
                    return new Color(0.36f, 1f, 0.92f, 0.45f + center * 0.55f);
                case GeneratedPattern.Cap:
                    return radius <= 0.46f
                        ? new Color(0.72f, 1f, 0.86f, 1f)
                        : Color.clear;
                case GeneratedPattern.Capture:
                    bool fleck = (x * 7 + y * 11) % 29 == 0;
                    return fleck
                        ? new Color(0.65f, 1f, 0.88f, 0.8f)
                        : new Color(0.16f, 0.7f, 0.62f, 0.72f);
                default:
                    return Color.magenta;
            }
        }

        private static ThemeDefinition GetOrCreateTheme(string path)
        {
            ThemeDefinition theme = AssetDatabase.LoadAssetAtPath<ThemeDefinition>(path);
            if (theme == null)
            {
                theme = ScriptableObject.CreateInstance<ThemeDefinition>();
                AssetDatabase.CreateAsset(theme, path);
            }

            return theme;
        }

        private static ThemeDefinition LoadTheme(string path)
        {
            ThemeDefinition theme =
                AssetDatabase.LoadAssetAtPath<ThemeDefinition>(path);
            return theme ?? throw new InvalidOperationException(
                $"Theme asset '{path}' could not be reloaded after opening the scene.");
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

        private static void ConfigureCleanupTheme(
            ThemeDefinition theme,
            IReadOnlyDictionary<string, Sprite> sprites)
        {
            theme.ConfigureForSetup(
                "cleanup-chamber-prototype",
                sprites["chamber_background"],
                Color.white,
                sprites["board_grid"],
                Color.white,
                sprites["frame"],
                Color.white,
                sprites["normal_threat"],
                Color.white,
                new Vector2(1f, 1f),
                Vector2.zero,
                sprites["threat_shadow"],
                new Color(0f, 0f, 0f, 0.32f),
                sprites["threat_trail"],
                new Color(1f, 0.42f, 0.44f, 0.38f),
                sprites["barrier_body"],
                sprites["barrier_cap"],
                sprites["barrier_body"],
                new Color(0.3f, 0.92f, 1f, 0.48f),
                new Color(0.35f, 1f, 0.92f, 1f),
                new Color(0.72f, 1f, 0.72f, 1f),
                new Color(1f, 0.3f, 0.38f, 0.92f),
                sprites["capture_fill"],
                null,
                Color.white,
                new Color(0.025f, 0.075f, 0.09f, 0.97f),
                new Color(0.28f, 1f, 0.82f, 1f),
                new Color(0.9f, 1f, 0.98f, 1f));
            EditorUtility.SetDirty(theme);
        }

        private static void ConfigureFallbackTheme(ThemeDefinition theme)
        {
            theme.ConfigureForSetup(
                "minimal-flat-fallback",
                null,
                new Color(0.025f, 0.04f, 0.05f, 1f),
                null,
                new Color(0.06f, 0.13f, 0.14f, 1f),
                null,
                new Color(0.25f, 0.82f, 0.76f, 1f),
                null,
                new Color(1f, 0.34f, 0.4f, 1f),
                Vector2.one,
                Vector2.zero,
                null,
                new Color(0f, 0f, 0f, 0.25f),
                null,
                new Color(1f, 0.34f, 0.4f, 0.2f),
                null,
                null,
                null,
                new Color(0.3f, 0.78f, 0.92f, 0.4f),
                new Color(0.35f, 0.9f, 0.92f, 1f),
                new Color(0.55f, 1f, 0.65f, 1f),
                new Color(1f, 0.3f, 0.36f, 0.9f),
                null,
                null,
                new Color(0.18f, 0.65f, 0.6f, 0.72f),
                new Color(0.03f, 0.07f, 0.08f, 0.98f),
                new Color(0.25f, 0.82f, 0.76f, 1f),
                Color.white);
            EditorUtility.SetDirty(theme);
        }

        private static void ConfigureScene(
            Scene scene,
            ThemeDefinition cleanup,
            ThemeDefinition fallback)
        {
            GameObject root = RequireRoot(scene, "VerticalSliceRoot");
            Transform canvas = RequireChild(root.transform, "Canvas");
            Transform safeArea = RequireChild(canvas, "SafeAreaRoot");
            Transform boardViewport = RequireChild(safeArea, "BoardViewport");
            Transform boardFrame = RequireChild(boardViewport, "BoardFrame");
            Transform topHud = RequireChild(safeArea, "TopHUD");
            Transform bottomHud = RequireChild(safeArea, "BottomHUD");

            RectTransform backgroundRect = GetOrCreateUiChild(
                canvas,
                "ThemeBackground");
            StretchToParent(backgroundRect);
            backgroundRect.SetSiblingIndex(0);
            Image background = GetOrAddComponent<Image>(backgroundRect.gameObject);
            background.raycastTarget = false;

            RectTransform surfaceRect = GetOrCreateUiChild(
                boardFrame,
                "BoardSurface");
            StretchToParent(surfaceRect);
            surfaceRect.offsetMin = new Vector2(2f, 2f);
            surfaceRect.offsetMax = new Vector2(-2f, -2f);
            surfaceRect.SetSiblingIndex(0);
            Image surface = GetOrAddComponent<Image>(surfaceRect.gameObject);
            surface.raycastTarget = false;

            Image frame = GetOrAddComponent<Image>(boardFrame.gameObject);
            Image topBackground = GetOrAddComponent<Image>(topHud.gameObject);
            Image bottomBackground = GetOrAddComponent<Image>(bottomHud.gameObject);
            Image blockerAccent = RequireChild(topHud, "HudBlockerButton")
                .GetComponent<Image>();

            ThreatPresenter threat = root
                .GetComponentInChildren<ThreatPresenter>(true);
            BarrierPresenter barrier = root
                .GetComponentInChildren<BarrierPresenter>(true);
            CaptureBoardPresenter capture = root
                .GetComponentInChildren<CaptureBoardPresenter>(true);
            FeedbackPresenter feedback = root
                .GetComponentInChildren<FeedbackPresenter>(true);
            GameObject services = GetOrCreateChild(
                root.transform,
                "ThemeServices");
            ThemePresenter presenter = GetOrAddComponent<ThemePresenter>(services);
            presenter.Configure(
                cleanup,
                fallback,
                background,
                surface,
                frame,
                new[] { topBackground, bottomBackground },
                new Graphic[] { blockerAccent },
                safeArea.GetComponentsInChildren<Text>(true),
                threat,
                barrier,
                capture,
                feedback);

            Transform completion = RequireChild(safeArea, "LevelCompleteOverlay");
            completion.SetAsLastSibling();
            EditorUtility.SetDirty(background);
            EditorUtility.SetDirty(surface);
            EditorUtility.SetDirty(frame);
            EditorUtility.SetDirty(topBackground);
            EditorUtility.SetDirty(bottomBackground);
            EditorUtility.SetDirty(presenter);
            EditorUtility.SetDirty(threat);
            EditorUtility.SetDirty(barrier);
            EditorUtility.SetDirty(capture);
        }

        private static void Validate(
            Scene scene,
            ThemeDefinition cleanup,
            ThemeDefinition fallback,
            IReadOnlyDictionary<string, Sprite> sprites)
        {
            GameObject root = RequireRoot(scene, "VerticalSliceRoot");
            ThemePresenter[] presenters = root
                .GetComponentsInChildren<ThemePresenter>(true);
            if (presenters.Length != 1)
            {
                throw new InvalidOperationException(
                    "Milestone 5 requires exactly one ThemePresenter.");
            }

            ThemePresenter presenter = presenters[0];
            var failures = new List<string>();
            AddFailure(
                failures,
                presenter.SelectedTheme == cleanup,
                "selected theme reference");
            AddFailure(
                failures,
                presenter.FallbackTheme == fallback,
                "fallback theme reference");
            AddFailure(failures, presenter.Background != null, "background reference");
            AddFailure(failures, presenter.BoardSurface != null, "board reference");
            AddFailure(failures, presenter.BoardFrame != null, "frame reference");
            AddFailure(
                failures,
                presenter.ThreatPresenter != null,
                "threat presenter reference");
            AddFailure(
                failures,
                presenter.BarrierPresenter != null,
                "barrier presenter reference");
            AddFailure(
                failures,
                presenter.CapturePresenter != null,
                "capture presenter reference");
            AddFailure(
                failures,
                presenter.FeedbackPresenter != null,
                "feedback presenter reference");
            AddFailure(
                failures,
                presenter.Current.StableId == cleanup.StableId,
                "selected theme resolution");
            AddFailure(
                failures,
                SameAssetReference(
                    cleanup.ThreatSprite,
                    sprites["normal_threat"]),
                "cleanup threat sprite GUID/file ID");
            AddFailure(
                failures,
                SameAssetReference(
                    cleanup.BarrierBodySprite,
                    sprites["barrier_body"]),
                "cleanup barrier sprite GUID/file ID");
            AddFailure(
                failures,
                SameAssetReference(
                    cleanup.CaptureSprite,
                    sprites["capture_fill"]),
                "cleanup capture sprite GUID/file ID");
            AddFailure(
                failures,
                fallback.ThreatSprite == null,
                "fallback threat sprite must be empty");
            AddFailure(
                failures,
                fallback.CaptureMaterial == null,
                "fallback capture material must be empty");
            AddFailure(
                failures,
                presenter.ThreatPresenter != null
                    && presenter.ThreatPresenter.HasThemeStyle,
                "threat theme application");
            AddFailure(
                failures,
                presenter.BarrierPresenter != null
                    && presenter.BarrierPresenter.HasThemeStyle,
                "barrier theme application");
            AddFailure(
                failures,
                presenter.CapturePresenter != null
                    && presenter.CapturePresenter.HasThemeStyle,
                "capture theme application");
            if (failures.Count > 0)
            {
                throw new InvalidOperationException(
                    "Milestone 5 theme references or fallback rules are invalid: " +
                    string.Join(", ", failures) + ".");
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

        private static void AddFailure(
            ICollection<string> failures,
            bool valid,
            string diagnostic)
        {
            if (!valid)
            {
                failures.Add(diagnostic);
            }
        }

        private static bool SameAssetReference(
            UnityEngine.Object left,
            UnityEngine.Object right)
        {
            if (left == null || right == null)
            {
                return left == null && right == null;
            }

            bool hasLeftIdentity =
                AssetDatabase.TryGetGUIDAndLocalFileIdentifier(
                    left,
                    out string leftGuid,
                    out long leftFileId);
            bool hasRightIdentity =
                AssetDatabase.TryGetGUIDAndLocalFileIdentifier(
                    right,
                    out string rightGuid,
                    out long rightFileId);
            return hasLeftIdentity
                && hasRightIdentity
                && string.Equals(leftGuid, rightGuid, StringComparison.Ordinal)
                && leftFileId == rightFileId;
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

        private enum GeneratedPattern
        {
            Background,
            Board,
            Frame,
            Threat,
            Shadow,
            Trail,
            Barrier,
            Cap,
            Capture,
        }
    }
}
