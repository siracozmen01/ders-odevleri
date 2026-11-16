using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Third-person follow camera written for the assignment (lecture notes, section 5).
/// - Runs in LateUpdate, after the player has finished moving in Update.
/// - Smoothing uses t = 1 - e^(-k * dt), so it converges at the same speed at any frame rate.
/// - A SphereCast from the focus point pulls the camera in front of hills and trees (exercise 7c).
/// </summary>
public class FollowCamera : MonoBehaviour
{
    [SerializeField] InputActionAsset actions;
    [SerializeField] Transform target;
    [SerializeField] float distance = 6f;
    [SerializeField] float focusHeight = 0.6f;          // capsule pivot is in the middle: head height
    [SerializeField] float mouseSensitivity = 0.15f;    // degrees per pixel
    [SerializeField] float stickSpeed = 120f;           // degrees per second for a gamepad stick
    [SerializeField] float minPitch = -20f, maxPitch = 60f;
    [SerializeField] float sharpness = 10f;             // k (1/s)

    [Header("Anti-clipping (exercise 7c)")]
    [SerializeField] LayerMask obstacles = 1;           // Default: terrain and trees, NOT the Player layer
    [SerializeField] float collisionRadius = 0.3f;

    InputAction look, lookStick;
    float yaw, pitch = 20f;

    void Awake()
    {
        if (actions == null) actions = InputSystem.actions;
        InputActionMap map = actions.FindActionMap("Player", true);
        map.Enable();
        look = map.FindAction("Look", true);
        lookStick = map.FindAction("LookStick", true);

        yaw = target.eulerAngles.y;
        Cursor.lockState = CursorLockMode.Locked;
        transform.position = DesiredPosition(out _);    // start in place, no fly-in
        transform.LookAt(Focus);
    }

    Vector3 Focus => target.position + Vector3.up * focusHeight;

    void LateUpdate()
    {
        float dt = Time.deltaTime;

        // Mouse delta is already "per frame": it is NOT multiplied by dt.
        // A stick behaves like a speed, so it IS multiplied by dt.
        Vector2 mouse = Cursor.lockState == CursorLockMode.Locked ? look.ReadValue<Vector2>() : Vector2.zero;
        Vector2 stick = lookStick.ReadValue<Vector2>() * (stickSpeed * dt);
        yaw += mouse.x * mouseSensitivity + stick.x;
        pitch = Mathf.Clamp(pitch - mouse.y * mouseSensitivity - stick.y, minPitch, maxPitch);

        Vector3 desired = DesiredPosition(out bool blocked);
        Vector3 focus = Focus;

        if (blocked && (desired - focus).sqrMagnitude < (transform.position - focus).sqrMagnitude)
        {
            // Something is between the player and the camera: jump in immediately,
            // otherwise the smoothing would show the inside of the hill for a moment.
            transform.position = desired;
        }
        else
        {
            float t = 1f - Mathf.Exp(-sharpness * dt);   // frame-rate independent
            transform.position = Vector3.Lerp(transform.position, desired, t);
        }
        transform.LookAt(focus);
    }

    Vector3 DesiredPosition(out bool blocked)
    {
        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 back = -(rotation * Vector3.forward);
        Vector3 focus = Focus;
        float m = distance;
        blocked = Physics.SphereCast(focus, collisionRadius, back, out RaycastHit hit, distance,
                                     obstacles, QueryTriggerInteraction.Ignore);
        if (blocked) m = hit.distance;   // the sphere centre stays collisionRadius away from the obstacle
        return focus + back * m;
    }

    /// <summary>Sets the orbit angles (used by the reset key and for screenshots).</summary>
    public void SetAngles(float newYaw, float newPitch, bool snap)
    {
        yaw = newYaw;
        pitch = Mathf.Clamp(newPitch, minPitch, maxPitch);
        if (snap)
        {
            transform.position = DesiredPosition(out _);
            transform.LookAt(Focus);
        }
    }
}
