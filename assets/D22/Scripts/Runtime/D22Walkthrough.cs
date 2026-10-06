using UnityEngine;
using UnityEngine.InputSystem;

namespace D22
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class D22Walkthrough : MonoBehaviour
    {
        [SerializeField] private Transform view;
        [SerializeField] private float speed = 2.4f;
        private float pitch;
        private float verticalSpeed;
        private Vector3 lastSafePosition;
        private void Start() => lastSafePosition = transform.position;
        public void SetView(Transform cameraTransform) => view = cameraTransform;

        private void Update()
        {
            if (D22GameFlow.InputBlocked)
            {
                D22Look.Unlock();
                return;
            }
            if (view == null && Camera.main) view = Camera.main.transform;
            var keyboard = Keyboard.current;
            if (keyboard == null || view == null) return;
            D22Look.ApplyYawPitch(transform, view, ref pitch, D22GameFlow.UiBlocksLook);
            float x = (keyboard.dKey.isPressed ? 1 : 0) - (keyboard.aKey.isPressed ? 1 : 0);
            float z = (keyboard.wKey.isPressed ? 1 : 0) - (keyboard.sKey.isPressed ? 1 : 0);
            var controller = GetComponent<CharacterController>();
            if (controller.isGrounded && verticalSpeed < 0) verticalSpeed = -2;
            if (controller.isGrounded && keyboard.spaceKey.wasPressedThisFrame) verticalSpeed = 3.2f;
            verticalSpeed += Physics.gravity.y * Time.deltaTime;
            Vector3 motion = transform.TransformDirection(Vector3.ClampMagnitude(new Vector3(x, 0, z), 1));
            if (motion.sqrMagnitude > .0001f) D22GameFlow.Instance?.NoteWalk();
            motion *= speed * (keyboard.leftShiftKey.isPressed ? 1.6f : 1);
            motion.y = verticalSpeed;
            Vector3 beforeMove = transform.position;
            controller.Move(motion * Time.deltaTime);
            var walkArea = GetComponent<D22WalkArea>();
            if (walkArea && !walkArea.Contains(transform.position))
            {
                controller.enabled = false;
                transform.position = beforeMove;
                controller.enabled = true;
                verticalSpeed = 0;
                return;
            }
            if (controller.isGrounded && transform.position.y > -.5f)
                lastSafePosition = transform.position;
            else if (transform.position.y < -1.25f)
            {
                controller.enabled = false;
                transform.position = lastSafePosition;
                controller.enabled = true;
                verticalSpeed = 0;
            }
        }
    }
}
