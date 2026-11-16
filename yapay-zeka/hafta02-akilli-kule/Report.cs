using System;
using System.Globalization;

/// <summary>Small console helpers: number formatting and PASS/FAIL bookkeeping.</summary>
public static class Report
{
    public static int Passed { get; private set; }
    public static int Failed { get; private set; }

    /// <summary>Formats a number with up to <paramref name="decimals"/> decimals (invariant culture).</summary>
    public static string N(double x, int decimals = 3)
    {
        if (double.IsNaN(x)) return "-";
        return x.ToString("0." + new string('#', decimals), CultureInfo.InvariantCulture);
    }

    /// <summary>Fixed number of decimals, for aligned tables.</summary>
    public static string F(double x, int decimals = 3)
    {
        if (double.IsNaN(x)) return "-";
        return x.ToString("F" + decimals, CultureInfo.InvariantCulture);
    }

    public static bool Near(double a, double b, double eps) => Math.Abs(a - b) <= eps;

    public static string Mark(bool ok)
    {
        if (ok) Passed++; else Failed++;
        return ok ? "PASS" : "FAIL";
    }

    public static void Title(string text)
    {
        Console.WriteLine();
        Console.WriteLine(text);
        Console.WriteLine(new string('-', text.Length));
    }

    /// <summary>One "computed vs expected" line.</summary>
    public static void Check(string label, string computed, string expected, bool ok, int labelWidth = 52, int valueWidth = 34)
    {
        Console.WriteLine(Pad(label, labelWidth) + Pad(computed, valueWidth) + Pad(expected, valueWidth) + Mark(ok));
    }

    public static void CheckHeader(int labelWidth = 52, int valueWidth = 34)
    {
        Console.WriteLine("".PadRight(labelWidth) + Pad("hesaplanan", valueWidth) + Pad("beklenen", valueWidth) + "sonuç");
    }

    /// <summary>PadRight that always leaves at least one space between columns.</summary>
    public static string Pad(string s, int width) => s.Length >= width ? s + " " : s.PadRight(width);
}
