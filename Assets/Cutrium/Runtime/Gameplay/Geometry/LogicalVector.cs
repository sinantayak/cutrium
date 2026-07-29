using System;

namespace Cutrium.Gameplay.Geometry
{
    public readonly struct LogicalVector : IEquatable<LogicalVector>
    {
        public LogicalVector(float x, float y)
        {
            EnsureFinite(x, nameof(x));
            EnsureFinite(y, nameof(y));

            X = x;
            Y = y;
        }

        public float X { get; }

        public float Y { get; }

        public float LengthSquared => (X * X) + (Y * Y);

        public float Length => MathF.Sqrt(LengthSquared);

        public static LogicalVector Zero => new LogicalVector(0f, 0f);

        public static float Dot(LogicalVector left, LogicalVector right)
        {
            return (left.X * right.X) + (left.Y * right.Y);
        }

        public static LogicalVector operator +(LogicalVector left, LogicalVector right)
        {
            return new LogicalVector(left.X + right.X, left.Y + right.Y);
        }

        public static LogicalVector operator -(LogicalVector left, LogicalVector right)
        {
            return new LogicalVector(left.X - right.X, left.Y - right.Y);
        }

        public static LogicalVector operator -(LogicalVector vector)
        {
            return new LogicalVector(-vector.X, -vector.Y);
        }

        public static LogicalVector operator *(LogicalVector vector, float scalar)
        {
            EnsureFinite(scalar, nameof(scalar));
            return new LogicalVector(vector.X * scalar, vector.Y * scalar);
        }

        public static LogicalVector operator *(float scalar, LogicalVector vector)
        {
            return vector * scalar;
        }

        public static LogicalVector operator /(LogicalVector vector, float scalar)
        {
            EnsureFinite(scalar, nameof(scalar));
            if (scalar == 0f)
            {
                throw new DivideByZeroException("A logical vector cannot be divided by zero.");
            }

            return new LogicalVector(vector.X / scalar, vector.Y / scalar);
        }

        public bool Equals(LogicalVector other)
        {
            return X.Equals(other.X) && Y.Equals(other.Y);
        }

        public override bool Equals(object obj)
        {
            return obj is LogicalVector other && Equals(other);
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

        public static bool operator ==(LogicalVector left, LogicalVector right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(LogicalVector left, LogicalVector right)
        {
            return !left.Equals(right);
        }

        private static void EnsureFinite(float value, string parameterName)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
            {
                throw new ArgumentOutOfRangeException(parameterName, value, "Logical vector values must be finite.");
            }
        }
    }
}
