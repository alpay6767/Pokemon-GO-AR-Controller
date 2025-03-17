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
    /// Illustrates how Litho Manipulable-specific events work; use this script for understanding
    /// and debugging Manipulable object events, and feel free to delete it if it is not needed
    /// </summary>
    [AddComponentMenu("LITHO/Demo/Manipulable Event Logger", -9099)]
    [RequireComponent(typeof(Manipulable))]
    public class ManipulableEventLogger : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Whether events should be printed to the Unity Console")]
        private bool _logEvents = true;

        private Manipulable _Manipulable;

        private void Awake()
        {
            _Manipulable = GetComponent<Manipulable>();

            _Manipulable.OnManipulatorEnter += HandleManipulatorEnter;
            _Manipulable.OnManipulatorExit += HandleManipulatorExit;
            _Manipulable.OnManipulatorGrab += HandleManipulatorGrab;
            _Manipulable.OnManipulatorHold += HandleManipulatorHold;
            _Manipulable.OnManipulatorRelease += HandleManipulatorRelease;
            _Manipulable.OnManipulatorTap += HandleManipulatorTap;
            _Manipulable.OnManipulatorLongHold += HandleManipulatorLongHold;
        }

        private void OnDestroy()
        {
            _Manipulable.OnManipulatorEnter -= HandleManipulatorEnter;
            _Manipulable.OnManipulatorExit -= HandleManipulatorExit;
            _Manipulable.OnManipulatorGrab -= HandleManipulatorGrab;
            _Manipulable.OnManipulatorHold -= HandleManipulatorHold;
            _Manipulable.OnManipulatorRelease -= HandleManipulatorRelease;
            _Manipulable.OnManipulatorTap -= HandleManipulatorTap;
            _Manipulable.OnManipulatorLongHold -= HandleManipulatorLongHold;
        }

        private void HandleManipulatorEnter(Manipulation manipulation)
        {
            if (_logEvents)
            {
                Debug.LogFormat("{0}: OnManipulatorEnter ({1})", this, manipulation.Manipulator);
            }
        }

        private void HandleManipulatorExit(Manipulation manipulation)
        {
            if (_logEvents)
            {
                Debug.LogFormat("{0}: OnManipulatorExit ({1})", this, manipulation.Manipulator);
            }
        }

        private void HandleManipulatorGrab(Manipulation manipulation)
        {
            if (_logEvents)
            {
                Debug.LogFormat("{0}: OnManipulatorDown ({1})", this, manipulation.Manipulator);
            }
        }

        private void HandleManipulatorHold(Manipulation manipulation)
        {
            if (_logEvents)
            {
                Debug.LogFormat("{0}: OnManipulatorHold ({1})", this, manipulation.Manipulator);
            }
        }

        private void HandleManipulatorRelease(Manipulation manipulation)
        {
            if (_logEvents)
            {
                Debug.LogFormat("{0}: OnManipulatorUp ({1})", this, manipulation.Manipulator);
            }
        }

        private void HandleManipulatorTap(Manipulation manipulation)
        {
            if (_logEvents)
            {
                Debug.LogFormat("{0}: OnManipulatorTap ({1})", this, manipulation.Manipulator);
            }
        }

        private void HandleManipulatorLongHold(Manipulation manipulation)
        {
            if (_logEvents)
            {
                Debug.LogFormat("{0}: OnManipulatorLongHold ({1})",
                                this, manipulation.Manipulator);
            }
        }
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(ManipulableEventLogger))]
    [CanEditMultipleObjects]
    public class ManipulableEventLoggerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            EditorGUILayout.HelpBox("This component exists only to illustrate how " +
                                    "object-specific Litho Manipulable events work. You can " +
                                    "disable logging of these events using the option above, or " +
                                    "more permanently by deleting this ManipulableEventLogger " +
                                    "component.",
                                    MessageType.Info);
        }
    }
#endif

}
