using UnityEngine;
using UnityEngine.InputSystem;

namespace MazeEscape
{
    // OmniOneLocomotion: replace ReadMoveInput() with Omni SDK speed/direction when available
    [RequireComponent(typeof(CharacterController))]
    public class MazeLocomotion : MonoBehaviour
    {
        [Header("Input Actions")]
        public InputActionReference MoveAction;
        public InputActionReference SprintAction;

        [Header("Speed")]
        public float WalkSpeed = 2f;
        public float SprintSpeed = 4f;
        public float Gravity = -9.81f;

        [Header("HMD Reference")]
        [Tooltip("Assign the Main Camera (inside XR Origin)")]
        public Transform HMDCamera;

        private CharacterController _cc;
        private float _verticalVelocity;

        void Awake()
        {
            _cc = GetComponent<CharacterController>();
        }

        void OnEnable()
        {
            MoveAction?.action.Enable();
            SprintAction?.action.Enable();
        }

        void OnDisable()
        {
            MoveAction?.action.Disable();
            SprintAction?.action.Disable();
        }

        void Update()
        {
            Vector2 input = ReadMoveInput();
            bool sprinting = SprintAction != null && SprintAction.action.IsPressed();
            float speed = sprinting ? SprintSpeed : WalkSpeed;

            // Map thumbstick XY to world XZ relative to HMD yaw only
            float yaw = HMDCamera != null ? HMDCamera.eulerAngles.y : 0f;
            Quaternion flatRotation = Quaternion.Euler(0f, yaw, 0f);
            Vector3 moveDir = flatRotation * new Vector3(input.x, 0f, input.y);

            // Gravity
            if (_cc.isGrounded && _verticalVelocity < 0f)
                _verticalVelocity = -2f;
            _verticalVelocity += Gravity * Time.deltaTime;

            Vector3 motion = moveDir * (speed * Time.deltaTime);
            motion.y = _verticalVelocity * Time.deltaTime;
            _cc.Move(motion);
        }

        private Vector2 ReadMoveInput()
        {
            if (MoveAction == null) return Vector2.zero;
            return MoveAction.action.ReadValue<Vector2>();
        }
    }
}
