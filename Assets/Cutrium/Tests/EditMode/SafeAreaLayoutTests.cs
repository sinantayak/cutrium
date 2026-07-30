using Cutrium.Unity.Layout;
using NUnit.Framework;
using UnityEngine;

namespace Cutrium.Gameplay.EditModeTests
{
    public sealed class SafeAreaLayoutTests
    {
        [Test]
        public void FullScreenSafeArea_UsesFullAnchors()
        {
            SafeAreaAnchors anchors = SafeAreaLayout.CalculateAnchors(
                new Rect(0f, 0f, 1080f, 1920f),
                new Vector2(1080f, 1920f));

            Assert.That(anchors.Minimum, Is.EqualTo(Vector2.zero));
            Assert.That(anchors.Maximum, Is.EqualTo(Vector2.one));
        }

        [Test]
        public void Insets_AreConvertedToNormalizedAnchors()
        {
            SafeAreaAnchors anchors = SafeAreaLayout.CalculateAnchors(
                Rect.MinMaxRect(60f, 90f, 1020f, 1860f),
                new Vector2(1080f, 1920f));

            Assert.That(anchors.Minimum.x, Is.EqualTo(60f / 1080f).Within(0.00001f));
            Assert.That(anchors.Minimum.y, Is.EqualTo(90f / 1920f).Within(0.00001f));
            Assert.That(anchors.Maximum.x, Is.EqualTo(1020f / 1080f).Within(0.00001f));
            Assert.That(anchors.Maximum.y, Is.EqualTo(1860f / 1920f).Within(0.00001f));
        }

        [Test]
        public void SafeAreaOutsideScreen_IsClamped()
        {
            SafeAreaAnchors anchors = SafeAreaLayout.CalculateAnchors(
                Rect.MinMaxRect(-20f, -10f, 1100f, 1950f),
                new Vector2(1080f, 1920f));

            Assert.That(anchors.Minimum, Is.EqualTo(Vector2.zero));
            Assert.That(anchors.Maximum, Is.EqualTo(Vector2.one));
        }

        [TestCase(0f, 1920f)]
        [TestCase(1080f, 0f)]
        public void InvalidScreenSize_IsRejected(float width, float height)
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(
                () => SafeAreaLayout.CalculateAnchors(
                    new Rect(0f, 0f, width, height),
                    new Vector2(width, height)));
        }
    }
}
