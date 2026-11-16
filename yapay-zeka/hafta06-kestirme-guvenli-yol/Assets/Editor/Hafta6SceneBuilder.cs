// File: Assets/Editor/Hafta6SceneBuilder.cs
// Builds the homework scene "Kestirme mi, Guvenli Yol mu?" from code so it is reproducible.
// Menu: Hafta6 > Build Scene   or   -executeMethod Hafta6SceneBuilder.Build
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using Unity.AI.Navigation;

public static class Hafta6SceneBuilder
{
    public const string ScenePath = "Assets/Scenes/Hafta6.unity";
    const string NavDataPath = "Assets/Scenes/Hafta6_NavMesh.asset";
    const string MatDir = "Assets/Materials";
    const string InputPath = "Assets/InputSystem_Actions.inputactions";

    // Layout (world coordinates, metres). Ground x 0..30, z 0..30.
    // A barrier at x 12..18 separates the NPC yard (left) from the character yard (right).
    // Passages through it, top to bottom:
    //   A  mud corridor     z 23..27  (NavMeshModifierVolume, area Camur)
    //   B  jump link        z 18 over a 1 m thin wall (z 15..21)
    //   C  water corridor   z 7..11   (NavMeshModifierVolume, area SigSu)
    //   D  door corridor    z 0..3    (SlidingDoor + carving NavMeshObstacle)
    public static readonly Vector3 NpcStart = new Vector3(4f, 0f, 18f);
    public static readonly Vector3 PlayerStart = new Vector3(26f, 0f, 18f);

    // The report's cost settings (keys 1-4): name, Camur, SigSu, Jump
    public static readonly AreaCostToggle.CostPreset[] Presets =
    {
        new AreaCostToggle.CostPreset { name = "Ayar 1", mud = 2f, water = 3f, jump = 2f },
        new AreaCostToggle.CostPreset { name = "Ayar 2", mud = 2f, water = 3f, jump = 6f },
        new AreaCostToggle.CostPreset { name = "Ayar 3", mud = 6f, water = 1f, jump = 6f },
        new AreaCostToggle.CostPreset { name = "Ayar 4", mud = 6f, water = 6f, jump = 10f },
    };

    [MenuItem("Hafta6/Build Scene")]
    public static void Build()
    {
        SetupInputActions();
        Directory.CreateDirectory("Assets/Scenes");
        Directory.CreateDirectory(MatDir);

        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // Materials
        Material ground = Mat("Ground", new Color(0.60f, 0.67f, 0.52f));
        Material wall = Mat("Wall", new Color(0.28f, 0.28f, 0.32f));
        Material mud = Mat("Mud", new Color(0.42f, 0.26f, 0.12f));
        Material water = Mat("Water", new Color(0.20f, 0.50f, 0.88f));
        Material door = Mat("Door", new Color(0.80f, 0.25f, 0.05f));
        Material npcMat = Mat("Npc", new Color(0.90f, 0.15f, 0.12f));
        Material playerMat = Mat("Player", new Color(0.15f, 0.35f, 0.95f));
        Material linkMat = Mat("LinkMarker", new Color(0.65f, 0.25f, 0.85f));
        Material pathMat = UnlitMat("PathLine", Color.yellow);

        // Light
        var lightGo = new GameObject("Directional Light");
        var light = lightGo.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 0.85f;
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.42f, 0.42f, 0.45f);
        light.shadows = LightShadows.Soft;
        lightGo.transform.rotation = Quaternion.Euler(60f, -30f, 0f);

