/////////////////////////////////////////////////
// LITHO SDK                                   //
// Copyright © 2020 Purple Tambourine Ltd.     //
// License: see LICENSE in package root folder //
/////////////////////////////////////////////////

/*
 * Uncomment the following line to opt in to alpha-quality Windows Bluetooth support.
 * Before doing so, please read the documentation for this feature.
 * Disabling this functionality fully will require a restart of the Unity Editor
 * (the Litho DLL will remain loaded even after exiting Play mode).
 */
 
#define WINDOWS_BLUETOOTH_ALPHA_OPTIN

using System;
using System.Runtime.InteropServices;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.XR.ARFoundation;
using System.Text;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
#if UNITY_ANDROID
using UnityEngine.Android;
#endif

namespace LITHO
{

    public enum Handedness
    {
        Right = 0,
        Left = 1
    }
    public enum GripType
    {
        Point = 0,
        Clutch = 1
    }
    public enum ViewerType
    {
        Handheld = 0,
        Headset = 1
    }
    public enum TrackingAccuracy
    {
        Low = 0,
        Medium = 1,
        High = 2,
        Max = 3
    };

    /// <summary>
    /// Represents a singleton instance of the Litho device - this will position itself relative to
    /// the given camera, such that it approximates the real-world position of whichever Litho
    /// device is connected to the application; also handles connection to Litho devices
    /// </summary>
    [AddComponentMenu("LITHO/Litho", -10000)]
    [DefaultExecutionOrder(-1000000)]
    public class Litho : MonoBehaviour
    {
        private enum EventCode
        {
            None = 0,
            DeviceFound,
            ConnectionSuccess,
            ConnectionFail,
            Disconnect,

            TouchStart = 100,
            TouchHold,
            TouchEnd,
            Tap,
            TouchLongHold,

            BatteryStatus = 200
        };

        [SerializeField]
        [Tooltip("Camera to treat as the main camera (default: Camera.main)")]
        private ARCameraManager _camera = null;
        public static ARCameraManager Camera
        {
            get { return _instance != null ? _instance._camera : null; }
            set { if (_instance != null) _instance._camera = value; }
        }

        [SerializeField]
        [Tooltip("Hand in which Litho is currently being held")]
        private Handedness _handedness = Handedness.Right;
        public static Handedness Handedness
        {
            get { return _instance != null ? _instance._handedness : Handedness.Right; }
            set { if (_instance != null) _instance._handedness = value; }
        }
        private static Handedness _lastHandedness = Handedness.Right;

        [SerializeField]
        [Tooltip("Grip in which Litho is currently being held")]
        private GripType _gripType = GripType.Point;
        public static GripType GripType
        {
            get { return _instance != null ? _instance._gripType : GripType.Point; }
            set { if (_instance != null) _instance._gripType = value; }
        }
        private static GripType _lastGripType = GripType.Point;

        [SerializeField]
        [Tooltip("Viewer type currently being used with Litho")]
        private ViewerType _viewerType = ViewerType.Handheld;
        public static ViewerType ViewerType
        {
            get { return _instance != null ? _instance._viewerType : ViewerType.Handheld; }
            set { if (_instance != null) _instance._viewerType = value; }
        }
        private static ViewerType _lastViewerType = ViewerType.Handheld;

        // The singleton instance of this Litho device class
        private static Litho _instance = null;

        public static bool IsConnected { get; private set; } = false;

        public static bool HasFatalError { get; private set; } = false;

        public static bool Touching { get; private set; }
        public static Vector2 TouchPosition { get; private set; }
        public static Vector2 TouchWorldPosition { get; private set; }
        public static float TouchStartTime { get; private set; }

        public static TrackingAccuracy AccelAccuracy { get; private set; } = TrackingAccuracy.Max;
        public static TrackingAccuracy GyroAccuracy { get; private set; } = TrackingAccuracy.Max;
        public static TrackingAccuracy MagAccuracy { get; private set; } = TrackingAccuracy.Max;
        public static bool IsCharging { get; private set; } = false;

        public static Transform Transform
        {
            get
            {
                return _instance != null ? _instance.transform : null;
            }
        }
        public static Vector3 Position
        {
            get
            {
                return _instance != null ? _instance.transform.position
                    : (Camera != null ? Camera.transform.position : Vector3.zero);
            }
        }
        public static Quaternion Rotation
        {
            get
            {
                return _instance != null ? _instance.transform.rotation : Quaternion.identity;
            }
        }

        public delegate void TouchEventHandler(Vector2 position, Vector2 worldPosition);
        public static event TouchEventHandler
            // When the thumb first makes contact with the trackpad:
            OnTouchStart,
            // Once per frame whilst the thumb is in contact with the trackpad:
            OnTouchHold,
            // When the thumb loses contact with the trackpad:
            OnTouchEnd;

