// File: Assets/Scripts/RouteClassifier.cs  (homework helper)
using UnityEngine;

// Names the route of a path by where it crosses the barrier line x = 15.
// The z bands match the passages built by Hafta6SceneBuilder (top to bottom):
//   A  mud corridor      z 23..27
//   B  jump link         z 15..21 (thin wall, link at z = 18)
//   C  shallow water     z 7..11
//   D  door corridor     z 0..3
public static class RouteClassifier
{
    public const float BarrierX = 15f;

    public static string Letter(Vector3[] corners)
    {
        if (corners == null || corners.Length < 2) return "-";
        for (int i = 0; i < corners.Length - 1; i++)
        {
            float x0 = corners[i].x - BarrierX, x1 = corners[i + 1].x - BarrierX;
            if (x0 == 0f && x1 == 0f) continue;
            if (x0 * x1 > 0f) continue;   // this segment does not cross the barrier
            float k = x0 / (x0 - x1);
            float z = Mathf.Lerp(corners[i].z, corners[i + 1].z, k);
            if (z >= 22.5f) return "A";
            if (z >= 14.5f) return "B";
            if (z >= 6.5f) return "C";
            return "D";
        }
        return "-";
    }

    public static string Name(string letter)
    {
        switch (letter)
        {
            case "A": return "A (Çamur)";
            case "B": return "B (Atlama)";
            case "C": return "C (Sığ su)";
            case "D": return "D (Kapı)";
            default: return letter;
        }
    }
}
