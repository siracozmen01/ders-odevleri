// File: Assets/Scripts/JumpLinkTraverser.cs  (week 6 section 11.5)
using UnityEngine;
using UnityEngine.AI;

// When the NPC reaches a link, it jumps to the other end along a parabola
[RequireComponent(typeof(NavMeshAgent))]
public class JumpLinkTraverser : MonoBehaviour
{
    public float arcHeight = 1.5f;   // h: apex height (m), wall height 1 m + 0.5 m
    public float duration = 0.6f;    // jump duration (s)

    NavMeshAgent agent;
    bool jumping;
    float t;
    Vector3 a, b;

    public bool IsJumping => jumping;   // read by the report harness

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.autoTraverseOffMeshLink = false;   // we traverse the link ourselves
    }

    void Update()
    {
        // 1. We are at the start of the link: prepare the jump
        if (agent.isOnOffMeshLink && !jumping)
        {
            OffMeshLinkData data = agent.currentOffMeshLinkData;
            a = transform.position;
            b = data.endPos + Vector3.up * agent.baseOffset;   // sphere centre is 0.5 m above the ground
            t = 0f;
            jumping = true;
        }
        if (!jumping) return;

        // 2. Parabola: straight line + 4h*t(1-t) upwards
        t += Time.deltaTime / duration;
        float k = Mathf.Clamp01(t);
        transform.position = Vector3.Lerp(a, b, k) + Vector3.up * (4f * arcHeight * k * (1f - k));

        // 3. Landed: tell the agent that the link is finished
        if (t >= 1f)
        {
            agent.CompleteOffMeshLink();
            jumping = false;
        }
    }
}
