/////////////////////////////////////////////////
// LITHO SDK                                   //
// Copyright © 2020 Purple Tambourine Ltd.     //
// License: see LICENSE in package root folder //
/////////////////////////////////////////////////

using UnityEngine;

namespace LITHO
{

    /// <summary>
    /// Handles manipulation of the rotation of the target object whilst it is being interacted
    /// with by a Manipulator
    /// </summary>
    [AddComponentMenu("LITHO/Manipulables/Rotatable", -9775)]
    public class Rotatable : SliderManipulable
    {
        [SerializeField]
        [Tooltip("Axis about which the target object should be rotated")]
        private Vector3 _rotationAxis = Vector3.up;

        protected override Vector3 Axis => Vector3.right;

        protected override float Value
        {
            get
            {
                Target.localRotation.ToAngleAxis(out float angle, out Vector3 axis);
                return -angle * Vector3.Dot(axis, _rotationAxis.normalized);
            }
        }

        protected override float MinValue => _initialValue - 185f;

        protected override float MaxValue => _initialValue + 185f;

        protected Quaternion _initialRotation;


        public override void InitializeManipulation(Manipulation manipulation)
        {
            base.InitializeManipulation(manipulation);

            _initialRotation = Target.localRotation;
        }

        protected override void UpdateValue(float targetValue)
        {
            Target.localRotation = _initialRotation * Quaternion.Lerp(
                Quaternion.Inverse(_initialRotation) * Target.localRotation,
                Quaternion.AngleAxis(_initialValue - targetValue, _rotationAxis),
                0.25f);
        }
    }

}
