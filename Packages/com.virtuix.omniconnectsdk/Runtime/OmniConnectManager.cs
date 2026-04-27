#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
using System;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Virtuix.OmniConnectSdk
{
    /// <summary>
    /// Singleton MonoBehaviour that connects to shared memory and provides Omni One movement and arm yaw data.
    /// </summary>
    public class OmniConnectManager : MonoBehaviour
    {
        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct OmniData
        {
            public float movementX;
            public float movementY;
            public float armYaw;
        }      

        public static OmniConnectManager Instance { get; private set; }

        private const string SHARED_MEMORY_NAME = "OmniOneSharedMemory";
        private static readonly int SHARED_MEMORY_SIZE = Marshal.SizeOf<OmniData>();

        private MemoryMappedFile sharedMemoryMappedFile;
        private MemoryMappedViewAccessor sharedMemoryAccessor;
        
        private OmniData latestData;

        private bool hasLoggedOpenMemoryFailure = false;
        private bool hasLoggedOmniNotConnected = false;


        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            OpenSharedMemory();
        }

        void OnDestroy()
        {
            CloseSharedMemory();
        }

        void Update()
        {
            if (sharedMemoryAccessor == null && !TryResolveMemory()) { return; }
            
            ReadSharedMemory();
            VerifyOmniDeviceConnection();
        }

        private static void EnsureInstance()
        {
            if (Instance != null) return;

            var go = new GameObject(nameof(OmniConnectManager));
            DontDestroyOnLoad(go);
            Instance = go.AddComponent<OmniConnectManager>();
        }

        private static bool TryResolveMemory()
        {
            Instance.OpenSharedMemory();
            if (Instance.sharedMemoryAccessor == null) { return false; }
            return true;
        }

        private void OpenSharedMemory()
        {
            try
            {
                sharedMemoryMappedFile = MemoryMappedFile.OpenExisting(SHARED_MEMORY_NAME);
                sharedMemoryAccessor = sharedMemoryMappedFile.CreateViewAccessor(0, SHARED_MEMORY_SIZE, MemoryMappedFileAccess.Read);
                Debug.Log("[OMNI] Shared memory mapped successfully.");
                hasLoggedOpenMemoryFailure = false;
            }
            catch (Exception ex)
            {
                if (!hasLoggedOpenMemoryFailure)
                {
                    Debug.LogError($"[OMNI] Failed to open shared memory. Please ensure Omni Connect is installed and running. {ex.Message}");
                    hasLoggedOpenMemoryFailure = true;
                }
            }
        }

        private void ReadSharedMemory()
        {
            if (sharedMemoryAccessor == null) { return; }

            try
            {
                sharedMemoryAccessor.Read(0, out latestData);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[OMNI] Shared memory read failed: {ex.Message}");
            }
        }

        private void VerifyOmniDeviceConnection()
        {
            if (latestData.movementX == 0.0f && latestData.movementY == 0 && latestData.armYaw == 0)
            {
                if (!hasLoggedOmniNotConnected)
                {
                    Debug.LogWarning("No Omni One device connected. Please connect your Omni One device to Omni Connect via Bluetooth.");
                    hasLoggedOmniNotConnected = true;
                }
                return;
            }
            hasLoggedOmniNotConnected = false;
        }

        private void CloseSharedMemory()
        {
            sharedMemoryAccessor?.Dispose();
            sharedMemoryAccessor = null;
            sharedMemoryMappedFile?.Dispose();
            sharedMemoryMappedFile = null;
        }

        /// <summary>
        /// Returns the latest Omni movement vector.
        /// To apply it to the player movement, rotate this vector by the world camera yaw for world-space movement input.
        /// (only call this function in the main Unity thread)
        /// </summary>
        public static Vector2 GetMovementVector()
        {
            EnsureInstance();
            return new Vector2(Instance.latestData.movementX, Instance.latestData.movementY);
        }

        /// <summary>
        /// Returns the latest Omni arm yaw.
        /// To apply it to the player model rotation, subtract the player yaw from it.
        /// (only call this function in the main Unity thread)
        /// </summary>
        public static float GetArmYaw()
        {
            EnsureInstance();
            return Instance.latestData.armYaw;
        }
    }
}
#endif