        public delegate void DeviceEventHandler(string deviceName);
        public static event DeviceEventHandler OnDeviceFound,
                                               OnConnected,
                                               OnConnectionFailed,
                                               OnDisconnected;

        public delegate void BatteryLevelEventHandler(int batteryLevel);
        public static event BatteryLevelEventHandler OnBatteryLevelReceived;

        public delegate void DeviceInfoEventHandler(string deviceInfo);
        public static event DeviceInfoEventHandler OnFirmwareVersionReceived,
                                                   OnHardwareVersionReceived,
                                                   OnModelNumberReceived;

        public delegate void HandednessChangedEventHandler(
           Handedness newHandedness, Handedness oldHandedness);
        public static event HandednessChangedEventHandler OnHandednessChanged;

        public delegate void GripTypeChangedEventHandler(
            GripType newGripType, GripType oldGripType);
        public static event GripTypeChangedEventHandler OnGripTypeChanged;

        public delegate void ViewerTypeChangedEventHandler(
            ViewerType newViewerType, ViewerType oldViewerTypee);
        public static event ViewerTypeChangedEventHandler OnViewerTypeChanged;

        public delegate void FavouriteDeviceChangedEventHandler(
           string newFavouriteDeviceName, string oldFavouriteDeviceName);
        public static event FavouriteDeviceChangedEventHandler OnFavouriteDeviceChanged;

        public delegate void FatalErrorEventHandler(string errorMessage);
        public static event FatalErrorEventHandler OnFatalError;

        private Coroutine _batteryLevelCoroutine = null;
        private Coroutine _statusFlagsCoroutine = null;
        private float _batteryUpdatePeriod = 60f;
        private float _statusFlagUpdatePeriod = 1f;

        private bool _isInitialized, _dllNotFound;

        // Define where to look for the plugin
#if UNITY_IOS && !UNITY_EDITOR
        private const string PLUGIN_NAME = "__Internal";
#else
        private const string PLUGIN_NAME = "ARController";
#endif

        [DllImport(PLUGIN_NAME)]
        private static extern void Initialize();

        [DllImport(PLUGIN_NAME)]
        private static extern void Deinitialize();

        [DllImport(PLUGIN_NAME)]
        private static extern void StartScan();

        [DllImport(PLUGIN_NAME)]
        private static extern void StopScan();

        [DllImport(PLUGIN_NAME)]
        private static extern int GetNextEvent(
            StringBuilder data, int size, ref float info, ref int info2);

        [DllImport(PLUGIN_NAME)]
        private static extern void SendHapticEvents(byte numEvents, ref byte events);

        [DllImport(PLUGIN_NAME)]
        private static extern void RequestBatteryStatus();

        [DllImport(PLUGIN_NAME)]
        private static extern string GetManufacturer();

        [DllImport(PLUGIN_NAME)]
        private static extern string GetFirmwareVersion();

        [DllImport(PLUGIN_NAME)]
        private static extern string GetHardwareVersion();

        [DllImport(PLUGIN_NAME)]
        private static extern string GetModelNumber();

        [DllImport(PLUGIN_NAME)]
        private static extern void ConnectTo(
            string deviceName);

        [DllImport(PLUGIN_NAME)]
        private static extern void Disconnect();

        [DllImport(PLUGIN_NAME)]
        private static extern void SetHandedness(
            bool leftHanded);

        [DllImport(PLUGIN_NAME)]
        private static extern void SetGripType(
            int gripTypeNumber);

        [DllImport(PLUGIN_NAME)]
        private static extern void SetViewerType(
            int viewerTypeNumber);

        [DllImport(PLUGIN_NAME)]
        private static extern void Calibrate(
            ref float cameraPositionF,
            ref float cameraOrientationF);

        [DllImport(PLUGIN_NAME)]
        private static extern void CalibrateGround(
            float groundHeight,
            float certainty);

        [DllImport(PLUGIN_NAME)]
        private static extern void GetLithoPosition(
            ref float cameraPositionF,
            ref float cameraOrientationF,
            ref float lithoPositionOutF,
            ref float lithoRotationOutF,
            ref float lithoVirtualRotationOutF);

        [DllImport(PLUGIN_NAME)]
        private static extern byte GetStatusFlags();

        private static readonly string[] OUTDATED_FIRMWARE_HASHES = {
            "4906f6590feafe8bdfcff964ae298a79d89a8cb2",
            "4178b9294820067f7dfb57e83f6d9a8393b6492c",
            "f1d05e1d0a8d3e6382121b9185cb8c514b4c1be4",
            "3846c3e1677e238512f3fbb8f0bebc7ba767855f",
            "d80f5d83bfb473d10f7222b545efc17e08cc7b4e",
            "23f7849d70e367ce58f4b8d9c9791874a3c6aca3",
            "222bf84544ccc27977f1be4820ea680a65ba1b82",
            "5358cd7ac305d5ec682c0bd20771c281adc58a04",
            "53a0efdb8e8c54be7d2be323b61f601a6f9f1c29"
        };

