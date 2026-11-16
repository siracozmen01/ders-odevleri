using System;
using System.Globalization;

/// <summary>
/// Minimal 3D vector for the bonus task (3D ballistics). Same style as Vector2D.
/// Axes follow Unity: y is up, the ground plane is (x, z).
/// </summary>
public struct Vector3D
{
    public double X, Y, Z;

    public Vector3D(double x, double y, double z) { X = x; Y = y; Z = z; }

    public static Vector3D Zero => new Vector3D(0, 0, 0);
    public static Vector3D Up => new Vector3D(0, 1, 0);

    public double Length() => Math.Sqrt(X * X + Y * Y + Z * Z);
    public double LengthSq() => X * X + Y * Y + Z * Z;
    public double Dot(Vector3D v) => X * v.X + Y * v.Y + Z * v.Z;

    public Vector3D Normalized()
    {
        double l = Length();
        return l > 1e-9 ? this * (1.0 / l) : this;
    }

    public static Vector3D operator +(Vector3D a, Vector3D b) => new Vector3D(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
    public static Vector3D operator -(Vector3D a, Vector3D b) => new Vector3D(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
    public static Vector3D operator -(Vector3D a) => new Vector3D(-a.X, -a.Y, -a.Z);
    public static Vector3D operator *(Vector3D a, double k) => new Vector3D(a.X * k, a.Y * k, a.Z * k);
    public static Vector3D operator *(double k, Vector3D a) => new Vector3D(a.X * k, a.Y * k, a.Z * k);

    public bool ApproximatelyEquals(Vector3D other, double eps = 1e-6)
        => Math.Abs(X - other.X) <= eps && Math.Abs(Y - other.Y) <= eps && Math.Abs(Z - other.Z) <= eps;

    public override string ToString()
        => "(" + X.ToString("0.###", CultureInfo.InvariantCulture) + ", "
               + Y.ToString("0.###", CultureInfo.InvariantCulture) + ", "
               + Z.ToString("0.###", CultureInfo.InvariantCulture) + ")";
}
