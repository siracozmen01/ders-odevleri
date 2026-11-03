using System.IO;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Builds both demo scenes from code (menu: Ders > Sahneleri Yeniden Oluştur).
/// Also runnable from the command line:
///   Unity.exe -batchmode -quit -projectPath . -executeMethod SceneBuilder.BuildAll
/// </summary>
public static class SceneBuilder
{
    const string ScenesDir = "Assets/Scenes";
    const string MatDir = "Assets/Materials";
    const string TerrainDir = "Assets/Terrain";
    const int GroundLayer = 6;
    const int ObstacleLayer = 7;

    [MenuItem("Ders/Sahneleri Yeniden Oluştur")]
    public static void BuildAll()
    {
        Directory.CreateDirectory(ScenesDir);
        Directory.CreateDirectory(MatDir);
        Directory.CreateDirectory(TerrainDir);
        SetupTagsAndLayers();

        string guard = BuildTerrainGuardScene();
        string astar = BuildAStarScene();

        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(guard, true),
            new EditorBuildSettingsScene(astar, true),
        };
        AssetDatabase.SaveAssets();
        EditorSceneManager.OpenScene(guard);
        Debug.Log("[SceneBuilder] Sahneler oluşturuldu.");
    }

    // ------------------------------------------------------------------ helpers

    static void SetupTagsAndLayers()
    {
        var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        var layers = tagManager.FindProperty("layers");
        layers.GetArrayElementAtIndex(GroundLayer).stringValue = "Ground";
        layers.GetArrayElementAtIndex(ObstacleLayer).stringValue = "Obstacle";

        var tags = tagManager.FindProperty("tags");
        bool has = false;
        for (int i = 0; i < tags.arraySize; i++) if (tags.GetArrayElementAtIndex(i).stringValue == "UserWall") has = true;
        if (!has)
        {
            tags.InsertArrayElementAtIndex(tags.arraySize);
            tags.GetArrayElementAtIndex(tags.arraySize - 1).stringValue = "UserWall";
        }
        tagManager.ApplyModifiedPropertiesWithoutUndo();
    }

    static Material Mat(string name, Color color, string shader = "Standard")
    {
        string path = $"{MatDir}/{name}.mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null)
        {
            m = new Material(Shader.Find(shader));
            AssetDatabase.CreateAsset(m, path);
        }
        m.color = color;
        if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", 0.15f);
        EditorUtility.SetDirty(m);
        return m;
    }

    static GameObject Prim(PrimitiveType type, string name, Vector3 pos, Vector3 scale, Material mat, int layer = 0, bool keepCollider = true)
    {
        var go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.position = pos;
        go.transform.localScale = scale;
        go.GetComponent<MeshRenderer>().sharedMaterial = mat;
        go.layer = layer;
        if (!keepCollider) Object.DestroyImmediate(go.GetComponent<Collider>());
        return go;
    }

    static NavMeshSurface BakeNavMesh(string assetPath)
    {
        var go = new GameObject("NavMesh Surface");
        var surface = go.AddComponent<NavMeshSurface>();
        surface.collectObjects = CollectObjects.All;
        surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
        surface.layerMask = (1 << GroundLayer) | (1 << ObstacleLayer);
        surface.BuildNavMesh();

        AssetDatabase.DeleteAsset(assetPath);
        AssetDatabase.CreateAsset(surface.navMeshData, assetPath);
        return surface;
    }

    static GameObject MakeCharacter(string name, Vector3 pos, Material body, Material visor)
    {
        var root = Prim(PrimitiveType.Capsule, name, pos, Vector3.one, body);
        // a small "visor" so you can see where the character is facing
        var v = Prim(PrimitiveType.Cube, "Visor", Vector3.zero, new Vector3(0.7f, 0.2f, 0.3f), visor, 0, false);
        v.transform.SetParent(root.transform, false);
        v.transform.localPosition = new Vector3(0f, 0.55f, 0.4f);
        return root;
    }

    // ------------------------------------------------------------------ Scene 1: Terrain & Gözcü

    static string BuildTerrainGuardScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        Random.InitState(42);

        var matPlayer = Mat("Player", new Color(0.2f, 0.45f, 1f));
        var matGuard = Mat("Guard", new Color(0.85f, 0.2f, 0.2f));
        var matVisor = Mat("Visor", new Color(0.1f, 0.1f, 0.1f));
        var matRock = Mat("Rock", new Color(0.45f, 0.43f, 0.4f));
        var matWall = Mat("Wall", new Color(0.55f, 0.42f, 0.3f));
        var matWaypoint = Mat("Waypoint", new Color(0.2f, 0.9f, 0.9f));
        var matCone = Mat("VisionCone", new Color(1f, 0.9f, 0.2f, 0.28f), "Sprites/Default");

        // ---- Terrain
        const float size = 80f, height = 13f;
        var data = new TerrainData { heightmapResolution = 129 };
        data.size = new Vector3(size, height, size);
        int res = data.heightmapResolution;
        var h = new float[res, res];
        for (int y = 0; y < res; y++)
            for (int x = 0; x < res; x++)
            {
                float u = (float)x / (res - 1), v = (float)y / (res - 1);
                float hills = Mathf.PerlinNoise(u * 4f + 3.1f, v * 4f + 7.7f) * 0.35f;
                // a long ridge across the map: you can hide behind it
                float ridge = Mathf.Exp(-Mathf.Pow((u - 0.55f) * 9f, 2f)) * Mathf.Clamp01(1f - Mathf.Abs(v - 0.5f) * 2.4f) * 0.55f;
                // flatten the edges a bit
                float edge = Mathf.Clamp01(Mathf.Min(Mathf.Min(u, 1 - u), Mathf.Min(v, 1 - v)) * 8f);
                h[y, x] = (hills + ridge) * edge;
            }
        data.SetHeights(0, 0, h);

        var grassTex = MakeNoiseTexture($"{TerrainDir}/Grass.png", new Color(0.32f, 0.52f, 0.22f), new Color(0.42f, 0.6f, 0.28f));
        var layer = new TerrainLayer { diffuseTexture = grassTex, tileSize = new Vector2(6, 6) };
        AssetDatabase.DeleteAsset($"{TerrainDir}/Grass.terrainlayer");
        AssetDatabase.CreateAsset(layer, $"{TerrainDir}/Grass.terrainlayer");
        data.terrainLayers = new[] { layer };
        AssetDatabase.DeleteAsset($"{TerrainDir}/GozcuTerrain.asset");
        AssetDatabase.CreateAsset(data, $"{TerrainDir}/GozcuTerrain.asset");

        var terrainGo = Terrain.CreateTerrainGameObject(data);
        terrainGo.name = "Terrain";
        terrainGo.layer = GroundLayer;
        terrainGo.transform.position = new Vector3(-size / 2f, 0f, -size / 2f);
        var terrain = terrainGo.GetComponent<Terrain>();

        float G(float x, float z) => terrain.SampleHeight(new Vector3(x, 0, z)) + terrainGo.transform.position.y;
        Vector3 OnGround(float x, float z, float up = 0f) => new Vector3(x, G(x, z) + up, z);

        // ---- Obstacles (rocks + walls) to hide behind
        var obstacles = new GameObject("Obstacles");
        Vector2[] rockSpots = { new(-18, 10), new(-8, -14), new(6, 18), new(22, -6), new(-26, -4), new(14, -22), new(-4, 26), new(28, 20), new(-14, -26), new(2, 2) };
        foreach (var s in rockSpots)
        {
            float sc = Random.Range(2f, 3.5f);
            var r = Prim(PrimitiveType.Sphere, "Rock", OnGround(s.x, s.y, sc * 0.25f), new Vector3(sc * 1.3f, sc, sc), matRock, ObstacleLayer);
            r.transform.SetParent(obstacles.transform);
        }
        (Vector2 pos, float rotY, float len)[] walls =
        {
            (new(-10, 0), 0f, 10f), (new(12, 8), 90f, 12f), (new(-22, 18), 45f, 8f), (new(18, -14), 0f, 9f), (new(0, -20), 90f, 8f),
        };
        foreach (var w in walls)
        {
            var wall = Prim(PrimitiveType.Cube, "Wall", OnGround(w.pos.x, w.pos.y, 1.25f), new Vector3(w.len, 2.5f, 0.6f), matWall, ObstacleLayer);
            wall.transform.rotation = Quaternion.Euler(0, w.rotY, 0);
            wall.transform.SetParent(obstacles.transform);
        }
        foreach (Transform t in obstacles.transform)
            GameObjectUtility.SetStaticEditorFlags(t.gameObject, StaticEditorFlags.BatchingStatic);

        // ---- NavMesh (terrain + obstacles only)
        BakeNavMesh($"{ScenesDir}/TerrainGozcu_NavMesh.asset");

        // ---- Player
        var player = MakeCharacter("Player", OnGround(-32, -32, 1.1f), matPlayer, matVisor);
        player.tag = "Player";
        Object.DestroyImmediate(player.GetComponent<CapsuleCollider>());
        var cc = player.AddComponent<CharacterController>();
        cc.height = 2f; cc.radius = 0.45f;
        player.AddComponent<PlayerController>();

        // ---- Guards with patrol routes
        var guardsRoot = new GameObject("Guards");
        var waypointsRoot = new GameObject("Waypoints");
        Vector2[][] routes =
        {
            new Vector2[] { new(-20, -10), new(-20, 14), new(-4, 14), new(-4, -10) },          // square around the left area
            new Vector2[] { new(8, -30), new(30, -30), new(30, -2), new(8, -2) },                // bottom right
            new Vector2[] { new(-28, 30), new(0, 32), new(26, 30), new(0, 22) },                 // top
        };
        int obstacleMask = (1 << 0) | (1 << GroundLayer) | (1 << ObstacleLayer); // Default (player) + Ground + Obstacle
        for (int g = 0; g < routes.Length; g++)
        {
            var wps = new Transform[routes[g].Length];
            for (int i = 0; i < wps.Length; i++)
            {
                var wp = Prim(PrimitiveType.Cylinder, $"Gozcu{g + 1}_WP{i + 1}", OnGround(routes[g][i].x, routes[g][i].y, 0.05f), new Vector3(0.6f, 0.05f, 0.6f), matWaypoint, 0, false);
                wp.transform.SetParent(waypointsRoot.transform);
                wps[i] = wp.transform;
            }

            var guard = MakeCharacter($"Gozcu {g + 1}", OnGround(routes[g][0].x, routes[g][0].y, 1f), matGuard, matVisor);
            guard.layer = 2; // Ignore Raycast: guards don't block each other's view
            foreach (Transform c in guard.transform) c.gameObject.layer = 2;
            guard.transform.SetParent(guardsRoot.transform);
            var agent = guard.AddComponent<NavMeshAgent>();
            agent.baseOffset = 1f; agent.radius = 0.45f; agent.height = 2f;
            agent.angularSpeed = 360f; agent.acceleration = 12f; agent.stoppingDistance = 0.3f;
            var ai = guard.AddComponent<GuardAI>();
            ai.waypoints = wps;
            ai.obstacleMask = obstacleMask;

            var cone = new GameObject($"Gozcu {g + 1} Gorus Konisi");
            cone.transform.SetParent(guardsRoot.transform);
            cone.AddComponent<MeshFilter>();
            var mr = cone.AddComponent<MeshRenderer>();
            mr.sharedMaterial = matCone;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            cone.AddComponent<VisionConeMesh>().guard = ai;
        }

        // ---- Alarm: siren + flashing red lights
        var alarm = new GameObject("Alarm System");
        var alarmComp = alarm.AddComponent<AlarmSystem>();
        Vector2[] lightSpots = { new(-25, -25), new(25, -25), new(-25, 25), new(25, 25), new(0, 0) };
        alarmComp.alarmLights = new Light[lightSpots.Length];
        for (int i = 0; i < lightSpots.Length; i++)
        {
            var pole = Prim(PrimitiveType.Cylinder, "Alarm Diregi", OnGround(lightSpots[i].x, lightSpots[i].y, 2.5f), new Vector3(0.25f, 2.5f, 0.25f), matVisor, 0, false);
            pole.transform.SetParent(alarm.transform);
            var lgo = new GameObject("Alarm Isigi");
            lgo.transform.SetParent(alarm.transform);
            lgo.transform.position = OnGround(lightSpots[i].x, lightSpots[i].y, 5.5f);
            var l = lgo.AddComponent<Light>();
            l.type = LightType.Point; l.color = Color.red; l.range = 30f; l.intensity = 3f;
            alarmComp.alarmLights[i] = l;
        }

        new GameObject("UI").AddComponent<GameUI>();

        // ---- Camera
        var cam = Camera.main;
        cam.transform.position = player.transform.position + new Vector3(0, 18, -14);
        cam.transform.LookAt(player.transform);
        cam.farClipPlane = 300f;
        cam.gameObject.AddComponent<FollowCamera>().target = player.transform;

        string path = $"{ScenesDir}/TerrainGozcu.unity";
        EditorSceneManager.SaveScene(scene, path);
        return path;
    }

    static Texture2D MakeNoiseTexture(string path, Color a, Color b)
    {
        const int n = 128;
        var tex = new Texture2D(n, n);
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float t = Mathf.PerlinNoise(x * 0.15f, y * 0.15f) * 0.7f + Random.value * 0.3f;
                tex.SetPixel(x, y, Color.Lerp(a, b, t));
            }
        File.WriteAllBytes(path, tex.EncodeToPNG());
        AssetDatabase.ImportAsset(path);
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    // ------------------------------------------------------------------ Scene 2: NavMesh & A*

    static string BuildAStarScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        var matGround = Mat("Ground", new Color(0.78f, 0.8f, 0.82f));
        var matWall = Mat("MazeWall", new Color(0.25f, 0.27f, 0.32f));
        var matAStar = Mat("AStarAgent", new Color(0.2f, 0.45f, 1f));
        var matNav = Mat("NavMeshAgent", new Color(0.2f, 0.8f, 0.3f));
        var matTarget = Mat("Target", new Color(1f, 0.8f, 0.1f));
        var matVisor = Mat("Visor", new Color(0.1f, 0.1f, 0.1f));

        var ground = Prim(PrimitiveType.Plane, "Ground", Vector3.zero, new Vector3(4, 1, 4), matGround, GroundLayer);

        // maze walls, all aligned to the 1m grid: (centerX, centerZ, sizeX, sizeZ)
        var wallsRoot = new GameObject("Walls");
        (float x, float z, float w, float d)[] walls =
        {
            (0, 20.5f, 41, 1), (0, -20.5f, 41, 1), (20.5f, 0, 1, 40), (-20.5f, 0, 1, 40),   // border
            // serpentine: every corridor is at least 3m wide so the NavMesh agent fits too
            (-11.5f, -6, 1, 28),                    // gap at the top
            (-3.5f, 6, 1, 28),                      // gap at the bottom
            (4.5f, -5, 1, 30),                      // gap at the top
            (12.5f, 5, 1, 30),                      // gap at the bottom
            // short stubs that force detours
            (-9, -2.5f, 4, 1), (7, 14.5f, 4, 1), (18, 0.5f, 4, 1), (-17.5f, 4.5f, 5, 1), (-1, -12.5f, 4, 1),
        };
        foreach (var w in walls)
        {
            var go = Prim(PrimitiveType.Cube, "Wall", new Vector3(w.x, 1f, w.z), new Vector3(w.w, 2f, w.d), matWall, ObstacleLayer);
            go.transform.SetParent(wallsRoot.transform);
            GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic);
        }

        BakeNavMesh($"{ScenesDir}/AStar_NavMesh.asset");

        // A* grid
        var gridGo = new GameObject("A* Grid");
        var grid = gridGo.AddComponent<AStarGrid>();
        grid.gridWorldSize = new Vector2(40, 40);
        grid.nodeRadius = 0.5f;
        grid.obstacleMask = 1 << ObstacleLayer;

        // agents
        var aStarAgent = MakeCharacter("A* Ajani (Mavi)", new Vector3(-16.5f, 1f, -16.5f), matAStar, matVisor);
        Object.DestroyImmediate(aStarAgent.GetComponent<Collider>());

        var navGo = MakeCharacter("NavMesh Ajani (Yesil)", new Vector3(-16.5f, 1f, -14.5f), matNav, matVisor);
        Object.DestroyImmediate(navGo.GetComponent<Collider>());
        var nav = navGo.AddComponent<NavMeshAgent>();
        nav.baseOffset = 1f; nav.speed = 5f; nav.angularSpeed = 720f; nav.acceleration = 30f; nav.radius = 0.4f;

        var target = Prim(PrimitiveType.Cylinder, "Hedef", new Vector3(16.5f, 0.05f, 16.5f), new Vector3(0.9f, 0.05f, 0.9f), matTarget, 0, false);

        var demo = new GameObject("Demo").AddComponent<AStarDemo>();
        demo.grid = grid;
        demo.aStarAgent = aStarAgent.transform;
        demo.navMeshAgent = nav;
        demo.groundMask = 1 << GroundLayer;
        demo.obstacleMask = 1 << ObstacleLayer;
        demo.wallMaterial = matWall;
        demo.targetMarker = target.transform;

        var cam = Camera.main;
        cam.transform.position = new Vector3(0, 40, -22);
        cam.transform.LookAt(new Vector3(0, 0, -1));
        cam.fieldOfView = 55f;

        string path = $"{ScenesDir}/NavMesh_AStar.unity";
        EditorSceneManager.SaveScene(scene, path);
        return path;
    }
}
