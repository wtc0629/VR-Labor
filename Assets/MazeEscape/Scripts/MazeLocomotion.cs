using UnityEngine;
using UnityEngine.InputSystem;
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
using Virtuix.OmniConnectSdk;
#endif

namespace MazeEscape
{
    [RequireComponent(typeof(CharacterController))]
    public class MazeLocomotion : MonoBehaviour
    {
        [Header("Input Actions (Fallback)")]
        public InputActionReference MoveAction;
        public InputActionReference SprintAction;

        [Header("Speed (Thumbstick)")]
        public float WalkSpeed = 2f;
        public float SprintSpeed = 4f;
        public float Gravity = -9.81f;

        [Header("Omni One")]
        [Tooltip("Scales Omni movement vector. Increase for large environments.")]
        public float OmniSpeedMultiplier = 3f;

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
            float yaw = HMDCamera != null ? HMDCamera.eulerAngles.y : 0f;
            Quaternion flatRotation = Quaternion.Euler(0f, yaw, 0f);

            Vector3 moveDir;
            bool usingOmni = false;

#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
            Vector2 omniInput = OmniConnectManager.GetMovementVector();
            if (omniInput.sqrMagnitude > 0.0001f)
            {
                // Omni vector already encodes direction + speed; rotate by HMD yaw
                moveDir = flatRotation * new Vector3(omniInput.x, 0f, omniInput.y);
                usingOmni = true;
            }
            else
#endif
            {
                Vector2 stickInput = MoveAction != null ? MoveAction.action.ReadValue<Vector2>() : Vector2.zero;
                bool sprinting = SprintAction != null && SprintAction.action.IsPressed();
                float speed = sprinting ? SprintSpeed : WalkSpeed;
                moveDir = flatRotation * new Vector3(stickInput.x, 0f, stickInput.y) * speed;
            }

            if (_cc.isGrounded && _verticalVelocity < 0f)
                _verticalVelocity = -2f;
            _verticalVelocity += Gravity * Time.deltaTime;

            float omniScale = usingOmni ? OmniSpeedMultiplier : 1f;
            Vector3 motion = moveDir * (omniScale * Time.deltaTime);
            motion.y = _verticalVelocity * Time.deltaTime;
            _cc.Move(motion);
        }
    }
}
