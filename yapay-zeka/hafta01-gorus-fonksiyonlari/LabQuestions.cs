using System;

/// <summary>
/// The five lab questions from slide 25 ("solve on paper first, then verify in code").
/// Expected values are the answers given in the speaker notes of the deck.
/// </summary>
public static class LabQuestions
{
    static int failed;

    public static int Run()
    {
        failed = 0;
        Console.WriteLine("=== Lab questions (slide 25) ===");

        // Q1: distance between A (2, 1) and B (8, 9)
        var a = new Vector2D(2, 1);
        var b = new Vector2D(8, 9);
        double dist = (b - a).Length();
        Check("Q1 Distance A(2, 1) -> B(8, 9)", $"{dist:0.###}", Near(dist, 10), "10");

        // Q2: length and unit vector of v = (6, 8)
        var v = new Vector2D(6, 8);
        double len = v.Length();
        Vector2D unit = v.Normalized();
        Check("Q2 |(6, 8)| and unit vector", $"{len:0.###}, {unit}",
              Near(len, 10) && unit.ApproximatelyEquals(new Vector2D(0.6, 0.8)), "10, (0.6, 0.8)");

        // Q3: agent at (0, 0) facing (1, 0), target at (2, 2): angle between heading and target direction
        var agent = Vector2D.Zero;
        var heading = new Vector2D(1, 0);
        Vector2D dir = (new Vector2D(2, 2) - agent).Normalized();
        double dot = heading.Dot(dir);
        double angle = Math.Acos(dot) * 180.0 / Math.PI;   // acos is fine here, this is the "by hand" check
        Check("Q3 Angle heading (1, 0) -> target (2, 2)", $"dir {dir}, dot {dot:0.###}, {angle:0.##} deg",
              Near(angle, 45), "dir (0.707, 0.707), dot 0.707, 45 deg");

        // Q4: heading (0, 1), direction to target (-3, 1): left or right?
        double cross = new Vector2D(0, 1).Cross(new Vector2D(-3, 1));
        int side = FovFunctions.SideOf(Vector2D.Zero, new Vector2D(0, 1), new Vector2D(-3, 1));
        Check("Q4 Heading (0, 1), target dir (-3, 1)", $"cross {cross:0.###} -> {(side > 0 ? "left" : side < 0 ? "right" : "collinear")}",
              Near(cross, 3) && side == +1, "cross 3 -> left");

        // Q5: turret fires at 20 units/s with a 60 degree angle: v = |v| (cos t, sin t)
        double rad = 60 * Math.PI / 180.0;
        var shot = new Vector2D(Math.Cos(rad), Math.Sin(rad)) * 20;
        Check("Q5 20 u/s at 60 deg -> (v.x, v.y)", shot.ToString(),
              Near(shot.X, 10) && Near(shot.Y, 17.3205, 1e-4), "(10, 17.32)");

        Console.WriteLine();
        return failed;
    }

    static bool Near(double a, double b, double eps = 1e-9) => Math.Abs(a - b) <= eps;

    static void Check(string question, string computed, bool ok, string expected)
    {
        if (!ok) failed++;
        Console.WriteLine($"{question,-42} computed: {computed,-38} expected: {expected,-38} {(ok ? "PASS" : "FAIL")}");
    }
}
