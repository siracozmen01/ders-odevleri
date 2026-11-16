using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Runs Deney 1, Deney 2, the admissibility check, the bonus measurements and the TODO 4 walk simulation
/// on the saved scene, and writes everything to deney-ciktisi.txt (project root) and the Console.
/// Menu: Dag Yolu > Run Experiments   |   batch: -executeMethod DagYoluExperiments.RunAll
/// </summary>
public static class DagYoluExperiments
{
    const string OutFile = "deney-ciktisi.txt";
    static readonly StringBuilder sb = new StringBuilder();
    static void Line(string s = "") => sb.AppendLine(s);

    /// <summary>Batch entry point: rebuild the scene, then run every experiment.</summary>
    public static void BuildAndRunAll()
    {
        DagYoluBuilder.Build();
        RunAll();
    }

    /// <summary>Batch entry point: rebuild the scene, run every experiment, render the screenshots.</summary>
    public static void BuildRunCapture()
    {
        BuildAndRunAll();
        DagYoluScreenshots.CaptureAll();
    }

    [MenuItem("Dag Yolu/Run Experiments")]
    public static void RunAll()
    {
        sb.Clear();
        EditorSceneManager.OpenScene(DagYoluBuilder.ScenePath);
        var grid = Object.FindAnyObjectByType<GridMap>();
        var npc = Object.FindAnyObjectByType<NpcGridChase>();
        grid.uphillExpensive = false;
        grid.sMax = 35f;
        grid.Scan();
        Vector2Int start = grid.WorldToCell(npc.transform.position);
        Vector2Int goal = grid.WorldToCell(npc.target.position);

        Line("# Dag Yolu - experiment output");
        Line($"Unity {Application.unityVersion}, grid {grid.width} x {grid.depth}, cell {grid.cellSize} m, s_max = {grid.sMax} deg");
        Line($"NPC start cell {start} (slope {grid.Slope(start):F1} deg), flag cell {goal} (slope {grid.Slope(goal):F1} deg)");
        Line($"Straight-line distance start-goal: {Vector2.Distance(start, goal) * grid.cellSize:F1} m (octile {Octile(start, goal) * grid.cellSize:F1} m)");
        SlopeHistogram(grid);
        Line($"SlopeCost check: s = 20, s_max = 35, k = 4 -> {CostWith(grid, 4f, 35f, 20f):F3} (diagonal x sqrt2 = {CostWith(grid, 4f, 35f, 20f) * Mathf.Sqrt(2f):F3}); s = 30, k = 8 -> {CostWith(grid, 8f, 35f, 30f):F3}; s = 36 -> {CostWith(grid, 8f, 35f, 36f)}");
        Line();

        Deney1(grid, start, goal);
        Deney2(grid, start, goal);
        Admissibility(grid, goal);
        HeapBonus(grid, start, goal);
        SmoothingBonus(grid, start, goal);
        UphillBonus(grid, start, goal);
        SpeedSimulation(grid, npc, start);

        grid.k = 4f; grid.uphillExpensive = false; grid.NotifyChanged();
        File.WriteAllText(OutFile, sb.ToString());
        Debug.Log(sb.ToString());
        Debug.Log("[DagYoluExperiments] written to " + Path.GetFullPath(OutFile));
    }

    // ------------------------------------------------------------------ Deney 1

    static void Deney1(GridMap grid, Vector2Int start, Vector2Int goal)
    {
        Line("## Deney 1: k = 0, 2, 8 (octile, 8 neighbours, binary heap)  [k = 4 added for reference]");
        Line("| k | length 3D (m) | length 2D (m) | max slope (deg) | mean slope (deg) | path cost | expanded | steps |");
        Line("|---|---|---|---|---|---|---|---|");
        foreach (float k in new[] { 0f, 2f, 4f, 8f })
        {
            SetK(grid, k);
            var a = NewSearch(Heuristic.Octile, true);
            var path = a.FindPath(grid, start, goal);
            var s = PathStats.Of(grid, path, a);
            Line($"| {k} | {s.length3D:F1} | {s.length2D:F1} | {s.maxSlope:F1} | {s.meanSlope:F1} | {s.cost:F2} | {s.expanded} | {s.steps} |");
            // what the same path would cost under the other k values (shows the trade-off)
            sb.Append($"   cost of this k={k} path under k=0/2/4/8: ");
            foreach (float k2 in new[] { 0f, 2f, 4f, 8f }) { SetK(grid, k2); sb.Append($"{GridPathCost(grid, path):F1}  "); }
            Line($"| ascent {s.ascent:F1} m, descent {s.descent:F1} m");
        }
        Line();
    }

