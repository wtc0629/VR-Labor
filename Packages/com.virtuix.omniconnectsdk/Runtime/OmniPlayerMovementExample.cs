using UnityEngine;

namespace Virtuix.OmniConnectSdk
{
    /// <summary>
    /// Provides an example of how to apply Omni One input as player movement.
    /// </summary>
    public class OmniPlayerMovementExample : MonoBehaviour
    {
        [Tooltip("Adjusts how fast the player moves based on Omni input. Increase for large environments, decrease for small ones.")]
        public float movementSpeedMultiplier = 1.0f;

        private Camera mainCamera;
        private bool hasLoggedMissingCamera = false;

        private void Awake()
        {
            TryResolveCamera();
        }

        void Update()
        {
            // Check for a valid camera reference and try to get it if there is not a valid reference.
            if (mainCamera == null && !TryResolveCamera()) { return; }

            // Get 2D movement vector from Omni Connect.
            Vector2 omniMovementVector2 = OmniConnectManager.GetMovementVector();

            if (omniMovementVector2.sqrMagnitude > 0.0001f)
            {
                // Convert the vector to a Vector3
                Vector3 omniMovementVector3 = new Vector3(omniMovementVector2.x, 0, omniMovementVector2.y);

                // Debug.Log(omniMovementVector2);

                // Rotate the movement vector to align with the camera's yaw.
                Quaternion cameraRotation = Quaternion.Euler(0.0f, mainCamera.transform.rotation.eulerAngles.y, 0.0f);
                Vector3 rotatedMovementVector3 = cameraRotation * omniMovementVector3;

                // Apply the final movement to the player.
                transform.Translate(Time.deltaTime * movementSpeedMultiplier * rotatedMovementVector3, Space.World);
            }

#if UNITY_EDITOR
            // Get omniArmYaw from Omni Connect. This can be used to drive the third-person character model for the player.
            // The head is not always facing the same direction as the body, so this would make it look more natural.
            // This variable is not needed if you are just trying to move a first-person character.
            float omniArmYaw = OmniConnectManager.GetArmYaw();

            // If all values are 0, then there is no Omni connected.
            if (omniMovementVector2.x != 0.0f || omniMovementVector2.y != 0.0f || omniArmYaw != 0.0f) 
            { 
                // Output Omni data to confirm the input is working.
                Debug.Log($"[OMNI] OmniMovementVector2: {omniMovementVector2}, omniArmYaw: {omniArmYaw:F2}");
            }

        #endif
        }

        private bool TryResolveCamera()
        {
            mainCamera = Camera.main;

            if (!mainCamera)
            {
                if (!hasLoggedMissingCamera)
                {
                    Debug.LogError(
                        "[OmniPlayerMovementExample] No camera tagged 'Main Camera' in the scene." +
                        "Omni movement is based off of the player camera forward and therefore needs a reference to the camera."
                    );
                    hasLoggedMissingCamera = true;      // ensure we only show error once.
                }
                return false;
            }
            if (hasLoggedMissingCamera) { hasLoggedMissingCamera = false; }
            return true;
        }
    }
}