using System;
using System.Globalization;

/// <summary>
/// Minimal 2D vector (a simplified version of Buckland's Vector2D).
/// Written by hand on purpose: no System.Numerics, no Unity types.
/// Kept to plain C# 9 so the file can be dropped into Unity in week 3.
/// Week 2 additions: Truncate, Distance, DistanceSq.
/// </summary>
public struct Vector2D
{
    public double X, Y;

    public Vector2D(double x, double y) { X = x; Y = y; }

    public static Vector2D Zero => new Vector2D(0, 0);

    /// <summary>|v| = sqrt(x^2 + y^2)</summary>
    public double Length() => Math.Sqrt(X * X + Y * Y);

    /// <summary>|v|^2, cheap: use it whenever you only need to compare lengths.</summary>
    public double LengthSq() => X * X + Y * Y;

    /// <summary>a . b = a.x*b.x + a.y*b.y = |a||b|cos(theta)</summary>
    public double Dot(Vector2D v) => X * v.X + Y * v.Y;

    /// <summary>2D cross product (a scalar): a.x*b.y - a.y*b.x. &gt; 0 means v is to the left of this.</summary>
    public double Cross(Vector2D v) => X * v.Y - Y * v.X;

    /// <summary>Unit vector in the same direction. The zero vector is returned unchanged (no division by zero).</summary>
    public Vector2D Normalized()
    {
        double l = Length();
        return l > 1e-9 ? this * (1.0 / l) : this;
    }

    /// <summary>
    /// Limits the length to <paramref name="max"/> while keeping the direction (Buckland: Truncate).
    /// Example: (6, 8).Truncate(5) = (3, 4).
    /// </summary>
    public Vector2D Truncate(double max)
    {
        if (LengthSq() > max * max) return Normalized() * max;
        return this;
    }

    /// <summary>Distance between two points: |v - this|.</summary>
    public double Distance(Vector2D v) => (v - this).Length();

    /// <summary>Squared distance, no square root: use it for range comparisons.</summary>
    public double DistanceSq(Vector2D v) => (v - this).LengthSq();

    public static Vector2D operator +(Vector2D a, Vector2D b) => new Vector2D(a.X + b.X, a.Y + b.Y);
    public static Vector2D operator -(Vector2D a, Vector2D b) => new Vector2D(a.X - b.X, a.Y - b.Y);
    public static Vector2D operator -(Vector2D a) => new Vector2D(-a.X, -a.Y);
    public static Vector2D operator *(Vector2D a, double k) => new Vector2D(a.X * k, a.Y * k);
    public static Vector2D operator *(double k, Vector2D a) => new Vector2D(a.X * k, a.Y * k);
    public static Vector2D operator /(Vector2D a, double k) => new Vector2D(a.X / k, a.Y / k);

    /// <summary>Component-wise comparison with a tolerance (useful in tests).</summary>
    public bool ApproximatelyEquals(Vector2D other, double eps = 1e-6)
        => Math.Abs(X - other.X) <= eps && Math.Abs(Y - other.Y) <= eps;

    public override string ToString()
        => "(" + X.ToString("0.###", CultureInfo.InvariantCulture) + ", "
               + Y.ToString("0.###", CultureInfo.InvariantCulture) + ")";
}
