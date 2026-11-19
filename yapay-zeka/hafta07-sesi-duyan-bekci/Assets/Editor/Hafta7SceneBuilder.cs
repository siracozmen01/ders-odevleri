// File: Assets/Editor/Hafta7SceneBuilder.cs
// Builds the week 7 scene from code so it is reproducible (week 7 section 8.1).
// Menu: Hafta7 > Build Scene   or   -executeMethod Hafta7SceneBuilder.Build
//
// The scene is the 20 x 20 area from weeks 4-6: the U obstacle (opening to the west) and, as in week 6,
// a sliding door with a carving NavMeshObstacle. Here the door sits in a 2 m gap in the U's back wall.
// The patrol loop P0 -> P1 -> P2 -> P3 passes through the door (P1 inside the U, P2 outside), and comes back
// over the top arm. When the door is closed far from the guard, the NavMesh finds the way around the U.
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using Unity.AI.Navigation;

public static class Hafta7SceneBuilder
{
    public const string ScenePath = "Assets/Scenes/Hafta7.unity";
    const string NavDataPath = "Assets/Scenes/Hafta7_NavMesh.asset";
    const string MatDir = "Assets/Materials";
    const string InputPath = "Assets/InputSystem_Actions.inputactions";

    // Layout (world coordinates, metres). Ground x 0..20, z 0..20.
    //   U back wall  x 12..13, z 5..9 and z 11..15   (gap z 9..11 holds the door)
    //   U arms       x 7..12,  z 5..6 and z 14..15
    public static readonly Vector3 DoorPos = new Vector3(12.5f, 1f, 10f);
    public static readonly Vector3[] PatrolPoints =
    {
        new Vector3(4f, 0f, 13f),     // P0  west of the U opening
        new Vector3(9.5f, 0f, 10f),   // P1  inside the U, one side of the door
        new Vector3(16f, 0f, 9f),     // P2  outside the U, the other side of the door
        new Vector3(14f, 0f, 18f),    // P3  north-east; the way back to P0 goes over the top arm
    };
    public static readonly Vector3 GuardStart = new Vector3(3.5f, 0f, 15.5f);
    public static readonly Vector3 PlayerStart = new Vector3(2f, 0f, 2f);

    [MenuItem("Hafta7/Build Scene")]
    public static void Build()
    {
        SetupInputActions();
        Directory.CreateDirectory("Assets/Scenes");
        Directory.CreateDirectory(MatDir);

        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // Materials
        Material ground = Mat("Ground", new Color(0.60f, 0.67f, 0.52f));
        Material wall = Mat("Wall", new Color(0.28f, 0.28f, 0.32f));
        Material door = Mat("Door", new Color(0.80f, 0.25f, 0.05f));
        Material doorTrace = Mat("DoorTrace", new Color(0.45f, 0.36f, 0.30f));   // floor mark: open door looks different from closed
        Material guardMat = Mat("Guard", Color.green);
        Material playerMat = Mat("Player", new Color(0.15f, 0.35f, 0.95f));
        Material pointMat = Mat("PatrolPoint", new Color(0.95f, 0.95f, 0.95f));
        Material pathMat = UnlitMat("PathLine", new Color(0.2f, 0.95f, 0.95f));
        Material lineMat = SpriteMat("FadeLine");   // vertex colours + alpha (noise ring, view cone)

        // Light
        var lightGo = new GameObject("Directional Light");
        var light = lightGo.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 0.85f;
        light.shadows = LightShadows.Soft;
        lightGo.transform.rotation = Quaternion.Euler(60f, -30f, 0f);
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.42f, 0.42f, 0.45f);

