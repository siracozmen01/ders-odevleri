using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Builds the "Dag Yolu" scene reproducibly: hilly terrain with a mountain ridge, two passes and rocky knolls,
/// the 128 x 128 cost grid, the red NPC, the flag, the runtime cost overlay, camera and HUD.
/// Menu: Dag Yolu > Build Scene   |   batch: -executeMethod DagYoluBuilder.Build
/// </summary>
public static class DagYoluBuilder
{
    public const string ScenePath = "Assets/Scenes/DagYolu.unity";
    const string MatDir = "Assets/Materials";
    const string TerrainDir = "Assets/Terrain";
    const int GroundLayer = 6;

    public const float TerrainSize = 128f;
    public const float TerrainHeight = 40f;
    const int HeightRes = 257;              // 0.5 m between height samples

    public static readonly Vector2 NpcStart = new Vector2(12.5f, 60.5f);
    public static readonly Vector2 FlagPos = new Vector2(115.5f, 66.5f);

    [MenuItem("Dag Yolu/Build Scene")]
    public static void Build()
    {
        Directory.CreateDirectory(MatDir);
        Directory.CreateDirectory(TerrainDir);
        Directory.CreateDirectory("Assets/Scenes");
        SetupLayers();

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // ---- light
        var lightGo = new GameObject("Directional Light");
        var light = lightGo.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.1f;
        light.shadows = LightShadows.Soft;
        lightGo.transform.rotation = Quaternion.Euler(50f, -35f, 0f);
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.62f, 0.68f, 0.78f);
        RenderSettings.ambientEquatorColor = new Color(0.45f, 0.47f, 0.45f);
        RenderSettings.ambientGroundColor = new Color(0.25f, 0.24f, 0.2f);

        // ---- terrain
        Terrain terrain = BuildTerrain();

        // ---- grid
        var gridGo = new GameObject("Grid");
        gridGo.transform.position = terrain.transform.position;
        var grid = gridGo.AddComponent<GridMap>();
        grid.terrain = terrain;
        grid.width = 128;
        grid.depth = 128;
        grid.cellSize = 1f;
        grid.k = 4f;
        grid.sMax = 35f;
        grid.drawCostMap = true;       // TODO 2 gizmos (16k cubes: untick if the Scene view gets slow)
        grid.Scan();

        // ---- runtime cost overlay (TODO 2 for the Game view)
        var overlayGo = new GameObject("CostMapOverlay");
        overlayGo.transform.SetParent(gridGo.transform, false);
        overlayGo.transform.position = Vector3.zero;
        overlayGo.AddComponent<MeshFilter>();
        var overlayRenderer = overlayGo.AddComponent<MeshRenderer>();
        var overlayMat = Mat("CostOverlay", Color.white, "Unlit/Transparent");
        overlayMat.renderQueue = 2900;   // before the path lines (3000), so lines are always drawn on top
        overlayRenderer.sharedMaterial = overlayMat;
        overlayRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        overlayRenderer.receiveShadows = false;
        var overlay = overlayGo.AddComponent<CostMapOverlay>();
        overlay.grid = grid;
        overlay.Rebuild();

        // ---- flag (target)
        var flag = new GameObject("Flag");
        flag.transform.position = OnGround(grid, FlagPos);
        var pole = Prim(PrimitiveType.Cylinder, "Pole", Mat("FlagPole", new Color(0.92f, 0.92f, 0.9f)));
        pole.transform.SetParent(flag.transform, false);
        pole.transform.localPosition = new Vector3(0f, 2f, 0f);
        pole.transform.localScale = new Vector3(0.25f, 2f, 0.25f);
        var cloth = Prim(PrimitiveType.Cube, "Cloth", Mat("FlagCloth", new Color(0.15f, 0.35f, 1f)));
        cloth.transform.SetParent(flag.transform, false);
        cloth.transform.localPosition = new Vector3(1.1f, 3.3f, 0f);
        cloth.transform.localScale = new Vector3(2f, 1.3f, 0.08f);

        // ---- path line
        var lineGo = new GameObject("PathLine");
        var lr = lineGo.AddComponent<LineRenderer>();
        lr.sharedMaterial = Mat("PathLine", Color.white, "Sprites/Default");
        lr.widthMultiplier = 0.45f;
        lr.startColor = lr.endColor = new Color(0.15f, 0.4f, 1f);
        lr.numCornerVertices = 2;
        lr.numCapVertices = 2;
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lr.positionCount = 0;
        var pathLine = lineGo.AddComponent<PathLine>();

