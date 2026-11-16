using System;

/// <summary>
/// Predicting where a moving target will be when a straight-flying projectile reaches it.
/// P: target position relative to the shooter, V: target velocity, s: projectile speed.
/// </summary>
public static class Interception
{
    /// <summary>
    /// Exact meeting time: |P + V*t| = s*t  =>  (V.V - s^2) t^2 + 2 (P.V) t + P.P = 0.
    /// Returns the smallest positive root, or -1 if the projectile can never catch the target.
    /// </summary>
    public static double InterceptTime(Vector2D P, Vector2D V, double s)
    {
        double a = V.Dot(V) - s * s;
        double b = 2 * P.Dot(V);
        double c = P.Dot(P);

        if (c < 1e-12) return 0;                       // target is already at the muzzle

        if (Math.Abs(a) < 1e-9)                        // target exactly as fast as the projectile: b*t + c = 0
        {
            if (b >= 0) return -1;                     // equally fast and not approaching: never caught
            return -c / b;
        }

        double disc = b * b - 4 * a * c;
        if (disc < 0) return -1;                       // no real root: the target outruns the projectile

        double sq = Math.Sqrt(disc);
        double t1 = (-b - sq) / (2 * a);
        double t2 = (-b + sq) / (2 * a);
        double tMin = Math.Min(t1, t2), tMax = Math.Max(t1, t2);
        if (tMin > 0) return tMin;
        if (tMax > 0) return tMax;
        return -1;                                     // both roots in the past
    }

    /// <summary>Buckland's cheap estimate (Pursuit): t ~ |P| / (s + |V|). Always returns a value.</summary>
    public static double ApproximateInterceptTime(Vector2D P, Vector2D V, double s)
    {
        return P.Length() / (s + V.Length());
    }

    /// <summary>
    /// How far the target is from the aim point at the moment a projectile fired at speed
    /// <paramref name="s"/> from <paramref name="shooter"/> arrives there. 0 means a hit.
    /// </summary>
    public static double MissDistance(Vector2D shooter, Vector2D target, Vector2D targetVelocity, double s, Vector2D aim)
    {
        double arrival = (aim - shooter).Length() / s;
        Vector2D targetThen = target + targetVelocity * arrival;
        return targetThen.Distance(aim);
    }
}
