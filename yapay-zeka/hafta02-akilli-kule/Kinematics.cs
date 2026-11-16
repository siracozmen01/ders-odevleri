using System;

/// <summary>
/// Constant-acceleration motion along a line (Buckland's SoccerBall helpers).
/// u: initial speed, a: constant acceleration (negative = friction), t: time, x: distance.
/// </summary>
public static class Kinematics
{
    /// <summary>
    /// Distance travelled after <paramref name="t"/> seconds: x = u*t + 1/2*a*t^2.
    /// If the acceleration slows the ball down (u and a have opposite signs), the formula is only
    /// valid until the ball stops; after that the ball stays at its stopping point.
    /// </summary>
    public static double FuturePosition(double u, double a, double t)
    {
        if (u * a < 0)
        {
            double tStop = -u / a;
            if (t > tStop) t = tStop;
        }
        return u * t + 0.5 * a * t * t;
    }

    /// <summary>
    /// Time needed to cover <paramref name="distance"/>, from v^2 = u^2 + 2*a*x and t = (v - u) / a.
    /// Returns -1 if the distance is never reached (v^2 &lt; 0: the ball stops first).
    /// </summary>
    public static double TimeToCoverDistance(double u, double a, double distance)
    {
        if (distance <= 0) return 0;
        if (Math.Abs(a) < 1e-12)                       // no acceleration: x = u * t
            return u > 0 ? distance / u : -1;

        double vSq = u * u + 2 * a * distance;
        if (vSq < 0) return -1;                        // cannot get that far
        double v = Math.Sqrt(vSq);
        double t = (v - u) / a;
        return t >= 0 ? t : -1;
    }

    /// <summary>Time until the speed reaches zero: 0 = u + a*t. Returns -1 if it never stops.</summary>
    public static double TimeToStop(double u, double a)
    {
        if (u * a >= 0) return u == 0 ? 0 : -1;
        return -u / a;
    }

    /// <summary>Distance travelled until it stops: 0 = u^2 + 2*a*x. Returns -1 if it never stops.</summary>
    public static double StoppingDistance(double u, double a)
    {
        if (u * a >= 0) return u == 0 ? 0 : -1;
        return -u * u / (2 * a);
    }
}
