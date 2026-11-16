using System;
using System.Collections.Generic;

/// <summary>
/// Console test cases for IsInFov and SideOf (PASS/FAIL), including boundary angles,
/// plus a randomized cross-check against a straightforward sqrt + acos reference.
/// </summary>
public static class FovTests
{
    static int passed, failed;

    public static int Run()
    {
        passed = failed = 0;
        Vector2D o = Vector2D.Zero;
        Vector2D up = new Vector2D(0, 1);

        Console.WriteLine("=== IsInFov(agent, heading, target, fovDeg, range) ===");
        Console.WriteLine(Row("#", "Case", "agent", "heading", "target", "fov", "range", "expected", "actual", "result"));
        Fov(1, "Slide 20: P1 in front", o, up, new Vector2D(1, 3), 90, 10, true);
        Fov(2, "Slide 20: P2 too far to the side", o, up, new Vector2D(4, 1), 90, 10, false);
        Fov(3, "Boundary: exactly 45 deg (half of 90)", o, up, new Vector2D(3, 3), 90, 10, true);
        Fov(4, "Just inside: 44.9 deg", o, up, Polar(o, 44.9, 5), 90, 10, true);
        Fov(5, "Just outside: 45.1 deg", o, up, Polar(o, 45.1, 5), 90, 10, false);
        Fov(6, "Directly behind (180 deg)", o, up, new Vector2D(0, -5), 90, 10, false);
        Fov(7, "Boundary: exactly at range", o, up, new Vector2D(0, 10), 90, 10, true);
        Fov(8, "Just beyond range", o, up, new Vector2D(0, 10.001), 90, 10, false);
        Fov(9, "Non-unit heading (0, 7), same as #1", o, new Vector2D(0, 7), new Vector2D(1, 3), 90, 10, true);
        Fov(10, "Agent not at origin, ~14 deg", new Vector2D(5, 5), new Vector2D(1, 0), new Vector2D(9, 6), 60, 8, true);
        Fov(11, "Narrow 10 deg cone, target at 4 deg", o, up, Polar(o, 4, 6), 10, 10, true);
        Fov(12, "Narrow 10 deg cone, target at 6 deg", o, up, Polar(o, 6, 6), 10, 10, false);
        Fov(13, "Boundary: 180 deg cone, exactly sideways", o, up, new Vector2D(5, 0), 180, 10, true);
        Fov(14, "180 deg cone, slightly behind the side", o, up, new Vector2D(5, -0.01), 180, 10, false);
        Fov(15, "Wide 270 deg cone, target at 120 deg", o, up, Polar(o, 120, 5), 270, 10, true);
        Fov(16, "Boundary: 270 deg cone, exactly 135 deg", o, up, new Vector2D(-4, -4), 270, 10, true);
        Fov(17, "Wide 270 deg cone, target directly behind", o, up, new Vector2D(0, -5), 270, 10, false);
        Fov(18, "Full 360 deg cone, target behind", o, up, new Vector2D(0, -5), 360, 10, true);
        Fov(19, "Target on top of the agent", o, up, o, 90, 10, true);
        Fov(20, "Zero heading (no facing direction)", o, Vector2D.Zero, new Vector2D(0, 5), 90, 10, false);

        Console.WriteLine();
        Console.WriteLine("=== SideOf(agent, heading, target): +1 left, -1 right, 0 collinear ===");
        Console.WriteLine(Row("#", "Case", "agent", "heading", "target", "", "", "expected", "actual", "result"));
        Side(1, "Slide 21: (1, 3) is right of (0, 1)", o, up, new Vector2D(1, 3), -1);
        Side(2, "Lab Q4: (-3, 1) is left of (0, 1)", o, up, new Vector2D(-3, 1), +1);
        Side(3, "Straight ahead", o, up, new Vector2D(0, 5), 0);
        Side(4, "Straight behind", o, up, new Vector2D(0, -5), 0);
        Side(5, "Diagonal, collinear, agent moved", new Vector2D(2, 2), new Vector2D(1, 1), new Vector2D(5, 5), 0);
        Side(6, "Diagonal heading, target left", new Vector2D(2, 2), new Vector2D(1, 1), new Vector2D(3, 5), +1);
        Side(7, "Diagonal heading, target right", new Vector2D(2, 2), new Vector2D(1, 1), new Vector2D(5, 3), -1);
        Side(8, "Heading +x, target (2, 2) is left", o, new Vector2D(1, 0), new Vector2D(2, 2), +1);
        Side(9, "Long heading, target barely left", o, new Vector2D(0, 10), new Vector2D(-0.001, 100), +1);

        Console.WriteLine();
        RandomCrossCheck(100000);

        Console.WriteLine();
        Console.WriteLine($"Summary: {passed} passed, {failed} failed");
        return failed;
    }

