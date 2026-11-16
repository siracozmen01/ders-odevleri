using System;
using static Report;

/// <summary>Lab steps 1-4 of slide 21. Expected values come from the slides and their speaker notes.</summary>
public static class Lab
{
    public static void Run()
    {
        Step1Vector();
        Step2Agent();
        Step3Ball();
        Step4Intercept();
    }

    // ------------------------------------------------------------------ step 1
    static void Step1Vector()
    {
        Title("LAB 1 - Vector2D eklentileri: Truncate, Distance, DistanceSq");
        CheckHeader();

        Vector2D t = new Vector2D(6, 8).Truncate(5);
        Check("(6, 8).Truncate(5)", t.ToString(), "(3, 4)", t.ApproximatelyEquals(new Vector2D(3, 4), 1e-12));

        Vector2D u = new Vector2D(3, 4).Truncate(10);
        Check("(3, 4).Truncate(10) (sınırın altında)", u.ToString(), "(3, 4) değişmez", u.ApproximatelyEquals(new Vector2D(3, 4), 1e-12));

        var a = new Vector2D(1, 1);
        var b = new Vector2D(4, 5);
        Check("(1, 1).Distance((4, 5))", N(a.Distance(b)), "5", Near(a.Distance(b), 5, 1e-12));
        Check("(1, 1).DistanceSq((4, 5))", N(a.DistanceSq(b)), "25", Near(a.DistanceSq(b), 25, 1e-12));
    }

    // ------------------------------------------------------------------ step 2
    static void Step2Agent()
    {
        Title("LAB 2a - Euler hata tablosu: m = 2 kg, F = (10, 0) N, durgun başlangıç, 1 s");
        Console.WriteLine("a = F / m = 5 m/s², gerçek konum = ½·a·t² = 2.5 m");
        Console.WriteLine();
        Console.WriteLine($"{"Δt (s)",-8}{"adım",-6}{"Euler x (m)",-13}{"gerçek (m)",-12}{"hata (m)",-10}{"hata / Δt",-11}{"slayt",-8}sonuç");

        double[] dts = { 0.5, 0.1, 0.02 };
        double[] slide = { 3.75, 2.75, 2.55 };
        for (int i = 0; i < dts.Length; i++)
        {
            double dt = dts[i];
            int steps = (int)Math.Round(1.0 / dt);
            var agent = new Agent(Vector2D.Zero, 2.0, 1000, 1000);
            for (int k = 0; k < steps; k++) agent.Update(new Vector2D(10, 0), dt);

            double x = agent.Position.X;
            double exact = 0.5 * 5 * 1 * 1;
            double err = x - exact;
            Console.WriteLine($"{N(dt),-8}{steps,-6}{F(x),-13}{F(exact),-12}{F(err),-10}{F(err / dt),-11}{N(slide[i]),-8}{Mark(Near(x, slide[i], 1e-9))}");
        }
        Console.WriteLine("Hata / Δt sabit (2.5): Euler birinci dereceden; Δt beşte birine inince hata da beşte birine iner.");

        Title("LAB 2b - MaxSpeed = 3 sınırı: m = 2 kg, F = (10, 0) N, Δt = 0.1 s");
        Console.WriteLine($"{"t (s)",-7}{"Velocity",-12}{"|v| (m/s)",-11}{"Position",-14}Heading");
        var capped = new Agent(Vector2D.Zero, 2.0, 3.0, 1000);
        for (int k = 1; k <= 12; k++)
        {
            capped.Update(new Vector2D(10, 0), 0.1);
            Console.WriteLine($"{N(k * 0.1, 1),-7}{capped.Velocity,-12}{F(capped.Velocity.Length(), 2),-11}{capped.Position,-14}{capped.Heading}");
        }
        CheckHeader();
        Check("1.2 s sonra |v|", N(capped.Velocity.Length()), "3 (MaxSpeed'de sabit)", Near(capped.Velocity.Length(), 3, 1e-12));

        var weak = new Agent(Vector2D.Zero, 2.0, 1000, 4.0);
        for (int k = 0; k < 10; k++) weak.Update(new Vector2D(10, 0), 0.1);
        Check("MaxForce = 4: F = (10, 0) kırpılır, 1 s sonra v", weak.Velocity.ToString(), "(2, 0) (a = 4 / 2 = 2)",
              weak.Velocity.ApproximatelyEquals(new Vector2D(2, 0), 1e-9));

        var still = new Agent(Vector2D.Zero, 1.0, 10, 10);
        still.Heading = new Vector2D(0, 1);
        still.Update(Vector2D.Zero, 0.1);
        Check("Duran ajan: Heading korunur (sıfıra bölme yok)", still.Heading.ToString(), "(0, 1)",
              still.Heading.ApproximatelyEquals(new Vector2D(0, 1), 1e-12));
    }

