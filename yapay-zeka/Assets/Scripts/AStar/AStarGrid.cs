using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Grid + A* pathfinding written from scratch (no NavMesh).
///  - The world is split into square nodes; a node is unwalkable if an obstacle overlaps it.
///  - A*: f(n) = g(n) + h(n)
///      g = real cost from the start, h = octile-distance estimate to the goal.
///  - 8-directional movement, diagonal cost = 14, straight = 10; no corner cutting.
/// The last search is visualized: open set (green), closed set (red), path (blue).
/// </summary>
public class AStarGrid : MonoBehaviour
{
    public class Node : IHeapItem<Node>
    {
        public bool walkable;
        public Vector3 worldPos;
        public int x, y;
        public int gCost, hCost;
        public Node parent;
        public int fCost => gCost + hCost;
        public int HeapIndex { get; set; }

        public int CompareTo(Node other)
        {
            int c = fCost.CompareTo(other.fCost);
            if (c == 0) c = hCost.CompareTo(other.hCost);
            return -c; // lower f = higher priority
        }
    }

    public Vector2 gridWorldSize = new Vector2(40, 40);
    public float nodeRadius = 0.5f;
    public LayerMask obstacleMask;
    public bool allowDiagonal = true;
    public bool showGrid = true;

    public Node[,] Grid { get; private set; }
    public int SizeX { get; private set; }
    public int SizeY { get; private set; }

    // results of the last search, for visualization / UI
    public readonly HashSet<Node> LastOpen = new HashSet<Node>();
    public readonly HashSet<Node> LastClosed = new HashSet<Node>();
    public List<Node> LastPath = new List<Node>();
    public float LastSearchMs { get; private set; }

    float nodeDiameter;

    void Awake() { CreateGrid(); }

    public void CreateGrid()
    {
        nodeDiameter = nodeRadius * 2f;
        SizeX = Mathf.RoundToInt(gridWorldSize.x / nodeDiameter);
        SizeY = Mathf.RoundToInt(gridWorldSize.y / nodeDiameter);
        Grid = new Node[SizeX, SizeY];
        Vector3 bottomLeft = transform.position - Vector3.right * gridWorldSize.x / 2f - Vector3.forward * gridWorldSize.y / 2f;

        for (int x = 0; x < SizeX; x++)
            for (int y = 0; y < SizeY; y++)
            {
                Vector3 p = bottomLeft + Vector3.right * (x * nodeDiameter + nodeRadius) + Vector3.forward * (y * nodeDiameter + nodeRadius);
                bool walkable = !Physics.CheckBox(p + Vector3.up * 0.5f, new Vector3(nodeRadius * 0.9f, 0.4f, nodeRadius * 0.9f), Quaternion.identity, obstacleMask, QueryTriggerInteraction.Ignore);
                Grid[x, y] = new Node { walkable = walkable, worldPos = p, x = x, y = y };
            }
    }

    public Node NodeFromWorld(Vector3 world)
    {
        Vector3 local = world - transform.position;
        float px = Mathf.Clamp01((local.x + gridWorldSize.x / 2f) / gridWorldSize.x);
        float py = Mathf.Clamp01((local.z + gridWorldSize.y / 2f) / gridWorldSize.y);
        int x = Mathf.Clamp(Mathf.FloorToInt(px * SizeX), 0, SizeX - 1);
        int y = Mathf.Clamp(Mathf.FloorToInt(py * SizeY), 0, SizeY - 1);
        return Grid[x, y];
    }

    IEnumerable<Node> Neighbours(Node n)
    {
        for (int dx = -1; dx <= 1; dx++)
            for (int dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0) continue;
                bool diagonal = dx != 0 && dy != 0;
                if (diagonal && !allowDiagonal) continue;
                int x = n.x + dx, y = n.y + dy;
                if (x < 0 || y < 0 || x >= SizeX || y >= SizeY) continue;
                // no corner cutting: both side cells must be free for a diagonal step
                if (diagonal && (!Grid[n.x + dx, n.y].walkable || !Grid[n.x, n.y + dy].walkable)) continue;
                yield return Grid[x, y];
            }
    }

    static int Distance(Node a, Node b)
    {
        int dx = Mathf.Abs(a.x - b.x);
        int dy = Mathf.Abs(a.y - b.y);
        return dx > dy ? 14 * dy + 10 * (dx - dy) : 14 * dx + 10 * (dy - dx); // octile
    }

    /// <summary>Runs A* and returns the list of world points, or null if no path exists.</summary>
    public List<Vector3> FindPath(Vector3 startPos, Vector3 targetPos)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        LastOpen.Clear(); LastClosed.Clear(); LastPath.Clear();

        Node start = NodeFromWorld(startPos);
        Node target = NodeFromWorld(targetPos);
        if (!target.walkable) { LastSearchMs = 0; return null; }

        foreach (var n in Grid) { n.gCost = int.MaxValue; n.hCost = 0; n.parent = null; }

        var open = new Heap<Node>(SizeX * SizeY);
        start.gCost = 0;
        start.hCost = Distance(start, target);
        open.Add(start);
        LastOpen.Add(start);

        while (open.Count > 0)
        {
            Node current = open.RemoveFirst();       // node with the lowest f cost
            LastOpen.Remove(current);
            LastClosed.Add(current);

            if (current == target)
            {
                LastPath = Retrace(start, target);
                LastSearchMs = (float)sw.Elapsed.TotalMilliseconds;
                var points = new List<Vector3>();
                foreach (var n in LastPath) points.Add(n.worldPos);
                return points;
            }

            foreach (Node nb in Neighbours(current))
            {
                if (!nb.walkable || LastClosed.Contains(nb)) continue;

                int newG = current.gCost + Distance(current, nb);
                if (newG < nb.gCost)
                {
                    nb.gCost = newG;
                    nb.hCost = Distance(nb, target);
                    nb.parent = current;
                    if (!open.Contains(nb)) { open.Add(nb); LastOpen.Add(nb); }
                    else open.UpdateItem(nb);
                }
            }
        }

        LastSearchMs = (float)sw.Elapsed.TotalMilliseconds;
        return null; // target unreachable
    }

    static List<Node> Retrace(Node start, Node end)
    {
        var path = new List<Node>();
        for (Node n = end; n != start; n = n.parent) path.Add(n);
        path.Reverse();
        return path;
    }

    void OnDrawGizmos()
    {
        Gizmos.DrawWireCube(transform.position, new Vector3(gridWorldSize.x, 0.1f, gridWorldSize.y));
        if (Grid == null || !showGrid) return;
        float d = nodeRadius * 2f * 0.9f;
        foreach (Node n in Grid)
        {
            Color c = n.walkable ? new Color(1, 1, 1, 0.15f) : new Color(0, 0, 0, 0.6f);
            if (LastClosed.Contains(n)) c = new Color(1f, 0.3f, 0.3f, 0.5f);
            if (LastOpen.Contains(n)) c = new Color(0.3f, 1f, 0.3f, 0.6f);
            if (LastPath.Contains(n)) c = new Color(0.2f, 0.5f, 1f, 0.9f);
            Gizmos.color = c;
            Gizmos.DrawCube(n.worldPos, new Vector3(d, 0.05f, d));
        }
    }
}
