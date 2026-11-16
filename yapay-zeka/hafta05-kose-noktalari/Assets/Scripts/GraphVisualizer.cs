// File: Assets/Scripts/GraphVisualizer.cs
// Not part of the lecture code. Gizmos (Waypoint.OnDrawGizmos, NpcWaypointGuard.OnDrawGizmos) are only drawn in the
// Scene view / Game view with the Gizmos button on, and never in camera captures or player builds.
// This component draws the same things with real geometry so they show up in the Game view, in builds and in the
// screenshots: cyan spheres for nodes (orange for the K corner points), white lines for edges, and the guard's
// remaining path coloured by its state (green patrol, red chase, yellow return).
using System.Collections.Generic;
using UnityEngine;

public class GraphVisualizer : MonoBehaviour
{
    public WaypointGraph graph;
    public NpcWaypointGuard guard;                 // optional: draw its remaining path
    public Material lineMaterial;                  // vertex-coloured (Sprites/Default)
    public Material nodeMaterial;                  // cyan
    public Material cornerMaterial;                // orange, for K1..K4
    public bool showLabels = true;
    public float edgeWidth = 0.08f, pathWidth = 0.16f, nodeSize = 0.6f;

    readonly List<GameObject> graphObjects = new();
    LineRenderer guardLine;
    int signature = -1;

    void Start() => Rebuild();

    void LateUpdate()
    {
        if (graph == null) return;
        if (Signature() != signature) Rebuild();
        DrawGuardPath();
    }

    int Signature()
    {
        if (graph.nodes == null) return 0;
        int s = graph.nodes.Length * 1000;
        foreach (Waypoint w in graph.nodes) if (w != null) s += w.neighbors.Count;
        return s;
    }

    // Recreate node spheres, labels and edge lines from graph.nodes.
    public void Rebuild()
    {
        foreach (GameObject go in graphObjects) Kill(go);
        graphObjects.Clear();
        signature = Signature();
        if (graph == null || graph.nodes == null) return;

        var drawn = new HashSet<(Waypoint, Waypoint)>();
        foreach (Waypoint a in graph.nodes)
        {
            if (a == null) continue;
            bool corner = a.name.StartsWith("K");
            graphObjects.Add(MakeNode(a.name, a.transform.position, corner ? cornerMaterial : nodeMaterial));
            foreach (Waypoint b in a.neighbors)
            {
                if (b == null || drawn.Contains((b, a))) continue;
                drawn.Add((a, b));
                graphObjects.Add(MakeLine($"Edge {a.name}-{b.name}",
                    new[] { a.transform.position, b.transform.position }, Color.white, edgeWidth).gameObject);
            }
        }
    }

    // Draw an extra polyline (used by the editor screenshot tool for A* / smoothed paths).
    public LineRenderer MakeLine(string lineName, IList<Vector3> points, Color color, float width, float lift = 0.02f)
    {
        var go = new GameObject(lineName);
        go.transform.SetParent(transform, false);
        var lr = go.AddComponent<LineRenderer>();
        lr.sharedMaterial = lineMaterial;
        lr.startColor = lr.endColor = color;
        lr.startWidth = lr.endWidth = width;
        lr.numCapVertices = 2;
        lr.numCornerVertices = 2;
        lr.alignment = LineAlignment.View;
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lr.receiveShadows = false;
        lr.positionCount = points.Count;
        for (int i = 0; i < points.Count; i++) lr.SetPosition(i, points[i] + Vector3.up * lift);
        return lr;
    }

    // A text label lying flat, readable from the top-down camera.
    public TextMesh MakeLabel(string text, Vector3 pos, Color color, float size = 0.12f)
    {
        var go = new GameObject("Label " + text);
        go.transform.SetParent(transform, false);
        go.transform.position = pos;
        go.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        var tm = go.AddComponent<TextMesh>();
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        tm.font = font;
        go.GetComponent<MeshRenderer>().sharedMaterial = font.material;
        tm.text = text;
        tm.fontSize = 64;
        tm.characterSize = size;
        tm.anchor = TextAnchor.MiddleLeft;
        tm.color = color;
        return tm;
    }

    GameObject MakeNode(string nodeName, Vector3 pos, Material mat)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = "Node " + nodeName;
        Kill(go.GetComponent<Collider>());               // purely visual
        go.transform.SetParent(transform, false);
        go.transform.position = pos;
        go.transform.localScale = Vector3.one * nodeSize;
        go.GetComponent<Renderer>().sharedMaterial = mat;
        if (showLabels)
        {
            TextMesh label = MakeLabel(nodeName, pos + new Vector3(0.35f, 0.6f, 0.35f), Color.black);
            label.transform.SetParent(go.transform, true);
        }
        return go;
    }

    void DrawGuardPath()
    {
        if (guard == null) return;
        if (guardLine == null) guardLine = MakeLine("Guard path", new Vector3[0], Color.green, pathWidth, 0.05f);

        Color c = guard.CurrentState == "Chase" ? Color.red : guard.CurrentState == "Return" ? Color.yellow : Color.green;
        guardLine.startColor = guardLine.endColor = c;

        IReadOnlyList<Vector3> p = guard.CurrentPath;
        int from = guard.CurrentPathIndex;
        bool patrolling = guard.CurrentState == "Patrol";
        int count = patrolling ? 0 : Mathf.Max(0, p.Count - from);
        guardLine.positionCount = count + 1;
        guardLine.SetPosition(0, guard.transform.position + Vector3.up * 0.05f);
        for (int i = 0; i < count; i++) guardLine.SetPosition(i + 1, p[from + i] + Vector3.up * 0.05f);
        if (patrolling)
        {
            // Same as the gizmo: a short ray in the view direction while patrolling.
            guardLine.positionCount = 2;
            guardLine.SetPosition(1, guard.transform.position + guard.transform.forward * 2f + Vector3.up * 0.05f);
        }
    }

    static void Kill(Object o)
    {
        if (o == null) return;
        if (Application.isPlaying) Destroy(o); else DestroyImmediate(o);
    }
}