        // ---- NPC
        var npcGo = Prim(PrimitiveType.Sphere, "NPC", Mat("NpcRed", new Color(0.9f, 0.1f, 0.08f)));
        npcGo.transform.position = OnGround(grid, NpcStart) + Vector3.up * 0.5f;
        var npc = npcGo.AddComponent<NpcGridChase>();
        npc.grid = grid;
        npc.target = flag.transform;
        npc.pathLine = pathLine;
        npc.heuristic = Heuristic.Octile;
        npc.useBinaryHeap = true;
        npc.speed = 4f;

        // ---- camera
        var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
        var cam = camGo.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.55f, 0.7f, 0.85f);
        cam.farClipPlane = 600f;
        camGo.AddComponent<AudioListener>();
        var modes = camGo.AddComponent<CameraModes>();
        modes.target = npcGo.transform;
        modes.grid = grid;
        modes.SnapToWanted();

        // ---- controls + HUD
        var controls = new GameObject("DemoControls").AddComponent<DemoControls>();
        controls.grid = grid;
        controls.npc = npc;
        controls.flag = flag.transform;
        controls.overlay = overlay;
        controls.cam = cam;
        controls.groundMask = 1 << GroundLayer;

        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        AssetDatabase.SaveAssets();
        Debug.Log("[DagYoluBuilder] Scene built: " + ScenePath);
        LogSlopeSummary(grid);
    }

    // ------------------------------------------------------------------ terrain

    /// <summary>Height in metres at world (x, z): rolling hills + a north-south ridge with two passes + rocky knolls.</summary>
    public static float HeightAt(float x, float z)
    {
        // rolling hills: slopes mostly 0-20 deg, so k matters everywhere
        float hills = Mathf.PerlinNoise(x * 0.028f + 11.3f, z * 0.028f + 4.7f) * 7f
                    + Mathf.PerlinNoise(x * 0.075f + 2.1f, z * 0.075f + 9.4f) * 2.2f;

        // the ridge between NPC and flag; its crest wiggles a little
        float xc = 64f + 5f * Mathf.Sin(z * 0.06f);
        float pass1 = Plateau(z, 62f, 5f, 8f);    // central saddle: short but steep (up to ~30 deg)
        float pass2 = Plateau(z, 106f, 6f, 10f);  // northern pass: long detour, gentle
        float H = 24f;
        float W = 10f;
        H = Mathf.Lerp(H, 7.5f, pass1);  W = Mathf.Lerp(W, 30f, pass1);
        H = Mathf.Lerp(H, 4.5f, pass2);  W = Mathf.Lerp(W, 34f, pass2);
        H *= Smooth01((z - 14f) / 14f);       // the ridge ends in the south: a flat way round
        float t = Mathf.Abs(x - xc) / W;
        float profile = t < 0.25f ? 1f : 1f - Smooth01((t - 0.25f) / 0.75f);
        float ridge = H * profile;

        // rocky knolls in the plains: steep sides (> 35 deg) = black cells
        float knolls = 7f * Gauss2(x, z, 34f, 40f, 3.2f) + 6f * Gauss2(x, z, 96f, 84f, 3f)
                     + 6.5f * Gauss2(x, z, 38f, 88f, 3f) + 7f * Gauss2(x, z, 92f, 40f, 3.4f)
                     + 5.5f * Gauss2(x, z, 26f, 66f, 2.6f);

        return hills + ridge + knolls;
    }

    static float Plateau(float v, float mu, float half, float fall) => 1f - Smooth01((Mathf.Abs(v - mu) - half) / fall);
    static float Gauss(float v, float mu, float sigma) => Mathf.Exp(-((v - mu) * (v - mu)) / (2f * sigma * sigma));
    static float Gauss2(float x, float z, float cx, float cz, float s) => Gauss(x, cx, s) * Gauss(z, cz, s);
    static float Smooth01(float t) { t = Mathf.Clamp01(t); return t * t * (3f - 2f * t); }

    static Terrain BuildTerrain()
    {
        var data = new TerrainData { heightmapResolution = HeightRes };
        data.size = new Vector3(TerrainSize, TerrainHeight, TerrainSize);
        int res = data.heightmapResolution;
        var h = new float[res, res];
        for (int zi = 0; zi < res; zi++)
            for (int xi = 0; xi < res; xi++)
            {
                float x = xi / (float)(res - 1) * TerrainSize, z = zi / (float)(res - 1) * TerrainSize;
                h[zi, xi] = Mathf.Clamp01(HeightAt(x, z) / TerrainHeight);
            }
        data.SetHeights(0, 0, h);
        // save the TerrainData as an asset before assigning layers, so its alphamap textures are saved too
        AssetDatabase.DeleteAsset($"{TerrainDir}/DagYoluTerrain.asset");
        AssetDatabase.CreateAsset(data, $"{TerrainDir}/DagYoluTerrain.asset");

        // two layers: grass on gentle ground, rock on steep ground (visual only)
        var grass = new TerrainLayer { diffuseTexture = NoiseTexture($"{TerrainDir}/Grass.png", new Color(0.3f, 0.5f, 0.2f), new Color(0.42f, 0.58f, 0.26f), 3), tileSize = new Vector2(6, 6) };
        var rock = new TerrainLayer { diffuseTexture = NoiseTexture($"{TerrainDir}/Rock.png", new Color(0.42f, 0.4f, 0.37f), new Color(0.58f, 0.56f, 0.52f), 7), tileSize = new Vector2(5, 5) };
        AssetDatabase.DeleteAsset($"{TerrainDir}/Grass.terrainlayer");
        AssetDatabase.DeleteAsset($"{TerrainDir}/Rock.terrainlayer");
        AssetDatabase.CreateAsset(grass, $"{TerrainDir}/Grass.terrainlayer");
        AssetDatabase.CreateAsset(rock, $"{TerrainDir}/Rock.terrainlayer");
        data.terrainLayers = new[] { grass, rock };
        data.alphamapResolution = 256;
        var alpha = new float[256, 256, 2];
        for (int zi = 0; zi < 256; zi++)
            for (int xi = 0; xi < 256; xi++)
            {
                float s = data.GetSteepness((xi + 0.5f) / 256f, (zi + 0.5f) / 256f);
                float r = Mathf.Clamp01((s - 22f) / 14f);
                alpha[zi, xi, 0] = 1f - r;
                alpha[zi, xi, 1] = r;
            }
        data.SetAlphamaps(0, 0, alpha);
        EditorUtility.SetDirty(data);
        AssetDatabase.SaveAssets();

        var go = Terrain.CreateTerrainGameObject(data);
        go.name = "Terrain";
        go.layer = GroundLayer;
        go.transform.position = Vector3.zero;
        var terrain = go.GetComponent<Terrain>();
        terrain.heightmapPixelError = 1f;
        terrain.basemapDistance = 1000f;
        return terrain;
    }

    static Texture2D NoiseTexture(string path, Color a, Color b, int seed)
    {
        const int size = 128;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var rnd = new System.Random(seed);
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float n = Mathf.PerlinNoise(x * 0.08f + seed, y * 0.08f) * 0.7f + (float)rnd.NextDouble() * 0.3f;
                tex.SetPixel(x, y, Color.Lerp(a, b, n));
            }
        tex.Apply();
        File.WriteAllBytes(path, tex.EncodeToPNG());
        AssetDatabase.ImportAsset(path);
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    // ------------------------------------------------------------------ helpers

    static Vector3 OnGround(GridMap grid, Vector2 xz) => grid.CellToWorld(grid.WorldToCell(new Vector3(xz.x, 0f, xz.y)));

    static void SetupLayers()
    {
        var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        var layers = tagManager.FindProperty("layers");
        layers.GetArrayElementAtIndex(GroundLayer).stringValue = "Ground";
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
        m.shader = Shader.Find(shader);
        m.color = color;
        EditorUtility.SetDirty(m);
        return m;
    }

    static GameObject Prim(PrimitiveType type, string name, Material mat)
    {
        var go = GameObject.CreatePrimitive(type);
        go.name = name;
        Object.DestroyImmediate(go.GetComponent<Collider>());   // only the terrain is hit by rays
        go.GetComponent<Renderer>().sharedMaterial = mat;
        return go;
    }

    /// <summary>Coarse ASCII view of the cost map (k = 4) in the log, used while tuning the terrain.</summary>
    public static void LogSlopeSummary(GridMap grid)
    {
        int blocked = 0, steep = 0, mid = 0, flat = 0;
        float maxS = 0f;
        for (int x = 0; x < grid.width; x++)
            for (int z = 0; z < grid.depth; z++)
            {
                float s = grid.Slope(new Vector2Int(x, z));
                maxS = Mathf.Max(maxS, s);
                if (s > grid.sMax) blocked++; else if (s > 20f) steep++; else if (s > 10f) mid++; else flat++;
            }
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"[DagYoluBuilder] cells: {grid.width * grid.depth}, impassable (> {grid.sMax} deg): {blocked}, 20-35 deg: {steep}, 10-20 deg: {mid}, < 10 deg: {flat}, max slope {maxS:F1} deg");
        for (int z = grid.depth - 2; z >= 0; z -= 3)
        {
            for (int x = 0; x < grid.width; x += 2)
            {
                float s = grid.Slope(new Vector2Int(x, z));
                sb.Append(s > grid.sMax ? '#' : s > 25f ? '+' : s > 15f ? '=' : s > 7f ? '-' : '.');
            }
            sb.AppendLine();
        }
        Debug.Log(sb.ToString());
    }
}
