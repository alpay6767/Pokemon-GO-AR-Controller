/////////////////////////////////////////////////
// LITHO SDK                                   //
// Copyright © 2020 Purple Tambourine Ltd.     //
// License: see LICENSE in package root folder //
/////////////////////////////////////////////////

using UnityEngine;
using UnityEngine.Events;

namespace LITHO
{

    /// <summary>
    /// Handles manipulation of the position of the target object, whilst ensuring smoothly
    /// pointing the target object in the direction of its velocity
    /// </summary>
    [AddComponentMenu("LITHO/Manipulables/Swooshable", -9773)]
    public class Swooshable : Positionable
    {
        [SerializeField]
        [Tooltip("The preferred neutral rotation about Euler each axis")]
        private Vector3 _neutralRotationEulers = Vector3.zero;
        public Vector3 NeutralRotationEulers { get { return _neutralRotationEulers; } }

        [SerializeField]
        [Tooltip("Speed above which the swooshing effect is activated")]
        private float _speedThreshold = 1f;

        [SerializeField]
        [Tooltip("Factor by which speed (above threshold) affects strength of swoosh effect")]
        private float _speedFactor = 7.5f;

        [SerializeField]
        [Tooltip("Whether to automatically make the Rigidbody kinematic when not manipulating")]
        private bool _autoLockAfterRelease = true;

        private bool _hasTriggeredAutoLock = false;

        [SerializeField]
        [Tooltip("Occurs after release once stationary and upright")]
        public UnityEvent OnStableUpright = new UnityEvent();

        private bool _hasTriggeredOnStableUpright = false;

        private Quaternion _releaseAngularDelta;

        private float _stabilityStartTime;

        protected const float STABILITY_EVALUATE_PERIOD = 0.1f;

        protected const float BASE_ROTATE_TORQUE = 0.1f;


        protected override void FixedUpdate()
        {
            base.FixedUpdate();

            // If approximately stationary and upright
            if (_rb != null
                && _rb.velocity.sqrMagnitude + _rb.angularVelocity.sqrMagnitude < 0.01f
                && Vector3.Angle(transform.up, Vector3.up) < 1f)
            {
                // If this object has been stable for the required period
                if (_stabilityStartTime >= 0f)
                {
                    if (Time.time - _stabilityStartTime > STABILITY_EVALUATE_PERIOD)
                    {
                        // If due to auto-lock
                        if (!_hasTriggeredAutoLock && _autoLockAfterRelease && !_rb.isKinematic)
                        {
                            // Lock in place
                            _rb.velocity = Vector3.zero;
                            _rb.angularVelocity = Vector3.zero;
                            _rb.isKinematic = true;
                            _hasTriggeredAutoLock = true;
                        }
                        // If stable and upright and due to trigger the event
                        if (!_hasTriggeredOnStableUpright)
                        {
                            // Trigger the stability gained event
                            OnStableUpright.Invoke();
                            _hasTriggeredOnStableUpright = true;
                        }
                    }
                }
                else
                {
                    _stabilityStartTime = Time.time;
                }
            }
            else
            {
                _stabilityStartTime = -1f;
            }
        }

        protected override bool TryGetValidRigidbody()
        {
            if (base.TryGetValidRigidbody())
            {
                if ((_rb.constraints & RigidbodyConstraints.FreezeRotation) > 0)
                {
                    Debug.LogWarning("Rigidbody is rotationally constrained, but has a " +
                                     this.GetType().Name + "component; uncheck Rigidbody " +
                                     "constraints on rotation to use this " + this.GetType().Name
                                     + " component.");
                }
                return true;
            }
            return false;
        }

        public override void InitializeManipulation(Manipulation manipulation)
        {
            base.InitializeManipulation(manipulation);

            _stabilityStartTime = -1f;
            _rb.isKinematic = false;
        }

        public override void PerformManipulation(Manipulation manipulation)
        {
            base.PerformManipulation(manipulation);

            if (Target == null || _rb == null)
            {
                return;
            }
            UpdateRotation(manipulation.Manipulator);
        }

        public override void FinalizeManipulation(Manipulation manipulation)
        {
            base.FinalizeManipulation(manipulation);

            _releaseAngularDelta.ToAngleAxis(
                out float angularVelocity, out Vector3 angularVelocityAxis);
            Vector3 releaseAngularVelocity = angularVelocityAxis * angularVelocity;
            _rb.angularVelocity = Vector3.ProjectOnPlane(_rb.angularVelocity, Vector3.up)
                + Vector3.Project(releaseAngularVelocity, Vector3.up);
            _releaseAngularDelta = Quaternion.identity;

            _hasTriggeredAutoLock = false;
            _hasTriggeredOnStableUpright = false;
            _stabilityStartTime = -1f;
        }

        private void UpdateRotation(Manipulator manipulator)
        {
            if (manipulator.Strength > 0)
            {
                Quaternion targetRotation = Quaternion.FromToRotation(Vector3.up, _rb.velocity);
                targetRotation = Quaternion.Slerp(
                    Quaternion.identity, targetRotation,
                    Mathf.Min(_speedThreshold <= Litho.EPSILON ? _rb.velocity.magnitude
                              : (_rb.velocity.magnitude - _speedThreshold) * _speedFactor, 1f));

                Vector3 oldPosition = transform.position;
                Quaternion oldRotation = Target.rotation;
                // Directly rotate to neutralize the orientation of the object
                Target.rotation = Quaternion.Slerp(
                        Target.rotation, targetRotation, BASE_ROTATE_TORQUE * manipulator.Strength);

                // Gradually apply an estimate for angular velocity
                Quaternion rotationDelta = Quaternion.Inverse(oldRotation) * Target.rotation;
                _releaseAngularDelta = Quaternion.Slerp(_releaseAngularDelta, rotationDelta, 0.2f);

                // If holding a handle
                if (GrabMode == GrabType.HoldThisHandle)
                {
                    // Adjust the position of the target such that rotation occurs about this handle
                    Target.position += oldPosition - transform.position;
                }
            }

            // Apply damping to the angular velocity of the object
            _rb.angularVelocity *= 1 - _baseDampingFactor * manipulator.Strength;
        }
    }

}