    // ------------------------------------------------------------------ Deney 2

    static void Deney2(GridMap grid, Vector2Int start, Vector2Int goal)
    {
        Line("## Deney 2: k = 4, heuristic comparison (Zero = Dijkstra gives the true cheapest cost)");
        RunHeuristics(grid, start, goal, 4f);
        Line("### Extra: same comparison for k = 0, 2, 8");
        foreach (float k in new[] { 0f, 2f, 8f }) RunHeuristics(grid, start, goal, k);
    }

    static void RunHeuristics(GridMap grid, Vector2Int start, Vector2Int goal, float k)
    {
        SetK(grid, k);
        var dijkstra = NewSearch(Heuristic.Zero, true);
        dijkstra.FindPath(grid, start, goal);
        float optimal = dijkstra.PathCost;
        Line($"k = {k}: optimal cost (Dijkstra) = {optimal:F3}");
        Line("| heuristic | path cost | expanded | steps | length 3D (m) | cheapest? | excess cost |");
        Line("|---|---|---|---|---|---|---|");
        foreach (Heuristic h in new[] { Heuristic.Octile, Heuristic.Manhattan, Heuristic.Euclid, Heuristic.Zero })
        {
            var a = NewSearch(h, true);
            var path = a.FindPath(grid, start, goal);
            var s = PathStats.Of(grid, path, a);
            bool cheapest = Mathf.Abs(s.cost - optimal) < 1e-3f;
            Line($"| {h} | {s.cost:F3} | {s.expanded} | {s.steps} | {s.length3D:F1} | {(cheapest ? "yes" : "NO")} | {s.cost - optimal:+0.000;-0.000;0} ({(s.cost / optimal - 1f) * 100f:F2}%) |");
        }
        Line();
    }

    // ------------------------------------------------------------------ admissibility

    /// <summary>Exact remaining cost h*(n) to the goal for every cell (reverse Dijkstra), then count where each heuristic overestimates.</summary>
    static void Admissibility(GridMap grid, Vector2Int goal)
    {
        Line("## Admissibility check: h(n) vs exact h*(n) (reverse Dijkstra from the flag over all reachable cells)");
        Line("Consistency: an edge n->m is inconsistent if h(n) > c(n,m) + h(m); then a closed node may hold a too-high g (our A* never reopens closed nodes).");
        Line("| k | heuristic | reachable cells | cells with h > h* | max h / h* | mean h / h* | inconsistent edges |");
        Line("|---|---|---|---|---|---|---|");
        foreach (float k in new[] { 0f, 4f, 8f })
        {
            SetK(grid, k);
            float[,] hStar = ReverseDijkstra(grid, goal);
            foreach (Heuristic h in new[] { Heuristic.Octile, Heuristic.Manhattan, Heuristic.Euclid })
            {
                int reachable = 0, over = 0;
                float maxRatio = 0f; double sumRatio = 0;
                for (int x = 0; x < grid.width; x++)
                    for (int z = 0; z < grid.depth; z++)
                    {
                        float hs = hStar[x, z];
                        var c = new Vector2Int(x, z);
                        if (float.IsInfinity(hs) || c == goal) continue;
                        reachable++;
                        float hv = HeuristicCells(h, c, goal) * grid.cellSize;
                        if (hv > hs + 1e-4f) over++;
                        float ratio = hv / hs;
                        maxRatio = Mathf.Max(maxRatio, ratio);
                        sumRatio += ratio;
                    }
                int edges = 0, bad = 0;
                for (int x = 0; x < grid.width; x++)
                    for (int z = 0; z < grid.depth; z++)
                    {
                        var n = new Vector2Int(x, z);
                        if (!grid.IsWalkable(n)) continue;
                        foreach (Vector2Int d in Dirs8)
                        {
                            Vector2Int m = n + d;
                            if (!grid.IsWalkable(m)) continue;
                            if (d.x != 0 && d.y != 0 &&
                                (!grid.IsWalkable(new Vector2Int(n.x + d.x, n.y)) || !grid.IsWalkable(new Vector2Int(n.x, n.y + d.y))))
                                continue;
                            edges++;
                            float hn = HeuristicCells(h, n, goal) * grid.cellSize, hm = HeuristicCells(h, m, goal) * grid.cellSize;
                            if (hn > grid.StepCost(n, m) + hm + 1e-4f) bad++;
                        }
                    }
                Line($"| {k} | {h} | {reachable} | {over} ({100f * over / reachable:F1}%) | {maxRatio:F3} | {sumRatio / reachable:F3} | {bad} / {edges} ({100f * bad / edges:F1}%) |");
            }
        }
        Line();
    }

