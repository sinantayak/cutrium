using Cutrium.Unity.Layout;
using NUnit.Framework;
using UnityEngine;

namespace Cutrium.Gameplay.EditModeTests
{
    public sealed class BoardViewportLayoutTests
    {
        [TestCase(1080f, 1920f)]
        [TestCase(1080f, 2400f)]
        [TestCase(1536f, 2048f)]
        public void AspectFit_PreservesFixedBoardAndNeverCrops(
            float screenWidth,
            float screenHeight)
        {
            var viewport = new Rect(0f, 0f, screenWidth, screenHeight);

            Rect board = BoardViewportLayout.CalculateAspectFitRect(viewport);

            Assert.That(
                board.width / board.height,
                Is.EqualTo(10f / 16f).Within(0.00001f));
            Assert.That(board.xMin, Is.GreaterThanOrEqualTo(viewport.xMin));
            Assert.That(board.yMin, Is.GreaterThanOrEqualTo(viewport.yMin));
            Assert.That(board.xMax, Is.LessThanOrEqualTo(viewport.xMax));
            Assert.That(board.yMax, Is.LessThanOrEqualTo(viewport.yMax));
            Assert.That(BoardViewportLayout.LogicalSize, Is.EqualTo(new Vector2(10f, 16f)));
        }

        [Test]
        public void TabletAspect_ProducesHorizontalPresentationMargins()
        {
            var viewport = new Rect(0f, 0f, 1536f, 2048f);

            Rect board = BoardViewportLayout.CalculateAspectFitRect(viewport);

            Assert.That(board.height, Is.EqualTo(2048f).Within(0.001f));
            Assert.That(board.width, Is.EqualTo(1280f).Within(0.001f));
            Assert.That(board.xMin, Is.EqualTo(128f).Within(0.001f));
            Assert.That(board.xMax, Is.EqualTo(1408f).Within(0.001f));
        }

        [Test]
        public void TallPhoneAspect_ProducesVerticalPresentationMargins()
        {
            var viewport = new Rect(0f, 0f, 1080f, 2400f);

            Rect board = BoardViewportLayout.CalculateAspectFitRect(viewport);

            Assert.That(board.width, Is.EqualTo(1080f).Within(0.001f));
            Assert.That(board.height, Is.EqualTo(1728f).Within(0.001f));
            Assert.That(board.yMin, Is.EqualTo(336f).Within(0.001f));
            Assert.That(board.yMax, Is.EqualTo(2064f).Within(0.001f));
        }

        [Test]
        public void BottomAlignment_PreservesAspectAndUsesLowerViewportEdge()
        {
            var viewport = new Rect(12f, 24f, 1080f, 2400f);

            Rect board = BoardViewportLayout.CalculateAspectFitRect(
                viewport,
                0f);

            Assert.That(board.width / board.height,
                Is.EqualTo(10f / 16f).Within(0.00001f));
            Assert.That(board.yMin, Is.EqualTo(viewport.yMin).Within(0.001f));
            Assert.That(board.xMin, Is.GreaterThanOrEqualTo(viewport.xMin));
            Assert.That(board.xMax, Is.LessThanOrEqualTo(viewport.xMax));
        }

        [TestCase(-0.01f)]
        [TestCase(1.01f)]
        [TestCase(float.NaN)]
        public void InvalidVerticalAlignment_IsRejected(float alignment)
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(
                () => BoardViewportLayout.CalculateAspectFitRect(
                    new Rect(0f, 0f, 1080f, 1920f),
                    alignment));
        }

        [Test]
        public void OrthographicSize_ContainsBothBoardDimensions()
        {
            float phoneSize = BoardViewportLayout.CalculateOrthographicSize(
                new Rect(0f, 0f, 1080f, 1920f));
            float tabletSize = BoardViewportLayout.CalculateOrthographicSize(
                new Rect(0f, 0f, 1536f, 2048f));

            Assert.That(phoneSize, Is.EqualTo(80f / 9f).Within(0.00001f));
            Assert.That(tabletSize, Is.EqualTo(8f).Within(0.00001f));
        }

        [TestCase(0f, 100f)]
        [TestCase(100f, 0f)]
        [TestCase(-1f, 100f)]
        public void InvalidViewportDimensions_AreRejected(float width, float height)
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(
                () => BoardViewportLayout.CalculateAspectFitRect(
                    new Rect(0f, 0f, width, height)));
        }
    }
}