        // The temporal length that a touch may have before it switches classification from a 'tap'
        // to a 'long hold'
        public const float TAP_TIME = 0.3f;

        public const float EPSILON = 0.0001f;

        // Keys with which to look up stored values
        // Changing these will lose track of any previously stored values
        private const string PREF_KEY_HANDEDNESS = "litho_handedness";
        private const string PREF_KEY_GRIP_TYPE = "litho_grip_type";
        private const string PREF_KEY_VIEWER_TYPE = "litho_viewer_type";

        private const string PREF_KEY_FAVOURITE_DEVICE_NAME = "litho_favourite_device_name";

        private const string PREF_KEY_HAS_ONBOARDED = "litho_has_onboarded";


#if UNITY_EDITOR_OSX || WINDOWS_BLUETOOTH_ALPHA_OPTIN
        public const bool EDITOR_IS_SUPPORTED = true;
#else
        public const bool EDITOR_IS_SUPPORTED = false;
#endif

#if UNITY_STANDALONE_OSX || UNITY_IOS || UNITY_ANDROID || (UNITY_STANDALONE_WIN && WINDOWS_BLUETOOTH_ALPHA_OPTIN)
        public const bool PLATFORM_IS_SUPPORTED = true;
#else
        public const bool PLATFORM_IS_SUPPORTED = false;
#endif

#if UNITY_EDITOR
        public static readonly bool CAN_CONNECT = EDITOR_IS_SUPPORTED;
#else
        public static readonly bool CAN_CONNECT = PLATFORM_IS_SUPPORTED;
#endif


        private void Awake()
        {
            if (_camera == null)
            {
                _camera = FindObjectOfType<ARCameraManager>();
                if (_camera == null)
                {
                    Debug.LogWarning("Default MainCamera not found; "
                                     + "cannot update Litho position");
                }
            }
            if (_instance == null)
            {
                _instance = this;

                // Disable screen dimming
                Screen.sleepTimeout = SleepTimeout.NeverSleep;
#if UNITY_EDITOR
                AssemblyReloadEvents.beforeAssemblyReload += AttemptDeinitialization;
#endif
            }

            RestoreHandedness();
            RestoreGripType();
            RestoreViewerType();
        }

        private void Update()
        {
            if (this != _instance)
            {
                Debug.LogWarning("More than one Litho script instance was found in the scene; " +
                                 "destroying additional instance. This behaviour is intended " +
                                 "when switching between Litho scenes");
                Destroy(gameObject);
                return;
            }

            AttemptInitialization();

            if (_isInitialized)
            {
                if (_camera != null)
                {
                    UpdateTransform();
                }
                if (Handedness != _lastHandedness)
                {
                    if (IsConnected)
                    {
                        SetHandedness(Handedness != Handedness.Right);
                    }
                    OnHandednessChanged?.Invoke(Handedness, _lastHandedness);
                    _lastHandedness = Handedness;
                    SaveHandedness();
                }
                if (GripType != _lastGripType)
                {
                    if (IsConnected)
                    {
                        SetGripType((int)GripType);
                    }
                    OnGripTypeChanged?.Invoke(GripType, _lastGripType);
                    _lastGripType = GripType;
                    SaveGripType();
                }
                if (ViewerType != _lastViewerType)
                {
                    if (IsConnected)
                    {
                        SetViewerType((int)ViewerType);
                    }
                    OnViewerTypeChanged?.Invoke(ViewerType, _lastViewerType);
                    _lastViewerType = ViewerType;
                    SaveViewerType();
                }

                HandleEvents();
            }
        }

        private void OnDestroy()
        {
            AttemptDeinitialization();
        }

        private void AttemptInitialization()
        {
            if (_dllNotFound || _isInitialized || this != _instance)
            {
                return;
            }
#if !UNITY_EDITOR && UNITY_ANDROID
            // Only initialize after camera permission has been granted
            if (Permission.HasUserAuthorizedPermission(Permission.Camera))
            {
                // On Android builds, only initialize after location permission has been granted
                if (Permission.HasUserAuthorizedPermission(Permission.FineLocation))
                {
                        PerformInitialization();
                }
                else
                {
                    Permission.RequestUserPermission(Permission.FineLocation);
                }
            }
            else
            {
                Permission.RequestUserPermission(Permission.Camera);
            }
#else
            // Otherwise, just initialize immediately
            PerformInitialization();
#endif
#if WINDOWS_BLUETOOTH_ALPHA_OPTIN && UNITY_EDITOR_WIN
            Debug.LogWarning("You have opted in to test Windows Bluetooth support, which is currently in Alpha. You may experience missing functionality, crashes, or instability. Please refer to the documentation for more information. To disable this feature, comment-out the following line in Litho.cs: #define WINDOWS_BLUETOOTH_ALPHA_OPTIN", this);
#endif
        }

