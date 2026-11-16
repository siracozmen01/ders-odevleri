// File: Assets/Scripts/PlayerMove.cs  (the character from week 3/4: WASD movement on the ground plane)
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerMove : MonoBehaviour
{
    public float speed = 5f;

    CharacterController controller;
    InputAction move;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        move = InputSystem.actions.FindAction("Move");
    }

    void Update()
    {
        Vector2 input = move.ReadValue<Vector2>();
        Vector3 dir = new Vector3(input.x, 0f, input.y);
        if (dir.sqrMagnitude > 1f) dir.Normalize();
        controller.Move(dir * speed * Time.deltaTime);
    }
}
