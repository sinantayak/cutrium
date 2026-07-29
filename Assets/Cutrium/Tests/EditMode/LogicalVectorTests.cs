using System;
using Cutrium.Gameplay.Geometry;
using NUnit.Framework;

namespace Cutrium.Gameplay.EditModeTests
{
    public sealed class LogicalVectorTests
    {
        [Test]
        public void ConstructorAndEquality_UseExactStoredValues()
        {
            var vector = new LogicalVector(3f, 4f);

            Assert.That(vector.X, Is.EqualTo(3f));
            Assert.That(vector.Y, Is.EqualTo(4f));
            Assert.That(vector, Is.EqualTo(new LogicalVector(3f, 4f)));
            Assert.That(vector, Is.Not.EqualTo(new LogicalVector(4f, 3f)));
        }

        [Test]
        public void Arithmetic_ProducesExpectedVectors()
        {
            var left = new LogicalVector(4f, -2f);
            var right = new LogicalVector(1f, 3f);

            Assert.That(left + right, Is.EqualTo(new LogicalVector(5f, 1f)));
            Assert.That(left - right, Is.EqualTo(new LogicalVector(3f, -5f)));
            Assert.That(-left, Is.EqualTo(new LogicalVector(-4f, 2f)));
            Assert.That(left * 2f, Is.EqualTo(new LogicalVector(8f, -4f)));
            Assert.That(2f * right, Is.EqualTo(new LogicalVector(2f, 6f)));
            Assert.That(left / 2f, Is.EqualTo(new LogicalVector(2f, -1f)));
        }

        [Test]
        public void LengthAndDotProduct_AreFoundationalFloatOperations()
        {
            var vector = new LogicalVector(3f, 4f);

            Assert.That(vector.LengthSquared, Is.EqualTo(25f));
            Assert.That(vector.Length, Is.EqualTo(5f));
            Assert.That(LogicalVector.Dot(vector, new LogicalVector(2f, -1f)), Is.EqualTo(2f));
        }

        [Test]
        public void DivisionByZero_IsRejected()
        {
            Assert.Throws<DivideByZeroException>(() =>
            {
                var ignored = new LogicalVector(1f, 2f) / 0f;
            });
        }

        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        [TestCase(float.NegativeInfinity)]
        public void Constructor_RejectsNonFiniteValues(float invalid)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new LogicalVector(invalid, 0f));
            Assert.Throws<ArgumentOutOfRangeException>(() => new LogicalVector(0f, invalid));
        }
    }
}
