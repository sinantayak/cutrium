using System;
using Cutrium.Gameplay.Board;
using Cutrium.Gameplay.Geometry;
using Cutrium.Gameplay.Threats;

namespace Cutrium.Gameplay.Barriers
{
    public static class GrowingBarrierMotionSolver
    {
        public static BarrierSimulationResult Move(
            RoomState room,
            ThreatState threat,
            BarrierState barrier,
            float elapsedTime,
            int maximumIterations,
            int maximumThreatImpacts,
            GeometryTolerancePolicy tolerance)
        {
            Validate(
                room,
                threat,
                barrier,
                elapsedTime,
                maximumIterations,
                maximumThreatImpacts);

            if (barrier.Lifecycle == BarrierLifecycle.Locked)
            {
                return MoveAfterLock(
                    room,
                    threat,
                    barrier,
                    elapsedTime,
                    maximumThreatImpacts,
                    tolerance,
                    BarrierSimulationEvent.None,
                    0,
                    float.PositiveInfinity);
            }

            if (barrier.Lifecycle == BarrierLifecycle.Failed
                || elapsedTime == 0f)
            {
                return new BarrierSimulationResult(
                    threat,
                    barrier,
                    BarrierSimulationEvent.None,
                    BarrierContactKind.None,
                    BarrierSimulationDiagnostic.None,
                    0,
                    float.PositiveInfinity);
            }

            float minX = room.Bounds.MinX + threat.Radius;
            float maxX = room.Bounds.MaxX - threat.Radius;
            float minY = room.Bounds.MinY + threat.Radius;
            float maxY = room.Bounds.MaxY - threat.Radius;
            float x = threat.Position.X;
            float y = threat.Position.Y;
            float velocityX = threat.Velocity.X;
            float velocityY = threat.Velocity.Y;
            float remaining = elapsedTime;
            int iterations = 0;

            while (remaining > tolerance.TimeTolerance)
            {
                if (iterations >= maximumIterations)
                {
                    ThreatState capped = threat.WithMotion(
                        new LogicalPoint(
                            Clamp(x, minX, maxX),
                            Clamp(y, minY, maxY)),
                        new LogicalVector(velocityX, velocityY));
                    return new BarrierSimulationResult(
                        capped,
                        barrier,
                        BarrierSimulationEvent.None,
                        BarrierContactKind.None,
                        BarrierSimulationDiagnostic.IterationLimitReached,
                        iterations,
                        float.PositiveInfinity);
                }

                iterations++;
                float xWallTime = TimeToBoundary(
                    x, velocityX, minX, maxX, tolerance);
                float yWallTime = TimeToBoundary(
                    y, velocityY, minY, maxY, tolerance);
                float negativeCompletion = TimeToCompletion(
                    barrier.NegativeLength,
                    barrier.NegativeTargetLength,
                    barrier.GrowthSpeed,
                    tolerance);
                float positiveCompletion = TimeToCompletion(
                    barrier.PositiveLength,
                    barrier.PositiveTargetLength,
                    barrier.GrowthSpeed,
                    tolerance);
                float horizon = Math.Min(
                    remaining,
                    Math.Min(
                        Math.Min(xWallTime, yWallTime),
                        Math.Min(negativeCompletion, positiveCompletion)));
                horizon = Math.Max(0f, horizon);

                bool hasContact = TryFindContact(
                    x,
                    y,
                    velocityX,
                    velocityY,
                    barrier,
                    threat.Radius + barrier.CollisionHalfWidth,
                    horizon,
                    tolerance,
                    out float contactTime,
                    out BarrierContactKind contactKind);
                BarrierState horizonBarrier = barrier.AdvanceGrowth(
                    horizon,
                    tolerance);
                bool lockAtHorizon =
                    horizonBarrier.Lifecycle == BarrierLifecycle.Locked;

                if (hasContact
                    && (!lockAtHorizon
                        || !tolerance.IsTimeApproximatelyEqual(
                            contactTime,
                            horizon)))
                {
                    x += velocityX * contactTime;
                    y += velocityY * contactTime;
                    barrier = barrier.AdvanceGrowth(contactTime, tolerance).Fail();
                    float elapsedUntilEvent =
                        elapsedTime - remaining + contactTime;
                    float afterContact = remaining - contactTime;
                    ThreatState contactThreat = threat.WithMotion(
                        new LogicalPoint(x, y),
                        new LogicalVector(velocityX, velocityY));
                    ThreatMotionResult continuation = ThreatMotionSolver.Move(
                        room,
                        contactThreat,
                        Math.Max(0f, afterContact),
                        maximumThreatImpacts,
                        tolerance);
                    return new BarrierSimulationResult(
                        continuation.Threat,
                        barrier,
                        BarrierSimulationEvent.Failed,
                        contactKind,
                        ToDiagnostic(continuation.Diagnostic),
                        iterations,
                        elapsedUntilEvent);
                }

                x += velocityX * horizon;
                y += velocityY * horizon;
                remaining -= horizon;
                barrier = horizonBarrier;

                bool hitX = tolerance.IsTimeApproximatelyEqual(
                    xWallTime,
                    horizon);
                bool hitY = tolerance.IsTimeApproximatelyEqual(
                    yWallTime,
                    horizon);
                if (hitX)
                {
                    x = velocityX > 0f ? maxX : minX;
                    velocityX = -velocityX;
                }

                if (hitY)
                {
                    y = velocityY > 0f ? maxY : minY;
                    velocityY = -velocityY;
                }

                if (barrier.Lifecycle == BarrierLifecycle.Locked)
                {
                    ThreatState lockThreat = threat.WithMotion(
                        new LogicalPoint(x, y),
                        new LogicalVector(velocityX, velocityY));
                    return MoveAfterLock(
                        room,
                        lockThreat,
                        barrier,
                        Math.Max(0f, remaining),
                        maximumThreatImpacts,
                        tolerance,
                        BarrierSimulationEvent.Locked,
                        iterations,
                        elapsedTime - remaining);
                }

                if (horizon == remaining && remaining == 0f)
                {
                    break;
                }
            }

            ThreatState moved = threat.WithMotion(
                new LogicalPoint(
                    Clamp(x, minX, maxX),
                    Clamp(y, minY, maxY)),
                new LogicalVector(velocityX, velocityY));
            return new BarrierSimulationResult(
                moved,
                barrier,
                BarrierSimulationEvent.None,
                BarrierContactKind.None,
                BarrierSimulationDiagnostic.None,
                iterations,
                float.PositiveInfinity);
        }

