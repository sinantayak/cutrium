using System;
using Cutrium.Gameplay.Board;
using Cutrium.Gameplay.Geometry;
using Cutrium.Gameplay.Threats;
using NUnit.Framework;

namespace Cutrium.Gameplay.EditModeTests
{
    public sealed class ThreatMotionSolverTests
    {
        private static readonly GeometryTolerancePolicy Tolerance =
            new GeometryTolerancePolicy(0.0001f, 0.00001f, 0.0001f, 0.001f);

        [Test]
        public void Move_ReflectsFromHorizontalBoundary()
        {
            ThreatMotionResult result = Move(
                new LogicalPoint(8f, 5f),
                new LogicalVector(4f, 0f),
                1f);

            AssertPoint(result.Threat.Position, 6f, 5f);
            AssertVector(result.Threat.Velocity, -4f, 0f);
            Assert.That(result.ImpactCount, Is.EqualTo(1));
        }

        [Test]
        public void Move_ReflectsFromVerticalBoundary()
        {
            ThreatMotionResult result = Move(
                new LogicalPoint(5f, 14f),
                new LogicalVector(0f, 4f),
                1f);

            AssertPoint(result.Threat.Position, 5f, 12f);
            AssertVector(result.Threat.Velocity, 0f, -4f);
            Assert.That(result.ImpactCount, Is.EqualTo(1));
        }

        [Test]
        public void Move_ReflectsBothComponentsAtExactCorner()
        {
            ThreatMotionResult result = Move(
                new LogicalPoint(8f, 14f),
                new LogicalVector(2f, 2f),
                1f);

            AssertPoint(result.Threat.Position, 8f, 14f);
            AssertVector(result.Threat.Velocity, -2f, -2f);
            Assert.That(result.ImpactCount, Is.EqualTo(1));
        }

        [Test]
        public void Move_TreatsNearCornerTimesWithinPolicyAsOneImpact()
        {
            ThreatMotionResult result = Move(
                new LogicalPoint(8f, 13.9999f),
                new LogicalVector(2f, 2f),
                0.5f);

            AssertPoint(result.Threat.Position, 9f, 15f);
            AssertVector(result.Threat.Velocity, -2f, -2f);
            Assert.That(result.ImpactCount, Is.EqualTo(1));
        }

        [Test]
        public void Move_PreservesShallowAngleMotion()
        {
            ThreatMotionResult result = Move(
                new LogicalPoint(8f, 8f),
                new LogicalVector(4f, 0.01f),
                0.5f);

            AssertPoint(result.Threat.Position, 8f, 8.005f);
            AssertVector(result.Threat.Velocity, -4f, 0.01f);
        }

        [Test]
        public void Move_HandlesMultipleImpactsInOneTick()
        {
            ThreatMotionResult result = Move(
                new LogicalPoint(5f, 8f),
                new LogicalVector(100f, 0f),
                0.2f,
                8);

            AssertPoint(result.Threat.Position, 9f, 8f);
            AssertVector(result.Threat.Velocity, -100f, 0f);
            Assert.That(result.ImpactCount, Is.EqualTo(3));
            Assert.That(result.Diagnostic, Is.EqualTo(ThreatMotionDiagnostic.None));
        }

        [Test]
        public void Move_HighSpeedNeverEscapesInsetRoom()
        {
            ThreatMotionResult result = Move(
                new LogicalPoint(5f, 8f),
                new LogicalVector(100f, 71f),
                1f,
                64);

            Assert.That(result.Threat.Position.X, Is.InRange(1f, 9f));
            Assert.That(result.Threat.Position.Y, Is.InRange(1f, 15f));
            Assert.That(result.Diagnostic, Is.EqualTo(ThreatMotionDiagnostic.None));
        }

