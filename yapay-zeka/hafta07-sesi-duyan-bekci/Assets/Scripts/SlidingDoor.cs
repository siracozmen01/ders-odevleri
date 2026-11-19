// File: Assets/Scripts/SlidingDoor.cs  (week 6 section 11.4; IsOpen added in week 7 section 8.6)
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

// The K key opens/closes the door. When open, the door sinks into the ground and does not carve the NavMesh.
[RequireComponent(typeof(NavMeshObstacle))]
public class SlidingDoor : MonoBehaviour
{
    public float closedY = 1f;     // centre height when closed (a 2 m door standing on the ground)
    public float openY = -1.1f;    // centre height when open (door fully below the ground)
    public float speed = 2f;       // metres per second
    public bool startOpen = true;

    NavMeshObstacle obstacle;
    InputAction toggle;
    bool isOpen;

    public bool IsOpen => isOpen;   // read-only: the guard reads it but cannot change it (week 7, 8.6)

    void Awake()
    {
        obstacle = GetComponent<NavMeshObstacle>();
        toggle = InputSystem.actions.FindAction("ToggleDoor");
        isOpen = startOpen;
        SetY(isOpen ? openY : closedY);
        obstacle.enabled = !isOpen;
    }

    void Update()
    {
        // 1. Key pressed: flip the state and switch the obstacle on/off right away
        if (toggle.WasPressedThisFrame())
            Toggle();

        // 2. Slide the door towards its target height
        float targetY = isOpen ? openY : closedY;
        SetY(Mathf.MoveTowards(transform.position.y, targetY, speed * Time.deltaTime));
    }

    // Same as pressing K
    public void Toggle()
    {
        isOpen = !isOpen;
        obstacle.enabled = !isOpen;
    }

    void SetY(float y)
    {
        Vector3 p = transform.position;
        p.y = y;
        transform.position = p;
    }
}
