using System.IO;
using Cutrium.Editor.Setup;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Cutrium.Gameplay.EditModeTests
{
    public sealed class SandTextureGeneratorTests
    {
        [Test]
        public void SandPixel_IsPureAndDeterministic()
        {
            Color first = SandTextureGenerator.SandPixel(37, 84, 256);
            Color second = SandTextureGenerator.SandPixel(37, 84, 256);

            Assert.That(second.r, Is.EqualTo(first.r));
            Assert.That(second.g, Is.EqualTo(first.g));
            Assert.That(second.b, Is.EqualTo(first.b));
            Assert.That(second.a, Is.EqualTo(first.a));
        }

        [Test]
        public void SandPixel_IsFullyOpaqueEverywhere()
        {
            // Sand fully covers uncaptured area -- unlike the earlier
            // translucent fog, it must never let anything show through.
            for (int y = 0; y < 256; y += 17)
            {
                for (int x = 0; x < 256; x += 17)
                {
                    Color pixel = SandTextureGenerator.SandPixel(x, y, 256);
                    Assert.That(pixel.a, Is.EqualTo(1f), $"x={x}, y={y}");
                }
            }
        }

        [Test]
        public void SandPixel_ReadsAsWarmTanNotGrayOrCold()
        {
            for (int y = 0; y < 256; y += 23)
            {
                for (int x = 0; x < 256; x += 23)
                {
                    Color pixel = SandTextureGenerator.SandPixel(x, y, 256);
                    // Warm sand: red highest, blue clearly lowest.
                    Assert.That(pixel.r, Is.GreaterThan(pixel.g), $"x={x}, y={y}");
                    Assert.That(pixel.g, Is.GreaterThan(pixel.b), $"x={x}, y={y}");
                }
            }
        }

        [Test]
        public void SandPixel_HasNoHarshPixelToPixelJumps()
        {
            float maxDelta = 0f;
            Color previous = SandTextureGenerator.SandPixel(0, 128, 256);
            for (int x = 1; x < 256; x++)
            {
                Color current = SandTextureGenerator.SandPixel(x, 128, 256);
                float delta = Mathf.Abs(current.r - previous.r);
                maxDelta = Mathf.Max(maxDelta, delta);
                previous = current;
            }

            Assert.That(maxDelta, Is.LessThan(0.1f));
        }

        [Test]
        public void EnsureSandTexture_PreservesExistingUserAuthoredBytes()
        {
            string expectedPath =
                $"{SandTextureGenerator.GeneratedFolder}/sand_surface.png";
            byte[] bytesBeforeSetup = ReadAssetBytes(expectedPath);

            Sprite first = SandTextureGenerator.EnsureSandTexture();
            byte[] firstBytes = ReadAssetBytes(AssetDatabase.GetAssetPath(first));

            Sprite second = SandTextureGenerator.EnsureSandTexture();
            byte[] secondBytes = ReadAssetBytes(AssetDatabase.GetAssetPath(second));

            Assert.That(
                AssetDatabase.GetAssetPath(first),
                Is.EqualTo(AssetDatabase.GetAssetPath(second)));
            Assert.That(firstBytes, Is.EqualTo(bytesBeforeSetup));
            Assert.That(secondBytes, Is.EqualTo(firstBytes));
        }

        [Test]
        public void EnsureSandTexture_LivesUnderItsOwnGeneratedFolder()
        {
            Sprite sand = SandTextureGenerator.EnsureSandTexture();

            Assert.That(
                AssetDatabase.GetAssetPath(sand),
                Does.StartWith(SandTextureGenerator.GeneratedFolder));
        }

        private static byte[] ReadAssetBytes(string assetPath)
        {
            string projectRoot = Directory.GetParent(Application.dataPath)
                ?.FullName
                ?? throw new System.InvalidOperationException(
                    "Unity project root could not be resolved.");
            string absolute = Path.Combine(
                projectRoot,
                assetPath.Replace('/', Path.DirectorySeparatorChar));
            return File.ReadAllBytes(absolute);
        }
    }
}
