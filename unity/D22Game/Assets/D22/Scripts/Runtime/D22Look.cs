using UnityEngine;
using UnityEngine.InputSystem;

namespace D22
{
    // Click-to-lock look, matching the web prototype. Right-hold still works.
    // Mouse.delta is unreliable in the Game view until the cursor is locked.
    public static class D22Look
    {
        public const float Sensitivity = .13f;
        public const float MinPitch = -75;
        public const float MaxPitch = 75;

        public static bool Locked => Cursor.lockState == CursorLockMode.Locked;

        public static void Unlock()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        public static void Lock()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        public static bool TryRead(bool uiBlocks, out Vector2 degrees)
        {
            degrees = Vector2.zero;
            var mouse = Mouse.current;
            if (mouse == null) return false;
            bool hold = mouse.rightButton.isPressed || mouse.leftButton.isPressed;
            if (mouse.leftButton.wasPressedThisFrame && !uiBlocks) Lock();
            if (mouse.rightButton.wasPressedThisFrame && !uiBlocks) Lock();
            if (!Locked && !(hold && !uiBlocks)) return false;
            Vector2 delta = mouse.delta.ReadValue() * Sensitivity;
            if (delta.sqrMagnitude < .0001f) return false;
            degrees = new Vector2(delta.x, -delta.y);
            return true;
        }

        public static void ApplyYawPitch(Transform body, Transform view, ref float pitch, bool uiBlocks)
        {
            if (!TryRead(uiBlocks, out var degrees) || body == null) return;
            body.Rotate(0, degrees.x, 0);
            if (view == null) return;
            pitch = Mathf.Clamp(pitch + degrees.y, MinPitch, MaxPitch);
            view.localRotation = Quaternion.Euler(pitch, 0, 0);
        }

        public static void ApplyFree(Transform view, ref float yaw, ref float pitch, bool uiBlocks)
        {
            if (!TryRead(uiBlocks, out var degrees) || view == null) return;
            yaw += degrees.x;
            pitch = Mathf.Clamp(pitch + degrees.y, MinPitch, MaxPitch);
            view.rotation = Quaternion.Euler(pitch, yaw, 0);
        }
    }
}
