using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace MazeEscape
{
    // While the right-hand ray hovers this slider's hitzone, the right
    // controller's thumbstick left/right steps Value up/down.
    public class MazeSizeSlider : MonoBehaviour
    {
        public int MinValue = 4;
        public int MaxValue = 20;
        public int Value { get; private set; }
        public System.Action<int> OnValueChanged;

        private const float Deadzone = 0.4f;
        private const float StepInterval = 0.2f;

        private InputAction _thumbstickAction;
        private bool _isHovered;
        private float _stepTimer;

        public void Setup(int initialValue)
        {
            Value = Mathf.Clamp(initialValue, MinValue, MaxValue);
        }

        void Awake()
        {
            _thumbstickAction = new InputAction("SliderThumbstick", InputActionType.Value, expectedControlType: "Vector2");
            _thumbstickAction.AddBinding("<XRController>{RightHand}/thumbstick");
            _thumbstickAction.Enable();
        }

        void OnEnable()
        {
            var interactable = GetComponent<XRSimpleInteractable>();
            interactable.hoverEntered.AddListener(args =>
            {
                if (args.interactorObject.handedness == InteractorHandedness.Right)
                    _isHovered = true;
            });
            interactable.hoverExited.AddListener(args =>
            {
                if (args.interactorObject.handedness == InteractorHandedness.Right)
                    _isHovered = false;
            });
        }

        void OnDisable() => _thumbstickAction?.Disable();

        void Update()
        {
            if (!_isHovered) return;

            _stepTimer -= Time.deltaTime;
            if (_stepTimer > 0f) return;

            float x = _thumbstickAction.ReadValue<Vector2>().x;
            if (Mathf.Abs(x) < Deadzone) return;

            int newValue = Mathf.Clamp(Value + (x > 0 ? 1 : -1), MinValue, MaxValue);
            if (newValue != Value)
            {
                Value = newValue;
                OnValueChanged?.Invoke(Value);
            }
            _stepTimer = StepInterval;
        }
    }
}
