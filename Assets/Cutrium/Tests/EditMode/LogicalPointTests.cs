using System;
using Cutrium.Gameplay.Geometry;
using NUnit.Framework;

namespace Cutrium.Gameplay.EditModeTests
{
    public sealed class LogicalPointTests
    {
        [Test]
        public void Constructor_StoresCoordinates()
        {
            var point = new LogicalPoint(2.5f, -4f);

            Assert.That(point.X, Is.EqualTo(2.5f));
            Assert.That(point.Y, Is.EqualTo(-4f));
        }

        [Test]
        public void Equality_UsesExactStoredValues()
        {
            var point = new LogicalPoint(2f, 3f);
            var same = new LogicalPoint(2f, 3f);
            var different = new LogicalPoint(2f, 3.0001f);

            Assert.That(point, Is.EqualTo(same));
            Assert.That(point == same, Is.True);
            Assert.That(point != different, Is.True);
        }

        [Test]
        public void AddingAndSubtractingVector_TranslatesPoint()
        {
            var point = new LogicalPoint(3f, 5f);
            var offset = new LogicalVector(-2f, 4f);

            Assert.That(point + offset, Is.EqualTo(new LogicalPoint(1f, 9f)));
            Assert.That(point - offset, Is.EqualTo(new LogicalPoint(5f, 1f)));
        }

        [Test]
        public void SubtractingPoints_ProducesDisplacementVector()
        {
            var displacement = new LogicalPoint(5f, 1f) - new LogicalPoint(2f, -3f);

            Assert.That(displacement, Is.EqualTo(new LogicalVector(3f, 4f)));
        }

        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        [TestCase(float.NegativeInfinity)]
        public void Constructor_RejectsNonFiniteCoordinates(float invalid)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new LogicalPoint(invalid, 0f));
            Assert.Throws<ArgumentOutOfRangeException>(() => new LogicalPoint(0f, invalid));
        }
    }
}
