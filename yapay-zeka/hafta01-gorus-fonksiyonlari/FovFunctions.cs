using System;

/// <summary>
/// Week 1 homework: field-of-view and left/right tests.
///
/// Rules from the assignment: no square root and no acos inside these functions.
/// Distances are compared as squares (d^2 &lt;= range^2) and the angle is compared through
/// the cosine of the half angle, without normalizing any vector.
/// </summary>
public static class FovFunctions
{
    /// <summary>
    /// Relative tolerance. Targets that lie exactly on the edge of the cone or exactly at the
    /// maximum range count as inside, even when floating-point round-off moves them by ~1e-16.
    /// </summary>
    const double Epsilon = 1e-9;

    /// <summary>Collinearity tolerance for SideOf: sin^2(angle) below this counts as "same line" (~0.00006 degrees).</summary>
    const double CollinearEpsilon = 1e-12;

    /// <summary>
    /// True if <paramref name="target"/> is within <paramref name="range"/> of the agent and inside the
    /// view cone of total angle <paramref name="fovDeg"/> centred on <paramref name="heading"/>.
    /// The heading does not have to be a unit vector. Boundaries (exact half angle, exact range) are inclusive.
    /// </summary>
    public static bool IsInFov(Vector2D agent, Vector2D heading, Vector2D target, double fovDeg, double range)
    {
        // Math.Cos is allowed; only sqrt and acos are forbidden. In a game this value would be
        // computed once per NPC type (see CosHalfAngle) and reused every frame.
        return IsInFov(agent, heading, target, CosHalfAngle(fovDeg), range, fovDeg >= 360.0);
    }

    /// <summary>cos(fov / 2). Precompute this once per agent type.</summary>
    public static double CosHalfAngle(double fovDeg)
    {
        return Math.Cos(fovDeg * 0.5 * Math.PI / 180.0);
    }

    /// <summary>Same test with a precomputed cos(fov / 2).</summary>
    public static bool IsInFov(Vector2D agent, Vector2D heading, Vector2D target,
                               double cosHalf, double range, bool fullCircle)
    {
        Vector2D toTarget = target - agent;

        // 1) Range: compare squared distance with squared range (no sqrt).
        double distSq = toTarget.LengthSq();
        double rangeSq = range * range;
        if (distSq > rangeSq * (1.0 + Epsilon)) return false;

        // A target standing on the agent is trivially visible.
        if (distSq < 1e-18) return true;
        if (fullCircle) return true;

        // An agent without a facing direction cannot have a view cone.
        double headingSq = heading.LengthSq();
        if (headingSq < 1e-18) return false;

        // 2) Angle. We need   dot / (|h| |d|) >= cosHalf   without |h| and |d| (they need sqrt).
        //    f(x) = x * |x| is strictly increasing and f(k * x) = k^2 * f(x) for k > 0, so
        //    the inequality is equivalent to   f(dot) >= f(cosHalf) * |h|^2 * |d|^2.
        //    This single comparison handles every sign case (narrow cones and cones wider than 180 degrees).
        double dot = heading.Dot(toTarget);
        double scale = headingSq * distSq;
        double lhs = dot * Math.Abs(dot);
        double rhs = cosHalf * Math.Abs(cosHalf) * scale;
        return lhs >= rhs - Epsilon * scale;
    }

    /// <summary>
    /// Which side of the agent's heading the target is on (2D math convention, y up):
    /// +1 = left, -1 = right, 0 = on the same line (straight ahead or straight behind).
    /// Note: Unity's left-handed (x, z) plane flips this sign (Vector3.Cross(fwd, dir).y &gt; 0 means right).
    /// </summary>
    public static int SideOf(Vector2D agent, Vector2D heading, Vector2D target)
    {
        Vector2D toTarget = target - agent;
        double cross = heading.Cross(toTarget);   // = |h| |d| sin(theta)

        // Collinear if sin^2(theta) is tiny: cross^2 <= eps * |h|^2 * |d|^2 (still no sqrt).
        if (cross * cross <= CollinearEpsilon * heading.LengthSq() * toTarget.LengthSq()) return 0;
        return cross > 0 ? +1 : -1;
    }
}
