/////////////////////////////////////////////////
// LITHO SDK                                   //
// Copyright © 2020 Purple Tambourine Ltd.     //
// License: see LICENSE in package root folder //
/////////////////////////////////////////////////

using UnityEngine;
using UnityEditor;

namespace LITHO
{

    /// <summary>
    /// Displays the given prefab around this object when any Manipulables on it or its children
    /// are hovered
    /// </summary>
    [AddComponentMenu("LITHO/Indicators/Prefab Indicator", -9700)]
    public class PrefabIndicator : ManipulationIndicator
    {
        public enum TargetingMode
        {
            FollowObject = 0,
            FollowManipulatorGrabPosition,
            FollowManipulatorTargetGrabPosition
        }
        public enum RotationMode
        {
            MatchObject = 0,
            MatchManipulator,
            MatchManipulatorYaw,
        }

        [SerializeField]
        [Tooltip("Prefab to create as a 'clamp' around this object")]
        private GameObject _indicatorPrefab = null;

        [SerializeField]
        [Tooltip("How to place the prefab")]
        private TargetingMode _targetingMode = TargetingMode.FollowObject;

        [SerializeField]
        [Tooltip("How to orient the prefab")]
        private RotationMode _rotationMode = RotationMode.MatchObject;

        [SerializeField]
        [Tooltip("Whether to scale the indicator proprtionally with this object")]
        private bool _scaleWithObject = true;

        [SerializeField, HideInInspector]
        protected GameObject _indicator;

        private Manipulator _manipulator;


        protected virtual void LateUpdate()
        {
            UpdateIndicatorTranform(_manipulator);
        }

        protected override void ProcessManipulatorEnter(Manipulation manipulation)
        {
            _manipulator = manipulation.Manipulator;
            if (_indicatorPrefab != null)
            {
                _indicator = Instantiate(_indicatorPrefab);
                // Include the name of this object in the name of the indicator
                _indicator.name = _indicator.name.Replace("Clone", name);
            }
            else
            {
                Debug.LogWarning("PrefabIndicator indicator prefab not assigned; assign a " +
                                 "prefab using the Inspector window", this);
            }
        }

        protected override void ProcessManipulatorExit(Manipulation manipulation)
        {
            if (_indicator != null)
            {
                Destroy(_indicator);
            }
        }

        protected override void ProcessManipulatorGrab(Manipulation manipulation)
        {
            _manipulator = manipulation?.Manipulator;
        }

        protected override void ProcessManipulatorRelease(Manipulation manipulation)
        {
            _manipulator = manipulation?.Manipulator;
        }

        protected virtual void UpdateIndicatorTranform(Manipulator manipulator)
        {
            if (_indicator != null && _indicator.gameObject.activeSelf)
            {
                switch (_targetingMode)
                {
                    case TargetingMode.FollowObject:
                        _indicator.transform.position = transform.position;
                        break;
                    case TargetingMode.FollowManipulatorGrabPosition:
                        if (manipulator != null)
                        {
                            _indicator.transform.position = manipulator.GrabPosition;
                        }
                        break;
                    case TargetingMode.FollowManipulatorTargetGrabPosition:
                        if (manipulator != null)
                        {
                            _indicator.transform.position = manipulator.TargetGrabPosition;
                        }
                        break;
                }

                switch (_rotationMode)
                {
                    case RotationMode.MatchObject:
                        _indicator.transform.rotation = transform.rotation;
                        break;
                    case RotationMode.MatchManipulator:
                        if (manipulator != null)
                        {
                            _indicator.transform.rotation = manipulator.transform.rotation;
                        }
                        break;
                    case RotationMode.MatchManipulatorYaw:
                        if (manipulator != null)
                        {
                            _indicator.transform.eulerAngles = new Vector3(
                                0f, manipulator.transform.eulerAngles.y, 0f);
                        }
                        break;
                }

                _indicator.transform.localScale = Vector3.Scale(
                    _scaleWithObject ? transform.lossyScale : Vector3.one,
                    _indicatorPrefab != null
                        ? _indicatorPrefab.transform.localScale : Vector3.one);
            }
        }
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(PrefabIndicator))]
    [CanEditMultipleObjects]
    public class PrefabIndicatorEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            SerializedProperty indicatorProperty = serializedObject.FindProperty("_indicator");
            if (Application.isPlaying && indicatorProperty.objectReferenceValue != null)
            {
                GUI.enabled = false;
                EditorGUILayout.PropertyField(indicatorProperty, new GUIContent("Indicator"));
                GUI.enabled = true;
            }
        }
    }
#endif

}