        // Camera: top-down orthographic so the whole level and the yellow path are visible
        var camGo = new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        var cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 18f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.12f, 0.14f, 0.18f);
        camGo.transform.position = new Vector3(15f, 40f, 17f);   // leaves room above the level for the on-screen panel
        camGo.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        camGo.AddComponent<AudioListener>();

        // Level root with the NavMeshSurface (collects only its children)
        var alan = new GameObject("Alan");
        var surface = alan.AddComponent<NavMeshSurface>();
        surface.collectObjects = CollectObjects.Children;
        surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
        surface.agentTypeID = 0;   // Humanoid: radius 0.5, height 2, step 0.75, slope 45

        Box("Zemin", alan, new Vector3(15f, -0.1f, 15f), new Vector3(30f, 0.2f, 30f), ground, false);

        // Barrier pieces (1 m high, Not Walkable on top so their roofs are not baked)
        Box("Duvar_KapiAlti", alan, new Vector3(15f, 0.5f, 5f), new Vector3(6f, 1f, 4f), wall, true);      // z 3..7
        Box("Duvar_SuUstu", alan, new Vector3(15f, 0.5f, 13f), new Vector3(6f, 1f, 4f), wall, true);       // z 11..15
        Box("Duvar_Ince", alan, new Vector3(15f, 0.5f, 18f), new Vector3(1f, 1f, 6f), wall, true);         // z 15..21, x 14.5..15.5
        Box("Duvar_CamurAlti", alan, new Vector3(15f, 0.5f, 22f), new Vector3(6f, 1f, 2f), wall, true);    // z 21..23
        Box("Duvar_CamurUstu", alan, new Vector3(15f, 0.5f, 28.5f), new Vector3(6f, 1f, 3f), wall, true);  // z 27..30

        // Custom areas (NavMeshModifierVolume) + visuals without colliders
        Volume("Camur", alan, new Vector3(15f, 0f, 25f), new Vector3(6f, 2f, 4f), "Camur");
        Visual("CamurGorsel", alan, new Vector3(15f, 0.01f, 25f), new Vector3(6f, 0.02f, 4f), mud);
        Volume("SigSu", alan, new Vector3(15f, 0f, 9f), new Vector3(6f, 2f, 4f), "SigSu");
        Visual("SigSuGorsel", alan, new Vector3(15f, 0.01f, 9f), new Vector3(6f, 0.02f, 4f), water);

        // Moving obstacle: sliding door in the bottom corridor (excluded from the bake, carves when closed)
        var kapi = Box("Kapi", alan, new Vector3(15f, 1f, 1.5f), new Vector3(1f, 2f, 3f), door, false);
        var kapiMod = kapi.AddComponent<NavMeshModifier>();
        kapiMod.ignoreFromBuild = true;   // "Remove object" mode
        var obstacle = kapi.AddComponent<NavMeshObstacle>();
        obstacle.shape = NavMeshObstacleShape.Box;
        obstacle.center = Vector3.zero;
        obstacle.size = Vector3.one;      // x scale = 1 x 2 x 3 m
        obstacle.carving = true;
        obstacle.carvingMoveThreshold = 0.1f;
        obstacle.carvingTimeToStationary = 0.5f;
        obstacle.carveOnlyStationary = true;
        // The door starts open: SlidingDoor.Awake switches the obstacle off at runtime anyway. Keeping it off in the
        // saved scene too stops edit-mode carving from cutting the corridor in the Scene view and in editor queries.
        obstacle.enabled = false;
        var sliding = kapi.AddComponent<SlidingDoor>();
        sliding.startOpen = true;
        Visual("KapiIzi", alan, new Vector3(15f, 0.015f, 1.5f), new Vector3(1f, 0.02f, 3f), door);   // door footprint on the floor

        // Jump link over the thin wall (Jump area, bidirectional)
        var linkGo = new GameObject("AtlamaBaglantisi");
        linkGo.transform.SetParent(alan.transform, false);
        linkGo.transform.position = new Vector3(15f, 0f, 18f);
        var link = linkGo.AddComponent<NavMeshLink>();
        link.agentTypeID = 0;
        link.startPoint = new Vector3(-1.5f, 0f, 0f);   // world (13.5, 0, 18)
        link.endPoint = new Vector3(1.5f, 0f, 0f);      // world (16.5, 0, 18)
        link.width = 0f;
        link.bidirectional = true;
        link.area = NavMesh.GetAreaFromName("Jump");
        link.activated = true;
        Marker("BaglantiUcu_Bas", alan, new Vector3(13.5f, 0.02f, 18f), linkMat);
        Marker("BaglantiUcu_Son", alan, new Vector3(16.5f, 0.02f, 18f), linkMat);

        // Route letters on the floor
        Label("Etiket_A", "A  Çamur", new Vector3(15f, 0.05f, 26.1f));
        Label("Etiket_B", "B  Atlama", new Vector3(15f, 1.05f, 19.3f));
        Label("Etiket_C", "C  Sığ su", new Vector3(15f, 0.05f, 7.9f));
        Label("Etiket_D", "D  Kapı", new Vector3(21f, 0.05f, 0.9f));
        Label("Etiket_N", "N", NpcStart + new Vector3(0f, 0.05f, -1.2f));
        Label("Etiket_P", "P", PlayerStart + new Vector3(0f, 0.05f, -1.2f));

        // Character (blue capsule, WASD)
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

        // NPC (red sphere) with the five in-class scripts + the bonus
        var npc = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        npc.name = "NPC";
        npc.transform.position = NpcStart + Vector3.up * 0.5f;
        npc.GetComponent<Renderer>().sharedMaterial = npcMat;
        var agent = npc.AddComponent<NavMeshAgent>();
        agent.agentTypeID = 0;
        agent.baseOffset = 0.5f;
        agent.radius = 0.5f;
        agent.height = 1f;
        agent.speed = 3.5f;
        agent.angularSpeed = 360f;
        agent.acceleration = 8f;
        agent.stoppingDistance = 0f;
        agent.enabled = false;   // NpcNavChase.Awake enables it once the NavMeshSurface data is loaded
        var line = npc.AddComponent<LineRenderer>();
        line.sharedMaterial = pathMat;
        line.widthMultiplier = 0.18f;
        line.positionCount = 0;
        line.useWorldSpace = true;
        line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        line.receiveShadows = false;
        line.numCornerVertices = 2;
        var chase = npc.AddComponent<NpcNavChase>();
        chase.target = player.transform;
        npc.AddComponent<JumpLinkTraverser>();
        var toggle = npc.AddComponent<AreaCostToggle>();
        toggle.jumpLink = link;
        toggle.door = sliding;
        toggle.presets = (AreaCostToggle.CostPreset[])Presets.Clone();
        var debugDraw = npc.AddComponent<PathDebugDraw>();
        debugDraw.jumpLink = link;
        npc.AddComponent<MudSlowdown>();

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

        PlayerSettings.productName = "Hafta6 Kestirme mi Guvenli Yol mu";
        PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
        PlayerSettings.defaultScreenWidth = 1000;
        PlayerSettings.defaultScreenHeight = 900;
        PlayerSettings.resizableWindow = true;
        PlayerSettings.runInBackground = true;
        AssetDatabase.SaveAssets();

        var tri = NavMesh.CalculateTriangulation();
        Debug.Log($"[Hafta6] Scene saved to {ScenePath}. NavMesh: {tri.vertices.Length} vertices, {tri.indices.Length / 3} triangles.");
    }

    static void SetupInputActions()
    {
        AssetDatabase.ImportAsset(InputPath, ImportAssetOptions.ForceUpdate);
        var asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputPath);
        if (asset == null) { Debug.LogError("[Hafta6] Input actions asset not found: " + InputPath); return; }
        InputSystem.actions = asset;   // project-wide actions (used by InputSystem.actions.FindAction)
        Debug.Log("[Hafta6] Project-wide input actions: " + AssetDatabase.GetAssetPath(InputSystem.actions));
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

    static void Volume(string name, GameObject parent, Vector3 pos, Vector3 size, string areaName)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        go.transform.position = pos;
        var vol = go.AddComponent<NavMeshModifierVolume>();
        vol.size = size;
        vol.center = Vector3.zero;
        vol.area = NavMesh.GetAreaFromName(areaName);
        if (vol.area < 0) Debug.LogError($"[Hafta6] Area '{areaName}' is missing in ProjectSettings/NavMeshAreas.asset");
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

    static void Marker(string name, GameObject parent, Vector3 pos, Material m)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        go.name = name;
        Object.DestroyImmediate(go.GetComponent<Collider>());
        go.transform.SetParent(parent.transform, false);
        go.transform.position = pos;
        go.transform.localScale = new Vector3(0.6f, 0.01f, 0.6f);
        go.GetComponent<Renderer>().sharedMaterial = m;
    }

    static void Label(string name, string text, Vector3 pos)
    {
        var go = new GameObject(name);
        go.transform.position = pos;
        go.transform.rotation = Quaternion.Euler(90f, 0f, 0f);   // lie flat, readable from the top camera
        var tm = go.AddComponent<TextMesh>();
        tm.text = text;
        tm.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        tm.fontSize = 64;
        tm.characterSize = 0.1f;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        tm.color = new Color(0.05f, 0.05f, 0.12f);
        tm.fontStyle = FontStyle.Bold;
        go.GetComponent<MeshRenderer>().sharedMaterial = tm.font.material;
    }
}
