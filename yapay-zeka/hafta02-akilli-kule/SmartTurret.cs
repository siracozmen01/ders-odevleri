using System;

/// <summary>
/// Week 2 homework "Akilli kule" (smart turret): exact lead aiming + ballistic firing angles.
/// The method names NisanAl and AtisAcisi and their signatures follow the assignment (slide 22).
/// Both report failure with false, so the AI can decide to move closer or pick another target.
/// </summary>
public static class SmartTurret
{
    /// <summary>Gravity magnitude (Unity's Physics.gravity is (0, -9.81, 0)).</summary>
    public const double G = 9.81;

    /// <summary>
    /// Exact lead aim. Solves |P + V*t| = s*t for the earliest t &gt; 0 and returns the meeting point.
    /// Returns false (nisan = current target position, sure = -1) if the projectile can never reach the target.
    /// </summary>
    public static bool NisanAl(Vector2D kule, Vector2D hedef, Vector2D hedefHiz, double mermiSurati,
                               out Vector2D nisan, out double sure)
    {
        double t = Interception.InterceptTime(hedef - kule, hedefHiz, mermiSurati);
        if (t < 0)
        {
            nisan = hedef;
            sure = -1;
            return false;
        }
        nisan = hedef + hedefHiz * t;
        sure = t;
        return true;
    }

    /// <summary>
    /// Buckland's approximate lead (t ~ |P| / (s + |V|)), kept for comparison. It always "succeeds",
    /// even when the target can never be hit; that is one of its weaknesses.
    /// </summary>
    public static void ApproximateAim(Vector2D kule, Vector2D hedef, Vector2D hedefHiz, double mermiSurati,
                                      out Vector2D nisan, out double sure)
    {
        sure = Interception.ApproximateInterceptTime(hedef - kule, hedefHiz, mermiSurati);
        nisan = hedef + hedefHiz * sure;
    }

    /// <summary>The term under the square root: v^4 - g (g x^2 + 2 y v^2). &gt; 0 two angles, = 0 one, &lt; 0 out of range.</summary>
    public static double RootTerm(double v, double x, double y)
    {
        double v2 = v * v;
        return v2 * v2 - G * (G * x * x + 2 * y * v2);
    }

    /// <summary>
    /// Elevation angles (degrees) that hit a point at horizontal distance <paramref name="x"/> and height
    /// difference <paramref name="y"/> with launch speed <paramref name="v"/>:
    /// tan(theta) = (v^2 -/+ sqrt(v^4 - g (g x^2 + 2 y v^2))) / (g x).
    /// Returns false (both angles NaN) when the target is out of range.
    /// </summary>
    public static bool AtisAcisi(double v, double x, double y, out double alcak, out double yuksek)
    {
        alcak = yuksek = double.NaN;
        if (v <= 0) return false;

        if (x < 1e-9)                                  // target straight above/below: shoot vertically
        {
            if (y > v * v / (2 * G)) return false;     // higher than the apex
            alcak = yuksek = 90.0;
            return true;
        }

        double root = RootTerm(v, x, y);
        double v4 = v * v * v * v;
        if (root < -1e-12 * v4) return false;          // out of range: no angle reaches the target
        if (root < 0) root = 0;                        // exactly at max range (lost to round-off): one angle

        double sq = Math.Sqrt(root);
        double v2 = v * v;
        alcak = Math.Atan((v2 - sq) / (G * x)) * 180.0 / Math.PI;
        yuksek = Math.Atan((v2 + sq) / (G * x)) * 180.0 / Math.PI;
        return true;
    }

    /// <summary>Flight time until the projectile has covered horizontal distance x: t = x / (v cos(theta)).</summary>
    public static double FlightTime(double v, double x, double angleDeg)
    {
        return x / (v * Math.Cos(angleDeg * Math.PI / 180.0));
    }

    /// <summary>
    /// Bonus (3D): launch velocity vector from a muzzle to a target with speed v (Unity axes, y up).
    ///  1) split d = target - muzzle into horizontal distance x = |(d.x, d.z)| and height y = d.y,
    ///  2) yaw = atan2(d.x, d.z), pitch = ballistic angle,
    ///  3) velocity = v * (cos(pitch) * unit(d.x, 0, d.z) + sin(pitch) * (0, 1, 0)).
    /// Use it with Rigidbody.AddForce(velocity, ForceMode.VelocityChange).
    /// </summary>
    public static bool LaunchVelocity(Vector3D muzzle, Vector3D target, double v, bool highArc,
                                      out Vector3D velocity, out double yawDeg, out double pitchDeg)
    {
        Vector3D d = target - muzzle;
        Vector3D horizontal = new Vector3D(d.X, 0, d.Z);
        double x = horizontal.Length();
        double y = d.Y;
        yawDeg = Math.Atan2(d.X, d.Z) * 180.0 / Math.PI;

        if (!AtisAcisi(v, x, y, out double low, out double high))
        {
            velocity = Vector3D.Zero;
            pitchDeg = double.NaN;
            return false;
        }

        pitchDeg = highArc ? high : low;
        double p = pitchDeg * Math.PI / 180.0;
        velocity = (horizontal.Normalized() * Math.Cos(p) + Vector3D.Up * Math.Sin(p)) * v;
        return true;
    }

    /// <summary>
    /// Moving target + ballistic shot (slide 18): the straight-line intercept time is too short for a
    /// shell that flies an arc, so re-predict the target with the real flight time a few times.
    /// Returns the aim point after <paramref name="iterations"/> rounds (false if out of range or unreachable).
    /// <paramref name="log"/> receives (iteration, time used, aim point, low angle, flight time) per round.
    /// </summary>
    public static bool BallisticLeadAim(Vector2D kule, Vector2D hedef, Vector2D hedefHiz, double v, int iterations,
                                        Action<int, double, Vector2D, double, double> log,
                                        out Vector2D nisan, out double alcak, out double ucus)
    {
        alcak = ucus = double.NaN;
        if (!NisanAl(kule, hedef, hedefHiz, v, out nisan, out double t)) return false;

        for (int i = 0; i <= iterations; i++)
        {
            nisan = hedef + hedefHiz * t;
            double x = (nisan - kule).Length();
            if (!AtisAcisi(v, x, 0, out alcak, out _)) return false;
            ucus = FlightTime(v, x, alcak);
            log?.Invoke(i, t, nisan, alcak, ucus);
            t = ucus;                                  // next round: predict with the real flight time
        }
        return true;
    }
}