    // ------------------------------------------------------------------ step 3
    static void Step3Ball()
    {
        Title("LAB 3 - Top tablosu (Simple Soccer): u = 10 m/s, a = -2 m/s²");
        const double u = 10, a = -2;
        Console.WriteLine($"{"Soru",-36}{"Fonksiyon",-36}{"Sonuç",-10}{"Slayt",-14}");

        Row("Ne zaman durur?", "TimeToStop(10, -2)", Kinematics.TimeToStop(u, a), 5, "5 s");
        Row("Duruncaya kadar ne kadar gider?", "StoppingDistance(10, -2)", Kinematics.StoppingDistance(u, a), 25, "25 m");
        Row("3 s sonra nerede?", "FuturePosition(10, -2, 3)", Kinematics.FuturePosition(u, a, 3), 21, "21 m");
        Row("16 m'yi kaç saniyede gider?", "TimeToCoverDistance(10, -2, 16)", Kinematics.TimeToCoverDistance(u, a, 16), 2, "2 s");
        Row("30 m'yi kaç saniyede gider?", "TimeToCoverDistance(10, -2, 30)", Kinematics.TimeToCoverDistance(u, a, 30), -1, "Ulaşamaz (-1)");
        Row("(ek) 7 s sonra nerede? (durmuş)", "FuturePosition(10, -2, 7)", Kinematics.FuturePosition(u, a, 7), 25, "25 m (not)");
        Row("(ek) 25 m'yi kaç saniyede gider?", "TimeToCoverDistance(10, -2, 25)", Kinematics.TimeToCoverDistance(u, a, 25), 5, "5 s (sınır)");
    }

    static void Row(string question, string call, double value, double expected, string slide)
    {
        Console.WriteLine($"{question,-36}{call,-36}{N(value),-10}{slide,-14}{Mark(Near(value, expected, 1e-9))}");
    }

    // ------------------------------------------------------------------ step 4
    static void Step4Intercept()
    {
        Title("LAB 4 - InterceptTime: örnek A ve B, kule (0, 0), mermi s = 15 m/s");
        Example("A", new Vector2D(10, 0), new Vector2D(0, 3), 15,
                0.556, new Vector2D(10, 1.667), 0.36, 0.680, new Vector2D(10, 2.04));
        Example("B", new Vector2D(10, 0), new Vector2D(5, 0), 15,
                0.5, new Vector2D(12.5, 0), double.NaN, 1.0, new Vector2D(15, 0));
    }

    static void Example(string name, Vector2D P, Vector2D V, double s,
                        double slideApproxT, Vector2D slideApproxAim, double slideApproxMiss,
                        double slideExactT, Vector2D slideExactAim)
    {
        Vector2D shooter = Vector2D.Zero;
        double qa = V.Dot(V) - s * s, qb = 2 * P.Dot(V), qc = P.Dot(P);

        double tA = Interception.ApproximateInterceptTime(P, V, s);
        Vector2D aimA = P + V * tA;
        double missA = Interception.MissDistance(shooter, P, V, s, aimA);

        double tE = Interception.InterceptTime(P, V, s);
        Vector2D aimE = P + V * tE;
        double missE = Interception.MissDistance(shooter, P, V, s, aimE);

        Console.WriteLine();
        Console.WriteLine($"Örnek {name}: hedef P = {P}, V = {V}, s = {N(s)}   denklem: {N(qa)}·t² + {N(qb)}·t + {N(qc)} = 0");
        Console.WriteLine($"  {"Yöntem",-10}{"t (s)",-9}{"nişan",-17}{"ıska (m)",-10}{"slayt",-34}sonuç");
        string slideA = $"t = {N(slideApproxT)} → {slideApproxAim}" + (double.IsNaN(slideApproxMiss) ? "" : $", {N(slideApproxMiss, 2)} m");
        bool okA = Near(tA, slideApproxT, 1e-3) && aimA.ApproximatelyEquals(slideApproxAim, 1e-3)
                   && (double.IsNaN(slideApproxMiss) || Near(missA, slideApproxMiss, 0.01));
        Console.WriteLine($"  {"yaklaşık",-10}{F(tA),-9}{aimA.ToString(),-17}{F(missA, 2),-10}{slideA,-34}{Mark(okA)}");
        string slideE = $"t = {N(slideExactT)} → {slideExactAim}";
        bool okE = Near(tE, slideExactT, 1e-3) && aimE.ApproximatelyEquals(slideExactAim, 5e-3) && missE < 1e-9;
        Console.WriteLine($"  {"kesin",-10}{F(tE),-9}{aimE.ToString(),-17}{F(missE, 2),-10}{slideE,-34}{Mark(okE)}");
    }
}