    class RNode : IHeapItem { public Vector2Int c; public float d; public int HeapIndex { get; set; } }

    static readonly Vector2Int[] Dirs8 =
    {
        new Vector2Int(0, 1), new Vector2Int(1, 0), new Vector2Int(0, -1), new Vector2Int(-1, 0),
        new Vector2Int(1, 1), new Vector2Int(1, -1), new Vector2Int(-1, -1), new Vector2Int(-1, 1),
    };

    static float[,] ReverseDijkstra(GridMap grid, Vector2Int goal)
    {
        var dist = new float[grid.width, grid.depth];
        var nodes = new RNode[grid.width, grid.depth];
        var done = new bool[grid.width, grid.depth];
        for (int x = 0; x < grid.width; x++) for (int z = 0; z < grid.depth; z++) dist[x, z] = float.PositiveInfinity;
        var heap = new BinaryHeap<RNode>((a, b) => a.d.CompareTo(b.d));
        dist[goal.x, goal.y] = 0f;
        nodes[goal.x, goal.y] = new RNode { c = goal, d = 0f };
        heap.Push(nodes[goal.x, goal.y]);
        while (heap.Count > 0)
        {
            RNode u = heap.Pop();
            done[u.c.x, u.c.y] = true;
            foreach (Vector2Int d in Dirs8)
            {
                Vector2Int v = u.c + d;                       // predecessor v -> u (forward edge)
                if (!grid.IsWalkable(v) || done[v.x, v.y]) continue;
                if (d.x != 0 && d.y != 0 &&
                    (!grid.IsWalkable(new Vector2Int(u.c.x + d.x, u.c.y)) || !grid.IsWalkable(new Vector2Int(u.c.x, u.c.y + d.y))))
                    continue;                                 // same side cells as the forward diagonal
                float nd = u.d + grid.StepCost(v, u.c);
                if (nd < dist[v.x, v.y])
                {
                    dist[v.x, v.y] = nd;
                    if (nodes[v.x, v.y] == null) { nodes[v.x, v.y] = new RNode { c = v, d = nd }; heap.Push(nodes[v.x, v.y]); }
                    else { nodes[v.x, v.y].d = nd; heap.DecreaseKey(nodes[v.x, v.y]); }
                }
            }
        }
        return dist;
    }

    // ------------------------------------------------------------------ bonus: binary heap

    static void HeapBonus(GridMap grid, Vector2Int start, Vector2Int goal)
    {
        Line("## Bonus: binary heap open list vs List scan (same start/goal, 20 runs each, after 3 warm-up runs)");
        Line("| k | search | open list | expanded | steps | path cost | same path | mean ms | min ms |");
        Line("|---|---|---|---|---|---|---|---|---|");
        foreach (float k in new[] { 4f, 8f })
        {
            SetK(grid, k);
            foreach (Heuristic h in new[] { Heuristic.Octile, Heuristic.Zero })
            {
                List<Vector2Int> listPath = null;
                foreach (bool heap in new[] { false, true })
                {
                    var a = NewSearch(h, heap);
                    List<Vector2Int> path = null;
                    for (int i = 0; i < 3; i++) path = a.FindPath(grid, start, goal);
                    double sum = 0, min = double.MaxValue;
                    for (int i = 0; i < 20; i++) { path = a.FindPath(grid, start, goal); sum += a.Milliseconds; min = System.Math.Min(min, a.Milliseconds); }
                    if (!heap) listPath = path;
                    bool same = heap ? listPath.SequenceEqual(path) : true;
                    string name = h == Heuristic.Zero ? "Dijkstra" : "A* octile";
                    Line($"| {k} | {name} | {(heap ? "binary heap" : "List scan")} | {a.Expanded} | {path.Count - 1} | {a.PathCost:F3} | {(heap ? (same ? "yes" : "NO") : "-")} | {sum / 20:F2} | {min:F2} |");
                }
            }
        }
        Line();
    }

