using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

/// <summary>
/// NavMesh vs A* demo.
///  Left click  : set a target -> the blue agent walks there with our own A*,
///                the green agent walks there with Unity's NavMesh (NavMeshAgent).
///  Right click : add / remove a wall (A* grid is rebuilt, NavMesh is carved by NavMeshObstacle).
///  G           : show / hide the A* grid overlay (open = green, closed = red, path = blue).
///  R           : restart.
/// </summary>
public class AStarDemo : MonoBehaviour
{
    public AStarGrid grid;
    public Transform aStarAgent;
    public NavMeshAgent navMeshAgent;
    public float aStarSpeed = 5f;
    public LayerMask groundMask;
    public LayerMask obstacleMask;
    public Material wallMaterial;
    public Transform targetMarker;

    List<Vector3> path;
    int pathIndex;
    LineRenderer aStarLine, navLine;
    Texture2D overlayTex;
    GameObject overlay;
    string status = "Bir hedef seçmek için zemine sol tıkla.";
    int lastPathLength;
    float lastPathDistance, lastNavDistance;

    void Start()
    {
        aStarLine = CreateLine("A* Path", new Color(0.2f, 0.5f, 1f));
        navLine = CreateLine("NavMesh Path", new Color(0.2f, 0.9f, 0.3f));
        CreateOverlay();
        RefreshOverlay();
    }

    LineRenderer CreateLine(string n, Color c)
    {
        var go = new GameObject(n);
        var lr = go.AddComponent<LineRenderer>();
        lr.widthMultiplier = 0.15f;
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = lr.endColor = c;
        lr.positionCount = 0;
        return lr;
    }

    void CreateOverlay()
    {
        overlayTex = new Texture2D(grid.SizeX, grid.SizeY, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
        overlay = GameObject.CreatePrimitive(PrimitiveType.Quad);
        overlay.name = "A* Grid Overlay";
        Destroy(overlay.GetComponent<Collider>());
        overlay.transform.position = grid.transform.position + Vector3.up * 0.03f;
        overlay.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        overlay.transform.localScale = new Vector3(grid.gridWorldSize.x, grid.gridWorldSize.y, 1f);
        var mat = new Material(Shader.Find("Sprites/Default")) { mainTexture = overlayTex };
        overlay.GetComponent<MeshRenderer>().material = mat;
    }

    void RefreshOverlay()
    {
        var pathSet = new HashSet<AStarGrid.Node>(grid.LastPath);
        for (int x = 0; x < grid.SizeX; x++)
            for (int y = 0; y < grid.SizeY; y++)
            {
                var n = grid.Grid[x, y];
                // checkerboard so the cells are visible
                Color c = ((x + y) % 2 == 0) ? new Color(1, 1, 1, 0.06f) : new Color(1, 1, 1, 0.12f);
                if (!n.walkable) c = new Color(0, 0, 0, 0.35f);
                if (grid.LastClosed.Contains(n)) c = new Color(1f, 0.3f, 0.3f, 0.45f);
                if (grid.LastOpen.Contains(n)) c = new Color(0.3f, 1f, 0.3f, 0.55f);
                if (pathSet.Contains(n)) c = new Color(0.2f, 0.5f, 1f, 0.85f);
                overlayTex.SetPixel(x, y, c);
            }
        overlayTex.Apply();
    }

    void Update()
    {
        if (InputHelper.RestartPressed()) { SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex); return; }
        if (InputHelper.KeyDown('g')) overlay.SetActive(!overlay.activeSelf);

        Ray ray = Camera.main.ScreenPointToRay(InputHelper.MousePosition());

        if (InputHelper.LeftClick() && Physics.Raycast(ray, out RaycastHit hit, 500f, groundMask | obstacleMask))
        {
            if (((1 << hit.collider.gameObject.layer) & obstacleMask) != 0) status = "Duvarın üstüne hedef konamaz.";
            else SetTarget(hit.point);
        }

        if (InputHelper.RightClick() && Physics.Raycast(ray, out RaycastHit hit2, 500f, groundMask | obstacleMask))
            ToggleWall(hit2);

        FollowPath();
        DrawNavMeshPath();
    }

