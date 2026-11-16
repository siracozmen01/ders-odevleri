using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Top view of the whole map and a smooth chase view behind the NPC; C toggles (in-class CameraModes pattern).
/// The chase view follows the NPC's walking direction, so it stays behind it on a winding mountain path.
/// </summary>
public class CameraModes : MonoBehaviour
{
    public Transform target;                              // the NPC
    public GridMap grid;
    public float followDistance = 13f;
    public float followHeight = 9f;
    public float smoothing = 4f;                          // t = 1 - e^(-k dt)
    public float topHeight = 125f;
    public bool top = true;

    Vector3 heading = Vector3.right;
    Vector3 lastTargetPos;

    void Start()
    {
        if (target != null) lastTargetPos = target.position;
    }

    void LateUpdate()
    {
        if (Keyboard.current != null && Keyboard.current.cKey.wasPressedThisFrame) top = !top;

        if (target != null)                               // smoothed walking direction of the NPC
        {
            Vector3 v = target.position - lastTargetPos;
            v.y = 0f;
            if (v.sqrMagnitude > 1e-6f)
                heading = Vector3.Slerp(heading, v.normalized, 1f - Mathf.Exp(-2f * Time.deltaTime)).normalized;
            lastTargetPos = target.position;
        }

        GetWanted(out Vector3 pos, out Quaternion rot);
        float t = 1f - Mathf.Exp(-smoothing * Time.deltaTime);
        transform.position = Vector3.Lerp(transform.position, pos, t);
        transform.rotation = Quaternion.Slerp(transform.rotation, rot, t);
    }

    public void SnapToWanted()
    {
        GetWanted(out Vector3 pos, out Quaternion rot);
        transform.SetPositionAndRotation(pos, rot);
    }

    void GetWanted(out Vector3 pos, out Quaternion rot)
    {
        if (top || target == null)
        {
            Vector3 centre = grid.transform.position + new Vector3(grid.width, 0f, grid.depth) * (grid.cellSize * 0.5f);
            pos = centre + Vector3.up * topHeight;
            rot = Quaternion.Euler(90f, 0f, 0f);
        }
        else
        {
            pos = target.position - heading * followDistance + Vector3.up * followHeight;
            rot = Quaternion.LookRotation(target.position + heading * 5f - pos);
        }
    }
}
