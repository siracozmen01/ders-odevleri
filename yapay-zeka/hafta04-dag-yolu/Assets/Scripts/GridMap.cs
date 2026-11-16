using System;
using UnityEngine;

/// <summary>
/// Grid laid over a Terrain (the "navigation mesh" of this homework).
/// Every cell gets a traversal cost from the terrain slope:
///     c(s) = 1 + k * (s / s_max)^2   if s <= s_max
///     c(s) = infinity                if s >  s_max   (impassable)
/// The step cost between two neighbours is  stepLength * c(entered cell)
/// (straight 1, diagonal sqrt(2) in cell units), optionally multiplied by an
/// uphill penalty (bonus task: direction-dependent edge cost).
/// </summary>
[ExecuteAlways]
public class GridMap : MonoBehaviour
{
    [Header("Grid (origin = this transform, x/z = grid x/y)")]
    public Terrain terrain;
    [Range(4, 150)] public int width = 128;
    [Range(4, 150)] public int depth = 128;
    [Min(0.1f)] public float cellSize = 1f;

    [Header("Slope cost  c(s) = 1 + k (s / s_max)^2 ;  s > s_max: impassable")]
    [Min(0f)] public float k = 4f;
    [Range(1f, 89f)] public float sMax = 35f;

    [Header("Bonus: uphill is expensive (direction-dependent edge cost)")]
    public bool uphillExpensive = false;
    [Tooltip("Uphill step cost is multiplied by 1 + uphillPenalty * grade (grade = rise / run). Downhill is unchanged.")]
    [Min(0f)] public float uphillPenalty = 3f;

    [Header("Gizmos (TODO 2)")]
    public bool drawCostMap = true;
    public float gizmoLift = 0.15f;
    [Tooltip("Cost drawn fully red. Fixed (not 1 + k) so that a larger k visibly reddens the map; 5 = cost at s = s_max for k = 4.")]
    [Min(1.01f)] public float colorMaxCost = 5f;

    /// <summary>Raised whenever k, s_max or the uphill settings change (Inspector or code).</summary>
    public event Action Changed;

    /// <summary>Lowest possible cost of entering a cell; the heuristic is scaled by it to stay admissible.</summary>
    public const float MinCellCost = 1f;

    float[,] slope;    // degrees, from TerrainData.GetSteepness
    float[,] height;   // world y of the cell centre
    float[,] cost;     // cached SlopeCost(slope), +inf = impassable
    float cachedK = -1f, cachedSMax = -1f;

    // ------------------------------------------------------------------ setup

    void OnEnable() => Scan();

    void OnValidate()
    {
        if (slope == null) return;           // not scanned yet (first import)
        NotifyChanged();
    }

    /// <summary>Reads slope and height of every cell from the terrain (call again if the terrain changes).</summary>
    public void Scan()
    {
        slope = new float[width, depth];
        height = new float[width, depth];
        for (int x = 0; x < width; x++)
            for (int z = 0; z < depth; z++)
            {
                Vector3 p = CellToWorldFlat(new Vector2Int(x, z));
                slope[x, z] = SampleSteepness(p);
                height[x, z] = GroundHeight(p);
            }
        RecomputeCosts();
    }

    /// <summary>Call after changing k / s_max / uphill settings from code; refreshes costs and raises Changed.</summary>
    public void NotifyChanged()
    {
        RecomputeCosts();
        Changed?.Invoke();
    }

    void RecomputeCosts()
    {
        if (slope == null || slope.GetLength(0) != width || slope.GetLength(1) != depth) { Scan(); return; }
        cost = new float[width, depth];
        for (int x = 0; x < width; x++)
            for (int z = 0; z < depth; z++)
                cost[x, z] = SlopeCost(slope[x, z]);
        cachedK = k;
        cachedSMax = sMax;
    }

    void EnsureCosts()
    {
        if (slope == null || cost == null || cachedK != k || cachedSMax != sMax) RecomputeCosts();
    }

    // ------------------------------------------------------------------ TODO 1

    /// <summary>
    /// TODO 1: cost of a cell from its slope in degrees.
    /// c(s) = 1 + k (s / s_max)^2 for s &lt;= s_max, +infinity (impassable) above.
    /// Example: s = 20, s_max = 35, k = 4  ->  1 + 4 * (20/35)^2 = 2.306.
    /// </summary>
    public float SlopeCost(float slopeDegrees)
    {
        if (slopeDegrees > sMax) return float.PositiveInfinity;
        float r = slopeDegrees / sMax;
        return 1f + k * r * r;
    }

    /// <summary>Terrain steepness in degrees at a world position (TerrainData.GetSteepness uses normalised 0..1 coordinates).</summary>
    public float SampleSteepness(Vector3 world)
    {
        if (terrain == null) return 0f;
        TerrainData data = terrain.terrainData;
        Vector3 local = world - terrain.transform.position;
        float u = Mathf.Clamp01(local.x / data.size.x);
        float v = Mathf.Clamp01(local.z / data.size.z);
        return data.GetSteepness(u, v);
    }