        private void PerformInitialization()
        {
            if (!_dllNotFound && !_isInitialized)
            {
                if (CAN_CONNECT)
                {
                    try
                    {
                        Initialize();
                        _isInitialized = true;
                    }
                    catch (DllNotFoundException)
                    {
                        _dllNotFound = true;
                        Debug.LogError("The Litho plugin was not found; the Android, iOS, Mac and Win " +
                                       "plugin folders should be located inside " +
                                       "Packages/Litho Beta SDK/Core/Plugins; please return the " +
                                       "plugin folders to the correct directory, or reinstall the " +
                                       "Litho SDK package.");
                    }
                    StartDeviceSearch();
                    // Take a guess at the ground height
                    //UpdateGroundHeight(GameObject.Find("Ground")?.transform.position.y ??
                                       //(_camera.transform.position.y - 1.4f), 0.1f);
                }
                else
                {
#if UNITY_EDITOR
                    _dllNotFound = true;
                    Debug.LogWarning("The current development platform does not support " +
                                     "Bluetooth connection to Litho devices; use the Litho " +
                                     "emulator instead.");
#else
                    _dllNotFound = true;
                    Debug.LogError("The current build platform does not support Bluetooth " +
                                   "connection to Litho devices; you will not be able to use " +
                                   "your Litho hardware.");
#endif
                }
            }
        }

        private void AttemptDeinitialization()
        {
            if (this == _instance)
            {
                if (_isInitialized)
                {
                    Deinitialize();
                }
                _instance = null;
            }
        }

        public static void StartDeviceSearch()
        {
            Debug.Log("Litho scan start");
            if (_instance == null || !_instance._isInitialized)
            {
                Debug.LogWarning((_instance == null
                                  ? "No Litho script is present in the current scene"
                                  : "The Litho instance has not been initialized")
                                 + "; cannot start scanning for devices");
                return;
            }
            StartScan();
        }

        public static void StopDeviceSearch()
        {
            if (_instance == null || !_instance._isInitialized)
            {
                Debug.LogWarning((_instance == null
                                  ? "No Litho script is present in the current scene"
                                  : "The Litho instance has not been initialized")
                                 + "; cannot stop scanning for devices");
                return;
            }
            StopScan();
        }

        public static void ConnectToDevice(string deviceName)
        {
            if (_instance == null || !_instance._isInitialized)
            {
                Debug.LogWarningFormat((_instance == null
                                        ? "No Litho script is present in the current scene"
                                        : "The Litho instance has not been initialized")
                                       + "; cannot connect to a device ({0})", deviceName);
                return;
            }
            ConnectTo(deviceName);
        }

        public static void DisconnectFromDevice()
        {
            if (_instance == null || !_instance._isInitialized)
            {
                Debug.LogWarning((_instance == null
                                  ? "No Litho script is present in the current scene"
                                  : "The Litho instance has not been initialized")
                                 + "; cannot disconnect from device");
                return;
            }
            Disconnect();
        }

        public void InvokeTouchStart(Vector2 touchPosition, Vector2 touchWorldPosition)
        {
            TouchStartTime = Time.unscaledTime;
            Touching = true;
            TouchPosition = touchPosition;
            TouchWorldPosition = touchWorldPosition;
            OnTouchStart?.Invoke(touchPosition, touchWorldPosition);
        }

        public void InvokeTouchHold(Vector2 touchPosition, Vector2 touchWorldPosition)
        {
            Touching = true;
            TouchPosition = touchPosition;
            TouchWorldPosition = touchWorldPosition;
            OnTouchHold?.Invoke(touchPosition, touchWorldPosition);
        }

        public void InvokeTouchEnd(Vector2 touchPosition, Vector2 touchWorldPosition)
        {
            TouchPosition = touchPosition;
            TouchWorldPosition = touchWorldPosition;
            OnTouchEnd?.Invoke(touchPosition, touchWorldPosition);
            Touching = false;
        }

        public void InvokeOnDeviceFound(string deviceName)
        {
            OnDeviceFound?.Invoke(deviceName);
        }