    // ------------------------------------------------------------------ bonus: smoothing

    static void SmoothingBonus(GridMap grid, Vector2Int start, Vector2Int goal)
    {
        Line("## Bonus: path smoothing (cost-aware string pulling). Costs below use the same continuous measure for both paths.");
        Line("| k | waypoints raw -> smoothed | length 2D raw -> smoothed (m) | continuous cost raw -> smoothed | change |");
        Line("|---|---|---|---|---|");
        foreach (float k in new[] { 0f, 2f, 4f, 8f })
        {
            SetK(grid, k);
            var a = NewSearch(Heuristic.Octile, true);
            var raw = a.FindPath(grid, start, goal);
            var t = System.Diagnostics.Stopwatch.StartNew();
            var smooth = PathSmoother.Smooth(grid, raw);
            t.Stop();
            float l0 = Length2D(grid, raw), l1 = Length2D(grid, smooth);
            float c0 = PathSmoother.PolylineCost(grid, raw), c1 = PathSmoother.PolylineCost(grid, smooth);
            Line($"| {k} | {raw.Count} -> {smooth.Count} | {l0:F1} -> {l1:F1} | {c0:F2} -> {c1:F2} | length {(l1 / l0 - 1f) * 100f:F1}%, cost {(c1 / c0 - 1f) * 100f:F1}%, {t.Elapsed.TotalMilliseconds:F1} ms |");
        }
        Line();
    }

    // ------------------------------------------------------------------ bonus: uphill

    static void UphillBonus(GridMap grid, Vector2Int start, Vector2Int goal)
    {
        Line($"## Bonus: uphill is expensive (step cost x (1 + {grid.uphillPenalty} * rise/run) when climbing), k = 4, octile");
        Line("| uphill | direction | path cost | length 3D (m) | ascent (m) | descent (m) | max slope | expanded | steps |");
        Line("|---|---|---|---|---|---|---|---|---|");
        SetK(grid, 4f);
        var paths = new Dictionary<string, List<Vector2Int>>();
        foreach (bool up in new[] { false, true })
        {
            grid.uphillExpensive = up;
            grid.NotifyChanged();
            foreach (bool forward in new[] { true, false })
            {
                var a = NewSearch(Heuristic.Octile, true);
                var path = forward ? a.FindPath(grid, start, goal) : a.FindPath(grid, goal, start);
                var s = PathStats.Of(grid, path, a);
                paths[$"{up}{forward}"] = forward ? path : Enumerable.Reverse(path).ToList();
                Line($"| {(up ? "on" : "off")} | {(forward ? "NPC -> flag" : "flag -> NPC")} | {s.cost:F2} | {s.length3D:F1} | {s.ascent:F1} | {s.descent:F1} | {s.maxSlope:F1} | {s.expanded} | {s.steps} |");
            }
        }
        Line($"off: forward and backward routes identical: {paths["FalseTrue"].SequenceEqual(paths["FalseFalse"])}, " +
             $"cells shared {Shared(paths["FalseTrue"], paths["FalseFalse"])}");
        Line($"on : forward and backward routes identical: {paths["TrueTrue"].SequenceEqual(paths["TrueFalse"])}, " +
             $"cells shared {Shared(paths["TrueTrue"], paths["TrueFalse"])}");
        Line($"forward route off vs on identical: {paths["FalseTrue"].SequenceEqual(paths["TrueTrue"])}, cells shared {Shared(paths["FalseTrue"], paths["TrueTrue"])}");
        grid.uphillExpensive = false;
        grid.NotifyChanged();
        Line();
    }

    static string Shared(List<Vector2Int> a, List<Vector2Int> b)
    {
        var set = new HashSet<Vector2Int>(b);
        return $"{a.Count(set.Contains)}/{a.Count}";
    }

    // ------------------------------------------------------------------ TODO 4 simulation