        private static BarrierSimulationResult MoveAfterLock(
            RoomState parentRoom,
            ThreatState threat,
            BarrierState barrier,
            float elapsedTime,
            int maximumThreatImpacts,
            GeometryTolerancePolicy tolerance,
            BarrierSimulationEvent simulationEvent,
            int iterations,
            float elapsedUntilEvent)
        {
            RoomState threatSide = CreateThreatSideRoom(
                parentRoom,
                threat,
                barrier);
            ThreatMotionResult moved = ThreatMotionSolver.Move(
                threatSide,
                threat,
                elapsedTime,
                maximumThreatImpacts,
                tolerance);
            return new BarrierSimulationResult(
                moved.Threat,
                barrier,
                simulationEvent,
                BarrierContactKind.None,
                ToDiagnostic(moved.Diagnostic),
                iterations,
                elapsedUntilEvent);
        }

        private static RoomState CreateThreatSideRoom(
            RoomState parent,
            ThreatState threat,
            BarrierState barrier)
        {
            LogicalRect bounds = parent.Bounds;
            if (barrier.Orientation == BarrierOrientation.Horizontal)
            {
                bounds = threat.Position.Y < barrier.Origin.Y
                    ? LogicalRect.FromMinMax(
                        bounds.MinX,
                        bounds.MinY,
                        bounds.MaxX,
                        barrier.Origin.Y - barrier.CollisionHalfWidth)
                    : LogicalRect.FromMinMax(
                        bounds.MinX,
                        barrier.Origin.Y + barrier.CollisionHalfWidth,
                        bounds.MaxX,
                        bounds.MaxY);
            }
            else
            {
                bounds = threat.Position.X < barrier.Origin.X
                    ? LogicalRect.FromMinMax(
                        bounds.MinX,
                        bounds.MinY,
                        barrier.Origin.X - barrier.CollisionHalfWidth,
                        bounds.MaxY)
                    : LogicalRect.FromMinMax(
                        barrier.Origin.X + barrier.CollisionHalfWidth,
                        bounds.MinY,
                        bounds.MaxX,
                        bounds.MaxY);
            }

            return new RoomState(parent.Id, bounds);
        }

