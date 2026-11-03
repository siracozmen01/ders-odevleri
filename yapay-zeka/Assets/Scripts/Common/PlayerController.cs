using UnityEngine;

/// <summary>
/// WASD movement relative to the camera. Shift = run, Ctrl = crouch (harder to spot).
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    public float walkSpeed = 4f;
    public float runSpeed = 7f;
    public float crouchSpeed = 2f;
    public float gravity = -20f;

    public bool IsCrouching { get; private set; }
    public bool IsCaught { get; set; }

    CharacterController controller;
    float verticalVelocity;
    Transform cam;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        if (Camera.main != null) cam = Camera.main.transform;
    }

    void Update()
    {
        if (IsCaught) return;

        Vector2 input = InputHelper.Move();
        IsCrouching = InputHelper.Crouch();
        float speed = IsCrouching ? crouchSpeed : (InputHelper.Sprint() ? runSpeed : walkSpeed);

        Vector3 forward = cam != null ? Vector3.ProjectOnPlane(cam.forward, Vector3.up).normalized : Vector3.forward;
        Vector3 right = cam != null ? Vector3.ProjectOnPlane(cam.right, Vector3.up).normalized : Vector3.right;
        Vector3 move = (forward * input.y + right * input.x) * speed;

        if (controller.isGrounded && verticalVelocity < 0f) verticalVelocity = -2f;
        verticalVelocity += gravity * Time.deltaTime;
        move.y = verticalVelocity;

        controller.Move(move * Time.deltaTime);

        Vector3 flat = new Vector3(move.x, 0f, move.z);
        if (flat.sqrMagnitude > 0.01f)
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(flat), 12f * Time.deltaTime);

        // Visual crouch
        transform.localScale = new Vector3(1f, IsCrouching ? 0.6f : 1f, 1f);
    }
}
