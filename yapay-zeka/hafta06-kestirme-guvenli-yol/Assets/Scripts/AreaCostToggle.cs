// File: Assets/Scripts/AreaCostToggle.cs  (week 6 section 11.6, extended for the homework)
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using Unity.AI.Navigation;

// Experiment panel.
//   M cycles the Camur cost, J toggles the jump link (as in class).
//   [NEW] N cycles the SigSu cost, H cycles the Jump cost, 1-4 apply the report's cost settings.
[RequireComponent(typeof(NavMeshAgent))]
public class AreaCostToggle : MonoBehaviour
{
    [System.Serializable]
    public struct CostPreset
    {
        public string name;
        public float mud, water, jump;
    }

    public NavMeshLink jumpLink;
    public SlidingDoor door;                       // [NEW] only for the on-screen status
    public string mudAreaName = "Camur";
    public string waterAreaName = "SigSu";         // [NEW]
    public float[] mudCosts = { 1f, 2f, 5f, 10f };
    public float[] waterCosts = { 1f, 2f, 3f, 5f, 10f };   // [NEW]
    public float[] jumpCosts = { 2f, 4f, 6f, 10f };        // [NEW]
    public CostPreset[] presets;                   // [NEW] the report's settings (keys 1-4)

    NavMeshAgent agent;
    InputAction cycleMud, cycleWater, cycleJump, toggleLink;
    readonly InputAction[] presetKeys = new InputAction[4];
    int mudArea, waterArea, jumpArea;
    float mudCost, waterCost, jumpCost;
    int presetIndex = -1;

    public int PresetIndex => presetIndex;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        cycleMud = InputSystem.actions.FindAction("CycleMudCost");
        cycleWater = InputSystem.actions.FindAction("CycleWaterCost");
        cycleJump = InputSystem.actions.FindAction("CycleJumpCost");
        toggleLink = InputSystem.actions.FindAction("ToggleLink");
        for (int i = 0; i < presetKeys.Length; i++)
            presetKeys[i] = InputSystem.actions.FindAction($"Preset{i + 1}");

        mudArea = AreaOrError(mudAreaName);        // area index (User 3 -> 3)
        waterArea = AreaOrError(waterAreaName);    // (User 4 -> 4)
        jumpArea = NavMesh.GetAreaFromName("Jump");
    }

    int AreaOrError(string areaName)
    {
        int a = NavMesh.GetAreaFromName(areaName);
        if (a < 0)
            Debug.LogError($"'{areaName}' adlı alan yok: Navigation > Areas sekmesini kontrol edin.");
        return a;
    }

    void Start()
    {
        if (presets != null && presets.Length > 0) ApplyPreset(0);
        else Apply(mudCosts[0], waterCosts[0], jumpCosts[0]);
    }

    void Update()
    {
        // 1. M / N / H: next Camur / SigSu / Jump cost
        if (cycleMud.WasPressedThisFrame()) { mudCost = Next(mudCosts, mudCost); presetIndex = -1; ApplyCosts(); }
        if (cycleWater.WasPressedThisFrame()) { waterCost = Next(waterCosts, waterCost); presetIndex = -1; ApplyCosts(); }
        if (cycleJump.WasPressedThisFrame()) { jumpCost = Next(jumpCosts, jumpCost); presetIndex = -1; ApplyCosts(); }

        // 2. J: link on/off
        if (toggleLink.WasPressedThisFrame() && jumpLink != null)
            jumpLink.activated = !jumpLink.activated;

        // 3. 1-4: the report's cost settings
        for (int i = 0; i < presetKeys.Length; i++)
            if (presetKeys[i] != null && presetKeys[i].WasPressedThisFrame() && presets != null && i < presets.Length)
                ApplyPreset(i);
    }

    // Next value in the list after the current one (wraps around)
    static float Next(float[] list, float current)
    {
        foreach (float v in list)
            if (v > current + 0.001f) return v;
        return list[0];
    }

    public void ApplyPreset(int i)
    {
        presetIndex = i;
        Apply(presets[i].mud, presets[i].water, presets[i].jump);
    }

    public void Apply(float mud, float water, float jump)
    {
        mudCost = mud;
        waterCost = water;
        jumpCost = jump;
        ApplyCosts();
    }

    void ApplyCosts()
    {
        // only for this agent (NavMesh.SetAreaCost would change every agent)
        if (mudArea >= 0) agent.SetAreaCost(mudArea, mudCost);
        if (waterArea >= 0) agent.SetAreaCost(waterArea, waterCost);
        if (jumpArea >= 0) agent.SetAreaCost(jumpArea, jumpCost);
    }

    void OnGUI()
    {
        string link = (jumpLink != null && jumpLink.activated) ? "açık" : "kapalı";
        string doorState = door == null ? "" : (door.IsOpen ? "açık" : "kapalı");
        GUI.Label(new Rect(10, 60, 900, 25),
            $"[M] Çamur: {mudCost:0.##}   [N] Sığ su: {waterCost:0.##}   [H] Atlama: {jumpCost:0.##}   [J] Bağlantı: {link}   [K] Kapı: {doorState}");
        string preset = presetIndex >= 0 ? $"Ayar {presetIndex + 1}" : "elle";
        string route = agent.hasPath ? RouteClassifier.Name(RouteClassifier.Letter(agent.path.corners)) : "-";
        GUI.Label(new Rect(10, 80, 900, 25), $"[1-4] {preset}   Rota: {route}");
    }
}
