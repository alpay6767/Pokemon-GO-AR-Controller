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
    /// are hovered, then changes the prefab scale when a Manipulable is grabbed
    /// </summary>
    [AddComponentMenu("LITHO/Indicators/Scaled Prefab Indicator", -9699)]
    public class ScaledPrefabIndicator : PrefabIndicator
    {
        [SerializeField]
        [Tooltip("Scale factor of the prefab when hovering this object")]
        private float _hoverScaleFactor = 1.25f;

        [SerializeField]
        [Tooltip("Scale factor of the prefab when grabbing this object")]
        private float _grabScaleFactor = 1f;

        [SerializeField]
        [Tooltip("How fast to exponentially change scale the icon")]
        private float _exponentialScaleRate = 0.5f;

        [SerializeField]
        [Tooltip("Whether to scale up from the center of the object on hover, or simply appear " +
                 "instantly")]
        private bool _scaleInFromCentre = false;

        private float _scaleFactor, _targetScaleFactor;


        protected override void LateUpdate()
        {
            _scaleFactor = _scaleFactor * (1f - _exponentialScaleRate)
                + _targetScaleFactor * _exponentialScaleRate;

            base.LateUpdate();
        }

        protected override void ProcessManipulatorEnter(Manipulation manipulation)
        {
            _scaleFactor = _scaleInFromCentre ? 0f : _hoverScaleFactor;
            _targetScaleFactor = _hoverScaleFactor;

            base.ProcessManipulatorEnter(manipulation);
        }

        protected override void ProcessManipulatorGrab(Manipulation manipulation)
        {
            base.ProcessManipulatorGrab(manipulation);
            _targetScaleFactor = _grabScaleFactor;
        }

        protected override void ProcessManipulatorRelease(Manipulation manipulation)
        {
            base.ProcessManipulatorRelease(manipulation);
            _targetScaleFactor = _hoverScaleFactor;
        }

        protected override void UpdateIndicatorTranform(Manipulator manipulator)
        {
            base.UpdateIndicatorTranform(manipulator);
            if (_indicator != null)
            {
                _indicator.transform.localScale *= _scaleFactor;
            }
        }
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(ScaledPrefabIndicator))]
    [CanEditMultipleObjects]
    public class ScaledPrefabIndicatorEditor : Editor
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
