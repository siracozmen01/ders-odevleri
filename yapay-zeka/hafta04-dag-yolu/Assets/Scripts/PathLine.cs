using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Draws a grid path as a LineRenderer that follows the terrain (runtime counterpart of the path gizmo,
/// visible in the Game view and in screenshots). Long smoothed segments are sampled every
/// <see cref="sampleSpacing"/> metres so the line does not cut through hills.
/// </summary>
[RequireComponent(typeof(LineRenderer))]
public class PathLine : MonoBehaviour
{
    public float lift = 0.5f;
    public float sampleSpacing = 0.5f;

    LineRenderer line;
    readonly List<Vector3> points = new List<Vector3>();

    LineRenderer Line => line != null ? line : (line = GetComponent<LineRenderer>());

    public void Draw(GridMap grid, IList<Vector2Int> path)
    {
        points.Clear();
        if (path == null || path.Count == 0) { Hide(); return; }
        for (int i = 0; i < path.Count; i++)
        {
            Vector3 a = grid.CellToWorld(path[i]);
            if (i > 0)
            {
                Vector3 prev = grid.CellToWorld(path[i - 1]);
                int samples = Mathf.CeilToInt(Vector3.Distance(prev, a) / sampleSpacing);
                for (int s = 1; s < samples; s++)
                {
                    Vector3 p = Vector3.Lerp(prev, a, s / (float)samples);
                    p.y = grid.GroundHeight(p);
                    points.Add(p + Vector3.up * lift);
                }
            }
            points.Add(a + Vector3.up * lift);
        }
        Line.useWorldSpace = true;
        Line.positionCount = points.Count;
        Line.SetPositions(points.ToArray());
    }

    public void Hide() => Line.positionCount = 0;
}
