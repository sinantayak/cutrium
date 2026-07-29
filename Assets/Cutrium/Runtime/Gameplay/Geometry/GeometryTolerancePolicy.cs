using System;

namespace Cutrium.Gameplay.Geometry
{
    public readonly struct GeometryTolerancePolicy : IEquatable<GeometryTolerancePolicy>
    {
        public GeometryTolerancePolicy(
            float distanceTolerance,
            float timeTolerance,
            float cornerTolerance,
            float areaTolerance)
        {
            ValidateTolerance(distanceTolerance, nameof(distanceTolerance));
            ValidateTolerance(timeTolerance, nameof(timeTolerance));
            ValidateTolerance(cornerTolerance, nameof(cornerTolerance));
            ValidateTolerance(areaTolerance, nameof(areaTolerance));

            DistanceTolerance = distanceTolerance;
            TimeTolerance = timeTolerance;
            CornerTolerance = cornerTolerance;
            AreaTolerance = areaTolerance;
        }

        public float DistanceTolerance { get; }

        public float TimeTolerance { get; }

        public float CornerTolerance { get; }

        public float AreaTolerance { get; }

        public bool IsDistanceApproximatelyEqual(float left, float right)
        {
            return IsApproximatelyEqual(left, right, DistanceTolerance);
        }

        public bool IsTimeApproximatelyEqual(float left, float right)
        {
            return IsApproximatelyEqual(left, right, TimeTolerance);
        }

        public bool IsCornerTimeTie(float leftImpactTime, float rightImpactTime)
        {
            return IsApproximatelyEqual(leftImpactTime, rightImpactTime, CornerTolerance);
        }

        public bool IsAreaApproximatelyEqual(float left, float right)
        {
            return IsApproximatelyEqual(left, right, AreaTolerance);
        }

        public bool AreApproximatelyEqual(LogicalPoint left, LogicalPoint right)
        {
            return IsDistanceApproximatelyEqual(left.X, right.X) &&
                   IsDistanceApproximatelyEqual(left.Y, right.Y);
        }

        public bool AreApproximatelyEqual(LogicalVector left, LogicalVector right)
        {
            return IsDistanceApproximatelyEqual(left.X, right.X) &&
                   IsDistanceApproximatelyEqual(left.Y, right.Y);
        }

        public bool AreApproximatelyEqual(LogicalRect left, LogicalRect right)
        {
            return IsDistanceApproximatelyEqual(left.MinX, right.MinX) &&
                   IsDistanceApproximatelyEqual(left.MinY, right.MinY) &&
                   IsDistanceApproximatelyEqual(left.MaxX, right.MaxX) &&
                   IsDistanceApproximatelyEqual(left.MaxY, right.MaxY);
        }

        public bool IsLessThanOrApproximatelyEqualDistance(float value, float boundary)
        {
            return value < boundary || IsDistanceApproximatelyEqual(value, boundary);
        }

        public bool IsGreaterThanOrApproximatelyEqualDistance(float value, float boundary)
        {
            return value > boundary || IsDistanceApproximatelyEqual(value, boundary);
        }

        public bool Contains(LogicalRect rectangle, LogicalPoint point)
        {
            return IsGreaterThanOrApproximatelyEqualDistance(point.X, rectangle.MinX) &&
                   IsLessThanOrApproximatelyEqualDistance(point.X, rectangle.MaxX) &&
                   IsGreaterThanOrApproximatelyEqualDistance(point.Y, rectangle.MinY) &&
                   IsLessThanOrApproximatelyEqualDistance(point.Y, rectangle.MaxY);
        }

        public bool Equals(GeometryTolerancePolicy other)
        {
            return DistanceTolerance.Equals(other.DistanceTolerance) &&
                   TimeTolerance.Equals(other.TimeTolerance) &&
                   CornerTolerance.Equals(other.CornerTolerance) &&
                   AreaTolerance.Equals(other.AreaTolerance);
        }

        public override bool Equals(object obj)
        {
            return obj is GeometryTolerancePolicy other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = DistanceTolerance.GetHashCode();
                hashCode = (hashCode * 397) ^ TimeTolerance.GetHashCode();
                hashCode = (hashCode * 397) ^ CornerTolerance.GetHashCode();
                hashCode = (hashCode * 397) ^ AreaTolerance.GetHashCode();
                return hashCode;
            }
        }

        public static bool operator ==(GeometryTolerancePolicy left, GeometryTolerancePolicy right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(GeometryTolerancePolicy left, GeometryTolerancePolicy right)
        {
            return !left.Equals(right);
        }

        private static bool IsApproximatelyEqual(float left, float right, float tolerance)
        {
            if (float.IsNaN(left) || float.IsNaN(right))
            {
                return false;
            }

            if (left.Equals(right))
            {
                return true;
            }

            if (float.IsInfinity(left) || float.IsInfinity(right))
            {
                return false;
            }

            return Math.Abs(left - right) <= tolerance;
        }

        private static void ValidateTolerance(float tolerance, string parameterName)
        {
            if (float.IsNaN(tolerance) || float.IsInfinity(tolerance) || tolerance < 0f)
            {
                throw new ArgumentOutOfRangeException(
                    parameterName,
                    tolerance,
                    "Geometry tolerances must be finite and non-negative.");
            }
        }
    }
}
