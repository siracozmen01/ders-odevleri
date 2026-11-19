// File: Assets/Scripts/NoiseRing.cs  (visual only)
// A ring on the floor at the noise position: an inner ring that grows out to the hearing distance and
// fades. Its radius is the guard's hearing distance, so a guard inside the ring heard the noise.
using UnityEngine;

public class NoiseRing : MonoBehaviour
{
    const int Segments = 72;
    public float duration = 1.6f;

    LineRenderer outer, inner;
    float radius, age;
    Color color = new Color(0.62f, 0.22f, 0.95f);

    public static void Show(Vector3 center, float radius, Material mat)
    {
        var go = new GameObject("SesHalkasi");
        go.transform.position = center + Vector3.up * 0.06f;
        var ring = go.AddComponent<NoiseRing>();
        ring.radius = radius;
        ring.outer = ring.MakeLine("Sinir", mat, 0.12f);
        ring.inner = ring.MakeLine("Dalga", mat, 0.08f);
        ring.Draw(ring.outer, radius);
        ring.Update();
    }

    LineRenderer MakeLine(string name, Material mat, float width)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform, false);
        var lr = go.AddComponent<LineRenderer>();
        lr.sharedMaterial = mat;
        lr.useWorldSpace = false;
        lr.loop = true;
        lr.positionCount = Segments;
        lr.widthMultiplier = width;
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lr.receiveShadows = false;
        return lr;
    }

    void Draw(LineRenderer lr, float r)
    {
        for (int i = 0; i < Segments; i++)
        {
            float a = i * Mathf.PI * 2f / Segments;
            lr.SetPosition(i, new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r));
        }
    }

    void Update()
    {
        age += Time.deltaTime;
        float t = Mathf.Clamp01(age / duration);
        Draw(inner, Mathf.Lerp(0.3f, radius, Mathf.Sqrt(t)));
        Color c = color;
        c.a = 1f - t * t;
        outer.startColor = outer.endColor = c;
        inner.startColor = inner.endColor = c;
        if (age >= duration) Destroy(gameObject);
    }
}