        [Test]
        public void Move_ZeroElapsedTimeDoesNotChangeState()
        {
            ThreatState threat = CreateThreat(
                new LogicalPoint(5f, 8f),
                new LogicalVector(3f, 2f));
            ThreatMotionResult result = ThreatMotionSolver.Move(
                CreateRoom(),
                threat,
                0f,
                8,
                Tolerance);

            Assert.That(result.Threat, Is.EqualTo(threat));
            Assert.That(result.ImpactCount, Is.Zero);
        }

        [Test]
        public void Move_ImpactLimitReturnsDiagnosticAndValidState()
        {
            ThreatMotionResult result = Move(
                new LogicalPoint(5f, 8f),
                new LogicalVector(100f, 0f),
                1f,
                1);

            Assert.That(
                result.Diagnostic,
                Is.EqualTo(ThreatMotionDiagnostic.ImpactLimitReached));
            Assert.That(result.ImpactCount, Is.EqualTo(1));
            Assert.That(result.Threat.Position.X, Is.InRange(1f, 9f));
            Assert.That(result.Threat.Position.Y, Is.InRange(1f, 15f));
        }

        [Test]
        public void Move_RepeatedExecutionIsDeterministic()
        {
            ThreatState left = CreateThreat(
                new LogicalPoint(5f, 8f),
                new LogicalVector(2.4f, 1.8f));
            ThreatState right = left;
            for (int index = 0; index < 600; index++)
            {
                left = ThreatMotionSolver.Move(
                    CreateRoom(), left, 1f / 60f, 8, Tolerance).Threat;
                right = ThreatMotionSolver.Move(
                    CreateRoom(), right, 1f / 60f, 8, Tolerance).Threat;
            }

            Assert.That(left, Is.EqualTo(right));
        }

        [Test]
        public void Move_RejectsThreatOutsideInsetRoom()
        {
            ThreatState threat = CreateThreat(
                new LogicalPoint(0.5f, 8f),
                new LogicalVector(1f, 0f));

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                ThreatMotionSolver.Move(
                    CreateRoom(), threat, 1f / 60f, 8, Tolerance));
        }

        [Test]
        public void Move_RejectsMismatchedRoomAndInvalidArguments()
        {
            ThreatState threat = CreateThreat(
                new LogicalPoint(5f, 8f),
                new LogicalVector(1f, 0f));
            var differentRoom = new RoomState(
                new RoomId(2),
                new LogicalRect(0f, 0f, 10f, 16f));

            Assert.Throws<ArgumentException>(() =>
                ThreatMotionSolver.Move(
                    differentRoom, threat, 1f / 60f, 8, Tolerance));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                ThreatMotionSolver.Move(
                    CreateRoom(), threat, float.NaN, 8, Tolerance));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                ThreatMotionSolver.Move(
                    CreateRoom(), threat, 1f / 60f, 0, Tolerance));
        }

        private static ThreatMotionResult Move(
            LogicalPoint position,
            LogicalVector velocity,
            float elapsedTime,
            int maximumImpacts = 8)
        {
            return ThreatMotionSolver.Move(
                CreateRoom(),
                CreateThreat(position, velocity),
                elapsedTime,
                maximumImpacts,
                Tolerance);
        }

        private static RoomState CreateRoom() =>
            new RoomState(
                new RoomId(1),
                new LogicalRect(0f, 0f, 10f, 16f));

        private static ThreatState CreateThreat(
            LogicalPoint position,
            LogicalVector velocity) =>
            new ThreatState(
                new ThreatId(1),
                new RoomId(1),
                position,
                velocity,
                1f);

        private static void AssertPoint(
            LogicalPoint point,
            float expectedX,
            float expectedY)
        {
            Assert.That(point.X, Is.EqualTo(expectedX).Within(0.0002f));
            Assert.That(point.Y, Is.EqualTo(expectedY).Within(0.0002f));
        }

        private static void AssertVector(
            LogicalVector vector,
            float expectedX,
            float expectedY)
        {
            Assert.That(vector.X, Is.EqualTo(expectedX).Within(0.0002f));
            Assert.That(vector.Y, Is.EqualTo(expectedY).Within(0.0002f));
        }
    }
}
