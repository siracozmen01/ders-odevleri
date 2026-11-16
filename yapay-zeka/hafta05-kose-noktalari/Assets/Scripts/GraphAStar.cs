// File: Assets/Scripts/GraphAStar.cs  (week 5 lecture notes, section 8.4)
using System.Collections.Generic;
using UnityEngine;

public class GraphAStar
{
    // Record of one node: waypoint, g, h, parent
    class Node
    {
        public Waypoint wp;                                 // [NEW] a waypoint instead of a cell
        public float g, h;
        public Node parent;
    }

    public bool useHeuristic = true;                        // false: Dijkstra (h = 0)
    public int Expanded { get; private set; }               // how many nodes were expanded

    public List<Waypoint> FindPath(Waypoint start, Waypoint goal)
    {
        var open = new List<Node>();                         // open list
        var openByWp = new Dictionary<Waypoint, Node>();     // "is this node open?"
        var closed = new HashSet<Waypoint>();                // closed list
        Expanded = 0;

        var first = new Node { wp = start, g = 0f, h = H(start, goal) };
        open.Add(first);
        openByWp[start] = first;

        while (open.Count > 0)
        {
            // 1. Pick the node with the smallest f from the open list; on a tie the smaller h
            Node current = open[0];
            foreach (Node n in open)
                if (F(n) < F(current) || (F(n) == F(current) && n.h < current.h))
                    current = n;

            // 2. Move it to the closed list
            open.Remove(current);
            openByWp.Remove(current.wp);
            closed.Add(current.wp);
            Expanded++;

            // 3. Goal? Retrace the path through the parents
            if (current.wp == goal) return Retrace(current);

            // 4. [NEW] Neighbours: not four directions but the node's own list
            foreach (Waypoint next in current.wp.neighbors)
            {
                if (closed.Contains(next)) continue;

                // [NEW] Edge cost: distance between the two points
                float newG = current.g + Vector3.Distance(current.wp.transform.position,
                                                          next.transform.position);

                if (!openByWp.TryGetValue(next, out Node node))
                {
                    // seen for the first time: add to the open list
                    node = new Node { wp = next, g = newG, h = H(next, goal), parent = current };
                    open.Add(node);
                    openByWp[next] = node;
                }
                else if (newG < node.g)
                {
                    // cheaper route: update
                    node.g = newG;
                    node.parent = current;
                }
            }
        }
        return null;                                         // no path
    }

    float F(Node n) => n.g + n.h;

    // [NEW] Heuristic: Euclidean distance
    float H(Waypoint a, Waypoint b) =>
        useHeuristic ? Vector3.Distance(a.transform.position, b.transform.position) : 0f;

    List<Waypoint> Retrace(Node end)
    {
        var path = new List<Waypoint>();
        for (Node n = end; n != null; n = n.parent) path.Add(n.wp);
        path.Reverse();                                      // reverse the goal->start list
        return path;
    }
}
