using UnityEngine;
using UnityEngine.InputSystem;
namespace D22
{
    // Scans have no collision mesh. Preserve the web prototype's bounded free camera.
    public sealed class D22ScanWalk : MonoBehaviour
    {
        public Bounds bounds = new Bounds(Vector3.zero, Vector3.one * 20);
        public float speed = 1.2f;
        private float yaw = 180, pitch;
        void Update()
        {
            if (D22GameFlow.InputBlocked) return;
            var k = Keyboard.current; var m = Mouse.current;
            if (k == null || m == null) return;
            if (m.rightButton.isPressed)
            {
                var delta = m.delta.ReadValue() * .09f;
                yaw += delta.x; pitch = Mathf.Clamp(pitch - delta.y, -80, 80);
                transform.rotation = Quaternion.Euler(pitch, yaw, 0);
            }
            Vector3 input = new Vector3((k.dKey.isPressed?1:0)-(k.aKey.isPressed?1:0),0,(k.wKey.isPressed?1:0)-(k.sKey.isPressed?1:0));
            var next = transform.position + transform.TransformDirection(Vector3.ClampMagnitude(input,1))*speed*Time.deltaTime;
            transform.position = bounds.ClosestPoint(next);
        }
    }
}
