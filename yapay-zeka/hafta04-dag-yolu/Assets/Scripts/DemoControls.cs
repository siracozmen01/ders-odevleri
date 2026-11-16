using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Keyboard / mouse controls for recording the video, plus a small HUD with the live numbers.
/// Left click: move the flag.  1-4: k = 0 / 2 / 4 / 8.  H: heuristic.  U: uphill bonus.
/// P: path smoothing.  B: binary heap.  M: cost overlay.  R: NPC back to start.  C: camera.
/// </summary>
public class DemoControls : MonoBehaviour
{
    public GridMap grid;
    public NpcGridChase npc;
    public Transform flag;
    public CostMapOverlay overlay;
    public Camera cam;
    public LayerMask groundMask = ~0;
    public bool showHud = true;

    Vector2Int npcStart;
    GUIStyle style;

    void Start()
    {
        npcStart = grid.WorldToCell(npc.transform.position);
    }

    void Update()
    {
        var kb = Keyboard.current;
        if (kb != null)
        {
            if (kb.digit1Key.wasPressedThisFrame) SetK(0f);
            if (kb.digit2Key.wasPressedThisFrame) SetK(2f);
            if (kb.digit3Key.wasPressedThisFrame) SetK(4f);
            if (kb.digit4Key.wasPressedThisFrame) SetK(8f);
            if (kb.hKey.wasPressedThisFrame)
            {
                npc.heuristic = (Heuristic)(((int)npc.heuristic + 1) % 4);
                npc.Repath();
            }
            if (kb.uKey.wasPressedThisFrame) { grid.uphillExpensive = !grid.uphillExpensive; grid.NotifyChanged(); }
            if (kb.pKey.wasPressedThisFrame) { npc.smoothPath = !npc.smoothPath; npc.Repath(); }
            if (kb.bKey.wasPressedThisFrame) { npc.useBinaryHeap = !npc.useBinaryHeap; npc.Repath(); }
            if (kb.mKey.wasPressedThisFrame && overlay != null) overlay.gameObject.SetActive(!overlay.gameObject.activeSelf);
            if (kb.rKey.wasPressedThisFrame) npc.Teleport(npcStart);
            if (kb.f1Key.wasPressedThisFrame) showHud = !showHud;
        }

        var mouse = Mouse.current;
        if (mouse != null && mouse.leftButton.wasPressedThisFrame && cam != null)
        {
            Ray ray = cam.ScreenPointToRay(mouse.position.ReadValue());
            if (Physics.Raycast(ray, out RaycastHit hit, 1000f, groundMask))
            {
                Vector2Int cell = grid.WorldToCell(hit.point);
                if (grid.IsWalkable(cell))
                {
                    flag.position = grid.CellToWorld(cell);
                    npc.Repath();
                }
                else Debug.Log($"[DemoControls] {cell}: impassable (slope {grid.Slope(cell):F1} deg > s_max) or outside the grid");
            }
        }
    }

    void SetK(float k)
    {
        grid.k = k;
        grid.NotifyChanged();          // overlay repaints, NPC re-plans
    }

    void OnGUI()
    {
        if (!showHud) return;
        style ??= new GUIStyle(GUI.skin.box) { alignment = TextAnchor.UpperLeft, fontSize = 15, richText = true };
        PathStats s = npc.LastStats;
        Vector2Int cell = grid.WorldToCell(npc.transform.position);
        string text =
            $"<b>Dag Yolu</b>   k = {grid.k:0.#}   s_max = {grid.sMax:0.#} deg   heuristic = {npc.heuristic}\n" +
            $"uphill: {(grid.uphillExpensive ? "ON" : "off")}   smoothing: {(npc.smoothPath ? "ON" : "off")}   heap: {(npc.useBinaryHeap ? "ON" : "off")}\n" +
            (s.found
                ? $"path: cost {s.cost:F1}, {s.length3D:F1} m, {s.steps} steps, max slope {s.maxSlope:F1} deg, {s.expanded} nodes, {s.ms:F1} ms\n"
                : "path: none\n") +
            $"NPC: slope {grid.Slope(cell):F1} deg, cost x{npc.CurrentCostFactor:F2}, speed {npc.CurrentSpeed:F2} m/s\n" +
            "<size=12>click: move flag | 1-4: k=0/2/4/8 | H: heuristic | U: uphill | P: smoothing | B: heap | M: overlay | R: reset | C: camera | F1: HUD</size>";
        GUI.Box(new Rect(10, 10, 760, 112), text, style);
    }
}
