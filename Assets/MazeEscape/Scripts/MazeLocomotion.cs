using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
using Virtuix.OmniConnectSdk;
#endif

namespace MazeEscape
{
    public enum MovementReferenceMode
    {
        ViewBased,
        OmniArmRelative
    }

    [RequireComponent(typeof(CharacterController))]
    public class MazeLocomotion : MonoBehaviour
    {
        [Header("Input Actions (Fallback)")]
        public InputActionReference MoveAction;

        [Header("Speed (Thumbstick)")]
        public float WalkSpeed = 2f;
        public float Gravity = -9.81f;
        public float threshhold;

        [Header("Omni One")]
        [Tooltip("Scales Omni movement vector. Increase for large environments.")]
        public float OmniSpeedMultiplier = 3f;

        [Header("Input Smoothing")]
        [Tooltip("How many seconds of input are averaged to reduce jitter.")]
        public float InputSmoothingWindow = 0.2f;
        [Tooltip("Five discrete movement levels selected from the averaged input magnitude.")]
        public float[] SpeedLevels = { 0f, 0.25f, 0.5f, 0.75f, 1f };

        [Header("HMD Reference")]
        [Tooltip("Assign the Main Camera (inside XR Origin)")]
        public Transform HMDCamera;

        [Header("Movement Direction")]
        [Tooltip("Choose whether movement follows the HMD view direction or the Omni arm yaw. Omni Arm Relative is the default.")]
        public MovementReferenceMode MovementMode = MovementReferenceMode.OmniArmRelative;

        private CharacterController _cc;
        private float _verticalVelocity;
        private readonly Queue<InputSample> _inputSamples = new Queue<InputSample>();

        private struct InputSample
        {
            public float Time;
            public Vector2 Value;
        }

        void Awake()
        {
            _cc = GetComponent<CharacterController>();
            transform.position += Vector3.up * (_cc.skinWidth + 0.5f);
        }

        void OnEnable()
        {
            MoveAction?.action.Enable();
        }

        void OnDisable()
        {
            MoveAction?.action.Disable();
        }

        void Update()
        {
            Vector3 moveDir;
            bool usingOmni = false;

            Quaternion flatRotation = GetMovementRotation();
            if (!IsFiniteQuaternion(flatRotation))
            {
                flatRotation = Quaternion.identity;
            }

            float currentYaw = flatRotation.eulerAngles.y;
            //Debug.Log($"[OMNI YAW] {currentYaw:F2}");

#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
            Vector2 omniInput = OmniConnectManager.GetMovementVector();
            if (!IsFiniteVector2(omniInput))
            {
                omniInput = Vector2.zero;
            }

            if (omniInput.sqrMagnitude > 0.0001f)
            {
                AddInputSample(omniInput);
                usingOmni = true;
            }
            else
#endif
            {
                Vector2 stickInput = MoveAction != null ? MoveAction.action.ReadValue<Vector2>() : Vector2.zero;
                if (!IsFiniteVector2(stickInput))
                {
                    stickInput = Vector2.zero;
                }

                AddInputSample(stickInput);
            }

            Vector2 smoothedInput = GetSmoothedInput();
            if (!IsFiniteVector2(smoothedInput))
            {
                smoothedInput = Vector2.zero;
            }

            float smoothedMagnitude = smoothedInput.magnitude;
            if (!IsFinite(smoothedMagnitude))
            {
                smoothedMagnitude = 0f;
            }

            if (smoothedMagnitude > threshhold)
            {
                Vector3 worldMove;
                if (!usingOmni)
                {
                    Quaternion viewRotation = Quaternion.Euler(0f, HMDCamera != null ? HMDCamera.eulerAngles.y : transform.eulerAngles.y, 0f);
                    worldMove = viewRotation * new Vector3(smoothedInput.x, 0f, smoothedInput.y);
                }
                else
                {
                    worldMove = flatRotation * Vector3.forward;
                }

                if (!IsFiniteVector3(worldMove))
                {
                    worldMove = Vector3.zero;
                }
                else if (worldMove.sqrMagnitude > 0.0001f)
                {
                    worldMove.Normalize();
                }

                float selectedSpeed = GetDiscreteSpeed(smoothedMagnitude, usingOmni);
                if (!IsFinite(selectedSpeed))
                {
                    selectedSpeed = 0f;
                }

                moveDir = worldMove * selectedSpeed;
            }
            else
            {
                moveDir = Vector3.zero;
            }

            if (_cc.isGrounded && _verticalVelocity < 0f)
                _verticalVelocity = -2f;
            _verticalVelocity += Gravity * Time.deltaTime;

            Vector3 motion = moveDir * Time.deltaTime;
            motion.y = _verticalVelocity * Time.deltaTime;
            _cc.Move(motion);
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private static bool IsFiniteVector2(Vector2 value)
        {
            return IsFinite(value.x) && IsFinite(value.y);
        }

        private static bool IsFiniteVector3(Vector3 value)
        {
            return IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z);
        }

        private static bool IsFiniteQuaternion(Quaternion value)
        {
            return IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z) && IsFinite(value.w);
        }

        private Quaternion GetMovementRotation()
        {
            switch (MovementMode)
            {
                case MovementReferenceMode.ViewBased:
                    if (HMDCamera != null)
                        return Quaternion.Euler(0f, HMDCamera.eulerAngles.y, 0f);
                    return Quaternion.identity;

                case MovementReferenceMode.OmniArmRelative:
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
                    float playerYaw = transform.eulerAngles.y;
                    float omniYaw = OmniConnectManager.GetArmYaw();
                    //float omniYaw = 0f;
                    float relativeYaw = Mathf.DeltaAngle(playerYaw, omniYaw);
                    return Quaternion.Euler(0f, playerYaw + relativeYaw, 0f);
#else
                    return Quaternion.identity;
#endif

                default:
                    return Quaternion.identity;
            }
        }

        private void AddInputSample(Vector2 input)
        {
            float now = Time.unscaledTime;
            _inputSamples.Enqueue(new InputSample { Time = now, Value = input });

            while (_inputSamples.Count > 0 && now - _inputSamples.Peek().Time > InputSmoothingWindow)
            {
                _inputSamples.Dequeue();
            }
        }

        private Vector2 GetSmoothedInput()
        {
            if (_inputSamples.Count == 0)
                return Vector2.zero;

            Vector2 sum = Vector2.zero;
            foreach (InputSample sample in _inputSamples)
            {
                sum += sample.Value;
            }

            return sum / _inputSamples.Count;
        }

        private float GetDiscreteSpeed(float smoothedMagnitude, bool usingOmni)
        {
            if (SpeedLevels == null || SpeedLevels.Length == 0)
                return 0f;

            int stepIndex = Mathf.Clamp(Mathf.RoundToInt(Mathf.Clamp01(smoothedMagnitude) * (SpeedLevels.Length - 1)), 0, SpeedLevels.Length - 1);
            float level = SpeedLevels[stepIndex];
            return usingOmni ? level * OmniSpeedMultiplier : level * WalkSpeed;
        }
    }
}
