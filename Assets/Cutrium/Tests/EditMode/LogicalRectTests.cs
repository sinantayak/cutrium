using System;
using Cutrium.Gameplay.Geometry;
using NUnit.Framework;

namespace Cutrium.Gameplay.EditModeTests
{
    public sealed class LogicalRectTests
    {
        [Test]
        public void Constructor_ExposesBoundsSizeCenterAndArea()
        {
            var rectangle = new LogicalRect(-2f, 3f, 10f, 6f);

            Assert.That(rectangle.Min, Is.EqualTo(new LogicalPoint(-2f, 3f)));
            Assert.That(rectangle.Max, Is.EqualTo(new LogicalPoint(8f, 9f)));
            Assert.That(rectangle.Size, Is.EqualTo(new LogicalVector(10f, 6f)));
            Assert.That(rectangle.Center, Is.EqualTo(new LogicalPoint(3f, 6f)));
            Assert.That(rectangle.Area, Is.EqualTo(60f));
        }

        [Test]
        public void FromMinMax_ConstructsEquivalentRectangle()
        {
            var rectangle = LogicalRect.FromMinMax(-2f, 3f, 8f, 9f);

            Assert.That(rectangle, Is.EqualTo(new LogicalRect(-2f, 3f, 10f, 6f)));
        }

        [Test]
        public void Contains_IsInclusiveAtBounds()
        {
            var rectangle = new LogicalRect(1f, 2f, 4f, 6f);

            Assert.That(rectangle.Contains(new LogicalPoint(1f, 2f)), Is.True);
            Assert.That(rectangle.Contains(new LogicalPoint(5f, 8f)), Is.True);
            Assert.That(rectangle.Contains(new LogicalPoint(3f, 4f)), Is.True);
        }

        [Test]
        public void Contains_RejectsPointsOutsideBounds()
        {
            var rectangle = new LogicalRect(1f, 2f, 4f, 6f);

            Assert.That(rectangle.Contains(new LogicalPoint(0.99f, 4f)), Is.False);
            Assert.That(rectangle.Contains(new LogicalPoint(5.01f, 4f)), Is.False);
            Assert.That(rectangle.Contains(new LogicalPoint(3f, 1.99f)), Is.False);
            Assert.That(rectangle.Contains(new LogicalPoint(3f, 8.01f)), Is.False);
        }

        [Test]
        public void ZeroDimensions_AreValidAndHaveZeroArea()
        {
            var rectangle = new LogicalRect(2f, 3f, 0f, 0f);

            Assert.That(rectangle.Area, Is.Zero);
            Assert.That(rectangle.Contains(new LogicalPoint(2f, 3f)), Is.True);
        }

        [TestCase(-1f, 1f)]
        [TestCase(1f, -1f)]
        public void Constructor_RejectsNegativeDimensions(float width, float height)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new LogicalRect(0f, 0f, width, height));
        }

        [Test]
        public void FromMinMax_RejectsReversedBounds()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => LogicalRect.FromMinMax(1f, 0f, 0f, 1f));
            Assert.Throws<ArgumentOutOfRangeException>(() => LogicalRect.FromMinMax(0f, 1f, 1f, 0f));
        }

        [Test]
        public void Equality_UsesExactStoredBounds()
        {
            var rectangle = new LogicalRect(1f, 2f, 3f, 4f);

            Assert.That(rectangle, Is.EqualTo(new LogicalRect(1f, 2f, 3f, 4f)));
            Assert.That(rectangle, Is.Not.EqualTo(new LogicalRect(1f, 2f, 3f, 4.0001f)));
        }
    }
}
