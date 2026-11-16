using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// Builds the whole assignment scene from code so it is reproducible:
/// procedural textures and TerrainLayers, a shaped and painted terrain with a hiding hill,
/// a 40-degree ramp (height trap demo), trees with colliders, the player, the follow camera,
/// two guards and the HUD.
/// Menu: Gozcu > Build Scene, or batch mode: -executeMethod GozcuSceneBuilder.Build
/// </summary>
public static class GozcuSceneBuilder
{
    const string Root = "Assets";
    const string ScenePath = "Assets/Scenes/Gozcu.unity";
    const string InputPath = "Assets/Input/GozcuControls.inputactions";

    // Terrain: 200 x 50 x 200 m, centred on the world origin (lab step 1).
    static readonly Vector3 TerrainSize = new Vector3(200f, 50f, 200f);
    static readonly Vector3 TerrainOrigin = new Vector3(-100f, 0f, -100f);
    const int HeightRes = 257;
    const int AlphaRes = 256;

    // Layout (world XZ). See README for the demo route.
    public static readonly Vector2 PlayerStart = new Vector2(0f, -28f);
    public static readonly Vector2 GuardA = new Vector2(0f, 22f);    // faces -Z (south)
    public static readonly Vector2 GuardB = new Vector2(40f, 0f);    // faces +Z, towards the ramp
    public static readonly Vector2 HillCentre = new Vector2(-3f, 14f);
    const float HillRadiusX = 5f, HillRadiusZ = 4f, HillHeight = 3.5f;
    public static readonly Vector2 TreeT1 = new Vector2(5f, 12f);
    const float RampStartZ = 2f, RampSlopeDeg = 40f, RampTop = 10f, RampEndZ = 26f;

    [MenuItem("Gozcu/Build Scene")]
    public static void Build()
    {
        foreach (string f in new[] { "Scenes", "Materials", "Terrain", "Terrain/Textures", "Prefabs", "Input" })
            Directory.CreateDirectory(Path.Combine(Root, f));
        AssetDatabase.Refresh();

        // ---------- materials ----------
        Material playerMat = Mat("Player", new Color(0.2f, 0.45f, 0.95f));
        Material visorMat = Mat("Visor", new Color(0.08f, 0.08f, 0.12f));
        Material guardMat = Mat("Guard", Color.green);
        Material noseMat = Mat("Nose", new Color(0.12f, 0.12f, 0.12f));
        Material barkMat = Mat("Bark", new Color(0.36f, 0.24f, 0.14f));
        Material leavesMat = Mat("Leaves", new Color(0.16f, 0.42f, 0.16f));
        Material coneMat = new Material(Shader.Find("Sprites/Default")) { name = "VisionCone" };
        coneMat = Save(coneMat, "Assets/Materials/VisionCone.mat");

        // ---------- terrain ----------
        TerrainLayer[] layers =
        {
            Layer("Grass", MakeTexture("Grass", new Color(0.30f, 0.47f, 0.18f), new Color(0.20f, 0.36f, 0.12f), 11), 6f),
            Layer("Rock", MakeTexture("Rock", new Color(0.50f, 0.49f, 0.46f), new Color(0.32f, 0.31f, 0.30f), 23), 8f),
            Layer("Dirt", MakeTexture("Dirt", new Color(0.52f, 0.40f, 0.26f), new Color(0.38f, 0.28f, 0.18f), 37), 5f),
        };
        GameObject treePrefab = CreateTreePrefab(barkMat, leavesMat);
        TerrainData data = CreateTerrainData(layers, treePrefab);

        // Created last: later AssetDatabase writes can unload an earlier loaded reference.
        CreateInputActions();

        // ---------- scene ----------
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        // Loaded after NewScene: opening a new scene unloads unreferenced assets.
        var input = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputPath);

        Light sun = Object.FindAnyObjectByType<Light>();
        sun.transform.rotation = Quaternion.Euler(48f, 25f, 0f);
        sun.shadows = LightShadows.Soft;
        sun.intensity = 1.1f;

