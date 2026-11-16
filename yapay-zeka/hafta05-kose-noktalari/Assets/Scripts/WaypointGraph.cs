// File: Assets/Scripts/WaypointGraph.cs  (week 5 lecture notes, section 8.3 + 9.1 experiment add-on)
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class WaypointGraph : MonoBehaviour
{
    public LayerMask obstacleMask;          // Obstacle layer
    public float maxEdgeLength = 8f;        // do not build longer edges

    [HideInInspector] public Waypoint[] nodes;

    void Awake() => BuildEdges();

    [ContextMenu("Kenarları Kur")]
    public void BuildEdges()
    {
        nodes = GetComponentsInChildren<Waypoint>();     // hierarchy order: W0, W1, ...
        foreach (Waypoint a in nodes)
        {
            a.neighbors.Clear();
            foreach (Waypoint b in nodes)
            {
                if (a == b) continue;
                float dist = Vector3.Distance(a.transform.position, b.transform.position);
                if (dist <= maxEdgeLength && CanSee(a.transform.position, b.transform.position))
                    a.neighbors.Add(b);
            }
        }

        // Homework task 1: one line that prints the edge count (undirected: every edge is stored twice).
        Debug.Log($"Kenarları Kur: {nodes.Length} düğüm, {nodes.Sum(n => n.neighbors.Count) / 2} kenar");
    }

    // Is there no obstacle between the two points? (week 3 line of sight)
    public bool CanSee(Vector3 from, Vector3 to)
    {
        return !Physics.Linecast(from, to, obstacleMask);
    }

    // The nearest node that is visible from a position
    public Waypoint Nearest(Vector3 pos)
    {
        Waypoint best = null;
        float bestDist = float.MaxValue;
        foreach (Waypoint w in nodes)
        {
            float d = Vector3.Distance(pos, w.transform.position);
            if (d < bestDist && CanSee(pos, w.transform.position))
            {
                best = w;
                bestDist = d;
            }
        }
        return best;
    }

    // ---- Experiment add-on (section 9.1) ----

    public Transform testFrom, testTo;          // TestBaşlangıç and TestHedef objects
    public bool testHeuristic = true;           // false: Dijkstra

    [ContextMenu("A* Testi")]
    void TestPath()
    {
        BuildEdges();
        Waypoint s = Nearest(testFrom.position);
        Waypoint g = Nearest(testTo.position);
        var astar = new GraphAStar { useHeuristic = testHeuristic };
        List<Waypoint> p = astar.FindPath(s, g);
        if (p == null) { Debug.Log("Yol yok"); return; }

        // Length: start -> first node + between nodes + last node -> goal
        float len = Vector3.Distance(testFrom.position, s.transform.position)
                  + Vector3.Distance(g.transform.position, testTo.position);
        for (int i = 1; i < p.Count; i++)
            len += Vector3.Distance(p[i - 1].transform.position, p[i].transform.position);

        Debug.Log($"{string.Join(" → ", p.ConvertAll(w => w.name))} | " +
                  $"genişletilen: {astar.Expanded} | uzunluk: {len:F1} m");
    }
}
