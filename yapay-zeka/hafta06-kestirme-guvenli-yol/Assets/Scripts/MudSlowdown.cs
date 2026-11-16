// File: Assets/Scripts/MudSlowdown.cs  (homework bonus)
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

// Area cost only steers the path finder; it does not change speed.
// This script really slows the NPC down in mud: every frame it samples the area under the agent
// (NavMesh.SamplePosition + PathDebugDraw.AreaIndex) and halves agent.speed while it is in Camur.
// The Y key switches the slowdown on/off.
[RequireComponent(typeof(NavMeshAgent))]
public class MudSlowdown : MonoBehaviour
{
    public string mudAreaName = "Camur";
    [Range(0.1f, 1f)] public float speedFactor = 0.5f;   // 0.5 = half speed in mud
    public bool slowdownOn = true;

    NavMeshAgent agent;
    InputAction toggle;
    int mudArea;
    bool slowed;
    float normalSpeed;

    public bool InMud { get; private set; }

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        toggle = InputSystem.actions.FindAction("ToggleSlowdown");
        mudArea = NavMesh.GetAreaFromName(mudAreaName);
    }

    void Update()
    {
        if (toggle != null && toggle.WasPressedThisFrame()) slowdownOn = !slowdownOn;

        // 1. Which area is under the agent? (the agent's transform is baseOffset above the mesh)
        InMud = false;
        if (!agent.isOnOffMeshLink)
        {
            Vector3 feet = transform.position - Vector3.up * agent.baseOffset;
            if (NavMesh.SamplePosition(feet, out NavMeshHit hit, 0.5f, NavMesh.AllAreas))
                InMud = PathDebugDraw.AreaIndex(hit.mask) == mudArea;
        }

        // 2. Entering mud: remember the speed and halve it. Leaving: restore it.
        bool wantSlow = slowdownOn && InMud;
        if (wantSlow && !slowed)
        {
            normalSpeed = agent.speed;
            agent.speed = normalSpeed * speedFactor;
            slowed = true;
        }
        else if (!wantSlow && slowed)
        {
            agent.speed = normalSpeed;
            slowed = false;
        }
    }

    void OnDisable()
    {
        if (slowed && agent != null) agent.speed = normalSpeed;
        slowed = false;
    }

    void OnGUI()
    {
        GUI.Label(new Rect(10, 100, 900, 25),
            $"[Y] Çamurda yavaşlama: {(slowdownOn ? "açık" : "kapalı")}   Hız: {agent.speed:0.##} m/s{(InMud ? "   (çamurda)" : "")}");
    }
}
