using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Works with both the new Input System and the legacy Input Manager,
/// so the project runs regardless of the "Active Input Handling" setting.
/// </summary>
public static class InputHelper
{
    public static Vector2 Move()
    {
#if ENABLE_INPUT_SYSTEM
        var k = Keyboard.current;
        if (k == null) return Vector2.zero;
        float x = (k.dKey.isPressed || k.rightArrowKey.isPressed ? 1f : 0f) - (k.aKey.isPressed || k.leftArrowKey.isPressed ? 1f : 0f);
        float y = (k.wKey.isPressed || k.upArrowKey.isPressed ? 1f : 0f) - (k.sKey.isPressed || k.downArrowKey.isPressed ? 1f : 0f);
        return Vector2.ClampMagnitude(new Vector2(x, y), 1f);
#else
        return Vector2.ClampMagnitude(new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")), 1f);
#endif
    }

    public static bool Sprint()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.leftShiftKey.isPressed;
#else
        return Input.GetKey(KeyCode.LeftShift);
#endif
    }

    public static bool Crouch()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.leftCtrlKey.isPressed;
#else
        return Input.GetKey(KeyCode.LeftControl);
#endif
    }

    public static bool RestartPressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.R);
#endif
    }

    public static bool KeyDown(char c)
    {
#if ENABLE_INPUT_SYSTEM
        var k = Keyboard.current;
        if (k == null) return false;
        switch (char.ToLowerInvariant(c))
        {
            case 'g': return k.gKey.wasPressedThisFrame;
            case 'c': return k.cKey.wasPressedThisFrame;
            case 'h': return k.hKey.wasPressedThisFrame;
            case 'r': return k.rKey.wasPressedThisFrame;
            case 'n': return k.nKey.wasPressedThisFrame;
            default: return false;
        }
#else
        return Input.GetKeyDown(char.ToLowerInvariant(c).ToString());
#endif
    }

    public static bool LeftClick()
    {
#if ENABLE_INPUT_SYSTEM
        return Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
#else
        return Input.GetMouseButtonDown(0);
#endif
    }

    public static bool RightClick()
    {
#if ENABLE_INPUT_SYSTEM
        return Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame;
#else
        return Input.GetMouseButtonDown(1);
#endif
    }

    public static Vector2 MousePosition()
    {
#if ENABLE_INPUT_SYSTEM
        return Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
#else
        return Input.mousePosition;
#endif
    }
}
