using UnityEngine;

/// <summary>
/// Runtime picture of a guard's vision (Gizmos are not drawn in the Game view or in builds).
///
/// A fan of sample points is draped over the terrain inside the horizontal cone. For each point
/// the guard's own GorusKonisinde + GorusHattiAcik are asked whether a standing player
/// (centre 1 m above the ground) would be seen there. Visible ground is tinted with the guard's
/// colour (green / red), hidden ground (behind the hill or a tree) is drawn dark, so the
/// "shadows" of the obstacles are visible on the ground. A LineRenderer draws the cone outline.
/// </summary>
[RequireComponent(typeof(Gozcu))]
public class VisionConeView : MonoBehaviour
{
    [SerializeField] Material material;        // Sprites/Default based: vertex colours + alpha
    [SerializeField] int rays = 40;
    [SerializeField] int rings = 24;
    [SerializeField] float playerCentreHeight = 1f;
    [SerializeField] float groundOffset = 0.12f;
    [SerializeField] Color seeColor = new Color(1f, 0.15f, 0.1f, 0.42f);
    [SerializeField] Color calmColor = new Color(0.2f, 1f, 0.25f, 0.34f);
    [SerializeField] Color hiddenColor = new Color(0.05f, 0.05f, 0.1f, 0.30f);

    Gozcu gozcu;
    Terrain terrain;
    Mesh mesh;
    Transform meshTransform;
    LineRenderer outline;
    Vector3[] vertices;
    Color[] colors;
    Vector3[] outlinePoints;

    void Awake()
    {
        gozcu = GetComponent<Gozcu>();
        terrain = Terrain.activeTerrain;

        var go = new GameObject("VisionConeMesh");
        go.layer = 2;   // Ignore Raycast
        meshTransform = go.transform;   // left unparented at the origin: vertices are in world space
        go.AddComponent<MeshFilter>().sharedMesh = mesh = new Mesh { name = "VisionCone" };
        var mr = go.AddComponent<MeshRenderer>();
        mr.sharedMaterial = material;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = false;
        mesh.MarkDynamic();

        int vCount = (rays + 1) * (rings + 1);
        vertices = new Vector3[vCount];
        colors = new Color[vCount];
        var triangles = new int[rays * rings * 6];
        int t = 0;
        for (int i = 0; i < rays; i++)
            for (int j = 0; j < rings; j++)
            {
                int a = i * (rings + 1) + j, b = a + 1, c = a + rings + 1, d = c + 1;
                triangles[t++] = a; triangles[t++] = c; triangles[t++] = b;
                triangles[t++] = b; triangles[t++] = c; triangles[t++] = d;
            }
        mesh.vertices = vertices;
        mesh.colors = colors;
        mesh.triangles = triangles;

        outline = go.AddComponent<LineRenderer>();
        outline.sharedMaterial = material;
        outline.useWorldSpace = true;
        outline.widthMultiplier = 0.1f;
        outline.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        outlinePoints = new Vector3[2 * (rings + 1) + rays];
        outline.positionCount = outlinePoints.Length;
    }

    void OnDestroy()
    {
        if (meshTransform != null) Destroy(meshTransform.gameObject);
    }

    void LateUpdate()
    {
        Vector3 eye = gozcu.Goz;
        Vector3 forward = transform.forward; forward.y = 0f; forward.Normalize();
        float half = gozcu.GorusAcisi * 0.5f;
        Color stateColor = gozcu.IsSeeing ? seeColor : calmColor;

        for (int i = 0; i <= rays; i++)
        {
            float angle = -half + gozcu.GorusAcisi * i / rays;
            Vector3 dir = Quaternion.AngleAxis(angle, Vector3.up) * forward;
            for (int j = 0; j <= rings; j++)
            {
                float r = gozcu.Menzil * j / rings;
                Vector3 p = transform.position + dir * r;
                float ground = Ground(p);
                p.y = ground + groundOffset;
                Vector3 standing = new Vector3(p.x, ground + playerCentreHeight, p.z);

                bool inCone = j == 0 || gozcu.GorusKonisinde(eye, standing);
                bool seen = j == 0 || (inCone && gozcu.GorusHattiAcik(eye, standing));
                int k = i * (rings + 1) + j;
                vertices[k] = p;
                colors[k] = seen ? stateColor : (inCone ? hiddenColor : Color.clear);
            }
        }
        mesh.vertices = vertices;
        mesh.colors = colors;
        mesh.RecalculateBounds();

        // Outline: centre -> left edge -> arc -> right edge -> centre, slightly above the fill.
        int n = 0;
        Vector3 lift = Vector3.up * 0.05f;
        for (int j = 0; j <= rings; j++) outlinePoints[n++] = vertices[j] + lift;
        for (int i = 1; i <= rays; i++) outlinePoints[n++] = vertices[i * (rings + 1) + rings] + lift;
        for (int j = rings; j >= 0; j--) outlinePoints[n++] = vertices[rays * (rings + 1) + j] + lift;
        outline.SetPositions(outlinePoints);
        Color line = stateColor; line.a = 1f;
        outline.startColor = outline.endColor = line;
    }

    float Ground(Vector3 p)
    {
        if (terrain == null) return transform.position.y - 1f;
        return terrain.SampleHeight(p) + terrain.GetPosition().y;
    }
}
