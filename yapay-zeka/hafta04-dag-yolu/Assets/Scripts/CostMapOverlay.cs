using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Runtime version of the TODO 2 cost-map gizmo: a transparent mesh that hugs the terrain, textured with
/// one pixel per grid cell in the same colours as GridMap.CostColor (green -> red, black = impassable).
/// Gizmos are not drawn in the Game view or in builds/screenshots; this overlay is.
/// </summary>
[ExecuteAlways]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class CostMapOverlay : MonoBehaviour
{
    public GridMap grid;
    public float lift = 0.25f;
    [Range(0f, 1f)] public float alpha = 0.55f;
    [Tooltip("Mesh vertices per cell edge; 2 matches a 0.5 m heightmap on 1 m cells")]
    [Range(1, 4)] public int subdivisions = 2;

    Texture2D texture;
    Mesh mesh;
    MaterialPropertyBlock block;

    void OnEnable()
    {
        if (grid == null) return;
        grid.Changed += Rebuild;
        Rebuild();
    }

    void OnDisable()
    {
        if (grid != null) grid.Changed -= Rebuild;
    }

    void OnValidate()
    {
        if (isActiveAndEnabled && grid != null && texture != null) Paint();
    }

    public void Rebuild()
    {
        if (grid == null) return;
        BuildMesh();
        Paint();
    }

    void BuildMesh()
    {
        int vx = grid.width * subdivisions + 1, vz = grid.depth * subdivisions + 1;
        var vertices = new Vector3[vx * vz];
        var uv = new Vector2[vx * vz];
        float step = grid.cellSize / subdivisions;
        Vector3 origin = grid.transform.position;
        for (int z = 0; z < vz; z++)
            for (int x = 0; x < vx; x++)
            {
                Vector3 w = origin + new Vector3(x * step, 0f, z * step);
                w.y = grid.GroundHeight(w) + lift;
                vertices[z * vx + x] = transform.InverseTransformPoint(w);
                uv[z * vx + x] = new Vector2(x / (float)(vx - 1), z / (float)(vz - 1));
            }
        var triangles = new int[(vx - 1) * (vz - 1) * 6];
        int t = 0;
        for (int z = 0; z < vz - 1; z++)
            for (int x = 0; x < vx - 1; x++)
            {
                int i = z * vx + x;
                triangles[t++] = i; triangles[t++] = i + vx; triangles[t++] = i + 1;
                triangles[t++] = i + 1; triangles[t++] = i + vx; triangles[t++] = i + vx + 1;
            }
        if (mesh == null) mesh = new Mesh { name = "CostMapOverlay", hideFlags = HideFlags.DontSave };
        mesh.Clear();
        mesh.indexFormat = vertices.Length > 65000 ? IndexFormat.UInt32 : IndexFormat.UInt16;
        mesh.vertices = vertices;
        mesh.uv = uv;
        mesh.triangles = triangles;
        mesh.RecalculateBounds();
        GetComponent<MeshFilter>().sharedMesh = mesh;
    }

    void Paint()
    {
        if (texture == null || texture.width != grid.width || texture.height != grid.depth)
        {
            texture = new Texture2D(grid.width, grid.depth, TextureFormat.RGBA32, false)
            {
                name = "CostMap", filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.DontSave
            };
        }
        var pixels = new Color32[grid.width * grid.depth];
        for (int z = 0; z < grid.depth; z++)
            for (int x = 0; x < grid.width; x++)
            {
                Color c = grid.CostColor(new Vector2Int(x, z));
                c.a = float.IsInfinity(grid.Cost(new Vector2Int(x, z))) ? Mathf.Min(1f, alpha + 0.25f) : alpha;
                pixels[z * grid.width + x] = c;
            }
        texture.SetPixels32(pixels);
        texture.Apply();
        block ??= new MaterialPropertyBlock();
        var r = GetComponent<MeshRenderer>();
        r.GetPropertyBlock(block);
        block.SetTexture("_MainTex", texture);
        r.SetPropertyBlock(block);
    }
}