        public void InvokeOnConnected(string deviceName)
        {
            // Flag that a connection is currently established
            IsConnected = true;
            // Ensure plugin handedness and grip type are up to date with the Unity records
            SetHandedness(Handedness != Handedness.Right);
            SetGripType((int)GripType);
            SetViewerType((int)ViewerType);
            // Make an initial approximate calibration
            StartCoroutine(CalibrateSoon());
            // Play a haptic effect as notifcation of connection
            PlayHapticEffect(HapticEffect.Type.PulsingSharp_1_100);
            // Get device info
            GetDeviceInfo(deviceName);
            // Schedule battery level updates
            _batteryLevelCoroutine = StartCoroutine(GetBatteryLevel());
            _statusFlagsCoroutine = StartCoroutine(CoroutineGetStatusFlags());

            OnConnected?.Invoke(deviceName);
        }

        public void InvokeOnDisconnected(string deviceName)
        {
            // Cancel scheduled battery updates
            if (_batteryLevelCoroutine != null)
            {
                StopCoroutine(_batteryLevelCoroutine);
            }

            if (_statusFlagsCoroutine != null)
            {
                StopCoroutine(_statusFlagsCoroutine);
            }

            IsConnected = false;
            // Reset accuracy flags
            AccelAccuracy = TrackingAccuracy.Max;
            GyroAccuracy = TrackingAccuracy.Max;
            MagAccuracy = TrackingAccuracy.Max;
            IsCharging = false;

            OnDisconnected?.Invoke(deviceName);
        }

        public void InvokeOnConnectionFailed(string deviceName)
        {
            OnConnectionFailed?.Invoke(deviceName);
        }

        private void HandleEvents()
        {
            if (!_isInitialized || this != _instance)
            {
                return;
            }
            StringBuilder data = new StringBuilder(255);
            float[] info = { 0, 0, 0, 0 };
            int info2 = 0;

            EventCode status;
            while ((status = (EventCode)GetNextEvent(data, data.Capacity, ref info[0], ref info2))
                   != EventCode.None)
            {
                string str = data.ToString();
                switch (status)
                {
                    case EventCode.DeviceFound:
                        InvokeOnDeviceFound(str);
                        break;

                    case EventCode.ConnectionSuccess:
                        InvokeOnConnected(str);
                        break;

                    case EventCode.ConnectionFail:
                        InvokeOnConnectionFailed(str);
                        break;

                    case EventCode.Disconnect:
                        InvokeOnDisconnected(str);
                        break;

                    case EventCode.TouchStart:
                        InvokeTouchStart(new Vector2(info[0], info[1]),
                                         new Vector2(info[2], info[3]));
                        break;

                    case EventCode.TouchEnd:
                        InvokeTouchEnd(new Vector2(info[0], info[1]),
                                       new Vector2(info[2], info[3]));
                        break;

                    case EventCode.TouchHold:
                        InvokeTouchHold(new Vector2(info[0], info[1]),
                                        new Vector2(info[2], info[3]));
                        break;

                    case EventCode.BatteryStatus:
                        OnBatteryLevelReceived?.Invoke(info2);
                        break;

                    default:
                        break;
                }
            }
        }

        private void UpdateTransform()
        {
            if (IsConnected && transform.lossyScale.y > 0f)
            {
                float[] cameraPositionF = { 0, 0, 0 };
                float[] cameraOrientationF = { 0, 0, 0, 0 };
                if (_camera != null)
                {
                    cameraPositionF = VectorToFloatArray(_camera.transform.localPosition);
                    cameraOrientationF = QuatToFloatArray(_camera.transform.localRotation);
                }

                float[] lithoPositionOutF = VectorToFloatArray(Vector3.zero);
                float[] lithoRotationOutF = QuatToFloatArray(Quaternion.identity);
                float[] lithoVirtualRotationOutF = QuatToFloatArray(Quaternion.identity);

                GetLithoPosition(
                    ref cameraPositionF[0],
                    ref cameraOrientationF[0],
                    ref lithoPositionOutF[0],
                    ref lithoRotationOutF[0],
                    ref lithoVirtualRotationOutF[0]);

                transform.localPosition = FloatArrayToVector(lithoPositionOutF);
                transform.localRotation = FloatArrayToQuat(lithoVirtualRotationOutF);
            }
            else if (_camera != null)
            {
                transform.position = _camera.transform.TransformPoint(new Vector3(
                    Handedness == Handedness.Right ? 0.5f : -0.5f, -1f));
            }
        }

        [ContextMenu("Calibrate")]
        public static void Calibrate()
        {
            if (_instance == null || !_instance._isInitialized || !IsConnected || Camera == null)
            {
                Debug.LogWarning((_instance == null
                                  ? "No Litho script is present in the current scene"
                                  : (!_instance._isInitialized
                                     ? "The Litho instance has not been initialized"
                                     : (!IsConnected
                                        ? "There is no Litho device connected"
                                        : "Camera not found")))
                                 + "; cannot calibrate device");
                return;
            }
            Calibrate(ref VectorToFloatArray(Camera.transform.localPosition)[0],
                      ref QuatToFloatArray(Camera.transform.localRotation)[0]);
        }

