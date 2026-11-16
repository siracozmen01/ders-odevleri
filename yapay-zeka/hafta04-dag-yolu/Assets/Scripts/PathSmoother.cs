using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Bonus task: path smoothing (string pulling) on the cost grid.
/// A grid path zig-zags through cell centres. We drop intermediate points when the straight segment
/// between two points (1) only crosses walkable cells, never cutting a blocked corner - a grid
/// "line of sight" test, the ray cast of the Gozcu homework done on the grid - and (2) is not more
/// expensive than the piece of grid path it replaces. Condition (2) keeps the smoothed path from
/// taking a straight short cut across a steep slope the cost function wanted to avoid.
/// </summary>
public static class PathSmoother
{
    public static List<Vector2Int> Smooth(GridMap grid, IList<Vector2Int> path)
    {
        var result = new List<Vector2Int>();
        if (path == null || path.Count == 0) return result;
        int n = path.Count;

        // prefix[i] = continuous cost of the grid polyline from path[0] to path[i]
        var prefix = new float[n];
        for (int i = 1; i < n; i++)
            prefix[i] = prefix[i - 1] + SegmentCost(grid, path[i - 1], path[i], out _);

        result.Add(path[0]);
        int a = 0;
        while (a < n - 1)
        {
            int best = a + 1;
            for (int b = n - 1; b > a + 1; b--)                  // farthest visible point first
            {
                float direct = SegmentCost(grid, path[a], path[b], out bool blocked);
                if (blocked) continue;
                if (direct <= prefix[b] - prefix[a] + 1e-4f) { best = b; break; }
            }
            result.Add(path[best]);
            a = best;
        }
        return result;
    }

    /// <summary>Continuous cost of a polyline through cell centres (same measure for raw and smoothed paths).</summary>
    public static float PolylineCost(GridMap grid, IList<Vector2Int> pts)
    {
        float sum = 0f;
        for (int i = 1; i < pts.Count; i++) sum += SegmentCost(grid, pts[i - 1], pts[i], out _);
        return sum;
    }

    /// <summary>
    /// Walks the straight segment between two cell centres cell by cell (Amanatides-Woo traversal) and sums
    /// length-in-cell * cost(cell) (times the uphill factor of that piece when that bonus is on).
    /// blocked = the segment touches an impassable cell or squeezes diagonally between two blocked cells.
    /// </summary>
    public static float SegmentCost(GridMap grid, Vector2Int a, Vector2Int b, out bool blocked)
    {
        blocked = false;
        var p0 = new Vector2(a.x + 0.5f, a.y + 0.5f);
        var p1 = new Vector2(b.x + 0.5f, b.y + 0.5f);
        Vector2 d = p1 - p0;
        float lenCells = d.magnitude;
        if (lenCells < 1e-6f) return 0f;

        int x = a.x, y = a.y;
        int stepX = d.x > 0 ? 1 : d.x < 0 ? -1 : 0;
        int stepY = d.y > 0 ? 1 : d.y < 0 ? -1 : 0;
        float tDeltaX = stepX != 0 ? 1f / Mathf.Abs(d.x) : float.PositiveInfinity;
        float tDeltaY = stepY != 0 ? 1f / Mathf.Abs(d.y) : float.PositiveInfinity;
        float tMaxX = stepX != 0 ? 0.5f * tDeltaX : float.PositiveInfinity;   // start at a cell centre
        float tMaxY = stepY != 0 ? 0.5f * tDeltaY : float.PositiveInfinity;

        float t = 0f, total = 0f;
        Vector3 origin = grid.transform.position;
        float cs = grid.cellSize;
        Vector3 World(float tt) => origin + new Vector3((p0.x + d.x * tt) * cs, 0f, (p0.y + d.y * tt) * cs);

        while (true)
        {
            var cell = new Vector2Int(x, y);
            if (!grid.IsWalkable(cell)) { blocked = true; return float.PositiveInfinity; }

            float tNext = Mathf.Min(1f, Mathf.Min(tMaxX, tMaxY));
            float run = (tNext - t) * lenCells * cs;
            if (run > 0f)
            {
                float c = grid.Cost(cell) * run;
                if (grid.uphillExpensive)
                    c *= grid.UphillFactor(grid.GroundHeight(World(tNext)) - grid.GroundHeight(World(t)), run);
                total += c;
            }
            if (tNext >= 1f) break;

            if (Mathf.Abs(tMaxX - tMaxY) < 1e-5f)
            {
                // passing exactly through a corner: same no-corner-cutting rule as the 8-neighbour A*
                if (!grid.IsWalkable(new Vector2Int(x + stepX, y)) || !grid.IsWalkable(new Vector2Int(x, y + stepY)))
                { blocked = true; return float.PositiveInfinity; }
                x += stepX; y += stepY;
                tMaxX += tDeltaX; tMaxY += tDeltaY;
            }
            else if (tMaxX < tMaxY) { x += stepX; tMaxX += tDeltaX; }
            else { y += stepY; tMaxY += tDeltaY; }
            t = tNext;
        }
        return total;
    }
}
