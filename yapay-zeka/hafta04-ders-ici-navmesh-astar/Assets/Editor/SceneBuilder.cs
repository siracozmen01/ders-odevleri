using System.IO;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Builds the NavMesh vs A* maze scene (Assets/Scenes/NavMesh_AStar.unity) from code,
/// sets up the Ground/Obstacle layers and the UserWall tag, and bakes the NavMesh.
/// Menu: Ders > Sahneyi Yeniden Oluştur. Also runnable from the command line:
///   Unity.exe -batchmode -quit -projectPath . -executeMethod SceneBuilder.BuildAll
/// </summary>
public static class SceneBuilder
{
    const string ScenesDir = "Assets/Scenes";
    const string MatDir = "Assets/Materials";
    const int GroundLayer = 6;
    const int ObstacleLayer = 7;

    [MenuItem("Ders/Sahneyi Yeniden Oluştur")]
    public static void BuildAll()
    {
        Directory.CreateDirectory(ScenesDir);
        Directory.CreateDirectory(MatDir);
        SetupTagsAndLayers();

        string scene = BuildAStarScene();

        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(scene, true) };
        AssetDatabase.SaveAssets();
        EditorSceneManager.OpenScene(scene);
        Debug.Log("[SceneBuilder] Scene built: " + scene);
    }

    // ------------------------------------------------------------------ helpers

    static void SetupTagsAndLayers()
    {
        var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        var layers = tagManager.FindProperty("layers");
        layers.GetArrayElementAtIndex(GroundLayer).stringValue = "Ground";
        layers.GetArrayElementAtIndex(ObstacleLayer).stringValue = "Obstacle";

        // "UserWall" marks walls added at runtime with right click, so only those can be removed again
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

    // ------------------------------------------------------------------ Scene: NavMesh & A*

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
