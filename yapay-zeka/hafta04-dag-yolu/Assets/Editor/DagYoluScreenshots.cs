using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Renders the README / report screenshots from the saved scene into screenshots/*.png.
/// Gizmos cannot be captured in batch mode, so the runtime CostMapOverlay and PathLine (same colours) are used.
/// Menu: Dag Yolu > Capture Screenshots   |   batch: -executeMethod DagYoluScreenshots.CaptureAll
/// The scene is not saved afterwards; temporary lines and cameras are destroyed.
/// </summary>
public static class DagYoluScreenshots
{
    const string Dir = "screenshots";

    [MenuItem("Dag Yolu/Capture Screenshots")]
    public static void CaptureAll()
    {
        Directory.CreateDirectory(Dir);
        EditorSceneManager.OpenScene(DagYoluBuilder.ScenePath);
        var grid = Object.FindAnyObjectByType<GridMap>();
        var npc = Object.FindAnyObjectByType<NpcGridChase>();
        var overlay = Object.FindAnyObjectByType<CostMapOverlay>();
        var sceneCam = Camera.main;
        sceneCam.gameObject.SetActive(false);
        grid.Scan();
        npc.logSearches = false;
        Vector2Int start = grid.WorldToCell(npc.transform.position);
        Vector2Int goal = grid.WorldToCell(npc.target.position);
        var temp = new List<GameObject>();

        // 0) scene overview in perspective, no overlay
        SetK(grid, overlay, 4f);
        overlay.gameObject.SetActive(false);
        npc.Repath();
        Render(Perspective(new Vector3(64f, 70f, -38f), new Vector3(64f, 4f, 66f), 50f), "00-sahne.png", 1600, 900);

        // top-down shots: enlarge NPC and flag so they are visible at 1 px = 0.1 m
        npc.transform.localScale = Vector3.one * 3.5f;
        npc.target.localScale = Vector3.one * 4f;

        // 1) cost map overview (k = 4), with the k = 4 path
        overlay.gameObject.SetActive(true);
        overlay.alpha = 0.6f;
        SetK(grid, overlay, 4f);
        npc.Repath();
        Render(TopDown(grid), "01-maliyet-haritasi-k4.png", 1200, 1200);

        // 1b) the same map for k = 8 (redder: the same slope costs more)
        SetK(grid, overlay, 8f);
        npc.Repath();
        Render(TopDown(grid), "01b-maliyet-haritasi-k8.png", 1200, 1200);

        // 2) k = 0 vs k = 8, same start and goal (map coloured for k = 8)
        npc.pathLine.Hide();
        SetK(grid, overlay, 0f);
        var p0 = new AStar { heuristic = Heuristic.Octile, useBinaryHeap = true }.FindPath(grid, start, goal);
        SetK(grid, overlay, 2f);
        var p2 = new AStar { heuristic = Heuristic.Octile, useBinaryHeap = true }.FindPath(grid, start, goal);
        SetK(grid, overlay, 8f);
        var p8 = new AStar { heuristic = Heuristic.Octile, useBinaryHeap = true }.FindPath(grid, start, goal);
        temp.Add(Line(grid, p0, new Color(1f, 1f, 1f), 0.9f, "k0"));
        temp.Add(Line(grid, p8, new Color(1f, 0.15f, 0.85f), 0.9f, "k8"));
        Render(TopDown(grid), "02-yollar-k0-k8.png", 1200, 1200);
        temp.Add(Line(grid, p2, new Color(0.15f, 0.4f, 1f), 0.6f, "k2"));
        Render(TopDown(grid), "02b-yollar-k0-k2-k8.png", 1200, 1200);
        overlay.gameObject.SetActive(false);
        Render(Perspective(new Vector3(64f, 70f, -38f), new Vector3(64f, 4f, 66f), 50f), "02c-yollar-perspektif.png", 1600, 900);
        foreach (var go in temp) Object.DestroyImmediate(go);
        temp.Clear();

        // 3) the NPC on its path (k = 4): simulate the walk until it is on the saddle
        npc.transform.localScale = Vector3.one;
        npc.target.localScale = Vector3.one;
        SetK(grid, overlay, 4f);
        npc.Repath();
        float t = 0f;
        while (!npc.Arrived && npc.transform.position.x < 52f && t < 200f) { npc.Tick(0.02f); t += 0.02f; }
        Vector3 at = npc.transform.position;
        Vector3 ahead = grid.CellToWorld(npc.Path[Mathf.Min(npc.Path.Count - 1, IndexNear(grid, npc.Path, at) + 12)]);
        Vector3 dir = new Vector3(ahead.x - at.x, 0f, ahead.z - at.z).normalized;
        Debug.Log($"[DagYoluScreenshots] NPC after {t:F1} s at {at}, speed {npc.CurrentSpeed:F2} m/s, cell slope {grid.Slope(grid.WorldToCell(at)):F1} deg");
        Render(Perspective(at - dir * 11f + Vector3.up * 7f, at + dir * 6f, 55f), "03-npc-yolda.png", 1600, 900);
        overlay.gameObject.SetActive(true);
        overlay.alpha = 0.45f;
        overlay.Rebuild();
        Render(Perspective(at - dir * 11f + Vector3.up * 7f, at + dir * 6f, 55f), "03b-npc-yolda-maliyet.png", 1600, 900);

        // 4) bonus: raw vs smoothed path (k = 8)
        npc.Teleport(start);
        npc.transform.localScale = Vector3.one * 3.5f;
        npc.target.localScale = Vector3.one * 4f;
        npc.pathLine.Hide();
        overlay.alpha = 0.6f;
        SetK(grid, overlay, 8f);
        var raw = new AStar { heuristic = Heuristic.Octile, useBinaryHeap = true }.FindPath(grid, start, goal);
        var smooth = PathSmoother.Smooth(grid, raw);
        temp.Add(Line(grid, raw, new Color(1f, 1f, 1f), 0.7f, "raw"));
        temp.Add(Line(grid, smooth, new Color(1f, 0.15f, 0.85f), 0.7f, "smooth"));
        Render(TopDown(grid), "04-yol-duzeltme-k8.png", 1200, 1200);
        foreach (var go in temp) Object.DestroyImmediate(go);

        // 5) bonus: uphill expensive, forward vs backward (k = 4)
        SetK(grid, overlay, 4f);
        grid.uphillExpensive = true;
        grid.NotifyChanged();
        var fw = new AStar { heuristic = Heuristic.Octile, useBinaryHeap = true }.FindPath(grid, start, goal);
        var bw = new AStar { heuristic = Heuristic.Octile, useBinaryHeap = true }.FindPath(grid, goal, start);
        grid.uphillExpensive = false;
        grid.NotifyChanged();
        var off = new AStar { heuristic = Heuristic.Octile, useBinaryHeap = true }.FindPath(grid, start, goal);
        temp.Clear();
        temp.Add(Line(grid, off, Color.white, 0.7f, "off"));
        temp.Add(Line(grid, fw, new Color(1f, 0.15f, 0.85f), 0.7f, "fw"));
        temp.Add(Line(grid, bw, new Color(0.15f, 0.4f, 1f), 0.5f, "bw"));
        Render(TopDown(grid), "05-yokus-yukari-k4.png", 1200, 1200);
        foreach (var go in temp) Object.DestroyImmediate(go);

        EditorSceneManager.OpenScene(DagYoluBuilder.ScenePath);   // discard changes
        Debug.Log("[DagYoluScreenshots] done: " + Path.GetFullPath(Dir));
    }

    static void SetK(GridMap grid, CostMapOverlay overlay, float k)
    {
        grid.k = k;
        grid.NotifyChanged();
        overlay.Rebuild();
    }

    static int IndexNear(GridMap grid, List<Vector2Int> path, Vector3 p)
    {
        int best = 0; float bd = float.MaxValue;
        for (int i = 0; i < path.Count; i++)
        {
            float d = (grid.CellToWorld(path[i]) - p).sqrMagnitude;
            if (d < bd) { bd = d; best = i; }
        }
        return best;
    }

    static GameObject Line(GridMap grid, List<Vector2Int> path, Color color, float width, string name)
    {
        var go = new GameObject("TempLine_" + name);
        var lr = go.AddComponent<LineRenderer>();
        lr.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/PathLine.mat");
        lr.widthMultiplier = width;
        lr.startColor = lr.endColor = color;
        lr.numCornerVertices = 2;
        var pl = go.AddComponent<PathLine>();
        pl.lift = 0.6f;
        pl.Draw(grid, path);
        return go;
    }

    static Camera TopDown(GridMap grid)
    {
        var cam = NewCam();
        cam.orthographic = true;
        cam.orthographicSize = grid.depth * grid.cellSize * 0.5f + 1f;
        cam.transform.position = grid.transform.position + new Vector3(grid.width, 0f, grid.depth) * (grid.cellSize * 0.5f) + Vector3.up * 150f;
        cam.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        return cam;
    }

    static Camera Perspective(Vector3 pos, Vector3 lookAt, float fov)
    {
        var cam = NewCam();
        cam.fieldOfView = fov;
        cam.transform.position = pos;
        cam.transform.rotation = Quaternion.LookRotation(lookAt - pos);
        return cam;
    }

    static Camera NewCam()
    {
        var cam = new GameObject("ShotCam").AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.55f, 0.7f, 0.85f);
        cam.farClipPlane = 1000f;
        return cam;
    }

    static void Render(Camera cam, string file, int w, int h)
    {
        var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
        cam.targetTexture = rt;
        cam.aspect = w / (float)h;
        cam.Render();
        RenderTexture.active = rt;
        var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
        tex.Apply();
        RenderTexture.active = null;
        cam.targetTexture = null;
        File.WriteAllBytes(Path.Combine(Dir, file), tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        Object.DestroyImmediate(rt);
        Object.DestroyImmediate(cam.gameObject);
        Debug.Log("[DagYoluScreenshots] " + file);
    }
}
