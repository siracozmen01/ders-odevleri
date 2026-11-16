using UnityEngine;

/// <summary>
/// Draws the guard's vision cone on the ground at runtime (visible in the Game view).
/// Rays are cast across the cone so the cone is cut where hills/walls block the view,
/// and every vertex is placed on the terrain surface so the cone follows the hills.
/// Color: yellow = patrol, orange = suspicious, red = chase.
/// </summary>
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class VisionConeMesh : MonoBehaviour
{
    public GuardAI guard;
    public int rayCount = 40;
    public int rings = 8;
    public float heightOffset = 0.25f;

    Mesh mesh;
    Material material;
    Terrain terrain;
    Vector3[] verts;
    int[] tris;

    static readonly Color Patrol = new Color(1f, 0.9f, 0.2f, 0.28f);
    static readonly Color Suspicious = new Color(1f, 0.55f, 0f, 0.4f);
    static readonly Color Chase = new Color(1f, 0.1f, 0.1f, 0.45f);

    void Awake()
    {
        mesh = new Mesh { name = "VisionCone" };
        mesh.MarkDynamic();
        GetComponent<MeshFilter>().mesh = mesh;
        material = GetComponent<MeshRenderer>().material;
        terrain = Terrain.activeTerrain;

        int cols = rayCount + 1;
        verts = new Vector3[1 + cols * rings];
        tris = new int[rayCount * 3 + (rings - 1) * rayCount * 6];
        int t = 0;
        for (int i = 0; i < rayCount; i++)                    // fan around the guard
        {
            tris[t++] = 0; tris[t++] = 1 + i; tris[t++] = 1 + i + 1;
        }
        for (int r = 0; r < rings - 1; r++)                   // quads between rings
            for (int i = 0; i < rayCount; i++)
            {
                int a = 1 + r * cols + i, b = a + 1, c = a + cols, d = c + 1;
                tris[t++] = a; tris[t++] = c; tris[t++] = b;
                tris[t++] = b; tris[t++] = c; tris[t++] = d;
            }
    }

    float GroundY(Vector3 world)
    {
        if (terrain == null) return guard.transform.position.y;
        return terrain.SampleHeight(world) + terrain.transform.position.y;
    }

    void LateUpdate()
    {
        if (guard == null) return;

        transform.position = Vector3.zero;   // vertices are written in world space
        transform.rotation = Quaternion.identity;
        transform.localScale = Vector3.one;

        Vector3 origin = guard.transform.position;
        Vector3 eye = origin + Vector3.up * guard.eyeHeight;
        float half = guard.viewAngle * 0.5f;
        int cols = rayCount + 1;

        verts[0] = new Vector3(origin.x, GroundY(origin) + heightOffset, origin.z);

        for (int i = 0; i <= rayCount; i++)
        {
            float angle = -half + guard.viewAngle * i / rayCount;
            Vector3 dir = Quaternion.Euler(0f, guard.transform.eulerAngles.y + angle, 0f) * Vector3.forward;

            // cut the cone where the line of sight is blocked
            float dist = guard.viewDistance;
            if (Physics.Raycast(eye, dir, out RaycastHit hit, guard.viewDistance, guard.obstacleMask, QueryTriggerInteraction.Ignore)
                && !hit.collider.CompareTag("Player"))
                dist = hit.distance;

            for (int r = 0; r < rings; r++)
            {
                Vector3 p = origin + dir * (dist * (r + 1) / rings);
                p.y = GroundY(p) + heightOffset;
                verts[1 + r * cols + i] = p;
            }
        }

        mesh.vertices = verts;
        if (mesh.triangles.Length != tris.Length) mesh.triangles = tris;
        mesh.RecalculateBounds();

        Color c = guard.CurrentState == GuardAI.State.Chase ? Chase
                : guard.CurrentState == GuardAI.State.Suspicious ? Color.Lerp(Patrol, Suspicious, guard.Awareness)
                : Patrol;
        material.color = c;
    }
}
