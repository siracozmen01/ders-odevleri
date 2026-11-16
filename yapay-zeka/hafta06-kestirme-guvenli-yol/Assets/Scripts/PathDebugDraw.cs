// File: Assets/Scripts/PathDebugDraw.cs  (week 6 section 11.7)
using UnityEngine;
using UnityEngine.AI;
using Unity.AI.Navigation;

// Measures the NPC's remaining path: corner count, length and area-weighted cost
[RequireComponent(typeof(NavMeshAgent))]
public class PathDebugDraw : MonoBehaviour
{
    public NavMeshLink jumpLink;      // used to recognise the link segment
    public float sampleStep = 0.25f;  // sample the cost every this many metres

    NavMeshAgent agent;
    float length, cost;
    int cornerCount;

    // Read-only values for the report harness
    public float Length => length;
    public float Cost => cost;
    public int CornerCount => cornerCount;

    void Awake() => agent = GetComponent<NavMeshAgent>();

    void Update()
    {
        if (agent.pathPending || !agent.hasPath) return;

        Vector3[] c = agent.path.corners;
        cornerCount = c.Length;
        length = 0f;
        cost = 0f;
        for (int i = 0; i < c.Length - 1; i++)
        {
            float segLen = Vector3.Distance(c[i], c[i + 1]);
            length += segLen;
            cost += SegmentCost(c[i], c[i + 1], segLen);
        }
    }

    float SegmentCost(Vector3 a, Vector3 b, float segLen)
    {
        // 1. Is this segment the jump link? Then length x the link's area cost
        if (IsLink(a, b))
            return segLen * agent.GetAreaCost(jumpLink.area);

        // 2. Otherwise split the segment into small steps and look at the area under each step's midpoint
        int steps = Mathf.Max(1, Mathf.CeilToInt(segLen / sampleStep));
        float stepLen = segLen / steps;
        float total = 0f;
        for (int s = 0; s < steps; s++)
        {
            Vector3 p = Vector3.Lerp(a, b, (s + 0.5f) / steps);
            int area = 0;   // treat as Walkable if nothing is found
            if (NavMesh.SamplePosition(p, out NavMeshHit hit, 0.5f, NavMesh.AllAreas))
                area = AreaIndex(hit.mask);
            total += stepLen * agent.GetAreaCost(area);
        }
        return total;
    }

    bool IsLink(Vector3 a, Vector3 b)
    {
        if (jumpLink == null || !jumpLink.activated) return false;
        Vector3 s = jumpLink.transform.TransformPoint(jumpLink.startPoint);
        Vector3 e = jumpLink.transform.TransformPoint(jumpLink.endPoint);
        return (Near(a, s) && Near(b, e)) || (Near(a, e) && Near(b, s));
    }

    static bool Near(Vector3 p, Vector3 q)
    {
        p.y = 0f;
        q.y = 0f;   // compare on the horizontal plane only
        return Vector3.Distance(p, q) < 0.3f;
    }

    public static int AreaIndex(int mask)
    {
        // hit.mask is a bit mask: area 3 sets only bit 3 (value 8)
        for (int i = 0; i < 32; i++)
            if ((mask & (1 << i)) != 0) return i;
        return 0;
    }

    void OnGUI()
    {
        GUI.Label(new Rect(10, 10, 500, 25), $"Köşe: {cornerCount}   Uzunluk: {length:F2} m   Maliyet: {cost:F2}");
        GUI.Label(new Rect(10, 30, 500, 25), $"Durum: {agent.pathStatus}");
    }

    void OnDrawGizmos()
    {
        if (agent == null || !agent.hasPath) return;
        Gizmos.color = Color.yellow;
        foreach (Vector3 p in agent.path.corners)
            Gizmos.DrawSphere(p, 0.2f);
    }
}