    static void SpeedSimulation(GridMap grid, NpcGridChase npc, Vector2Int start)
    {
        Line($"## TODO 4: simulated walk with NpcGridChase.Tick (dt = 0.02 s, base speed {npc.speed} m/s, speed = speed / cell cost)");
        Line("| k | smoothing | travel time (s) | 2D distance (m) | mean speed (m/s) | min speed (m/s) | max speed (m/s) | speed on steepest cell | time x speed vs continuous cost |");
        Line("|---|---|---|---|---|---|---|---|---|");
        Vector3 home = npc.transform.position;
        bool oldLog = npc.logSearches;
        npc.logSearches = false;
        foreach (float k in new[] { 0f, 4f, 8f })
            foreach (bool smooth in new[] { false, true })
            {
                SetK(grid, k);
                npc.Teleport(start);
                npc.smoothPath = smooth;
                npc.Repath();
                var route = new List<Vector2Int>(npc.Path);
                float time = 0f, dist = 0f, minV = float.MaxValue, maxV = 0f, steepest = -1f, steepV = 0f;
                Vector3 prev = npc.transform.position;
                while (!npc.Arrived && time < 2000f)
                {
                    npc.Tick(0.02f);
                    time += 0.02f;
                    Vector3 p = npc.transform.position;
                    dist += new Vector2(p.x - prev.x, p.z - prev.z).magnitude;
                    prev = p;
                    if (npc.CurrentSpeed > 0f) { minV = Mathf.Min(minV, npc.CurrentSpeed); maxV = Mathf.Max(maxV, npc.CurrentSpeed); }
                    float sl = grid.Slope(grid.WorldToCell(p));
                    if (sl > steepest) { steepest = sl; steepV = npc.CurrentSpeed; }
                }
                float cont = PathSmoother.PolylineCost(grid, route);
                Line($"| {k} | {(smooth ? "on" : "off")} | {time:F1} | {dist:F1} | {dist / time:F2} | {minV:F2} | {maxV:F2} | {steepV:F2} m/s at {steepest:F1} deg | {time * npc.speed:F1} vs {cont:F1} |");
            }
        npc.smoothPath = false;
        npc.logSearches = oldLog;
        npc.transform.position = home;
        Line();
    }

    // ------------------------------------------------------------------ helpers

    static AStar NewSearch(Heuristic h, bool heap) => new AStar
    {
        mode = h == Heuristic.Zero ? SearchMode.Dijkstra : SearchMode.AStar,
        heuristic = h, weight = 1f, eightNeighbours = true, useBinaryHeap = heap,
    };

    static void SetK(GridMap grid, float k) { grid.k = k; grid.NotifyChanged(); }

    static float CostWith(GridMap grid, float k, float sMax, float s)
    {
        float ok = grid.k, os = grid.sMax;
        grid.k = k; grid.sMax = sMax;
        float c = grid.SlopeCost(s);
        grid.k = ok; grid.sMax = os;
        return c;
    }

    static float Octile(Vector2Int a, Vector2Int b) => HeuristicCells(Heuristic.Octile, a, b);

    static float HeuristicCells(Heuristic h, Vector2Int a, Vector2Int b)
    {
        float dx = Mathf.Abs(a.x - b.x), dy = Mathf.Abs(a.y - b.y);
        switch (h)
        {
            case Heuristic.Octile: return Mathf.Max(dx, dy) + (Mathf.Sqrt(2f) - 1f) * Mathf.Min(dx, dy);
            case Heuristic.Manhattan: return dx + dy;
            case Heuristic.Euclid: return Mathf.Sqrt(dx * dx + dy * dy);
            default: return 0f;
        }
    }

    static float GridPathCost(GridMap grid, List<Vector2Int> path)
    {
        float c = 0f;
        for (int i = 1; i < path.Count; i++) c += grid.StepCost(path[i - 1], path[i]);
        return c;
    }

    static float Length2D(GridMap grid, List<Vector2Int> path)
    {
        float l = 0f;
        for (int i = 1; i < path.Count; i++) l += Vector2.Distance(path[i - 1], path[i]) * grid.cellSize;
        return l;
    }

    static void SlopeHistogram(GridMap grid)
    {
        int[] bins = new int[6];
        for (int x = 0; x < grid.width; x++)
            for (int z = 0; z < grid.depth; z++)
            {
                float s = grid.Slope(new Vector2Int(x, z));
                bins[s > grid.sMax ? 5 : Mathf.Min(4, (int)(s / 7f))]++;
            }
        Line($"Slope histogram (cells): 0-7: {bins[0]}, 7-14: {bins[1]}, 14-21: {bins[2]}, 21-28: {bins[3]}, 28-35: {bins[4]}, > 35 (impassable): {bins[5]}");
    }
}
