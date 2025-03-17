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
    /// Implements a LaserManipulator such that it can be easily controlled by the Unity Editor, or
    /// by other scripts via its exposed methods and properties
    /// </summary>
    [AddComponentMenu("LITHO/Manipulators/Virtual Pointer", -9899)]
    [RequireComponent(typeof(LineRenderer))]
    public class VirtualPointer : LaserManipulator
    {
        [Header("Virtual Pointer Properties")]

        [SerializeField]
        [Tooltip("Current depth of the manipulated object as a multiple of its initial depth")]
        [Range(0, 10f)]
        protected float _depthFactor = 1f;

        [SerializeField]
        [Tooltip("Euler angles to offset the neutral rotation of the manipulated object by")]
        private Vector3 _offsetEulerAngles = Vector3.zero;

        protected override float DepthFactor
        {
            get
            {
                if (IsInteracting && CurrentManipulable.SupportsDepthControl)
                {
                    _depthFactor = 1f;
                }
                return _depthFactor;
            }
        }

        protected override Quaternion OffsetRotation
        {
            get
            {
                return Quaternion.Euler(_offsetEulerAngles);
            }
        }

        private bool _shouldStartTouch, _shouldEndTouch;


        protected virtual void Update()
        {
            // Process fake touches
            if (_shouldStartTouch)
            {
                if (!IsGrabbing)
                {
                    Grab();
                }
                _shouldStartTouch = false;
            }
            else if (_shouldEndTouch)
            {
                if (IsGrabbing)
                {
                    Release();
                }
                _shouldEndTouch = false;
            }
        }

        public override void Grab()
        {
            base.Grab();
            _shouldStartTouch = false;
            _depthFactor = 1f;
        }

        public override void Release(bool automateTap = true)
        {
            base.Release(automateTap);
            _shouldEndTouch = false;
        }

        public void StartTouch()
        {
            _shouldStartTouch = true;
        }

        public void EndTouch()
        {
            _shouldEndTouch = true;
        }
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(VirtualPointer))]
    [CanEditMultipleObjects]
    public class VirtualPointerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            EditorGUI.BeginDisabledGroup(!Application.isPlaying);

            if (!((VirtualPointer)target).IsGrabbingManipulable)
            {
                if (GUILayout.Button("Start Touch"))
                {
                    ((VirtualPointer)target).StartTouch();
                }
            }
            else
            {
                if (GUILayout.Button("End Touch"))
                {
                    ((VirtualPointer)target).EndTouch();
                }
            }

            EditorGUI.EndDisabledGroup();
        }
    }
#endif
}
