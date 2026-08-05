using System;
using Cutrium.Gameplay.Board;
using Cutrium.Gameplay.Geometry;
using Cutrium.Gameplay.Session;
using Cutrium.Gameplay.Threats;
using NUnit.Framework;

namespace Cutrium.Gameplay.EditModeTests
{
    public sealed class RoomAndThreatStateTests
    {
        [Test]
        public void Ids_RequirePositiveValuesAndCompareByValue()
        {
            Assert.That(new RoomId(3), Is.EqualTo(new RoomId(3)));
            Assert.That(new ThreatId(5), Is.EqualTo(new ThreatId(5)));
            Assert.Throws<ArgumentOutOfRangeException>(() => new RoomId(0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new ThreatId(-1));
        }

        [Test]
        public void Room_RequiresPositiveDimensions()
        {
            var id = new RoomId(1);
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new RoomState(id, new LogicalRect(0f, 0f, 0f, 16f)));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new RoomState(id, new LogicalRect(0f, 0f, 10f, 0f)));
        }

        [Test]
        public void Threat_StoresFloatBackedLogicalMotionState()
        {
            var threat = new ThreatState(
                new ThreatId(1),
                new RoomId(2),
                new LogicalPoint(4f, 6f),
                new LogicalVector(3f, 4f),
                0.35f);

            Assert.That(threat.Position, Is.EqualTo(new LogicalPoint(4f, 6f)));
            Assert.That(threat.Velocity, Is.EqualTo(new LogicalVector(3f, 4f)));
            Assert.That(threat.Speed, Is.EqualTo(5f));
            Assert.That(threat.Radius, Is.EqualTo(0.35f));
        }

        [TestCase(0f)]
        [TestCase(-1f)]
        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        public void Threat_RejectsInvalidRadius(float radius)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new ThreatState(
                new ThreatId(1),
                new RoomId(1),
                new LogicalPoint(5f, 8f),
                new LogicalVector(1f, 0f),
                radius));
        }

        [Test]
        public void Threat_RejectsZeroVelocity()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new ThreatState(
                new ThreatId(1),
                new RoomId(1),
                new LogicalPoint(5f, 8f),
                LogicalVector.Zero,
                0.35f));
        }

        [Test]
        public void Construction_RejectsNonFiniteLogicalState()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new LogicalRect(0f, 0f, float.PositiveInfinity, 16f));
            Assert.Throws<ArgumentOutOfRangeException>(() => new ThreatState(
                new ThreatId(1),
                new RoomId(1),
                new LogicalPoint(5f, 8f),
                new LogicalVector(float.MaxValue, float.MaxValue),
                0.35f));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new ThreatMotionConfiguration(
                    new LogicalRect(0f, 0f, 10f, 16f),
                    new LogicalPoint(5f, 8f),
                    new LogicalVector(1f, 0f),
                    float.NaN,
                    0.35f,
                    8));
        }

        [Test]
        public void Configuration_NormalizesDirectionWithoutChangingSpeed()
        {
            var configuration = new ThreatMotionConfiguration(
                new LogicalRect(0f, 0f, 10f, 16f),
                new LogicalPoint(5f, 8f),
                new LogicalVector(3f, 4f),
                2.5f,
                0.35f,
                8);

            Assert.That(configuration.InitialDirection.Length, Is.EqualTo(1f));
            Assert.That(configuration.Speed, Is.EqualTo(2.5f));
        }

        [Test]
        public void Session_ResetRestoresExactInitialState()
        {
            ThreatMotionSession session = CreateSession();
            ThreatState initial = session.Threat;
            session.Tick(1f / 60f);

            Assert.That(session.Threat, Is.Not.EqualTo(initial));
            session.Reset();

            Assert.That(session.Threat, Is.EqualTo(initial));
            Assert.That(session.TickCount, Is.Zero);
            Assert.That(session.LastDiagnostic, Is.EqualTo(ThreatMotionDiagnostic.None));
        }

        private static ThreatMotionSession CreateSession()
        {
            var configuration = new ThreatMotionConfiguration(
                new LogicalRect(0f, 0f, 10f, 16f),
                new LogicalPoint(5f, 8f),
                new LogicalVector(0.8f, 0.6f),
                3f,
                0.35f,
                8);
            return new ThreatMotionSession(configuration, CreateTolerance());
        }

        private static GeometryTolerancePolicy CreateTolerance() =>
            new GeometryTolerancePolicy(0.0001f, 0.00001f, 0.0001f, 0.001f);
    }
}
