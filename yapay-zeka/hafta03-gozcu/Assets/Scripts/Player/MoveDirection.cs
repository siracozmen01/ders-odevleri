using UnityEngine;

/// <summary>
/// Turns the Move input (a 2D vector from the New Input System) into a world-space
/// direction on the ground plane, relative to the camera (lecture notes, section 4.2):
///   d = f_xz * g_y + r_xz * g_x,  |d| &lt;= 1
/// </summary>
public static class MoveDirection
{
    public static Vector3 RelativeToCamera(Vector2 input, Transform cam)
    {
        // Flatten the camera axes onto the XZ plane. Without this, a camera looking
        // 30 degrees down would push the character into the ground and slow it down.
        Vector3 forward = cam.forward; forward.y = 0f; forward.Normalize();
        Vector3 right = cam.right; right.y = 0f; right.Normalize();

        Vector3 direction = forward * input.y + right * input.x;

        // W + D gives (1, 1) with length 1.41 on a raw input. Clamping keeps the
        // diagonal speed equal to the straight speed.
        return Vector3.ClampMagnitude(direction, 1f);
    }
}
