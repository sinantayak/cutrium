using System.IO;
using Cutrium.Editor.Setup;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Cutrium.Gameplay.EditModeTests
{
    public sealed class BowlSpriteGeneratorTests
    {
        [Test]
        public void BowlInteriorMaskPixel_IsPureAndDeterministic()
        {
            Color first = BowlSpriteGenerator.BowlInteriorMaskPixel(64, 40, 128);
            Color second = BowlSpriteGenerator.BowlInteriorMaskPixel(64, 40, 128);

            Assert.That(second, Is.EqualTo(first));
        }

        [Test]
        public void BowlInteriorMaskPixel_IsOpaqueAtCenterBottomOfInterior()
        {
            // A point guaranteed inside any reasonable bowl shape: on the
            // vertical center line, well above the rounded bottom corner
            // but still low in the bowl. Texture row y=0 is the bottom in
            // Unity's SetPixels convention, matching v=0 = bottom, so
            // v=0.3 is simply y = 0.3 * size.
            int x = 64;
            int y = Mathf.RoundToInt(0.3f * 128f);
            Color pixel = BowlSpriteGenerator.BowlInteriorMaskPixel(x, y, 128);
            Assert.That(pixel.a, Is.EqualTo(1f));
        }

        [Test]
        public void BowlInteriorMaskPixel_IsTransparentOutsideTheSilhouette()
        {
            // Far left/right edges of the texture, at mid-height, sit well
            // outside the bowl's tapered silhouette.
            Color left = BowlSpriteGenerator.BowlInteriorMaskPixel(2, 64, 128);
            Color right = BowlSpriteGenerator.BowlInteriorMaskPixel(125, 64, 128);
            Assert.That(left.a, Is.Zero);
            Assert.That(right.a, Is.Zero);
        }

        [Test]
        public void BowlInteriorMaskPixel_IsTransparentAtTheExactBottomCorners()
        {
            // Texture row y=0 is the bottom in Unity's SetPixels
            // convention (matching v=0 = bottom in BowlHalfWidthAt's own
            // doc comment), where the bowl's rounded bottom tapers to zero
            // width -- the mask must not bleed outside that rounded
            // bottom at a point away from dead-center.
            Color farLeftAtBottom =
                BowlSpriteGenerator.BowlInteriorMaskPixel(10, 0, 128);
            Assert.That(farLeftAtBottom.a, Is.Zero);
        }

        [Test]
        public void BowlOutlinePixel_IsVisibleAlongTheSilhouetteBoundary()
        {
            // At mid-height, the boundary sits at center +/- halfWidth;
            // sample just at that edge.
            float halfWidth = BowlSpriteGenerator.BowlHalfWidthAt(0.5f);
            int size = 128;
            int edgeX = Mathf.RoundToInt((0.5f + halfWidth - 0.01f) * size);
            Color pixel = BowlSpriteGenerator.BowlOutlinePixel(edgeX, size / 2, size);
            Assert.That(pixel.a, Is.GreaterThan(0f));
        }

        [Test]
        public void BowlOutlinePixel_IsTransparentFarFromTheBoundary()
        {
            Color center = BowlSpriteGenerator.BowlOutlinePixel(64, 64, 128);
            Assert.That(center.a, Is.Zero);
        }

        [Test]
        public void BowlHalfWidthAt_WidensFromBottomToTopRim()
        {
            float bottom = BowlSpriteGenerator.BowlHalfWidthAt(0.05f);
            float middle = BowlSpriteGenerator.BowlHalfWidthAt(0.5f);
            float top = BowlSpriteGenerator.BowlHalfWidthAt(1f);

            Assert.That(middle, Is.GreaterThan(bottom));
            Assert.That(top, Is.GreaterThan(middle));
        }

        [Test]
        public void EnsureBowlOutline_IsDeterministicAcrossRuns()
        {
            Sprite first = BowlSpriteGenerator.EnsureBowlOutline();
            byte[] firstBytes = ReadAssetBytes(AssetDatabase.GetAssetPath(first));

            Sprite second = BowlSpriteGenerator.EnsureBowlOutline();
            byte[] secondBytes = ReadAssetBytes(AssetDatabase.GetAssetPath(second));

            Assert.That(
                AssetDatabase.GetAssetPath(first),
                Is.EqualTo(AssetDatabase.GetAssetPath(second)));
            Assert.That(secondBytes, Is.EqualTo(firstBytes));
        }

        [Test]
        public void EnsureBowlInteriorMask_IsDeterministicAcrossRuns()
        {
            Sprite first = BowlSpriteGenerator.EnsureBowlInteriorMask();
            byte[] firstBytes = ReadAssetBytes(AssetDatabase.GetAssetPath(first));

            Sprite second = BowlSpriteGenerator.EnsureBowlInteriorMask();
            byte[] secondBytes = ReadAssetBytes(AssetDatabase.GetAssetPath(second));

            Assert.That(
                AssetDatabase.GetAssetPath(first),
                Is.EqualTo(AssetDatabase.GetAssetPath(second)));
            Assert.That(secondBytes, Is.EqualTo(firstBytes));
        }

        [Test]
        public void GeneratedBowlSpritesLiveUnderTheirOwnGeneratedFolder()
        {
            Sprite outline = BowlSpriteGenerator.EnsureBowlOutline();
            Sprite mask = BowlSpriteGenerator.EnsureBowlInteriorMask();

            Assert.That(
                AssetDatabase.GetAssetPath(outline),
                Does.StartWith(BowlSpriteGenerator.GeneratedFolder));
            Assert.That(
                AssetDatabase.GetAssetPath(mask),
                Does.StartWith(BowlSpriteGenerator.GeneratedFolder));
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