        private static bool TryFindContact(
            float x,
            float y,
            float velocityX,
            float velocityY,
            BarrierState barrier,
            float contactRadius,
            float maximumTime,
            GeometryTolerancePolicy tolerance,
            out float contactTime,
            out BarrierContactKind contactKind)
        {
            float along = barrier.Orientation == BarrierOrientation.Horizontal
                ? x
                : y;
            float across = barrier.Orientation == BarrierOrientation.Horizontal
                ? y
                : x;
            float alongVelocity =
                barrier.Orientation == BarrierOrientation.Horizontal
                    ? velocityX
                    : velocityY;
            float acrossVelocity =
                barrier.Orientation == BarrierOrientation.Horizontal
                    ? velocityY
                    : velocityX;
            float originAlong =
                barrier.Orientation == BarrierOrientation.Horizontal
                    ? barrier.Origin.X
                    : barrier.Origin.Y;
            float originAcross =
                barrier.Orientation == BarrierOrientation.Horizontal
                    ? barrier.Origin.Y
                    : barrier.Origin.X;

            contactTime = float.PositiveInfinity;
            contactKind = BarrierContactKind.None;
            ConsiderBodyContact(
                along,
                across,
                alongVelocity,
                acrossVelocity,
                originAlong,
                originAcross,
                barrier,
                contactRadius,
                maximumTime,
                tolerance,
                ref contactTime,
                ref contactKind);

            float negativeTip = originAlong - barrier.NegativeLength;
            float negativeTipVelocity = barrier.NegativeComplete
                ? 0f
                : -barrier.GrowthSpeed;
            ConsiderTipContact(
                along - negativeTip,
                across - originAcross,
                alongVelocity - negativeTipVelocity,
                acrossVelocity,
                contactRadius,
                maximumTime,
                BarrierContactKind.NegativeTip,
                tolerance,
                ref contactTime,
                ref contactKind);

            float positiveTip = originAlong + barrier.PositiveLength;
            float positiveTipVelocity = barrier.PositiveComplete
                ? 0f
                : barrier.GrowthSpeed;
            ConsiderTipContact(
                along - positiveTip,
                across - originAcross,
                alongVelocity - positiveTipVelocity,
                acrossVelocity,
                contactRadius,
                maximumTime,
                BarrierContactKind.PositiveTip,
                tolerance,
                ref contactTime,
                ref contactKind);

            return !float.IsPositiveInfinity(contactTime);
        }

        private static void ConsiderBodyContact(
            float along,
            float across,
            float alongVelocity,
            float acrossVelocity,
            float originAlong,
            float originAcross,
            BarrierState barrier,
            float radius,
            float maximumTime,
            GeometryTolerancePolicy tolerance,
            ref float earliest,
            ref BarrierContactKind kind)
        {
            if (tolerance.IsDistanceApproximatelyEqual(acrossVelocity, 0f))
            {
                if (Math.Abs(across - originAcross) <= radius)
                {
                    ConsiderBodyTime(
                        0f,
                        along,
                        alongVelocity,
                        originAlong,
                        barrier,
                        maximumTime,
                        tolerance,
                        ref earliest,
                        ref kind);
                }

                return;
            }

            ConsiderBodyTime(
                (originAcross - radius - across) / acrossVelocity,
                along,
                alongVelocity,
                originAlong,
                barrier,
                maximumTime,
                tolerance,
                ref earliest,
                ref kind);
            ConsiderBodyTime(
                (originAcross + radius - across) / acrossVelocity,
                along,
                alongVelocity,
                originAlong,
                barrier,
                maximumTime,
                tolerance,
                ref earliest,
                ref kind);
        }

        private static void ConsiderBodyTime(
            float time,
            float along,
            float alongVelocity,
            float originAlong,
            BarrierState barrier,
            float maximumTime,
            GeometryTolerancePolicy tolerance,
            ref float earliest,
            ref BarrierContactKind kind)
        {
            if (!IsInInterval(time, maximumTime, tolerance))
            {
                return;
            }

            time = Math.Max(0f, time);
            float negativeLength = Math.Min(
                barrier.NegativeTargetLength,
                barrier.NegativeLength + barrier.GrowthSpeed * time);
            float positiveLength = Math.Min(
                barrier.PositiveTargetLength,
                barrier.PositiveLength + barrier.GrowthSpeed * time);
            float atTime = along + alongVelocity * time;
            if (tolerance.IsGreaterThanOrApproximatelyEqualDistance(
                    atTime,
                    originAlong - negativeLength)
                && tolerance.IsLessThanOrApproximatelyEqualDistance(
                    atTime,
                    originAlong + positiveLength))
            {
                SelectEarlier(
                    time,
                    BarrierContactKind.Body,
                    tolerance,
                    ref earliest,
                    ref kind);
            }
        }