        GameObject terrainGo = Terrain.CreateTerrainGameObject(data);
        terrainGo.name = "Terrain";
        terrainGo.transform.position = TerrainOrigin;
        Terrain terrain = terrainGo.GetComponent<Terrain>();
        terrain.treeDistance = 400f;
        terrain.treeBillboardDistance = 400f;
        terrain.heightmapPixelError = 2f;
        terrain.basemapDistance = 400f;
        TerrainCollider tc = terrainGo.GetComponent<TerrainCollider>();
        var tcSo = new SerializedObject(tc);
        SerializedProperty treeColliders = tcSo.FindProperty("m_EnableTreeColliders");
        if (treeColliders != null) { treeColliders.boolValue = true; tcSo.ApplyModifiedPropertiesWithoutUndo(); }
        Debug.Log("[Builder] Tree colliders property found: " + (treeColliders != null));

        int playerLayer = LayerMask.NameToLayer("Player");
        int guardLayer = LayerMask.NameToLayer("Guard");
        if (playerLayer < 0 || guardLayer < 0) throw new System.Exception("Layers 'Player' and 'Guard' are missing in the TagManager.");

        // Player: capsule with a CharacterController (no CapsuleCollider), pivot in the middle.
        GameObject player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        player.name = "Player";
        Object.DestroyImmediate(player.GetComponent<CapsuleCollider>());
        player.GetComponent<Renderer>().sharedMaterial = playerMat;
        player.transform.position = OnGround(data, PlayerStart, 1.08f);
        var cc = player.AddComponent<CharacterController>();
        cc.height = 2f; cc.radius = 0.5f; cc.center = Vector3.zero; cc.slopeLimit = 45f; cc.stepOffset = 0.3f;
        GameObject visor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        visor.name = "Visor";
        Object.DestroyImmediate(visor.GetComponent<BoxCollider>());
        visor.transform.SetParent(player.transform, false);
        visor.transform.localPosition = new Vector3(0f, 0.5f, 0.42f);
        visor.transform.localScale = new Vector3(0.6f, 0.18f, 0.25f);
        visor.GetComponent<Renderer>().sharedMaterial = visorMat;
        SetLayer(player, playerLayer);

        // Camera
        Camera cam = Camera.main;
        cam.farClipPlane = 600f;
        cam.fieldOfView = 60f;
        var follow = cam.gameObject.AddComponent<FollowCamera>();
        Set(follow, "actions", input);
        Set(follow, "target", player.transform);

        var move = player.AddComponent<PlayerMovement>();
        Set(move, "actions", input);
        Set(move, "cameraTransform", cam.transform);
        cam.transform.position = player.transform.position + new Vector3(0f, 3f, -6f);
        cam.transform.LookAt(player.transform.position);

        // Guards
        Gozcu a = CreateGuard("Gozcu_A", data, GuardA, 180f, player.transform, guardMat, noseMat, coneMat, guardLayer);
        Gozcu b = CreateGuard("Gozcu_B", data, GuardB, 0f, player.transform, guardMat, noseMat, coneMat, guardLayer);

        // HUD
        var hud = new GameObject("HUD").AddComponent<HelpOverlay>();
        Set(hud, "actions", input);
        Set(hud, "player", move);
        Set(hud, "followCamera", follow);

        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Skybox;
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogStartDistance = 120f;
        RenderSettings.fogEndDistance = 400f;
        RenderSettings.fogColor = new Color(0.74f, 0.82f, 0.9f);

