using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The red NPC: periodically asks our own A* for a path to the flag and walks it cell by cell
/// (the in-class NpcGridChase.cs, extended for the homework).
/// TODO 4: the speed is divided by the cost of the cell the NPC stands on, so it visibly slows down on steep cells.
/// </summary>
public class NpcGridChase : MonoBehaviour
{
    public GridMap grid;
    public Transform target;
    public PathLine pathLine;

    [Header("Search")]
    public SearchMode mode = SearchMode.AStar;
    public Heuristic heuristic = Heuristic.Octile;
    [Min(0f)] public float weight = 1f;
    [Tooltip("Bonus: binary heap open list instead of scanning a List")]
    public bool useBinaryHeap = true;
    [Tooltip("Bonus: drop zig-zag points when a straight segment is walkable and not more expensive")]
    public bool smoothPath = false;
    public float repathInterval = 0.5f;

    [Header("Movement (TODO 4: speed / cell cost)")]
    [Min(0f)] public float speed = 4f;        // m/s on flat ground (cost 1)
    public float hover = 0.5f;                // sphere radius: centre above the ground
    public bool logSearches = true;

    [Header("Gizmos")]
    public bool drawClosedList = true;
    public bool drawPath = true;

    /// <summary>Current speed after TODO 4 (m/s) and the cost factor it was divided by.</summary>
    public float CurrentSpeed { get; private set; }
    public float CurrentCostFactor { get; private set; } = 1f;
    public PathStats LastStats { get; private set; }
    public List<Vector2Int> Path => path;
    public List<Vector2Int> RawPath => rawPath;
    public AStar Search => search;
    public bool Arrived => path != null && index >= path.Count;

    readonly AStar search = new AStar();
    List<Vector2Int> rawPath, path;
    int index;
    float timer;
    string lastLog;

    void OnEnable()
    {
        if (grid != null) grid.Changed += Repath;
        timer = 0f;
    }

    void OnDisable()
    {
        if (grid != null) grid.Changed -= Repath;
    }

    void Update()
    {
        timer -= Time.deltaTime;                                   // 1. re-plan regularly (the flag can be moved)
        if (timer <= 0f) { Repath(); timer = repathInterval; }
        Tick(Time.deltaTime);                                      // 2. follow the path
    }

    /// <summary>One movement step; public so the editor experiments can simulate the walk deterministically.</summary>
    public void Tick(float dt)
    {
        if (path == null || index >= path.Count) { CurrentSpeed = 0f; return; }

        Vector3 pos = transform.position;
        float timeLeft = dt;
        while (timeLeft > 1e-6f && index < path.Count)
        {
            Vector3 next = grid.CellToWorld(path[index]);
            var flat = new Vector3(next.x - pos.x, 0f, next.z - pos.z);
            float dist = flat.magnitude;
            Vector3 dir = dist > 1e-5f ? flat / dist : Vector3.zero;

            // TODO 4: slower on steep cells. speed / c(cell); c = 1 on flat ground, 1 + k at s = s_max.
            CurrentCostFactor = grid.MoveCostFactor(pos, dir);
            CurrentSpeed = speed / CurrentCostFactor;
            if (CurrentSpeed <= 0f) break;

            float stepLength = CurrentSpeed * timeLeft;
            if (stepLength >= dist)                                // reached the waypoint: keep the rest of the frame
            {
                pos.x = next.x; pos.z = next.z;
                timeLeft -= dist / CurrentSpeed;
                index++;
            }
            else
            {
                pos += dir * stepLength;
                timeLeft = 0f;
            }
        }

        pos.y = grid.GroundHeight(pos) + hover;                    // stay on the terrain
        transform.position = pos;
    }

    public void Repath()
    {
        if (grid == null || target == null) return;
        search.mode = mode;
        search.heuristic = heuristic;
        search.weight = weight;
        search.useBinaryHeap = useBinaryHeap;
        search.eightNeighbours = true;

        Vector2Int start = grid.WorldToCell(transform.position);
        Vector2Int goal = grid.WorldToCell(target.position);
        rawPath = search.FindPath(grid, start, goal);
        path = smoothPath && rawPath != null ? PathSmoother.Smooth(grid, rawPath) : rawPath;
        index = 1;                                                 // path[0]: the cell we are in
        LastStats = PathStats.Of(grid, rawPath, search);

        if (pathLine != null) pathLine.Draw(grid, path);

        if (logSearches)
        {
            string settings = $"{mode} {heuristic} w={weight} k={grid.k} s_max={grid.sMax} uphill={grid.uphillExpensive} smooth={smoothPath} heap={useBinaryHeap} goal={goal}";
            if (settings != lastLog)                               // log once per new setting / goal, not every 0.5 s
            {
                lastLog = settings;
                string smooth = smoothPath && path != null ? $", smoothed to {path.Count} waypoints" : "";
                Debug.Log($"[NpcGridChase] {settings}: {LastStats}{smooth}");
            }
        }
    }

    /// <summary>Put the NPC on a cell centre and forget the current path.</summary>
    public void Teleport(Vector2Int cell)
    {
        transform.position = grid.CellToWorld(cell) + Vector3.up * hover;
        path = rawPath = null;
        timer = 0f;
    }

    void OnDrawGizmos()
    {
        if (grid == null) return;
        if (drawClosedList)
        {
            Gizmos.color = new Color(0.5f, 0.5f, 0.5f, 0.8f);
            var size = new Vector3(grid.cellSize * 0.4f, 0.1f, grid.cellSize * 0.4f);
            foreach (Vector2Int c in search.Closed) Gizmos.DrawCube(grid.CellToWorld(c) + Vector3.up * 0.3f, size);
        }
        if (drawPath && path != null)
        {
            Gizmos.color = Color.yellow;
            for (int i = 1; i < path.Count; i++)
                Gizmos.DrawLine(grid.CellToWorld(path[i - 1]) + Vector3.up * 0.5f, grid.CellToWorld(path[i]) + Vector3.up * 0.5f);
        }
    }
}