        private IEnumerator CalibrateSoon()
        {
            // Wait for the end of this frame to finish the Awake/ Start frame
            yield return new WaitForEndOfFrame();
            // Wait for the end of the next frame to allow Litho to update the transform used for
            // comparison in the calibration process
            yield return new WaitForEndOfFrame();
            Calibrate();
        }

        public static void UpdateGroundHeight(float groundHeight, float certainty = 1f)
        {
            if (_instance == null || !_instance._isInitialized)
            {
                Debug.LogWarning((_instance == null
                                  ? "No Litho script is present in the current scene"
                                  : "The Litho instance has not been initialized")
                                 + "; cannot update ground height");
                return;
            }
            CalibrateGround(groundHeight, certainty);
        }

        public static void PlayHapticEffect(HapticEffect.Type type)
        {
            PlayHapticEffects(new HapticEffect.Type[] { type });
        }
        public static void PlayHapticEffects(List<HapticEffect.Type> types)
        {
            PlayHapticEffects(types.ToArray());
        }
        public static void PlayHapticEffects(HapticEffect.Type[] types)
        {
            if (_instance == null || !_instance._isInitialized)
            {
                Debug.LogWarning((_instance == null
                                  ? "No Litho script is present in the current scene"
                                  : "The Litho instance has not been initialized")
                                 + "; cannot play haptic effect");
                return;
            }
            if (IsConnected)
            {
                byte count = (byte)Math.Min(types.Length, byte.MaxValue);
                byte[] bytes = new byte[count];
                for (int t = 0; t < count; t++)
                {
                    bytes[t] = (byte)types[t];
                }
                SendHapticEvents((byte)bytes.Length, ref bytes[0]);
            }
        }

        private IEnumerator GetBatteryLevel()
        {
            while (true)
            {
                if (IsConnected)
                {
                    RequestBatteryStatus();
                }
                yield return new WaitForSeconds(_batteryUpdatePeriod);
            }
        }

        private IEnumerator CoroutineGetStatusFlags()
        {
            while (true)
            {
                if (IsConnected)
                {
                    byte statusFlags = GetStatusFlags();
                    AccelAccuracy = (TrackingAccuracy)(statusFlags & 0b11);
                    GyroAccuracy = (TrackingAccuracy)((statusFlags & 0b1100) >> 2);
                    MagAccuracy = (TrackingAccuracy)((statusFlags & 0b110000) >> 4);
                    IsCharging = (statusFlags & (1 << 6)) != 0;
                }
                yield return new WaitForSeconds(_statusFlagUpdatePeriod);
            }
        }

        private void GetDeviceInfo(string deviceName = "")
        {
            if (IsConnected)
            {
                string firmwareVersion = GetFirmwareVersion();
                ValidateFirmwareVersion(deviceName, firmwareVersion);
                OnFirmwareVersionReceived?.Invoke(firmwareVersion);
                OnHardwareVersionReceived?.Invoke(GetHardwareVersion());
                OnModelNumberReceived?.Invoke(GetModelNumber());
                Debug.Log("Firmware: " + GetFirmwareVersion());
                Debug.Log("Hardware: " + GetHardwareVersion());
                Debug.Log("Model: " + GetModelNumber());
            }
        }

        private void SaveHandedness()
        {
            PlayerPrefs.SetInt(PREF_KEY_HANDEDNESS, (int)Handedness);
        }
        private void RestoreHandedness()
        {
            // Attempt to recover a stored value
            int restoredHandedness = PlayerPrefs.GetInt(PREF_KEY_HANDEDNESS, -1);
            if (restoredHandedness >= 0 && restoredHandedness <= (int)Handedness.Left)
            {
                Handedness = (Handedness)restoredHandedness;
            }
        }

        private void SaveGripType()
        {
            PlayerPrefs.SetInt(PREF_KEY_GRIP_TYPE, (int)GripType);
        }
        private void RestoreGripType()
        {
            // Attempt to recover a stored value
            int restoredGripType = PlayerPrefs.GetInt(PREF_KEY_GRIP_TYPE, -1);
            if (restoredGripType >= 0 && restoredGripType <= (int)GripType.Clutch)
            {
                GripType = (GripType)restoredGripType;
            }
        }

        private void SaveViewerType()
        {
            PlayerPrefs.SetInt(PREF_KEY_VIEWER_TYPE, (int)ViewerType);
        }
        private void RestoreViewerType()
        {
            // Attempt to recover a stored value
            int restoredViewerType = PlayerPrefs.GetInt(PREF_KEY_VIEWER_TYPE, -1);
            if (restoredViewerType >= 0 && restoredViewerType <= (int)ViewerType.Headset)
            {
                ViewerType = (ViewerType)restoredViewerType;
            }
        }