    // ------------------------------------------------------------------ queries

    public bool InBounds(Vector2Int c) => c.x >= 0 && c.y >= 0 && c.x < width && c.y < depth;

    public bool IsWalkable(Vector2Int c)
    {
        if (!InBounds(c)) return false;
        EnsureCosts();
        return !float.IsInfinity(cost[c.x, c.y]);
    }

    /// <summary>Cost of entering the cell (+inf if impassable or outside).</summary>
    public float Cost(Vector2Int c)
    {
        if (!InBounds(c)) return float.PositiveInfinity;
        EnsureCosts();
        return cost[c.x, c.y];
    }

    public float Slope(Vector2Int c)
    {
        EnsureCosts();
        return InBounds(c) ? slope[c.x, c.y] : 90f;
    }

    public float Height(Vector2Int c)
    {
        EnsureCosts();
        return InBounds(c) ? height[c.x, c.y] : 0f;
    }

    /// <summary>Multiplier for climbing: 1 + uphillPenalty * max(0, rise / run). 1 when the bonus is off.</summary>
    public float UphillFactor(float rise, float run)
    {
        if (!uphillExpensive || rise <= 0f || run <= 1e-5f) return 1f;
        return 1f + uphillPenalty * (rise / run);
    }

    /// <summary>
    /// Cost of one grid step from a to its neighbour b: length (cellSize or sqrt(2) cellSize) times the
    /// cost of the entered cell, times the uphill factor when that bonus is on.
    /// </summary>
    public float StepCost(Vector2Int a, Vector2Int b)
    {
        bool diagonal = a.x != b.x && a.y != b.y;
        float run = (diagonal ? 1.41421356f : 1f) * cellSize;
        float c = run * Cost(b);
        if (uphillExpensive) c *= UphillFactor(Height(b) - Height(a), run);
        return c;
    }

    /// <summary>
    /// TODO 4 helper: how many times slower than on flat ground an agent moves at this position
    /// in this horizontal direction (cell cost, times uphill factor when that bonus is on).
    /// </summary>
    public float MoveCostFactor(Vector3 world, Vector3 horizontalDir)
    {
        Vector2Int cell = WorldToCell(world);
        float c = Cost(cell);
        if (float.IsInfinity(c)) c = 1f + k;                 // should not happen; never freeze the agent
        if (uphillExpensive && horizontalDir.sqrMagnitude > 1e-6f)
        {
            const float probe = 0.5f;
            float rise = GroundHeight(world + horizontalDir.normalized * probe) - GroundHeight(world);
            c *= UphillFactor(rise, probe);
        }
        return c;
    }

    // ------------------------------------------------------------------ conversions

    Vector3 CellToWorldFlat(Vector2Int c) =>
        transform.position + new Vector3((c.x + 0.5f) * cellSize, 0f, (c.y + 0.5f) * cellSize);

    /// <summary>Grid -> world: centre of the cell, on the terrain surface (week 3: SampleHeight).</summary>
    public Vector3 CellToWorld(Vector2Int c)
    {
        Vector3 p = CellToWorldFlat(c);
        p.y = GroundHeight(p);
        return p;
    }

    public Vector2Int WorldToCell(Vector3 world)
    {
        Vector3 local = world - transform.position;
        return new Vector2Int(Mathf.FloorToInt(local.x / cellSize), Mathf.FloorToInt(local.z / cellSize));
    }

    /// <summary>Terrain height at a world position (terrain.transform.position.y + SampleHeight).</summary>
    public float GroundHeight(Vector3 world)
    {
        if (terrain == null) return transform.position.y;
        return terrain.transform.position.y + terrain.SampleHeight(world);
    }

    // ------------------------------------------------------------------ TODO 2 (colours shared with the runtime overlay)

    /// <summary>
    /// TODO 2 colour scale: green (cost 1) -> yellow -> red (cost >= colorMaxCost), black = impassable.
    /// The scale is fixed, so k = 0 is all green and k = 8 turns the steep cells red.
    /// </summary>
    public Color CostColor(Vector2Int c)
    {
        float v = Cost(c);
        if (float.IsInfinity(v)) return Color.black;
        float t = Mathf.Clamp01((v - 1f) / (colorMaxCost - 1f));
        return Color.HSVToRGB(Mathf.Lerp(0.33f, 0f, t), 0.9f, 0.95f);
    }

    void OnDrawGizmos()
    {
        if (!drawCostMap || terrain == null) return;
        EnsureCosts();
        Vector3 size = new Vector3(cellSize * 0.92f, 0.05f, cellSize * 0.92f);
        for (int x = 0; x < width; x++)
            for (int z = 0; z < depth; z++)
            {
                var c = new Vector2Int(x, z);
                Color col = CostColor(c);
                col.a = 0.6f;
                Gizmos.color = col;
                Gizmos.DrawCube(CellToWorld(c) + Vector3.up * gizmoLift, size);
            }
    }
}
