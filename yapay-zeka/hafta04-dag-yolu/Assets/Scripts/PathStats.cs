using System.Collections.Generic;
using UnityEngine;

/// <summary>Numbers reported for a path (Deney 1 / Deney 2 tables, HUD, console).</summary>
public struct PathStats
{
    public bool found;
    public int steps;            // moves = cells - 1
    public int expanded;         // closed list size
    public float cost;           // g(goal) as found by the search
    public float length2D;       // horizontal length, m
    public float length3D;       // length along the surface (cell centre to cell centre), m
    public float maxSlope;       // steepest cell on the path, degrees
    public float meanSlope;      // average slope of the cells on the path, degrees
    public float ascent;         // total climb, m
    public float descent;        // total descent, m
    public double ms;            // search time

    public static PathStats Of(GridMap grid, IList<Vector2Int> path, AStar search)
    {
        var s = new PathStats();
        if (search != null) { s.expanded = search.Expanded; s.cost = search.PathCost; s.ms = search.Milliseconds; }
        if (path == null || path.Count == 0) return s;
        s.found = true;
        s.steps = path.Count - 1;
        float slopeSum = 0f;
        for (int i = 0; i < path.Count; i++)
        {
            float sl = grid.Slope(path[i]);
            slopeSum += sl;
            s.maxSlope = Mathf.Max(s.maxSlope, sl);
            if (i == 0) continue;
            Vector3 a = grid.CellToWorld(path[i - 1]), b = grid.CellToWorld(path[i]);
            float flat = new Vector2(b.x - a.x, b.z - a.z).magnitude;
            float rise = b.y - a.y;
            s.length2D += flat;
            s.length3D += Mathf.Sqrt(flat * flat + rise * rise);
            if (rise > 0f) s.ascent += rise; else s.descent -= rise;
        }
        s.meanSlope = slopeSum / path.Count;
        return s;
    }

    public override string ToString() => found
        ? $"{expanded} nodes, {steps} steps, cost {cost:F2}, length {length3D:F1} m (2D {length2D:F1} m), max slope {maxSlope:F1} deg, mean slope {meanSlope:F1} deg, {ms:F2} ms"
        : $"no path, {expanded} nodes expanded";
}
