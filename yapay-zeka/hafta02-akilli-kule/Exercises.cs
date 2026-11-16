using System;
using static Report;

/// <summary>The seven exercises of slide 23, verified against the answer key in the speaker notes.</summary>
public static class Exercises
{
    public static void Run()
    {
        Title("ALIŞTIRMALAR (slayt 23) - ders notundaki cevap anahtarıyla doğrulama");
        CheckHeader(50, 40);

        // 1) 144 FPS: dt and the step of a 6 units/s agent in one frame
        double dt = 1.0 / 144;
        double step = 6 * dt;
        Check("1) 144 FPS: Δt ve 6 birim/s ile bir kare", $"Δt = {N(dt, 5)} s, adım = {N(step, 4)}",
              "Δt ≈ 0.00694 s, adım ≈ 0.0417", Near(dt, 0.00694, 5e-6) && Near(step, 0.0417, 5e-5), 50, 40);

        // 2) m = 3 kg, F = (12, -6) N
        var agent2 = new Agent(Vector2D.Zero, 3, 1000, 1000);
        agent2.Update(new Vector2D(12, -6), 1.0);           // one 1 s step from rest: velocity == acceleration
        Vector2D acc = new Vector2D(12, -6) * (1.0 / 3);
        Check("2) m = 3, F = (12, -6): ivme", $"{acc} (Agent: Δv/1 s = {agent2.Velocity})", "(4, -2) m/s²",
              acc.ApproximatelyEquals(new Vector2D(4, -2), 1e-12) && agent2.Velocity.ApproximatelyEquals(new Vector2D(4, -2), 1e-12), 50, 40);

        // 3) Truncate (9, 12) to 10
        Vector2D tr = new Vector2D(9, 12).Truncate(10);
        Check("3) (9, 12), |v| = 15, MaxSpeed = 10", tr.ToString(), "(6, 8)", tr.ApproximatelyEquals(new Vector2D(6, 8), 1e-12), 50, 40);

        // 4) m = 1, F = (4, 0), dt = 0.25, two Euler steps
        var agent4 = new Agent(Vector2D.Zero, 1, 1000, 1000);
        agent4.Update(new Vector2D(4, 0), 0.25);
        double v1 = agent4.Velocity.X, p1 = agent4.Position.X;
        agent4.Update(new Vector2D(4, 0), 0.25);
        double exact4 = 0.5 * 4 * 0.5 * 0.5;
        double err4 = agent4.Position.X - exact4;
        Check("4) Euler, Δt = 0.25: 1. adım", $"v = {N(v1)}, p = {N(p1)}", "v = 1, p = 0.25", Near(v1, 1, 1e-12) && Near(p1, 0.25, 1e-12), 50, 40);
        Check("   2. adım", $"v = {N(agent4.Velocity.X)}, p = {N(agent4.Position.X)}", "v = 2, p = 0.75",
              Near(agent4.Velocity.X, 2, 1e-12) && Near(agent4.Position.X, 0.75, 1e-12), 50, 40);
        Check("   0.5 s sonra konum / gerçek / hata", $"{N(agent4.Position.X)} / {N(exact4)} / {N(err4)} m",
              "0.75 / 0.5 / 0.25 m", Near(agent4.Position.X, 0.75, 1e-12) && Near(err4, 0.25, 1e-12), 50, 40);

        // 5) Ball u = 12, a = -3
        double tStop = Kinematics.TimeToStop(12, -3);
        double dStop = Kinematics.StoppingDistance(12, -3);
        double t20 = Kinematics.TimeToCoverDistance(12, -3, 20);
        double v20 = Math.Sqrt(12 * 12 + 2 * -3 * 20);
        Check("5) Top u = 12, a = -3: durma / yol / 20 m", $"{N(tStop)} s / {N(dStop)} m / {N(t20, 2)} s (v = {N(v20, 2)})",
              "4 s / 24 m / ≈ 2.37 s (v ≈ 4.90)", Near(tStop, 4, 1e-12) && Near(dStop, 24, 1e-12) && Near(t20, 2.37, 0.005), 50, 40);

        // 6) Turret (0, 0), target (12, 0), V = (0, 4), bullet 20 m/s
        var kule = Vector2D.Zero;
        var hedef = new Vector2D(12, 0);
        var hiz = new Vector2D(0, 4);
        SmartTurret.ApproximateAim(kule, hedef, hiz, 20, out Vector2D aimA, out double tA);
        SmartTurret.NisanAl(kule, hedef, hiz, 20, out Vector2D aimE, out double tE);
        Check("6) Yaklaşık nişan", $"t = {N(tA)} → {aimA}", "t = 0.5 → (12, 2)", Near(tA, 0.5, 1e-12) && aimA.ApproximatelyEquals(new Vector2D(12, 2), 1e-12), 50, 40);
        Check("   Kesin nişan", $"t = {N(tE)} → {aimE}", "t ≈ 0.612 → (12, 2.45)", Near(tE, 0.612, 5e-4) && aimE.ApproximatelyEquals(new Vector2D(12, 2.45), 5e-3), 50, 40);

        // 7) v = 25, x = 40, y = 0
        double root = SmartTurret.RootTerm(25, 40, 0);
        bool ok7 = SmartTurret.AtisAcisi(25, 40, 0, out double low, out double high);
        Check("7) v = 25, x = 40: kökün içi / kök", $"{N(root, 0)} / {N(Math.Sqrt(root), 1)}", "≈ 236647 / ≈ 486.5",
              Near(root, 236647, 1) && Near(Math.Sqrt(root), 486.5, 0.05), 50, 40);
        Check("   iki açı", $"{F(low, 2)}° / {F(high, 2)}°", "≈ 19.4° / ≈ 70.6°", ok7 && Near(low, 19.4, 0.05) && Near(high, 70.6, 0.05), 50, 40);
        double fLow = SmartTurret.FlightTime(25, 40, low), fHigh = SmartTurret.FlightTime(25, 40, high);
        Check("   keskin nişancı: uçuş süreleri", $"alçak {F(fLow, 2)} s < yüksek {F(fHigh, 2)} s", "alçak açı (kısa uçuş)", fLow < fHigh, 50, 40);
    }
}
