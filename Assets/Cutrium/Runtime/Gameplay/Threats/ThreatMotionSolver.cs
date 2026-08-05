using System;
using Cutrium.Gameplay.Board;
using Cutrium.Gameplay.Geometry;

namespace Cutrium.Gameplay.Threats
{
    public static class ThreatMotionSolver
    {
        public static ThreatMotionResult Move(
            RoomState room,
            ThreatState threat,
            float elapsedTime,
            int maximumImpacts,
            GeometryTolerancePolicy tolerance)
        {
            ValidateInputs(
                room,
                threat,
                elapsedTime,
                maximumImpacts,
                tolerance,
                out float minX,
                out float minY,
                out float maxX,
                out float maxY);

            if (elapsedTime == 0f)
            {
                return new ThreatMotionResult(
                    threat,
                    0,
                    0f,
                    ThreatMotionDiagnostic.None);
            }

            float x = Clamp(threat.Position.X, minX, maxX);
            float y = Clamp(threat.Position.Y, minY, maxY);
            float velocityX = threat.Velocity.X;
            float velocityY = threat.Velocity.Y;
            float remaining = elapsedTime;
            float simulated = 0f;
            int impacts = 0;
            ThreatMotionDiagnostic diagnostic = ThreatMotionDiagnostic.None;

            while (remaining > tolerance.TimeTolerance)
            {
                float xImpactTime = TimeToBoundary(
                    x,
                    velocityX,
                    minX,
                    maxX,
                    tolerance);
                float yImpactTime = TimeToBoundary(
                    y,
                    velocityY,
                    minY,
                    maxY,
                    tolerance);
                float earliestImpact = Math.Min(xImpactTime, yImpactTime);

                if (float.IsPositiveInfinity(earliestImpact)
                    || earliestImpact > remaining
                    && !tolerance.IsTimeApproximatelyEqual(
                        earliestImpact,
                        remaining))
                {
                    x += velocityX * remaining;
                    y += velocityY * remaining;
                    simulated += remaining;
                    remaining = 0f;
                    break;
                }

                if (impacts >= maximumImpacts)
                {
                    diagnostic = ThreatMotionDiagnostic.ImpactLimitReached;
                    break;
                }

                float step = Clamp(earliestImpact, 0f, remaining);
                x += velocityX * step;
                y += velocityY * step;
                remaining -= step;
                simulated += step;

                bool hitX = tolerance.IsCornerTimeTie(
                    xImpactTime,
                    earliestImpact);
                bool hitY = tolerance.IsCornerTimeTie(
                    yImpactTime,
                    earliestImpact);

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

                impacts++;
            }

            x = Clamp(x, minX, maxX);
            y = Clamp(y, minY, maxY);
            ThreatState moved = threat.WithMotion(
                new LogicalPoint(x, y),
                new LogicalVector(velocityX, velocityY));
            return new ThreatMotionResult(
                moved,
                impacts,
                simulated,
                diagnostic);
        }

        private static void ValidateInputs(
            RoomState room,
            ThreatState threat,
            float elapsedTime,
            int maximumImpacts,
            GeometryTolerancePolicy tolerance,
            out float minX,
            out float minY,
            out float maxX,
            out float maxY)
        {
            if (room.Id != threat.RoomId)
            {
                throw new ArgumentException(
                    "The threat must belong to the supplied room.",
                    nameof(threat));
            }

            if (!IsFinite(elapsedTime) || elapsedTime < 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(elapsedTime),
                    elapsedTime,
                    "Elapsed time must be finite and non-negative.");
            }

            if (maximumImpacts <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(maximumImpacts),
                    maximumImpacts,
                    "The impact limit must be positive.");
            }

            minX = room.Bounds.MinX + threat.Radius;
            minY = room.Bounds.MinY + threat.Radius;
            maxX = room.Bounds.MaxX - threat.Radius;
            maxY = room.Bounds.MaxY - threat.Radius;
            if (minX > maxX || minY > maxY)
            {
                throw new ArgumentException(
                    "The threat diameter must fit inside its room.",
                    nameof(threat));
            }

            LogicalPoint position = threat.Position;
            if (!tolerance.IsGreaterThanOrApproximatelyEqualDistance(
                    position.X,
                    minX)
                || !tolerance.IsLessThanOrApproximatelyEqualDistance(
                    position.X,
                    maxX)
                || !tolerance.IsGreaterThanOrApproximatelyEqualDistance(
                    position.Y,
                    minY)
                || !tolerance.IsLessThanOrApproximatelyEqualDistance(
                    position.Y,
                    maxY))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(threat),
                    threat,
                    "The threat circle must begin fully inside its room.");
            }
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
            if (time < 0f && tolerance.IsTimeApproximatelyEqual(time, 0f))
            {
                return 0f;
            }

            return time;
        }

        private static float Clamp(float value, float minimum, float maximum) =>
            Math.Max(minimum, Math.Min(maximum, value));

        private static bool IsFinite(float value) =>
            !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
