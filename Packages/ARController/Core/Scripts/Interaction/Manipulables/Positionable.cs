/////////////////////////////////////////////////
// LITHO SDK                                   //
// Copyright © 2020 Purple Tambourine Ltd.     //
// License: see LICENSE in package root folder //
/////////////////////////////////////////////////

using UnityEngine;

namespace LITHO
{

    /// <summary>
    /// Handles manipulation of the position of the target object whilst it is being interacted
    /// with by a Manipulator
    /// </summary>
    [AddComponentMenu("LITHO/Manipulables/Positionable", -9778)]
    public class Positionable : CentredGrabManipulable
    {
        [SerializeField]
        [Tooltip("Whether to support this object against gravity whilst interacting with it")]
        private bool _counteractGravity = true;

        private bool _hasCounteractedGravity;

        protected Rigidbody _rb;

        protected const float _baseMoveForce = 200f;
        protected float _baseDampingFactor = 0.4f;


        protected override void Awake()
        {
            base.Awake();

            _rb = Target.GetComponentInParent<Rigidbody>();
        }

        protected virtual void FixedUpdate()
        {
            _hasCounteractedGravity = false;
        }

        protected virtual void OnEnable()
        {
            TryGetValidRigidbody();
        }

        // Returns true if any Rigidbody found (even if constraints make it unusable)
        protected virtual bool TryGetValidRigidbody()
        {
            if (_rb == null)
            {
                _rb = Target.GetComponentInParent<Rigidbody>();
                if (_rb != null)
                {
                    Debug.LogWarning("Rigidbody on " + this + " was automatically changed to " +
                                     _rb + " as the previous Rigidbody was unset");
                    if (((_rb.constraints & RigidbodyConstraints.FreezePositionX) > 0)
                        && ((_rb.constraints & RigidbodyConstraints.FreezePositionY) > 0)
                        && ((_rb.constraints & RigidbodyConstraints.FreezePositionZ) > 0))
                    {
                        Debug.LogWarning("Rigidbody on " + this + " is positionally " +
                                         "constrained, but has a " + this.GetType().Name + 
                                         " component; uncheck Rigidbody constraints on X, Y, " +
                                         "and/ or Z position to use this " + this.GetType().Name +
                                         " component.");
                    }
                }
                else
                {
                    Debug.LogError("No Rigidbody component was found for " + this + "; " + 
                                   this.GetType().Name + " script will not work as intended. " +
                                   "Attach a Rigidbody to " + this + " or to one of its parent " +
                                   "objects to fix this.");
                }
            }
            return _rb != null;
        }

        public override void InitializeManipulation(Manipulation manipulation)
        {
            base.InitializeManipulation(manipulation);

            TryGetValidRigidbody();
        }

        public override void PerformManipulation(Manipulation manipulation)
        {
            base.PerformManipulation(manipulation);

            if (_rb == null)
            {
                return;
            }
            UpdatePosition(manipulation.Manipulator);
        }

        private void UpdatePosition(Manipulator manipulator)
        {
            if (_rb == null)
            {
                return;
            }
            // Apply an acceleration (mass-independent) to the grabbed object
            // Pulls towards the target from the handle position and damps the velocity
            Vector3 offsetForce = (manipulator.TargetGrabPosition - manipulator.GrabPosition)
                * _rb.mass * _baseMoveForce * manipulator.Strength;
            // Apply damping to the velocity of the object
            Vector3 centreForce = _rb.mass * _rb.velocity * -_baseDampingFactor
                * manipulator.Strength / Time.fixedDeltaTime;
            // If relevant for this manipulator (and not already done for this frame)
            if (_counteractGravity && _rb.useGravity && !_hasCounteractedGravity)
            {
                centreForce -= Physics.gravity * _rb.mass;
                _hasCounteractedGravity = true;
            }
            _rb.AddForce(centreForce);
            _rb.AddForceAtPosition(offsetForce, manipulator.GrabPosition);

            Rigidbody manipulatorRigidbody = manipulator.GetComponentInParent<Rigidbody>();
            if (manipulatorRigidbody != null)
            {
                manipulatorRigidbody.AddForceAtPosition(-centreForce, _rb.position);
                manipulatorRigidbody.AddForceAtPosition(-offsetForce, manipulator.GrabPosition);
            }
        }
    }

}
