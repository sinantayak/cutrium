using System;

namespace Cutrium.Gameplay.Geometry
{
    public readonly struct LogicalRect : IEquatable<LogicalRect>
    {
        public LogicalRect(float minX, float minY, float width, float height)
        {
            EnsureFinite(minX, nameof(minX));
            EnsureFinite(minY, nameof(minY));
            EnsureFinite(width, nameof(width));
            EnsureFinite(height, nameof(height));

            if (width < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(width), width, "Logical rectangle width cannot be negative.");
            }

            if (height < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(height), height, "Logical rectangle height cannot be negative.");
            }

            EnsureFinite(minX + width, nameof(width));
            EnsureFinite(minY + height, nameof(height));

            MinX = minX;
            MinY = minY;
            Width = width;
            Height = height;
        }

        public float MinX { get; }

        public float MinY { get; }

        public float Width { get; }

        public float Height { get; }

        public float MaxX => MinX + Width;

        public float MaxY => MinY + Height;

        public float Area => Width * Height;

        public LogicalPoint Min => new LogicalPoint(MinX, MinY);

        public LogicalPoint Max => new LogicalPoint(MaxX, MaxY);

        public LogicalPoint Center => new LogicalPoint(MinX + (Width * 0.5f), MinY + (Height * 0.5f));

        public LogicalVector Size => new LogicalVector(Width, Height);

        public static LogicalRect FromMinMax(float minX, float minY, float maxX, float maxY)
        {
            EnsureFinite(maxX, nameof(maxX));
            EnsureFinite(maxY, nameof(maxY));

            if (maxX < minX)
            {
                throw new ArgumentOutOfRangeException(nameof(maxX), maxX, "Maximum X cannot be less than minimum X.");
            }

            if (maxY < minY)
            {
                throw new ArgumentOutOfRangeException(nameof(maxY), maxY, "Maximum Y cannot be less than minimum Y.");
            }

            return new LogicalRect(minX, minY, maxX - minX, maxY - minY);
        }

        public bool Contains(LogicalPoint point)
        {
            return point.X >= MinX &&
                   point.X <= MaxX &&
                   point.Y >= MinY &&
                   point.Y <= MaxY;
        }

        public bool Contains(LogicalPoint point, GeometryTolerancePolicy tolerancePolicy)
        {
            return tolerancePolicy.Contains(this, point);
        }

        public bool Equals(LogicalRect other)
        {
            return MinX.Equals(other.MinX) &&
                   MinY.Equals(other.MinY) &&
                   Width.Equals(other.Width) &&
                   Height.Equals(other.Height);
        }

        public override bool Equals(object obj)
        {
            return obj is LogicalRect other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = MinX.GetHashCode();
                hashCode = (hashCode * 397) ^ MinY.GetHashCode();
                hashCode = (hashCode * 397) ^ Width.GetHashCode();
                hashCode = (hashCode * 397) ^ Height.GetHashCode();
                return hashCode;
            }
        }

        public override string ToString()
        {
            return $"({MinX}, {MinY}, {Width}, {Height})";
        }

        public static bool operator ==(LogicalRect left, LogicalRect right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(LogicalRect left, LogicalRect right)
        {
            return !left.Equals(right);
        }

        private static void EnsureFinite(float value, string parameterName)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
            {
                throw new ArgumentOutOfRangeException(parameterName, value, "Logical rectangle values must be finite.");
            }
        }
    }
}
