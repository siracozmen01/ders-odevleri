// File: Assets/Editor/RouteCostReport.cs
// Batch measurement for the report: after the bake, sets global area costs with NavMesh.SetAreaCost,
// asks Unity for the path with NavMesh.CalculatePath and measures it with the PathDebugDraw logic.
// Menu: Hafta6 > Route Cost Report   or   -executeMethod RouteCostReport.Run
// Output: rapor/kanit/editor-rota-hesabi.txt (and the Editor log)
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using Unity.AI.Navigation;

public static class RouteCostReport
{
    const string OutFile = "rapor/kanit/editor-rota-hesabi.txt";
    static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    static int mudArea, waterArea, jumpArea;
    static NavMeshLink link;

    // Per-route length split, measured from Unity's path
    struct Breakdown
    {
        public string letter;
        public float total, walk, mud, water, jump;
        public Vector3[] corners;
        public float G(float m, float s, float j) => walk + mud * m + water * s + jump * j;
    }

    [MenuItem("Hafta6/Route Cost Report")]
    public static void Run()
    {
        EditorSceneManager.OpenScene(Hafta6SceneBuilder.ScenePath);
        var surface = Object.FindAnyObjectByType<NavMeshSurface>();
        if (NavMesh.CalculateTriangulation().vertices.Length == 0) surface.AddData();
        link = Object.FindAnyObjectByType<NavMeshLink>();
        link.UpdateLink();
        // The door starts open in Play mode (SlidingDoor disables its obstacle in Awake).
        // In the editor the obstacle component is still enabled and would carve the corridor, so switch it off here.
        foreach (var obs in Object.FindObjectsByType<NavMeshObstacle>()) obs.enabled = false;

        mudArea = NavMesh.GetAreaFromName("Camur");
        waterArea = NavMesh.GetAreaFromName("SigSu");
        jumpArea = NavMesh.GetAreaFromName("Jump");
        float[] defaults = { NavMesh.GetAreaCost(mudArea), NavMesh.GetAreaCost(waterArea), NavMesh.GetAreaCost(jumpArea) };

        var sb = new StringBuilder();
        void W(string s) { sb.AppendLine(s); Debug.Log("[Rapor] " + s); }

        var tri = NavMesh.CalculateTriangulation();
        var areaCount = new Dictionary<int, int>();
        foreach (int a in tri.areas) areaCount[a] = areaCount.TryGetValue(a, out int n) ? n + 1 : 1;
        W($"Unity {Application.unityVersion}, AI Navigation 2.0.16. Scene {Hafta6SceneBuilder.ScenePath}");
        W($"NavMesh: {tri.vertices.Length} vertices, {tri.indices.Length / 3} triangles; triangles per area: " +
          Join(areaCount));
        W($"Area indices: Camur={mudArea}, SigSu={waterArea}, Jump={jumpArea}. Project default costs: Camur {F(defaults[0])}, SigSu {F(defaults[1])}, Jump {F(defaults[2])}");
        Vector3 from = Snap(Hafta6SceneBuilder.NpcStart), to = Snap(Hafta6SceneBuilder.PlayerStart);
        W($"NPC N = {V(from)}   character P = {V(to)}");
        W("");

        // 1. Each route on its own: force it with prohibitive costs on the other passages, then measure its length split
        W("== 1. Route lengths read from Unity (CalculatePath; split by area, 0.02 m steps) ==");
        var forced = new Dictionary<string, Breakdown>();
        var forceCosts = new Dictionary<string, float[]>
        {
            { "A", new[] { 1f, 50f, 50f } }, { "B", new[] { 50f, 50f, 1f } },
            { "C", new[] { 50f, 1f, 50f } }, { "D", new[] { 50f, 50f, 50f } },
        };
        foreach (var kv in forceCosts)
        {
            SetCosts(kv.Value[0], kv.Value[1], kv.Value[2]);
            var path = new NavMeshPath();
            NavMesh.CalculatePath(from, to, NavMesh.AllAreas, path);
            var b = Split(path.corners);
            forced[kv.Key] = b;
            W($"Route {kv.Key} (forced, got {b.letter}, {path.status}): L = {F(b.total)} m  [walk {F(b.walk)}, Camur {F(b.mud)}, SigSu {F(b.water)}, link {F(b.jump)}]");
            W($"   corners: {Corners(path.corners)}");
        }
        W("");

        // 2. The report's cost settings
        W("== 2. Cost settings: hand estimate (g = sum L_i * c_i with the lengths above) vs Unity ==");
        foreach (var p in Hafta6SceneBuilder.Presets)
        {
            SetCosts(p.mud, p.water, p.jump);
            var path = new NavMeshPath();
            NavMesh.CalculatePath(from, to, NavMesh.AllAreas, path);
            string uni = RouteClassifier.Letter(path.corners);
            float cost = ScreenCost(path.corners, out float len);
            string est = "";
            string best = null; float bestG = float.MaxValue;
            foreach (var r in forced)
            {
                float g = r.Value.G(p.mud, p.water, p.jump);
                est += $"g({r.Key})={F(g)}  ";
                if (g < bestG) { bestG = g; best = r.Key; }
            }
            W($"{p.name}: Camur {F(p.mud)}, SigSu {F(p.water)}, Jump {F(p.jump)}");
            W($"   estimate: {est}-> {best}");
            W($"   Unity: route {uni}, status {path.status}, corners {path.corners.Length}, length {F(len)} m, PathDebugDraw cost {F(cost)}");
            W($"   corners: {Corners(path.corners)}");
        }
        W("");

        // 3. Threshold sweeps: change one cost at a time around each setting, record where Unity's route switches
        W("== 3. Sweeps (one cost varied 1..15 in 0.05 steps, the others fixed): route switches ==");
        string[] names = { "Camur", "SigSu", "Jump" };
        foreach (var p in Hafta6SceneBuilder.Presets)
        {
            float[] baseCosts = { p.mud, p.water, p.jump };
            for (int k = 0; k < 3; k++)
            {
                var unitySw = new List<string>();
                var handSw = new List<string>();
                string prevU = null, prevH = null;
                for (int step = 0; step <= 280; step++)
                {
                    float v = 1f + step * 0.05f;
                    float[] c = (float[])baseCosts.Clone();
                    c[k] = v;
                    SetCosts(c[0], c[1], c[2]);
                    var path = new NavMeshPath();
                    NavMesh.CalculatePath(from, to, NavMesh.AllAreas, path);
                    string u = RouteClassifier.Letter(path.corners);
                    string h = null; float hg = float.MaxValue;
                    foreach (var r in forced) { float g = r.Value.G(c[0], c[1], c[2]); if (g < hg) { hg = g; h = r.Key; } }
                    if (prevU != null && u != prevU) unitySw.Add($"{prevU}->{u} at {F(v)}");
                    if (prevH != null && h != prevH) handSw.Add($"{prevH}->{h} at {F(v)}");
                    prevU = u; prevH = h;
                }
                W($"{p.name}, vary {names[k]} (others {F(c0(baseCosts, k, 0))}/{F(c0(baseCosts, k, 1))}/{F(c0(baseCosts, k, 2))}): " +
                  $"Unity [{string.Join("; ", unitySw)}]   hand [{string.Join("; ", handSw)}]");
            }
        }

        SetCosts(defaults[0], defaults[1], defaults[2]);   // restore the project defaults
        Directory.CreateDirectory(Path.GetDirectoryName(OutFile));
        File.WriteAllText(OutFile, sb.ToString(), new UTF8Encoding(false));
        Debug.Log("[Rapor] written " + OutFile);
    }

