// File: Assets/Editor/Hafta5Builder.cs
// Editor tools for the week 5 homework "Köşe Noktaları":
//   - BuildScene:         builds Assets/Scenes/Hafta5.unity (the week 4 U-obstacle scene + the week 5 waypoint graph)
//   - RunExperiments:     homework task 2 table, run as a batch: K1..K4 off / on, A* and Dijkstra, plus guard paths
//   - CaptureScreenshots: renders the graph / path images into <project>/screenshots
// All of them can be run from the "Hafta 5" menu or with -batchmode -executeMethod Hafta5Builder.<Method>.
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class Hafta5Builder
{
    const string ScenePath = "Assets/Scenes/Hafta5.unity";
    const string MatDir = "Assets/Materials";
    const int ObstacleLayer = 8, AgentLayer = 9;

    static readonly (string name, Vector3 pos)[] RingPoints =
    {
        ("W0", new Vector3(3f, 0.5f, 3f)),   ("W1", new Vector3(10f, 0.5f, 3f)),
        ("W2", new Vector3(16f, 0.5f, 3f)),  ("W3", new Vector3(16f, 0.5f, 10f)),
        ("W4", new Vector3(16f, 0.5f, 17f)), ("W5", new Vector3(10f, 0.5f, 17f)),
        ("W6", new Vector3(3f, 0.5f, 17f)),  ("W7", new Vector3(3f, 0.5f, 10f)),
        ("W8", new Vector3(9f, 0.5f, 10f)),
    };

    // Homework task 1: corner points about 1 m outside the U's outer corners, placed below W8.
    static readonly (string name, Vector3 pos)[] CornerPoints =
    {
        ("K1", new Vector3(6f, 0.5f, 4f)),  ("K2", new Vector3(14f, 0.5f, 4f)),
        ("K3", new Vector3(14f, 0.5f, 16f)), ("K4", new Vector3(6f, 0.5f, 16f)),
    };

    // ------------------------------------------------------------------ scene

    [MenuItem("Hafta 5/Build Scene")]
    public static void BuildScene()
    {
        EnsureLayer(ObstacleLayer, "Obstacle");
        EnsureLayer(AgentLayer, "Agent");
        Directory.CreateDirectory(MatDir);
        Directory.CreateDirectory("Assets/Scenes");

        Material ground = Mat("Ground", "Standard", new Color(0.20f, 0.25f, 0.21f));
        Material obstacle = Mat("Obstacle", "Standard", new Color(0.62f, 0.52f, 0.40f));
        Material playerMat = Mat("Player", "Standard", new Color(0.15f, 0.35f, 0.95f));
        Material npcMat = Mat("Npc", "Standard", new Color(0.90f, 0.12f, 0.10f));
        Material nodeMat = Mat("Node", "Unlit/Color", Color.cyan);
        Material cornerMat = Mat("Corner", "Unlit/Color", new Color(1f, 0.55f, 0.05f));
        Material lineMat = Mat("Line", "Sprites/Default", Color.white);

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // Camera and light (week 4 slide 9: camera at (10, 25, 10), rotation (90, 0, 0))
        var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
        var cam = camGo.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.12f, 0.13f, 0.16f);
        camGo.AddComponent<AudioListener>();
        camGo.transform.SetPositionAndRotation(new Vector3(10f, 25f, 10f), Quaternion.Euler(90f, 0f, 0f));

        var lightGo = new GameObject("Directional Light");
        var light = lightGo.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.1f;
        light.shadows = LightShadows.Soft;
        lightGo.transform.rotation = Quaternion.Euler(55f, -30f, 0f);

        // "Alan": 20 x 20 ground with the U obstacle (week 4 slide 9, positions relative to Alan = world origin here)
        var alan = new GameObject("Alan");
        var zemin = GameObject.CreatePrimitive(PrimitiveType.Plane);
        zemin.name = "Zemin";
        zemin.transform.SetParent(alan.transform);
        zemin.transform.localPosition = new Vector3(10f, 0f, 10f);
        zemin.transform.localScale = new Vector3(2f, 1f, 2f);
        zemin.GetComponent<Renderer>().sharedMaterial = ground;

        Box(alan.transform, "SirtDuvari", new Vector3(12.5f, 1f, 10f), new Vector3(1f, 2f, 10f), obstacle);
        Box(alan.transform, "AltKol", new Vector3(9.5f, 1f, 5.5f), new Vector3(5f, 2f, 1f), obstacle);
        Box(alan.transform, "UstKol", new Vector3(9.5f, 1f, 14.5f), new Vector3(5f, 2f, 1f), obstacle);

        // Player (blue capsule, CharacterController, PlayerMove)
        var player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        player.name = "Player";
        Object.DestroyImmediate(player.GetComponent<CapsuleCollider>());
        player.layer = AgentLayer;
        player.transform.position = new Vector3(17.5f, 1f, 9.5f);
        player.GetComponent<Renderer>().sharedMaterial = playerMat;
        var cc = player.AddComponent<CharacterController>();
        cc.height = 2f; cc.radius = 0.5f; cc.center = Vector3.zero;
        player.AddComponent<PlayerMove>();

        // Waypoints (section 8.1): W0..W8 in this order, then K1..K4 below W8 (homework task 1)
        var wpRoot = new GameObject("Waypoints");
        wpRoot.transform.position = Vector3.zero;
        var all = new Dictionary<string, Waypoint>();
        foreach (var (n, p) in RingPoints.Concat(CornerPoints))
        {
            var go = new GameObject(n);
            go.transform.SetParent(wpRoot.transform);
            go.transform.localPosition = p;
            all[n] = go.AddComponent<Waypoint>();
        }

        var testFrom = new GameObject("TestBaşlangıç").transform;
        testFrom.position = new Vector3(6.5f, 0.5f, 13.5f);
        var testTo = new GameObject("TestHedef").transform;
        testTo.position = new Vector3(17.5f, 0.5f, 9.5f);

        var graph = wpRoot.AddComponent<WaypointGraph>();
        graph.obstacleMask = 1 << ObstacleLayer;
        graph.maxEdgeLength = 8f;
        graph.testFrom = testFrom;
        graph.testTo = testTo;
        graph.testHeuristic = true;

        // NPC (red sphere) with the waypoint guard; patrol route W0..W7 (W8 is only in the graph)
        var npc = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        npc.name = "NPC";
        npc.layer = AgentLayer;
        npc.transform.position = new Vector3(6.5f, 0.5f, 13.5f);
        npc.GetComponent<Renderer>().sharedMaterial = npcMat;
        var guard = npc.AddComponent<NpcWaypointGuard>();
        guard.graph = graph;
        guard.player = player.transform;
        guard.patrolRoute = Enumerable.Range(0, 8).Select(i => all["W" + i]).ToArray();
        guard.pingPong = false;
        guard.smoothPath = true;

        // Runtime stand-in for the Gizmos (see GraphVisualizer.cs)
        var vizGo = new GameObject("GraphVisualizer");
        var viz = vizGo.AddComponent<GraphVisualizer>();
        viz.graph = graph;
        viz.guard = guard;
        viz.lineMaterial = lineMat;
        viz.nodeMaterial = nodeMat;
        viz.cornerMaterial = cornerMat;

        // Build the edges once in edit mode so the Scene view Gizmos show them right away (Awake rebuilds them on Play).
        Physics.SyncTransforms();
        graph.BuildEdges();

        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        AssetDatabase.SaveAssets();
        Debug.Log("[Hafta5] Scene built: " + ScenePath);
    }

    static void Box(Transform parent, string name, Vector3 pos, Vector3 scale, Material mat)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.layer = ObstacleLayer;
        go.transform.SetParent(parent);
        go.transform.localPosition = pos;
        go.transform.localScale = scale;
        go.GetComponent<Renderer>().sharedMaterial = mat;
    }

    static Material Mat(string name, string shader, Color color)
    {
        string path = $"{MatDir}/{name}.mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null)
        {
            m = new Material(Shader.Find(shader));
            AssetDatabase.CreateAsset(m, path);
        }
        m.shader = Shader.Find(shader);
        m.color = color;
        if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", 0.1f);
        EditorUtility.SetDirty(m);
        return m;
    }

    static void EnsureLayer(int index, string name)
    {
        var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty layer = tagManager.FindProperty("layers").GetArrayElementAtIndex(index);
        if (layer.stringValue == name) return;
        layer.stringValue = name;
        tagManager.ApplyModifiedProperties();
    }

    // ------------------------------------------------------------------ experiments (homework task 2)

    class Ctx
    {
        public WaypointGraph graph;
        public NpcWaypointGuard guard;
        public GraphVisualizer viz;
        public GameObject[] corners;
    }

    static Ctx OpenScene()
    {
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        var ctx = new Ctx
        {
            graph = Object.FindAnyObjectByType<WaypointGraph>(),
            guard = Object.FindAnyObjectByType<NpcWaypointGuard>(),
            viz = Object.FindAnyObjectByType<GraphVisualizer>(),
        };
        ctx.corners = CornerPoints.Select(c => ctx.graph.transform.Find(c.name).gameObject).ToArray();
        return ctx;
    }

    static void SetCorners(Ctx ctx, bool on)
    {
        foreach (GameObject k in ctx.corners) k.SetActive(on);   // GetComponentsInChildren skips inactive objects
        Physics.SyncTransforms();
    }

    [MenuItem("Hafta 5/Run A* Experiments (K1-K4 off and on)")]
    public static void RunExperiments()
    {
        var log = new StringBuilder();
        void Capture(string msg, string stack, LogType type)
        {
            if (!msg.StartsWith("[Hafta5]")) log.AppendLine(msg);
        }
        Application.logMessageReceived += Capture;
        try
        {
            Ctx ctx = OpenScene();
            MethodInfo testPath = typeof(WaypointGraph).GetMethod("TestPath", BindingFlags.Instance | BindingFlags.NonPublic);

            foreach (bool cornersOn in new[] { false, true })
            {
                string title = cornersOn ? "Halka + köşeler (K1–K4 açık)" : "Halka (K1–K4 kapalı)";
                log.AppendLine($"=== {title} ===");
                SetCorners(ctx, cornersOn);

                foreach (bool heuristic in new[] { true, false })
                {
                    ctx.graph.testHeuristic = heuristic;
                    log.AppendLine(heuristic ? "-- A* Testi (Test Heuristic açık) --" : "-- A* Testi (Test Heuristic kapalı = Dijkstra) --");
                    testPath.Invoke(ctx.graph, null);       // TestPath() calls BuildEdges(), which logs the edge count
                }

                // Edge list, so the edge count can be checked by hand
                var edges = new List<string>();
                foreach (Waypoint a in ctx.graph.nodes)
                    foreach (Waypoint b in a.neighbors)
                        if (System.Array.IndexOf(ctx.graph.nodes, a) < System.Array.IndexOf(ctx.graph.nodes, b))
                            edges.Add($"{a.name}–{b.name} {Vector3.Distance(a.transform.position, b.transform.position):F2}");
                log.AppendLine($"Kenarlar ({edges.Count}): {string.Join(", ", edges)}");
                foreach (Waypoint a in ctx.graph.nodes)
                    log.AppendLine($"  {a.name}.neighbors = [{string.Join(", ", a.neighbors.Select(n => n.name))}]");

                // Bonus: what NpcWaypointGuard.PlanTo produces from the same start/goal, without and with smoothing
                foreach (bool smooth in new[] { false, true })
                {
                    var (desc, len, _) = GuardPlan(ctx, smooth);
                    log.AppendLine($"-- NpcWaypointGuard.PlanTo, smoothPath={(smooth ? "açık" : "kapalı")} -- NPC → {desc} | uzunluk: {len:F1} m");
                }
                log.AppendLine();
            }

            SetCorners(ctx, true);
            ctx.graph.testHeuristic = true;
            ctx.graph.BuildEdges();
        }
        finally
        {
            Application.logMessageReceived -= Capture;
        }

        Directory.CreateDirectory("results");
        File.WriteAllText("results/astar-experiments.txt", log.ToString(), new UTF8Encoding(false));
        Debug.Log("[Hafta5] Experiments written to results/astar-experiments.txt\n" + log);
    }

    // Runs the guard's private PlanTo(TestHedef) from the NPC's position with a fresh heading.
    static (string desc, float length, List<Vector3> points) GuardPlan(Ctx ctx, bool smooth)
    {
        var t = typeof(NpcWaypointGuard);
        const BindingFlags F = BindingFlags.Instance | BindingFlags.NonPublic;
        ctx.graph.BuildEdges();
        ctx.guard.transform.position = ctx.graph.testFrom.position;
        ctx.guard.smoothPath = smooth;
        t.GetField("heading", F).SetValue(ctx.guard, null);
        t.GetMethod("PlanTo", F).Invoke(ctx.guard, new object[] { ctx.graph.testTo.position });

        var pts = new List<Vector3>((List<Vector3>)t.GetField("path", F).GetValue(ctx.guard));
        var nodes = (List<Waypoint>)t.GetField("pathNodes", F).GetValue(ctx.guard);
        float len = 0f;
        Vector3 prev = ctx.guard.transform.position;
        foreach (Vector3 p in pts) { len += Vector3.Distance(prev, p); prev = p; }
        string desc = string.Join(" → ", nodes.Select(n => n != null ? n.name : "Hedef"));
        ctx.guard.smoothPath = true;
        t.GetField("heading", F).SetValue(ctx.guard, null);
        return (desc, len, pts);
    }

    // ------------------------------------------------------------------ screenshots

    [MenuItem("Hafta 5/Capture Screenshots")]
    public static void CaptureScreenshots()
    {
        Ctx ctx = OpenScene();
        Directory.CreateDirectory("screenshots");
        var temp = new List<Object>();

        // Overview from the scene's Main Camera, graph with corners
        SetCorners(ctx, true);
        ctx.graph.BuildEdges();
        ctx.viz.Rebuild();
        Render(Camera.main, "screenshots/00-sahne-genel.png", 1600, 1000);

        // Orthographic top-down camera for the comparison images
        var camGo = new GameObject("CaptureCamera");
        temp.Add(camGo);
        var cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 12f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.12f, 0.13f, 0.16f);
        camGo.transform.SetPositionAndRotation(new Vector3(10f, 30f, 10.5f), Quaternion.Euler(90f, 0f, 0f));

        foreach (bool cornersOn in new[] { false, true })
        {
            SetCorners(ctx, cornersOn);
            ctx.graph.BuildEdges();
            ctx.viz.Rebuild();
            int edgeCount = ctx.graph.nodes.Sum(n => n.neighbors.Count) / 2;
            string tag = cornersOn ? "halka-kose" : "halka";
            string title = cornersOn ? "Halka + köşeler (K1–K4)" : "Halka (K1–K4 kapalı)";

            var caption = Caption(ctx, $"{title}: {ctx.graph.nodes.Length} düğüm, {edgeCount} kenar (maxEdgeLength 8)");
            Render(cam, $"screenshots/{(cornersOn ? "02" : "01")}-{tag}-kenarlar.png", 1200, 1200);
            Object.DestroyImmediate(caption);

            // Bonus: guard path without / with smoothing
            var (rawDesc, rawLen, rawPts) = GuardPlan(ctx, false);
            var (smDesc, smLen, smPts) = GuardPlan(ctx, true);
            Vector3 npc = ctx.graph.testFrom.position;

            var lines = new List<GameObject>
            {
                ctx.viz.MakeLine("A* path", Prepend(npc, rawPts), new Color(1f, 0.6f, 0f), 0.2f, 0.1f).gameObject,
                Caption(ctx, $"{title}\nturuncu: yumuşatmasız A* yolu  NPC → {rawDesc} = {rawLen:F1} m"),
            };
            Render(cam, $"screenshots/{(cornersOn ? "05" : "03")}-{tag}-yol-yumusatmasiz.png", 1200, 1200);

            lines.Add(ctx.viz.MakeLine("Smoothed path", Prepend(npc, smPts), new Color(0.2f, 0.55f, 1f), 0.24f, 0.15f).gameObject);
            Object.DestroyImmediate(lines[1]);
            lines.Add(Caption(ctx, $"{title}\nturuncu: yumuşatmasız {rawLen:F1} m   mavi: yumuşatılmış  NPC → {smDesc} = {smLen:F1} m"));
            Render(cam, $"screenshots/{(cornersOn ? "06" : "04")}-{tag}-yol-yumusatilmis.png", 1200, 1200);
            foreach (GameObject l in lines) if (l != null) Object.DestroyImmediate(l);
        }

        foreach (Object o in temp) Object.DestroyImmediate(o);
        ctx.viz.Rebuild();
        foreach (Transform child in ctx.viz.transform.Cast<Transform>().ToList()) Object.DestroyImmediate(child.gameObject);
        Debug.Log("[Hafta5] Screenshots written to screenshots/");
    }

    static List<Vector3> Prepend(Vector3 first, List<Vector3> rest)
    {
        var l = new List<Vector3> { first };
        l.AddRange(rest);
        return l;
    }

    static GameObject Caption(Ctx ctx, string text)
    {
        TextMesh tm = ctx.viz.MakeLabel(text, new Vector3(-1.6f, 1f, 21.8f), Color.white, 0.075f);
        tm.anchor = TextAnchor.UpperLeft;
        // Mark the start (NPC) and the goal (TestHedef = player position) as well
        Vector3 s = ctx.graph.testFrom.position, g = ctx.graph.testTo.position;
        ctx.viz.MakeLabel("NPC", s + new Vector3(-1.9f, 1f, 0.9f), Color.red, 0.09f).transform.SetParent(tm.transform, true);
        ctx.viz.MakeLabel("P", g + new Vector3(0.6f, 1.5f, -0.3f), new Color(0.1f, 0.2f, 0.9f), 0.09f).transform.SetParent(tm.transform, true);
        return tm.gameObject;
    }

    static void Render(Camera cam, string file, int w, int h)
    {
        var rt = new RenderTexture(w, h, 24) { antiAliasing = 8 };
        var prevTarget = cam.targetTexture;
        float prevAspect = cam.aspect;
        cam.targetTexture = rt;
        cam.aspect = (float)w / h;
        cam.Render();
        RenderTexture.active = rt;
        var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
        tex.Apply();
        File.WriteAllBytes(file, tex.EncodeToPNG());
        RenderTexture.active = null;
        cam.targetTexture = prevTarget;
        cam.aspect = prevAspect;
        Object.DestroyImmediate(tex);
        rt.Release();
        Object.DestroyImmediate(rt);
        Debug.Log("[Hafta5] Saved " + file);
    }

    // ------------------------------------------------------------------ batch entry point

    public static void BatchAll()
    {
        BuildScene();
        RunExperiments();
        CaptureScreenshots();
    }
}
