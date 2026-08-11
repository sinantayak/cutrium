using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Cutrium.Editor.Setup
{
    /// Editor-only generator for the procedural bowl sprites used by the
    /// sand/bowl landmark reveal's BottomHUD fill indicator (see ADR-026):
    /// a decorative outline sprite and an interior-alpha mask sprite that
    /// drives a `UnityEngine.UI.Mask` so a rising sand-fill `Image` only
    /// ever shows inside the bowl's silhouette. Both are pure functions of
    /// pixel coordinates (no `UnityEngine.Random`), matching the
    /// deterministic procedural-sprite technique used elsewhere in this
    /// project's setup utilities.
    public static class BowlSpriteGenerator
    {
        public const string GeneratedFolder =
            "Assets/Cutrium/Art/Generated/Bowl";

        private const int TextureSize = 128;

        // The bowl cross-section: half-width (as a fraction of the texture
        // half-width) at the top rim vs. at the bottom, with a rounded
        // bottom corner. Shared by both sprites so the outline and the
        // interior mask describe the same silhouette.
        private const float TopHalfWidth = 0.46f;
        private const float BottomHalfWidth = 0.22f;
        private const float BottomRoundness = 0.16f;
        private const float RimThickness = 0.05f;

        [MenuItem("Cutrium/Setup/Generate Bowl Sprites")]
        public static void GenerateAll()
        {
            EnsureBowlOutline();
            EnsureBowlInteriorMask();
            AssetDatabase.SaveAssets();
            Debug.Log("Bowl outline/interior-mask sprites generated.");
        }

        public static Sprite EnsureBowlOutline()
        {
            EnsureFolder(GeneratedFolder);
            string path = $"{GeneratedFolder}/bowl_outline.png";
            EnsureGeneratedPng(path, BowlOutlinePixel);
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            return sprite != null
                ? sprite
                : throw new InvalidOperationException(
                    $"Generated bowl outline '{path}' did not import.");
        }

        public static Sprite EnsureBowlInteriorMask()
        {
            EnsureFolder(GeneratedFolder);
            string path = $"{GeneratedFolder}/bowl_interior_mask.png";
            EnsureGeneratedPng(path, BowlInteriorMaskPixel);
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            return sprite != null
                ? sprite
                : throw new InvalidOperationException(
                    $"Generated bowl interior mask '{path}' did not import.");
        }

        /// Half-width (0..0.5, as a fraction of texture width) of the bowl
        /// interior at normalized height v (0 = bottom, 1 = top rim).
        public static float BowlHalfWidthAt(float v)
        {
            float clamped = Mathf.Clamp01(v);
            float linear = Mathf.Lerp(BottomHalfWidth, TopHalfWidth, clamped);
            if (clamped >= BottomRoundness)
            {
                return linear;
            }

            // Round the bottom corner: blend toward 0 as v approaches the
            // true bottom, using a quarter-circle-like ease instead of a
            // hard corner.
            float t = clamped / BottomRoundness;
            float eased = Mathf.Sin(t * Mathf.PI * 0.5f);
            return Mathf.Lerp(0f, linear, eased);
        }

        public static Color BowlInteriorMaskPixel(int x, int y, int size)
        {
            float u = (x + 0.5f) / size;
            float v = (y + 0.5f) / size;
            float halfWidth = BowlHalfWidthAt(v);
            float distanceFromCenter = Mathf.Abs(u - 0.5f);
            bool inside = distanceFromCenter <= halfWidth;
            return inside ? Color.white : Color.clear;
        }

        public static Color BowlOutlinePixel(int x, int y, int size)
        {
            float u = (x + 0.5f) / size;
            float v = (y + 0.5f) / size;
            float halfWidth = BowlHalfWidthAt(v);
            float distanceFromCenter = Mathf.Abs(u - 0.5f);

            // A thin rim band just outside/at the interior boundary --
            // reads as the bowl's visible edge regardless of current fill
            // level, which is drawn separately (inside the mask).
            float ringInner = halfWidth - RimThickness;
            bool onRing = distanceFromCenter <= halfWidth
                && distanceFromCenter >= Mathf.Max(0f, ringInner);
            if (!onRing)
            {
                return Color.clear;
            }

            var rimColor = new Color(0.62f, 0.48f, 0.32f, 1f);
            return rimColor;
        }

        private static void EnsureGeneratedPng(
            string path,
            Func<int, int, int, Color> pixelFunction)
        {
            var texture = new Texture2D(
                TextureSize,
                TextureSize,
                TextureFormat.RGBA32,
                false,
                true);
            var pixels = new Color[TextureSize * TextureSize];
            for (int y = 0; y < TextureSize; y++)
            {
                for (int x = 0; x < TextureSize; x++)
                {
                    pixels[(y * TextureSize) + x] = pixelFunction(x, y, TextureSize);
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
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.filterMode = FilterMode.Bilinear;
                importer.SaveAndReimport();
            }
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            string leaf = Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
            {
                EnsureFolder(parent);
            }

            AssetDatabase.CreateFolder(parent, leaf);
        }
    }
}
