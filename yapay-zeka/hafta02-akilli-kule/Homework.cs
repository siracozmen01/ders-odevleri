using System;
using static Report;

/// <summary>
/// Homework of slide 22 ("Akilli kule"): NisanAl + AtisAcisi checks, the turret scenarios
/// with approximate and exact aim side by side, and the 3D bonus.
/// </summary>
public static class Homework
{
    struct Scenario
    {
        public string Name;
        public Vector2D Target, Velocity;
        public Scenario(string name, Vector2D target, Vector2D velocity) { Name = name; Target = target; Velocity = velocity; }
    }

    const double BulletSpeed = 20.0;                   // m/s, same speed as the ballistics examples
    static readonly Vector2D Turret = Vector2D.Zero;

    static readonly Scenario[] Scenarios =
    {
        new Scenario("Yandan geçen",                new Vector2D(15, 0),  new Vector2D(0, 5)),
        new Scenario("Uzaklaşan",                   new Vector2D(12, 9),  new Vector2D(4, 3)),
        new Scenario("Yaklaşan",                    new Vector2D(30, 0),  new Vector2D(-6, 0)),
        new Scenario("Duran",                       new Vector2D(10, 10), new Vector2D(0, 0)),
        new Scenario("Mermiden hızlı kaçan",        new Vector2D(10, 0),  new Vector2D(25, 0)),
        new Scenario("Menzil dışı",                 new Vector2D(45, 0),  new Vector2D(0, 3)),
        new Scenario("(ek) Hızlı yandan geçen",     new Vector2D(20, 0),  new Vector2D(0, 15)),
        new Scenario("(ek) Mermiden hızlı yaklaşan", new Vector2D(30, 10), new Vector2D(-25, 0)),
        new Scenario("(ek) Mermi hızında yaklaşan", new Vector2D(10, 0),  new Vector2D(-20, 0)),
    };

    public static void Run()
    {
        AtisAcisiChecks();
        ScenarioTable();
        BallisticTable();
        IterativeCorrection();
        Bonus3D();
    }

    // ------------------------------------------------------------ AtisAcisi vs slides 16-17
    static void AtisAcisiChecks()
    {
        Title("ÖDEV - AtisAcisi(v, x, y): slayt 16-17 tablosuyla karşılaştırma (g = 9.81)");
        Console.WriteLine($"{"v",-5}{"x (m)",-9}{"y (m)",-7}{"kökün içi",-12}{"dönüş",-7}{"alçak",-9}{"yüksek",-9}{"uçuş (s)",-15}{"slayt",-36}sonuç");

        BallisticRow(20, 30, 0, true, 23.7, 66.3, 1.64, 3.73, "73388; 23.7° / 66.3°; 1.64 / 3.73 s");
        BallisticRow(20, 30, 5, true, 36.2, 63.3, 1.86, 3.34, "34148; 36.2° / 63.3°; 1.86 / 3.34 s");
        BallisticRow(20, 45, 0, false, 0, 0, 0, 0, "< 0; menzil dışı");
        BallisticRow(20, 400 / SmartTurret.G, 0, true, 45, 45, double.NaN, double.NaN, "= 0; tek açı (45°, en uzak atış)");
        BallisticRow(25, 40, 0, true, 19.4, 70.6, double.NaN, double.NaN, "236647; 19.4° / 70.6° (alıştırma 7)");
    }

    static void BallisticRow(double v, double x, double y, bool expectOk, double low, double high,
                             double tLow, double tHigh, string slide)
    {
        bool ok = SmartTurret.AtisAcisi(v, x, y, out double a1, out double a2);
        double root = SmartTurret.RootTerm(v, x, y);
        string flight = ok ? $"{F(SmartTurret.FlightTime(v, x, a1), 2)} / {F(SmartTurret.FlightTime(v, x, a2), 2)}" : "-";

        bool pass = ok == expectOk;
        if (ok && expectOk)
        {
            pass &= Near(a1, low, 0.05) && Near(a2, high, 0.05);
            if (!double.IsNaN(tLow))
                pass &= Near(SmartTurret.FlightTime(v, x, a1), tLow, 0.006) && Near(SmartTurret.FlightTime(v, x, a2), tHigh, 0.006);
        }
        string rootText = Math.Abs(root) < 1e-6 ? "0" : N(root, 0);
        Console.WriteLine($"{N(v),-5}{N(x, 2),-9}{N(y),-7}{rootText,-12}{ok,-7}{(ok ? F(a1, 2) + "°" : "-"),-9}{(ok ? F(a2, 2) + "°" : "-"),-9}{flight,-15}{slide,-36}{Mark(pass)}");
    }