    static float c0(float[] c, int k, int i) => i == k ? float.NaN : c[i];

    static void SetCosts(float m, float s, float j)
    {
        NavMesh.SetAreaCost(mudArea, m);
        NavMesh.SetAreaCost(waterArea, s);
        NavMesh.SetAreaCost(jumpArea, j);
    }

    static Vector3 Snap(Vector3 p) =>
        NavMesh.SamplePosition(p, out NavMeshHit hit, 1f, NavMesh.AllAreas) ? hit.position : p;

    // Same algorithm as PathDebugDraw.Update/SegmentCost (0.25 m steps), with the global area costs
    static float ScreenCost(Vector3[] c, out float length)
    {
        length = 0f;
        float cost = 0f;
        for (int i = 0; i < c.Length - 1; i++)
        {
            float segLen = Vector3.Distance(c[i], c[i + 1]);
            length += segLen;
            if (IsLink(c[i], c[i + 1])) { cost += segLen * NavMesh.GetAreaCost(link.area); continue; }
            int steps = Mathf.Max(1, Mathf.CeilToInt(segLen / 0.25f));
            float stepLen = segLen / steps;
            for (int s = 0; s < steps; s++)
            {
                Vector3 p = Vector3.Lerp(c[i], c[i + 1], (s + 0.5f) / steps);
                int area = 0;
                if (NavMesh.SamplePosition(p, out NavMeshHit hit, 0.5f, NavMesh.AllAreas))
                    area = PathDebugDraw.AreaIndex(hit.mask);
                cost += stepLen * NavMesh.GetAreaCost(area);
            }
        }
        return cost;
    }

    // Fine split of a path into walkable / mud / water / link lengths
    static Breakdown Split(Vector3[] c)
    {
        var b = new Breakdown { letter = RouteClassifier.Letter(c), corners = c };
        for (int i = 0; i < c.Length - 1; i++)
        {
            float segLen = Vector3.Distance(c[i], c[i + 1]);
            b.total += segLen;
            if (IsLink(c[i], c[i + 1])) { b.jump += segLen; continue; }
            int steps = Mathf.Max(1, Mathf.CeilToInt(segLen / 0.02f));
            float stepLen = segLen / steps;
            for (int s = 0; s < steps; s++)
            {
                Vector3 p = Vector3.Lerp(c[i], c[i + 1], (s + 0.5f) / steps);
                int area = 0;
                if (NavMesh.SamplePosition(p, out NavMeshHit hit, 0.5f, NavMesh.AllAreas))
                    area = PathDebugDraw.AreaIndex(hit.mask);
                if (area == mudArea) b.mud += stepLen;
                else if (area == waterArea) b.water += stepLen;
                else b.walk += stepLen;
            }
        }
        return b;
    }

    static bool IsLink(Vector3 a, Vector3 b)
    {
        if (link == null || !link.activated) return false;
        Vector3 s = link.transform.TransformPoint(link.startPoint);
        Vector3 e = link.transform.TransformPoint(link.endPoint);
        return (Near(a, s) && Near(b, e)) || (Near(a, e) && Near(b, s));
    }

    static bool Near(Vector3 p, Vector3 q) { p.y = 0f; q.y = 0f; return Vector3.Distance(p, q) < 0.3f; }

    static string F(float v) => float.IsNaN(v) ? "*" : v.ToString("0.00", Inv);
    static string V(Vector3 v) => $"({F(v.x)}, {F(v.z)})";
    static string Corners(Vector3[] c) { var s = new List<string>(); foreach (var p in c) s.Add(V(p)); return string.Join(" -> ", s); }
    static string Join(Dictionary<int, int> d) { var s = new List<string>(); foreach (var kv in d) s.Add($"area {kv.Key}: {kv.Value}"); return string.Join(", ", s); }
}
