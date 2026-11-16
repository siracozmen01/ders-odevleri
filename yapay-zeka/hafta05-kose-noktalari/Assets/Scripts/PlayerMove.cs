// File: Assets/Scripts/PlayerMove.cs  (week 4 slide "PlayerMove.cs - New Input System"; unchanged in week 5)
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerMove : MonoBehaviour
{
    public float speed = 5f;
    public float turnSpeed = 10f;
    public float gravity = -20f;

    CharacterController cc;
    InputAction move;
    float verticalSpeed;

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        move = InputSystem.actions != null ? InputSystem.actions.FindAction("Move") : null;   // Project-Wide Actions (week 3)

        // This standalone project has no project-wide actions asset, so build the same Move action in code:
        // WASD + arrow keys + gamepad left stick.
        if (move == null)
        {
            move = new InputAction("Move", InputActionType.Value);
            move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
            move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/upArrow").With("Down", "<Keyboard>/downArrow")
                .With("Left", "<Keyboard>/leftArrow").With("Right", "<Keyboard>/rightArrow");
            move.AddBinding("<Gamepad>/leftStick");
        }
        move.Enable();
    }

    void Update()
    {
        Vector2 input = move.ReadValue<Vector2>();       // 1. Move -> wanted direction
        Vector3 dir = new Vector3(input.x, 0f, input.y);
        if (dir.sqrMagnitude > 1f) dir.Normalize();

        if (dir.sqrMagnitude > 0.001f)                  // 2. turn smoothly towards it
        {
            float t = 1f - Mathf.Exp(-turnSpeed * Time.deltaTime);
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), t);
        }

        if (cc.isGrounded && verticalSpeed < 0f) verticalSpeed = -2f;
        verticalSpeed += gravity * Time.deltaTime;          // 3. gravity (Euler)
        cc.Move((dir * speed + Vector3.up * verticalSpeed) * Time.deltaTime);  // 4. move
    }
}