    // ------------------------------------------------------------ scenarios: approximate vs exact
    static void ScenarioTable()
    {
        Title($"ÖDEV - Senaryolar: kule {Turret}, mermi sürati {N(BulletSpeed)} m/s, yaklaşık ve kesin nişan yan yana");
        Console.WriteLine("ıska = mermi nişan noktasına vardığı anda hedefin o noktaya uzaklığı (0 = isabet)");
        Console.WriteLine();
        Console.WriteLine($"{"#",-3}{"Senaryo",-30}{"hedef",-10}{"V",-10}{"|V|",-6}| {"yakl. t",-8}{"yaklaşık nişan",-17}{"ıska",-7}| {"NisanAl",-8}{"kesin t",-8}{"kesin nişan",-17}{"ıska",-6}");
        Console.WriteLine(new string('-', 136));

        for (int i = 0; i < Scenarios.Length; i++)
        {
            Scenario sc = Scenarios[i];
            SmartTurret.ApproximateAim(Turret, sc.Target, sc.Velocity, BulletSpeed, out Vector2D aimA, out double tA);
            double missA = Interception.MissDistance(Turret, sc.Target, sc.Velocity, BulletSpeed, aimA);

            bool ok = SmartTurret.NisanAl(Turret, sc.Target, sc.Velocity, BulletSpeed, out Vector2D aimE, out double tE);
            string exactT = ok ? F(tE) : "-";
            string exactAim = ok ? aimE.ToString() : "yetişemez";
            string exactMiss = ok ? F(Interception.MissDistance(Turret, sc.Target, sc.Velocity, BulletSpeed, aimE), 2) : "-";

            Console.WriteLine($"{i + 1,-3}{sc.Name,-30}{sc.Target.ToString(),-10}{sc.Velocity.ToString(),-10}{N(sc.Velocity.Length(), 1),-6}| " +
                              $"{F(tA),-8}{aimA.ToString(),-17}{F(missA, 2),-7}| {ok,-8}{exactT,-8}{exactAim,-17}{exactMiss,-6}");
        }

        // Automatic checks of the expected behaviour per scenario.
        Console.WriteLine();
        CheckHeader();
        ExpectHit(0); ExpectHit(1); ExpectHit(2); ExpectHit(3);
        bool ok5 = SmartTurret.NisanAl(Turret, Scenarios[4].Target, Scenarios[4].Velocity, BulletSpeed, out _, out double t5);
        Check("5 Mermiden hızlı kaçan: NisanAl", $"{ok5}, süre {N(t5)}", "false, -1 (diskriminant < 0)", !ok5 && t5 == -1);
        ExpectHit(5);
        ExpectHit(6); ExpectHit(7); ExpectHit(8);

        SmartTurret.ApproximateAim(Turret, Scenarios[3].Target, Scenarios[3].Velocity, BulletSpeed, out Vector2D stillA, out _);
        SmartTurret.NisanAl(Turret, Scenarios[3].Target, Scenarios[3].Velocity, BulletSpeed, out Vector2D stillE, out _);
        Check("4 Duran: yaklaşık = kesin = hedef", $"{stillA} / {stillE}", "(10, 10) / (10, 10)",
              stillA.ApproximatelyEquals(Scenarios[3].Target, 1e-12) && stillE.ApproximatelyEquals(Scenarios[3].Target, 1e-12));
    }

    static void ExpectHit(int i)
    {
        Scenario sc = Scenarios[i];
        bool ok = SmartTurret.NisanAl(Turret, sc.Target, sc.Velocity, BulletSpeed, out Vector2D aim, out double t);
        double miss = ok ? Interception.MissDistance(Turret, sc.Target, sc.Velocity, BulletSpeed, aim) : double.NaN;
        // The exact aim must satisfy |aim - turret| = s * t (the bullet and the target arrive together).
        bool pass = ok && t > 0 && miss < 1e-9 && Near((aim - Turret).Length(), BulletSpeed * t, 1e-9);
        Check($"{i + 1} {sc.Name}: kesin nişan isabet", ok ? $"true, ıska {F(miss, 9)}" : "false", "true, ıska 0", pass);
    }

    // ------------------------------------------------------------ ballistic angles for the exact aim points
    static void BallisticTable()
    {
        Title($"ÖDEV - Kesin nişan noktasına balistik atış açıları (AtisAcisi, v = {N(BulletSpeed)} m/s, y = 0, en uzak atış v²/g = {N(BulletSpeed * BulletSpeed / SmartTurret.G, 2)} m)");
        Console.WriteLine($"{"#",-3}{"Senaryo",-30}{"kesin nişan",-17}{"x (m)",-9}{"AtisAcisi",-11}{"alçak",-9}{"yüksek",-9}{"uçuş alçak/yüksek (s)",-24}karar");
        for (int i = 0; i < Scenarios.Length; i++)
        {
            Scenario sc = Scenarios[i];
            if (!SmartTurret.NisanAl(Turret, sc.Target, sc.Velocity, BulletSpeed, out Vector2D aim, out _))
            {
                Console.WriteLine($"{i + 1,-3}{sc.Name,-30}{"yetişemez",-17}{"-",-9}{"-",-11}{"-",-9}{"-",-9}{"-",-24}ateş etme, hedef değiştir");
                continue;
            }
            double x = (aim - Turret).Length();
            bool ok = SmartTurret.AtisAcisi(BulletSpeed, x, 0, out double low, out double high);
            string flight = ok ? $"{F(SmartTurret.FlightTime(BulletSpeed, x, low), 2)} / {F(SmartTurret.FlightTime(BulletSpeed, x, high), 2)}" : "-";
            string decision = ok ? "alçak açıyla ateş" : "menzil dışı: yaklaş";
            Console.WriteLine($"{i + 1,-3}{sc.Name,-30}{aim.ToString(),-17}{F(x, 2),-9}{ok,-11}{(ok ? F(low, 2) + "°" : "-"),-9}{(ok ? F(high, 2) + "°" : "-"),-9}{flight,-24}{decision}");
        }
        Console.WriteLine();
        CheckHeader();
        SmartTurret.NisanAl(Turret, Scenarios[5].Target, Scenarios[5].Velocity, BulletSpeed, out Vector2D far, out _);
        bool farOk = SmartTurret.AtisAcisi(BulletSpeed, far.Length(), 0, out _, out _);
        Check("6 Menzil dışı: AtisAcisi", farOk.ToString(), "false (kökün içi < 0)", !farOk);
    }

    // ------------------------------------------------------------ extra: ballistic flight time correction
    static void IterativeCorrection()
    {
        Scenario sc = Scenarios[0];
        Title($"EK - Hareketli hedef + balistik atış: uçuş süresiyle yinelemeli düzeltme (senaryo 1, {sc.Name})");
        Console.WriteLine("Düz çizgi kestirimi t'yi mermi hızıyla hesaplar; balistik merminin yatay hızı v·cos θ olduğu için gerçek uçuş daha uzundur.");
        Console.WriteLine($"{"tur",-5}{"kullanılan t",-14}{"nişan",-17}{"alçak",-9}{"gerçek uçuş",-13}ıska (hedef - nişan, uçuş bitince)");
        SmartTurret.BallisticLeadAim(Turret, sc.Target, sc.Velocity, BulletSpeed, 5,
            (i, t, aim, low, flight) =>
            {
                double miss = (sc.Target + sc.Velocity * flight).Distance(aim);
                Console.WriteLine($"{i,-5}{F(t),-14}{aim.ToString(),-17}{F(low, 2) + "°",-9}{F(flight),-13}{F(miss, 4)} m");
            },
            out Vector2D finalAim, out double finalLow, out double finalFlight);
        double finalMiss = (sc.Target + sc.Velocity * finalFlight).Distance(finalAim);
        CheckHeader();
        Check("5 tur sonra ıska", $"{F(finalMiss, 6)} m", "≈ 0 (yakınsar)", finalMiss < 1e-3);
    }

    // ------------------------------------------------------------ bonus: 3D launch velocity
    static void Bonus3D()
    {
        Title("EK GÖREV (3B) - Vector3D ile fırlatma hızı: namlu (0, 1, 0), hedef (12, 1, 16), v = 20 m/s");
        var muzzle = new Vector3D(0, 1, 0);
        var target = new Vector3D(12, 1, 16);
        const double v = 20;

        bool ok = SmartTurret.LaunchVelocity(muzzle, target, v, false, out Vector3D vel, out double yaw, out double pitch);
        Vector3D d = target - muzzle;
        double x = Math.Sqrt(d.X * d.X + d.Z * d.Z);
        Console.WriteLine($"d = hedef - namlu = {d}, yatay mesafe x = {N(x)} m, y = {N(d.Y)} m");
        Console.WriteLine($"birim(yatay) = {new Vector3D(d.X, 0, d.Z).Normalized()}");

        CheckHeader();
        Check("Yaw = atan2(d.x, d.z)", $"{F(yaw, 2)}°", "≈ 36.9°", Near(yaw, 36.9, 0.05));
        Check("Pitch (alçak açı)", $"{F(pitch, 2)}°", "≈ 14.7°", ok && Near(pitch, 14.7, 0.05));
        Check("Fırlatma hızı (alçak)", vel.ToString(), "≈ (11.6, 5.1, 15.5)",
              ok && Near(vel.X, 11.6, 0.05) && Near(vel.Y, 5.1, 0.05) && Near(vel.Z, 15.5, 0.05));
        Check("|fırlatma hızı|", N(vel.Length()), "20", Near(vel.Length(), v, 1e-9));

        // Verify by flying the shell analytically: p(t) = muzzle + vel*t + 1/2 * (0, -g, 0) * t^2.
        double t = SmartTurret.FlightTime(v, x, pitch);
        Vector3D landing = muzzle + vel * t + new Vector3D(0, -SmartTurret.G, 0) * (0.5 * t * t);
        Check($"Uçuş sonu konum (t = {F(t, 3)} s)", landing.ToString(), "(12, 1, 16) = hedef", landing.ApproximatelyEquals(target, 1e-9));

        // Same check with a small-step Euler simulation (dt = 0.001 s), like a physics engine would do.
        Vector3D p = muzzle, pv = vel;
        double dt = 0.001, closest = double.MaxValue;
        for (int k = 0; k < 5000; k++)
        {
            pv = pv + new Vector3D(0, -SmartTurret.G, 0) * dt;
            p = p + pv * dt;
            closest = Math.Min(closest, (p - target).Length());
        }
        Check("Euler simülasyonu (Δt = 0.001): hedefe en yakın", $"{F(closest, 3)} m", "< 0.05 m", closest < 0.05);

        bool okHigh = SmartTurret.LaunchVelocity(muzzle, target, v, true, out Vector3D velHigh, out _, out double pitchHigh);
        double tHigh = SmartTurret.FlightTime(v, x, pitchHigh);
        Vector3D landingHigh = muzzle + velHigh * tHigh + new Vector3D(0, -SmartTurret.G, 0) * (0.5 * tHigh * tHigh);
        Check($"(ek) Yüksek açı {F(pitchHigh, 2)}°, hız", velHigh.ToString(), "hedefe ulaşmalı", okHigh && landingHigh.ApproximatelyEquals(target, 1e-9));

        bool okFar = SmartTurret.LaunchVelocity(muzzle, new Vector3D(40, 1, 30), v, false, out _, out _, out _);
        Check("(ek) Hedef (40, 1, 30), 50 m: menzil dışı", okFar.ToString(), "false", !okFar);
    }
}
