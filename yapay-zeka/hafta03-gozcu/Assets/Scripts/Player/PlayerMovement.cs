using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Player character moved with a CharacterController (see README for why).
/// Input comes from the "Player" action map of the New Input System; the movement
/// direction is relative to the follow camera and every step is multiplied by delta time.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [SerializeField] InputActionAsset actions;   // GozcuControls.inputactions
    [SerializeField] Transform cameraTransform;
    [SerializeField] float walkSpeed = 4f;
    [SerializeField] float sprintSpeed = 7f;
    [SerializeField] float jumpHeight = 1.5f;
    [SerializeField] float gravity = -9.81f;
    [SerializeField] float turnSpeed = 720f;     // degrees per second

    CharacterController controller;
    InputAction move, jump, sprint;
    float verticalSpeed;
    Vector3 spawnPosition;
    Quaternion spawnRotation;

    /// <summary>Horizontal velocity requested in the last frame (m/s), shown in the HUD.</summary>
    public Vector3 HorizontalVelocity { get; private set; }

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        if (actions == null) actions = InputSystem.actions;
        InputActionMap map = actions.FindActionMap("Player", true);
        map.Enable();
        move = map.FindAction("Move", true);
        jump = map.FindAction("Jump", true);
        sprint = map.FindAction("Sprint", true);
        if (cameraTransform == null && Camera.main != null) cameraTransform = Camera.main.transform;

        spawnPosition = transform.position;
        spawnRotation = transform.rotation;
    }

    void Update()
    {
        float dt = Time.deltaTime;

        Vector3 direction = MoveDirection.RelativeToCamera(move.ReadValue<Vector2>(), cameraTransform);
        float speed = sprint.IsPressed() ? sprintSpeed : walkSpeed;

        // A small downward speed keeps the capsule pressed to the ground so isGrounded does not flicker.
        if (controller.isGrounded && verticalSpeed < 0f) verticalSpeed = -2f;
        if (controller.isGrounded && jump.WasPressedThisFrame())
            verticalSpeed = Mathf.Sqrt(2f * -gravity * jumpHeight);   // v0 = sqrt(2 g h)

        verticalSpeed += gravity * dt;                                // Euler step: velocity

        HorizontalVelocity = direction * speed;
        Vector3 velocity = HorizontalVelocity + Vector3.up * verticalSpeed;
        controller.Move(velocity * dt);                               // Euler step: position

        if (direction.sqrMagnitude > 0.001f)
        {
            Quaternion target = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, target, turnSpeed * dt);
        }
    }

    /// <summary>Moves the player without the CharacterController fighting the jump.</summary>
    public void Teleport(Vector3 position, Quaternion rotation)
    {
        controller.enabled = false;
        transform.SetPositionAndRotation(position, rotation);
        controller.enabled = true;
        verticalSpeed = 0f;
    }

    public void ResetToSpawn() => Teleport(spawnPosition, spawnRotation);
}
