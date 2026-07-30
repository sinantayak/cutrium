using Cutrium.Gameplay.Geometry;
using Cutrium.Unity.Layout;
using NUnit.Framework;
using UnityEngine;

namespace Cutrium.Gameplay.EditModeTests
{
    public sealed class BoardScreenMapperTests
    {
        private static readonly Rect BoardRect = new Rect(128f, 0f, 1280f, 2048f);

        [Test]
        public void Center_MapsToCenterOfFixedLogicalBoard()
        {
            bool mapped = BoardScreenMapper.TryMap(
                BoardRect,
                BoardRect.center,
                out LogicalPoint logicalPoint);

            Assert.That(mapped, Is.True);
            Assert.That(logicalPoint, Is.EqualTo(new LogicalPoint(5f, 8f)));
        }

        [TestCase(128f, 0f, 0f, 0f)]
        [TestCase(1408f, 0f, 10f, 0f)]
        [TestCase(128f, 2048f, 0f, 16f)]
        [TestCase(1408f, 2048f, 10f, 16f)]
        public void BoardEdges_MapToLogicalBounds(
            float screenX,
            float screenY,
            float logicalX,
            float logicalY)
        {
            bool mapped = BoardScreenMapper.TryMap(
                BoardRect,
                new Vector2(screenX, screenY),
                out LogicalPoint logicalPoint);

            Assert.That(mapped, Is.True);
            Assert.That(logicalPoint, Is.EqualTo(new LogicalPoint(logicalX, logicalY)));
        }

        [TestCase(127f, 1024f)]
        [TestCase(1409f, 1024f)]
        [TestCase(768f, -1f)]
        [TestCase(768f, 2049f)]
        public void DecorativeMargins_DoNotMap(float screenX, float screenY)
        {
            bool mapped = BoardScreenMapper.TryMap(
                BoardRect,
                new Vector2(screenX, screenY),
                out LogicalPoint logicalPoint);

            Assert.That(mapped, Is.False);
            Assert.That(logicalPoint, Is.EqualTo(default(LogicalPoint)));
        }
    }
}