        public static string GetFavouriteDeviceName()
        {
            return PlayerPrefs.GetString(PREF_KEY_FAVOURITE_DEVICE_NAME, "");
        }

        public static void SaveFavouriteDeviceName(string newFavouriteDeviceName)
        {
            string oldFavouriteDeviceName = GetFavouriteDeviceName();
            PlayerPrefs.SetString(PREF_KEY_FAVOURITE_DEVICE_NAME, newFavouriteDeviceName);
            if (oldFavouriteDeviceName != newFavouriteDeviceName)
            {
                OnFavouriteDeviceChanged?.Invoke(newFavouriteDeviceName, oldFavouriteDeviceName);
            }
        }

        public static bool GetHasOnboarded()
        {
            return PlayerPrefs.GetInt(PREF_KEY_HAS_ONBOARDED, 0) > 0;
        }

        public static void SaveHasOnboarded(bool hasOnboarded)
        {
            PlayerPrefs.SetInt(PREF_KEY_HAS_ONBOARDED, hasOnboarded ? 1 : 0);
        }

        public static void ToggleHandedness()
        {
            if (_instance == null)
            {
                Debug.LogWarning("No Litho script is present in the current scene; "
                                 + "cannot toggle handedness");
                return;
            }
            Handedness = (Handedness)(((int)Handedness + 1)
                                      % Enum.GetNames(typeof(Handedness)).Length);
        }

        public static void ToggleGripType()
        {
            if (_instance == null)
            {
                Debug.LogWarning("No Litho script is present in the current scene; "
                                 + "cannot toggle grip type");
                return;
            }
            GripType = (GripType)(((int)GripType + 1) % Enum.GetNames(typeof(GripType)).Length);
        }

        public static void ToggleViewerType()
        {
            if (_instance == null)
            {
                Debug.LogWarning("No Litho script is present in the current scene; "
                                 + "cannot toggle viewer type");
                return;
            }
            ViewerType = (ViewerType)(((int)ViewerType + 1)
                                      % Enum.GetNames(typeof(ViewerType)).Length);
        }

        private static bool ValidateFirmwareVersion(string deviceName, string firmwareVersion)
        {
            foreach (string hash in OUTDATED_FIRMWARE_HASHES)
            {
                if (firmwareVersion == hash)
                {
                    HasFatalError = true;
                    if (OnFatalError != null)
                    {
                        OnFatalError.Invoke(
                            "The firmware version on your Litho device (" + deviceName + ") is " +
                            "out of date; you must download the Litho Companion app from the " +
#if UNITY_ANDROID
                            "Google Play Store" +
#else
                            "App Store" +
#endif
                            " in order to upgrade your Litho firmware to the latest version.\n\n" +
                            "Your device is running firmware with hash code:\n" + firmwareVersion);
                    }
                }
            }


            return false;
        }


        private static float[] VectorToFloatArray(Vector3 vector)
        {
            // Convert from Unity vector format to plugin format
            // Unity: x, y, z = Right, Up, Forward (left-handed)
            // Plugin: x, y, z =  Forward, Left, Up (right-handed)
            float xUnity = vector.x;
            float yUnity = vector.y;
            float zUnity = vector.z;
            float xPlugin = zUnity;
            float yPlugin = -xUnity;
            float zPlugin = yUnity;
            return new float[3]
                   {
                   xPlugin,
                   yPlugin,
                   zPlugin
                   };
        }

        private static float[] QuatToFloatArray(Quaternion quat)
        {
            // Convert from Unity quaternion format to plugin format
            // (additionally note the change of component order)
            // Unity: x, y, z, w = Right, Up, Forward, W (left-handed)
            // Plugin: w, x, y, z =  W, Forward, Left, Up (right-handed)
            // (additionally note negation of w to preserve direction of rotation)
            float wUnity = quat.w;
            float xUnity = quat.x;
            float yUnity = quat.y;
            float zUnity = quat.z;
            float wPlugin = -wUnity;
            float xPlugin = zUnity;
            float yPlugin = -xUnity;
            float zPlugin = yUnity;
            return new float[4]
                   {
                   wPlugin,
                   xPlugin,
                   yPlugin,
                   zPlugin
                   };
        }

        private static Vector3 FloatArrayToVector(float[] floatArray)
        {
            if (floatArray.Length == 3)
            {
                // Convert from plugin vector format to Unity format
                // Plugin: x, y, z =  Forward, Left, Up (right-handed)
                // Unity: x, y, z = Right, Up, Forward (left-handed)
                float xPlugin = floatArray[0];
                float yPlugin = floatArray[1];
                float zPlugin = floatArray[2];
                float xUnity = -yPlugin;
                float yUnity = zPlugin;
                float zUnity = xPlugin;
                return new Vector3(xUnity, yUnity, zUnity);
            }
            else
            {
                Debug.LogWarning("Float array does not have 3 elements;" +
                                 " cannot convert it into a Vector3");
                return Vector3.zero;
            }
        }

