/////////////////////////////////////////////////
// LITHO SDK                                   //
// Copyright © 2020 Purple Tambourine Ltd.     //
// License: see LICENSE in package root folder //
/////////////////////////////////////////////////
using UnityEngine;
using UnityEditor;

namespace LITHO.Demo
{

    /// <summary>
    /// Illustrates how Litho global events work; use this script for understanding and debugging Litho
    /// global, and feel free to delete it if it is not needed
    /// </summary>
    [AddComponentMenu("LITHO/Demo/Litho Global Event Logger", -9100)]
    public class LithoGlobalEventLogger : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Whether events should be printed to the Unity Console")]
        private bool _logEvents = true;

        private static bool _instanceExists;


        private void Awake()
        {
            if (_instanceExists)
            {
                Destroy(this);
            }
            _instanceExists = true;

            Litho.OnConnected += HandleLithoConnected;
            Litho.OnConnectionFailed += HandleLithoConnectionFailed;
            Litho.OnDisconnected += HandleLithoDisconnected;

            Litho.OnHandednessChanged += HandleLithoHandednessChanged;
            Litho.OnGripTypeChanged += HandleLithoGripTypeChanged;

            Litho.OnTouchStart += HandleLithoTouchStart;
            Litho.OnTouchHold += HandleLithoTouchMove;
            Litho.OnTouchEnd += HandleLithoTouchEnd;
        }

        private void OnDestroy()
        {
            Litho.OnConnected -= HandleLithoConnected;
            Litho.OnConnectionFailed -= HandleLithoConnectionFailed;
            Litho.OnDisconnected -= HandleLithoDisconnected;

            Litho.OnHandednessChanged -= HandleLithoHandednessChanged;
            Litho.OnGripTypeChanged -= HandleLithoGripTypeChanged;

            Litho.OnTouchStart -= HandleLithoTouchStart;
            Litho.OnTouchHold -= HandleLithoTouchMove;
            Litho.OnTouchEnd -= HandleLithoTouchEnd;
        }

        private void HandleLithoConnected(string deviceName)
        {
            if (_logEvents)
            {
                Debug.LogFormat("{0}: Connected to {1}", this, deviceName);
            }
        }

        private void HandleLithoConnectionFailed(string deviceName)
        {
            if (_logEvents)
            {
                Debug.LogFormat("{0}: Failed to connect to {1}", this, deviceName);
            }
        }

        private void HandleLithoDisconnected(string deviceName)
        {
            if (_logEvents)
            {
                Debug.LogFormat("{0}: Disconnected from {1}", this, deviceName);
            }
        }

        private void HandleLithoHandednessChanged(Handedness newHandedness, Handedness oldHandedness)
        {
            if (_logEvents)
            {
                Debug.LogFormat("{0}: Handedness changed from {1} to {2}",
                                this, oldHandedness, newHandedness);
            }
        }

        private void HandleLithoGripTypeChanged(GripType newGripType, GripType oldGripType)
        {
            if (_logEvents)
            {

                Debug.LogFormat("{0}: Grip type changed from {1} to {2}",
                                this, oldGripType, newGripType);
            }
        }

        private void HandleLithoTouchStart(Vector2 position, Vector2 worldPosition)
        {
            if (_logEvents)
            {
                Debug.LogFormat("{0}: Touch started at position ({1}, {2})",
                                this, worldPosition.x, worldPosition.y);
            }
        }
        private void HandleLithoTouchMove(Vector2 position, Vector2 worldPosition)
        {
            if (_logEvents)
            {
                Debug.LogFormat("{0}: Touch holding at position ({1}, {2})",
                                this, worldPosition.x, worldPosition.y);
            }
        }

        private void HandleLithoTouchEnd(Vector2 position, Vector2 worldPosition)
        {
            if (_logEvents)
            {
                Debug.LogFormat("{0}: Touch ended at position ({1}, {2})",
                                this, worldPosition.x, worldPosition.y);
            }
        }
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(LithoGlobalEventLogger))]
    public class LithoGlobalEventLoggerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            EditorGUILayout.HelpBox("This component exists only to illustrate how global Litho " +
                                    "events work. You can disable logging of these events using the " +
                                    "option above, or more permanently by deleting this " +
                                    "LithoGlobalEventLogger component or this entire GameObject.",
                                    MessageType.Info);
        }
    }
#endif

}