        if (new SerializedObject(move).FindProperty("actions").objectReferenceValue == null)
            throw new System.Exception("Input actions reference was not stored in the scene.");
        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        AssetDatabase.SaveAssets();
        Debug.Log($"[Builder] Scene saved: {ScenePath}. Guards: {a.name}, {b.name}");
    }

    // ------------------------------------------------------------------
    static Gozcu CreateGuard(string name, TerrainData data, Vector2 xz, float yaw, Transform player,
                             Material body, Material noseMat, Material coneMat, int layer)
    {
        GameObject g = GameObject.CreatePrimitive(PrimitiveType.Capsule);   // keeps its CapsuleCollider
        g.name = name;
        g.transform.SetPositionAndRotation(OnGround(data, xz, 1f), Quaternion.Euler(0f, yaw, 0f));
        g.GetComponent<Renderer>().sharedMaterial = body;

        GameObject nose = GameObject.CreatePrimitive(PrimitiveType.Cube);   // the "nose" shows the front
        nose.name = "Nose";
        Object.DestroyImmediate(nose.GetComponent<BoxCollider>());
        nose.transform.SetParent(g.transform, false);
        nose.transform.localPosition = new Vector3(0f, 0.6f, 0.6f);
        nose.transform.localScale = new Vector3(0.25f, 0.25f, 0.5f);
        nose.GetComponent<Renderer>().sharedMaterial = noseMat;
        SetLayer(g, layer);

        Gozcu gozcu = g.AddComponent<Gozcu>();
        Set(gozcu, "oyuncu", player);
        var so = new SerializedObject(gozcu);
        so.FindProperty("engeller").intValue = 1 << 0;   // Default only: terrain + trees, not Player / Guard
        so.ApplyModifiedPropertiesWithoutUndo();

        var view = g.AddComponent<VisionConeView>();
        Set(view, "material", coneMat);
        return gozcu;
    }

    // ------------------------------------------------------------------
    // Height field
    // ------------------------------------------------------------------
    public static float HeightAt(float x, float z)
    {
        // Gentle base with low-frequency noise (+-0.3 m) around 1 m.
        float h = 1f + 0.6f * (Mathf.PerlinNoise(x * 0.035f + 13.1f, z * 0.035f + 7.7f) - 0.5f);

        // Rolling hills near the border.
        float r = Mathf.Sqrt(x * x + z * z);
        h += 9f * Smooth(Mathf.InverseLerp(70f, 98f, r)) * (0.6f + 0.8f * Mathf.PerlinNoise(x * 0.02f, z * 0.02f));
        h += Bump(x, z, -48f, 38f, 16f, 16f, 7f);
        h += Bump(x, z, 52f, -45f, 14f, 14f, 6f);
        h += Bump(x, z, -55f, -40f, 18f, 12f, 5f);

        // Hiding hill in front of guard A.
        h += Bump(x, z, HillCentre.x, HillCentre.y, HillRadiusX, HillRadiusZ, HillHeight);

        // 40-degree ramp up to a 10 m plateau in front of guard B, with steep (> 45 deg) sides and back.
        float ramp = Mathf.Clamp((z - RampStartZ) * Mathf.Tan(RampSlopeDeg * Mathf.Deg2Rad), 0f, RampTop);
        float back = Mathf.Clamp01(1f - (z - RampEndZ) / 5f);
        float side = 1f - Smooth(Mathf.InverseLerp(8f, 13f, Mathf.Abs(x - GuardB.x)));
        h = Mathf.Max(h, 1f + ramp * Smooth(back) * side);

        // Flat pads under the guards.
        h = Mathf.Lerp(1f, h, Smooth(Mathf.InverseLerp(2f, 4f, Vector2.Distance(new Vector2(x, z), GuardA))));
        h = Mathf.Lerp(1f, h, Smooth(Mathf.InverseLerp(1.5f, 2f, Vector2.Distance(new Vector2(x, z), GuardB))));
        return h;
    }

    static float Bump(float x, float z, float cx, float cz, float rx, float rz, float height)
    {
        float dx = (x - cx) / rx, dz = (z - cz) / rz;
        float d = Mathf.Sqrt(dx * dx + dz * dz);
        return d >= 1f ? 0f : height * 0.5f * (1f + Mathf.Cos(Mathf.PI * d));   // cosine bump
    }

    static float Smooth(float t) => t * t * (3f - 2f * t);

    static TerrainData CreateTerrainData(TerrainLayer[] layers, GameObject treePrefab)
    {
        var data = new TerrainData();
        data.heightmapResolution = HeightRes;
        data.size = TerrainSize;
        data.alphamapResolution = AlphaRes;
        data.baseMapResolution = 512;
        // Make it an asset first, so the splat (alpha) textures are stored as its sub-assets.
        data = Save(data, "Assets/Terrain/GozcuTerrain.asset");

        var heights = new float[HeightRes, HeightRes];
        for (int j = 0; j < HeightRes; j++)
            for (int i = 0; i < HeightRes; i++)
            {
                float x = TerrainOrigin.x + TerrainSize.x * i / (HeightRes - 1);
                float z = TerrainOrigin.z + TerrainSize.z * j / (HeightRes - 1);
                heights[j, i] = HeightAt(x, z) / TerrainSize.y;
            }
        data.SetHeights(0, 0, heights);

        // Paint: grass everywhere, rock on slopes, dirt on the path and under the guards.
        data.terrainLayers = layers;
        var alpha = new float[AlphaRes, AlphaRes, 3];
        for (int j = 0; j < AlphaRes; j++)
            for (int i = 0; i < AlphaRes; i++)
            {
                float u = (i + 0.5f) / AlphaRes, v = (j + 0.5f) / AlphaRes;
                float x = TerrainOrigin.x + TerrainSize.x * u, z = TerrainOrigin.z + TerrainSize.z * v;
                float steep = data.GetSteepness(u, v);
                float rock = Smooth(Mathf.InverseLerp(24f, 36f, steep));
                float wobble = 0.6f * Mathf.Sin(z * 0.25f) + 0.4f * Mathf.PerlinNoise(x * 0.3f, z * 0.3f);
                float path = (z > -32f && z < 7f) ? 1f - Smooth(Mathf.InverseLerp(1.0f, 2.2f, Mathf.Abs(x + 1.5f - wobble))) : 0f;
                float pads = Mathf.Max(
                    1f - Smooth(Mathf.InverseLerp(2f, 3.5f, Vector2.Distance(new Vector2(x, z), GuardA))),
                    1f - Smooth(Mathf.InverseLerp(2f, 3.5f, Vector2.Distance(new Vector2(x, z), GuardB))));
                float patches = Smooth(Mathf.InverseLerp(0.62f, 0.72f, Mathf.PerlinNoise(x * 0.06f + 50f, z * 0.06f + 20f)));
                float dirt = Mathf.Max(Mathf.Max(path, pads), patches * 0.7f) * (1f - rock);
                float grass = Mathf.Max(0f, 1f - rock - dirt);
                float sum = grass + rock + dirt;
                alpha[j, i, 0] = grass / sum;
                alpha[j, i, 1] = rock / sum;
                alpha[j, i, 2] = dirt / sum;
            }
        data.SetAlphamaps(0, 0, alpha);

        // Trees (prefab has a CapsuleCollider; tree colliders are enabled on the TerrainCollider).
        data.treePrototypes = new[] { new TreePrototype { prefab = treePrefab } };
        data.RefreshPrototypes();
        var trees = new System.Collections.Generic.List<TreeInstance>();
        void AddTree(float x, float z, float scale)
        {
            trees.Add(new TreeInstance
            {
                position = new Vector3((x - TerrainOrigin.x) / TerrainSize.x, 0f, (z - TerrainOrigin.z) / TerrainSize.z),
                widthScale = scale, heightScale = scale, prototypeIndex = 0,
                color = Color.white, lightmapColor = Color.white, rotation = 0f,
            });
        }
        // Hand-placed trees: T1 is the "hide behind a tree" spot inside guard A's cone,
        // the others give cover along the approach route.
        AddTree(TreeT1.x, TreeT1.y, 1.1f);
        AddTree(-9f, 3f, 1f);
        AddTree(6f, -6f, 1.05f);
        AddTree(-7f, -14f, 0.95f);
        AddTree(9f, 4f, 1f);
        AddTree(-12f, -22f, 1.1f);
        AddTree(28f, -6f, 1f);
        AddTree(52f, 6f, 1.05f);

        var rng = new System.Random(3);
        int placed = 0, tries = 0;
        while (placed < 45 && tries++ < 5000)
        {
            float x = (float)(rng.NextDouble() * 170 - 85), z = (float)(rng.NextDouble() * 170 - 85);
            bool nearRoute = x > -16f && x < 16f && z > -36f && z < 32f;
            bool nearRamp = x > 24f && x < 56f && z > -10f && z < 36f;
            if (nearRoute || nearRamp) continue;
            if (HeightAt(x, z) > 7f) continue;
            AddTree(x, z, 0.85f + 0.4f * (float)rng.NextDouble());
            placed++;
        }
        data.SetTreeInstances(trees.ToArray(), true);

        EditorUtility.SetDirty(data);
        AssetDatabase.SaveAssets();
        return data;
    }

    static Vector3 OnGround(TerrainData data, Vector2 xz, float up)
    {
        float u = (xz.x - TerrainOrigin.x) / TerrainSize.x, v = (xz.y - TerrainOrigin.z) / TerrainSize.z;
        return new Vector3(xz.x, TerrainOrigin.y + data.GetInterpolatedHeight(u, v) + up, xz.y);
    }

    // ------------------------------------------------------------------
    // Procedural, tileable textures (periodic value noise) and terrain layers
    // ------------------------------------------------------------------
    static Texture2D MakeTexture(string name, Color light, Color dark, int seed)
    {
        const int size = 256;
        var rng = new System.Random(seed);
        float[][] lattices = new float[4][];
        int[] cells = { 4, 8, 16, 64 };
        float[] weights = { 0.35f, 0.3f, 0.2f, 0.15f };
        for (int o = 0; o < 4; o++)
        {
            lattices[o] = new float[cells[o] * cells[o]];
            for (int k = 0; k < lattices[o].Length; k++) lattices[o][k] = (float)rng.NextDouble();
        }
        var tex = new Texture2D(size, size, TextureFormat.RGB24, false);
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float n = 0f;
                for (int o = 0; o < 4; o++)
                {
                    int c = cells[o];
                    float fx = (float)x / size * c, fy = (float)y / size * c;
                    int x0 = Mathf.FloorToInt(fx), y0 = Mathf.FloorToInt(fy);
                    float tx = Smooth(fx - x0), ty = Smooth(fy - y0);
                    float a = lattices[o][(y0 % c) * c + x0 % c], b = lattices[o][(y0 % c) * c + (x0 + 1) % c];
                    float d = lattices[o][((y0 + 1) % c) * c + x0 % c], e = lattices[o][((y0 + 1) % c) * c + (x0 + 1) % c];
                    n += weights[o] * Mathf.Lerp(Mathf.Lerp(a, b, tx), Mathf.Lerp(d, e, tx), ty);
                }
                tex.SetPixel(x, y, Color.Lerp(dark, light, Mathf.Clamp01((n - 0.25f) * 2f)));
            }
        tex.Apply();
        string path = $"Assets/Terrain/Textures/{name}.png";
        File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(path);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.wrapMode = TextureWrapMode.Repeat;
        importer.mipmapEnabled = true;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    static TerrainLayer Layer(string name, Texture2D texture, float tile)
    {
        var layer = new TerrainLayer { diffuseTexture = texture, tileSize = new Vector2(tile, tile), name = name };
        return Save(layer, $"Assets/Terrain/{name}.terrainlayer");
    }

    // ------------------------------------------------------------------
    // Tree prefab: one mesh (trunk + canopy as two sub-meshes) with a CapsuleCollider on the root.
    // ------------------------------------------------------------------
    static GameObject CreateTreePrefab(Material bark, Material leaves)
    {
        Mesh cylinder = Resources.GetBuiltinResource<Mesh>("New-Cylinder.fbx");
        Mesh sphere = Resources.GetBuiltinResource<Mesh>("New-Sphere.fbx");
        var parts = new[]
        {
            new CombineInstance { mesh = cylinder, transform = Matrix4x4.TRS(new Vector3(0f, 1.6f, 0f), Quaternion.identity, new Vector3(0.8f, 1.6f, 0.8f)) },
            new CombineInstance { mesh = sphere, transform = Matrix4x4.TRS(new Vector3(0f, 4.3f, 0f), Quaternion.identity, new Vector3(3.2f, 3.4f, 3.2f)) },
        };
        var mesh = new Mesh { name = "TreeMesh" };
        mesh.CombineMeshes(parts, false, true);
        mesh.RecalculateBounds();
        mesh = Save(mesh, "Assets/Prefabs/TreeMesh.asset");

        var go = new GameObject("Tree");
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        go.AddComponent<MeshRenderer>().sharedMaterials = new[] { bark, leaves };
        var col = go.AddComponent<CapsuleCollider>();
        col.center = new Vector3(0f, 3f, 0f);
        col.radius = 0.4f;
        col.height = 6f;
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, "Assets/Prefabs/Tree.prefab");
        Object.DestroyImmediate(go);
        return prefab;
    }

    // ------------------------------------------------------------------
    // Input actions (.inputactions asset) - same idea as InputSystem_Actions in the notes.
    // ------------------------------------------------------------------
    static InputActionAsset CreateInputActions()
    {
        var asset = ScriptableObject.CreateInstance<InputActionAsset>();
        asset.name = "GozcuControls";

        InputActionMap player = asset.AddActionMap("Player");
        InputAction move = player.AddAction("Move", InputActionType.Value, expectedControlLayout: "Vector2");
        move.AddCompositeBinding("2DVector")   // default mode: Digital Normalized
            .With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
        move.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/upArrow").With("Down", "<Keyboard>/downArrow")
            .With("Left", "<Keyboard>/leftArrow").With("Right", "<Keyboard>/rightArrow");
        move.AddBinding("<Gamepad>/leftStick");
        player.AddAction("Look", InputActionType.Value, "<Pointer>/delta", expectedControlLayout: "Vector2");
        player.AddAction("LookStick", InputActionType.Value, "<Gamepad>/rightStick", expectedControlLayout: "Vector2");
        InputAction jump = player.AddAction("Jump", InputActionType.Button, "<Keyboard>/space");
        jump.AddBinding("<Gamepad>/buttonSouth");
        InputAction sprint = player.AddAction("Sprint", InputActionType.Button, "<Keyboard>/leftShift");
        sprint.AddBinding("<Gamepad>/leftStickPress");

        InputActionMap demo = asset.AddActionMap("Demo");
        demo.AddAction("ToggleScan", InputActionType.Button, "<Keyboard>/1");
        demo.AddAction("ToggleHeightTrap", InputActionType.Button, "<Keyboard>/2");
        demo.AddAction("ToggleChase", InputActionType.Button, "<Keyboard>/3");
        demo.AddAction("ToggleCursor", InputActionType.Button, "<Keyboard>/escape");
        demo.AddAction("ResetPlayer", InputActionType.Button, "<Keyboard>/r");

        // Keep an existing file so action ids stay stable between rebuilds.
        if (!File.Exists(InputPath)) File.WriteAllText(InputPath, asset.ToJson());
        Object.DestroyImmediate(asset);
        AssetDatabase.ImportAsset(InputPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
        var loaded = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputPath);
        if (loaded == null) throw new System.Exception("Could not load " + InputPath + " (run the builder again after the Input System package has imported).");
        return loaded;
    }

    // ------------------------------------------------------------------
    static Material Mat(string name, Color color)
    {
        var m = new Material(Shader.Find("Standard")) { color = color, name = name };
        m.SetFloat("_Glossiness", 0.25f);
        return Save(m, $"Assets/Materials/{name}.mat");
    }

    static T Save<T>(T asset, string path) where T : Object
    {
        AssetDatabase.DeleteAsset(path);
        AssetDatabase.CreateAsset(asset, path);
        return AssetDatabase.LoadAssetAtPath<T>(path);
    }

    static void Set(Object target, string field, Object value)
    {
        var so = new SerializedObject(target);
        SerializedProperty p = so.FindProperty(field);
        if (p == null) throw new System.Exception($"Field '{field}' not found on {target.GetType().Name}");
        p.objectReferenceValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static void SetLayer(GameObject go, int layer)
    {
        foreach (Transform t in go.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = layer;
    }
}
