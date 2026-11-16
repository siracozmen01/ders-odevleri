using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Minimal on-screen help: controls, the state of every guard, and demo keys that switch the
/// bonus behaviours on all guards at once (the same switches are in each guard's Inspector).
/// </summary>
public class HelpOverlay : MonoBehaviour
{
    [SerializeField] InputActionAsset actions;
    [SerializeField] PlayerMovement player;
    [SerializeField] FollowCamera followCamera;

    InputAction toggleScan, toggleHeight, toggleChase, toggleCursor, resetPlayer;
    Gozcu[] guards;
    GUIStyle box;

    void Awake()
    {
        if (actions == null) actions = InputSystem.actions;
        InputActionMap map = actions.FindActionMap("Demo", true);
        map.Enable();
        toggleScan = map.FindAction("ToggleScan", true);
        toggleHeight = map.FindAction("ToggleHeightTrap", true);
        toggleChase = map.FindAction("ToggleChase", true);
        toggleCursor = map.FindAction("ToggleCursor", true);
        resetPlayer = map.FindAction("ResetPlayer", true);
    }

    void Start()
    {
        guards = FindObjectsByType<Gozcu>();
        System.Array.Sort(guards, (x, y) => string.CompareOrdinal(x.name, y.name));
    }

    void Update()
    {
        if (toggleScan.WasPressedThisFrame()) Toggle(g => g.ScanEnabled = !g.ScanEnabled);
        if (toggleHeight.WasPressedThisFrame()) Toggle(g => g.HeightLimitEnabled = !g.HeightLimitEnabled);
        if (toggleChase.WasPressedThisFrame()) Toggle(g => g.ChaseEnabled = !g.ChaseEnabled);
        if (toggleCursor.WasPressedThisFrame())
            Cursor.lockState = Cursor.lockState == CursorLockMode.Locked ? CursorLockMode.None : CursorLockMode.Locked;
        if (resetPlayer.WasPressedThisFrame() && player != null)
        {
            player.ResetToSpawn();
            if (followCamera != null) followCamera.SetAngles(player.transform.eulerAngles.y, 20f, true);
        }
    }

    void Toggle(System.Action<Gozcu> change)
    {
        foreach (Gozcu g in guards) change(g);
    }

    void OnGUI()
    {
        if (box == null)
        {
            box = new GUIStyle(GUI.skin.box) { alignment = TextAnchor.UpperLeft, fontSize = 15, richText = true };
            box.padding = new RectOffset(10, 10, 8, 8);
            box.normal.background = MakeTexture(new Color(0f, 0f, 0f, 0.6f));
        }

        string text =
            "<b>Gözcü - week 3</b>\n" +
            "WASD / left stick: move (relative to camera)    Shift: sprint    Space: jump\n" +
            "Mouse / right stick: orbit camera    R: back to start    Esc: free / lock cursor\n" +
            "Bonus switches for all guards:  1 scan   2 height trap   3 chase";

        if (guards != null)
        {
            foreach (Gozcu g in guards)
            {
                string state = g.IsSeeing
                    ? $"<color=#ff5544><b>SEES THE PLAYER</b></color> ({(g.PlayerSide == Gozcu.Side.Right ? "right" : g.PlayerSide == Gozcu.Side.Left ? "left" : "ahead")})"
                    : "<color=#66ff66><b>does not see</b></color>" + (g.PlayerInCone ? " (in cone, line of sight blocked)" : "");
                text += $"\n<b>{g.name}</b>: {state}   mode: {g.CurrentMode}   " +
                        $"[scan {OnOff(g.ScanEnabled)}, height trap {OnOff(g.HeightLimitEnabled)}, chase {OnOff(g.ChaseEnabled)}]";
            }
        }

        Vector2 size = box.CalcSize(new GUIContent(text));
        GUI.Box(new Rect(10, 10, size.x + 10, size.y), text, box);
    }

    static string OnOff(bool on) => on ? "<color=#ffd34d>ON</color>" : "off";

    static Texture2D MakeTexture(Color c)
    {
        var tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, c);
        tex.Apply();
        return tex;
    }
}
