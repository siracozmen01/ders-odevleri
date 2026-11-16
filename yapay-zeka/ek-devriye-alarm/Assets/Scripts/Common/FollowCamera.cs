using UnityEngine;

/// <summary>Simple smooth follow camera looking at the target from a fixed offset.</summary>
public class FollowCamera : MonoBehaviour
{
    public Transform target;
    public Vector3 offset = new Vector3(0f, 18f, -14f);
    public float smooth = 6f;

    void LateUpdate()
    {
        if (target == null) return;
        Vector3 wanted = target.position + offset;
        transform.position = Vector3.Lerp(transform.position, wanted, smooth * Time.deltaTime);
        transform.LookAt(target.position + Vector3.up);
    }
}