        private static Quaternion FloatArrayToQuat(float[] floatArray)
        {
            if (floatArray.Length == 4)
            {
                // Convert from plugin quaternion format to Unity format
                // (additionally note the change of component order)
                // Plugin: w, x, y, z =  W, Forward, Left, Up (right-handed)
                // Unity: x, y, z, w = Right, Up, Forward, W (left-handed)
                // (additionally note negation of w to preserve direction of rotation)
                float wPlugin = floatArray[0];
                float xPlugin = floatArray[1];
                float yPlugin = floatArray[2];
                float zPlugin = floatArray[3];
                float wUnity = -wPlugin;
                float xUnity = -yPlugin;
                float yUnity = zPlugin;
                float zUnity = xPlugin;
                return new Quaternion(xUnity, yUnity, zUnity, wUnity);
            }
            else
            {
                Debug.LogWarning("Float array does not have 4 elements;" +
                                 " cannot convert it into a Quaternion");
                return Quaternion.identity;
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (_camera != null)
            {
                // Draw the phone position
                Gizmos.matrix = _camera.transform.localToWorldMatrix;
                Gizmos.color = new Color(0.2f, 0.2f, 0.2f, 0.75f);
                Gizmos.DrawCube(Vector3.up * -0.025f, new Vector3(0.05f, 0.1f, 0.005f));
                Gizmos.matrix = Matrix4x4.identity;

                // Draw the camera transform
                DrawTransformGizmo(_camera.transform);
            }

            // Draw our transform
            DrawTransformGizmo(transform);
        }

        public static void DrawTransformGizmo(Transform target)
        {
            Color preColour = Gizmos.color;
            Matrix4x4 oldMatrix = Gizmos.matrix;

            Gizmos.matrix = target.localToWorldMatrix;
            if (target.localScale.x > 0)
            {
                Gizmos.color = new Color(1f, 0f, 0f, 0.75f);
                Gizmos.DrawLine(Vector3.zero, Vector3.right * 0.1f / target.localScale.x);
            }
            if (target.localScale.y > 0)
            {
                Gizmos.color = new Color(0f, 1f, 0f, 0.75f);
                Gizmos.DrawLine(Vector3.zero, Vector3.up * 0.1f / target.localScale.y);
            }
            if (target.localScale.z > 0)
            {
                Gizmos.color = new Color(0f, 0f, 1f, 0.75f);
                Gizmos.DrawLine(Vector3.zero, Vector3.forward * 0.1f / target.localScale.z);
            }

            Gizmos.matrix = oldMatrix;
            Gizmos.color = preColour;
        }

        /// <summary>
        /// Add a menu item for calibration to a Unity editor menu
        /// </summary>
        [MenuItem("LITHO/Get Info", false, 200)]
        private static void GetInfoViaMenu()
        {
            if (_instance != null)
            {
                _instance.GetDeviceInfo();
            }
        }

        /// <summary>
        /// Validates whether the menu item for get info should be enabled
        /// </summary>
        [MenuItem("LITHO/Get Info", true, 200)]
        private static bool GetInfoViaMenuValidate()
        {
            return Application.isPlaying
                && _instance != null
                && _instance._isInitialized
                && IsConnected;
        }

        /// <summary>
        /// Add a menu item for calibration to a Unity editor menu
        /// </summary>
        [MenuItem("LITHO/Calibrate", false, 201)]
        private static void CalibrateViaMenu()
        {
            Calibrate();
        }

        /// <summary>
        /// Validates whether the menu item for calibration should be enabled
        /// </summary>
        [MenuItem("LITHO/Calibrate", true, 201)]
        private static bool CalibrateViaMenuValidate()
        {
            return Application.isPlaying
                && _instance != null
                && _instance._isInitialized
                && IsConnected;
        }

        [ContextMenu("Calibrate", true)]
        private bool CalibrateViaContextMenuValidate()
        {
            return Application.isPlaying
                && _instance != null
                && _instance._isInitialized;
        }
#endif
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(Litho))]
    [CanEditMultipleObjects]
    public class LithoEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            EditorGUI.BeginDisabledGroup(!Application.isPlaying);

            if (!Litho.Touching)
            {
                if (GUILayout.Button("Simulate Start Touch"))
                {
                    ((Litho)target).InvokeTouchStart(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
                }
            }
            else
            {
                if (GUILayout.Button("Simulate End Touch"))
                {
                    ((Litho)target).InvokeTouchEnd(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
                }
            }

            EditorGUI.EndDisabledGroup();
        }
    }
#endif

}
