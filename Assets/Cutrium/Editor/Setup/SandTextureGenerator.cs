using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Cutrium.Editor.Setup
{
    /// Editor-only generator for the procedural sand surface texture used
    /// to obscure uncaptured board area in the sand/bowl landmark reveal
    /// (see ADR-026). A pure function of pixel coordinates (no
    /// `UnityEngine.Random`, no runtime seeding), matching the
    /// deterministic procedural-sprite technique already used elsewhere in
    /// this project's setup utilities. Once the PNG exists it is treated as
    /// user-authored replacement art: setup may import it, but never rewrites
    /// its bytes. Procedural generation is only the missing-file fallback.
    public static class SandTextureGenerator
    {
        public const string GeneratedFolder =
            "Assets/Cutrium/Art/Generated/Sand";

        private const int TextureSize = 256;

        [MenuItem("Cutrium/Setup/Generate Sand Texture")]
        public static void GenerateAll()
        {
            EnsureSandTexture();
            AssetDatabase.SaveAssets();
            Debug.Log(
                "Sand surface texture ready; existing artwork was preserved.");
        }

        public static Sprite EnsureSandTexture()
        {
            EnsureFolder(GeneratedFolder);
            string path = $"{GeneratedFolder}/sand_surface.png";
            EnsureGeneratedPng(path, SandPixel);
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            return sprite != null
                ? sprite
                : throw new InvalidOperationException(
                    $"Generated sand texture '{path}' did not import.");
        }

        public static Color SandPixel(int x, int y, int size)
        {
            float u = (x + 0.5f) / size;
            float v = (y + 0.5f) / size;

            // Two low-frequency bands (unrelated frequencies/phases so no
            // single period reads as a repeating grid) approximate gentle
            // wind-blown dune ripples across the surface.
            float bandA = Mathf.Sin((u * 9f) + (v * 2.3f) + 0.4f);
            float bandB = Mathf.Sin((u * 3.1f) - (v * 5.7f) + 1.9f);
            float ripple = 0.5f + (0.5f * ((bandA * 0.6f) + (bandB * 0.4f)));

            // Fine per-cluster grain variation -- clustered (not
            // independent per-pixel) so it reads as sand texture rather
            // than harsh static.
            int clusterX = x / 2;
            int clusterY = y / 2;
            float grain = ((clusterX * 13 + clusterY * 7) % 11) / 11f;

            float shade = Mathf.Lerp(0.72f, 0.9f, (ripple * 0.75f) + (grain * 0.25f));

            // A warm tan/beige sand color -- red highest, green a touch
            // lower, blue noticeably lower, kept fully opaque (sand fully
            // covers, unlike the previous translucent fog).
            return new Color(
                shade,
                shade * 0.86f,
                shade * 0.62f,
                1f);
        }

        private static void EnsureGeneratedPng(
            string path,
            Func<int, int, int, Color> pixelFunction)
        {
            string projectRoot = Directory.GetParent(Application.dataPath)
                ?.FullName
                ?? throw new InvalidOperationException(
                    "Unity project root could not be resolved.");
            string absolute = Path.Combine(
                projectRoot,
                path.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(absolute))
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
                        pixels[(y * TextureSize) + x] =
                            pixelFunction(x, y, TextureSize);
                    }
                }

                texture.SetPixels(pixels);
                texture.Apply(false, false);
                byte[] png = texture.EncodeToPNG();
                UnityEngine.Object.DestroyImmediate(texture);
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
                importer.alphaIsTransparency = false;
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
