using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Cutrium.Presentation.Barriers;
using Cutrium.Presentation.Capture;
using Cutrium.Presentation.HUD;
using Cutrium.Presentation.Landmark;
using Cutrium.Presentation.Theme;
using Cutrium.Presentation.Threats;
using Cutrium.Unity.Layout;
using Cutrium.Unity.Simulation;
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
    /// data-driven LandmarkRevealPresenter that obscures active area and
    /// reveals landmark artwork as it is captured. This is not Milestone 7
    /// and does not change gameplay.
    public static class LandmarkRevealPresentationSetup
    {
        public const string GeneratedFolder =
            "Assets/Cutrium/Art/Generated/Landmark";
        public const string LandmarkContentFolder =
            "Assets/Cutrium/Content/Landmarks";
        public const string GalataArtworkFolder =
            LandmarkContentFolder + "/Artwork";
        public const string CleanupThemePath =
            Milestone5SceneSetup.CleanupThemePath;

        private const float BarrierVisualLogicalThickness = 0.13f;
        private const float RevealFadeSeconds = 0.35f;

        // Near-opaque, near-black: unrevealed area should read as solidly
        // hidden -- almost nothing of the artwork should be visible.
        private static readonly Color VeilColor =
            new Color(0.015f, 0.02f, 0.03f, 0.996f);

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
            Configure(scene, sprites, cleanup, landmarks);
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
                "Landmark Reveal Presentation Pass verified. A compact " +
                "integrated power row, a full-screen opaque completion " +
                "reward screen with a fixed-aspect framed hero photo, and " +
                "a three-landmark reveal pipeline (led by Galata Kulesi) " +
                "are ready.");
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
                { "veil_texture", GeneratedPattern.Veil },
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
                int size = isLandmark ? 48
                    : pair.Value == GeneratedPattern.Veil ? 64
                    : 32;
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

                case GeneratedPattern.Veil:
                {
                    // Two-octave noise approximates a soft frosted/blurred
                    // obscuring surface (rather than a flat semi-transparent
                    // tile). Kept low-contrast and dark so, combined with
                    // VeilColor's near-opaque near-black tint, almost
                    // nothing of the hidden artwork reads through.
                    float macro = 0.5f + 0.5f
                        * Mathf.Sin(u * 5.1f + 1.7f)
                        * Mathf.Cos(v * 4.3f + 0.9f);
                    float micro = (x * 13 + y * 7) % 17 / 17f;
                    float shade = Mathf.Lerp(0.06f, 0.14f, macro * 0.7f + micro * 0.3f);
                    return new Color(shade, shade, shade, 1f);
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
                theme.BackgroundSprite,
                Color.white,
                sprites["board_calm"],
                Color.white,
                sprites["frame_soft"],
                Color.white,
                sprites["threat_gem"],
                Color.white,
                Vector2.one,
                Vector2.zero,
                theme.ThreatShadowSprite,
                new Color(0f, 0f, 0f, 0.26f),
                theme.ThreatTrailSprite,
                new Color(0.92f, 0.62f, 0.5f, 0.2f),
                sprites["barrier_body_soft"],
                theme.BarrierCapSprite,
                sprites["barrier_body_soft"],
                new Color(0.62f, 0.85f, 0.95f, 0.3f),
                new Color(0.65f, 0.88f, 0.95f, 0.92f),
                new Color(0.95f, 0.8f, 0.4f, 1f),
                new Color(0.9f, 0.42f, 0.44f, 0.85f),
                theme.CaptureSprite,
                null,
                new Color(0.85f, 0.72f, 0.42f, 0.16f),
                // Fully transparent: TopHUD/BottomHUD no longer paint their
                // own panel behind their content, so they show through to
                // the same outer canvas background as everything else
                // instead of reading as separate black header/footer bands.
                new Color(0.03f, 0.045f, 0.06f, 0f),
                new Color(0.85f, 0.78f, 0.62f, 1f),
                new Color(0.96f, 0.95f, 0.92f, 1f));
            EditorUtility.SetDirty(theme);
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
            LandmarkDefinition galata = GetOrCreateLandmark(
                LandmarkAssetPaths[0],
                LegacyAlpineLandmarkPath);
            Sprite galataArtwork =
                LoadGalataArtworkIfPresent() ?? sprites["landmark_alpine"];
            galata.ConfigureForSetup(
                "galata-kulesi",
                "Galata Kulesi",
                "A centuries-old stone watchtower rising above the Golden " +
                "Horn, marking where old Istanbul meets the strait.",
                "Türkiye",
                galataArtwork);
            EditorUtility.SetDirty(galata);

            LandmarkDefinition coastal = GetOrCreateLandmark(
                LandmarkAssetPaths[1]);
            coastal.ConfigureForSetup(
                "coastal-lagoon",
                "Coastal Lagoon",
                "Warm turquoise water meets soft white sand beneath an " +
                "endless open sky.",
                "Oceania",
                sprites["landmark_coastal"]);
            EditorUtility.SetDirty(coastal);

            LandmarkDefinition desert = GetOrCreateLandmark(
                LandmarkAssetPaths[2]);
            desert.ConfigureForSetup(
                "desert-dunes",
                "Desert Dunes",
                "Rolling amber dunes catch the last light of dusk across a " +
                "silent horizon.",
                "Middle East",
                sprites["landmark_desert"]);
            EditorUtility.SetDirty(desert);

            return new[] { galata, coastal, desert };
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
            var reloaded = new LandmarkDefinition[LandmarkAssetPaths.Length];
            for (int index = 0; index < LandmarkAssetPaths.Length; index++)
            {
                string path = LandmarkAssetPaths[index];
                LandmarkDefinition landmark =
                    AssetDatabase.LoadAssetAtPath<LandmarkDefinition>(path);
                reloaded[index] = landmark
                    ?? throw new InvalidOperationException(
                        $"Landmark asset '{path}' could not be reloaded " +
                        "after opening the scene.");
            }

            return reloaded;
        }

        // ------------------------------------------------------------
        // Scene composition
        // ------------------------------------------------------------

        private static void Configure(
            Scene scene,
            IReadOnlyDictionary<string, Sprite> sprites,
            ThemeDefinition cleanup,
            LandmarkDefinition[] landmarks)
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

            ConfigureLandmarkLayer(
                root,
                boardFrame,
                sprites,
                controller,
                RequireChild(safeArea, "LevelCompleteOverlay"),
                landmarks,
                out LandmarkRevealPresenter landmarkPresenter);

            Transform topHud = RequireChild(safeArea, "TopHUD");
            Transform bottomHud = RequireChild(safeArea, "BottomHUD");
            RestyleHud(hud, topHud, sprites);
            ConfigureBottomHud(root, safeArea, controller, sprites);
            HideDebugFooter(bottomHud);
            FinalizeThemeTextSync(themePresenter, topHud, bottomHud);

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

            EditorUtility.SetDirty(controller);
            EditorUtility.SetDirty(barrierPresenter);
            EditorUtility.SetDirty(landmarkPresenter);
            EditorUtility.SetDirty(hud);
            EditorUtility.SetDirty(powerHud);
            EditorUtility.SetDirty(boardCameraFitter);
            EditorUtility.SetDirty(boardViewport.gameObject);
        }

        private static void ConfigureLandmarkLayer(
            GameObject root,
            RectTransform boardFrame,
            IReadOnlyDictionary<string, Sprite> sprites,
            FirstPlayableController controller,
            Transform completionOverlay,
            LandmarkDefinition[] landmarks,
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
            overlayBackground.color = new Color(0.04f, 0.07f, 0.12f, 1f);
            overlayBackground.raycastTarget = true;

            // The hero photo lives in a fixed square frame instead of
            // stretching to fill the screen, so its aspect ratio is never
            // distorted. HeroFrameBounds is the available square slot
            // (positioned above the text/button content); HeroArtwork fits
            // itself into a centered square within it (AspectRatioFitter),
            // then the photo itself letterboxes inside that square if it
            // isn't natively square (Image.preserveAspect). A real frame
            // sprite (e.g. "hung on a wall") can be layered on this later
            // without changing the layout.
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
            heroFitter.aspectRatio = 1f;
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
            contentColumn.spacing = 8f;
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
                52,
                TextAnchor.LowerCenter,
                new Color(0.99f, 0.96f, 0.9f, 1f));
            titleText.fontStyle = FontStyle.Bold;
            LayoutElement titleLayout =
                GetOrAddComponent<LayoutElement>(titleRect.gameObject);
            titleLayout.minHeight = 66f;
            titleLayout.preferredHeight = 66f;
            titleLayout.flexibleHeight = 0f;

            RectTransform sectorRect = GetOrCreateUiChild(contentRect, "Sector");
            sectorRect.anchorMin = new Vector2(0.5f, 1f);
            sectorRect.anchorMax = new Vector2(0.5f, 1f);
            sectorRect.pivot = new Vector2(0.5f, 1f);
            Text sectorText = ConfigureText(
                sectorRect,
                "Sector",
                26,
                TextAnchor.UpperCenter,
                new Color(0.85f, 0.78f, 0.62f, 0.92f));
            LayoutElement sectorLayout =
                GetOrAddComponent<LayoutElement>(sectorRect.gameObject);
            sectorLayout.minHeight = 36f;
            sectorLayout.preferredHeight = 36f;
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
                26,
                TextAnchor.UpperCenter,
                new Color(0.9f, 0.89f, 0.86f, 0.9f));
            descriptionText.horizontalOverflow = HorizontalWrapMode.Wrap;
            descriptionText.verticalOverflow = VerticalWrapMode.Truncate;
            LayoutElement descriptionLayout =
                GetOrAddComponent<LayoutElement>(descriptionRect.gameObject);
            descriptionLayout.minHeight = 76f;
            descriptionLayout.preferredHeight = 92f;
            descriptionLayout.flexibleHeight = 0f;

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
            statsText.fontSize = 26;
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
            retryRect.anchorMin = new Vector2(0.16f, 0.02f);
            retryRect.anchorMax = new Vector2(0.44f, 0.095f);
            retryRect.pivot = new Vector2(0.5f, 0.5f);
            retryRect.offsetMin = Vector2.zero;
            retryRect.offsetMax = Vector2.zero;
            CanvasGroup retryGroup =
                GetOrAddComponent<CanvasGroup>(retryTransform.gameObject);

            Transform nextTransform = RequireChild(completionOverlay, "NextButton");
            var nextRect = (RectTransform)nextTransform;
            nextRect.anchorMin = new Vector2(0.56f, 0.02f);
            nextRect.anchorMax = new Vector2(0.84f, 0.095f);
            nextRect.pivot = new Vector2(0.5f, 0.5f);
            nextRect.offsetMin = Vector2.zero;
            nextRect.offsetMax = Vector2.zero;
            CanvasGroup nextGroup =
                GetOrAddComponent<CanvasGroup>(nextTransform.gameObject);

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
                sprites["veil_texture"],
                VeilColor,
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

            EditorUtility.SetDirty(artworkImage);
            EditorUtility.SetDirty(heroImage);
            EditorUtility.SetDirty(overlayBackground);
            EditorUtility.SetDirty(titleText);
            EditorUtility.SetDirty(sectorText);
            EditorUtility.SetDirty(descriptionText);
            EditorUtility.SetDirty(statsText);
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
            IReadOnlyDictionary<string, Sprite> sprites)
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

            // bottomColumn (BottomHUD's VerticalLayoutGroup) is shared with
            // the (now hidden) debug rows and produced inconsistent
            // Middle/Center alignment math for a lone LayoutGroup-controlled
            // child. Bypass it entirely with ignoreLayout=true and an
            // explicit centered anchor/sizeDelta -- the same reliable
            // pattern already used for the level label above -- so the
            // button's real screen position always matches exactly where
            // it is configured, with nothing left to LayoutGroup timing.
            RectTransform retryRect = GetOrCreateUiChild(bottomHud, "QuickRetryButton");
            retryRect.anchorMin = new Vector2(0.5f, 0.5f);
            retryRect.anchorMax = new Vector2(0.5f, 0.5f);
            retryRect.pivot = new Vector2(0.5f, 0.5f);
            retryRect.sizeDelta = new Vector2(128f, 40f);
            retryRect.anchoredPosition = Vector2.zero;
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

        // ------------------------------------------------------------
        // Validation
        // ------------------------------------------------------------

        private static void Validate(Scene scene, LandmarkDefinition[] landmarks)
        {
            GameObject root = RequireRoot(scene, "VerticalSliceRoot");
            BarrierPresenter barrierPresenter = root
                .GetComponentInChildren<BarrierPresenter>(true);
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
                || landmarkPresenter.CompletionArtworkImage == null
                || landmarkPresenter.ScrimCanvasGroup == null
                || landmarkPresenter.ContentCanvasGroup == null
                || landmarkPresenter.StatsCanvasGroup == null
                || landmarkPresenter.RetryCanvasGroup == null
                || landmarkPresenter.NextCanvasGroup == null
                || landmarkPresenter.CompletionTitleText == null
                || landmarkPresenter.CompletionDescriptionText == null
                || landmarkPresenter.CompletionSectorText == null
                || landmarkPresenter.Landmarks.Count != 3)
            {
                throw new InvalidOperationException(
                    "LandmarkRevealPresenter has a missing or mismatched " +
                    "serialized reference, or is not wired to exactly " +
                    "three landmarks.");
            }

            for (int index = 0; index < landmarks.Length; index++)
            {
                if (landmarkPresenter.Landmarks[index] != landmarks[index])
                {
                    throw new InvalidOperationException(
                        "LandmarkRevealPresenter landmark order does not " +
                        "match the configured catalog.");
                }
            }

            if (landmarkPresenter.Landmarks[0].LandmarkId != "galata-kulesi")
            {
                throw new InvalidOperationException(
                    "The first landmark slot must be Galata Kulesi.");
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
            Transform retryButtonTransform = RequireChild(
                bottomHud,
                "QuickRetryButton");
            if (retryButtonTransform.GetComponent<Button>() == null)
            {
                throw new InvalidOperationException(
                    "BottomHUD's quick-retry element must have a Button.");
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

        private static void ValidateBoardHierarchy(GameObject root)
        {
            Transform safeArea = RequireChild(
                root.transform,
                "Canvas/SafeAreaRoot");
            Transform boardStage = RequireChild(safeArea, "BoardStage");
            Transform boardViewport = RequireChild(boardStage, "BoardViewport");
            Transform boardFrame = RequireChild(boardViewport, "BoardFrame");

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
                || fitter.BoardFrame != boardFrameRect)
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
        }

        // ------------------------------------------------------------
        // Shared helpers
        // ------------------------------------------------------------

        private static Text ConfigureText(
            RectTransform rect,
            string value,
            int fontSize,
            TextAnchor alignment,
            Color color)
        {
            Text text = GetOrAddComponent<Text>(rect.gameObject);
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
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

        private enum GeneratedPattern
        {
            Frame,
            Board,
            BarrierBody,
            ThreatGem,
            PowerButton,
            Veil,
            LandmarkAlpine,
            LandmarkCoastal,
            LandmarkDesert,
            CompletionScrim,
            ChipRounded,
        }
    }
}