    void SetTarget(Vector3 point)
    {
        if (targetMarker != null) targetMarker.position = point;

        path = grid.FindPath(aStarAgent.position, point);
        pathIndex = 0;
        if (path == null)
        {
            status = "A*: Hedefe yol bulunamadı!";
            aStarLine.positionCount = 0;
        }
        else
        {
            lastPathLength = path.Count;
            lastPathDistance = 0f;
            var pts = new List<Vector3> { aStarAgent.position + Vector3.up * 0.1f };
            foreach (var p in path) pts.Add(p + Vector3.up * 0.1f);
            for (int i = 1; i < pts.Count; i++) lastPathDistance += Vector3.Distance(pts[i - 1], pts[i]);
            aStarLine.positionCount = pts.Count;
            aStarLine.SetPositions(pts.ToArray());
            status = $"A*: {lastPathLength} düğüm, {lastPathDistance:F1} m, {grid.LastClosed.Count} düğüm incelendi, {grid.LastSearchMs:F2} ms";
        }

        if (navMeshAgent != null) navMeshAgent.SetDestination(point);
        RefreshOverlay();
    }

    void ToggleWall(RaycastHit hit)
    {
        if (((1 << hit.collider.gameObject.layer) & obstacleMask) != 0)
        {
            if (hit.collider.CompareTag("UserWall")) Destroy(hit.collider.gameObject);
            else { status = "Sadece sağ tıkla eklediğin duvarlar silinebilir."; return; }
        }
        else
        {
            var node = grid.NodeFromWorld(hit.point);
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = "UserWall";
            wall.tag = "UserWall";
            wall.layer = LayerMaskToLayer(obstacleMask);
            float size = grid.nodeRadius * 2f * 2f;
            wall.transform.position = node.worldPos + Vector3.up * 1f;
            wall.transform.localScale = new Vector3(size, 2f, size);
            if (wallMaterial != null) wall.GetComponent<MeshRenderer>().sharedMaterial = wallMaterial;
            var obs = wall.AddComponent<NavMeshObstacle>();
            obs.carving = true;
        }
        Invoke(nameof(RebuildGrid), 0.05f); // wait a frame so Destroy has happened
    }

    void RebuildGrid()
    {
        grid.CreateGrid();
        grid.LastOpen.Clear(); grid.LastClosed.Clear(); grid.LastPath.Clear();
        RefreshOverlay();
        if (targetMarker != null && path != null) SetTarget(targetMarker.position);
        else status = "Duvarlar güncellendi.";
    }

    static int LayerMaskToLayer(LayerMask m)
    {
        int v = m.value;
        for (int i = 0; i < 32; i++) if ((v & (1 << i)) != 0) return i;
        return 0;
    }

    void FollowPath()
    {
        if (path == null || pathIndex >= path.Count) return;
        Vector3 target = path[pathIndex];
        target.y = aStarAgent.position.y;
        Vector3 dir = target - aStarAgent.position;
        if (dir.sqrMagnitude > 0.001f)
            aStarAgent.rotation = Quaternion.Slerp(aStarAgent.rotation, Quaternion.LookRotation(dir), 10f * Time.deltaTime);
        aStarAgent.position = Vector3.MoveTowards(aStarAgent.position, target, aStarSpeed * Time.deltaTime);
        if (dir.magnitude < 0.05f) pathIndex++;
    }

    void DrawNavMeshPath()
    {
        if (navMeshAgent == null || !navMeshAgent.hasPath) return;
        var corners = navMeshAgent.path.corners;
        navLine.positionCount = corners.Length;
        lastNavDistance = 0f;
        for (int i = 0; i < corners.Length; i++)
        {
            navLine.SetPosition(i, corners[i] + Vector3.up * 0.15f);
            if (i > 0) lastNavDistance += Vector3.Distance(corners[i - 1], corners[i]);
        }
    }

    void OnGUI()
    {
        var style = new GUIStyle(GUI.skin.label) { fontSize = 16, richText = true };
        style.normal.textColor = Color.white;
        GUI.Box(new Rect(10, 10, 470, 190), GUIContent.none);
        GUI.Label(new Rect(20, 15, 460, 185),
            "<b>NavMesh ve A*</b>\n" +
            "Sol tık: hedef seç   Sağ tık: duvar ekle/sil\n" +
            "G: A* ızgarasını aç/kapat   R: yeniden başla\n" +
            "<color=#4d88ff>Mavi</color> = bizim A*   <color=#4dff66>Yeşil</color> = Unity NavMesh\n" +
            "Izgara: <color=#66ff66>yeşil</color> açık liste, <color=#ff6666>kırmızı</color> kapalı liste\n\n" +
            status + (navMeshAgent != null && navMeshAgent.hasPath ? $"\nNavMesh yolu: {lastNavDistance:F1} m ({navMeshAgent.path.corners.Length} köşe)" : ""),
            style);
    }
}