        private static void ConsiderTipContact(
            float relativeAlong,
            float relativeAcross,
            float relativeAlongVelocity,
            float relativeAcrossVelocity,
            float radius,
            float maximumTime,
            BarrierContactKind candidateKind,
            GeometryTolerancePolicy tolerance,
            ref float earliest,
            ref BarrierContactKind kind)
        {
            double c =
                (double)relativeAlong * relativeAlong
                + (double)relativeAcross * relativeAcross
                - (double)radius * radius;
            if (c <= 0d)
            {
                SelectEarlier(0f, candidateKind, tolerance, ref earliest, ref kind);
                return;
            }

            double a =
                (double)relativeAlongVelocity * relativeAlongVelocity
                + (double)relativeAcrossVelocity * relativeAcrossVelocity;
            if (a == 0d)
            {
                return;
            }

            double b = 2d * (
                (double)relativeAlong * relativeAlongVelocity
                + (double)relativeAcross * relativeAcrossVelocity);
            double discriminant = b * b - 4d * a * c;
            if (discriminant < 0d)
            {
                return;
            }

            double root = (-b - Math.Sqrt(discriminant)) / (2d * a);
            float time = (float)root;
            if (IsInInterval(time, maximumTime, tolerance))
            {
                SelectEarlier(
                    Math.Max(0f, time),
                    candidateKind,
                    tolerance,
                    ref earliest,
                    ref kind);
            }
        }

        private static void SelectEarlier(
            float time,
            BarrierContactKind candidateKind,
            GeometryTolerancePolicy tolerance,
            ref float earliest,
            ref BarrierContactKind kind)
        {
            if (time < earliest
                && !tolerance.IsTimeApproximatelyEqual(time, earliest))
            {
                earliest = time;
                kind = candidateKind;
            }
            else if (float.IsPositiveInfinity(earliest))
            {
                earliest = time;
                kind = candidateKind;
            }
        }

        private static bool IsInInterval(
            float time,
            float maximumTime,
            GeometryTolerancePolicy tolerance) =>
            time >= 0f
            || tolerance.IsTimeApproximatelyEqual(time, 0f)
                ? time <= maximumTime
                    || tolerance.IsTimeApproximatelyEqual(time, maximumTime)
                : false;

        private static float TimeToCompletion(
            float length,
            float target,
            float growthSpeed,
            GeometryTolerancePolicy tolerance)
        {
            if (tolerance.IsDistanceApproximatelyEqual(length, target))
            {
                return float.PositiveInfinity;
            }

            return Math.Max(0f, (target - length) / growthSpeed);
        }

        private static float TimeToBoundary(
            float position,
            float velocity,
            float minimum,
            float maximum,
            GeometryTolerancePolicy tolerance)
        {
            if (tolerance.IsDistanceApproximatelyEqual(velocity, 0f))
            {
                return float.PositiveInfinity;
            }

            float time = velocity > 0f
                ? (maximum - position) / velocity
                : (minimum - position) / velocity;
            return time < 0f
                && tolerance.IsTimeApproximatelyEqual(time, 0f)
                    ? 0f
                    : time;
        }

        private static BarrierSimulationDiagnostic ToDiagnostic(
            ThreatMotionDiagnostic diagnostic) =>
            diagnostic == ThreatMotionDiagnostic.ImpactLimitReached
                ? BarrierSimulationDiagnostic.ThreatImpactLimitReached
                : BarrierSimulationDiagnostic.None;

        private static float Clamp(float value, float minimum, float maximum) =>
            Math.Max(minimum, Math.Min(maximum, value));

        private static void Validate(
            RoomState room,
            ThreatState threat,
            BarrierState barrier,
            float elapsedTime,
            int maximumIterations,
            int maximumThreatImpacts)
        {
            if (room.Id != threat.RoomId || room.Id != barrier.ParentRoomId)
            {
                throw new ArgumentException(
                    "Threat and barrier must belong to the supplied room.");
            }

            if (float.IsNaN(elapsedTime)
                || float.IsInfinity(elapsedTime)
                || elapsedTime < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(elapsedTime));
            }

            if (maximumIterations <= 0 || maximumThreatImpacts <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maximumIterations));
            }
        }
    }
}