    // ---------------------------------------------------------------- helpers

    static void Fov(int id, string name, Vector2D agent, Vector2D heading, Vector2D target,
                    double fov, double range, bool expected)
    {
        bool actual = FovFunctions.IsInFov(agent, heading, target, fov, range);
        Report(Row(id.ToString(), name, agent.ToString(), heading.ToString(), target.ToString(),
                   fov.ToString(), range.ToString(), expected.ToString(), actual.ToString(), ""), actual == expected);
    }

    static void Side(int id, string name, Vector2D agent, Vector2D heading, Vector2D target, int expected)
    {
        int actual = FovFunctions.SideOf(agent, heading, target);
        Report(Row(id.ToString(), name, agent.ToString(), heading.ToString(), target.ToString(),
                   "", "", expected.ToString("+0;-0;0"), actual.ToString("+0;-0;0"), ""), actual == expected);
    }

    static void Report(string row, bool ok)
    {
        if (ok) passed++; else failed++;
        Console.WriteLine(row + (ok ? "PASS" : "FAIL"));
    }

    static string Row(string id, string name, string agent, string heading, string target,
                      string fov, string range, string expected, string actual, string result)
        => $"{id,-3}{name,-44}{agent,-9}{heading,-9}{target,-17}{fov,-5}{range,-6}{expected,-9}{actual,-8}{result}";

    /// <summary>Point at <paramref name="dist"/> from the agent, <paramref name="deg"/> degrees clockwise from +y (test helper only).</summary>
    static Vector2D Polar(Vector2D agent, double deg, double dist)
    {
        double r = deg * Math.PI / 180.0;
        return agent + new Vector2D(Math.Sin(r), Math.Cos(r)) * dist;
    }

    /// <summary>
    /// Compares the sqrt/acos-free IsInFov with a naive reference that uses Length() and Math.Acos.
    /// Cases within 1e-6 of a boundary are skipped because the two can legitimately differ by round-off there.
    /// </summary>
    static void RandomCrossCheck(int n)
    {
        var rng = new Random(12345);
        int mismatches = 0, checkedCases = 0, inside = 0;
        for (int i = 0; i < n; i++)
        {
            var agent = new Vector2D(rng.NextDouble() * 20 - 10, rng.NextDouble() * 20 - 10);
            var heading = new Vector2D(rng.NextDouble() * 4 - 2, rng.NextDouble() * 4 - 2);
            var target = new Vector2D(rng.NextDouble() * 40 - 20, rng.NextDouble() * 40 - 20);
            double fov = rng.NextDouble() * 359 + 0.5;
            double range = rng.NextDouble() * 25;

            Vector2D d = target - agent;
            if (heading.Length() < 1e-6 || d.Length() < 1e-6) continue;
            double angle = Math.Acos(Math.Max(-1, Math.Min(1, heading.Normalized().Dot(d.Normalized())))) * 180 / Math.PI;
            if (Math.Abs(angle - fov / 2) < 1e-6 || Math.Abs(d.Length() - range) < 1e-6) continue;

            bool reference = d.Length() <= range && angle <= fov / 2;
            bool fast = FovFunctions.IsInFov(agent, heading, target, fov, range);
            checkedCases++;
            if (reference) inside++;
            if (reference != fast) mismatches++;
        }
        bool ok = mismatches == 0;
        Report($"Randomized cross-check vs sqrt+acos reference: {checkedCases} cases ({inside} visible), {mismatches} mismatches  ", ok);
    }
}
