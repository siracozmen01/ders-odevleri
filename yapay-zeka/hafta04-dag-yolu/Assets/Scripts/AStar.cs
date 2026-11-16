using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

public enum SearchMode { Dijkstra, AStar, Greedy }

public enum Heuristic { Octile, Manhattan, Euclid, Zero }

/// <summary>
/// A* on a <see cref="GridMap"/> (the in-class AStar.cs extended for the homework).
/// TODO 3: 8 neighbours, no corner cutting, octile heuristic.
/// One loop, four searches: h = 0 Dijkstra, f = h greedy, f = g + w h (weighted) A*.
/// Not a MonoBehaviour: NpcGridChase and the editor experiments create their own instance.
/// </summary>
public class AStar
{
    public SearchMode mode = SearchMode.AStar;
    public Heuristic heuristic = Heuristic.Octile;
    public float weight = 1f;
    public bool eightNeighbours = true;
    public bool useBinaryHeap = false;     // bonus: binary heap instead of the linear List scan

    /// <summary>Closed list of the last search (expanded cells).</summary>
    public readonly HashSet<Vector2Int> Closed = new HashSet<Vector2Int>();
    public int Expanded => Closed.Count;
    /// <summary>g of the goal in the last successful search (path cost).</summary>
    public float PathCost { get; private set; }
    public double Milliseconds { get; private set; }

    const float Sqrt2 = 1.41421356f;

    class Node : IHeapItem
    {
        public Vector2Int cell;
        public float g, h;
        public Node parent;
        public int order;                   // insertion order: last tie-breaker, same as the List version
        public int HeapIndex { get; set; }
    }

    // Neighbour order: N, E, S, W (as in class), then the diagonals NE, SE, SW, NW.
    static readonly Vector2Int[] Dirs =
    {
        new Vector2Int(0, 1), new Vector2Int(1, 0), new Vector2Int(0, -1), new Vector2Int(-1, 0),
        new Vector2Int(1, 1), new Vector2Int(1, -1), new Vector2Int(-1, -1), new Vector2Int(-1, 1),
    };

    GridMap grid;

    public List<Vector2Int> FindPath(GridMap grid, Vector2Int start, Vector2Int goal)
    {
        this.grid = grid;
        var watch = Stopwatch.StartNew();
        List<Vector2Int> result = Search(start, goal);
        watch.Stop();
        Milliseconds = watch.Elapsed.TotalMilliseconds;
        return result;
    }

    List<Vector2Int> Search(Vector2Int start, Vector2Int goal)
    {
        var open = new List<Node>();                                    // open list (class version)
        var heap = new BinaryHeap<Node>(Compare);                       // open list (bonus version)
        var openByCell = new Dictionary<Vector2Int, Node>();            // "is it open?"
        Closed.Clear();
        PathCost = float.PositiveInfinity;
        int order = 0;

        var first = new Node { cell = start, g = 0f, h = H(start, goal), order = order++ };
        Add(first);

        int dirCount = eightNeighbours ? 8 : 4;
        while (openByCell.Count > 0)
        {
            Node current = PopBest();                                   // 1. smallest f, tie: smaller h, then older
            openByCell.Remove(current.cell);                            // 2. move to closed
            Closed.Add(current.cell);
            if (current.cell == goal)                                   // 3. goal: retrace parents
            {
                PathCost = current.g;
                return Retrace(current);
            }

            for (int i = 0; i < dirCount; i++)                          // 4. neighbours
            {
                Vector2Int d = Dirs[i];
                Vector2Int next = current.cell + d;
                if (!grid.IsWalkable(next) || Closed.Contains(next)) continue;
                // TODO 3: no corner cutting - a diagonal step needs both side cells walkable
                if (d.x != 0 && d.y != 0 &&
                    (!grid.IsWalkable(new Vector2Int(current.cell.x + d.x, current.cell.y)) ||
                     !grid.IsWalkable(new Vector2Int(current.cell.x, current.cell.y + d.y))))
                    continue;

                float g = current.g + grid.StepCost(current.cell, next);
                if (openByCell.TryGetValue(next, out Node known))
                {
                    if (g < known.g)                                    // cheaper path: update (relaxation)
                    {
                        known.g = g;
                        known.parent = current;
                        if (useBinaryHeap) heap.DecreaseKey(known);
                    }
                }
                else
                {
                    Add(new Node { cell = next, g = g, h = H(next, goal), parent = current, order = order++ });
                }
            }
        }
        return null;                                                    // open list empty: no path

        void Add(Node n)
        {
            openByCell[n.cell] = n;
            if (useBinaryHeap) heap.Push(n); else open.Add(n);
        }

        Node PopBest()
        {
            if (useBinaryHeap) return heap.Pop();
            Node best = open[0];
            foreach (Node n in open)
                if (F(n) < F(best) || (F(n) == F(best) && n.h < best.h)) best = n;
            open.Remove(best);
            return best;
        }
    }

    int Compare(Node a, Node b)
    {
        float fa = F(a), fb = F(b);
        if (fa != fb) return fa < fb ? -1 : 1;
        if (a.h != b.h) return a.h < b.h ? -1 : 1;
        return a.order.CompareTo(b.order);
    }

    float F(Node n) => mode == SearchMode.Greedy ? n.h : n.g + weight * n.h;

    /// <summary>
    /// TODO 3: heuristic in cost units. Octile = max + (sqrt2 - 1) min, scaled by cellSize and the
    /// cheapest possible cell cost (1), so it never overestimates with 8 neighbours.
    /// </summary>
    public float H(Vector2Int a, Vector2Int b)
    {
        if (mode == SearchMode.Dijkstra) return 0f;
        float dx = Mathf.Abs(a.x - b.x), dy = Mathf.Abs(a.y - b.y);
        float cells;
        switch (heuristic)
        {
            case Heuristic.Octile: cells = Mathf.Max(dx, dy) + (Sqrt2 - 1f) * Mathf.Min(dx, dy); break;
            case Heuristic.Manhattan: cells = dx + dy; break;
            case Heuristic.Euclid: cells = Mathf.Sqrt(dx * dx + dy * dy); break;
            default: cells = 0f; break;
        }
        return cells * grid.cellSize * GridMap.MinCellCost;
    }

    static List<Vector2Int> Retrace(Node n)
    {
        var path = new List<Vector2Int>();
        for (; n != null; n = n.parent) path.Add(n.cell);
        path.Reverse();
        return path;
    }
}