        // Camera: top-down orthographic; the level sits on the right, the state panel on the left
        var camGo = new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        var cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 11.5f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.12f, 0.14f, 0.18f);
        camGo.transform.position = new Vector3(5.2f, 30f, 10f);
        camGo.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        camGo.AddComponent<AudioListener>();

        // Level root with the NavMeshSurface (collects only its children)
        var alan = new GameObject("Alan");
        var surface = alan.AddComponent<NavMeshSurface>();
        surface.collectObjects = CollectObjects.Children;
        surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
        surface.agentTypeID = 0;   // Humanoid: radius 0.5, height 2, step 0.75, slope 45

        Box("Zemin", alan, new Vector3(10f, -0.1f, 10f), new Vector3(20f, 0.2f, 20f), ground, false);

        // Low border so the character cannot walk off the ground
        Box("Sinir_Bati", alan, new Vector3(-0.25f, 0.5f, 10f), new Vector3(0.5f, 1f, 20.5f), wall, true);
        Box("Sinir_Dogu", alan, new Vector3(20.25f, 0.5f, 10f), new Vector3(0.5f, 1f, 20.5f), wall, true);
        Box("Sinir_Guney", alan, new Vector3(10f, 0.5f, -0.25f), new Vector3(21f, 1f, 0.5f), wall, true);
        Box("Sinir_Kuzey", alan, new Vector3(10f, 0.5f, 20.25f), new Vector3(21f, 1f, 0.5f), wall, true);

        // U obstacle (weeks 4-6), 2 m high so it blocks the guard's view; back wall split by the door gap
        Box("SirtDuvari_Alt", alan, new Vector3(12.5f, 1f, 7f), new Vector3(1f, 2f, 4f), wall, true);
        Box("SirtDuvari_Ust", alan, new Vector3(12.5f, 1f, 13f), new Vector3(1f, 2f, 4f), wall, true);
        Box("AltKol", alan, new Vector3(9.5f, 1f, 5.5f), new Vector3(5f, 2f, 1f), wall, true);
        Box("UstKol", alan, new Vector3(9.5f, 1f, 14.5f), new Vector3(5f, 2f, 1f), wall, true);

        // Sliding door in the gap (week 6): excluded from the bake, carves when closed, K key
        var kapi = Box("Kapi", alan, DoorPos, new Vector3(1f, 2f, 2f), door, false);
        var kapiMod = kapi.AddComponent<NavMeshModifier>();
        kapiMod.ignoreFromBuild = true;   // "Remove object" mode
        var obstacle = kapi.AddComponent<NavMeshObstacle>();
        obstacle.shape = NavMeshObstacleShape.Box;
        obstacle.center = Vector3.zero;
        obstacle.size = Vector3.one;      // x scale = 1 x 2 x 2 m
        obstacle.carving = true;
        obstacle.carvingMoveThreshold = 0.1f;
        obstacle.carvingTimeToStationary = 0.5f;
        obstacle.carveOnlyStationary = true;
        obstacle.enabled = false;         // the door starts open; SlidingDoor.Awake sets this anyway
        var sliding = kapi.AddComponent<SlidingDoor>();
        sliding.startOpen = true;
        Visual("KapiIzi", alan, new Vector3(DoorPos.x, 0.015f, DoorPos.z), new Vector3(1f, 0.02f, 2f), doorTrace);

        // Patrol points P0..P3 (empty objects + a small floor marker)
        var points = new Transform[PatrolPoints.Length];
        var pointRoot = new GameObject("DevriyeNoktalari");
        for (int i = 0; i < PatrolPoints.Length; i++)
        {
            var p = new GameObject("P" + i);
            p.transform.SetParent(pointRoot.transform, false);
            p.transform.position = PatrolPoints[i];
            points[i] = p.transform;
            var m = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            m.name = "Isaret";
            Object.DestroyImmediate(m.GetComponent<Collider>());
            m.transform.SetParent(p.transform, false);
            m.transform.localPosition = new Vector3(0f, 0.02f, 0f);
            m.transform.localScale = new Vector3(0.5f, 0.01f, 0.5f);
            m.GetComponent<Renderer>().sharedMaterial = pointMat;
            Label("Etiket_P" + i, "P" + i, PatrolPoints[i] + new Vector3(0f, 0.05f, -0.8f), 0.08f);
        }
        Label("Etiket_Kapi", "Kapı", new Vector3(14.3f, 0.05f, 10.6f), 0.07f);

        // Character (blue capsule, WASD + N for noise)
        var player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        player.name = "Player";
        Object.DestroyImmediate(player.GetComponent<Collider>());
        player.transform.position = PlayerStart + Vector3.up;
        player.GetComponent<Renderer>().sharedMaterial = playerMat;
        var cc = player.AddComponent<CharacterController>();
        cc.height = 2f;
        cc.radius = 0.5f;
        cc.center = Vector3.zero;
        player.AddComponent<PlayerMove>();
        var noise = player.AddComponent<PlayerNoise>();
        noise.ringMaterial = lineMat;

        // The guard ("Bekci", the red sphere of week 6 turned into a guard)
        var bekci = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        bekci.name = "Bekci";
        bekci.transform.position = GuardStart + Vector3.up * 0.5f;
        bekci.GetComponent<Renderer>().sharedMaterial = guardMat;
        var agent = bekci.AddComponent<NavMeshAgent>();
        agent.agentTypeID = 0;
        agent.baseOffset = 0.5f;
        agent.radius = 0.5f;
        agent.height = 1f;
        agent.speed = 2f;
        agent.angularSpeed = 360f;
        agent.acceleration = 8f;
        agent.stoppingDistance = 0f;
        agent.enabled = false;   // Guard.Awake enables it once the NavMeshSurface data is loaded
        var line = bekci.AddComponent<LineRenderer>();
        line.sharedMaterial = pathMat;
        line.widthMultiplier = 0.12f;
        line.positionCount = 0;
        line.useWorldSpace = true;
        line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        line.receiveShadows = false;
        var oldChase = bekci.AddComponent<NpcNavChase>();   // 8.1: uncheck NpcNavChase, do not delete it
        oldChase.target = player.transform;
        oldChase.enabled = false;
        var guard = bekci.AddComponent<Guard>();
        guard.player = player.transform;
        guard.patrolPoints = points;
        guard.door = sliding;
        var view = bekci.AddComponent<GuardDebugView>();
        view.coneMaterial = lineMat;
        noise.guard = guard;

        // Bake and store the NavMesh as an asset next to the scene
        surface.BuildNavMesh();
        if (AssetDatabase.LoadAssetAtPath<NavMeshData>(NavDataPath) != null)
            AssetDatabase.DeleteAsset(NavDataPath);
        AssetDatabase.CreateAsset(surface.navMeshData, NavDataPath);
        surface.navMeshData = AssetDatabase.LoadAssetAtPath<NavMeshData>(NavDataPath);
        surface.AddData();

        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };

        PlayerSettings.productName = "Hafta7 Sesi Duyan Bekci";
        PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
        PlayerSettings.defaultScreenWidth = 1200;
        PlayerSettings.defaultScreenHeight = 900;
        PlayerSettings.resizableWindow = true;
        PlayerSettings.runInBackground = true;
        AssetDatabase.SaveAssets();

        var tri = NavMesh.CalculateTriangulation();
        Debug.Log($"[Hafta7] Scene saved to {ScenePath}. NavMesh: {tri.vertices.Length} vertices, {tri.indices.Length / 3} triangles.");
    }

    static void SetupInputActions()
    {
        AssetDatabase.ImportAsset(InputPath, ImportAssetOptions.ForceUpdate);
        var asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputPath);
        if (asset == null) { Debug.LogError("[Hafta7] Input actions asset not found: " + InputPath); return; }
        InputSystem.actions = asset;   // project-wide actions (used by InputSystem.actions.FindAction)
        Debug.Log("[Hafta7] Project-wide input actions: " + AssetDatabase.GetAssetPath(InputSystem.actions)
                  + ", MakeNoise: " + (asset.FindAction("MakeNoise") != null));
    }

    static Material Mat(string name, Color c)
    {
        string path = $"{MatDir}/{name}.mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null) { m = new Material(Shader.Find("Standard")); AssetDatabase.CreateAsset(m, path); }
        m.color = c;
        m.SetFloat("_Glossiness", 0.1f);
        EditorUtility.SetDirty(m);
        return m;
    }

    static Material UnlitMat(string name, Color c)
    {
        string path = $"{MatDir}/{name}.mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null) { m = new Material(Shader.Find("Unlit/Color")); AssetDatabase.CreateAsset(m, path); }
        m.color = c;
        EditorUtility.SetDirty(m);
        return m;
    }

    static Material SpriteMat(string name)
    {
        string path = $"{MatDir}/{name}.mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null) { m = new Material(Shader.Find("Sprites/Default")); AssetDatabase.CreateAsset(m, path); }
        EditorUtility.SetDirty(m);
        return m;
    }

    static GameObject Box(string name, GameObject parent, Vector3 pos, Vector3 scale, Material m, bool notWalkableTop)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent.transform, false);
        go.transform.position = pos;
        go.transform.localScale = scale;
        go.GetComponent<Renderer>().sharedMaterial = m;
        if (notWalkableTop)
        {
            var mod = go.AddComponent<NavMeshModifier>();
            mod.overrideArea = true;
            mod.area = NavMesh.GetAreaFromName("Not Walkable");
        }
        return go;
    }

    static void Visual(string name, GameObject parent, Vector3 pos, Vector3 scale, Material m)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        Object.DestroyImmediate(go.GetComponent<Collider>());   // visual only
        go.transform.SetParent(parent.transform, false);
        go.transform.position = pos;
        go.transform.localScale = scale;
        var r = go.GetComponent<Renderer>();
        r.sharedMaterial = m;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    }

    static void Label(string name, string text, Vector3 pos, float size)
    {
        var go = new GameObject(name);
        go.transform.position = pos;
        go.transform.rotation = Quaternion.Euler(90f, 0f, 0f);   // lie flat, readable from the top camera
        var tm = go.AddComponent<TextMesh>();
        tm.text = text;
        tm.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        tm.fontSize = 64;
        tm.characterSize = size;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        tm.color = new Color(0.05f, 0.05f, 0.12f);
        tm.fontStyle = FontStyle.Bold;
        go.GetComponent<MeshRenderer>().sharedMaterial = tm.font.material;
    }
}
