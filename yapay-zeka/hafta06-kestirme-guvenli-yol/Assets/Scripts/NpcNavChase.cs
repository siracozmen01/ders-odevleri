// File: Assets/Scripts/NpcNavChase.cs  (week 4 script, extended in week 6 section 11.3)
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent), typeof(LineRenderer))]
public class NpcNavChase : MonoBehaviour
{
    public Transform target;              // the character to chase
    public float repathInterval = 0.25f;  // how often (s) the destination is refreshed

    NavMeshAgent agent;
    LineRenderer line;
    float timer;
    NavMeshPathStatus lastStatus = NavMeshPathStatus.PathComplete;   // [NEW]

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        line = GetComponent<LineRenderer>();
        // The agent is saved disabled and switched on here, after NavMeshSurface (execution order -102)
        // has registered the baked data; otherwise Unity logs "Failed to create agent" on the first frame.
        agent.enabled = true;
    }

    void Update()
    {
        // 1. The target moves: refresh the destination at a fixed interval
        timer -= Time.deltaTime;
        if (timer <= 0f && !agent.isOnOffMeshLink)   // [NEW] do not request a new path while jumping
        {
            agent.SetDestination(target.position);
            timer = repathInterval;
        }

        // 2. [NEW] Log the path status whenever it changes
        if (!agent.pathPending && agent.pathStatus != lastStatus)
        {
            lastStatus = agent.pathStatus;
            Debug.Log($"Yol durumu: {lastStatus}");
        }

        // 3. Show the path: the corner points found by the NavMesh (yellow line)
        Vector3[] corners = agent.path.corners;
        line.positionCount = corners.Length;
        for (int i = 0; i < corners.Length; i++)
            line.SetPosition(i, corners[i] + Vector3.up * 0.2f);
    }

    void OnDisable()
    {
        if (line != null) line.positionCount = 0;
    }
}
