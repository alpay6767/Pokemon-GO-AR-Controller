/////////////////////////////////////////////////
// LITHO SDK                                   //
// Copyright © 2020 Purple Tambourine Ltd.     //
// License: see LICENSE in package root folder //
/////////////////////////////////////////////////

using UnityEngine;

namespace LITHO
{

    /// <summary>
    /// Handles manipulation of the position and rotation of the target object whilst it is being
    /// interacted with by a Manipulator
    /// </summary>
    [AddComponentMenu("LITHO/Manipulables/Movable", -9777)]
    public class Movable : Positionable
    {
        [SerializeField]
        [Tooltip("Whether this can be rotated by a Pointer")]
        private bool _rotationIsControlled = true;
        public bool RotationIsControlled { get { return _rotationIsControlled; } }

        [SerializeField]
        [Tooltip("Whether to manipulate this object relative to initial yaw; otherwise align " +
                 "with the Manipulator orientation")]
        private bool _preserveYaw = true;

        [SerializeField]
        [Tooltip("The preferred neutral rotation about Euler each axis")]
        private Vector3 _neutralRotationEulers = Vector3.zero;
        public Vector3 NeutralRotationEulers { get { return _neutralRotationEulers; } }

        private Quaternion _releaseAngularDelta;

        protected const float BASE_ROTATE_TORQUE = 0.2f;


        protected override bool TryGetValidRigidbody()
        {
            if (base.TryGetValidRigidbody())
            {
                if ((_rb.constraints & RigidbodyConstraints.FreezeRotationY) > 0)
                {
                    Debug.LogWarning("Rigidbody is rotationally constrained, but has a " +
                                     this.GetType().Name + "component; uncheck Rigidbody " +
                                     "constraints on Y rotation to use this " +
                                     this.GetType().Name + " component.");
                }
                return true;
            }
            return false;
        }

        public override Vector3 GetOffsetEulers(Manipulator manipulator)
        {
            return _preserveYaw
                ? Target.eulerAngles - manipulator.transform.eulerAngles : Vector3.zero;
        }

        public override void InitializeManipulation(Manipulation manipulation)
        {
            base.InitializeManipulation(manipulation);

            _releaseAngularDelta = Quaternion.AngleAxis(_rb.angularVelocity.magnitude,
                                                        _rb.angularVelocity.normalized);
        }

        public override void PerformManipulation(Manipulation manipulation)
        {
            base.PerformManipulation(manipulation);

            if (RotationIsControlled)
            {
                UpdateRotation(manipulation);
            }
        }

        public override void FinalizeManipulation(Manipulation manipulation)
        {
            base.FinalizeManipulation(manipulation);

            if (RotationIsControlled)
            {
                if (_rb != null)
                {
                    _releaseAngularDelta.ToAngleAxis(
                        out float angularVelocity, out Vector3 angularVelocityAxis);
                    _rb.angularVelocity = transform.rotation * angularVelocityAxis
                        * angularVelocity * Mathf.Deg2Rad / Time.fixedDeltaTime;
                }
                _releaseAngularDelta = Quaternion.identity;
            }
        }

        private void UpdateRotation(Manipulation manipulation)
        {
            if (manipulation.Manipulator.Strength <= 0)
            {
                return;
            }
            if (Target != null)
            {
                Quaternion oldRotation = Target.rotation;
                // Directly rotate to neutralize the orientation of the object
                Target.rotation = Quaternion.Slerp(
                    Target.rotation,
                    manipulation.Manipulator.GetTargetRotationOffset()
                        * Quaternion.Euler(NeutralRotationEulers),
                    BASE_ROTATE_TORQUE * manipulation.Manipulator.Strength);

                // Gradually apply an estimate for angular velocity
                Quaternion rotationDelta = Quaternion.Inverse(oldRotation) * Target.rotation;
                _releaseAngularDelta = Quaternion.Slerp(_releaseAngularDelta, rotationDelta, 0.05f);
            }
            if (_rb != null)
            {
                // Apply damping to the angular velocity of the object
                _rb.angularVelocity *= 1f - _baseDampingFactor * 2f * BASE_ROTATE_TORQUE
                    * manipulation.Manipulator.Strength;
            }
        }
    }

}
