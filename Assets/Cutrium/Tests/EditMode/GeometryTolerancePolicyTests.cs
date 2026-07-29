using System;
using Cutrium.Gameplay.Geometry;
using NUnit.Framework;

namespace Cutrium.Gameplay.EditModeTests
{
    public sealed class GeometryTolerancePolicyTests
    {
        private GeometryTolerancePolicy _policy;

        [SetUp]
        public void SetUp()
        {
            _policy = new GeometryTolerancePolicy(
                distanceTolerance: 0.125f,
                timeTolerance: 0.0625f,
                cornerTolerance: 0.03125f,
                areaTolerance: 0.25f);
        }

        [Test]
        public void Constructor_StoresNamedToleranceValues()
        {
            Assert.That(_policy.DistanceTolerance, Is.EqualTo(0.125f));
            Assert.That(_policy.TimeTolerance, Is.EqualTo(0.0625f));
            Assert.That(_policy.CornerTolerance, Is.EqualTo(0.03125f));
            Assert.That(_policy.AreaTolerance, Is.EqualTo(0.25f));
        }

        [TestCase(-0.01f, 0f, 0f, 0f)]
        [TestCase(0f, float.NaN, 0f, 0f)]
        [TestCase(0f, 0f, float.PositiveInfinity, 0f)]
        [TestCase(0f, 0f, 0f, -0.01f)]
        public void Constructor_RejectsInvalidToleranceValues(
            float distance,
            float time,
            float corner,
            float area)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new GeometryTolerancePolicy(distance, time, corner, area));
        }

        [Test]
        public void DistanceComparison_IsTrueBelowAndAtBoundaryButFalseAbove()
        {
            Assert.That(_policy.IsDistanceApproximatelyEqual(0f, 0.0625f), Is.True);
            Assert.That(_policy.IsDistanceApproximatelyEqual(0f, 0.125f), Is.True);
            Assert.That(_policy.IsDistanceApproximatelyEqual(0f, 0.1251f), Is.False);
        }

        [Test]
        public void NamedComparisons_UseTheirOwnToleranceValues()
        {
            Assert.That(_policy.IsTimeApproximatelyEqual(0f, 0.0625f), Is.True);
            Assert.That(_policy.IsTimeApproximatelyEqual(0f, 0.0626f), Is.False);
            Assert.That(_policy.IsCornerTimeTie(0f, 0.03125f), Is.True);
            Assert.That(_policy.IsCornerTimeTie(0f, 0.0313f), Is.False);
            Assert.That(_policy.IsAreaApproximatelyEqual(10f, 10.25f), Is.True);
            Assert.That(_policy.IsAreaApproximatelyEqual(10f, 10.251f), Is.False);
        }

        [Test]
        public void PointVectorAndRectangleComparisons_UseDistanceTolerance()
        {
            Assert.That(
                _policy.AreApproximatelyEqual(
                    new LogicalPoint(1f, 2f),
                    new LogicalPoint(1.125f, 1.875f)),
                Is.True);
            Assert.That(
                _policy.AreApproximatelyEqual(
                    new LogicalVector(1f, 2f),
                    new LogicalVector(1.1251f, 2f)),
                Is.False);
            Assert.That(
                _policy.AreApproximatelyEqual(
                    new LogicalRect(0f, 0f, 4f, 6f),
                    new LogicalRect(0.125f, 0f, 4f, 6f)),
                Is.True);
        }

        [Test]
        public void ApproximateContainment_AcceptsToleranceBoundaryAndRejectsBeyondIt()
        {
            var rectangle = new LogicalRect(0f, 0f, 4f, 6f);

            Assert.That(rectangle.Contains(new LogicalPoint(-0.125f, 3f), _policy), Is.True);
            Assert.That(rectangle.Contains(new LogicalPoint(-0.1251f, 3f), _policy), Is.False);
            Assert.That(rectangle.Contains(new LogicalPoint(4.125f, 3f), _policy), Is.True);
            Assert.That(rectangle.Contains(new LogicalPoint(4.1251f, 3f), _policy), Is.False);
        }

        [Test]
        public void OrderedDistanceComparisons_IncludeApproximateBoundary()
        {
            Assert.That(_policy.IsLessThanOrApproximatelyEqualDistance(5.125f, 5f), Is.True);
            Assert.That(_policy.IsLessThanOrApproximatelyEqualDistance(5.1251f, 5f), Is.False);
            Assert.That(_policy.IsGreaterThanOrApproximatelyEqualDistance(4.875f, 5f), Is.True);
            Assert.That(_policy.IsGreaterThanOrApproximatelyEqualDistance(4.8749f, 5f), Is.False);
        }

        [Test]
        public void NonFiniteValues_AreNotApproximatelyEqualUnlessIdenticalInfinity()
        {
            Assert.That(_policy.IsDistanceApproximatelyEqual(float.NaN, float.NaN), Is.False);
            Assert.That(_policy.IsDistanceApproximatelyEqual(float.PositiveInfinity, 1f), Is.False);
            Assert.That(
                _policy.IsDistanceApproximatelyEqual(float.PositiveInfinity, float.PositiveInfinity),
                Is.True);
        }
    }
}
