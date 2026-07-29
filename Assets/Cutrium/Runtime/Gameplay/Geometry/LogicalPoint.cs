using System;

namespace Cutrium.Gameplay.Geometry
{
    public readonly struct LogicalPoint : IEquatable<LogicalPoint>
    {
        public LogicalPoint(float x, float y)
        {
            EnsureFinite(x, nameof(x));
            EnsureFinite(y, nameof(y));

            X = x;
            Y = y;
        }

        public float X { get; }

        public float Y { get; }

        public static LogicalPoint operator +(LogicalPoint point, LogicalVector vector)
        {
            return new LogicalPoint(point.X + vector.X, point.Y + vector.Y);
        }

        public static LogicalPoint operator -(LogicalPoint point, LogicalVector vector)
        {
            return new LogicalPoint(point.X - vector.X, point.Y - vector.Y);
        }

        public static LogicalVector operator -(LogicalPoint left, LogicalPoint right)
        {
            return new LogicalVector(left.X - right.X, left.Y - right.Y);
        }

        public bool Equals(LogicalPoint other)
        {
            return X.Equals(other.X) && Y.Equals(other.Y);
        }

        public override bool Equals(object obj)
        {
            return obj is LogicalPoint other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (X.GetHashCode() * 397) ^ Y.GetHashCode();
            }
        }

        public override string ToString()
        {
            return $"({X}, {Y})";
        }

        public static bool operator ==(LogicalPoint left, LogicalPoint right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(LogicalPoint left, LogicalPoint right)
        {
            return !left.Equals(right);
        }

        private static void EnsureFinite(float value, string parameterName)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
            {
                throw new ArgumentOutOfRangeException(parameterName, value, "Logical coordinates must be finite.");
            }
        }
    }
}
